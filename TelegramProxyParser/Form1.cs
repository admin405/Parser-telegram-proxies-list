using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using TelegramProxyParser.Models;
using TelegramProxyParser.Services;
using TelegramProxyParser.UI.Controls;
using TelegramProxyParser.UI.Forms;
using TelegramProxyParser.UI.Helpers;

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
        private Button btnShare;
        private Button btnCancel;
        private bool _cancelRequested = false;

        private int currentTimeout = 300;
        private int currentConcurrency = 5;

        private List<ProxyInfo> allProxies;
        private List<ProxyInfo> workingProxies;
        private ProxyParserService proxyParser;
        private ProxyCheckerService proxyChecker;
        private ProxyLoadService proxyLoadService;

        private const string APP_VERSION = "1.9.2";
        private const string PROXY_EU_URL = "https://raw.githubusercontent.com/kort0881/telegram-proxy-collector/main/proxy_eu.txt";
        private const string PROXY_RU_URL = "https://raw.githubusercontent.com/kort0881/telegram-proxy-collector/main/proxy_ru.txt";
        private const string PROXY_TEST_URL = "https://raw.githubusercontent.com/Surfboardv2ray/TGProto/refs/heads/main/proxies-tested.txt";

        public Form1()
        {
            InitializeComponent();
            InitializeServices();
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

        private void InitializeServices()
        {
            proxyParser = new ProxyParserService();
            proxyChecker = new ProxyCheckerService();
            proxyLoadService = new ProxyLoadService(proxyParser, proxyChecker);
            proxyChecker.SetTimeout(currentTimeout);
            allProxies = new List<ProxyInfo>();
            workingProxies = new List<ProxyInfo>();
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

            lblProgramName = new Label()
            {
                Text = $"Парсер прокси Telegram v{APP_VERSION}",
                AutoSize = true,
                Location = new Point(415, 25),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleRight
            };

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

            topPanel.Controls.AddRange(new Control[] { btnProxyEU, btnProxyRU, btnTest, btnAbout, btnLoadCustom, btnSettings, lblProgramName });

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
                Height = 30,
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
                Location = new Point(10, 6),
                AutoSize = true,
                Font = new Font("Tahoma", 9, FontStyle.Regular),
                ForeColor = Color.FromArgb(114, 118, 125),
                Text = "Готов к работе"
            };

            statusPanel.Controls.Add(lblStatus);

            btnShare = new Button()
            {
                Text = "СКОПИРОВАТЬ",
                Size = new Size(180, 45),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(144, 212, 19),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Visible = false
            };
            btnShare.FlatAppearance.BorderSize = 0;
            btnShare.FlatAppearance.MouseOverBackColor = Color.FromArgb(39, 174, 96);
            btnShare.FlatAppearance.MouseDownBackColor = Color.FromArgb(33, 148, 82);
            btnShare.Click += BtnShare_Click;

            btnCancel = new Button()
            {
                Text = "СТОП",
                Size = new Size(120, 40),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(231, 76, 60),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Visible = false
            };
            btnCancel.FlatAppearance.BorderSize = 0;
            
            btnCancel.Click += (s, e) =>
            {
                _cancelRequested = true;
                btnCancel.Visible = false;
                lblStatus.Text = "Отмена... дождитесь завершения текущих проверок";
            };

            this.Controls.AddRange(new Control[] { flowProxies, loadingPanel, statusPanel, topPanel });
            this.Controls.Add(btnShare);
            this.Controls.Add(btnCancel);

            btnShare.Location = new Point((this.ClientSize.Width - btnShare.Width) / 2,
                                           this.ClientSize.Height - btnShare.Height - 55);
        }

        private void ShowWelcomeMessage()
        {
            flowProxies.Controls.Clear();
            var welcomePanel = ProxyUICreator.CreateWelcomeMessage(flowProxies.Width, APP_VERSION);
            flowProxies.Controls.Add(welcomePanel);
        }

        private void ResetToWelcomeState()
        {
            _cancelRequested = false;
            btnCancel.Visible = false;
            ShowWelcomeMessage();
            lblStatus.Text = "Готов к работе";
            ShowLoading(false);
            btnShare.Visible = false;
        }

        private void BtnSettings_Click(object sender, EventArgs e)
        {
            var (timeout, concurrency, saved) = SettingsForm.ShowDialog(this, currentTimeout, currentConcurrency);
            if (saved)
            {
                currentTimeout = timeout;
                currentConcurrency = concurrency;
                proxyChecker.SetTimeout(currentTimeout);
                lblStatus.Text = $"Настройки сохранены: таймаут {currentTimeout} мс, потоков: {currentConcurrency}";
            }
        }

        private void BtnAbout_Click(object sender, EventArgs e)
        {
            AboutForm.ShowDialog(this, APP_VERSION, currentTimeout, currentConcurrency);
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
                SetControlsEnabled(false);
                ShowLoading(true, "Загрузка списка прокси...");

                flowProxies.Controls.Clear();
                allProxies.Clear();
                workingProxies.Clear();
                btnShare.Visible = false;

                lblStatus.Text = $"Загрузка прокси из файла: {fileName}...";

                allProxies = proxyLoadService.LoadProxiesFromFile(proxyLines);

                if (allProxies.Count == 0)
                {
                    MessageBox.Show("Не удалось распарсить прокси!\n\n" +
                        "Убедитесь, что файл содержит ссылки в формате:\n" +
                        "https://t.me/proxy?server=IP&port=ПОРТ&secret=СЕКРЕТ",
                        "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                    ResetToWelcomeState();
                    return;
                }

                await CheckAllProxies(fileName);
                ShowResult();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                ResetToWelcomeState();
            }
            finally
            {
                SetControlsEnabled(true);
            }
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
            await LoadAndCheckProxies(PROXY_TEST_URL, "Surfboardv2ray");
        }

        private async Task LoadAndCheckProxies(string url, string region)
        {
            try
            {
                SetControlsEnabled(false);
                ShowLoading(true, "Загрузка списка прокси...");

                flowProxies.Controls.Clear();
                allProxies.Clear();
                workingProxies.Clear();
                btnShare.Visible = false;

                lblStatus.Text = $"Загрузка прокси {region}...";

                allProxies = await proxyLoadService.LoadProxiesFromUrlAsync(url);

                if (allProxies.Count == 0)
                {
                    MessageBox.Show("Прокси не найдены!", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    ResetToWelcomeState();
                    return;
                }

                await CheckAllProxies(region);
                ShowResult();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                ResetToWelcomeState();
            }
            finally
            {
                SetControlsEnabled(true);
            }
        }

        private async Task CheckAllProxies(string region)
        {
            _cancelRequested = false;

            var cancelTimer = new System.Windows.Forms.Timer();
            cancelTimer.Interval = 2000;
            cancelTimer.Tick += (s, e) =>
            {
                if (!_cancelRequested && !btnCancel.IsDisposed)
                {
                    btnCancel.Visible = true;
                }
                cancelTimer.Stop();
            };
            cancelTimer.Start();

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
                    if (_cancelRequested)
                        break;

                    await semaphore.WaitAsync();

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
                    }));
                }

                await Task.WhenAll(tasks);
            }

            cancelTimer.Dispose();
            btnCancel.Visible = false;

            if (_cancelRequested)
            {
                lblStatus.Text = $"Проверка отменена | Успело провериться: {completed}/{total} | Рабочих: {workingProxies.Count}";
                return;
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
                var noProxiesPanel = ProxyUICreator.CreateNoProxiesMessage(flowProxies.Width);
                flowProxies.Controls.Add(noProxiesPanel);

                int fakeTlsCount = allProxies.Count(p => p.ProxyType == "Fake TLS");
                int secureCount = allProxies.Count(p => p.ProxyType == "Secure");
                int classicCount = allProxies.Count(p => p.ProxyType == "Classic");

                lblStatus.Text = $"Завершено | Всего: {allProxies.Count} | Рабочих: 0 | Fake TLS: {fakeTlsCount} | Secure: {secureCount} | Classic: {classicCount}";
                flowProxies.Refresh();
                btnShare.Visible = false;
            }
            else
            {
                var sortedProxies = workingProxies.OrderBy(p => p.Ping <= 0 ? 0 : p.Ping).ToList();

                foreach (var proxy in sortedProxies)
                {
                    var proxyCard = new ProxyCard(proxy, flowProxies.Width, flowProxies);
                    flowProxies.Controls.Add(proxyCard);
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
                                $"Classic: {workingClassic}/{totalClassic} | Сортировка по пингу";

                btnShare.Visible = true;
                btnShare.BringToFront();
                btnShare.Location = new Point((this.ClientSize.Width - btnShare.Width) / 2,
                                               this.ClientSize.Height - btnShare.Height - 55);
            }
        }

        private void BtnShare_Click(object sender, EventArgs e)
        {
            if (workingProxies == null || workingProxies.Count == 0)
            {
                MessageBox.Show("Нет рабочих прокси для публикации!", "Информация",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var topProxies = workingProxies
                .OrderBy(p => p.Ping <= 0 ? 0 : p.Ping)
                .Take(10)
                .ToList();

            string shareText = "";

            for (int i = 0; i < topProxies.Count; i++)
            {
                var proxy = topProxies[i];
                shareText += $"{i + 1}. {proxy.OriginalUrl}\n";
            }

            shareText += "\n\nСкачать парсер для Windows или Android:\n";
            shareText += "https://github.com/ComradeBingo/Proxy-telegram-windows\n";
            shareText += "https://github.com/ComradeBingo/Proxy-Telegram-Android";

            Clipboard.SetText(shareText);

            lblStatus.Text = $"Скопировано {topProxies.Count} прокси в буфер обмена!";

            string originalText = btnShare.Text;
            btnShare.Text = "СКОПИРОВАНО!";
            btnShare.Enabled = false;

            System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
            timer.Interval = 1500;
            timer.Tick += (s, ev) =>
            {
                btnShare.Text = originalText;
                btnShare.Enabled = true;
                timer.Stop();
            };
            timer.Start();
        }

        private void ShowLoading(bool show, string message = null)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => ShowLoading(show, message)));
                return;
            }

            if (loadingPanel != null)
                loadingPanel.Visible = show;

            if (flowProxies != null)
                flowProxies.Visible = !show;

            if (show && loadingPanel != null)
            {
                loadingPanel.BringToFront();
                if (message != null && lblLoadingProgress != null && progressBar != null)
                {
                    lblLoadingProgress.Text = message;
                    lblLoadingProgress.AutoSize = true;
                    lblLoadingProgress.MaximumSize = new Size(loadingPanel.Width - 40, 0);

                    progressBar.Location = new Point(loadingPanel.Width / 2 - progressBar.Width / 2,
                                                    loadingPanel.Height / 2 - 30);
                    lblLoadingProgress.Location = new Point(loadingPanel.Width / 2 - lblLoadingProgress.Width / 2,
                                                           loadingPanel.Height / 2 + 10);

                    if (btnCancel != null && btnCancel.Visible)
                    {
                        btnCancel.Location = new Point(loadingPanel.Width / 2 - btnCancel.Width / 2,
                                                       loadingPanel.Height / 2 + 170); // Отступ кнопки "СТОП"
                        btnCancel.BringToFront();
                    }
                }
            }
            else if (!show && flowProxies != null)
            {
                flowProxies.BringToFront();
                flowProxies.Refresh();

                if (btnShare != null && btnShare.Visible)
                    btnShare.BringToFront();
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
                btnSettings.Text = "Настройки";
                btnLoadCustom.Text = "СВОЙ СПИСОК";
            }
            else
            {
                btnProxyEU.Text = "ЗАГРУЗКА";
                btnProxyRU.Text = "ЗАГРУЗКА";
                btnTest.Text = "ЗАГРУЗКА";
                btnSettings.Text = "НАСТРОЙКИ";
                btnLoadCustom.Text = "ЗАГРУЗКА";
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
    }
}