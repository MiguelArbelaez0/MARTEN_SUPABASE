using System.Security.Cryptography;
using System.Text;
using Marten;
using MARTEN_SUPABASE.Models;

namespace MARTEN_SUPABASE.Services;

public class VerificationCodeService
{
    // ============================================================
    // DEPENDENCIAS
    // ============================================================

    // IDocumentStore permite acceder a Marten.
    private readonly IDocumentStore _store;

    // Servicio encargado de enviar el código por correo.
    private readonly IEmailService _emailService;


    // ============================================================
    // CONSTRUCTOR
    // ============================================================

    public VerificationCodeService(
        IDocumentStore store,
        IEmailService emailService)
    {
        _store = store;
        _emailService = emailService;
    }


    // ============================================================
    // EVENTO: SOLICITAR CÓDIGO
    // ============================================================

    // Este método se ejecuta cuando el usuario solicita
    // un nuevo código de verificación.
    public async Task SendCodeAsync(string email)
    {
        // Normalizamos el correo para evitar diferencias
        // entre mayúsculas, minúsculas y espacios.
        email = email.Trim().ToLowerInvariant();


        // ========================================================
        // GENERACIÓN SEGURA DEL CÓDIGO
        // ========================================================

        // Generamos un código aleatorio de exactamente
        // 6 dígitos utilizando RandomNumberGenerator.
        //
        // Rango:
        //
        // 100000 → mínimo
        // 999999 → máximo
        //
        // Esto produce códigos como:
        //
        // 583214
        // 742891
        // 105637
        var code = RandomNumberGenerator
            .GetInt32(100000, 1000000)
            .ToString();


        // Calculamos el hash SHA-256.
        //
        // El código original NO se guarda en la base de datos.
        var codeHash = ComputeHash(code);


        // ========================================================
        // SESIÓN DE MARTEN
        // ========================================================

        // Abrimos una sesión ligera para trabajar
        // con los documentos almacenados por Marten.
        await using var session =
            _store.LightweightSession();


        // ========================================================
        // EVENTO: SOLICITAR UN NUEVO CÓDIGO
        // ========================================================

        // Buscamos códigos anteriores del mismo correo
        // que todavía no hayan sido utilizados.
        var existingCodes = await session
            .Query<VerificationCode>()
            .Where(x =>
                x.Email == email &&
                !x.Used)
            .ToListAsync();


        // ========================================================
        // TRANSICIÓN: PENDIENTE → REEMPLAZADO
        // ========================================================

        // Si ya existía un código pendiente y el usuario
        // solicita otro, el código anterior deja de ser válido.
        foreach (var existingCode in existingCodes)
        {
            existingCode.Used = true;

            existingCode.Status = "Reemplazado";

            session.Store(existingCode);
        }


        // ========================================================
        // CONTROL DEL TIEMPO
        // ========================================================

        var now = DateTime.UtcNow;


        // ========================================================
        // CREACIÓN DEL NUEVO ESTADO
        // ========================================================

        // El nuevo código comienza siempre en:
        //
        // PENDIENTE
        //
        // Porque todavía no ha sido verificado.
        var verification = new VerificationCode
        {
            Id = Guid.NewGuid(),

            Email = email,

            CodeHash = codeHash,

            CreatedAt = now,

            // El código solamente será válido durante
            // 10 minutos.
            ExpiresAt = now.AddMinutes(10),

            Used = false,

            Attempts = 0,

            Status = "Pendiente",

            VerifiedAt = null
        };


        // ========================================================
        // PERSISTENCIA DEL NUEVO ESTADO
        // ========================================================

        // Registramos el documento en la sesión de Marten.
        session.Store(verification);

        // Guardamos definitivamente los cambios
        // en PostgreSQL/Supabase.
        await session.SaveChangesAsync();


        // ========================================================
        // EVENTO: ENVÍO DEL CÓDIGO
        // ========================================================

        // Después de guardar el código,
        // enviamos el código original al correo.
        //
        // IMPORTANTE:
        //
        // El usuario recibe el código original,
        // pero la base de datos solamente conserva su hash.
        await _emailService.SendVerificationCodeAsync(
            email,
            code);
    }


