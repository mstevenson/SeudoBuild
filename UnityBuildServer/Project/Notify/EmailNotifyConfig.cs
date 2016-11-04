namespace UnityBuildServer
{
    public class EmailNotifyConfig : NotifyConfig
    {
        public string SMTPUser { get; set; }
        public string SMTPPassword { get; set; }
        public string FromAddress { get; set; }
        public string ToAddress { get; set; }
        public int Port { get; set; } = 587;
        public bool UseSSL { get; set; } = true;
        public string Host { get; set; } = "smtp.google.com";
    }
}
