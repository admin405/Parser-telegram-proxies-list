////////////////////////
/// Методы проксирования
////////////////////////
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using TelegramProxyParser.Models;

namespace TelegramProxyParser.Services
{
    public class ProxyLoadService
    {
        private readonly ProxyParserService _parserService;
        private readonly ProxyCheckerService _checkerService;

        public ProxyLoadService(ProxyParserService parserService, ProxyCheckerService checkerService)
        {
            _parserService = parserService;
            _checkerService = checkerService;
        }

        // Для Европы/России - стандартные списки
        public async Task<List<ProxyInfo>> LoadStandardProxiesAsync(string url, CancellationToken cancellationToken = default)
        {
            var proxyUrls = await _parserService.LoadProxiesFromUrlAsync(url, cancellationToken);
            var proxies = _parserService.ParseProxyUrls(proxyUrls);

            // Определяем типы прокси
            foreach (var proxy in proxies)
            {
                proxy.ProxyType = _checkerService.DetectProxyType(proxy.Secret);
            }

            return proxies;
        }

        // Для SurfboardV2ray - сырые данные с HTML
        public async Task<List<ProxyInfo>> LoadRawProxiesAsync(string url, CancellationToken cancellationToken = default)
        {
            var rawLines = await _parserService.LoadRawProxiesFromUrlAsync(url, cancellationToken);
            return ParseSpecialProxyFormat(rawLines);
        }

        // Для локального файла
        public List<ProxyInfo> LoadProxiesFromFile(List<string> lines)
        {
            return ParseSpecialProxyFormat(lines);
        }

        // Общий метод парсинга специального формата
        private List<ProxyInfo> ParseSpecialProxyFormat(List<string> proxyLines)
        {
            var proxies = new List<ProxyInfo>();

            foreach (var line in proxyLines)
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                try
                {
                    if (line.TrimStart().StartsWith("<") || line.Contains("<!DOCTYPE"))
                        continue;

                    if (line.Contains("t.me/proxy") && line.Contains("server="))
                    {
                        var proxy = ParseTelegramProxyLink(line);
                        if (proxy != null)
                            proxies.Add(proxy);
                    }
                    else if (line.Trim().StartsWith("https://t.me/proxy?"))
                    {
                        var proxy = ParseTelegramProxyLink(line.Trim());
                        if (proxy != null)
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

        private ProxyInfo ParseTelegramProxyLink(string url)
        {
            try
            {
                var serverMatch = Regex.Match(url, @"server=([^&]+)");
                var portMatch = Regex.Match(url, @"port=(\d+)");
                var secretMatch = Regex.Match(url, @"secret=([^&]+)");

                if (serverMatch.Success && portMatch.Success && secretMatch.Success)
                {
                    string server = Uri.UnescapeDataString(serverMatch.Groups[1].Value);
                    string portStr = Uri.UnescapeDataString(portMatch.Groups[1].Value);
                    string secret = Uri.UnescapeDataString(secretMatch.Groups[1].Value);

                    if (int.TryParse(portStr, out int port))
                    {
                        string tgProxyUrl = $"tg://proxy?server={Uri.EscapeDataString(server)}&port={port}&secret={Uri.EscapeDataString(secret)}";

                        return new ProxyInfo
                        {
                            Server = server,
                            Port = port,
                            Secret = secret,
                            OriginalUrl = tgProxyUrl,
                            ProxyType = _checkerService.DetectProxyType(secret)
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка парсинга ссылки: {url}, {ex.Message}");
            }

            return null;
        }
    }
}