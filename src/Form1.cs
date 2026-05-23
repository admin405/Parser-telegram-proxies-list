using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
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
        private Button btnTest;
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

        // Версия приложения
        private const string APP_VERSION = "1.5";

        //Подтягиваем список проксей, любезно заготовленных комрадом kort0881
        private const string PROXY_EU_URL = "https://raw.githubusercontent.com/kort0881/telegram-proxy-collector/main/proxy_eu.txt";
        private const string PROXY_RU_URL = "https://raw.githubusercontent.com/kort0881/telegram-proxy-collector/main/proxy_ru.txt";
        private const string PROXY_TEST_URL = "https://raw.githubusercontent.com/Surfboardv2ray/TGProto/refs/heads/main/proxies-tested.txt";

        public Form1()
        {
            InitializeComponent();
            proxyParser = new ProxyParserService();
            proxyChecker = new ProxyCheckerService();
            allProxies = new List<ProxyInfo>();
            workingProxies = new List<ProxyInfo>();

            // Показываем приветственное сообщение при старте
            ShowWelcomeMessage();

            // АВТОМАТИЧЕСКАЯ ПРОВЕРКА ОБНОВЛЕНИЙ ПРИ ЗАПУСКЕ
            // Запускаем через 2 секунды после загрузки формы
            var timer = new System.Windows.Forms.Timer();
            timer.Interval = 2000;
            timer.Tick += async (s, e) =>
            {
                timer.Stop();
                await AutoCheckForUpdatesAsync();
            };
            timer.Start();
        }

        //...и шапочку
        private void InitializeComponent()
        {
            this.Text = $"Telegram Proxy Parser v{APP_VERSION}";
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
                Height = 110,
                BackColor = Color.FromArgb(0, 53, 119),
                Padding = new Padding(15, 10, 15, 10)
            };

            // Кнопка Европа (первый ряд, слева)
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

            // Кнопка Россия (первый ряд, справа от Европы)
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

            // Кнопка Surfboardv2ray (второй ряд, по центру)
            btnTest = new Button()
            {
                Text = "SurfboardV2ray",
                Location = new Point(113, 62),
                Size = new Size(150, 38),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(244, 109, 58),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnTest.FlatAppearance.BorderSize = 0;
            btnTest.FlatAppearance.MouseOverBackColor = Color.FromArgb(93, 21, 21);
            btnTest.FlatAppearance.MouseDownBackColor = Color.FromArgb(127, 58, 156);
            btnTest.Click += BtnTest_Click;

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

            // Название программы (по центру справа)
            lblProgramName = new Label()
            {
                Text = $"Парсер прокси Telegram v{APP_VERSION}",
                AutoSize = true,
                Location = new Point(390, 40),
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
            topPanel.Controls.AddRange(new Control[] { btnProxyEU, btnProxyRU, btnTest, btnAbout, lblProgramName });
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
                Height = 650,  // Увеличена высота, чтобы кнопка точно поместилась
                BackColor = Color.White,
                Margin = new Padding(0, 20, 0, 0)
            };

            // Заголовок
            var titleLabel = new Label()
            {
                Text = "КАК ПОЛЬЗОВАТЬСЯ:",
                Location = new Point(20, 30),
                Width = welcomePanel.Width - 40,
                Height = 40,
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                ForeColor = Color.FromArgb(52, 73, 94),
                TextAlign = ContentAlignment.MiddleCenter
            };

           

            
            // Пошаговая инструкция 
            var stepsLabel = new Label()
            {
                Text = "1️⃣ Кнопка «ЕВРОПА» - Маскировка трафика под Google, Amazon, Microsoft и др.\n\n" +
                       "2️⃣ Кнопка «РОССИЯ» - Маскировка трафика под Yandex, VK, Mail.ru, Gosuslugi и др.\n\n" +
                       "3️⃣ Кнопка «SurfboardV2ray» - Большой список прокси\n\n" +
                       "4️⃣ Дождитесь проверки всех прокси\n\n" +
                       "5️⃣ Нажмите на любую рабочую прокси для автоматического открытия в Telegram\n",
                Location = new Point(35, 100),
                Width = welcomePanel.Width - 60,
                Height = 200,  
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.FromArgb(31, 31, 31),
                TextAlign = ContentAlignment.TopLeft
            };

            // Кнопка GitHub в приветственном окне
            Button btnGitHubWelcome = new Button()
            {
                Text = "GitHub",
                Location = new Point(welcomePanel.Width / 2 - 65, 600),  
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

            // Добавляем все элементы
            welcomePanel.Controls.AddRange(new Control[] {
        titleLabel, stepsLabel, btnGitHubWelcome
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

        private async void BtnTest_Click(object sender, EventArgs e)
        {
            await LoadAndCheckTestProxies(PROXY_TEST_URL, "Surfboardv2ray");
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

                // Очищаем панель
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

        private async Task LoadAndCheckTestProxies(string url, string region)
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

                // Очищаем панель
                flowProxies.Controls.Clear();
                allProxies.Clear();
                workingProxies.Clear();

                lblStatus.Text = $"Загрузка прокси {region}...";

                // Загружаем содержимое файла
                var proxyLines = await proxyParser.LoadProxiesFromUrlAsync(url);

                if (proxyLines.Count == 0)
                {
                    MessageBox.Show("Прокси не найдены!", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                lblStatus.Text = $"Парсинг прокси {region} (специальный формат)...";

                // Парсим специальный формат ссылок
                allProxies = ParseSpecialProxyFormat(proxyLines);

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

        private List<ProxyInfo> ParseSpecialProxyFormat(List<string> proxyLines)
        {
            var proxies = new List<ProxyInfo>();

            foreach (var line in proxyLines)
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                try
                {
                    // Пропускаем HTML-теги и пустые строки
                    if (line.TrimStart().StartsWith("<") || line.Contains("<!DOCTYPE"))
                        continue;

                    // Ищем ссылку вида https://t.me/proxy?server=...&port=...&secret=...
                    if (line.Contains("t.me/proxy") && line.Contains("server="))
                    {
                        var proxy = ParseTelegramProxyLink(line);
                        if (proxy != null)
                        {
                            proxies.Add(proxy);
                        }
                    }
                    // Альтернативный формат: просто ссылка без лишнего текста
                    else if (line.Trim().StartsWith("https://t.me/proxy?"))
                    {
                        var proxy = ParseTelegramProxyLink(line.Trim());
                        if (proxy != null)
                        {
                            proxies.Add(proxy);
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Ошибка парсинга строки: {line}, {ex.Message}");
                }
            }

            return proxies;
        }

        private ProxyInfo ParseTelegramProxyLink(string url)
        {
            try
            {
                // Извлекаем параметры из URL
                var serverMatch = Regex.Match(url, @"server=([^&]+)");
                var portMatch = Regex.Match(url, @"port=(\d+)");
                var secretMatch = Regex.Match(url, @"secret=([^&]+)");

                if (serverMatch.Success && portMatch.Success && secretMatch.Success)
                {
                    string server = Uri.UnescapeDataString(serverMatch.Groups[1].Value);
                    string portStr = Uri.UnescapeDataString(portMatch.Groups[1].Value);
                    string secret = Uri.UnescapeDataString(secretMatch.Groups[1].Value);

                    if (int.TryParse(portStr, out int port))
                    {
                        string proxyType = DetermineProxyType(secret);

                        // Формируем ссылку в формате tg:// для Telegram
                        string tgProxyUrl = $"tg://proxy?server={Uri.EscapeDataString(server)}&port={port}&secret={Uri.EscapeDataString(secret)}";

                        return new ProxyInfo
                        {
                            Server = server,
                            Port = port,
                            Secret = secret,
                            OriginalUrl = tgProxyUrl,
                            ProxyType = proxyType
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка парсинга ссылки: {url}, {ex.Message}");
            }

            return null;
        }

        private string DetermineProxyType(string secret)
        {
            if (string.IsNullOrEmpty(secret))
                return "Unknown";

            // Простая логика определения типа на основе секрета
            if (secret.Length >= 2)
            {
                string prefix = secret.Substring(0, 2).ToLower();
                if (prefix == "ee" || prefix == "dd")
                    return "Fake TLS";
                else if (prefix == "ee" && secret.Length > 10)
                    return "Secure";
            }

            return "Classic";
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
                // СОРТИРУЕМ прокси по пингу (от меньшего к большему)
                var sortedProxies = workingProxies.OrderBy(p => p.Ping <= 0 ? 0 : p.Ping).ToList();

                foreach (var proxy in sortedProxies)
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
                                $"Classic: {workingClassic}/{totalClassic} | 📊 Сортировка по пингу";
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
            btnTest.Enabled = enabled;
            btnAbout.Enabled = enabled;

            if (enabled)
            {
                btnProxyEU.Text = "ЕВРОПА";
                btnProxyRU.Text = "РОССИЯ";
                btnTest.Text = "SurfboardV2ray";
            }
            else
            {
                btnProxyEU.Text = "⏳ ЗАГРУЗКА";
                btnProxyRU.Text = "⏳ ЗАГРУЗКА";
                btnTest.Text = "⏳ ЗАГРУЗКА";
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

        // АВТОМАТИЧЕСКАЯ ПРОВЕРКА ОБНОВЛЕНИЙ
        private async Task AutoCheckForUpdatesAsync()
        {
            try
            {
                string gitHubOwner = "ComradeBingo";
                string gitHubRepo = "Proxy-telegram-windows";
                string apiUrl = $"https://api.github.com/repos/{gitHubOwner}/{gitHubRepo}/releases/latest";

                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("User-Agent", "Telegram-Proxy-Parser-App");
                    var response = await client.GetAsync(apiUrl);

                    if (response.IsSuccessStatusCode)
                    {
                        var json = await response.Content.ReadAsStringAsync();

                        var tagMatch = Regex.Match(json, "\"tag_name\":\\s*\"([^\"]+)\"");
                        if (tagMatch.Success)
                        {
                            string latestVersion = tagMatch.Groups[1].Value;
                            if (latestVersion.StartsWith("v"))
                                latestVersion = latestVersion.Substring(1);

                            Version currentVersion = new Version(APP_VERSION);
                            Version newVersion = new Version(latestVersion);

                            if (newVersion > currentVersion)
                            {
                                DialogResult result = MessageBox.Show(
                                    $"Доступна новая версия {latestVersion}!\n\nТекущая версия: {currentVersion}\n\nПерейти на страницу загрузки?",
                                    "Доступно обновление",
                                    MessageBoxButtons.YesNo,
                                    MessageBoxIcon.Information);

                                if (result == DialogResult.Yes)
                                {
                                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                                    {
                                        FileName = $"https://github.com/{gitHubOwner}/{gitHubRepo}/releases/latest",
                                        UseShellExecute = true
                                    });
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка автоматической проверки обновлений: {ex.Message}");
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
                Text = $"Telegram Proxy Parser v{APP_VERSION}\n\n" +
                       "Парсер и проверка прокси для Telegram\n\n" +
                       "Источники:\n" +
                       "• Европа: Маскировка под Google, Amazon, Microsoft и др.\n" +
                       "• Россия: Маскировка под Yandex, VK, Mail.ru, Gosuslugi и др.\n" +
                       "• SurfboardV2ray: Большой список прокси\n\n" +
                       "• Списки обновляются каждый час.\n" +
                       "• Доступность сервера не гарантирует его работоспособность!\n" +
                       "• Они не дремлют... Но и мы не спим!\n\n",
                Location = new Point(25, 20),
                Size = new Size(430, 280),
                Font = new Font("Tahoma", 10),
                TextAlign = ContentAlignment.TopLeft
            };

            // Ссылка на Android версию
            LinkLabel lblAndroid = new LinkLabel()
            {
                Text = "Скачать версию для Android",
                Location = new Point(100, 300),
                Size = new Size(280, 30),
                Font = new Font("Tahoma", 11, FontStyle.Bold | FontStyle.Underline),
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
                Location = new Point(100, 330),
                Size = new Size(280, 30),
                Font = new Font("Tahoma", 11, FontStyle.Bold | FontStyle.Underline),
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
                Location = new Point(25, 400),
                Size = new Size(430, 1),
                BackColor = Color.FromArgb(224, 227, 234)
            };

            // Надпись "Поддержать проект"
            Label lblSupport = new Label()
            {
                Text = "Поддержать проект",
                Location = new Point(25, 410),
                Size = new Size(430, 30),
                Font = new Font("Tahoma", 11, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.FromArgb(52, 59, 75)
            };

            // Кнопка GitHub В ОКНЕ СПРАВКИ
            Button btnGitHub = new Button()
            {
                Text = "GitHub",
                Location = new Point(175, 450),
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
            btnGitHub.Click += (s, args) =>
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

            // Добавляем все элементы на форму
            aboutForm.Controls.AddRange(new Control[] { lblInfo, lblAndroid, lblGitHub, separator, lblSupport, btnGitHub });

            // Показываем форму
            aboutForm.ShowDialog();
        }
    }
}