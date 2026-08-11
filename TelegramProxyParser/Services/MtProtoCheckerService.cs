using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TelegramProxyParser.Models;

namespace TelegramProxyParser.Services
{
    public class MtProtoCheckerService
    {
        // Жестко заданные константы
        private const int MT_PROTO_TIMEOUT = 5000;      // Задержка
        private const int MT_PROTO_CONCURRENCY = 25;     // Потоки

        private int _timeout = MT_PROTO_TIMEOUT;
        private int _concurrency = MT_PROTO_CONCURRENCY;
        private static readonly object _logLock = new object();

        public void SetParameters(int timeout, int concurrency)
        {
            // Игнорируем входящие параметры из "Настроек", используем константы
            _timeout = MT_PROTO_TIMEOUT;
            _concurrency = MT_PROTO_CONCURRENCY;

        }

        public async Task<List<ProxyInfo>> CheckProxiesAsync(
            List<ProxyInfo> proxies,
            Action<string> progressCallback,
            CancellationToken cancellationToken)
        {
            if (proxies == null || proxies.Count == 0)
                return new List<ProxyInfo>();

            var workingProxies = new List<ProxyInfo>();
            int working = 0;
            int completed = 0;

            var mtProtoProxies = proxies
                .Where(p => p != null &&
                           !string.IsNullOrEmpty(p.Server) &&
                           p.Port > 0 &&
                           !string.IsNullOrEmpty(p.Secret) &&
                           p.Secret.StartsWith("ee", StringComparison.OrdinalIgnoreCase))
                .ToList();

            int total = mtProtoProxies.Count;
            Log($"Начало MTProto проверки. Найдено целевых прокси: {total}");

            if (total == 0)
            {
                progressCallback?.Invoke("Нет прокси с секретом 'ee' для MTProto проверки");
                return workingProxies;
            }

            using (var semaphore = new SemaphoreSlim(_concurrency))
            {
                var tasks = mtProtoProxies.Select(async proxy =>
                {
                    if (proxy == null || cancellationToken.IsCancellationRequested)
                    {
                        Interlocked.Increment(ref completed);
                        return;
                    }

                    await semaphore.WaitAsync(cancellationToken);

                    try
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        var result = await Task.Run(() =>
                            CheckProxyWithHandshake(proxy.Server, proxy.Port, proxy.Secret),
                            cancellationToken
                        );

                        int currentCompleted = Interlocked.Increment(ref completed);

                        if (result.IsWorking)
                        {
                            proxy.IsWorking = true;
                            proxy.ProxyType = result.ProxyType;
                            proxy.Ping = result.PingMs;

                            lock (workingProxies)
                            {
                                workingProxies.Add(proxy);
                            }

                            Interlocked.Increment(ref working);
                            Log($"[{currentCompleted}/{total}] {proxy.Server}:{proxy.Port} - {result.ProxyType} РАБОТАЕТ ({result.PingMs}ms)");
                        }
                        else
                        {
                            proxy.IsWorking = false;
                            proxy.ProxyType = "Invalid";
                            proxy.Ping = -1;
                            proxy.ErrorMessage = result.ErrorMessage;

                            if (currentCompleted <= 5 || currentCompleted % 50 == 0 || currentCompleted == total)
                            {
                                Log($"[{currentCompleted}/{total}] {proxy.Server}:{proxy.Port} - {result.ErrorMessage}");
                            }
                        }

                        if (currentCompleted % 10 == 0 || currentCompleted == total)
                        {
                            progressCallback?.Invoke(
                                $"MTProto проверка: {currentCompleted}/{total} | Рабочих: {working}"
                            );
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        Interlocked.Increment(ref completed);
                    }
                    catch (Exception ex)
                    {
                        if (proxy != null)
                        {
                            proxy.IsWorking = false;
                            proxy.ErrorMessage = $"Error: {ex.Message}";
                            proxy.Ping = -1;
                        }
                        Interlocked.Increment(ref completed);
                    }
                    finally
                    {
                        try { semaphore.Release(); }
                        catch { }
                    }
                });

                await Task.WhenAll(tasks);
            }

            workingProxies = workingProxies
                .Where(p => p != null && p.Ping > 0)
                .OrderBy(p => p.Ping)
                .ToList();

            Log($"Завершено. Рабочих: {workingProxies.Count}");
            return workingProxies;
        }

