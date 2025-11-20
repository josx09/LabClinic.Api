using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;

namespace LabClinic.Api.Services
{
    public class SmtpEmailService : IEmailService
    {
        private readonly IConfiguration _config;

        public SmtpEmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendAsync(
            string toEmail,
            string subject,
            string plainTextBody,
            string? htmlBody = null,
            CancellationToken ct = default)
        {
            var host = _config["Email:Host"];
            var port = int.Parse(_config["Email:Port"] ?? "587");
            var user = _config["Email:User"];
            var pass = _config["Email:Pass"];
            var from = _config["Email:From"] ?? user;
            var useSsl = bool.Parse(_config["Email:UseSsl"] ?? "true");

            // 🧠 Gmail NO permite From diferente del usuario, salvo verificación.
            // Extraemos nombre si está en formato "Nombre <correo>"
            string displayName = user;
            string fromAddress = user;
            if (from.Contains("<") && from.Contains(">"))
            {
                int start = from.IndexOf("<") + 1;
                int end = from.IndexOf(">");
                fromAddress = from.Substring(start, end - start).Trim();
                displayName = from.Substring(0, start - 1).Trim();
            }

            using var smtp = new SmtpClient(host, port)
            {
                Credentials = new NetworkCredential(user, pass),
                EnableSsl = useSsl
            };

            var message = new MailMessage
            {
                From = new MailAddress(fromAddress, displayName),
                Subject = subject,
                Body = string.IsNullOrEmpty(htmlBody) ? plainTextBody : htmlBody,
                IsBodyHtml = !string.IsNullOrEmpty(htmlBody)
            };

            message.To.Add(toEmail);

            try
            {
                await smtp.SendMailAsync(message, ct);
            }
            catch (SmtpException ex)
            {
                throw new InvalidOperationException($"❌ Error SMTP: {ex.StatusCode} - {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"❌ Error al enviar correo: {ex.Message}", ex);
            }
        }
    }
}
