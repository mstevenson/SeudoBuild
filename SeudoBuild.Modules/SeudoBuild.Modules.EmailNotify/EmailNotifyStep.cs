using System;
using System.Net;
using System.Net.Mail;

namespace SeudoBuild.Modules.EmailNotify
{
    public class EmailNotifyStep : INotifyStep<EmailNotifyConfig>
    {
        EmailNotifyConfig config;

        public void Initialize(EmailNotifyConfig config, Workspace workspace)
        {
            this.config = config;
        }

        public string Type { get; } = "Email Notification";

        public NotifyStepResults ExecuteStep(DistributeSequenceResults distributeResults, Workspace workspace)
        {
            try
            {
                SendMessage(config.FromAddress, config.ToAddress, "Build Completed", "finished a build");
                return new NotifyStepResults { IsSuccess = true };
            }
            catch (Exception e)
            {
                return new NotifyStepResults { IsSuccess = false, Exception = e };
            }
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

            BuildConsole.WriteLine("Email notification sent");
        }
    }
}
