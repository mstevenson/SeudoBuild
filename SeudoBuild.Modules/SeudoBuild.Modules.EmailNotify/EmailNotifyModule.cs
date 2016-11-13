using System;
using Newtonsoft.Json;
using SeudoBuild.Data;

namespace SeudoBuild.Modules.EmailNotify
{
    public class EmailNotifyModule : INotifyModule
    {
        public Type StepType { get; } = typeof(EmailNotifyStep);

        public JsonConverter ConfigConverter { get; } = new EmailNotifyConfigConverter();

        public bool MatchesConfigType(StepConfig config)
        {
            return config is EmailNotifyConfig;
        }
    }
}
