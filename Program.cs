using DotNetEnv;
using Marten;
using MARTEN_SUPABASE.Services;

Env.Load();

var builder = WebApplication.CreateBuilder(args);


// ==========================================
// CONEXIÓN A SUPABASE
// ==========================================

var connectionString =
    Environment.GetEnvironmentVariable(
        "SUPABASE_CONNECTION_STRING");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "No se encontró SUPABASE_CONNECTION_STRING en .env.");
}


// ==========================================
// COMPROBAR USUARIO DE POSTGRESQL
// ==========================================

var username = connectionString
    .Split("Username=")[1]
    .Split(";")[0];

Console.WriteLine(
    $"USUARIO POSTGRESQL LEÍDO: {username}");


// ==========================================
// MARTEN
// ==========================================

builder.Services
    .AddMarten(options =>
    {
        options.Connection(connectionString);
    })
    .UseLightweightSessions()
    .ApplyAllDatabaseChangesOnStartup();


// ==========================================
// SERVICIOS
// ==========================================

builder.Services.AddScoped<IEmailService, SmtpEmailService>();

builder.Services.AddScoped<VerificationCodeService>();

builder.Services.AddOpenApi();


// ==========================================
// APLICACIÓN
// ==========================================

var app = builder.Build();


// ==========================================
// OPENAPI
// ==========================================

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}


// ==========================================
// HTTPS
// ==========================================

app.UseHttpsRedirection();


// ==========================================
// ENVIAR CÓDIGO DE VERIFICACIÓN
// ==========================================

app.MapPost(
    "/auth/send-code",
    async (
        SendCodeRequest request,
        VerificationCodeService service) =>
    {
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return Results.BadRequest(new
            {
                message =
                    "El correo electrónico es obligatorio."
            });
        }

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
            return Results.Problem(
                detail: ex.Message,
                statusCode: 500);
        }
    });


// ==========================================
// VALIDAR CÓDIGO
// ==========================================

app.MapPost(
    "/auth/verify-code",
    async (
        VerifyCodeRequest request,
        VerificationCodeService service) =>
    {
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return Results.BadRequest(new
            {
                message =
                    "El correo electrónico es obligatorio."
            });
        }

        if (string.IsNullOrWhiteSpace(request.Code))
        {
            return Results.BadRequest(new
            {
                message =
                    "El código es obligatorio."
            });
        }

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


// ==========================================
// EJECUTAR
// ==========================================

app.Run();


// ==========================================
// REQUESTS
// ==========================================

public record SendCodeRequest(
    string Email
);

public record VerifyCodeRequest(
    string Email,
    string Code
);