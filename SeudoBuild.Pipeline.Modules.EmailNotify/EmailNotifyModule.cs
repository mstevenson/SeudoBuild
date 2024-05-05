using System;

namespace SeudoBuild.Pipeline.Modules.EmailNotify
{
    public class EmailNotifyModule : INotifyModule
    {
        public string Name { get; } = "Email";

        public Type StepType { get; } = typeof(EmailNotifyStep);

        public Type StepConfigType { get; } = typeof(EmailNotifyConfig);

        public string StepConfigName { get; } = "Email Notification";
    }
}
