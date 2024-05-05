using System;
using MailKit.Net.Smtp;
using MimeKit;
using SeudoCI.Core;

namespace SeudoCI.Pipeline.Modules.EmailNotify
{
    public class EmailNotifyStep : INotifyStep<EmailNotifyConfig>
    {
        private EmailNotifyConfig _config;
        private ILogger _logger;

        public string Type { get; } = "Email Notification";

        public void Initialize(EmailNotifyConfig config, ITargetWorkspace workspace, ILogger logger)
        {
            _config = config;
            _logger = logger;
        }

        public NotifyStepResults ExecuteStep(DistributeSequenceResults distributeResults, ITargetWorkspace workspace)
        {
            try
            {
                string subject = "Build Completed • %project_name% • %build_target_name%";
                subject = workspace.Macros.ReplaceVariablesInText(subject);
                SendMessage(_config.FromAddress, _config.ToAddress, subject, $"Build completed in {distributeResults.Duration} seconds.");
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
            message.From.Add(new MailboxAddress("SeudoCI", fromAddress));
            message.To.Add(new MailboxAddress("recipient", toAddress));
            message.Subject = subject;
            message.Body = new TextPart("plain")
            {
                Text = body
            };

            _logger.Write($"Sending email notification to {toAddress}", LogType.SmallBullet);

            using (var client = new SmtpClient())
            {
                client.Timeout = 10000;
                client.ServerCertificateValidationCallback = (s, c, h, e) => true;
                client.Connect(_config.Host, _config.Port, false);
                // Note: since we don't have an OAuth2 token, disable
                // the XOAUTH2 authentication mechanism.
                client.AuthenticationMechanisms.Remove("XOAUTH2");
                client.Authenticate(_config.SMTPUser, _config.SMTPPassword);
                client.Send(message);
                client.Disconnect(true);
            }

            _logger.Write("Email notification sent", LogType.SmallBullet);
        }
    }
}
