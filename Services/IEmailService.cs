namespace LabClinic.Api.Services
{
    public interface IEmailService
    {
        Task SendAsync(
            string toEmail,
            string subject,
            string plainTextBody,
            string? htmlBody = null,
            CancellationToken ct = default);
    }
}
