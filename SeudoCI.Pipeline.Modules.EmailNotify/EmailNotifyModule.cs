namespace SeudoCI.Pipeline.Modules.EmailNotify;

public class EmailNotifyModule : INotifyModule
{
    public string Name => "Email";

    public Type StepType { get; } = typeof(EmailNotifyStep);

    public Type StepConfigType { get; } = typeof(EmailNotifyConfig);

    public string StepConfigName => "Email Notification";
}