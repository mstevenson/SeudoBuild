using System;
using Newtonsoft.Json;
using SeudoBuild.Data;

namespace SeudoBuild.Modules.EmailNotify
{
    public class EmailNotifyModule : INotifyModule
    {
        public Type ArchiveStepType { get; } = typeof(EmailNotifyStep);

        public JsonConverter ConfigConverter { get; } = new EmailNotifyConfigConverter();

        public bool MatchesConfigType(NotifyStepConfig config)
        {
            return config is EmailNotifyConfig;
        }
    }
}
