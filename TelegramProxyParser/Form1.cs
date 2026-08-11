using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using TelegramProxyParser.Models;
using TelegramProxyParser.Services;
using TelegramProxyParser.UI;
using TelegramProxyParser.UI.Controls;
using TelegramProxyParser.UI.Forms;
using TelegramProxyParser.UI.Helpers;

namespace TelegramProxyParser
{
    public partial class Form1 : Form
    {
        private Form1UI _ui;
        private ProxyCheckOrchestrator _orchestrator;
        private CancellationTokenSource _cts;
        private bool _cancelRequested;

        private int _timeout = 300;
        private int _concurrency = 50;

        private const string APP_VERSION = "2.1.0";

        public Form1()
        {
            InitializeUI();
            InitializeServices();
            ShowWelcomeMessage();
            StartUpdateChecker();
        }

        private void InitializeUI()
        {
            _ui = new Form1UI(
                APP_VERSION,
                BtnMtProtoClick,
                BtnLoadCustomClick,
                BtnSettingsClick,
                BtnAboutClick);

            _ui.Build(this);
            _ui.AddSourceButtons(ProxySources.GetActiveSources(), BtnSourceClick);
            _ui.BtnShare.Click += BtnShareClick;
        }

        private void InitializeServices()
        {
            var parser = new ProxyParserService();
            var checker = new ProxyCheckerService();
            var loader = new ProxyLoadService(parser, checker);
            var mtProto = new MtProtoCheckerService();

            checker.SetTimeout(_timeout);
            mtProto.SetParameters(_timeout, _concurrency);

            _orchestrator = new ProxyCheckOrchestrator(loader, checker, mtProto);
            _orchestrator.StatusChanged += status =>
            this.Invoke(new Action(() => _ui.LblStatus.Text = status));
            _orchestrator.ProgressChanged += (msg, cur, total) =>
                this.Invoke(new Action(() => _ui.LblLoadingProgress.Text = msg));
        }

        // ===== ОБРАБОТЧИКИ КНОПОК =====

        private async void BtnMtProtoClick(object sender, EventArgs e)
        {
            await RunCheckAsync(async () =>
            {
                _ui.SetLoadingState(true, "Загрузка всех источников...");
                await _orchestrator.LoadFromMultipleUrlsAsync(ProxySources.GetActiveUrls());
                await _orchestrator.CheckMtProtoAsync(_concurrency, _cts.Token);
            });
        }

        private async void BtnSourceClick(object sender, EventArgs e)
        {
            if (!(sender is Button btn && btn.Tag is ProxySources.SourceInfo source))
                return;

            await RunCheckAsync(async () =>
            {
                _ui.SetLoadingState(true, "Загрузка списка прокси...");
                await _orchestrator.LoadFromUrlAsync(source.Url, source.Name);
                await _orchestrator.CheckAllAsync(_timeout, _concurrency, _cts.Token);
            });
        }


        private bool ShowCheckTypeDialog(out bool useMtProto)
        {
            useMtProto = false;

            var dialog = new Form()
            {
                Text = "Тип проверки",
                Size = new Size(320, 200),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = Color.White
            };

            var lbl = new Label()
            {
                Text = "Выберите тип проверки:",
                Location = new Point(20, 20),
                AutoSize = true,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };

            var btnMtProto = new Button()
            {
                Text = "MTProto проверка\n(только ee Fake TLS)",
                Size = new Size(130, 55),
                Location = new Point(20, 60),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(0, 136, 204),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                DialogResult = DialogResult.Yes
            };
            btnMtProto.FlatAppearance.BorderSize = 0;

            var btnTcp = new Button()
            {
                Text = "Обычная проверка\n(все прокси)",
                Size = new Size(130, 55),
                Location = new Point(160, 60),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(46, 204, 113),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                DialogResult = DialogResult.No
            };
            btnTcp.FlatAppearance.BorderSize = 0;

            var btnCancel = new Button()
            {
                Text = "Отмена",
                Size = new Size(100, 30),
                Location = new Point(110, 125),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(149, 165, 166),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                DialogResult = DialogResult.Cancel
            };
            btnCancel.FlatAppearance.BorderSize = 0;

            dialog.Controls.AddRange(new Control[] { lbl, btnMtProto, btnTcp, btnCancel });
            dialog.AcceptButton = btnMtProto;
            dialog.CancelButton = btnCancel;

            var result = dialog.ShowDialog(this);
            useMtProto = (result == DialogResult.Yes);

            return result != DialogResult.Cancel;
        }

