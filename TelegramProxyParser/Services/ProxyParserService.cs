using System;
using System.Collections.Generic;
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
                throw new Exception($"Ошибка загрузки прокси: {ex.Message}", ex);
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
                throw new Exception($"Ошибка загрузки прокси: {ex.Message}", ex);
            }
        }

        // УНИВЕРСАЛЬНЫЙ МЕТОД ПАРСИНГА - работает с любым форматом и мусором
        public List<ProxyInfo> ParseProxyLines(List<string> lines, ProxyCheckerService checkerService = null)
        {
            var proxies = new List<ProxyInfo>();

            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                try
                {
                    string trimmedLine = line.Trim();

                    // Пропускаем явный мусор
                    if (IsGarbageLine(trimmedLine))
                        continue;

                    // Проверяем наличие ключевых параметров
                    if (!HasProxyParameters(trimmedLine))
                        continue;

                    // Извлекаем параметры
                    string server = ExtractParameter(trimmedLine, "server");
                    string portStr = ExtractParameter(trimmedLine, "port");
                    string secret = ExtractParameter(trimmedLine, "secret");

                    if (string.IsNullOrEmpty(server) ||
                        string.IsNullOrEmpty(portStr) ||
                        string.IsNullOrEmpty(secret))
                        continue;

                    if (int.TryParse(portStr, out int port))
                    {
                        string tgProxyUrl = $"tg://proxy?server={Uri.EscapeDataString(server)}&port={port}&secret={Uri.EscapeDataString(secret)}";

                        var proxy = new ProxyInfo
                        {
                            Server = server,
                            Port = port,
                            Secret = secret,
                            OriginalUrl = tgProxyUrl,
                            ProxyType = checkerService?.DetectProxyType(secret) ?? "Unknown"
                        };

                        proxies.Add(proxy);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Ошибка парсинга строки: {line}, {ex.Message}");
                }
            }

            return proxies;
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
            string pattern = $@"{paramName}=([^&\s]+)";
            var match = Regex.Match(text, pattern);
            return match.Success ? Uri.UnescapeDataString(match.Groups[1].Value) : null;
        }
    }
}