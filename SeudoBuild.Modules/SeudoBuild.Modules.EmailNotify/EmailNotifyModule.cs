using System;
using Newtonsoft.Json;

namespace SeudoBuild.Modules.EmailNotify
{
    public class EmailNotifyModule : INotifyModule
    {
        public string Name { get; } = "Email";

        public Type StepType { get; } = typeof(EmailNotifyStep);

        public JsonConverter ConfigConverter { get; } = new EmailNotifyConfigConverter();

        public bool MatchesConfigType(StepConfig config)
        {
            return config is EmailNotifyConfig;
        }
    }
}