        private ProxyResult CheckProxyWithHandshake(string ip, int port, string secret)
        {
            if (string.IsNullOrEmpty(ip) || port <= 0)
                return new ProxyResult { ErrorMessage = "Invalid IP or port" };

            if (string.IsNullOrEmpty(secret))
                return new ProxyResult { ErrorMessage = "Secret is empty" };

            string secretLower = secret.ToLower();
            string proxyType = "Fake TLS";

            var result = new ProxyResult { ProxyType = proxyType };
            DateTime startTime = DateTime.Now;

            TcpClient client = null;

            try
            {
                client = new TcpClient();
                client.NoDelay = true;
                client.ReceiveTimeout = _timeout;
                client.SendTimeout = _timeout;

                IAsyncResult asyncResult = client.BeginConnect(ip, port, null, null);
                bool success = asyncResult.AsyncWaitHandle.WaitOne(_timeout);

                if (!success)
                {
                    result.ErrorMessage = $"Connection timeout ({_timeout}ms)";
                    return result;
                }

                client.EndConnect(asyncResult);

                if (!client.Connected)
                {
                    result.ErrorMessage = "Connection failed";
                    return result;
                }

                using (var stream = client.GetStream())
                {
                    stream.ReadTimeout = _timeout;
                    stream.WriteTimeout = _timeout;

                    // В методе CheckProxyWithHandshake, блок EE:

                    byte[] request = BuildFakeTLSRequest(secret);
                    if (request == null || request.Length == 0)
                    {
                        result.ErrorMessage = "Failed to build FakeTLS request";
                        return result;
                    }

                    stream.Write(request, 0, request.Length);
                    stream.Flush();

                    byte[] buffer = new byte[1024];
                    int tlsBytesRead = stream.Read(buffer, 0, buffer.Length);

                    if (tlsBytesRead > 0)
                    {
                        // Старая проверка - только 0x16
                        if (buffer[0] == 0x16)
                        {
                            result.IsWorking = true;
                            result.PingMs = (int)(DateTime.Now - startTime).TotalMilliseconds;
                            return result;
                        }
                        result.ErrorMessage = "Response is not a valid TLS handshake";
                    }
                    else
                    {
                        result.ErrorMessage = "Empty response / Connection closed by proxy";
                    }
                }
            }
            catch (Exception ex)
            {
                result.ErrorMessage = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
            }
            finally
            {
                try
                {
                    if (client != null)
                    {
                        client.Close();
                    }
                }
                catch { }
            }

            return result;
        }

