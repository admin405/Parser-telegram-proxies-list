using System;
using System.Drawing;
using System.Windows.Forms;
using TelegramProxyParser.Models;

namespace TelegramProxyParser.UI.Controls
{
    public class ProxyCard : Panel
    {
        private ProxyInfo proxy;
        private FlowLayoutPanel parentFlow;

        public ProxyCard(ProxyInfo proxyInfo, int width, FlowLayoutPanel parent)
        {
            this.proxy = proxyInfo;
            this.parentFlow = parent;
            this.Width = width - 35;
            this.Height = 55;
            this.Margin = new Padding(0, 0, 0, 10);
            this.BackColor = Color.White;
            this.Padding = new Padding(0);

            CreateControls();
        }

        private void CreateControls()
        {
            // Только если Fake TLS из MTProto проверки
            if (proxy.ProxyType == "Fake TLS" && proxy.IsFromMtProtoCheck)
            {
                CreateMtProtoStyle();
            }
            else
            {
                CreateDefaultStyle();
            }
        }

        private void CreateMtProtoStyle()
        {
            // Кнопка во всю ширину, крупный шрифт, цвет MTProto, без типа и пинга
            var btnProxy = new Button()
            {
                Location = new Point(0, 0),
                Width = this.Width,
                Height = 55,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(0, 136, 204), // Цвет MTProto
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 14, FontStyle.Bold), // Крупный шрифт
                TextAlign = ContentAlignment.MiddleCenter,
                Text = $"{proxy.Server}:{proxy.Port}", // Только адрес, без типа
                Tag = proxy.OriginalUrl,
                Cursor = Cursors.Hand
            };
            btnProxy.FlatAppearance.BorderSize = 0;
            btnProxy.FlatAppearance.MouseOverBackColor = Color.FromArgb(0, 153, 230);
            btnProxy.FlatAppearance.MouseDownBackColor = Color.FromArgb(0, 119, 179);
            btnProxy.Click += (s, e) => OpenProxy();

            this.Controls.Add(btnProxy);
        }

        private void CreateDefaultStyle()
        {
            // Старый стиль для остальных типов
            var btnProxy = new Button()
            {
                Location = new Point(0, 0),
                Width = this.Width - 115,
                Height = 55,
                FlatStyle = FlatStyle.Flat,
                BackColor = GetTypeColor(proxy.ProxyType),
                ForeColor = Color.White,
                Font = new Font("Tahoma", 10, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                Text = $"{proxy.Server}:{proxy.Port}   |   {proxy.ProxyType}",
                Tag = proxy.OriginalUrl,
                Cursor = Cursors.Hand
            };
            btnProxy.FlatAppearance.BorderSize = 0;
            btnProxy.FlatAppearance.MouseOverBackColor = GetTypeHoverColor(proxy.ProxyType);
            btnProxy.FlatAppearance.MouseDownBackColor = GetTypeDownColor(proxy.ProxyType);
            btnProxy.Click += (s, e) => OpenProxy();

            var pingPanel = new Panel()
            {
                Location = new Point(this.Width - 115, 0),
                Size = new Size(115, 55),
                BackColor = Color.White
            };

            var lblPingValue = new Label()
            {
                Text = GetPingText(),
                Location = new Point(0, 0),
                Size = new Size(115, 55),
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = GetPingColor(),
                ForeColor = Color.Black,
                AutoSize = false
            };

            pingPanel.Controls.Add(lblPingValue);
            this.Controls.AddRange(new Control[] { btnProxy, pingPanel });
        }

        private Color GetTypeColor(string proxyType)
        {
            switch (proxyType)
            {
                case "MTProto":
                    return Color.FromArgb(155, 89, 182);
                case "Secure":
                    return Color.FromArgb(52, 152, 219);
                case "Classic":
                    return Color.FromArgb(243, 156, 18);
                default:
                    return Color.FromArgb(0, 136, 204);
            }
        }

        private Color GetTypeHoverColor(string proxyType)
        {
            switch (proxyType)
            {
                case "MTProto":
                    return Color.FromArgb(142, 68, 173);
                case "Secure":
                    return Color.FromArgb(41, 128, 185);
                case "Classic":
                    return Color.FromArgb(211, 134, 16);
                default:
                    return Color.FromArgb(114, 137, 218);
            }
        }

        private Color GetTypeDownColor(string proxyType)
        {
            switch (proxyType)
            {
                case "MTProto":
                    return Color.FromArgb(128, 58, 156);
                case "Secure":
                    return Color.FromArgb(31, 97, 141);
                case "Classic":
                    return Color.FromArgb(186, 118, 14);
                default:
                    return Color.FromArgb(68, 78, 188);
            }
        }

        private string GetPingText()
        {
            if (proxy.Ping <= 0)
                return "ОТЛИЧНО";

            if (proxy.Ping <= 80)
                return $"{proxy.Ping} МС";
            else if (proxy.Ping <= 250)
                return $"{proxy.Ping} МС";
            else
                return $"{proxy.Ping} МС";
        }

        private Color GetPingColor()
        {
            if (proxy.Ping <= 0)
                return Color.FromArgb(46, 204, 113);

            if (proxy.Ping <= 80)
                return Color.FromArgb(46, 204, 113);
            else if (proxy.Ping <= 250)
                return Color.FromArgb(241, 196, 15);
            else
                return Color.FromArgb(231, 76, 60);
        }

        private void OpenProxy()
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = proxy.OriginalUrl,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка открытия: {ex.Message}\n\nПроверьте, установлен ли Telegram.",
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}