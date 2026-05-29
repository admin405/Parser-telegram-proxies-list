using System;
using System.Collections.Generic;
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

        // Универсальный метод для загрузки из URL (любой источник)
        public async Task<List<ProxyInfo>> LoadProxiesFromUrlAsync(string url)
        {
            var rawLines = await _parserService.LoadRawProxiesFromUrlAsync(url);
            var proxies = _parserService.ParseProxyLines(rawLines, _checkerService);

            // Дополнительно определяем типы (если не определились)
            foreach (var proxy in proxies)
            {
                if (proxy.ProxyType == "Unknown")
                    proxy.ProxyType = _checkerService.DetectProxyType(proxy.Secret);
            }

            return proxies;
        }

        // Для локального файла
        public List<ProxyInfo> LoadProxiesFromFile(List<string> lines)
        {
            return _parserService.ParseProxyLines(lines, _checkerService);
        }
    }
}