        private byte[] BuildFakeTLSRequest(string secret)
        {
            if (secret.StartsWith("ee", StringComparison.OrdinalIgnoreCase))
                secret = secret.Substring(2);

            byte[] domainBytes;

            if (secret.Length > 32)
            {
                string domainHex = secret.Substring(32);
                domainBytes = HexStringToBytes(domainHex);
            }
            else
            {
                domainBytes = Encoding.UTF8.GetBytes("t.me");
            }

            if (domainBytes == null || domainBytes.Length == 0)
            {
                domainBytes = Encoding.UTF8.GetBytes("google.com");
            }

            var packet = new List<byte>();

            // TLS Record Header
            packet.Add(0x16);
            packet.Add(0x03); packet.Add(0x01);
            packet.AddRange(new byte[] { 0x00, 0x00 });

            int handshakeStartOffset = packet.Count;

            // Handshake Header
            packet.Add(0x01);
            packet.AddRange(new byte[] { 0x00, 0x00, 0x00 });

            // Version
            packet.Add(0x03); packet.Add(0x03);

            // Random
            byte[] random = new byte[32];
            using (var rng = new RNGCryptoServiceProvider()) { rng.GetBytes(random); }
            packet.AddRange(random);

            // Session ID
            packet.Add(0x20);
            byte[] sessionId = new byte[32];
            using (var rng = new RNGCryptoServiceProvider()) { rng.GetBytes(sessionId); }
            packet.AddRange(sessionId);

            // Cipher Suites
            packet.AddRange(new byte[] { 0x00, 0x12 });
            packet.AddRange(new byte[] {
                0x13, 0x01, 0x13, 0x02, 0x13, 0x03,
                0xC0, 0x2B, 0xC0, 0x2F, 0xC0, 0x2C, 0xC0, 0x30,
                0x00, 0x9C, 0x00, 0x9D
            });

            // Compression Methods
            packet.Add(0x01); packet.Add(0x00);

            // Extensions
            int extensionsLengthOffset = packet.Count;
            packet.AddRange(new byte[] { 0x00, 0x00 });

            // SNI Extension
            packet.AddRange(new byte[] { 0x00, 0x00 });
            int sniLengthOffset = packet.Count;
            packet.AddRange(new byte[] { 0x00, 0x00 });

            int serverNameListLengthOffset = packet.Count;
            packet.AddRange(new byte[] { 0x00, 0x00 });

            packet.Add(0x00);
            packet.Add((byte)((domainBytes.Length >> 8) & 0xFF));
            packet.Add((byte)(domainBytes.Length & 0xFF));
            packet.AddRange(domainBytes);

            // Пересчет длин
            int totalPayload = packet.Count;

            int serverNameListLength = totalPayload - serverNameListLengthOffset - 2;
            packet[serverNameListLengthOffset] = (byte)(serverNameListLength >> 8);
            packet[serverNameListLengthOffset + 1] = (byte)(serverNameListLength & 0xFF);

            int sniLength = totalPayload - sniLengthOffset - 2;
            packet[sniLengthOffset] = (byte)(sniLength >> 8);
            packet[sniLengthOffset + 1] = (byte)(sniLength & 0xFF);

            int extLength = totalPayload - extensionsLengthOffset - 2;
            packet[extensionsLengthOffset] = (byte)(extLength >> 8);
            packet[extensionsLengthOffset + 1] = (byte)(extLength & 0xFF);

            int handshakeLength = packet.Count - handshakeStartOffset - 4;
            packet[handshakeStartOffset + 1] = (byte)((handshakeLength >> 16) & 0xFF);
            packet[handshakeStartOffset + 2] = (byte)((handshakeLength >> 8) & 0xFF);
            packet[handshakeStartOffset + 3] = (byte)(handshakeLength & 0xFF);

            int recordLength = packet.Count - 5;
            packet[3] = (byte)(recordLength >> 8);
            packet[4] = (byte)(recordLength & 0xFF);

            return packet.ToArray();
        }

        private byte[] HexStringToBytes(string hex)
        {
            if (string.IsNullOrEmpty(hex))
                return new byte[0];

            hex = hex.Replace(" ", "").Replace("-", "");

            if (hex.Length % 2 != 0)
                return new byte[0];

            try
            {
                byte[] bytes = new byte[hex.Length / 2];
                for (int i = 0; i < hex.Length; i += 2)
                {
                    bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
                }
                return bytes;
            }
            catch
            {
                return new byte[0];
            }
        }

        private void Log(string message)
        {
            lock (_logLock)
            {
                Debug.WriteLine($"[MTProto] {message}");
                Console.WriteLine($"[MTProto] {message}");
            }
        }

        private class ProxyResult
        {
            public bool IsWorking { get; set; } = false;
            public string ProxyType { get; set; } = "Unknown";
            public int PingMs { get; set; } = -1;
            public string ErrorMessage { get; set; } = "";
        }
    }
}