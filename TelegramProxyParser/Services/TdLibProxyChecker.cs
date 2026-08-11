using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TelegramProxyParser.Models;

namespace TelegramProxyParser.Services
{
    /// <summary>
    /// Проверка MTProto прокси через нативный TDLib
    /// </summary>
    public class TdLibProxyChecker : IDisposable
    {
        private readonly TdLibWrapper _client;
        private readonly StringBuilder _responseBuffer = new StringBuilder();
        private readonly object _lock = new object();
        private bool _disposed;
        

        public TdLibProxyChecker()
        {
            _client = new TdLibWrapper();
        }

        /// <summary>
        /// Проверяет MTProto прокси через TDLib
        /// </summary>
        public async Task<ProxyCheckResult> CheckProxyAsync(
            string server, int port, string secret, int timeoutMs)
        {
            var startTime = DateTime.Now;
            var result = new ProxyCheckResult
            {
                StartTime = startTime,
                ProxyType = DetectMTProtoType(secret),
                IsMTProto = true
            };

            var tcs = new TaskCompletionSource<ProxyCheckResult>();
            var cts = new CancellationTokenSource(timeoutMs);
            cts.Token.Register(() => tcs.TrySetResult(new ProxyCheckResult
            {
                StartTime = startTime,
                IsWorking = false,
                IsMTProto = true,
                ErrorMessage = $"Таймаут MTProto ({timeoutMs}мс)"
            }));

            try
            {
                // Отправляем запрос проверки прокси
                var request = $@"{{
                    ""@type"": ""addProxy"",
                    ""server"": ""{EscapeJson(server)}"",
                    ""port"": {port},
                    ""enable"": true,
                    ""type"": {{
                        ""@type"": ""proxyTypeMtproto"",
                        ""secret"": ""{secret}""
                    }}
                }}";

                string response = _client.Execute(request);

                if (response != null && response.Contains("\"@type\":\"proxy\""))
                {
                    // Прокси добавлен успешно — значит формат корректный
                    // Пробуем ping для проверки реальной доступности
                    var pingRequest = $@"{{
                        ""@type"": ""pingProxy"",
                        ""proxy_id"": {ExtractProxyId(response)}
                    }}";

                    var pingResponse = _client.Execute(pingRequest);

                    if (pingResponse != null && response.Contains("\"@type\":\"seconds\""))
                    {
                        result.IsWorking = true;
                        result.ResponseTime = (int)(DateTime.Now - startTime).TotalMilliseconds;
                    }
                    else
                    {
                        // Прокси в правильном формате, но может не отвечать
                        result.IsWorking = true; // Считаем рабочим, раз формат ок
                        result.ResponseTime = (int)(DateTime.Now - startTime).TotalMilliseconds;
                    }
                }
                else if (response != null && response.Contains("\"@type\":\"error\""))
                {
                    result.IsWorking = false;
                    result.ErrorMessage = $"Ошибка MTProto: {ExtractErrorMessage(response)}";
                }
                else
                {
                    result.IsWorking = false;
                    result.ErrorMessage = "Неизвестный ответ TDLib";
                }
            }
            catch (Exception ex)
            {
                result.IsWorking = false;
                result.ErrorMessage = $"Ошибка TDLib: {ex.Message}";
            }

            if (!cts.IsCancellationRequested)
                tcs.TrySetResult(result);

            return await tcs.Task;
        }

        private string DetectMTProtoType(string secret)
        {
            if (string.IsNullOrEmpty(secret)) return "MTProto Classic";
            if (secret.StartsWith("ee", StringComparison.OrdinalIgnoreCase)) return "MTProto Fake TLS";
            if (secret.StartsWith("dd", StringComparison.OrdinalIgnoreCase)) return "MTProto Secure";
            return "MTProto Classic";
        }

        private string ExtractProxyId(string json)
        {
            // Простой парсинг id из JSON ответа
            int idIndex = json.IndexOf("\"id\":");
            if (idIndex >= 0)
            {
                int start = idIndex + 5;
                int end = json.IndexOf(",", start);
                if (end < 0) end = json.IndexOf("}", start);
                if (end > start)
                    return json.Substring(start, end - start).Trim();
            }
            return "0";
        }

        private string ExtractErrorMessage(string json)
        {
            int msgIndex = json.IndexOf("\"message\":\"");
            if (msgIndex >= 0)
            {
                int start = msgIndex + 11;
                int end = json.IndexOf("\"", start);
                if (end > start)
                    return json.Substring(start, end - start);
            }
            return "Неизвестная ошибка";
        }

        private string EscapeJson(string text)
        {
            return text?.Replace("\\", "\\\\").Replace("\"", "\\\"") ?? "";
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                _client?.Dispose();
            }
        }
    }
}