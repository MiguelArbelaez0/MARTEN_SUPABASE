using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace MARTEN_SUPABASE.Services;

public class SmtpEmailService : IEmailService
{
    private readonly string _smtpHost;
    private readonly int _smtpPort;
    private readonly string _smtpUser;
    private readonly string _smtpPassword;
    private readonly string _smtpFrom;
    private readonly string _smtpFromName;

    public SmtpEmailService()
    {
        _smtpHost =
            Environment.GetEnvironmentVariable("SMTP_HOST")
            ?? throw new InvalidOperationException(
                "Falta SMTP_HOST en .env.");

        _smtpPort = int.Parse(
            Environment.GetEnvironmentVariable("SMTP_PORT")
            ?? "587");

        _smtpUser =
            Environment.GetEnvironmentVariable("SMTP_USER")
            ?? throw new InvalidOperationException(
                "Falta SMTP_USER en .env.");

        _smtpPassword =
            Environment.GetEnvironmentVariable("SMTP_PASSWORD")
            ?? throw new InvalidOperationException(
                "Falta SMTP_PASSWORD en .env.");

        _smtpFrom =
            Environment.GetEnvironmentVariable("SMTP_FROM")
            ?? _smtpUser;

        _smtpFromName =
            Environment.GetEnvironmentVariable("SMTP_FROM_NAME")
            ?? "Marten Supabase";
    }

    public async Task SendVerificationCodeAsync(
        string email,
        string code)
    {
        var message = new MimeMessage();

        message.From.Add(
            new MailboxAddress(
                _smtpFromName,
                _smtpFrom));

        message.To.Add(
            MailboxAddress.Parse(email));

        message.Subject =
            "Código de verificación - Marten Supabase";

        var body = $"""
        <html>
        <body style="font-family: Arial, sans-serif;">

            <h2>Verificación de correo electrónico</h2>

            <p>Tu código de verificación es:</p>

            <div style="
                font-size: 32px;
                font-weight: bold;
                letter-spacing: 8px;
                margin: 20px 0;
            ">
                {code}
            </div>

            <p>
                Este código tiene una validez de
                <strong>10 minutos</strong>.
            </p>

            <p>
                Si no solicitaste este código,
                puedes ignorar este mensaje.
            </p>

            <hr>

            <small>
                Ejercicio académico - Marten + Supabase
            </small>

        </body>
        </html>
        """;

        message.Body = new BodyBuilder
        {
            HtmlBody = body
        }.ToMessageBody();

        using var smtp = new SmtpClient();

        await smtp.ConnectAsync(
            _smtpHost,
            _smtpPort,
            SecureSocketOptions.StartTls);

        await smtp.AuthenticateAsync(
            _smtpUser,
            _smtpPassword);

        await smtp.SendAsync(message);

        await smtp.DisconnectAsync(true);
    }
}