using System.Net;
using System.Net.Mail;

namespace duAnLUAGA.Helpers
{
    public class EmailHelper
    {
        public static async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            var smtp = new SmtpClient("smtp.gmail.com", 587)
            {
                Credentials = new NetworkCredential("latuthien20@gmail.com", "lybb ioil mmim qijw"),
                EnableSsl = true
            };

            var message = new MailMessage("latuthien20@gmail.com", toEmail, subject, body);
            await smtp.SendMailAsync(message);
        }
    }
}
