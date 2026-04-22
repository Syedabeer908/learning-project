using MailKit.Net.Smtp;
using MimeKit;
using WebApplication1.Common.Email;
using WebApplication1.Settings;

namespace WebApplication1.Services
{
    public class EmailService
    {
        private readonly EmailSettings _settings;
        private readonly ILogger<EmailService> _logger;

        public EmailService(EmailSettings settings, ILogger<EmailService> logger)
        {
            _settings = settings;
            _logger = logger;
        }

        private async Task Send(MimeMessage email)
        {
            using var smtp = new SmtpClient();

            try
            {
                await smtp.ConnectAsync(
                    "sandbox.smtp.mailtrap.io",
                    2525,
                    MailKit.Security.SecureSocketOptions.StartTls);

                await smtp.AuthenticateAsync("b73321d8c15324", "11c92fe8c44d19");

                await smtp.SendAsync(email);

                _logger.LogInformation("Email sent successfully to {To}", email.To);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Email sending failed");
                throw;
            }
            finally
            {
                await smtp.DisconnectAsync(true);
            }
        }

        public async Task SendEmailAsync(EmailRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.ToEmail))
                return;

            var email = new MimeMessage();

            email.From.Add(new MailboxAddress("Transviti", "b73321d8c15324"));
            email.To.Add(new MailboxAddress("", request.ToEmail));
            email.Subject = request.Subject;

            email.Body = new TextPart("html")
            {
                Text = request.Body
            };

            await Send(email);
        }
    }
}
