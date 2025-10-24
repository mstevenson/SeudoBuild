using JetBrains.Annotations;

namespace SeudoCI.Pipeline.Modules.EmailNotify;

using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Core;

public class EmailNotifyStep : INotifyStep<EmailNotifyConfig>
{
    private EmailNotifyConfig _config = null!;
    private ILogger _logger = null!;

    public string? Type => "Email Notification";

    [UsedImplicitly]
    public void Initialize(EmailNotifyConfig config, ITargetWorkspace workspace, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(logger);

        ValidateConfiguration(config);

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

    private void SendMessage(string fromAddress, string toAddress, string subject, string body)
    {
        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(fromAddress));
        message.To.Add(MailboxAddress.Parse(toAddress));
        message.Subject = subject;
        message.Body = new TextPart("plain")
        {
            Text = body
        };

        _logger.Write($"Sending email notification to {toAddress}", LogType.SmallBullet);

        using (var client = new SmtpClient())
        {
            client.Timeout = 10000;
            client.Connect(_config.Host, _config.Port, GetSecureSocketOptions(_config.Port));
            client.AuthenticationMechanisms.Remove("XOAUTH2");
            client.Authenticate(_config.SMTPUser, _config.SMTPPassword);
            client.Send(message);
            client.Disconnect(true);
        }

        _logger.Write("Email notification sent", LogType.SmallBullet);
    }

    private static SecureSocketOptions GetSecureSocketOptions(int port)
    {
        return port == 465 ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTlsWhenAvailable;
    }

    private static void ValidateConfiguration(EmailNotifyConfig config)
    {
        if (string.IsNullOrWhiteSpace(config.FromAddress))
        {
            throw new ArgumentException("FromAddress cannot be empty", nameof(config.FromAddress));
        }

        if (!MailboxAddress.TryParse(config.FromAddress, out _))
        {
            throw new ArgumentException("FromAddress is not a valid email address", nameof(config.FromAddress));
        }

        if (string.IsNullOrWhiteSpace(config.ToAddress))
        {
            throw new ArgumentException("ToAddress cannot be empty", nameof(config.ToAddress));
        }

        if (!MailboxAddress.TryParse(config.ToAddress, out _))
        {
            throw new ArgumentException("ToAddress is not a valid email address", nameof(config.ToAddress));
        }

        if (string.IsNullOrWhiteSpace(config.Host))
        {
            throw new ArgumentException("Host cannot be empty", nameof(config.Host));
        }

        if (config.Port is < 1 or > 65535)
        {
            throw new ArgumentOutOfRangeException(nameof(config.Port), "Port must be between 1 and 65535");
        }

        if (string.IsNullOrWhiteSpace(config.SMTPUser))
        {
            throw new ArgumentException("SMTPUser cannot be empty", nameof(config.SMTPUser));
        }

        if (string.IsNullOrWhiteSpace(config.SMTPPassword))
        {
            throw new ArgumentException("SMTPPassword cannot be empty", nameof(config.SMTPPassword));
        }
    }
}
