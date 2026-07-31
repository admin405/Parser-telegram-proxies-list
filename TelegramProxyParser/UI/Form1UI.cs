using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace TelegramProxyParser.UI
{
    public class Form1UI
    {
        public Panel HeaderPanel { get; private set; }
        public Panel SidebarPanel { get; private set; }
        public Panel MainContentPanel { get; private set; }
        public FlowLayoutPanel FlowProxies { get; private set; }
        public Panel LoadingPanel { get; private set; }
        public Label LblLoadingProgress { get; private set; }
        public ProgressBar ProgressBar { get; private set; }
        public Label LblStatus { get; private set; }
        public Button BtnShare { get; private set; }
        public Button BtnCancel { get; private set; }
        public Button BtnMtProtoMain { get; private set; }
        public Panel MtProtoButtonPanel { get; private set; }
        public Button BtnLoadCustom { get; private set; }
        public Button BtnSettings { get; private set; }
        public Button BtnAbout { get; private set; }
        public Button BtnMtProtoSidebar { get; private set; }
        public List<Button> SourceButtons { get; private set; }

        private readonly string _appVersion;
        private readonly EventHandler _btnMtProtoClick;
        private readonly EventHandler _btnLoadCustomClick;
        private readonly EventHandler _btnSettingsClick;
        private readonly EventHandler _btnAboutClick;
        private Panel _statusPanel;

        public Form1UI(
            string appVersion,
            EventHandler btnMtProtoClick,
            EventHandler btnLoadCustomClick,
            EventHandler btnSettingsClick,
            EventHandler btnAboutClick)
        {
            _appVersion = appVersion;
            _btnMtProtoClick = btnMtProtoClick;
            _btnLoadCustomClick = btnLoadCustomClick;
            _btnSettingsClick = btnSettingsClick;
            _btnAboutClick = btnAboutClick;
            SourceButtons = new List<Button>();
        }

        public void Build(Form form)
        {
            form.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            form.Text = $"Telegram Proxy Parser v{_appVersion}";
            form.Size = new Size(850, 900);
            form.MinimumSize = new Size(850, 700);
            form.FormBorderStyle = FormBorderStyle.None;  // Убираем стандартную рамку
            form.StartPosition = FormStartPosition.CenterScreen;
            form.BackColor = Color.FromArgb(240, 242, 245);

            // Делаем форму перемещаемой
            form.MouseDown += (s, e) => { if (e.Button == MouseButtons.Left) form.Capture = false; };

            BuildHeader(form);
            BuildSidebar();
            BuildMainContent();
            BuildStatusBar();
            BuildBottomButtons();

            form.Controls.Add(MainContentPanel);
            form.Controls.Add(MtProtoButtonPanel);
            form.Controls.Add(SidebarPanel);
            form.Controls.Add(HeaderPanel);
            form.Controls.Add(_statusPanel);
            form.Controls.Add(BtnShare);
            form.Controls.Add(BtnCancel);

            form.Resize += (s, e) => PositionBottomButtons(form);
        }

        private void BuildHeader(Form form)
        {
            HeaderPanel = new Panel()
            {
                Dock = DockStyle.Top,
                Height = 35,
                BackColor = Color.FromArgb(0, 136, 204),
                Padding = new Padding(10, 0, 5, 0)
            };

            // Заголовок
            var lblTitle = new Label()
            {
                Text = $"Telegram Proxy Parser v{_appVersion}",
                Location = new Point(10, 7),
                AutoSize = true,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 11, FontStyle.Bold)
            };

            // Кнопки управления (без изменений)

            var btnMinimize = new Button()
            {
                Text = "\u2500",
                Size = new Size(35, 25),
                Location = new Point(HeaderPanel.Width - 70, 5),  // Сдвинули правее
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                ForeColor = Color.White,
                Font = new Font("Segoe MDL2 Assets", 10),
                Cursor = Cursors.Hand
            };
            btnMinimize.FlatAppearance.BorderSize = 0;
            btnMinimize.FlatAppearance.MouseOverBackColor = Color.FromArgb(0, 100, 170);
            btnMinimize.Click += (s, e) => form.WindowState = FormWindowState.Minimized;

            var btnClose = new Button()
            {
                Text = "\u2715",
                Size = new Size(35, 25),
                Location = new Point(HeaderPanel.Width - 35, 5),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                ForeColor = Color.White,
                Font = new Font("Segoe MDL2 Assets", 10),
                Cursor = Cursors.Hand
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.FlatAppearance.MouseOverBackColor = Color.FromArgb(232, 17, 35);
            btnClose.Click += (s, e) => form.Close();

            HeaderPanel.Controls.Add(lblTitle);
            HeaderPanel.Controls.Add(btnMinimize);
            HeaderPanel.Controls.Add(btnClose);  // Только две кнопки

            // Позиционирование
            HeaderPanel.Resize += (s, e) =>
            {
                btnMinimize.Location = new Point(HeaderPanel.Width - 70, 5);
                btnClose.Location = new Point(HeaderPanel.Width - 35, 5);
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.FlatAppearance.MouseOverBackColor = Color.FromArgb(232, 17, 35);
            btnClose.Click += (s, e) => form.Close();

            HeaderPanel.Controls.Add(lblTitle);
            HeaderPanel.Controls.Add(btnMinimize);
            
            HeaderPanel.Controls.Add(btnClose);

            // Перемещение за ЛЮБОЕ место в HeaderPanel
            bool dragging = false;
            Point startPoint = Point.Empty;

            HeaderPanel.MouseDown += (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    dragging = true;
                    startPoint = new Point(e.X, e.Y);
                }
            };
            HeaderPanel.MouseMove += (s, e) =>
            {
                if (dragging)
                {
                    form.Location = new Point(
                        form.Location.X + e.X - startPoint.X,
                        form.Location.Y + e.Y - startPoint.Y);
                }
            };
            HeaderPanel.MouseUp += (s, e) => dragging = false;

            
        }

        private void BuildSidebar()
        {
            SidebarPanel = new Panel()
            {
                Dock = DockStyle.Left,
                Width = 220,
                BackColor = Color.White,
                Padding = new Padding(10)
            };

            // Заголовок "Меню"
            var lblActions = new Label()
            {
                Text = "МЕНЮ",
                Location = new Point(10, 15),
                AutoSize = true,
                ForeColor = Color.FromArgb(52, 73, 94),
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };

            // Кнопка MTProto в сайдбаре
            BtnMtProtoSidebar = CreateSidebarButton(
                "MTProto ПРОВЕРКА",
                45,
                Color.FromArgb(0, 136, 204),
                42,
                _btnMtProtoClick);

            // Кнопка "Свой список"
            BtnLoadCustom = CreateSidebarButton(
                "Загрузить свой список",
                97,
                Color.FromArgb(207, 157, 8),
                38,
                _btnLoadCustomClick);

            // Кнопка "Настройки"
            BtnSettings = CreateSidebarButton(
                "Настройки",
                145,
                Color.FromArgb(52, 73, 94),
                38,
                _btnSettingsClick);

            // Кнопка "Справка"
            BtnAbout = CreateSidebarButton(
                "Справка",
                193,
                Color.FromArgb(149, 165, 166),
                38,
                _btnAboutClick);

            // Разделитель
            var separator = new Label()
            {
                Text = "━━━━━━━━━━━━━━━━━━━",
                Location = new Point(10, 242),
                AutoSize = true,
                ForeColor = Color.FromArgb(200, 200, 205),
                Font = new Font("Segoe UI", 7)
            };

            // Заголовок "Источники"
            var lblSources = new Label()
            {
                Text = "ИСТОЧНИКИ ПРОКСИ",
                Location = new Point(10, 267),
                AutoSize = true,
                ForeColor = Color.FromArgb(52, 73, 94),
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };

            SidebarPanel.Controls.AddRange(new Control[] {
                lblActions,
                BtnMtProtoSidebar,
                BtnLoadCustom,
                BtnSettings,
                BtnAbout,
                separator,
                lblSources
            });
        }

        public void AddSourceButtons(List<ProxySources.SourceInfo> sources, EventHandler clickHandler)
        {
            int yPos = 297;
            foreach (var source in sources)
            {
                var btn = CreateSourceButton(source, yPos, clickHandler);
                SourceButtons.Add(btn);
                SidebarPanel.Controls.Add(btn);
                yPos += 48;
            }
        }

        private Button CreateSidebarButton(string text, int y, Color color, int height, EventHandler handler)
        {
            var btn = new Button()
            {
                Text = text,
                Location = new Point(10, y),
                Size = new Size(200, height),
                FlatStyle = FlatStyle.Flat,
                BackColor = color,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Cursor = Cursors.Hand,
                TextAlign = height == 42 ? ContentAlignment.MiddleCenter : ContentAlignment.MiddleLeft,
                Padding = height == 42 ? new Padding(0) : new Padding(10, 0, 0, 0)
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = LightenColor(color, 20);
            btn.FlatAppearance.MouseDownBackColor = DarkenColor(color, 20);
            btn.Click += handler;
            return btn;
        }

        private Button CreateSourceButton(ProxySources.SourceInfo source, int yPos, EventHandler handler)
        {
            var btn = new Button()
            {
                Text = source.Name,
                Location = new Point(10, yPos),
                Size = new Size(200, 40),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White,
                ForeColor = Color.Black,
                Font = new Font("Segoe UI", 14, FontStyle.Regular),
                Cursor = Cursors.Hand,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(12, 0, 0, 0),
                Tag = source
            };

            btn.FlatAppearance.BorderSize = 1;
            btn.FlatAppearance.BorderColor = Color.FromArgb(0, 136, 204); // Цвет MTProto
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(240, 248, 255);
            btn.FlatAppearance.MouseDownBackColor = Color.FromArgb(220, 238, 255);

            btn.Click += handler;

            var tooltip = new ToolTip();
            tooltip.SetToolTip(btn, source.Description);

            return btn;
        }

        private void BuildMainContent()
        {
            MainContentPanel = new Panel()
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(240, 242, 245)
            };

            // Главная кнопка MTProto
            MtProtoButtonPanel = new Panel()
            {
                Dock = DockStyle.Top,
                Height = 145,
                BackColor = Color.FromArgb(240, 242, 245),
                Padding = new Padding(30, 10, 30, 10)
            };

            BtnMtProtoMain = new Button()
            {
                Text = "ЗАПУСТИТЬ MTPROTO ПРОВЕРКУ",
                Size = new Size(500, 75),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(0, 136, 204),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                Cursor = Cursors.Hand,
                TextAlign = ContentAlignment.MiddleCenter
            };
            BtnMtProtoMain.FlatAppearance.BorderSize = 2;
            BtnMtProtoMain.FlatAppearance.BorderColor = Color.FromArgb(0, 100, 160);
            BtnMtProtoMain.FlatAppearance.MouseOverBackColor = Color.FromArgb(0, 153, 230);
            BtnMtProtoMain.FlatAppearance.MouseDownBackColor = Color.FromArgb(0, 119, 179);
            BtnMtProtoMain.Click += _btnMtProtoClick;

            var lblHint1 = new Label()
            {
                Text = "Проверка всех источников через MTProto handshake",
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.FromArgb(120, 130, 140),
                Font = new Font("Segoe UI", 11)
            };

            var lblHint2 = new Label()
            {
                Text = "Это займёт время, но даст 100% результат",
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.FromArgb(120, 130, 140),
                Font = new Font("Segoe UI", 11)
            };

            MtProtoButtonPanel.Controls.Add(BtnMtProtoMain);
            MtProtoButtonPanel.Controls.Add(lblHint1);
            MtProtoButtonPanel.Controls.Add(lblHint2);

            MtProtoButtonPanel.Resize += (s, e) =>
            {
                BtnMtProtoMain.Location = new Point(
                    (MtProtoButtonPanel.Width - BtnMtProtoMain.Width) / 2, 20);
                lblHint1.Location = new Point(
                    (MtProtoButtonPanel.Width - lblHint1.Width) / 2, 95);
                lblHint2.Location = new Point(
                    (MtProtoButtonPanel.Width - lblHint2.Width) / 2, 112);
            };

            // Панель с прокси
            FlowProxies = new FlowLayoutPanel()
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(20),
                BackColor = Color.FromArgb(240, 242, 245),
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false
            };

            // Панель загрузки
            LoadingPanel = new Panel()
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(240, 242, 245),
                Visible = false
            };

            ProgressBar = new ProgressBar()
            {
                Style = ProgressBarStyle.Marquee,
                Size = new Size(400, 8),
                MarqueeAnimationSpeed = 25
            };

            LblLoadingProgress = new Label()
            {
                Size = new Size(500, 40),
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.FromArgb(52, 59, 75)
            };

            LoadingPanel.Controls.Add(ProgressBar);
            LoadingPanel.Controls.Add(LblLoadingProgress);

            LoadingPanel.Resize += (s, e) =>
            {
                ProgressBar.Location = new Point(
                    (LoadingPanel.Width - ProgressBar.Width) / 2,
                    LoadingPanel.Height / 2 - 40);
                LblLoadingProgress.Location = new Point(
                    (LoadingPanel.Width - LblLoadingProgress.Width) / 2,
                    LoadingPanel.Height / 2);
            };

            MainContentPanel.Controls.Add(FlowProxies);
            MainContentPanel.Controls.Add(LoadingPanel);
        }

        private void BuildStatusBar()
        {
            _statusPanel = new Panel()
            {
                Dock = DockStyle.Bottom,
                Height = 32,
                BackColor = Color.FromArgb(248, 249, 250),
                Padding = new Padding(15, 0, 15, 0)
            };

            _statusPanel.Paint += (s, e) =>
            {
                using (var pen = new Pen(Color.FromArgb(220, 223, 230), 1))
                {
                    e.Graphics.DrawLine(pen, 0, 0, _statusPanel.Width, 0);
                }
            };

            LblStatus = new Label()
            {
                Location = new Point(15, 7),
                AutoSize = true,
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(114, 118, 125),
                Text = "Готов к работе"
            };

            _statusPanel.Controls.Add(LblStatus);
        }

        private void BuildBottomButtons()
        {
            BtnShare = new Button()
            {
                Text = "СКОПИРОВАТЬ 10 ПЕРВЫХ",
                Size = new Size(280, 48),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(46, 204, 113),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Visible = false
            };
            BtnShare.FlatAppearance.BorderSize = 0;

            BtnCancel = new Button()
            {
                Text = "ОСТАНОВИТЬ",
                Size = new Size(160, 42),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(231, 76, 60),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Visible = false
            };
            BtnCancel.FlatAppearance.BorderSize = 0;
        }

        public void PositionBottomButtons(Form form)
        {
            int offset = 100;
            if (BtnShare.Visible)
            {
                BtnShare.Location = new Point(
                    (form.ClientSize.Width - BtnShare.Width) / 2 + offset,
                    form.ClientSize.Height - BtnShare.Height - 55);
                BtnShare.BringToFront();
            }
            if (BtnCancel.Visible)
            {
                BtnCancel.Location = new Point(
                    (form.ClientSize.Width - BtnCancel.Width) / 2 + offset,
                    form.ClientSize.Height - BtnCancel.Height - 55);
                BtnCancel.BringToFront();
            }
        }

        public void SetLoadingState(bool loading, string message = null)
        {
            if (loading)
            {
                LoadingPanel.Visible = true;
                LoadingPanel.BringToFront();
                FlowProxies.Visible = false;
                MtProtoButtonPanel.Visible = false;
                if (message != null) LblLoadingProgress.Text = message;
            }
            else
            {
                LoadingPanel.Visible = false;
                FlowProxies.Visible = true;
                FlowProxies.BringToFront();
            }
        }

        public void SetBusyState(bool busy)
        {
            BtnMtProtoMain.Enabled = !busy;
            BtnMtProtoSidebar.Enabled = !busy;
            BtnLoadCustom.Enabled = !busy;
            BtnSettings.Enabled = !busy;
            BtnAbout.Enabled = !busy;

            foreach (var btn in SourceButtons)
                btn.Enabled = !busy;

            if (busy)
            {
                BtnMtProtoMain.Text = "ВЫПОЛНЯЕТСЯ ПРОВЕРКА...";
                BtnMtProtoMain.BackColor = Color.FromArgb(0, 119, 179);
                BtnMtProtoSidebar.Text = "ПРОВЕРКА...";
                BtnMtProtoSidebar.BackColor = Color.FromArgb(0, 119, 179);
            }
            else
            {
                BtnMtProtoMain.Text = "ЗАПУСТИТЬ MTPROTO ПРОВЕРКУ";
                BtnMtProtoMain.BackColor = Color.FromArgb(0, 136, 204);
                BtnMtProtoSidebar.Text = "MTProto ПРОВЕРКА";
                BtnMtProtoSidebar.BackColor = Color.FromArgb(0, 136, 204);
            }
        }

        private static Color LightenColor(Color color, int amount)
        {
            return Color.FromArgb(
                Math.Min(255, color.R + amount),
                Math.Min(255, color.G + amount),
                Math.Min(255, color.B + amount));
        }

        private static Color DarkenColor(Color color, int amount)
        {
            return Color.FromArgb(
                Math.Max(0, color.R - amount),
                Math.Max(0, color.G - amount),
                Math.Max(0, color.B - amount));
        }
    }
}