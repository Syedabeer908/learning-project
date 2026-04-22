using WebApplication1.Entities;

namespace WebApplication1.Common.Email.EmailTemplates
{
    public class RegisterEmailTemplate
    {
        public EmailRequest Template (User user)
        {
            return new EmailRequest
            {
                ToEmail = user.Email,
                ToName = user.Username,
                Subject = "Welcome",
                Body = $"<h2>Welcome {user.Username}</h2><p>Your account is ready.</p>"
            };
        }
    }
}
