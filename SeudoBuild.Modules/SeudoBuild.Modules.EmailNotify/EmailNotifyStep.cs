using System;
using MailKit.Net.Smtp;
using MimeKit;

namespace SeudoBuild.Pipeline.Modules.EmailNotify
{
    public class EmailNotifyStep : INotifyStep<EmailNotifyConfig>
    {
        EmailNotifyConfig config;

        public string Type { get; } = "Email Notification";

        public void Initialize(EmailNotifyConfig config, IWorkspace workspace)
        {
            this.config = config;
        }

        public NotifyStepResults ExecuteStep(DistributeSequenceResults distributeResults, IWorkspace workspace)
        {
            try
            {
                string subject = "Build Completed • %project_name% • %build_target_name%";
                subject = workspace.Macros.ReplaceVariablesInText(subject);
                SendMessage(config.FromAddress, config.ToAddress, subject, $"Build completed in {distributeResults.Duration} seconds.");
                return new NotifyStepResults { IsSuccess = true };
            }
            catch (Exception e)
            {
                return new NotifyStepResults { IsSuccess = false, Exception = e };
            }
        }

        void SendMessage(string fromAddress, string toAddress, string subject, string body)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("SeudoBuild", fromAddress));
            message.To.Add(new MailboxAddress(toAddress));
            message.Subject = subject;
            message.Body = new TextPart("plain")
            {
                Text = body
            };

            BuildConsole.WriteLine($"Sending email notification to {toAddress}");

            using (var client = new SmtpClient())
            {
                client.ServerCertificateValidationCallback = (s, c, h, e) => true;
                client.Connect(config.Host, config.Port, false);
                // Note: since we don't have an OAuth2 token, disable
                // the XOAUTH2 authentication mechanism.
                client.AuthenticationMechanisms.Remove("XOAUTH2");
                client.Authenticate(config.SMTPUser, config.SMTPPassword);
                client.Send(message);
                client.Disconnect(true);
                client.Timeout = 10000;
            }

            BuildConsole.WriteLine("Email notification sent");
        }
    }
}
