using System;
using System.Drawing;
using System.Windows.Forms;

namespace TelegramProxyParser.UI.Forms
{
    public class SettingsForm : Form
    {
        private int currentTimeout;
        private int currentConcurrency;
        private ComboBox cmbTimeout;
        private ComboBox cmbConcurrency;

        public int Timeout { get; private set; }
        public int Concurrency { get; private set; }

        public SettingsForm(int timeout, int concurrency)
        {
            currentTimeout = timeout;
            currentConcurrency = concurrency;
            InitializeForm();
        }

        private void InitializeForm()
        {
            this.Text = "Настройки";
            this.Size = new Size(420, 350);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.White;

            Label lblTimeout = new Label()
            {
                Text = "Таймаут проверки (мс):",
                Location = new Point(25, 25),
                Size = new Size(170, 25),
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.FromArgb(52, 59, 75)
            };

            cmbTimeout = new ComboBox()
            {
                Location = new Point(210, 23),
                Size = new Size(120, 25),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 10)
            };
            cmbTimeout.Items.AddRange(new object[] { "300", "500", "750", "1000","2000","3000" });
            cmbTimeout.SelectedItem = currentTimeout.ToString();

            Label lblConcurrency = new Label()
            {
                Text = "Параллельных потоков:",
                Location = new Point(25, 70),
                Size = new Size(170, 25),
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.FromArgb(52, 59, 75)
            };

            cmbConcurrency = new ComboBox()
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
            btnSave.Click += BtnSave_Click;

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
            btnCancel.Click += (s, ev) => this.Close();

            this.Controls.AddRange(new Control[] { lblTimeout, cmbTimeout, lblConcurrency, cmbConcurrency, lblInfo, btnSave, btnCancel });
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            Timeout = int.Parse(cmbTimeout.SelectedItem.ToString());
            Concurrency = int.Parse(cmbConcurrency.SelectedItem.ToString());
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        public static (int timeout, int concurrency, bool saved) ShowDialog(IWin32Window owner, int currentTimeout, int currentConcurrency)
        {
            using (var form = new SettingsForm(currentTimeout, currentConcurrency))
            {
                var result = form.ShowDialog(owner);
                if (result == DialogResult.OK)
                {
                    return (form.Timeout, form.Concurrency, true);
                }
                return (currentTimeout, currentConcurrency, false);
            }
        }
    }
}