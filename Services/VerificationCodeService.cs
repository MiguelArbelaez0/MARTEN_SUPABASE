using System.Security.Cryptography;
using System.Text;
using Marten;
using MARTEN_SUPABASE.Models;

namespace MARTEN_SUPABASE.Services;

public class VerificationCodeService
{
    // Store representa el acceso de Marten
    // a la base de datos PostgreSQL.
    private readonly IDocumentStore _store;

    // Servicio encargado del envío de correos.
    private readonly IEmailService _emailService;


    // Inyección de dependencias.
    // Recibimos Marten y el servicio de correo.
    public VerificationCodeService(
        IDocumentStore store,
        IEmailService emailService)
    {
        _store = store;
        _emailService = emailService;
    }


    // ============================================================
    // GENERACIÓN Y ENVÍO DEL CÓDIGO
    // ============================================================

    public async Task SendCodeAsync(string email)
    {
        // Normalizamos el correo:
        // quitamos espacios y convertimos a minúsculas.
        email = email.Trim().ToLowerInvariant();


        // Generamos un código aleatorio de 6 dígitos.
        var code = RandomNumberGenerator
            .GetInt32(100000, 1000000)
            .ToString();


        // Convertimos el código a SHA-256.
        // En la base de datos almacenaremos este hash.
        var codeHash = ComputeHash(code);


        // Abrimos una sesión ligera de Marten.
        // Esta sesión permitirá consultar y guardar documentos.
        await using var session =
            _store.LightweightSession();


        // Buscamos códigos anteriores del mismo correo
        // que todavía no hayan sido utilizados.
        var existingCodes = await session
            .Query<VerificationCode>()
            .Where(x =>
                x.Email == email &&
                !x.Used)
            .ToListAsync();


        // Si existen códigos anteriores activos,
        // los marcamos como Reemplazado.
        foreach (var existingCode in existingCodes)
        {
            existingCode.Used = true;
            existingCode.Status = "Reemplazado";

            session.Store(existingCode);
        }


        // Obtenemos la fecha y hora actual.
        var now = DateTime.UtcNow;


        // Creamos el nuevo documento.
        var verification = new VerificationCode
        {
            // Generamos un ID único.
            Id = Guid.NewGuid(),

            // Guardamos el correo.
            Email = email,

            // Guardamos solamente el hash.
            CodeHash = codeHash,

            // Guardamos la fecha de creación.
            CreatedAt = now,

            // El código expira después de 10 minutos.
            ExpiresAt = now.AddMinutes(10),

            // Inicialmente no ha sido utilizado.
            Used = false,

            // Todavía no existen intentos.
            Attempts = 0,

            // Estado inicial.
            Status = "Pendiente",

            // Todavía no ha sido verificado.
            VerifiedAt = null
        };


        // Registramos el documento en la sesión de Marten.
        session.Store(verification);


        // Confirmamos la operación.
        // Aquí se persiste la información en PostgreSQL/Supabase.
        await session.SaveChangesAsync();


        // Enviamos el código original al correo.
        // La base de datos conserva únicamente el hash.
        await _emailService.SendVerificationCodeAsync(
            email,
            code);
    }


    // ============================================================
    // VERIFICACIÓN DEL CÓDIGO
    // ============================================================

    public async Task<bool> VerifyCodeAsync(
        string email,
        string code)
    {
        // Normalizamos el correo recibido.
        email = email.Trim().ToLowerInvariant();


        // Abrimos una sesión de Marten.
        await using var session =
            _store.LightweightSession();


        // Buscamos el código pendiente más reciente
        // asociado al correo.
        var verification = await session
            .Query<VerificationCode>()
            .Where(x =>
                x.Email == email &&
                !x.Used)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync();


        // Si no existe ningún código pendiente,
        // la verificación falla.
        if (verification is null)
        {
            return false;
        }


        // Comprobamos si el código ya expiró.
        if (verification.ExpiresAt < DateTime.UtcNow)
        {
            // Marcamos el código como utilizado.
            verification.Used = true;

            // Cambiamos su estado.
            verification.Status = "Expirado";

            // Guardamos los cambios.
            session.Store(verification);

            await session.SaveChangesAsync();

            return false;
        }


        // Comprobamos el límite máximo de intentos.
        if (verification.Attempts >= 5)
        {
            // Bloqueamos el código.
            verification.Used = true;

            verification.Status = "Bloqueado";

            session.Store(verification);

            await session.SaveChangesAsync();

            return false;
        }


        // Incrementamos el contador de intentos.
        verification.Attempts++;


        // Generamos el hash del código
        // que acaba de introducir el usuario.
        var submittedHash = ComputeHash(code);


        // Comparamos el hash recibido
        // con el hash almacenado.
        if (submittedHash != verification.CodeHash)
        {
            // Guardamos el nuevo número de intentos.
            session.Store(verification);

            await session.SaveChangesAsync();

            return false;
        }


        // ========================================================
        // CÓDIGO CORRECTO
        // ========================================================

        // Marcamos el código como utilizado.
        verification.Used = true;


        // Cambiamos el estado a Verificado.
        verification.Status = "Verificado";


        // Registramos la fecha y hora de verificación.
        verification.VerifiedAt = DateTime.UtcNow;


        // Persistimos los cambios.
        session.Store(verification);

        await session.SaveChangesAsync();


        // Informamos que la verificación fue correcta.
        return true;
    }


    // ============================================================
    // SHA-256
    // ============================================================

    private static string ComputeHash(string value)
    {
        // Convertimos el texto a bytes.
        var bytes = Encoding.UTF8.GetBytes(value);


        // Aplicamos SHA-256.
        var hash = SHA256.HashData(bytes);


        // Convertimos el hash a hexadecimal.
        return Convert.ToHexString(hash);
    }
}