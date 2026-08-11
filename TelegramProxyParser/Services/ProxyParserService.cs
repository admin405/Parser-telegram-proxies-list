using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TelegramProxyParser.Models;

namespace TelegramProxyParser.Services
{
    public class ProxyParserService
    {
        private static readonly HttpClient httpClient = new HttpClient();

        public ProxyParserService()
        {
            httpClient.DefaultRequestHeaders.Add("User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:109.0) Gecko/20100101 Firefox/115.0");
            httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        // Загрузка с фильтрацией (только валидные ссылки)
        public async Task<List<string>> LoadProxiesFromUrlAsync(string url)
        {
            try
            {
                using (var request = new HttpRequestMessage(HttpMethod.Get, url))
                using (var response = await httpClient.SendAsync(request))
                {
                    response.EnsureSuccessStatusCode();
                    var content = await response.Content.ReadAsStringAsync();

                    var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                    var proxies = new List<string>();
                    foreach (var line in lines)
                    {
                        var trimmedLine = line.Trim();
                        if (trimmedLine.StartsWith("tg://proxy", StringComparison.OrdinalIgnoreCase) ||
                            trimmedLine.StartsWith("https://t.me/proxy", StringComparison.OrdinalIgnoreCase))
                        {
                            proxies.Add(trimmedLine);
                        }
                    }

                    var uniqueProxies = new HashSet<string>(proxies);
                    return new List<string>(uniqueProxies);
                }
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Ошибка загрузки прокси: {0}", ex.Message), ex);
            }
        }

        // Загрузка сырых данных (без фильтрации, для HTML-страниц)
        public async Task<List<string>> LoadRawProxiesFromUrlAsync(string url)
        {
            try
            {
                using (var request = new HttpRequestMessage(HttpMethod.Get, url))
                using (var response = await httpClient.SendAsync(request))
                {
                    response.EnsureSuccessStatusCode();
                    var content = await response.Content.ReadAsStringAsync();

                    var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                    var proxies = new List<string>();
                    foreach (var line in lines)
                    {
                        var trimmedLine = line.Trim();
                        if (!string.IsNullOrEmpty(trimmedLine) &&
                            !trimmedLine.StartsWith("<") &&
                            !trimmedLine.StartsWith("<!DOCTYPE"))
                        {
                            proxies.Add(trimmedLine);
                        }
                    }

                    var uniqueProxies = new HashSet<string>(proxies);
                    return new List<string>(uniqueProxies);
                }
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Ошибка загрузки прокси: {0}", ex.Message), ex);
            }
        }

        // Нормализация ключа прокси 
        private string NormalizeProxyKey(string server, int port, string secret)
        {
            // Очищаем secret от мусора
            string cleanSecret = secret;
            int cutIndex = cleanSecret.IndexOfAny(new[] { '#', '@', '$', '&' });
            if (cutIndex > 0)
            {
                cleanSecret = cleanSecret.Substring(0, cutIndex);
            }

            // Извлекаем только hex-часть
            cleanSecret = System.Text.RegularExpressions.Regex.Match(cleanSecret, @"[a-fA-F0-9]{16,}").Value;
            if (string.IsNullOrEmpty(cleanSecret))
            {
                cleanSecret = secret;
            }

            cleanSecret = cleanSecret.ToLowerInvariant();
            return $"{server}:{port}:{cleanSecret}";
        }

        // Парсинг одной строки в ProxyInfo
        private ProxyInfo ParseSingleProxy(string line, ProxyCheckerService checkerService = null)
        {
            try
            {
                string trimmedLine = line.Trim();

                if (IsGarbageLine(trimmedLine))
                    return null;

                if (!HasProxyParameters(trimmedLine))
                    return null;

                string server = ExtractParameter(trimmedLine, "server");
                string portStr = ExtractParameter(trimmedLine, "port");
                string secret = ExtractParameter(trimmedLine, "secret");

                if (string.IsNullOrEmpty(server) ||
                    string.IsNullOrEmpty(portStr) ||
                    string.IsNullOrEmpty(secret))
                    return null;

                int port;
                if (!int.TryParse(portStr, out port))
                    return null;

                string tgProxyUrl = string.Format("tg://proxy?server={0}&port={1}&secret={2}",
                    Uri.EscapeDataString(server),
                    port,
                    Uri.EscapeDataString(secret));

                return new ProxyInfo
                {
                    Server = server,
                    Port = port,
                    Secret = secret,
                    OriginalUrl = tgProxyUrl,
                    ProxyType = checkerService != null ? checkerService.DetectProxyType(secret) : "Unknown"
                };
            }
            catch
            {
                return null;
            }
        }

        // УНИВЕРСАЛЬНЫЙ МЕТОД ПАРСИНГА - сначала сбор, потом нормализация и дедупликация
        public List<ProxyInfo> ParseProxyLines(List<string> lines, ProxyCheckerService checkerService = null)
        {
            // 1. Сначала парсим все строки в прокси (без дедупликации)
            var allParsedProxies = new List<ProxyInfo>();
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                var proxy = ParseSingleProxy(line, checkerService);
                if (proxy != null)
                {
                    allParsedProxies.Add(proxy);
                }
            }

            // 2. Дедупликация по нормализованному ключу (server:port:cleanSecret)
            var uniqueProxies = new Dictionary<string, ProxyInfo>();
            foreach (var proxy in allParsedProxies)
            {
                string key = NormalizeProxyKey(proxy.Server, proxy.Port, proxy.Secret);
                if (!uniqueProxies.ContainsKey(key))
                {
                    uniqueProxies.Add(key, proxy);
                }
            }

            // 3. Возвращаем уникальные прокси
            return uniqueProxies.Values.ToList();
        }

        // Для обратной совместимости
        public List<ProxyInfo> ParseProxyUrls(List<string> proxyUrls)
        {
            return ParseProxyLines(proxyUrls, null);
        }

        private bool IsGarbageLine(string line)
        {
            if (line.Length < 20) return true;

            string[] garbageMarkers = {
                "<", "<!DOCTYPE", "<!--", "/", "*", "```",
                "null", "undefined", "NaN", "function", "var ",
                "const ", "let ", "return", "console.", "window.",
                "===", "---", "___", ">>>", "<<<"
            };

            foreach (var marker in garbageMarkers)
            {
                if (line.StartsWith(marker)) return true;
                if (line.Contains(marker) && line.Length < 50) return true;
            }

            return false;
        }

        private bool HasProxyParameters(string line)
        {
            return line.Contains("server=") &&
                   line.Contains("port=") &&
                   line.Contains("secret=");
        }

        private string ExtractParameter(string text, string paramName)
        {
            // Ищем paramName= и берем значение до & ИЛИ до @ ИЛИ до # ИЛИ до пробела ИЛИ до конца строки
            string pattern = string.Format(@"{0}=([^&#@\s]+)", paramName);
            var match = Regex.Match(text, pattern);
            return match.Success ? Uri.UnescapeDataString(match.Groups[1].Value) : null;
        }
    }
}