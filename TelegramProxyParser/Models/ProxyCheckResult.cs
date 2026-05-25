using System;

namespace TelegramProxyParser.Models
{
    public class ProxyCheckResult
    {
        public DateTime StartTime { get; set; }
        public bool IsWorking { get; set; }
        public string ProxyType { get; set; }
        public int ResponseTime { get; set; } // в миллисекундах
        public string ErrorMessage { get; set; }

        // Добавляем новое свойство
        public bool IsMTProto { get; set; } = false; // Поддерживает ли прокси MTProto протокол

        // Дополнительные полезные свойства
        public bool IsTimeout { get; set; }
        public int RetryCount { get; set; }
        public DateTime EndTime => StartTime.AddMilliseconds(ResponseTime);
    }
}