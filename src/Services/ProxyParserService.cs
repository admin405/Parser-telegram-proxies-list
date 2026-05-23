using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
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

        public async Task<List<string>> LoadProxiesFromUrlAsync(string url, CancellationToken cancellationToken = default)
        {
            try
            {
                // Создаем запрос с возможностью отмены
                using (var request = new HttpRequestMessage(HttpMethod.Get, url))
                using (var response = await httpClient.SendAsync(request, cancellationToken))
                {
                    response.EnsureSuccessStatusCode();

                    // ReadAsStringAsync без CancellationToken в .NET Framework
                    var content = await response.Content.ReadAsStringAsync();

                    var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                    var proxies = new List<string>();
                    foreach (var line in lines)
                    {
                        var trimmedLine = line.Trim();
                        // Исправлено: теперь поддерживаются оба формата
                        if (trimmedLine.StartsWith("tg://proxy", StringComparison.OrdinalIgnoreCase) ||
                            trimmedLine.StartsWith("https://t.me/proxy", StringComparison.OrdinalIgnoreCase))
                        {
                            proxies.Add(trimmedLine);
                        }
                    }

                    // Удаляем дубликаты
                    var uniqueProxies = new HashSet<string>(proxies);
                    return new List<string>(uniqueProxies);
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка загрузки прокси: {ex.Message}", ex);
            }
        }

        // Новый метод для загрузки прокси без фильтрации (для тестового источника)
        public async Task<List<string>> LoadRawProxiesFromUrlAsync(string url, CancellationToken cancellationToken = default)
        {
            try
            {
                using (var request = new HttpRequestMessage(HttpMethod.Get, url))
                using (var response = await httpClient.SendAsync(request, cancellationToken))
                {
                    response.EnsureSuccessStatusCode();
                    var content = await response.Content.ReadAsStringAsync();

                    var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                    var proxies = new List<string>();
                    foreach (var line in lines)
                    {
                        var trimmedLine = line.Trim();
                        // Пропускаем пустые строки и HTML-теги
                        if (!string.IsNullOrEmpty(trimmedLine) &&
                            !trimmedLine.StartsWith("<") &&
                            !trimmedLine.StartsWith("<!DOCTYPE"))
                        {
                            proxies.Add(trimmedLine);
                        }
                    }

                    // Удаляем дубликаты
                    var uniqueProxies = new HashSet<string>(proxies);
                    return new List<string>(uniqueProxies);
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка загрузки прокси: {ex.Message}", ex);
            }
        }

        public List<ProxyInfo> ParseProxyUrls(List<string> proxyUrls)
        {
            var proxies = new List<ProxyInfo>();

            foreach (var url in proxyUrls)
            {
                var proxy = ParseProxyUrl(url);
                if (proxy != null && !string.IsNullOrEmpty(proxy.Server))
                {
                    proxies.Add(proxy);
                }
            }

            return proxies;
        }

        private ProxyInfo ParseProxyUrl(string proxyUrl)
        {
            var proxyInfo = new ProxyInfo
            {
                OriginalUrl = proxyUrl,
                Port = 443
            };

            try
            {
                var serverMatch = Regex.Match(proxyUrl, @"server=([^&]+)");
                if (serverMatch.Success)
                    proxyInfo.Server = Uri.UnescapeDataString(serverMatch.Groups[1].Value);

                var portMatch = Regex.Match(proxyUrl, @"port=(\d+)");
                if (portMatch.Success)
                    proxyInfo.Port = int.Parse(portMatch.Groups[1].Value);

                var secretMatch = Regex.Match(proxyUrl, @"secret=([^&]+)");
                if (secretMatch.Success)
                    proxyInfo.Secret = secretMatch.Groups[1].Value;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Parse error for {proxyUrl}: {ex.Message}");
                return null;
            }

            return proxyInfo;
        }
    }
}