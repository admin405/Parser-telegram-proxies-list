using System;
using System.Drawing;
using System.Windows.Forms;

namespace TelegramProxyParser.UI.Helpers
{
    public static class ProxyUICreator
    {
        public static Panel CreateNoProxiesMessage(int width)
        {
            var panel = new Panel()
            {
                Width = width - 40,
                Height = 300,
                BackColor = Color.White,
                Margin = new Padding(0, 50, 0, 0)
            };

            var messageLabel = new Label()
            {
                Text = "РАБОЧИХ ПРОКСИ НЕ НАЙДЕНО",
                Location = new Point(20, 40),
                Width = panel.Width - 40,
                Height = 40,
                Font = new Font("Tahoma", 14, FontStyle.Bold),
                ForeColor = Color.FromArgb(231, 76, 60),
                TextAlign = ContentAlignment.MiddleCenter
            };

            var hintLabel = new Label()
            {
                Text = "Попробуйте:\n" +
                       "• Выбрать другую категорию\n" +
                       "• Увеличить таймаут в настройках\n" +
                       "\n" +
                       "• Либо загрузить свой список прокси (кнопка «СВОЙ СПИСОК»)\n\n" +
                       "Текстовый документ .txt со списком вида:\n" +
                       "https://t.me/proxy?server=IP&port=ПОРТ&secret=СЕКРЕТ\n" +
                       "или\n" +
                       "tg://proxy?server=...&port=...&secret=",
                Location = new Point(20, 90),
                Width = panel.Width - 40,
                Height = 300,
                Font = new Font("Tahoma", 10),
                ForeColor = Color.FromArgb(114, 118, 125),
                TextAlign = ContentAlignment.TopLeft
            };

            panel.Controls.AddRange(new Control[] { messageLabel, hintLabel });
            return panel;
        }

        public static Panel CreateWelcomeMessage(int width, string appVersion)
        {
            var panel = new Panel()
            {
                Width = width - 40,
                Height = 520, 
                BackColor = Color.White,
                Margin = new Padding(0, 50, 0, 0) 
            };

            var titleLabel = new Label()
            {
                Text = "TELEGRAM PROXY PARSER",
                Location = new Point(20, 20),
                Width = panel.Width - 40,
                Height = 35,
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 136, 204),
                TextAlign = ContentAlignment.MiddleCenter
            };

            var versionLabel = new Label()
            {
                Text = $"Версия {appVersion}",
                Location = new Point(20, 55),
                Width = panel.Width - 40,
                Height = 20,
                Font = new Font("Segoe UI", 9, FontStyle.Regular),
                ForeColor = Color.FromArgb(127, 140, 141),
                TextAlign = ContentAlignment.MiddleCenter
            };

            var stepsLabel = new Label()
            {
                Text =
                        "\n" +
                        "\n" +
                        "\n" +
                        "Парсер MTProto прокси с интеллектуальной системой проверки.\n" +
                        "Автоматически собирает, фильтрует и тестирует сотни прокси\n" +
                        "\n" +
                        
                        "Глубокая проверка рабочих прокси из открытых списков.\n" +
                        "\n" +
                        "Возможность проверить доступность прокси из .txt файла (TCP и MTProto проверка).\n" +
                        "\n" +
                        
                        "Точность и скорость проверки регулируется в настройках.\n" +
                        "\n" +
                        "Для начала работы нажмите «Запустить MTProto проверку»\n",
                        
                Location = new Point(30, 85),
                Width = panel.Width - 60,
                Height = 340,
                Font = new Font("Segoe UI", 12, FontStyle.Regular), 
                ForeColor = Color.FromArgb(60, 60, 60),
                TextAlign = ContentAlignment.TopLeft
            };

            Button btnGitHubWelcome = new Button()
            {
                Text = "⭐ GitHub",
                Location = new Point((panel.Width - 140) / 2, 445),
                Size = new Size(140, 35),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(36, 41, 46),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnGitHubWelcome.FlatAppearance.BorderSize = 0;
            btnGitHubWelcome.FlatAppearance.MouseOverBackColor = Color.FromArgb(46, 51, 56);
            btnGitHubWelcome.FlatAppearance.MouseDownBackColor = Color.FromArgb(26, 31, 36);
            btnGitHubWelcome.Click += (s, e) =>
            {
                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "https://github.com/ComradeBingo/Proxy-telegram-windows",
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка открытия ссылки: {ex.Message}", "Ошибка",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            panel.Controls.AddRange(new Control[] { titleLabel, versionLabel, stepsLabel, btnGitHubWelcome });
            return panel;
        }
    }
}