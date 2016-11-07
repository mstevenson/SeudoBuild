using System.Net;
using System.Net.Mail;

namespace UnityBuild
{
    public class EmailNotifyStep : NotifyStep
    {
        EmailNotifyConfig config;

        public EmailNotifyStep(EmailNotifyConfig config)
        {
            this.config = config;
        }

        public override string Type { get; } = "Email Notification";

        public override void Notify()
        {
            SendMessage(config.FromAddress, config.ToAddress, "Build Completed", "finished a build");
        }

        void SendMessage(string from, string to, string subject, string body)
        {
            SmtpClient client = new SmtpClient
            {
                Host = config.Host,
                Port = config.Port,
                EnableSsl = config.UseSSL,
                Timeout = 10000,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(config.SMTPUser, config.SMTPPassword)
            };

            BuildConsole.WriteLine($"Sending email notification to {to}");

            MailMessage message = new MailMessage(from, to);
            message.Subject = subject;
            message.Body = body;
            client.Send(message);

            BuildConsole.WriteLine("Email notification send");
        }
    }
}
