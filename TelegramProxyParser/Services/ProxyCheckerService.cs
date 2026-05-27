using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using TelegramProxyParser.Models;

namespace TelegramProxyParser.Services
{
    public class ProxyCheckerService
    {
        private int defaultTimeout = 300;

        public void SetTimeout(int timeoutMs)
        {
            defaultTimeout = timeoutMs;
        }

        // Надежная версия с ManualResetEvent для всех версий .NET
        public async Task<ProxyCheckResult> CheckProxyWithTimeoutAsync(string server, int port, string secret, int timeoutMs)
        {
            return await Task.Run(() =>
            {
                var result = new ProxyCheckResult
                {
                    StartTime = DateTime.Now,
                    ProxyType = DetectProxyType(secret)
                };

                TcpClient tcpClient = null;

                try
                {
                    tcpClient = new TcpClient();
                    var connectDone = new ManualResetEvent(false);

                    var asyncResult = tcpClient.BeginConnect(server, port,
                        (ar) => { connectDone.Set(); }, null);

                    bool connected = connectDone.WaitOne(timeoutMs);

                    if (connected)
                    {
                        try
                        {
                            tcpClient.EndConnect(asyncResult);
                            result.IsWorking = true;
                            result.ResponseTime = (int)(DateTime.Now - result.StartTime).TotalMilliseconds;
                        }
                        catch (Exception ex)
                        {
                            result.ErrorMessage = ex.Message;
                            result.IsWorking = false;
                        }
                    }
                    else
                    {
                        result.ErrorMessage = $"Таймаут подключения ({timeoutMs}мс)";
                        result.IsWorking = false;
                        tcpClient.Close();
                    }
                }
                catch (SocketException ex)
                {
                    result.ErrorMessage = ex.Message.Contains("refused") ? "Порт закрыт" : $"Ошибка сокета: {ex.Message}";
                    result.IsWorking = false;
                }
                catch (Exception ex)
                {
                    result.ErrorMessage = ex.Message;
                    result.IsWorking = false;
                }
                finally
                {
                    try { tcpClient?.Close(); } catch { }
                    try { tcpClient?.Dispose(); } catch { }
                }

                return result;
            });
        }

        // Синхронная версия
        public ProxyCheckResult CheckProxySync(string server, int port, string secret)
        {
            var result = new ProxyCheckResult
            {
                StartTime = DateTime.Now,
                ProxyType = DetectProxyType(secret)
            };

            TcpClient tcpClient = null;

            try
            {
                tcpClient = new TcpClient();
                var connectDone = new ManualResetEvent(false);

                var asyncResult = tcpClient.BeginConnect(server, port,
                    (ar) => { connectDone.Set(); }, null);

                bool connected = connectDone.WaitOne(defaultTimeout);

                if (connected)
                {
                    try
                    {
                        tcpClient.EndConnect(asyncResult);
                        result.IsWorking = true;
                        result.ResponseTime = (int)(DateTime.Now - result.StartTime).TotalMilliseconds;
                    }
                    catch (Exception ex)
                    {
                        result.ErrorMessage = ex.Message;
                        result.IsWorking = false;
                    }
                }
                else
                {
                    result.ErrorMessage = $"Таймаут подключения ({defaultTimeout}мс)";
                    result.IsWorking = false;
                    tcpClient.Close();
                }
            }
            catch (SocketException ex)
            {
                result.ErrorMessage = ex.Message.Contains("refused") ? "Порт закрыт" : $"Ошибка сокета: {ex.Message}";
                result.IsWorking = false;
            }
            catch (Exception ex)
            {
                result.ErrorMessage = ex.Message;
                result.IsWorking = false;
            }
            finally
            {
                try { tcpClient?.Close(); } catch { }
                try { tcpClient?.Dispose(); } catch { }
            }

            return result;
        }

        public async Task<ProxyCheckResult> CheckProxyFastAsync(string server, int port, string secret)
        {
            return await CheckProxyWithTimeoutAsync(server, port, secret, 200);
        }

        private string DetectProxyType(string secret)
        {
            if (string.IsNullOrEmpty(secret))
                return "Classic";

            if (secret.StartsWith("ee", StringComparison.OrdinalIgnoreCase))
                return "Fake TLS";

            if (secret.StartsWith("dd", StringComparison.OrdinalIgnoreCase))
                return "Secure";

            return "Classic";
        }
    }
}