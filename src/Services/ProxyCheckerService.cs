using System;
using System.Net.Sockets;
using TelegramProxyParser.Models;

namespace TelegramProxyParser.Services
{
    public class ProxyCheckerService
    {
        private const int TIMEOUT_MS = 300;

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

                // Синхронное подключение с таймаутом
                var connectResult = tcpClient.BeginConnect(server, port, null, null);
                bool connected = connectResult.AsyncWaitHandle.WaitOne(TIMEOUT_MS);

                if (!connected)
                {
                    result.ErrorMessage = "Таймаут подключения";
                    result.IsWorking = false;
                    return result;
                }

                tcpClient.EndConnect(connectResult);

                if (tcpClient.Connected)
                {
                    result.IsWorking = true;
                    result.ResponseTime = (int)(DateTime.Now - result.StartTime).TotalMilliseconds;
                }
                else
                {
                    result.ErrorMessage = "Не удалось подключиться";
                    result.IsWorking = false;
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
                tcpClient?.Close();
                tcpClient?.Dispose();
            }

            return result;
        }

        private string DetectProxyType(string secret)
        {
            if (string.IsNullOrEmpty(secret))
                return "Classic";

            // Fake TLS - маскируется под HTTPS трафик
            if (secret.StartsWith("ee", StringComparison.OrdinalIgnoreCase))
                return "Fake TLS";

            // Secure - с дополнительной криптозащитой
            if (secret.StartsWith("dd", StringComparison.OrdinalIgnoreCase))
                return "Secure";

            // Обычный классический MTProto
            return "Classic";
        }
    }
}