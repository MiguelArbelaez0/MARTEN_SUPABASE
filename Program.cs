using DotNetEnv;
using Marten;
using MARTEN_SUPABASE.Services;


// ============================================================
// CARGA DE VARIABLES DE ENTORNO
// ============================================================

// DotNetEnv permite leer las variables que tenemos
// almacenadas en el archivo .env.
//
// Allí tenemos información sensible como:
// - Conexión a Supabase
// - Usuario SMTP
// - Contraseña/App Password de Gmail
//
// De esta manera NO escribimos las contraseñas
// directamente dentro del código fuente.

Env.Load();


var builder = WebApplication.CreateBuilder(args);


// ============================================================
// CONFIGURACIÓN DE MARTEN + SUPABASE
// ============================================================

// Obtenemos la cadena de conexión de Supabase
// desde la variable de entorno.
//
// La cadena contiene:
// - Host
// - Puerto
// - Base de datos
// - Usuario
// - Contraseña
// - SSL

var connectionString =
    Environment.GetEnvironmentVariable(
        "SUPABASE_CONNECTION_STRING")
    ?? throw new InvalidOperationException(
        "SUPABASE_CONNECTION_STRING no está configurada.");


// Registramos Marten en la aplicación.
//
// Marten funciona como Document Store y utiliza
// PostgreSQL como sistema de almacenamiento.
//
// En nuestro proyecto PostgreSQL está alojado
// en Supabase.

builder.Services.AddMarten(options =>
{
    // Indicamos a Marten dónde está
    // nuestra base de datos PostgreSQL.

    options.Connection(connectionString);


    // Registramos VerificationCode como documento
    // que será administrado y almacenado por Marten.

    options.Schema.For<
        MARTEN_SUPABASE.Models.VerificationCode>();
});


// ============================================================
// INYECCIÓN DE DEPENDENCIAS
// ============================================================

// Registramos el servicio encargado
// del envío de correos.
//
// Cuando la aplicación necesite IEmailService,
// ASP.NET Core utilizará SmtpEmailService.

builder.Services.AddScoped<
    IEmailService,
    SmtpEmailService>();


// Registramos el servicio que contiene
// toda la lógica de los códigos.
//
// Aquí se encuentra la lógica para:
// - Generar códigos
// - Guardarlos
// - Verificarlos
// - Controlar expiración
// - Controlar intentos
// - Cambiar estados

builder.Services.AddScoped<
    VerificationCodeService>();


// ============================================================
// OPENAPI
// ============================================================

// Permite generar documentación de los
// endpoints de la API durante desarrollo.

builder.Services.AddOpenApi();


var app = builder.Build();


// ============================================================
// ARCHIVOS ESTÁTICOS
// ============================================================

// Permite que ASP.NET Core sirva los archivos
// que están dentro de wwwroot:
//
// index.html
// styles.css
// app.js

app.UseDefaultFiles();

app.UseStaticFiles();


// ============================================================
// OPENAPI EN DESARROLLO
// ============================================================

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}


// ============================================================
// HTTPS
// ============================================================

// Utiliza HTTPS cuando existe una configuración
// HTTPS disponible.

app.UseHttpsRedirection();


// ============================================================
// VALIDACIÓN DE CORREO GMAIL
// ============================================================

// Esta función verifica que el correo introducido
// por el usuario pertenezca al dominio Gmail.
//
// IMPORTANTE:
//
// NO estamos comprobando que sea nuestro correo.
//
// Estamos permitiendo cualquier cuenta Gmail.
//
// Ejemplos permitidos:
//
// usuario@gmail.com
// profesor@gmail.com
// cualquierpersona@gmail.com
//
// Ejemplos rechazados:
//
// usuario@hotmail.com
// usuario@outlook.com
// usuario@yahoo.com

static bool IsGmailAddress(string email)
{
    // Si el correo está vacío,
// devolvemos false.

    if (string.IsNullOrWhiteSpace(email))
    {
        return false;
    }


    // Eliminamos espacios al principio
    // y al final.

    email = email.Trim();


    // Comprobamos que termine en @gmail.com.
    //
    // OrdinalIgnoreCase permite aceptar
    // mayúsculas y minúsculas.

    return email.EndsWith(
        "@gmail.com",
        StringComparison.OrdinalIgnoreCase);
}


// ============================================================
// ENDPOINT: ENVIAR CÓDIGO
// ============================================================

