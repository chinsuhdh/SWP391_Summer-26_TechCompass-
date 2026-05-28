using System.Threading.Tasks;

namespace Service_TechCompass.Interfaces
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string body);
    }
}