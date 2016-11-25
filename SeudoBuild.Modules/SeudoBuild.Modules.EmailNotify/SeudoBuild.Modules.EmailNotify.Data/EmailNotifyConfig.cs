namespace SeudoBuild.Modules.EmailNotify
{
    public class EmailNotifyConfig : NotifyStepConfig
    {
        public override string Type { get; } = "Email Notification";
        public string FromAddress { get; set; }
        public string ToAddress { get; set; }
        public string Host { get; set; } = "smtp.google.com";
        public int Port { get; set; } = 587;
        public string SMTPUser { get; set; }
        public string SMTPPassword { get; set; }
    }
}
