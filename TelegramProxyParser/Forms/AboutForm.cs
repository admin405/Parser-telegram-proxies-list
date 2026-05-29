using System;
using System.Drawing;
using System.Windows.Forms;

namespace TelegramProxyParser.UI.Forms
{
    public class AboutForm : Form
    {
        private string appVersion;
        private int currentTimeout;
        private int currentConcurrency;

        public AboutForm(string version, int timeout, int concurrency)
        {
            appVersion = version;
            currentTimeout = timeout;
            currentConcurrency = concurrency;
            InitializeForm();
        }

        private void InitializeForm()
        {
            this.Text = "О программе";
            this.Size = new Size(550, 580);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.White;

            Label lblInfo = new Label()
            {
                Text = $"Telegram Proxy Parser v{appVersion}\n\n" +
                       "Парсер и проверка прокси для Telegram\n\n" +
                       "Источники:\n" +
                       "• Европа: Маскировка под Google, Amazon, Microsoft и др.\n" +
                       "• Россия: Маскировка под Yandex, VK, Mail.ru, Gosuslugi и др.\n" +
                       "• SurfboardV2ray: Большой список прокси\n" +
                       "• СВОЙ СПИСОК: Загрузка своего .txt файла\n\n" +
                       "• Списки обновляются каждый час.\n" +
                       "• Доступность сервера не гарантирует его работоспособность!\n" +
                       "• Они не дремлют... Но и мы не спим!\n\n" +
                       $"Текущие настройки: таймаут {currentTimeout} мс, потоков: {currentConcurrency}\n" +
                       "Настройки можно изменить, нажав кнопку «Настройки»",
                Location = new Point(25, 20),
                Size = new Size(490, 350),
                Font = new Font("Tahoma", 10),
                TextAlign = ContentAlignment.TopLeft
            };

            LinkLabel lblAndroid = new LinkLabel()
            {
                Text = "📱 Скачать версию для Android",
                Location = new Point(125, 360),
                Size = new Size(280, 30),
                Font = new Font("Tahoma", 11, FontStyle.Bold | FontStyle.Underline),
                TextAlign = ContentAlignment.MiddleCenter,
                LinkColor = Color.FromArgb(46, 204, 113),
                ActiveLinkColor = Color.FromArgb(39, 174, 96)
            };
            lblAndroid.LinkClicked += (s, args) => OpenUrl("https://github.com/ComradeBingo/Proxy-Telegram-Android");

            LinkLabel lblGitHub = new LinkLabel()
            {
                Text = "💻 GitHub (Windows версия)",
                Location = new Point(125, 400),
                Size = new Size(280, 30),
                Font = new Font("Tahoma", 11, FontStyle.Bold | FontStyle.Underline),
                TextAlign = ContentAlignment.MiddleCenter,
                LinkColor = Color.FromArgb(88, 101, 242),
                ActiveLinkColor = Color.FromArgb(68, 78, 188)
            };
            lblGitHub.LinkClicked += (s, args) => OpenUrl("https://github.com/ComradeBingo/Proxy-telegram-windows/");

            Panel separator = new Panel()
            {
                Location = new Point(25, 440),
                Size = new Size(490, 1),
                BackColor = Color.FromArgb(224, 227, 234)
            };

            Label lblSupport = new Label()
            {
                Text = "⭐ Поддержать проект",
                Location = new Point(25, 460),
                Size = new Size(490, 30),
                Font = new Font("Tahoma", 11, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.FromArgb(52, 59, 75)
            };

            Button btnGitHub = new Button()
            {
                Text = "GitHub",
                Location = new Point(200, 500),
                Size = new Size(130, 35),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(36, 41, 46),
                ForeColor = Color.White,
                Font = new Font("Tahoma", 11, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnGitHub.FlatAppearance.BorderSize = 0;
            btnGitHub.FlatAppearance.MouseOverBackColor = Color.FromArgb(24, 28, 32);
            btnGitHub.FlatAppearance.MouseDownBackColor = Color.FromArgb(15, 18, 21);
            btnGitHub.Click += (s, args) => OpenUrl("https://github.com/ComradeBingo");

            this.Controls.AddRange(new Control[] { lblInfo, lblAndroid, lblGitHub, separator, lblSupport, btnGitHub });
        }

        private void OpenUrl(string url)
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка открытия ссылки: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public static void ShowDialog(IWin32Window owner, string version, int timeout, int concurrency)
        {
            using (var form = new AboutForm(version, timeout, concurrency))
            {
                form.ShowDialog(owner);
            }
        }
    }
}