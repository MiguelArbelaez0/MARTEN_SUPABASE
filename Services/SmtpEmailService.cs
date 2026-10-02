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


    // El constructor obtiene la configuración
    // desde las variables de entorno del archivo .env.
    public SmtpEmailService()
    {
        _smtpHost =
            Environment.GetEnvironmentVariable("SMTP_HOST")
            ?? "smtp.gmail.com";

        _smtpPort =
            int.TryParse(
                Environment.GetEnvironmentVariable("SMTP_PORT"),
                out var port)
                ? port
                : 587;

        _smtpUser =
            Environment.GetEnvironmentVariable("SMTP_USER")
            ?? throw new InvalidOperationException(
                "SMTP_USER no está configurado.");

        _smtpPassword =
            Environment.GetEnvironmentVariable("SMTP_PASSWORD")
            ?? throw new InvalidOperationException(
                "SMTP_PASSWORD no está configurado.");

        _smtpFrom =
            Environment.GetEnvironmentVariable("SMTP_FROM")
            ?? _smtpUser;

        _smtpFromName =
            Environment.GetEnvironmentVariable("SMTP_FROM_NAME")
            ?? "Marten Supabase";
    }


    // Este método implementa el contrato definido
    // anteriormente en IEmailService.
    public async Task SendVerificationCodeAsync(
        string email,
        string code)
    {
        // Creamos el mensaje de correo.
        var message = new MimeMessage();


        // Definimos quién envía el correo.
        message.From.Add(
            new MailboxAddress(
                _smtpFromName,
                _smtpFrom));


        // Definimos quién recibe el código.
        message.To.Add(
            MailboxAddress.Parse(email));


        // Definimos el asunto.
        message.Subject =
            "Código de verificación - Marten Supabase";


        // Construimos el contenido del correo.
        var body = new BodyBuilder
        {
            HtmlBody = $"""
                <h2>Código de verificación</h2>

                <p>Tu código de verificación es:</p>

                <h1>{code}</h1>

                <p>
                    Este código tiene una vigencia de 10 minutos.
                </p>
                """
        };


        // Agregamos el cuerpo al mensaje.
        message.Body = body.ToMessageBody();


        // Creamos el cliente SMTP de MailKit.
        using var smtp = new SmtpClient();


        // Nos conectamos a Gmail utilizando:
        // Host: smtp.gmail.com
        // Puerto: 587
        // Seguridad: STARTTLS
        await smtp.ConnectAsync(
            _smtpHost,
            _smtpPort,
            SecureSocketOptions.StartTls);


        // Autenticamos la cuenta de Gmail.
        await smtp.AuthenticateAsync(
            _smtpUser,
            _smtpPassword);


        // Enviamos el correo.
        await smtp.SendAsync(message);


        // Cerramos correctamente la conexión SMTP.
        await smtp.DisconnectAsync(true);
    }
}