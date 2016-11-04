using System.Net;
using System.Net.Mail;

namespace UnityBuildServer
{
    public class EmailNotifyStep : NotifyStep
    {
        EmailNotifyConfig config;

        public EmailNotifyStep(EmailNotifyConfig config)
        {
            this.config = config;
        }

        public override string TypeName
        {
            get
            {
                return "Email Notification";
            }
        }

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

            MailMessage message = new MailMessage(from, to);
            message.Subject = subject;
            message.Body = body;
            client.Send(message);
        }
    }
}
