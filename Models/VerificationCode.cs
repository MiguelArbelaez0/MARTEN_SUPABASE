namespace MARTEN_SUPABASE.Models;

public class VerificationCode
{
    public Guid Id { get; set; }

    public string Email { get; set; } = string.Empty;

    public string CodeHash { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime ExpiresAt { get; set; }

    public bool Used { get; set; }

    public int Attempts { get; set; }

    public string Status { get; set; } = "Pendiente";

    public DateTime? VerifiedAt { get; set; }
}