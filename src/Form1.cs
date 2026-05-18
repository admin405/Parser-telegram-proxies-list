using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using TelegramProxyParser.Models;
using TelegramProxyParser.Services;

namespace TelegramProxyParser
{
    public partial class Form1 : Form
    {
        private Panel topPanel;
        private Button btnProxyEU;
        private Button btnProxyRU;
        private Button btnAbout;
        private Label lblProgramName;
        private FlowLayoutPanel flowProxies;
        private Label lblStatus;
        private Panel loadingPanel;
        private Label lblLoadingProgress;
        private ProgressBar progressBar;

        private List<ProxyInfo> allProxies;
        private List<ProxyInfo> workingProxies;
        private ProxyParserService proxyParser;
        private ProxyCheckerService proxyChecker;
        private CancellationTokenSource cts;

        //Подтягиваем список проксей, любезно заготовленных комрадом kort0881
        private const string PROXY_EU_URL = "https://raw.githubusercontent.com/kort0881/telegram-proxy-collector/main/proxy_eu.txt";
        private const string PROXY_RU_URL = "https://raw.githubusercontent.com/kort0881/telegram-proxy-collector/main/proxy_ru.txt";

        public Form1()
        {
            InitializeComponent();
            proxyParser = new ProxyParserService();
            proxyChecker = new ProxyCheckerService();
            allProxies = new List<ProxyInfo>();
            workingProxies = new List<ProxyInfo>();

            // Показываем приветственное сообщение при старте
            ShowWelcomeMessage();
        }

