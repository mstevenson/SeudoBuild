
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using JetBrains.Annotations;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using SeudoCI.Core;

namespace SeudoCI.Pipeline.Modules.EmailNotify;

public class EmailNotifyStep : INotifyStep<EmailNotifyConfig>
{
    private const string DefaultSubjectTemplate = "Build {{PipelineStatus}} • %project_name% • %build_target_name%";

    private const string DefaultBodyTemplate =
        "Build {{PipelineStatus}} for %project_name% (%build_target_name%) at {{BuildTimestamp}}.\n\n" +
        "Pipeline summary:\n{{SummaryTable}}\n\n" +
        "Failed Stage: {{FailedStage}}\n" +
        "Failure Reason: {{FailureReason}}";

    private EmailNotifyConfig _config = null!;
    private ILogger _logger = null!;
    private IReadOnlyList<MailboxAddress> _toRecipients = Array.Empty<MailboxAddress>();
    private IReadOnlyList<MailboxAddress> _ccRecipients = Array.Empty<MailboxAddress>();
    private IReadOnlyList<MailboxAddress> _bccRecipients = Array.Empty<MailboxAddress>();
    private string _smtpUser = string.Empty;
    private string _smtpPassword = string.Empty;

    public string? Type => "Email Notification";

    [UsedImplicitly]
    public void Initialize(EmailNotifyConfig config, ITargetWorkspace workspace, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(logger);

        _logger = logger;

        NormalizeConfiguration(config);
        ValidateConfiguration(config);

        _config = config;

        _toRecipients = ParseAddresses(MergeAddresses(config.ToAddresses, config.ToAddress), nameof(config.ToAddresses));
        _ccRecipients = ParseAddresses(config.CcAddresses, nameof(config.CcAddresses));
        _bccRecipients = ParseAddresses(config.BccAddresses, nameof(config.BccAddresses));

        _smtpUser = ResolveSecret(config.SMTPUser, config.SMTPUserEnvironmentVariable, nameof(config.SMTPUser));
        _smtpPassword = ResolveSecret(config.SMTPPassword, config.SMTPPasswordEnvironmentVariable, nameof(config.SMTPPassword));
    }

    public NotifyStepResults ExecuteStep(PipelineRunResults pipelineResults, ITargetWorkspace workspace)
    {
        try
        {
            var tokens = BuildTemplateTokens(pipelineResults);
            var subject = RenderTemplate(_config.SubjectTemplate, DefaultSubjectTemplate, tokens, workspace);
            var body = RenderTemplate(_config.BodyTemplate, DefaultBodyTemplate, tokens, workspace);

            var message = BuildMessage(subject, body);

            if (TrySendWithRetries(message, out var sendException))
            {
                return new NotifyStepResults { IsSuccess = true };
            }

            if (_config.FailPipelineOnError)
            {
                return new NotifyStepResults { IsSuccess = false, Exception = sendException };
            }

            _logger.Write(
                $"Email notification delivery failed but FailPipelineOnError is disabled: {sendException?.Message}",
                LogType.Alert);

            return new NotifyStepResults { IsSuccess = true, Exception = sendException };
        }
        catch (Exception e)
        {
            if (_config.FailPipelineOnError)
            {
                return new NotifyStepResults { IsSuccess = false, Exception = e };
            }

            _logger.Write($"Email notification failed but pipeline configured to continue: {e.Message}", LogType.Alert);
            return new NotifyStepResults { IsSuccess = true, Exception = e };
        }
    }

    private static void NormalizeConfiguration(EmailNotifyConfig config)
    {
        config.ToAddresses ??= new List<string>();
        config.CcAddresses ??= new List<string>();
        config.BccAddresses ??= new List<string>();

        if (!string.IsNullOrWhiteSpace(config.ToAddress) &&
            !config.ToAddresses.Any(address => string.Equals(address, config.ToAddress, StringComparison.OrdinalIgnoreCase)))
        {
            config.ToAddresses.Add(config.ToAddress);
        }

        config.ToAddress = string.Empty;
    }

    private static string RenderTemplate(string? userTemplate, string defaultTemplate,
        IReadOnlyDictionary<string, string> tokens, ITargetWorkspace workspace)
    {
        var template = string.IsNullOrWhiteSpace(userTemplate) ? defaultTemplate : userTemplate;
        foreach (var token in tokens)
        {
            template = template.Replace($"{{{{{token.Key}}}}}", token.Value, StringComparison.OrdinalIgnoreCase);
        }

        return workspace.Macros.ReplaceVariablesInText(template);
    }

