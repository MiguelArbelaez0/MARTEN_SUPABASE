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

app.UseDefaultFiles();

app.UseStaticFiles();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();


// ======================================================
// ENVIAR CÓDIGO DE VERIFICACIÓN
// ======================================================

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


// ======================================================
// HISTORIAL DE VERIFICACIONES
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


app.Run();


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