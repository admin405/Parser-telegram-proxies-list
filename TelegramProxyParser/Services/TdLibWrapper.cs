using System;
using System.Runtime.InteropServices;

namespace TelegramProxyParser.Services
{
    /// <summary>
    /// Обертка для tdjson.dll — только проверка прокси, без авторизации
    /// </summary>
    public class TdLibWrapper : IDisposable
    {
        private IntPtr _clientPtr;
        private bool _disposed;

        // API данные хранятся прямо в коде
         
        private const int API_ID = 36488326;   
        private const string API_HASH = "28e0c7f8112c7b6c5c2d31fed35b66ac"; 

        [DllImport("tdjson.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr td_json_client_create();

        [DllImport("tdjson.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern void td_json_client_send(IntPtr client, string request);

        [DllImport("tdjson.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr td_json_client_receive(IntPtr client, double timeout);

        [DllImport("tdjson.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr td_json_client_execute(IntPtr client, string request);

        [DllImport("tdjson.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern void td_json_client_destroy(IntPtr client);

        public TdLibWrapper()
        {
            _clientPtr = td_json_client_create();
            if (_clientPtr == IntPtr.Zero)
                throw new InvalidOperationException(
                    "Не удалось создать TDLib клиент. Проверьте наличие tdjson.dll в папке с программой.");
        }

        /// <summary>
        /// Проверяет MTProto прокси через TDLib
        /// </summary>
        public bool CheckProxy(string server, int port, string secret)
        {
            try
            {
                string request = "{" +
                    "\"@type\":\"addProxy\"," +
                    "\"server\":\"" + EscapeJson(server) + "\"," +
                    "\"port\":" + port + "," +
                    "\"enable\":true," +
                    "\"type\":{" +
                        "\"@type\":\"proxyTypeMtproto\"," +
                        "\"secret\":\"" + EscapeJson(secret) + "\"" +
                    "}" +
                "}";

                string response = Execute(request);

                if (!string.IsNullOrEmpty(response))
                {
                    // Прокси принят TDLib
                    if (response.Contains("\"@type\":\"proxy\""))
                        return true;

                    // Ошибка — прокси нерабочий
                    if (response.Contains("\"@type\":\"error\""))
                        return false;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        public string Execute(string json)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(TdLibWrapper));

            IntPtr ptr = td_json_client_execute(_clientPtr, json);
            if (ptr == IntPtr.Zero)
                return null;

            return Marshal.PtrToStringAnsi(ptr);
        }

        private string EscapeJson(string text)
        {
            if (string.IsNullOrEmpty(text))
                return "";
            return text.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                if (_clientPtr != IntPtr.Zero)
                {
                    td_json_client_destroy(_clientPtr);
                    _clientPtr = IntPtr.Zero;
                }
            }
        }
    }
}