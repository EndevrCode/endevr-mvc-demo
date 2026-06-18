using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace Nestled.Services
{
    public class NullEmailSender : IEmailSender
    {
        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            // no-op
            return Task.CompletedTask;
        }
    }
}