        //...и шапочку
        private void InitializeComponent()
        {
            this.Text = "Telegram Proxy Parser v1.4";
            this.Size = new Size(750, 900);
            this.MinimumSize = new Size(750, 900);
            this.MaximumSize = new Size(750, 900);
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(0, 153, 119);

            // Верхняя панель с градиентом
            topPanel = new Panel()
            {
                Dock = DockStyle.Top,
                Height = 70,
                BackColor = Color.FromArgb(0, 53, 119),
                Padding = new Padding(15, 10, 15, 10)
            };



            // Кнопка Европа
            btnProxyEU = new Button()
            {
                Text = "ЕВРОПА",
                Location = new Point(15, 17),
                Size = new Size(110, 38),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(46, 204, 113),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnProxyEU.FlatAppearance.BorderSize = 0;
            btnProxyEU.FlatAppearance.MouseOverBackColor = Color.FromArgb(39, 174, 96);
            btnProxyEU.FlatAppearance.MouseDownBackColor = Color.FromArgb(33, 148, 82);
            btnProxyEU.Click += BtnProxyEU_Click;

            // Кнопка Россия
            btnProxyRU = new Button()
            {
                Text = "РОССИЯ",
                Location = new Point(135, 17),
                Size = new Size(110, 38),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(52, 152, 219),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnProxyRU.FlatAppearance.BorderSize = 0;
            btnProxyRU.FlatAppearance.MouseOverBackColor = Color.FromArgb(41, 128, 185);
            btnProxyRU.FlatAppearance.MouseDownBackColor = Color.FromArgb(31, 97, 141);
            btnProxyRU.Click += BtnProxyRU_Click;

            // Кнопка "О программе"
            btnAbout = new Button()
            {
                Text = "СПРАВКА",
                Location = new Point(255, 17),
                Size = new Size(110, 38),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(149, 165, 166),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnAbout.FlatAppearance.BorderSize = 0;
            btnAbout.FlatAppearance.MouseOverBackColor = Color.FromArgb(127, 140, 141);
            btnAbout.FlatAppearance.MouseDownBackColor = Color.FromArgb(108, 122, 122);
            btnAbout.Click += BtnAbout_Click;

            // Название программы
            lblProgramName = new Label()
            {
                Text = "Парсер прокси Telegram v1.4",
                AutoSize = true,
                Location = new Point(390, 22),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleRight
            };

            // Панель для прокси с закругленными углами
            flowProxies = new FlowLayoutPanel()
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(15),
                BackColor = Color.FromArgb(245, 247, 250),
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false
            };

            // Панель загрузки
            loadingPanel = new Panel()
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(245, 247, 250),
                Visible = false
            };

            progressBar = new ProgressBar()
            {
                Style = ProgressBarStyle.Marquee,
                Size = new Size(350, 6),
                MarqueeAnimationSpeed = 20,
                BackColor = Color.FromArgb(224, 227, 234),
                ForeColor = Color.FromArgb(88, 101, 242)
            };

            lblLoadingProgress = new Label()
            {
                Size = new Size(400, 40),
                Font = new Font("Tahoma", 13, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.FromArgb(52, 59, 75)
            };

            loadingPanel.Controls.Add(progressBar);
            loadingPanel.Controls.Add(lblLoadingProgress);

            // Панель статуса
            var statusPanel = new Panel()
            {
                Dock = DockStyle.Bottom,
                Height = 40,
                BackColor = Color.FromArgb(255, 255, 255),
                Padding = new Padding(10)
            };

            // Добавляем тень сверху панели статуса
            statusPanel.Paint += (sender, e) =>
            {
                using (var pen = new Pen(Color.FromArgb(224, 227, 234), 1))
                {
                    e.Graphics.DrawLine(pen, 0, 0, statusPanel.Width, 0);
                }
            };

            lblStatus = new Label()
            {
                Location = new Point(10, 10),
                Size = new Size(710, 25),
                Font = new Font("Tahoma", 9, FontStyle.Regular),
                ForeColor = Color.FromArgb(114, 118, 125),
                Text = "Готов к работе"
            };

            statusPanel.Controls.Add(lblStatus);
            topPanel.Controls.AddRange(new Control[] { btnProxyEU, btnProxyRU, btnAbout, lblProgramName });
            this.Controls.AddRange(new Control[] { flowProxies, loadingPanel, statusPanel, topPanel });
        }

        // Показываем приветственное сообщение
        private void ShowWelcomeMessage()
        {
            // Очищаем панель
            flowProxies.Controls.Clear();

            // Создаем панель с информацией
            var welcomePanel = new Panel()
            {
                Width = flowProxies.Width - 40,
                Height = 400,
                BackColor = Color.White,
                Margin = new Padding(0, 20, 0, 0)
            };

            

            // Заголовок
            var titleLabel = new Label()
            {
                Text = "ДОБРО ПОЖАЛОВАТЬ!",
                Location = new Point(20, 30),
                Width = welcomePanel.Width - 40,
                Height = 40,
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                ForeColor = Color.FromArgb(52, 73, 94),
                TextAlign = ContentAlignment.MiddleCenter
            };

 

            // Описание
            var descLabel = new Label()
            {
                Text = "Telegram Proxy Parser v1.4\n\n" +
                       "Программа для парсинга и проверки прокси для Telegram\n",
                Location = new Point(20, 80),
                Width = welcomePanel.Width - 40,
                Height = 90,
                Font = new Font("Segoe UI", 11),
                ForeColor = Color.FromArgb(85, 85, 85),
                TextAlign = ContentAlignment.MiddleCenter
            };

            // Инструкция
            var instructionLabel = new Label()
            {
                Text = "КАК ПОЛЬЗОВАТЬСЯ:",
                Location = new Point(20, 170),
                Width = welcomePanel.Width - 40,
                Height = 30,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Color.FromArgb(52, 152, 219),
                TextAlign = ContentAlignment.MiddleCenter
            };

            // Шаги
            var stepsLabel = new Label()
            {
                Text = "1️⃣ Кнопка «ЕВРОПА» - Маскировка трафика под Google, Amazon, Microsoft и др.\n\n" +
                       "2️⃣ Кнопка «РОССИЯ» - Маскировка трафика под Yandex, VK, Mail.ru, Gosuslugi и др.\n\n" +
                       "3️⃣ Дождитесь проверки всех прокси\n\n" +
                       "4️⃣ Нажмите на любую рабочую прокси для автоматического открытия в Telegram\n",
                Location = new Point(30, 220),
                Width = welcomePanel.Width - 60,
                Height = 250,
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.FromArgb(31, 31, 31),
                TextAlign = ContentAlignment.TopLeft
            };

            // Добавляем все элементы
            welcomePanel.Controls.AddRange(new Control[] {
                titleLabel, descLabel, instructionLabel, stepsLabel
            });

            flowProxies.Controls.Add(welcomePanel);
        }

        private async void BtnProxyEU_Click(object sender, EventArgs e)
        {
            await LoadAndCheckProxies(PROXY_EU_URL, "ЕВРОПА");
        }

        private async void BtnProxyRU_Click(object sender, EventArgs e)
        {
            await LoadAndCheckProxies(PROXY_RU_URL, "РОССИЯ");
        }

        private async Task LoadAndCheckProxies(string url, string region)
        {
            try
            {
                if (cts != null)
                {
                    cts.Cancel();
                    cts.Dispose();
                }
                cts = new CancellationTokenSource();

                SetControlsEnabled(false);
                ShowLoading(true, "Загрузка списка прокси...");

                // Очищаем панель (приветствие исчезнет здесь)
                flowProxies.Controls.Clear();
                allProxies.Clear();
                workingProxies.Clear();

                lblStatus.Text = $"Загрузка прокси {region}...";
                var proxyUrls = await proxyParser.LoadProxiesFromUrlAsync(url);

                if (proxyUrls.Count == 0)
                {
                    MessageBox.Show("Прокси не найдены!", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                lblStatus.Text = $"Парсинг прокси {region}...";
                allProxies = proxyParser.ParseProxyUrls(proxyUrls);

                if (allProxies.Count == 0)
                {
                    MessageBox.Show("Не удалось распарсить прокси!", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                await CheckAllProxies(region);
                ShowResult();
            }
            catch (OperationCanceledException)
            {
                lblStatus.Text = "Операция отменена";
                ShowLoading(false);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                lblStatus.Text = $"Ошибка: {ex.Message}";
                ShowLoading(false);
            }
            finally
            {
                SetControlsEnabled(true);
            }
        }

        private async Task CheckAllProxies(string region)
        {
            int total = allProxies.Count;
            int completed = 0;
            workingProxies = new List<ProxyInfo>();

            ShowLoading(true, $"Проверка прокси: 0/{total}");

            for (int i = 0; i < total; i++)
            {
                if (cts.Token.IsCancellationRequested)
                    break;

                var proxy = allProxies[i];

                var result = await Task.Run(() => proxyChecker.CheckProxySync(proxy.Server, proxy.Port, proxy.Secret));

                proxy.IsWorking = result.IsWorking;
                proxy.ProxyType = result.ProxyType;
                proxy.Ping = result.ResponseTime;
                proxy.ErrorMessage = result.ErrorMessage;

                if (proxy.IsWorking)
                {
                    workingProxies.Add(proxy);
                }

                completed++;

                ShowLoading(true, $"Проверка прокси: {completed}/{total}");
                lblStatus.Text = $"Проверка {region}: {completed}/{total} | Найдено рабочих: {workingProxies.Count}";

                await Task.Delay(10);
            }
        }

        private void ShowResult()
        {
            if (InvokeRequired)
            {
                Invoke(new Action(ShowResult));
                return;
            }

            loadingPanel.Visible = false;
            flowProxies.Visible = true;

            flowProxies.Controls.Clear();
            flowProxies.BringToFront();

            if (workingProxies.Count == 0)
            {
                var noProxiesPanel = CreateNoProxiesMessage();
                flowProxies.Controls.Add(noProxiesPanel);

                int fakeTlsCount = allProxies.Count(p => p.ProxyType == "Fake TLS");
                int secureCount = allProxies.Count(p => p.ProxyType == "Secure");
                int classicCount = allProxies.Count(p => p.ProxyType == "Classic");

                lblStatus.Text = $"Завершено | Всего: {allProxies.Count} | Рабочих: 0 | Fake TLS: {fakeTlsCount} | Secure: {secureCount} | Classic: {classicCount}";
                flowProxies.Refresh();
            }
            else
            {
                foreach (var proxy in workingProxies)
                {
                    CreateProxyControl(proxy);
                }

                int workingFakeTls = workingProxies.Count(p => p.ProxyType == "Fake TLS");
                int workingSecure = workingProxies.Count(p => p.ProxyType == "Secure");
                int workingClassic = workingProxies.Count(p => p.ProxyType == "Classic");

                int totalFakeTls = allProxies.Count(p => p.ProxyType == "Fake TLS");
                int totalSecure = allProxies.Count(p => p.ProxyType == "Secure");
                int totalClassic = allProxies.Count(p => p.ProxyType == "Classic");

                lblStatus.Text = $"Завершено | Всего: {allProxies.Count} | Рабочих: {workingProxies.Count} | " +
                                $"Fake TLS: {workingFakeTls}/{totalFakeTls} | " +
                                $"Secure: {workingSecure}/{totalSecure} | " +
                                $"Classic: {workingClassic}/{totalClassic}";
            }
        }

        private Panel CreateNoProxiesMessage()
        {
            var panel = new Panel()
            {
                Width = flowProxies.Width - 40,
                Height = 200,
                BackColor = Color.White,
                Margin = new Padding(0, 50, 0, 0)
            };

            var messageLabel = new Label()
            {
                Text = "РАБОЧИХ ПРОКСИ НЕ НАЙДЕНО",
                Location = new Point(20, 60),
                Width = panel.Width - 40,
                Height = 40,
                Font = new Font("Tahoma", 14, FontStyle.Bold),
                ForeColor = Color.FromArgb(231, 76, 60),
                TextAlign = ContentAlignment.MiddleCenter
            };

            var hintLabel = new Label()
            {
                Text = "Попробуйте выбрать другую категорию",
                Location = new Point(20, 110),
                Width = panel.Width - 40,
                Height = 30,
                Font = new Font("Tahoma", 11, FontStyle.Regular),
                ForeColor = Color.FromArgb(114, 118, 125),
                TextAlign = ContentAlignment.MiddleCenter
            };

            panel.Controls.AddRange(new Control[] { messageLabel, hintLabel });
            return panel;
        }

        private void CreateProxyControl(ProxyInfo proxy)
        {
            var proxyPanel = new Panel()
            {
                Width = flowProxies.Width - 35,
                Height = 55,
                Margin = new Padding(0, 0, 0, 10),
                BackColor = Color.White,
                Padding = new Padding(0)
            };

            var btnProxy = new Button()
            {
                Location = new Point(0, 0),
                Width = proxyPanel.Width - 115,
                Height = 55,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(0, 136, 204),
                ForeColor = Color.White,
                Font = new Font("Tahoma", 10, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                Text = $"{proxy.Server}:{proxy.Port}   |   {proxy.ProxyType}",
                Tag = proxy.OriginalUrl,
                Cursor = Cursors.Hand
            };
            btnProxy.FlatAppearance.BorderSize = 0;
            btnProxy.FlatAppearance.MouseOverBackColor = Color.FromArgb(114, 137, 218);
            btnProxy.FlatAppearance.MouseDownBackColor = Color.FromArgb(68, 78, 188);
            btnProxy.Click += (s, e) => OpenProxy(proxy.OriginalUrl);

            var pingPanel = new Panel()
            {
                Location = new Point(proxyPanel.Width - 115, 0),
                Size = new Size(115, 55),
                BackColor = Color.White
            };

            var lblPingValue = new Label()
            {
                Text = GetPingText(proxy),
                Location = new Point(0, 0),
                Size = new Size(115, 55),
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = GetPingColor(proxy),
                ForeColor = Color.Black,
                AutoSize = false
            };

            pingPanel.Controls.Add(lblPingValue);
            proxyPanel.Controls.AddRange(new Control[] { btnProxy, pingPanel });
            flowProxies.Controls.Add(proxyPanel);
        }

        private string GetPingText(ProxyInfo proxy)
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

        private Color GetPingColor(ProxyInfo proxy)
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

        private void ShowLoading(bool show, string message = null)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => ShowLoading(show, message)));
                return;
            }

            loadingPanel.Visible = show;
            flowProxies.Visible = !show;

            if (show)
            {
                loadingPanel.BringToFront();
                if (message != null)
                {
                    lblLoadingProgress.Text = message;

                    progressBar.Location = new Point(loadingPanel.Width / 2 - progressBar.Width / 2,
                                                    loadingPanel.Height / 2 - 30);
                    lblLoadingProgress.Location = new Point(loadingPanel.Width / 2 - lblLoadingProgress.Width / 2,
                                                           loadingPanel.Height / 2 + 10);
                }
            }
            else
            {
                flowProxies.BringToFront();
                flowProxies.Refresh();
            }
        }

        private void SetControlsEnabled(bool enabled)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => SetControlsEnabled(enabled)));
                return;
            }

