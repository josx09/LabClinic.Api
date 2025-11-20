using System.Net;
using System.Net.Mail;
using System.Text;
using Microsoft.Extensions.Configuration;


namespace LabClinic.Api.Models.Dtos
{
    public class EmailReporteDto
    {
        public string correo { get; set; } = "";
        public string asunto { get; set; } = "";
        public string titulo { get; set; } = "";
        public List<EmailItemDto> items { get; set; } = new();
    }

    public class EmailItemDto
    {
        public string fecha { get; set; } = "";
        public string insumo { get; set; } = "";
        public int cantidad { get; set; }
        public string tipoExamen { get; set; } = "";
        public string justificacion { get; set; } = "";
    }
}

namespace LabClinic.Api.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;
        private readonly SmtpClient _smtp;
        private readonly string _from;

        public EmailService(IConfiguration config)
        {
            _config = config;
            var emailSection = _config.GetSection("Email");

            string host = emailSection["Host"] ?? "smtp.gmail.com";
            int port = int.TryParse(emailSection["Port"], out int p) ? p : 587;
            string user = emailSection["User"] ?? throw new Exception("Falta 'Email:User' en configuración.");
            string pass = emailSection["Pass"] ?? throw new Exception("Falta 'Email:Pass' en configuración.");
            bool useSsl = bool.TryParse(emailSection["UseSsl"], out bool ssl) ? ssl : true;

            _from = emailSection["From"] ?? user;

            _smtp = new SmtpClient(host)
            {
                Port = port,
                Credentials = new NetworkCredential(user, pass),
                EnableSsl = useSsl
            };
        }

        public async Task SendAsync(
            string toEmail,
            string subject,
            string plainTextBody,
            string? htmlBody = null,
            CancellationToken ct = default)
        {
            var msg = new MailMessage
            {
                From = new MailAddress(_from, "Laboratorio Clínico CDC Poptún", Encoding.UTF8),
                Subject = subject,
                Body = htmlBody ?? plainTextBody,
                IsBodyHtml = htmlBody != null,
                BodyEncoding = Encoding.UTF8,
                SubjectEncoding = Encoding.UTF8
            };

            foreach (var addr in toEmail.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
                msg.To.Add(addr.Trim());

            await _smtp.SendMailAsync(msg, ct);
        }
    }
}
