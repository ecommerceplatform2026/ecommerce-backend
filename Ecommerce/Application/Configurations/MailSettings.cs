namespace Application.Configurations
{
    public sealed class MailSettings
    {
        public string SmtpServer { get; set; } = string.Empty;
        public string Port { get; set; } = "587";
        public string From { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FromName { get; set; } = "Ecommerce Platform";
        public bool EnableSsl { get; set; } = true;
    }
}
