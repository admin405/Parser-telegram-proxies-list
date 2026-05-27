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
        private Button btnSettings;
        private Button btnLoadCustom;
        private Label lblProgramName;
        private FlowLayoutPanel flowProxies;
        private Label lblStatus;
        private Panel loadingPanel;
        private Label lblLoadingProgress;
        private ProgressBar progressBar;

        // Текущие настройки
        private int currentTimeout = 300;
        private int currentConcurrency = 5;

        private List<ProxyInfo> allProxies;
        private List<ProxyInfo> workingProxies;
        private ProxyParserService proxyParser;
        private ProxyCheckerService proxyChecker;
        private CancellationTokenSource cts;

        private const string APP_VERSION = "1.7";
        private const string PROXY_EU_URL = "https://raw.githubusercontent.com/kort0881/telegram-proxy-collector/main/proxy_eu.txt";
        private const string PROXY_RU_URL = "https://raw.githubusercontent.com/kort0881/telegram-proxy-collector/main/proxy_ru.txt";
        private const string PROXY_TEST_URL = "https://raw.githubusercontent.com/Surfboardv2ray/TGProto/refs/heads/main/proxies-tested.txt";

        public Form1()
        {
            InitializeComponent();
            proxyParser = new ProxyParserService();
            proxyChecker = new ProxyCheckerService();
            proxyChecker.SetTimeout(currentTimeout);
            allProxies = new List<ProxyInfo>();
            workingProxies = new List<ProxyInfo>();
            cts = null;

            ShowWelcomeMessage();

            var timer = new System.Windows.Forms.Timer();
            timer.Interval = 1000;
            timer.Tick += async (s, e) =>
            {
                timer.Stop();
                await AutoCheckForUpdatesAsync();
            };
            timer.Start();
        }

        private void InitializeComponent()
        {
            this.Text = $"Telegram Proxy Parser v{APP_VERSION}";
            this.Size = new Size(750, 900);
            this.MinimumSize = new Size(750, 900);
            this.MaximumSize = new Size(750, 900);
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(0, 153, 119);

            topPanel = new Panel()
            {
                Dock = DockStyle.Top,
                Height = 110,
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

            // Кнопка SurfboardV2ray
            btnTest = new Button()
            {
                Text = "SurfboardV2ray",
                Location = new Point(255, 17),
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

            // Кнопка Справка
            btnAbout = new Button()
            {
                Text = "СПРАВКА",
                Location = new Point(580, 62),
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

            // Кнопка загрузки своего списка
            btnLoadCustom = new Button()
            {
                Text = "📁 СВОЙ СПИСОК",
                Location = new Point(115, 62),
                Size = new Size(150, 38),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(155, 89, 182),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnLoadCustom.FlatAppearance.BorderSize = 0;
            btnLoadCustom.FlatAppearance.MouseOverBackColor = Color.FromArgb(142, 68, 173);
            btnLoadCustom.FlatAppearance.MouseDownBackColor = Color.FromArgb(128, 58, 156);
            btnLoadCustom.Click += BtnLoadCustom_Click;

            // Название программы
            lblProgramName = new Label()
            {
                Text = $"Парсер прокси Telegram v{APP_VERSION}",
                AutoSize = true,
                Location = new Point(415, 25),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleRight
            };

            // Кнопка Настройки
            btnSettings = new Button()
            {
                Text = "⚙️ Настройки",
                Location = new Point(420, 62),
                Size = new Size(140, 38),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(52, 73, 94),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnSettings.FlatAppearance.BorderSize = 0;
            btnSettings.FlatAppearance.MouseOverBackColor = Color.FromArgb(44, 62, 80);
            btnSettings.FlatAppearance.MouseDownBackColor = Color.FromArgb(36, 51, 66);
            btnSettings.Click += BtnSettings_Click;

            flowProxies = new FlowLayoutPanel()
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(15),
                BackColor = Color.FromArgb(245, 247, 250),
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false
            };

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

            var statusPanel = new Panel()
            {
                Dock = DockStyle.Bottom,
                Height = 40,
                BackColor = Color.FromArgb(255, 255, 255),
                Padding = new Padding(10)
            };

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
            topPanel.Controls.AddRange(new Control[] { btnProxyEU, btnProxyRU, btnTest, btnAbout, btnLoadCustom, btnSettings, lblProgramName });
            this.Controls.AddRange(new Control[] { flowProxies, loadingPanel, statusPanel, topPanel });
        }

        private void BtnSettings_Click(object sender, EventArgs e)
        {
            Form settingsForm = new Form();
            settingsForm.Text = "Настройки";
            settingsForm.Size = new Size(420, 350);
            settingsForm.StartPosition = FormStartPosition.CenterParent;
            settingsForm.FormBorderStyle = FormBorderStyle.FixedDialog;
            settingsForm.MaximizeBox = false;
            settingsForm.MinimizeBox = false;
            settingsForm.BackColor = Color.White;

            Label lblTimeout = new Label()
            {
                Text = "Таймаут проверки (мс):",
                Location = new Point(25, 25),
                Size = new Size(170, 25),
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.FromArgb(52, 59, 75)
            };

            ComboBox cmbTimeout = new ComboBox()
            {
                Location = new Point(210, 23),
                Size = new Size(120, 25),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 10)
            };
            cmbTimeout.Items.AddRange(new object[] { "300", "500", "750", "1000" });
            cmbTimeout.SelectedItem = currentTimeout.ToString();

            Label lblConcurrency = new Label()
            {
                Text = "Параллельных потоков:",
                Location = new Point(25, 70),
                Size = new Size(170, 25),
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.FromArgb(52, 59, 75)
            };

            ComboBox cmbConcurrency = new ComboBox()
            {
                Location = new Point(210, 68),
                Size = new Size(120, 25),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 10)
            };
            cmbConcurrency.Items.AddRange(new object[] { "5", "10", "15", "20" });
            cmbConcurrency.SelectedItem = currentConcurrency.ToString();

            Label lblInfo = new Label()
            {
                Text = "⚠️ Примечание:\n\n• Меньший таймаут = быстрее проверка,\n  но меньше рабочих прокси\n\n• Больше потоков = быстрее проверка,\n  но выше нагрузка на сеть",
                Location = new Point(25, 115),
                Size = new Size(360, 120),
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(114, 118, 125)
            };

            Button btnSave = new Button()
            {
                Text = "Сохранить",
                Location = new Point(95, 260),
                Size = new Size(100, 35),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(46, 204, 113),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += (s, ev) =>
            {
                currentTimeout = int.Parse(cmbTimeout.SelectedItem.ToString());
                currentConcurrency = int.Parse(cmbConcurrency.SelectedItem.ToString());
                proxyChecker.SetTimeout(currentTimeout);
                lblStatus.Text = $"Настройки сохранены: таймаут {currentTimeout} мс, потоков: {currentConcurrency}";
                settingsForm.Close();
            };

            Button btnCancel = new Button()
            {
                Text = "Отмена",
                Location = new Point(215, 260),
                Size = new Size(100, 35),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(149, 165, 166),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.Click += (s, ev) => settingsForm.Close();

            settingsForm.Controls.AddRange(new Control[] { lblTimeout, cmbTimeout, lblConcurrency, cmbConcurrency, lblInfo, btnSave, btnCancel });
            settingsForm.ShowDialog();
        }

        private async void BtnLoadCustom_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Title = "Выберите файл со списком прокси";
                openFileDialog.Filter = "Текстовые файлы (*.txt)|*.txt|Все файлы (*.*)|*.*";
                openFileDialog.FilterIndex = 1;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        string[] lines = System.IO.File.ReadAllLines(openFileDialog.FileName);
                        List<string> proxyLines = new List<string>();

                        foreach (string line in lines)
                        {
                            string trimmedLine = line.Trim();
                            if (!string.IsNullOrWhiteSpace(trimmedLine) && !trimmedLine.StartsWith("//") && !trimmedLine.StartsWith("#"))
                            {
                                proxyLines.Add(trimmedLine);
                            }
                        }

                        if (proxyLines.Count == 0)
                        {
                            MessageBox.Show("Файл не содержит ссылок на прокси!", "Информация",
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
                            return;
                        }

                        await LoadCustomProxies(proxyLines, System.IO.Path.GetFileName(openFileDialog.FileName));
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка чтения файла: {ex.Message}", "Ошибка",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private async Task LoadCustomProxies(List<string> proxyLines, string fileName)
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

                flowProxies.Controls.Clear();
                allProxies.Clear();
                workingProxies.Clear();

                lblStatus.Text = $"Загрузка прокси из файла: {fileName}...";

                allProxies = ParseSpecialProxyFormat(proxyLines);

                if (allProxies.Count == 0)
                {
                    MessageBox.Show("Не удалось распарсить прокси!\n\n" +
                        "Убедитесь, что файл содержит ссылки в формате:\n" +
                        "https://t.me/proxy?server=IP&port=ПОРТ&secret=СЕКРЕТ",
                        "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                await CheckAllProxies(fileName);
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

        private void ShowWelcomeMessage()
        {
            flowProxies.Controls.Clear();

            var welcomePanel = new Panel()
            {
                Width = flowProxies.Width - 40,
                Height = 680,
                BackColor = Color.White,
                Margin = new Padding(0, 20, 0, 0)
            };

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

            var stepsLabel = new Label()
            {
                Text = "1️⃣ Кнопка «ЕВРОПА» - Маскировка трафика под Google, Amazon, Microsoft и др.\n\n" +
                       "2️⃣ Кнопка «РОССИЯ» - Маскировка трафика под Yandex, VK, Mail.ru, Gosuslugi и др.\n\n" +
                       "3️⃣ Кнопка «SurfboardV2ray» - Большой список прокси\n\n" +
                       "4️⃣ Кнопка «СВОЙ СПИСОК» - Загрузить свой .txt файл с прокси\n\n" +
                       "5️⃣ Дождитесь проверки всех прокси\n\n" +
                       "6️⃣ Нажмите на любую рабочую прокси для открытия в Telegram\n\n" +
                       "⚙️ Кнопка «Настройки» - выберите таймаут и количество потоков",
                Location = new Point(35, 100),
                Width = welcomePanel.Width - 60,
                Height = 340,
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.FromArgb(31, 31, 31),
                TextAlign = ContentAlignment.TopLeft
            };

            Button btnGitHubWelcome = new Button()
            {
                Text = "GitHub",
                Location = new Point(welcomePanel.Width / 2 - 65, 630),
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

            welcomePanel.Controls.AddRange(new Control[] { titleLabel, stepsLabel, btnGitHubWelcome });
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

                flowProxies.Controls.Clear();
                allProxies.Clear();
                workingProxies.Clear();

                lblStatus.Text = $"Загрузка прокси {region}...";
                var proxyLines = await proxyParser.LoadProxiesFromUrlAsync(url);

                if (proxyLines.Count == 0)
                {
                    MessageBox.Show("Прокси не найдены!", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                lblStatus.Text = $"Парсинг прокси {region} (специальный формат)...";
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
                    if (line.TrimStart().StartsWith("<") || line.Contains("<!DOCTYPE"))
                        continue;

                    if (line.Contains("t.me/proxy") && line.Contains("server="))
                    {
                        var proxy = ParseTelegramProxyLink(line);
                        if (proxy != null)
                            proxies.Add(proxy);
                    }
                    else if (line.Trim().StartsWith("https://t.me/proxy?"))
                    {
                        var proxy = ParseTelegramProxyLink(line.Trim());
                        if (proxy != null)
                            proxies.Add(proxy);
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

            if (secret.Length >= 2)
            {
                string prefix = secret.Substring(0, 2).ToLower();
                if (prefix == "ee" || prefix == "dd")
                    return "Fake TLS";
            }

            return "Classic";
        }

        private async Task CheckAllProxies(string region)
        {
            int total = allProxies.Count;
            int completed = 0;
            int failedConsecutive = 0;
            workingProxies = new List<ProxyInfo>();

            int concurrency = currentConcurrency;
            int maxConcurrency = currentConcurrency;
            int minConcurrency = Math.Max(1, currentConcurrency / 2);

            using (var semaphore = new SemaphoreSlim(concurrency))
            {
                var tasks = new List<Task>();
                var progressUpdateInterval = TimeSpan.FromMilliseconds(100);
                var lastProgressUpdate = DateTime.MinValue;

                foreach (var proxy in allProxies)
                {
                    if (cts.Token.IsCancellationRequested)
                        break;

                    await semaphore.WaitAsync(cts.Token);

                    tasks.Add(Task.Run(async () =>
                    {
                        try
                        {
                            var result = await proxyChecker.CheckProxyWithTimeoutAsync(
                                proxy.Server, proxy.Port, proxy.Secret, currentTimeout);

                            proxy.IsWorking = result.IsWorking;
                            proxy.ProxyType = result.ProxyType;
                            proxy.Ping = result.ResponseTime;
                            proxy.ErrorMessage = result.ErrorMessage;

                            if (proxy.IsWorking)
                            {
                                lock (workingProxies)
                                {
                                    workingProxies.Add(proxy);
                                    failedConsecutive = 0;
                                }

                                if (workingProxies.Count % 10 == 0 && concurrency < maxConcurrency)
                                {
                                    concurrency = Math.Min(maxConcurrency, concurrency + 5);
                                    semaphore.Release();
                                }
                            }
                            else
                            {
                                failedConsecutive++;
                                if (failedConsecutive > 10 && concurrency > minConcurrency)
                                {
                                    concurrency = Math.Max(minConcurrency, concurrency - 5);
                                    await semaphore.WaitAsync();
                                    semaphore.Release();
                                }
                            }

                            int currentCompleted = Interlocked.Increment(ref completed);

                            var now = DateTime.Now;
                            if (now - lastProgressUpdate >= progressUpdateInterval || currentCompleted == total)
                            {
                                lastProgressUpdate = now;
                                BeginInvoke(new Action(() =>
                                {
                                    ShowLoading(true, $"Проверка прокси: {currentCompleted}/{total} (потоков: {concurrency}, таймаут: {currentTimeout}мс)");
                                    lblStatus.Text = $"Проверка {region}: {currentCompleted}/{total} | Найдено рабочих: {workingProxies.Count}";
                                }));
                            }
                        }
                        finally
                        {
                            semaphore.Release();
                        }
                    }, cts.Token));
                }

                await Task.WhenAll(tasks);
            }

            ShowLoading(true, $"Проверка прокси: {total}/{total}");
            lblStatus.Text = $"Проверка {region}: {total}/{total} | Найдено рабочих: {workingProxies.Count}";
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
                Height = 250,
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
                       "• Загрузить свой список прокси (кнопка «СВОЙ СПИСОК»)\n\n" +
                       "Формат файла:\n" +
                       "https://t.me/proxy?server=IP&port=ПОРТ&secret=СЕКРЕТ",
                Location = new Point(20, 90),
                Width = panel.Width - 40,
                Height = 130,
                Font = new Font("Tahoma", 10),
                ForeColor = Color.FromArgb(114, 118, 125),
                TextAlign = ContentAlignment.TopLeft
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

                    // Автоматически подгоняем ширину под текст
                    lblLoadingProgress.AutoSize = true;
                    lblLoadingProgress.MaximumSize = new Size(loadingPanel.Width - 40, 0);

                    // Центрируем элементы
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
            btnSettings.Enabled = enabled;
            btnLoadCustom.Enabled = enabled;

            if (enabled)
            {
                btnProxyEU.Text = "ЕВРОПА";
                btnProxyRU.Text = "РОССИЯ";
                btnTest.Text = "SurfboardV2ray";
                btnSettings.Text = "⚙️ Настройки";
                btnLoadCustom.Text = "📁 СВОЙ СПИСОК";
            }
            else
            {
                btnProxyEU.Text = "⏳ ЗАГРУЗКА";
                btnProxyRU.Text = "⏳ ЗАГРУЗКА";
                btnTest.Text = "⏳ ЗАГРУЗКА";
                btnSettings.Text = "⏳ НАСТРОЙКИ";
                btnLoadCustom.Text = "⏳ ЗАГРУЗКА";
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
            Form aboutForm = new Form();
            aboutForm.Text = "О программе";
            aboutForm.Size = new Size(550, 580);
            aboutForm.StartPosition = FormStartPosition.CenterParent;
            aboutForm.FormBorderStyle = FormBorderStyle.FixedDialog;
            aboutForm.MaximizeBox = false;
            aboutForm.MinimizeBox = false;
            aboutForm.BackColor = Color.White;

            Label lblInfo = new Label()
            {
                Text = $"Telegram Proxy Parser v{APP_VERSION}\n\n" +
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

            aboutForm.Controls.AddRange(new Control[] { lblInfo, lblAndroid, lblGitHub, separator, lblSupport, btnGitHub });
            aboutForm.ShowDialog();
        }
    }
}