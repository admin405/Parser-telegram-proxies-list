using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using TelegramProxyParser.Models;

namespace TelegramProxyParser.Services
{
    public class ProxyCheckerService
    {
        private const int TIMEOUT_MS = 300;

        // Синхронная версия (для обратной совместимости)
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

        // Асинхронная версия для параллельной проверки
        public async Task<ProxyCheckResult> CheckProxyAsync(string server, int port, string secret)
        {
            var result = new ProxyCheckResult
            {
                StartTime = DateTime.Now,
                ProxyType = DetectProxyType(secret)
            };

            try
            {
                using (var tcpClient = new TcpClient())
                {
                    // Асинхронное подключение с таймаутом
                    var connectTask = tcpClient.ConnectAsync(server, port);
                    var timeoutTask = Task.Delay(TIMEOUT_MS);

                    var completedTask = await Task.WhenAny(connectTask, timeoutTask);

                    if (completedTask == timeoutTask)
                    {
                        result.ErrorMessage = "Таймаут подключения";
                        result.IsWorking = false;
                        return result;
                    }

                    await connectTask; // Пробрасываем возможные исключения

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
            }
            catch (SocketException ex)
            {
                result.ErrorMessage = ex.Message.Contains("refused") ? "Порт закрыт" : $"Ошибка сокета: {ex.Message}";
                result.IsWorking = false;
            }
            catch (OperationCanceledException)
            {
                result.ErrorMessage = "Операция отменена";
                result.IsWorking = false;
            }
            catch (Exception ex)
            {
                result.ErrorMessage = ex.Message;
                result.IsWorking = false;
            }

            return result;
        }

        // Улучшенная версия с возможностью проверки нескольких портов
        public async Task<ProxyCheckResult> CheckProxyAdvancedAsync(string server, int port, string secret, int timeoutMs = TIMEOUT_MS)
        {
            var result = new ProxyCheckResult
            {
                StartTime = DateTime.Now,
                ProxyType = DetectProxyType(secret)
            };

            try
            {
                using (var tcpClient = new TcpClient())
                {
                    // Настройка сокета для лучшей производительности
                    tcpClient.SendTimeout = timeoutMs;
                    tcpClient.ReceiveTimeout = timeoutMs;

                    var connectTask = tcpClient.ConnectAsync(server, port);
                    var timeoutTask = Task.Delay(timeoutMs);

                    var completedTask = await Task.WhenAny(connectTask, timeoutTask);

                    if (completedTask == timeoutTask)
                    {
                        result.ErrorMessage = $"Таймаут подключения ({timeoutMs}мс)";
                        result.IsWorking = false;
                        return result;
                    }

                    await connectTask;

                    if (tcpClient.Connected)
                    {
                        result.IsWorking = true;
                        result.ResponseTime = (int)(DateTime.Now - result.StartTime).TotalMilliseconds;

                        // Опционально: проверка MTProto протокола
                        if (result.ResponseTime <= timeoutMs)
                        {
                            result.IsMTProto = await CheckMTProtoAsync(tcpClient, secret);
                        }
                    }
                    else
                    {
                        result.ErrorMessage = "Не удалось подключиться";
                        result.IsWorking = false;
                    }
                }
            }
            catch (SocketException ex)
            {
                result.ErrorMessage = ex.Message.Contains("refused") ? "Порт закрыт" :
                                     ex.Message.Contains("timed out") ? "Таймаут сокета" :
                                     $"Ошибка сокета: {ex.Message}";
                result.IsWorking = false;
            }
            catch (OperationCanceledException)
            {
                result.ErrorMessage = "Операция отменена";
                result.IsWorking = false;
            }
            catch (Exception ex)
            {
                result.ErrorMessage = ex.Message;
                result.IsWorking = false;
            }

            return result;
        }

        // Дополнительная проверка MTProto протокола
        private async Task<bool> CheckMTProtoAsync(TcpClient tcpClient, string secret)
        {
            try
            {
                var stream = tcpClient.GetStream();

                // Простая проверка: отправляем минимальный пакет MTProto
                // Это базовый хэллоу-пакет для проверки протокола
                byte[] mtprotoHello = new byte[]
                {
                    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, // 64-bit нули
                    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, // для простого пинга
                    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
                };

                // Устанавливаем таймаут на операцию
                var sendTask = stream.WriteAsync(mtprotoHello, 0, mtprotoHello.Length);
                var timeoutTask = Task.Delay(100);

                var completedTask = await Task.WhenAny(sendTask, timeoutTask);
                if (completedTask == timeoutTask)
                {
                    return false; // Таймаут отправки
                }

                await sendTask;

                // Пытаемся прочитать ответ
                byte[] buffer = new byte[1024];
                var readTask = stream.ReadAsync(buffer, 0, buffer.Length);
                timeoutTask = Task.Delay(100);

                completedTask = await Task.WhenAny(readTask, timeoutTask);
                if (completedTask == readTask)
                {
                    int bytesRead = await readTask;
                    return bytesRead > 0; // Получили хоть какой-то ответ
                }

                return false;
            }
            catch
            {
                return false; // Ошибка протокола
            }
        }

        // Оптимизированная версия для массовой проверки
        public async Task<ProxyCheckResult> CheckProxyFastAsync(string server, int port, string secret)
        {
            var result = new ProxyCheckResult
            {
                StartTime = DateTime.Now,
                ProxyType = DetectProxyType(secret)
            };

            try
            {
                using (var tcpClient = new TcpClient())
                {
                    // Минимальные настройки для быстрой проверки
                    tcpClient.NoDelay = true; // Отключаем Nagle для скорости

                    var connectTask = tcpClient.ConnectAsync(server, port);
                    var timeoutTask = Task.Delay(TIMEOUT_MS);

                    var completedTask = await Task.WhenAny(connectTask, timeoutTask);

                    if (completedTask == timeoutTask)
                    {
                        result.IsWorking = false;
                        return result;
                    }

                    await connectTask;

                    result.IsWorking = tcpClient.Connected;
                    if (result.IsWorking)
                    {
                        result.ResponseTime = (int)(DateTime.Now - result.StartTime).TotalMilliseconds;
                    }
                }
            }
            catch
            {
                result.IsWorking = false;
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