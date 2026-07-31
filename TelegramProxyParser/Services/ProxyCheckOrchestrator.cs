using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TelegramProxyParser.Models;

namespace TelegramProxyParser.Services
{
    public class ProxyCheckOrchestrator
    {
        private readonly ProxyLoadService _loadService;
        private readonly ProxyCheckerService _checkerService;
        private readonly MtProtoCheckerService _mtProtoService;

        public event Action<string> StatusChanged;
        public event Action<string, int, int> ProgressChanged;

        public List<ProxyInfo> AllProxies { get; private set; } = new List<ProxyInfo>();
        public List<ProxyInfo> WorkingProxies { get; private set; } = new List<ProxyInfo>();
        public string CurrentSourceName { get; set; }

        public ProxyCheckOrchestrator(
            ProxyLoadService loadService,
            ProxyCheckerService checkerService,
            MtProtoCheckerService mtProtoService)
        {
            _loadService = loadService;
            _checkerService = checkerService;
            _mtProtoService = mtProtoService;
        }

        public async Task LoadFromUrlAsync(string url, string sourceName)
        {
            CurrentSourceName = sourceName;
            StatusChanged?.Invoke($"Загрузка прокси: {sourceName}...");
            AllProxies = await _loadService.LoadProxiesFromUrlAsync(url);
        }

        public void LoadFromFile(List<string> lines)
        {
            AllProxies = _loadService.LoadProxiesFromFile(lines);
        }

        public async Task LoadFromMultipleUrlsAsync(List<string> urls)
        {
            StatusChanged?.Invoke("Загрузка всех источников...");

            var tasks = urls.Select(async url =>
            {
                try { return await _loadService.LoadProxiesFromUrlAsync(url); }
                catch { return new List<ProxyInfo>(); }
            });

            var results = await Task.WhenAll(tasks);
            var allLoaded = results.SelectMany(r => r).ToList();

            // Убираем дубликаты
            AllProxies = allLoaded
                .GroupBy(p => $"{p.Server}:{p.Port}:{p.Secret}")
                .Select(g => g.First())
                .ToList();
        }

        public async Task CheckAllAsync(int timeout, int concurrency, CancellationToken ct)
        {
            WorkingProxies = new List<ProxyInfo>();
            int total = AllProxies.Count;
            int completed = 0;

            if (total == 0) return;

            using (var semaphore = new SemaphoreSlim(concurrency))
            {
                var tasks = AllProxies.Select(async proxy =>
                {
                    if (ct.IsCancellationRequested) return;

                    await semaphore.WaitAsync(ct);
                    try
                    {
                        ct.ThrowIfCancellationRequested();

                        var result = await _checkerService.CheckProxyWithTimeoutAsync(
                            proxy.Server, proxy.Port, proxy.Secret, timeout);

                        proxy.IsWorking = result.IsWorking;
                        proxy.ProxyType = result.ProxyType;
                        proxy.Ping = result.ResponseTime;
                        proxy.ErrorMessage = result.ErrorMessage;

                        if (proxy.IsWorking)
                        {
                            lock (WorkingProxies) WorkingProxies.Add(proxy);
                        }

                        int current = Interlocked.Increment(ref completed);

                        if (current % 5 == 0 || current == total)
                        {
                            ProgressChanged?.Invoke(
                                $"Проверка: {current}/{total} (потоков: {concurrency})",
                                current,
                                total);
                        }

                        StatusChanged?.Invoke(
                            $"Проверка {CurrentSourceName}: {current}/{total} | Рабочих: {WorkingProxies.Count}");
                    }
                    catch (OperationCanceledException) { }
                    finally { semaphore.Release(); }
                });

                await Task.WhenAll(tasks);
            }
        }

        public async Task CheckMtProtoAsync(int concurrency, CancellationToken ct)
        {
            var eeProxies = AllProxies
                .Where(p => p != null &&
                           !string.IsNullOrEmpty(p.Server) &&
                           p.Port > 0 &&
                           !string.IsNullOrEmpty(p.Secret) &&
                           p.Secret.StartsWith("ee", StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (eeProxies.Count == 0)
            {
                WorkingProxies = new List<ProxyInfo>();
                return;
            }

            // ВОТ СЮДА ДОБАВИТЬ:
            _mtProtoService.SetParameters(3000, concurrency);

            StatusChanged?.Invoke($"MTProto проверка {eeProxies.Count} прокси...");

            WorkingProxies = await _mtProtoService.CheckProxiesAsync(
                eeProxies,
                progress => ProgressChanged?.Invoke(progress, 0, eeProxies.Count),
                ct);

            foreach (var proxy in WorkingProxies)
            {
                proxy.IsFromMtProtoCheck = true;
            }

            StatusChanged?.Invoke(
                $"MTProto проверка завершена | Всего: {eeProxies.Count} | Рабочих: {WorkingProxies.Count}");
        }

        public void Reset()
        {
            AllProxies.Clear();
            WorkingProxies.Clear();
        }
    }
}