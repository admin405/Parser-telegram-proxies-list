using System;

namespace TelegramProxyParser.Models
{
    public class ProxyCheckResult
    {
        public bool IsWorking { get; set; }
        public string ProxyType { get; set; }
        public string ErrorMessage { get; set; }
        public int ResponseTime { get; set; }
        public DateTime StartTime { get; set; }

        public ProxyCheckResult()
        {
            IsWorking = false;
            ProxyType = "Unknown";
            ResponseTime = -1;
            StartTime = DateTime.Now;
        }
    }
}