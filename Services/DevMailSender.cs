using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Logging;

namespace PracticeLogger.Services
{
    public class DevMailSender : IEmailSender
    {
        private readonly ILogger<NoOpEmailSender> _logger;
        public DevMailSender(ILogger<NoOpEmailSender> logger) => _logger = logger;

        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            // Dev: logga istället för att skicka
            _logger.LogInformation("Pretend-sent mail to {Email}. Subject: {Subject}", email, subject);
            return Task.CompletedTask;
        }
    }
}
