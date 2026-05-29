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
                    string trimmedLine = line.Trim();

                    // 1. Пропускаем HTML теги
                    if (trimmedLine.StartsWith("<") ||
                        trimmedLine.StartsWith("<!DOCTYPE") ||
                        trimmedLine.StartsWith("<!--") ||
                        trimmedLine.StartsWith("/") ||
                        trimmedLine.StartsWith("*") ||
                        trimmedLine.StartsWith("```"))  // маркеры кода
                        continue;

                    // 2. Пропускаем явно невалидные строки
                    if (trimmedLine.Length < 20) // слишком короткие
                        continue;

                    if (trimmedLine.Contains("null") ||
                        trimmedLine.Contains("undefined") ||
                        trimmedLine.Contains("NaN"))
                        continue;

                    // 3. Проверяем наличие ключевых параметров прокси
                    bool hasServer = trimmedLine.Contains("server=");
                    bool hasPort = trimmedLine.Contains("port=");
                    bool hasSecret = trimmedLine.Contains("secret=");

                    if (!hasServer || !hasPort || !hasSecret)
                        continue;

                    // 4. Извлекаем параметры (даже если есть лишний текст вокруг)
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

                        proxies.Add(new ProxyInfo
                        {
                            Server = server,
                            Port = port,
                            Secret = secret,
                            OriginalUrl = tgProxyUrl,
                            ProxyType = _checkerService.DetectProxyType(secret)
                        });
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Ошибка парсинга строки: {line}, {ex.Message}");
                }
            }

            return proxies;
        }

        // Вспомогательный метод для извлечения параметров из мусора
        private string ExtractParameter(string text, string paramName)
        {
            // Ищем paramName=xxx до & или конца строки
            string pattern = $@"{paramName}=([^&\s]+)";
            var match = Regex.Match(text, pattern);

            if (match.Success)
            {
                return Uri.UnescapeDataString(match.Groups[1].Value);
            }

            return null;
        }
    }
}