        private async void BtnLoadCustomClick(object sender, EventArgs e)
        {
            using (var dialog = new OpenFileDialog())
            {
                dialog.Title = "Выберите файл со списком прокси";
                dialog.Filter = "Текстовые файлы (*.txt)|*.txt|Все файлы (*.*)|*.*";

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        var lines = System.IO.File.ReadAllLines(dialog.FileName)
                            .Select(l => l.Trim())
                            .Where(l => !string.IsNullOrWhiteSpace(l) && !l.StartsWith("//") && !l.StartsWith("#"))
                            .ToList();

                        if (lines.Count == 0)
                        {
                            ShowInfo("Файл не содержит ссылок на прокси!");
                            return;
                        }

                        // Красивый диалог выбора
                        if (!ShowCheckTypeDialog(out bool useMtProto))
                            return;

                        await RunCheckAsync(async () =>
                        {
                            _ui.SetLoadingState(true, "Загрузка списка прокси...");
                            _orchestrator.LoadFromFile(lines);
                            _orchestrator.CurrentSourceName = System.IO.Path.GetFileName(dialog.FileName);

                            if (useMtProto)
                            {
                                await _orchestrator.CheckMtProtoAsync(_concurrency, _cts.Token);
                            }
                            else
                            {
                                await _orchestrator.CheckAllAsync(_timeout, _concurrency, _cts.Token);
                            }
                        });
                    }
                    catch (Exception ex)
                    {
                        ShowError($"Ошибка чтения файла: {ex.Message}");
                    }
                }
            }
        }

        private void BtnSettingsClick(object sender, EventArgs e)
        {
            var (timeout, concurrency, saved) = SettingsForm.ShowDialog(this, _timeout, _concurrency);
            if (saved)
            {
                _timeout = timeout;
                _concurrency = concurrency;
                _ui.LblStatus.Text = $"Настройки сохранены: таймаут {_timeout}мс, потоков: {_concurrency}";
            }
        }

        private void BtnAboutClick(object sender, EventArgs e)
        {
            AboutForm.ShowDialog(this, APP_VERSION, _timeout, _concurrency);
        }

        private void BtnShareClick(object sender, EventArgs e)
        {
            if (_orchestrator.WorkingProxies.Count == 0) return;

            var topProxies = _orchestrator.WorkingProxies
                .OrderBy(p => p.Ping <= 0 ? int.MaxValue : p.Ping)
                .Take(10)
                .ToList();

            var text = string.Join("\n", topProxies.Select((p, i) => $"{i + 1}. {p.OriginalUrl}"));
            text += "\n\nПарсер прокси Telegram (Windows):\nhttps://github.com/ComradeBingo/Proxy-telegram-windows";
            text += "\n\nВерсия для Android:\nhttps://github.com/ComradeBingo/Proxy-Telegram-Android";

            Clipboard.SetText(text);
            _ui.LblStatus.Text = $"Скопировано {topProxies.Count} прокси в буфер обмена!";

            FlashButton(_ui.BtnShare, "СКОПИРОВАНО!");
        }

        // ===== ОСНОВНЫЕ МЕТОДЫ =====

        private async Task RunCheckAsync(Func<Task> checkAction)
        {
            _ui.SetBusyState(true);
            _cancelRequested = false;
            _cts = new CancellationTokenSource();

            // Настраиваем кнопку отмены
            _ui.BtnCancel.Visible = true;
            _ui.PositionBottomButtons(this);

            EventHandler cancelHandler = null;
            cancelHandler = (s, e) =>
            {
                _cancelRequested = true;
                _cts.Cancel();
                _ui.BtnCancel.Visible = false;
                _ui.LblStatus.Text = "Отмена...";
                _ui.BtnCancel.Click -= cancelHandler;
            };
            _ui.BtnCancel.Click += cancelHandler;

            try
            {
                _ui.FlowProxies.Controls.Clear();
                _orchestrator.Reset();
                _ui.BtnShare.Visible = false;

                await checkAction();
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                ShowError($"Ошибка: {ex.Message}");
            }
            finally
            {
                _ui.BtnCancel.Click -= cancelHandler;
                _ui.BtnCancel.Visible = false;
                _cts.Dispose();
                ShowResult();
                _ui.SetBusyState(false);
            }
        }

        private void ShowResult()
        {
            _ui.SetLoadingState(false);
            _ui.MtProtoButtonPanel.Visible = false;
            _ui.FlowProxies.Controls.Clear();

            if (_orchestrator.WorkingProxies.Count == 0)
            {
                _ui.FlowProxies.Controls.Add(
                    ProxyUICreator.CreateNoProxiesMessage(_ui.FlowProxies.Width));
                _ui.BtnShare.Visible = false;
            }
            else
            {
                // Добавляем подсказку вверху
                var lblHint = new Label()
                {
                    Text = "Кликните на прокси, чтобы добавить в Telegram",
                    Width = _ui.FlowProxies.Width - 40,
                    Height = 30,
                    Font = new Font("Segoe UI", 16, FontStyle.Bold),
                    ForeColor = Color.FromArgb(0, 136, 204),
                    TextAlign = ContentAlignment.MiddleCenter,
                    Margin = new Padding(0, 5, 0, 10)
                };
                _ui.FlowProxies.Controls.Add(lblHint);

                var sorted = _orchestrator.WorkingProxies
                    .OrderBy(p => p.Ping <= 0 ? int.MaxValue : p.Ping);

                foreach (var proxy in sorted)
                {
                    _ui.FlowProxies.Controls.Add(
                        new ProxyCard(proxy, _ui.FlowProxies.Width - 40, _ui.FlowProxies));
                }

                _ui.BtnShare.Visible = true;
                _ui.PositionBottomButtons(this);
            }
        }

        private void ShowWelcomeMessage()
        {
            _ui.FlowProxies.Controls.Clear();
            _ui.MtProtoButtonPanel.Visible = true;
            _ui.FlowProxies.Controls.Add(
                ProxyUICreator.CreateWelcomeMessage(_ui.FlowProxies.Width, APP_VERSION));
        }

        // ===== ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ =====

        private void ShowError(string message)
        {
            MessageBox.Show(message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void ShowInfo(string message)
        {
            MessageBox.Show(message, "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void FlashButton(Button btn, string flashText)
        {
            var originalText = btn.Text;
            btn.Text = flashText;
            btn.Enabled = false;

            var timer = new System.Windows.Forms.Timer { Interval = 1500 };
            timer.Tick += (s, e) =>
            {
                btn.Text = originalText;
                btn.Enabled = true;
                timer.Stop();
                timer.Dispose();
            };
            timer.Start();
        }

        private void StartUpdateChecker()
        {
            var timer = new System.Windows.Forms.Timer { Interval = 1000 };
            timer.Tick += async (s, e) =>
            {
                timer.Stop();
                await CheckForUpdatesAsync();
            };
            timer.Start();
        }

        private async Task CheckForUpdatesAsync()
        {
            try
            {
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("User-Agent", "Telegram-Proxy-Parser-App");
                    var response = await client.GetAsync(
                        "https://api.github.com/repos/ComradeBingo/Proxy-telegram-windows/releases/latest");

                    if (response.IsSuccessStatusCode)
                    {
                        var json = await response.Content.ReadAsStringAsync();
                        var match = Regex.Match(json, "\"tag_name\":\\s*\"([^\"]+)\"");

                        if (match.Success)
                        {
                            var latest = match.Groups[1].Value.TrimStart('v');
                            if (new Version(latest) > new Version(APP_VERSION))
                            {
                                var result = MessageBox.Show(
                                    $"Доступна новая версия {latest}!\nТекущая: {APP_VERSION}\n\nПерейти к загрузке?",
                                    "Обновление", MessageBoxButtons.YesNo, MessageBoxIcon.Information);

                                if (result == DialogResult.Yes)
                                    Process.Start("https://github.com/ComradeBingo/Proxy-telegram-windows/releases/latest");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка проверки обновлений: {ex.Message}");
            }
        }
    }
}