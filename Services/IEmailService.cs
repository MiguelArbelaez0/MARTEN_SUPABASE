namespace MARTEN_SUPABASE.Services;

public interface IEmailService
{
    Task SendVerificationCodeAsync(
        string email,
        string code);
}