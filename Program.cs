using DotNetEnv;
using Marten;
using MARTEN_SUPABASE.Services;

Env.Load();

var builder = WebApplication.CreateBuilder(args);


// ============================================================
// CONFIGURACIÓN DE MARTEN
// ============================================================

// Obtenemos la cadena de conexión de Supabase
// desde las variables de entorno.
var connectionString =
    Environment.GetEnvironmentVariable(
        "SUPABASE_CONNECTION_STRING")
    ?? throw new InvalidOperationException(
        "SUPABASE_CONNECTION_STRING no está configurada.");


// Registramos Marten utilizando PostgreSQL.
builder.Services.AddMarten(options =>
{
    // Configuramos la conexión a Supabase.
    options.Connection(connectionString);

    // Registramos VerificationCode como documento.
    options.Schema.For<MARTEN_SUPABASE.Models.VerificationCode>();
});


// ============================================================
// INYECCIÓN DE DEPENDENCIAS
// ============================================================

// Registramos el servicio de correo.
// Cuando se solicite IEmailService,
// ASP.NET Core utilizará SmtpEmailService.
builder.Services.AddScoped<
    IEmailService,
    SmtpEmailService>();


// Registramos el servicio que contiene
// la lógica de los códigos de verificación.
builder.Services.AddScoped<
    VerificationCodeService>();


// Servicios de OpenAPI para documentación de la API.
builder.Services.AddOpenApi();


var app = builder.Build();


// ============================================================
// ARCHIVOS ESTÁTICOS
// ============================================================

// Permite servir index.html, CSS y JavaScript
// desde la carpeta wwwroot.
app.UseDefaultFiles();
app.UseStaticFiles();


if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}


// Redirección HTTPS cuando corresponde.
app.UseHttpsRedirection();


// ============================================================
// VALIDACIÓN DE GMAIL
// ============================================================

// Comprueba que el correo pertenezca a Gmail.
static bool IsGmailAddress(string email)
{
    if (string.IsNullOrWhiteSpace(email))
    {
        return false;
    }

    email = email.Trim();

    return email.EndsWith(
        "@gmail.com",
        StringComparison.OrdinalIgnoreCase);
}


// ============================================================
// ENDPOINT PARA ENVIAR CÓDIGO
// ============================================================

app.MapPost(
    "/auth/send-code",
    async (
        SendCodeRequest request,
        VerificationCodeService service) =>
    {
        // Validamos que exista un correo.
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return Results.BadRequest(
                "El correo es obligatorio.");
        }


        // Validamos que sea Gmail.
        if (!IsGmailAddress(request.Email))
        {
            return Results.BadRequest(
                "Solo se permiten direcciones de correo Gmail (@gmail.com).");
        }


        try
        {
            // Solicitamos al servicio
            // generar y enviar el código.
            await service.SendCodeAsync(
                request.Email);

            return Results.Ok(
                "Código de verificación enviado.");
        }
        catch (Exception ex)
        {
            // Registramos el error para diagnóstico.
            Console.WriteLine(ex);

            return Results.Problem(
                detail: ex.Message,
                statusCode: 500);
        }
    });


// ============================================================
// ENDPOINT PARA VERIFICAR CÓDIGO
// ============================================================

app.MapPost(
    "/auth/verify-code",
    async (
        VerifyCodeRequest request,
        VerificationCodeService service) =>
    {
        // Validamos el correo.
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return Results.BadRequest(
                "El correo es obligatorio.");
        }


        // Validamos que sea Gmail.
        if (!IsGmailAddress(request.Email))
        {
            return Results.BadRequest(
                "Solo se permiten direcciones de correo Gmail (@gmail.com).");
        }


        // Validamos que exista un código.
        if (string.IsNullOrWhiteSpace(request.Code))
        {
            return Results.BadRequest(
                "El código es obligatorio.");
        }


        // Ejecutamos la lógica de verificación.
        var verified =
            await service.VerifyCodeAsync(
                request.Email,
                request.Code);


        // Si el código no es válido.
        if (!verified)
        {
            return Results.BadRequest(
                "El código no es válido.");
        }


        // Si todo fue correcto.
        return Results.Ok(
            "Código verificado correctamente.");
    });


// ============================================================
// HISTORIAL
// ============================================================

app.MapGet(
    "/auth/history",
    async (IDocumentStore store) =>
    {
        // Abrimos una sesión de Marten.
        await using var session =
            store.LightweightSession();


        // Consultamos los últimos 20 registros.
        var history = await session
            .Query<MARTEN_SUPABASE.Models.VerificationCode>()
            .OrderByDescending(x => x.CreatedAt)
            .Take(20)
            .ToListAsync();


        // Seleccionamos los campos
        // que queremos mostrar.
        var result = history.Select(x => new
        {
            id = x.Id,
            email = x.Email,
            createdAt = x.CreatedAt,
            expiresAt = x.ExpiresAt,
            attempts = x.Attempts,
            status = x.Status,
            verifiedAt = x.VerifiedAt
        });


        // Devolvemos el historial como JSON.
        return Results.Ok(result);
    });


app.Run();


// ============================================================
// MODELOS DE REQUEST
// ============================================================

// Información necesaria para solicitar un código.
public record SendCodeRequest(
    string Email);


// Información necesaria para verificar un código.
public record VerifyCodeRequest(
    string Email,
    string Code);