// Este endpoint recibe una solicitud POST:
//
// POST /auth/send-code
//
// El frontend envía el correo que escribió
// el usuario.
//
// Ejemplo:
//
// {
//     "email": "usuario@gmail.com"
// }

app.MapPost(
    "/auth/send-code",
    async (
        SendCodeRequest request,
        VerificationCodeService service) =>
    {
        // Primero comprobamos que
        // el usuario haya escrito un correo.

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return Results.BadRequest(
                "El correo es obligatorio.");
        }


        // Después verificamos que sea
        // una cuenta Gmail.

        if (!IsGmailAddress(request.Email))
        {
            return Results.BadRequest(
                "Solo se permiten direcciones de correo Gmail (@gmail.com).");
        }


        try
        {
            // Aquí empieza realmente
            // el proceso de generación del código.
            //
            // Le pasamos al servicio EXACTAMENTE
            // el correo que escribió el usuario.
            //
            // Por eso cualquier Gmail puede recibir
            // su propio código.

            await service.SendCodeAsync(
                request.Email);


            // Si todo funciona correctamente,
            // devolvemos una respuesta HTTP 200.

            return Results.Ok(
                "Código de verificación enviado.");
        }
        catch (Exception ex)
        {
            // Mostramos el error en la consola
            // para facilitar el diagnóstico.

            Console.WriteLine(ex);


            // Devolvemos un error HTTP 500
            // al frontend.

            return Results.Problem(
                detail: ex.Message,
                statusCode: 500);
        }
    });


// ============================================================
// ENDPOINT: VERIFICAR CÓDIGO
// ============================================================

// Este endpoint recibe:
//
// POST /auth/verify-code
//
// Recibe:
//
// {
//     "email": "usuario@gmail.com",
//     "code": "123456"
// }

app.MapPost(
    "/auth/verify-code",
    async (
        VerifyCodeRequest request,
        VerificationCodeService service) =>
    {
        // Validamos que exista un correo.

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return Results.BadRequest(
                "El correo es obligatorio.");
        }


        // Volvemos a comprobar que sea Gmail.

        if (!IsGmailAddress(request.Email))
        {
            return Results.BadRequest(
                "Solo se permiten direcciones de correo Gmail (@gmail.com).");
        }


        // Comprobamos que el código exista.

        if (string.IsNullOrWhiteSpace(request.Code))
        {
            return Results.BadRequest(
                "El código es obligatorio.");
        }


        // Enviamos el correo y el código
        // al servicio de verificación.

        var verified =
            await service.VerifyCodeAsync(
                request.Email,
                request.Code);


        // Si el servicio devuelve false,
        // el código no es válido.

        if (!verified)
        {
            return Results.BadRequest(
                "El código no es válido.");
        }


        // Si devuelve true,
        // la verificación fue correcta.

        return Results.Ok(
            "Código verificado correctamente.");
    });


// ============================================================
// ENDPOINT: HISTORIAL
// ============================================================

// Este endpoint:
//
// GET /auth/history
//
// Consulta los registros almacenados
// por Marten en PostgreSQL/Supabase.

app.MapGet(
    "/auth/history",
    async (IDocumentStore store) =>
    {
        // Abrimos una sesión ligera de Marten.
//
// LightweightSession permite trabajar
// con la base de datos sin necesidad
// de cargar funcionalidades adicionales.

        await using var session =
            store.LightweightSession();


        // Consultamos los documentos
// VerificationCode almacenados.
//
// Los ordenamos del más reciente
// al más antiguo.
//
// Limitamos la consulta a 20 registros.

        var history = await session
            .Query<
                MARTEN_SUPABASE.Models.VerificationCode>()
            .OrderByDescending(
                x => x.CreatedAt)
            .Take(20)
            .ToListAsync();


        // Seleccionamos solamente
// los datos que queremos enviar
// al frontend.

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


        // Devolvemos el historial
// como JSON.

        return Results.Ok(result);
    });


// ============================================================
// INICIAR APLICACIÓN
// ============================================================

// Ejecuta el servidor ASP.NET Core.

app.Run();


// ============================================================
// MODELOS DE REQUEST
// ============================================================

// Este record representa la información
// necesaria para solicitar un código.
//
// El frontend solamente necesita enviar
// el correo.

public record SendCodeRequest(
    string Email);


// Este record representa la información
// necesaria para verificar un código.
//
// Necesitamos:
// - Correo
// - Código recibido

public record VerifyCodeRequest(
    string Email,
    string Code);