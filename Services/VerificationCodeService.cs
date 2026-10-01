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

        var code = RandomNumberGenerator
            .GetInt32(100000, 1000000)
            .ToString();

        var codeHash = ComputeHash(code);

        await using var session =
            _store.LightweightSession();

        var existingCodes = await session
            .Query<VerificationCode>()
            .Where(x =>
                x.Email == email &&
                !x.Used)
            .ToListAsync();

        foreach (var existingCode in existingCodes)
        {
            existingCode.Used = true;
            existingCode.Status = "Reemplazado";

            session.Store(existingCode);
        }

        var now = DateTime.UtcNow;

        var verification = new VerificationCode
        {
            Id = Guid.NewGuid(),
            Email = email,
            CodeHash = codeHash,
            CreatedAt = now,
            ExpiresAt = now.AddMinutes(10),
            Used = false,
            Attempts = 0,
            Status = "Pendiente",
            VerifiedAt = null
        };

        session.Store(verification);

        await session.SaveChangesAsync();

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

        if (verification.ExpiresAt < DateTime.UtcNow)
        {
            verification.Used = true;
            verification.Status = "Expirado";

            session.Store(verification);

            await session.SaveChangesAsync();

            return false;
        }

        if (verification.Attempts >= 5)
        {
            verification.Used = true;
            verification.Status = "Bloqueado";

            session.Store(verification);

            await session.SaveChangesAsync();

            return false;
        }

        verification.Attempts++;

        var submittedHash = ComputeHash(code);

        if (submittedHash != verification.CodeHash)
        {
            session.Store(verification);

            await session.SaveChangesAsync();

            return false;
        }

        verification.Used = true;
        verification.Status = "Verificado";
        verification.VerifiedAt = DateTime.UtcNow;

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