            btnProxyEU.Enabled = enabled;
            btnProxyRU.Enabled = enabled;
            btnAbout.Enabled = enabled;

            if (enabled)
            {
                btnProxyEU.Text = "ЕВРОПА";
                btnProxyRU.Text = "РОССИЯ";
            }
            else
            {
                btnProxyEU.Text = "⏳ ЗАГРУЗКА";
                btnProxyRU.Text = "⏳ ЗАГРУЗКА";
            }
        }

        private void OpenProxy(string proxyUrl)
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = proxyUrl,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка открытия: {ex.Message}\n\nПроверьте, установлен ли Telegram.",
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnAbout_Click(object sender, EventArgs e)
        {
            // Создаем форму для отображения информации и кнопок доната
            Form aboutForm = new Form();
            aboutForm.Text = "О программе";
            aboutForm.Size = new Size(480, 550);
            aboutForm.StartPosition = FormStartPosition.CenterParent;
            aboutForm.FormBorderStyle = FormBorderStyle.FixedDialog;
            aboutForm.MaximizeBox = false;
            aboutForm.MinimizeBox = false;
            aboutForm.BackColor = Color.White;

            // Информационная часть
            Label lblInfo = new Label()
            {
                Text = "Telegram Proxy Parser v1.4\n\n" +
                       "Парсер и проверка прокси для Telegram\n\n" +
                       "Источники:\n" +
                       "• Европа: Маскировка под Google, Amazon, Microsoft и др.\n" +
                       "• Россия: Маскировка под Yandex, VK, Mail.ru, Gosuslugi и др.\n\n" +
                       "• Списки обновляются каждый час.\n" +
                       "• Доступность сервера не гарантирует его работоспособность!\n" +
                       "• Они не дремлют... Но и мы не спим!\n\n" +
                       "© by Comrade Bingo",
                Location = new Point(25, 20),
                Size = new Size(430, 300),
                Font = new Font("Tahoma", 10),
                TextAlign = ContentAlignment.TopLeft
            };

            // Ссылка на Android версию
            LinkLabel lblAndroid = new LinkLabel()
            {
                Text = "Скачать версию для Android",
                Location = new Point(100, 330),
                Size = new Size(280, 25),
                Font = new Font("Tahoma", 11, FontStyle.Underline),
                TextAlign = ContentAlignment.MiddleCenter,
                LinkColor = Color.FromArgb(46, 204, 113),
                ActiveLinkColor = Color.FromArgb(39, 174, 96)
            };
            lblAndroid.LinkClicked += (s, args) =>
            {
                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "https://github.com/ComradeBingo/Proxy-Telegram-Android",
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка открытия ссылки: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            // Ссылка на GitHub win-версии
            LinkLabel lblGitHub = new LinkLabel()
            {
                Text = "GitHub (Windows версия)",
                Location = new Point(140, 365),
                Size = new Size(200, 25),
                Font = new Font("Tahoma", 11, FontStyle.Underline),
                TextAlign = ContentAlignment.MiddleCenter,
                LinkColor = Color.FromArgb(88, 101, 242),
                ActiveLinkColor = Color.FromArgb(68, 78, 188)
            };
            lblGitHub.LinkClicked += (s, args) =>
            {
                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "https://github.com/ComradeBingo/Proxy-telegram-windows/",
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка открытия ссылки: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            // Разделительная линия
            Panel separator = new Panel()
            {
                Location = new Point(25, 405),
                Size = new Size(430, 1),
                BackColor = Color.FromArgb(224, 227, 234)
            };

            // Надпись "Поддержать проект"
            Label lblSupport = new Label()
            {
                Text = "Поддержать проект",
                Location = new Point(25, 420),
                Size = new Size(430, 30),
                Font = new Font("Tahoma", 11, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.FromArgb(52, 59, 75)
            };

            // Кнопка Boosty
            Button btnBoosty = new Button()
            {
                Text = "Boosty",
                Location = new Point(115, 460),
                Size = new Size(110, 35),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(241, 196, 15),
                ForeColor = Color.White,
                Font = new Font("Tahoma", 11, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnBoosty.FlatAppearance.BorderSize = 0;
            btnBoosty.FlatAppearance.MouseOverBackColor = Color.FromArgb(243, 156, 18);
            btnBoosty.FlatAppearance.MouseDownBackColor = Color.FromArgb(211, 84, 0);
            btnBoosty.Click += (s, args) =>
            {
                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "https://boosty.to/comradebingo/donate",
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка открытия ссылки: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            // Кнопка YooMoney 
            Button btnYooMoney = new Button()
            {
                Text = "YooMoney",
                Location = new Point(250, 460),
                Size = new Size(110, 35),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(46, 204, 113),
                ForeColor = Color.White,
                Font = new Font("Tahoma", 11, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnYooMoney.FlatAppearance.BorderSize = 0;
            btnYooMoney.FlatAppearance.MouseOverBackColor = Color.FromArgb(39, 174, 96);
            btnYooMoney.FlatAppearance.MouseDownBackColor = Color.FromArgb(33, 148, 82);
            btnYooMoney.Click += (s, args) =>
            {
                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "https://yoomoney.ru/to/410011017939948",
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка открытия ссылки: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            // Добавляем все элементы на форму
            aboutForm.Controls.AddRange(new Control[] { lblInfo, lblAndroid, lblGitHub, separator, lblSupport, btnBoosty, btnYooMoney });

            // Показываем форму
            aboutForm.ShowDialog();
        }
    }
}