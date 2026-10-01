using System.Security.Cryptography;
using System.Text;
using Marten;
using MARTEN_SUPABASE.Models;

namespace MARTEN_SUPABASE.Services;

public class VerificationCodeService
{
    private readonly IDocumentStore _store;
    private readonly IEmailService _emailService;

    public VerificationCodeService(
        IDocumentStore store,
        IEmailService emailService)
    {
        _store = store;
        _emailService = emailService;
    }

    public async Task SendCodeAsync(string email)
    {
        email = email.Trim().ToLowerInvariant();

        // Generar código aleatorio de 6 dígitos
        var code = RandomNumberGenerator
            .GetInt32(100000, 1000000)
            .ToString();

        // Guardamos solamente el hash
        var codeHash = ComputeHash(code);

        await using var session =
            _store.LightweightSession();

        // Invalidar códigos anteriores del mismo correo
        var existingCodes = await session
            .Query<VerificationCode>()
            .Where(x =>
                x.Email == email &&
                !x.Used)
            .ToListAsync();

        foreach (var existingCode in existingCodes)
        {
            existingCode.Used = true;
            session.Store(existingCode);
        }

        // Crear nuevo código
        var verification = new VerificationCode
        {
            Id = Guid.NewGuid(),
            Email = email,
            CodeHash = codeHash,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(10),
            Used = false,
            Attempts = 0
        };

        session.Store(verification);

        await session.SaveChangesAsync();

        // Enviar código al correo
        await _emailService.SendVerificationCodeAsync(
            email,
            code);
    }

    public async Task<bool> VerifyCodeAsync(
        string email,
        string code)
    {
        email = email.Trim().ToLowerInvariant();

        await using var session =
            _store.LightweightSession();

        // Buscar el código más reciente
        var verification = await session
            .Query<VerificationCode>()
            .Where(x =>
                x.Email == email &&
                !x.Used)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync();

        if (verification is null)
        {
            return false;
        }

        // Comprobar expiración
        if (verification.ExpiresAt < DateTime.UtcNow)
        {
            verification.Used = true;

            session.Store(verification);

            await session.SaveChangesAsync();

            return false;
        }

        // Máximo 5 intentos
        if (verification.Attempts >= 5)
        {
            verification.Used = true;

            session.Store(verification);

            await session.SaveChangesAsync();

            return false;
        }

        verification.Attempts++;

        // Comparar hash
        var submittedHash = ComputeHash(code);

        if (submittedHash != verification.CodeHash)
        {
            session.Store(verification);

            await session.SaveChangesAsync();

            return false;
        }

        // Código correcto
        verification.Used = true;

        session.Store(verification);

        await session.SaveChangesAsync();

        return true;
    }

    private static string ComputeHash(string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);

        var hash = SHA256.HashData(bytes);

        return Convert.ToHexString(hash);
    }
}