using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Service_TechCompass.Interfaces;
using System.Threading.Tasks;

namespace Service_TechCompass.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            var emailConfig = _config.GetSection("EmailConfiguration");

            var smtpServer = emailConfig["SmtpServer"];

            var port = int.Parse(emailConfig["SmtpPort"]!);

            var senderEmail = emailConfig["SenderEmail"];

            var senderPassword = emailConfig["Password"];

            var senderName = emailConfig["SenderName"];

            var mailMessage = new MailMessage
            {
                From = new MailAddress(senderEmail!, senderName),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };
            mailMessage.To.Add(toEmail);

            using var smtpClient = new SmtpClient(smtpServer, port)
            {
                Credentials = new NetworkCredential(senderEmail, senderPassword),
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false
            };

            await smtpClient.SendMailAsync(mailMessage);
        }
    }
}