    // ============================================================
    // EVENTO: INTENTAR VERIFICAR CÓDIGO
    // ============================================================

    public async Task<bool> VerifyCodeAsync(
        string email,
        string code)
    {
        // Normalizamos el correo.
        email = email.Trim().ToLowerInvariant();


        // Abrimos una sesión de Marten.
        await using var session =
            _store.LightweightSession();


        // ========================================================
        // BUSCAR EL CÓDIGO ACTIVO
        // ========================================================

        // Buscamos el código más reciente que todavía
        // no haya sido utilizado.
        var verification = await session
            .Query<VerificationCode>()
            .Where(x =>
                x.Email == email &&
                !x.Used)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync();


        // Si no existe un código disponible,
        // la verificación falla.
        if (verification is null)
        {
            return false;
        }


        // ========================================================
        // EVENTO: EXPIRACIÓN
        // ========================================================

        // Comprobamos si ya pasó el tiempo de vigencia.
        if (verification.ExpiresAt < DateTime.UtcNow)
        {
            // El código deja de estar disponible.
            verification.Used = true;

            // TRANSICIÓN:
            //
            // PENDIENTE → EXPIRADO
            verification.Status = "Expirado";

            // Persistimos el nuevo estado.
            session.Store(verification);

            await session.SaveChangesAsync();

            return false;
        }


        // ========================================================
        // EVENTO: LÍMITE DE INTENTOS
        // ========================================================

        // Verificamos si ya alcanzó el límite permitido.
        if (verification.Attempts >= 5)
        {
            // El código deja de estar disponible.
            verification.Used = true;

            // TRANSICIÓN:
            //
            // PENDIENTE → BLOQUEADO
            verification.Status = "Bloqueado";

            // Persistimos el estado.
            session.Store(verification);

            await session.SaveChangesAsync();

            return false;
        }


        // ========================================================
        // REGISTRAR INTENTO
        // ========================================================

        // Cada intento de verificación incrementa
        // el contador.
        verification.Attempts++;


        // ========================================================
        // COMPARACIÓN SEGURA
        // ========================================================

        // Calculamos SHA-256 del código que acaba
        // de introducir el usuario.
        var submittedHash = ComputeHash(code);


        // Comparamos:
        //
        // Hash recibido
        //        VS
        // Hash almacenado
        //
        // Si son diferentes, el código es incorrecto.
        if (submittedHash != verification.CodeHash)
        {
            // Guardamos el incremento del intento.
            session.Store(verification);

            await session.SaveChangesAsync();

            // El código continúa pendiente,
            // pero ahora tiene un intento adicional.
            return false;
        }


        // ========================================================
        // EVENTO: VERIFICACIÓN CORRECTA
        // ========================================================

        // El código ya fue utilizado correctamente.
        verification.Used = true;


        // TRANSICIÓN:
        //
        // PENDIENTE → VERIFICADO
        verification.Status = "Verificado";


        // Guardamos el momento exacto de la verificación.
        verification.VerifiedAt =
            DateTime.UtcNow;


        // ========================================================
        // PERSISTENCIA DEL NUEVO ESTADO
        // ========================================================

        session.Store(verification);

        await session.SaveChangesAsync();


        // Informamos al endpoint que la verificación
        // fue exitosa.
        return true;
    }


    // ============================================================
    // SHA-256
    // ============================================================

    private static string ComputeHash(string value)
    {
        // Convertimos el texto a bytes.
        var bytes =
            Encoding.UTF8.GetBytes(value);


        // Calculamos el hash SHA-256.
        var hash =
            SHA256.HashData(bytes);


        // Convertimos el resultado a hexadecimal.
        return Convert.ToHexString(hash);
    }
}