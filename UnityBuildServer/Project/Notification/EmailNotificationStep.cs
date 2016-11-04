using System.Net.Mail;

namespace UnityBuildServer
{
    public class EmailNotificationStep : NotificationStep
    {
        EmailNotificationConfig config;

        public EmailNotificationStep(EmailNotificationConfig config)
        {
            this.config = config;
        }

        public void SendMessage(string subject, string body)
        {
            SmtpClient client = new SmtpClient
            {
                Port = config.Port,
                Host = config.Host,
                EnableSsl = config.UseSSL,
                Timeout = 10000,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new System.Net.NetworkCredential(config.SMTPUser, config.SMTPPassword)
            };

            MailMessage message = new MailMessage(config.FromAddress, config.ToAddress);
            message.Subject = subject;
            message.Body = body;
            client.Send(message);
        }
    }
}