    private MimeMessage BuildMessage(string subject, string body)
    {
        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(_config.FromAddress));
        foreach (var address in _toRecipients)
        {
            message.To.Add(address);
        }
        foreach (var address in _ccRecipients)
        {
            message.Cc.Add(address);
        }
        foreach (var address in _bccRecipients)
        {
            message.Bcc.Add(address);
        }

        message.Subject = subject;
        message.Body = new TextPart("plain")
        {
            Text = body
        };

        return message;
    }

    private bool TrySendWithRetries(MimeMessage message, out Exception? sendException)
    {
        sendException = null;
        var attempts = Math.Max(1, _config.MaxRetryAttempts);
        var backoff = TimeSpan.FromSeconds(Math.Max(0, _config.RetryBackoffSeconds));

        for (var attempt = 1; attempt <= attempts; attempt++)
        {
            try
            {
                SendMessage(message);
                return true;
            }
            catch (Exception ex)
            {
                sendException = ex;
                _logger.Write($"Attempt {attempt} to send email notification failed: {ex.Message}", LogType.Alert);

                if (attempt < attempts && backoff > TimeSpan.Zero)
                {
                    _logger.Write($"Retrying email delivery in {backoff.TotalSeconds} seconds...", LogType.SmallBullet);
                    Thread.Sleep(backoff);
                }
            }
        }

        return false;
    }

    private void SendMessage(MimeMessage message)
    {
        _logger.Write($"Sending email notification to {string.Join(", ", _toRecipients.Select(r => r.Address))}", LogType.SmallBullet);

        using var client = new SmtpClient();
        client.Timeout = 10000;
        client.Connect(_config.Host, _config.Port, GetSecureSocketOptions(_config.Port));
        client.AuthenticationMechanisms.Remove("XOAUTH2");

        if (!string.IsNullOrWhiteSpace(_smtpUser))
        {
            client.Authenticate(_smtpUser, _smtpPassword);
        }

        client.Send(message);
        client.Disconnect(true);

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

        var toRecipients = MergeAddresses(config.ToAddresses ?? new List<string>(), string.Empty).ToList();
        if (toRecipients.Count == 0)
        {
            throw new ArgumentException("At least one recipient must be specified", nameof(config.ToAddresses));
        }
        foreach (var address in toRecipients)
        {
            if (!MailboxAddress.TryParse(address, out _))
            {
                throw new ArgumentException($"Recipient '{address}' is not a valid email address", nameof(config.ToAddresses));
            }
        }

        ValidateAddresses(config.CcAddresses ?? new List<string>(), nameof(config.CcAddresses));
        ValidateAddresses(config.BccAddresses ?? new List<string>(), nameof(config.BccAddresses));

        if (string.IsNullOrWhiteSpace(config.Host))
        {
            throw new ArgumentException("Host cannot be empty", nameof(config.Host));
        }
        if (config.Port is < 1 or > 65535)
        {
            throw new ArgumentOutOfRangeException(nameof(config.Port), "Port must be between 1 and 65535");
        }
        if (config.MaxRetryAttempts < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(config.MaxRetryAttempts), "MaxRetryAttempts must be at least 1");
        }
        if (config.RetryBackoffSeconds < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(config.RetryBackoffSeconds), "RetryBackoffSeconds cannot be negative");
        }
        if (string.IsNullOrWhiteSpace(config.SMTPUser) && string.IsNullOrWhiteSpace(config.SMTPUserEnvironmentVariable))
        {
            throw new ArgumentException("SMTPUser or SMTPUserEnvironmentVariable must be provided", nameof(config.SMTPUser));
        }
        if (string.IsNullOrWhiteSpace(config.SMTPPassword) && string.IsNullOrWhiteSpace(config.SMTPPasswordEnvironmentVariable))
        {
            throw new ArgumentException("SMTPPassword or SMTPPasswordEnvironmentVariable must be provided", nameof(config.SMTPPassword));
        }
    }

    private static void ValidateAddresses(IEnumerable<string> addresses, string propertyName)
    {
        foreach (var address in addresses)
        {
            var trimmed = address?.Trim();
            if (string.IsNullOrEmpty(trimmed) || !MailboxAddress.TryParse(trimmed, out _))
            {
                throw new ArgumentException($"Address '{address}' is not a valid email address", propertyName);
            }
        }
    }

    private static IEnumerable<string> MergeAddresses(IEnumerable<string> addresses, string legacyAddress)
    {
        var unique = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(legacyAddress))
        {
            unique.Add(legacyAddress.Trim());
        }

        foreach (var address in addresses)
        {
            if (!string.IsNullOrWhiteSpace(address))
            {
                unique.Add(address.Trim());
            }
        }

        return unique;
    }

    private static IReadOnlyList<MailboxAddress> ParseAddresses(IEnumerable<string> addresses, string propertyName)
    {
        var parsed = new List<MailboxAddress>();
        foreach (var address in addresses)
        {
            var trimmed = address?.Trim();
            if (!string.IsNullOrEmpty(trimmed) && MailboxAddress.TryParse(trimmed, out var mailbox))
            {
                parsed.Add(mailbox);
            }
            else
            {
                throw new ArgumentException($"Address '{address}' is not a valid email address", propertyName);
            }
        }

        return parsed;
    }

    private static string ResolveSecret(string explicitValue, string environmentVariable, string propertyName)
    {
        if (!string.IsNullOrWhiteSpace(environmentVariable))
        {
            var environmentValue = Environment.GetEnvironmentVariable(environmentVariable);
            if (string.IsNullOrEmpty(environmentValue))
            {
                throw new InvalidOperationException($"Environment variable '{environmentVariable}' specified for {propertyName} is not set.");
            }

            return environmentValue;
        }

        return explicitValue;
    }

    private static IReadOnlyDictionary<string, string> BuildTemplateTokens(PipelineRunResults pipelineResults)
    {
        var tokens = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["PipelineStatus"] = pipelineResults.IsPipelineSuccessful ? "Succeeded" : "Failed",
            ["BuildTimestamp"] = DateTime.UtcNow.ToString("u"),
            ["SummaryTable"] = BuildSummary(pipelineResults),
            ["TotalDuration"] = FormatDuration(pipelineResults.SourceResults.Duration +
                                                pipelineResults.BuildResults.Duration +
                                                pipelineResults.ArchiveResults.Duration +
                                                pipelineResults.DistributeResults.Duration)
        };

        var failure = FindFailure(pipelineResults);
        tokens["FailedStage"] = failure.Stage;
        tokens["FailureReason"] = failure.Reason;

        foreach (var (name, results) in pipelineResults.EnumerateStages())
        {
            tokens[$"{name}Status"] = DescribeStatus(results);
            tokens[$"{name}Duration"] = FormatDuration(results.Duration);
            tokens[$"{name}Exception"] = results.Exception?.Message ?? string.Empty;
        }

        return tokens;
    }

    private static string BuildSummary(PipelineRunResults pipelineResults)
    {
        var builder = new StringBuilder();
        foreach (var (name, results) in pipelineResults.EnumerateStages())
        {
            builder.Append(name.PadRight(11));
            builder.Append(": ");
            builder.Append(DescribeStatus(results));
            builder.Append(' ');
            builder.Append('(');
            builder.Append(FormatDuration(results.Duration));
            builder.Append(')');
            builder.AppendLine();

            if (!results.IsSuccess && results.Exception != null)
            {
                builder.AppendLine($"  Error: {results.Exception.Message}");
            }
        }

        return builder.ToString().TrimEnd();
    }

    private static string DescribeStatus(PipelineSequenceResults results)
    {
        if (results.IsSkipped)
        {
            return "Skipped";
        }

        return results.IsSuccess ? "Succeeded" : "Failed";
    }

    private static (string Stage, string Reason) FindFailure(PipelineRunResults pipelineResults)
    {
        foreach (var (name, results) in pipelineResults.EnumerateStages())
        {
            if (!results.IsSuccess)
            {
                return (name, results.Exception?.Message ?? "Unknown failure");
            }
        }

        return ("None", "N/A");
    }

    private static string FormatDuration(TimeSpan duration)
    {
        if (duration == TimeSpan.Zero)
        {
            return "00:00:00";
        }

        return duration.ToString(@"hh\:mm\:ss");
    }
}
