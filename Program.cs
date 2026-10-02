using DotNetEnv;
using Marten;
using MARTEN_SUPABASE.Services;

Env.Load();

var builder = WebApplication.CreateBuilder(args);

var connectionString =
    Environment.GetEnvironmentVariable(
        "SUPABASE_CONNECTION_STRING");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "No se encontró SUPABASE_CONNECTION_STRING en .env.");
}

var username = connectionString
    .Split("Username=")[1]
    .Split(";")[0];

Console.WriteLine(
    $"USUARIO POSTGRESQL LEÍDO: {username}");


// ======================================================
// SERVICIOS
// ======================================================

builder.Services
    .AddMarten(options =>
    {
        options.Connection(connectionString);
    })
    .UseLightweightSessions()
    .ApplyAllDatabaseChangesOnStartup();

builder.Services.AddScoped<IEmailService, SmtpEmailService>();

builder.Services.AddScoped<VerificationCodeService>();

builder.Services.AddOpenApi();

var app = builder.Build();


// ======================================================
// ARCHIVOS WEB
// ======================================================

app.UseDefaultFiles();

app.UseStaticFiles();


// ======================================================
// OPENAPI
// ======================================================

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}


// ======================================================
// HTTPS
// ======================================================

app.UseHttpsRedirection();


// ======================================================
// ENVIAR CÓDIGO
// ======================================================

app.MapPost(
    "/auth/send-code",
    async (
        SendCodeRequest request,
        VerificationCodeService service) =>
    {
        // ----------------------------------------------
        // VALIDAR CORREO VACÍO
        // ----------------------------------------------

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return Results.BadRequest(new
            {
                message =
                    "El correo electrónico es obligatorio."
            });
        }


        // ----------------------------------------------
        // VALIDAR QUE SEA GMAIL
        // ----------------------------------------------

        if (!IsGmailAddress(request.Email))
        {
            return Results.BadRequest(new
            {
                message =
                    "Solo se permiten direcciones de correo Gmail (@gmail.com)."
            });
        }


        // ----------------------------------------------
        // ENVIAR CÓDIGO
        // ----------------------------------------------

        try
        {
            await service.SendCodeAsync(
                request.Email);

            return Results.Ok(new
            {
                message =
                    "Código de verificación enviado."
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine();

            Console.WriteLine(
                "==========================================");

            Console.WriteLine(
                "ERROR EN EL ENVÍO DEL CÓDIGO");

            Console.WriteLine(
                "==========================================");

            Console.WriteLine(
                ex.ToString());

            Console.WriteLine(
                "==========================================");

            Console.WriteLine();

            return Results.Problem(
                detail: ex.Message,
                statusCode: 500);
        }
    });


// ======================================================
// VERIFICAR CÓDIGO
// ======================================================

app.MapPost(
    "/auth/verify-code",
    async (
        VerifyCodeRequest request,
        VerificationCodeService service) =>
    {
        // ----------------------------------------------
        // VALIDAR CORREO
        // ----------------------------------------------

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return Results.BadRequest(new
            {
                message =
                    "El correo electrónico es obligatorio."
            });
        }


        // ----------------------------------------------
        // VALIDAR QUE SEA GMAIL
        // ----------------------------------------------

        if (!IsGmailAddress(request.Email))
        {
            return Results.BadRequest(new
            {
                message =
                    "Solo se permiten direcciones de correo Gmail (@gmail.com)."
            });
        }


        // ----------------------------------------------
        // VALIDAR CÓDIGO VACÍO
        // ----------------------------------------------

        if (string.IsNullOrWhiteSpace(request.Code))
        {
            return Results.BadRequest(new
            {
                message =
                    "El código es obligatorio."
            });
        }


        // ----------------------------------------------
        // VERIFICAR CÓDIGO
        // ----------------------------------------------

        var valid =
            await service.VerifyCodeAsync(
                request.Email,
                request.Code);


        if (!valid)
        {
            return Results.BadRequest(new
            {
                verified = false,

                message =
                    "Código incorrecto, expirado o inválido."
            });
        }


        return Results.Ok(new
        {
            verified = true,

            message =
                "Correo electrónico verificado correctamente."
        });
    });


// ======================================================
// HISTORIAL
// ======================================================

app.MapGet(
    "/auth/history",
    async (IDocumentStore store) =>
    {
        await using var session =
            store.LightweightSession();

        var history = await session
            .Query<MARTEN_SUPABASE.Models.VerificationCode>()
            .OrderByDescending(x => x.CreatedAt)
            .Take(20)
            .ToListAsync();

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

        return Results.Ok(result);
    });


// ======================================================
// INICIAR APLICACIÓN
// ======================================================

app.Run();


// ======================================================
// VALIDACIÓN DE GMAIL
// ======================================================

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


// ======================================================
// REQUESTS
// ======================================================

public record SendCodeRequest(
    string Email
);

public record VerifyCodeRequest(
    string Email,
    string Code
);