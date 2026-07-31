using System;

namespace TelegramProxyParser.Models
{
    public class ProxyInfo
    {
        public string OriginalUrl { get; set; }
        public string Server { get; set; }
        public int Port { get; set; }
        public string Secret { get; set; }
        public long Ping { get; set; }
        public bool IsWorking { get; set; }
        public string ProxyType { get; set; }
        public string ErrorMessage { get; set; }
        public DateTime CheckTime { get; set; }
        public bool IsFromMtProtoCheck { get; set; } = false;

        public ProxyInfo()
        {
            Port = 443;
            IsWorking = false;
            Ping = -1;
            ProxyType = "Unknown";
            CheckTime = DateTime.Now;
        }
    }
}