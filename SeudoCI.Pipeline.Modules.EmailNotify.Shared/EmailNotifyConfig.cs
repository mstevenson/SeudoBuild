using System.Collections.Generic;

namespace SeudoCI.Pipeline.Modules.EmailNotify;

/// <inheritdoc />
/// <summary>
/// Configuration values for a notify pipeline step that sends an email.
/// </summary>
public class EmailNotifyConfig : NotifyStepConfig
{
    public EmailNotifyConfig()
    {
        RunOnFailure = true;
    }

    public override string Name { get; } = "Email Notification";

    public string FromAddress { get; set; } = string.Empty;

    /// <summary>
    /// Legacy single recipient property retained for backwards compatibility.
    /// When specified the address will be merged into <see cref="ToAddresses"/>.
    /// </summary>
    public string ToAddress { get; set; } = string.Empty;

    public List<string> ToAddresses { get; set; } = new();

    public List<string> CcAddresses { get; set; } = new();

    public List<string> BccAddresses { get; set; } = new();

    public string Host { get; set; } = "smtp.gmail.com";

    public int Port { get; set; } = 587;

    public string SMTPUser { get; set; } = string.Empty;

    public string SMTPUserEnvironmentVariable { get; set; } = string.Empty;

    public string SMTPPassword { get; set; } = string.Empty;

    public string SMTPPasswordEnvironmentVariable { get; set; } = string.Empty;

    public int MaxRetryAttempts { get; set; } = 3;

    public int RetryBackoffSeconds { get; set; } = 5;

    public bool FailPipelineOnError { get; set; } = true;

    public string SubjectTemplate { get; set; } = string.Empty;

    public string BodyTemplate { get; set; } = string.Empty;
}