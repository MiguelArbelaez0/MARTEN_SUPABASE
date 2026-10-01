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

        // ==========================================
        // REMITENTE
        // ==========================================

        message.From.Add(
            new MailboxAddress(
                _smtpFromName,
                _smtpFrom));

        // ==========================================
        // DESTINATARIO
        // ==========================================

        message.To.Add(
            MailboxAddress.Parse(email));

        // ==========================================
        // ASUNTO
        // ==========================================

        message.Subject =
            "Código de verificación - Marten Supabase";

        // ==========================================
        // CONTENIDO DEL CORREO
        // ==========================================

        var body = $"""
        <html>
        <body style="
            font-family: Arial, sans-serif;
            line-height: 1.6;
            color: #222;
        ">

            <h2>
                Verificación de correo electrónico
            </h2>

            <p>
                Has solicitado verificar tu correo electrónico.
            </p>

            <p>
                Tu código de verificación es:
            </p>

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

        // ==========================================
        // CONEXIÓN SMTP
        // ==========================================

        using var smtp = new SmtpClient();

        await smtp.ConnectAsync(
            _smtpHost,
            _smtpPort,
            SecureSocketOptions.StartTls);

        // ==========================================
        // AUTENTICACIÓN
        // ==========================================

        await smtp.AuthenticateAsync(
            _smtpUser,
            _smtpPassword);

        // ==========================================
        // ENVIAR CORREO
        // ==========================================

        await smtp.SendAsync(message);

        // ==========================================
        // CERRAR CONEXIÓN
        // ==========================================

        await smtp.DisconnectAsync(true);
    }
}