namespace SeudoCI.Pipeline.Modules.EmailNotify;

/// <inheritdoc />
/// <summary>
/// Configuration values for a notify pipeline step that sends an email.
/// </summary>
public class EmailNotifyConfig : NotifyStepConfig
{
    public override string Name { get; } = "Email Notification";
    public string FromAddress { get; set; } = string.Empty;
    public string ToAddress { get; set; } = string.Empty;
    public string Host { get; set; } = "smtp.gmail.com";
    public int Port { get; set; } = 587;
    public string SMTPUser { get; set; } = string.Empty;
    public string SMTPPassword { get; set; } = string.Empty;
}