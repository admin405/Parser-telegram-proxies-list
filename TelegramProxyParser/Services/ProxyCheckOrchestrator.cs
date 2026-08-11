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
        private int _timeout = 300;

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

        public void SetTimeout(int timeout)
        {
            _timeout = timeout;
            _checkerService.SetTimeout(timeout);
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
            // 1. Фильтруем ee-прокси
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
                StatusChanged?.Invoke("Нет прокси с секретом 'ee' для MTProto проверки");
                return;
            }

            // 2. ДЕДУПЛИКАЦИЯ
            var uniqueEeProxies = eeProxies
                .GroupBy(p => $"{p.Server}:{p.Port}:{p.Secret}")
                .Select(g => g.First())
                .ToList();

            int duplicatesCount = eeProxies.Count - uniqueEeProxies.Count;
            if (duplicatesCount > 0)
            {
                StatusChanged?.Invoke($"🗑️ Удалено дублей: {duplicatesCount} (было {eeProxies.Count}, стало {uniqueEeProxies.Count})");
            }

            // 3. Используем настройки пользователя
            _mtProtoService.SetParameters(_timeout, concurrency);

            StatusChanged?.Invoke($"MTProto проверка {uniqueEeProxies.Count} уникальных прокси...");

            // 4. Проверяем только уникальные
            WorkingProxies = await _mtProtoService.CheckProxiesAsync(
                uniqueEeProxies,
                progress => ProgressChanged?.Invoke(progress, 0, uniqueEeProxies.Count),
                ct);

            // 5. Отмечаем, что проверены через MTProto
            foreach (var proxy in WorkingProxies)
            {
                proxy.IsFromMtProtoCheck = true;
            }

            StatusChanged?.Invoke(
                $"MTProto проверка завершена | Уникальных: {uniqueEeProxies.Count} | Рабочих: {WorkingProxies.Count}");
        }

        public void Reset()
        {
            AllProxies.Clear();
            WorkingProxies.Clear();
        }
    }
}