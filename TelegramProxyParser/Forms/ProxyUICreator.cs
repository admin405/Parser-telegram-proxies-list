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
                       "\n"+
                       "• Либо загрузить свой список прокси (кнопка «СВОЙ СПИСОК»)\n\n" +
                       "Текстовый документ .txt со списком вида:\n" +
                       "https://t.me/proxy?server=IP&port=ПОРТ&secret=СЕКРЕТ\n"+
                       "или\n"+
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
                Height = 680,
                BackColor = Color.White,
                Margin = new Padding(0, 20, 0, 0)
            };

            var titleLabel = new Label()
            {
                Text = "КАК ПОЛЬЗОВАТЬСЯ:",
                Location = new Point(20, 30),
                Width = panel.Width - 40,
                Height = 40,
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                ForeColor = Color.FromArgb(52, 73, 94),
                TextAlign = ContentAlignment.MiddleCenter
            };

            var stepsLabel = new Label()
            {
                Text = "1️⃣ Кнопка «ЕВРОПА» - Маскировка трафика под Google, Amazon, Microsoft и др.\n\n" +
                       "2️⃣ Кнопка «РОССИЯ» - Маскировка трафика под Yandex, VK, Mail.ru, Gosuslugi и др.\n\n" +
                       "3️⃣ Кнопка «SurfboardV2ray» и другие - Авторские списки прокси\n\n" +
                       "4️⃣ Кнопка «СВОЙ СПИСОК» - Загрузить свой .txt файл с прокси\n\n" +
                       "5️⃣ Дождитесь проверки всех прокси\n\n" +
                       "6️⃣ Нажмите на любую рабочую прокси для открытия в Telegram\n\n" +
                       "⚙️ Кнопка «Настройки» - выберите таймаут и количество потоков",
                Location = new Point(35, 100),
                Width = panel.Width - 60,
                Height = 340,
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.FromArgb(31, 31, 31),
                TextAlign = ContentAlignment.TopLeft
            };

            Button btnGitHubWelcome = new Button()
            {
                Text = "GitHub",
                Location = new Point(panel.Width / 2 - 65, 630),
                Size = new Size(130, 35),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(36, 41, 46),
                ForeColor = Color.White,
                Font = new Font("Tahoma", 14, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnGitHubWelcome.FlatAppearance.BorderSize = 0;
            btnGitHubWelcome.FlatAppearance.MouseOverBackColor = Color.FromArgb(24, 28, 32);
            btnGitHubWelcome.FlatAppearance.MouseDownBackColor = Color.FromArgb(15, 18, 21);
            btnGitHubWelcome.Click += (s, e) =>
            {
                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "https://github.com/ComradeBingo",
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка открытия ссылки: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            panel.Controls.AddRange(new Control[] { titleLabel, stepsLabel, btnGitHubWelcome });
            return panel;
        }
    }
}