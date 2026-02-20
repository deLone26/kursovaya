using Microsoft.Web.WebView2.WinForms;
using Microsoft.Web.WebView2.Core;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public partial class MainForm : Form
    {
        private Panel sidePanel;
        private Panel contentPanel;
        private WebView2 webView;
        private Form activeForm = null;

        private bool isCollapsed = false;
        private int expandedWidth = 250;
        private int collapsedWidth = 70;

        private string connectionString;
        private int employeeId;

        public MainForm(string connString, int userId)
        {
            this.connectionString = connString;
            this.employeeId = userId;

            InitializeComponent();
            InitializeLayout();
            CreateMenuButtons();
            this.Load += async (s, e) => await InitializeWebView();
            ShowHome();
        }

        private void InitializeLayout()
        {
            this.WindowState = FormWindowState.Maximized;
            this.BackColor = Color.FromArgb(240, 242, 245);
            this.Text = "Информационная система для автоматизации, планирования и учета технического обслуживания котельного оборудования";

            sidePanel = new Panel();
            sidePanel.Dock = DockStyle.Left;
            sidePanel.Width = expandedWidth;
            sidePanel.BackColor = Color.FromArgb(24, 28, 40);
            sidePanel.AutoScroll = true;
            sidePanel.Padding = new Padding(10, 20, 10, 10);

            contentPanel = new Panel();
            contentPanel.Dock = DockStyle.Fill;
            contentPanel.BackColor = Color.FromArgb(245, 247, 250);
            contentPanel.Padding = new Padding(20);

            this.Controls.Add(contentPanel);
            this.Controls.Add(sidePanel);
        }

        private void CreateMenuButtons()
        {
            sidePanel.Controls.Clear();

            TableLayoutPanel tlp = new TableLayoutPanel();
            tlp.Dock = DockStyle.Top;
            tlp.AutoSize = true;
            tlp.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            tlp.ColumnCount = 1;
            tlp.Padding = new Padding(0);
            tlp.Margin = new Padding(0);

            int row = 0;

            // ГЛАВНОЕ
            tlp.RowCount = row + 1;
            tlp.Controls.Add(CreateHeaderLabel("ГЛАВНОЕ"), 0, row);
            tlp.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            row++;

            tlp.RowCount = row + 1;
            tlp.Controls.Add(CreateMenuButton("🏠", "Главная", "home"), 0, row);
            tlp.RowStyles.Add(new RowStyle(SizeType.Absolute, 45));
            row++;

            tlp.RowCount = row + 1;
            tlp.Controls.Add(CreateMenuButton("📊", "Дашборд", "dashboard"), 0, row);
            tlp.RowStyles.Add(new RowStyle(SizeType.Absolute, 45));
            row++;

            tlp.Controls.Add(CreateSeparator(), 0, row++);

            // ОБОРУДОВАНИЕ
            tlp.RowCount = row + 1;
            tlp.Controls.Add(CreateHeaderLabel("ОБОРУДОВАНИЕ"), 0, row);
            tlp.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            row++;

            tlp.RowCount = row + 1;
            tlp.Controls.Add(CreateMenuButton("🔧", "Всё оборудование", "equipment"), 0, row);
            tlp.RowStyles.Add(new RowStyle(SizeType.Absolute, 45));
            row++;

            tlp.RowCount = row + 1;
            tlp.Controls.Add(CreateMenuButton("⚠️", "Аварии", "accidents"), 0, row);
            tlp.RowStyles.Add(new RowStyle(SizeType.Absolute, 45));
            row++;

            tlp.RowCount = row + 1;
            tlp.Controls.Add(CreateMenuButton("🔨", "Ремонты", "repairs"), 0, row);
            tlp.RowStyles.Add(new RowStyle(SizeType.Absolute, 45));
            row++;

            tlp.RowCount = row + 1;
            tlp.Controls.Add(CreateMenuButton("📋", "Паспорта", "passports"), 0, row);
            tlp.RowStyles.Add(new RowStyle(SizeType.Absolute, 45));
            row++;

            tlp.Controls.Add(CreateSeparator(), 0, row++);

            // ПЛАНИРОВАНИЕ
            tlp.RowCount = row + 1;
            tlp.Controls.Add(CreateHeaderLabel("ПЛАНИРОВАНИЕ"), 0, row);
            tlp.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            row++;

            tlp.RowCount = row + 1;
            tlp.Controls.Add(CreateMenuButton("📅", "Планы ТО", "plans"), 0, row);
            tlp.RowStyles.Add(new RowStyle(SizeType.Absolute, 45));
            row++;

            tlp.RowCount = row + 1;
            tlp.Controls.Add(CreateMenuButton("📈", "Графики", "schedules"), 0, row);
            tlp.RowStyles.Add(new RowStyle(SizeType.Absolute, 45));
            row++;

            tlp.Controls.Add(CreateSeparator(), 0, row++);

            // РУКОВОДСТВО (показываем всем, но в menu.html будет скрыто для обычных пользователей)
            tlp.RowCount = row + 1;
            tlp.Controls.Add(CreateHeaderLabel("РУКОВОДСТВО"), 0, row);
            tlp.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            row++;

            tlp.RowCount = row + 1;
            tlp.Controls.Add(CreateMenuButton("👑", "Панель начальника", "boss"), 0, row);
            tlp.RowStyles.Add(new RowStyle(SizeType.Absolute, 45));
            row++;

            tlp.RowCount = row + 1;
            tlp.Controls.Add(CreateMenuButton("👥", "Сотрудники", "employees"), 0, row);
            tlp.RowStyles.Add(new RowStyle(SizeType.Absolute, 45));
            row++;

            tlp.RowCount = row + 1;
            tlp.Controls.Add(CreateMenuButton("💰", "Бюджет", "budget"), 0, row);
            tlp.RowStyles.Add(new RowStyle(SizeType.Absolute, 45));
            row++;

            tlp.Controls.Add(CreateSeparator(), 0, row++);

            // ВЫХОД
            tlp.RowCount = row + 1;
            Button btnExit = CreateMenuButton("🚪", "Выход", "exit");
            btnExit.BackColor = Color.FromArgb(220, 53, 69);
            tlp.Controls.Add(btnExit, 0, row);
            tlp.RowStyles.Add(new RowStyle(SizeType.Absolute, 45));

            sidePanel.Controls.Add(tlp);
        }

        private Button CreateMenuButton(string icon, string text, string tag)
        {
            Button btn = new Button();
            btn.Text = $"  {icon}  {text}";
            btn.TextAlign = ContentAlignment.MiddleLeft;
            btn.Dock = DockStyle.Top;
            btn.Height = 45;
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.ForeColor = Color.White;
            btn.BackColor = Color.FromArgb(24, 28, 40);
            btn.Font = new Font("Segoe UI", 11);
            btn.Tag = tag;
            btn.Cursor = Cursors.Hand;
            btn.Margin = new Padding(0);
            btn.Padding = new Padding(10, 0, 0, 0);

            btn.MouseEnter += (s, e) => {
                if (btn.BackColor != Color.FromArgb(0, 123, 255) && btn.BackColor != Color.FromArgb(220, 53, 69))
                    btn.BackColor = Color.FromArgb(52, 58, 64);
            };

            btn.MouseLeave += (s, e) => {
                if (btn.BackColor != Color.FromArgb(0, 123, 255) && btn.BackColor != Color.FromArgb(220, 53, 69))
                    btn.BackColor = Color.FromArgb(24, 28, 40);
            };

            btn.Click += MenuButton_Click;

            return btn;
        }

        private Label CreateHeaderLabel(string text)
        {
            Label lbl = new Label();
            lbl.Text = text;
            lbl.ForeColor = Color.FromArgb(160, 174, 192);
            lbl.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            lbl.Dock = DockStyle.Top;
            lbl.Height = 30;
            lbl.Padding = new Padding(10, 10, 0, 0);
            lbl.Margin = new Padding(0);
            return lbl;
        }

        private Panel CreateSeparator()
        {
            Panel sep = new Panel();
            sep.Dock = DockStyle.Top;
            sep.Height = 1;
            sep.BackColor = Color.FromArgb(52, 58, 64);
            sep.Margin = new Padding(10, 5, 10, 5);
            return sep;
        }

        private void MenuButton_Click(object sender, EventArgs e)
        {
            Button btn = sender as Button;
            string page = btn.Tag.ToString();

            foreach (Control ctrl in sidePanel.Controls)
            {
                if (ctrl is TableLayoutPanel tlp)
                {
                    foreach (Control subCtrl in tlp.Controls)
                    {
                        if (subCtrl is Button b)
                        {
                            b.BackColor = Color.FromArgb(24, 28, 40);
                            b.ForeColor = Color.White;
                        }
                    }
                }
            }

            btn.BackColor = Color.FromArgb(0, 123, 255);
            btn.ForeColor = Color.White;

            HandleMenuClick(page);
        }

        private async System.Threading.Tasks.Task InitializeWebView()
        {
            try
            {
                webView = new WebView2();
                webView.Dock = DockStyle.Fill;

                contentPanel.Controls.Clear();
                contentPanel.Controls.Add(webView);

                await webView.EnsureCoreWebView2Async(null);

                string webUIPath = @"C:\Users\Daniil\Desktop\4\kursovaya3\kursovaya\WebUI";
                string htmlPath = Path.Combine(webUIPath, "menu.html");

                if (File.Exists(htmlPath))
                {
                    webView.CoreWebView2.Navigate($"file:///{htmlPath.Replace('\\', '/')}");
                }

                webView.CoreWebView2.WebMessageReceived += (s, e) =>
                {
                    string page = e.TryGetWebMessageAsString();
                    HandleMenuClick(page);
                };
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка WebView2: {ex.Message}");
            }
        }

        private void HandleMenuClick(string page)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action<string>(HandleMenuClick), page);
                return;
            }

            switch (page)
            {
                case "home":
                    ShowHome();
                    break;
                case "dashboard":
                    ShowDashboard();
                    break;
                case "equipment":
                    OpenChildForm(new Form1());
                    break;
                case "accidents":
                    ShowPlaceholder("Журнал аварий");
                    break;
                case "repairs":
                    ShowPlaceholder("Учет ремонтов");
                    break;
                case "passports":
                    ShowPlaceholder("Паспорта оборудования");
                    break;
                case "plans":
                    ShowPlaceholder("Планы технического обслуживания");
                    break;
                case "schedules":
                    ShowPlaceholder("Графики работ");
                    break;
                case "boss":
                    OpenChildForm(new FormBoss(connectionString, employeeId));
                    break;
                case "employees":
                    OpenChildForm(new Form2());
                    break;
                case "budget":
                    ShowPlaceholder("Бюджет и затраты");
                    break;
                case "exit":
                    Application.Exit();
                    break;
            }
        }

        private void OpenChildForm(Form childForm)
        {
            if (activeForm != null)
                activeForm.Close();

            activeForm = childForm;
            childForm.TopLevel = false;
            childForm.FormBorderStyle = FormBorderStyle.None;
            childForm.Dock = DockStyle.Fill;

            contentPanel.Controls.Clear();
            contentPanel.Controls.Add(childForm);
            childForm.Show();
        }

        private void ShowPlaceholder(string title)
        {
            if (activeForm != null)
                activeForm.Close();

            contentPanel.Controls.Clear();

            Panel placeholder = new Panel();
            placeholder.Dock = DockStyle.Fill;
            placeholder.BackColor = Color.White;
            placeholder.Padding = new Padding(30);

            Label lblTitle = new Label();
            lblTitle.Text = title;
            lblTitle.Font = new Font("Segoe UI", 24, FontStyle.Bold);
            lblTitle.ForeColor = Color.FromArgb(33, 37, 41);
            lblTitle.Location = new Point(30, 30);
            lblTitle.AutoSize = true;

            Label lblDesc = new Label();
            lblDesc.Text = "Раздел находится в разработке";
            lblDesc.Font = new Font("Segoe UI", 12);
            lblDesc.ForeColor = Color.Gray;
            lblDesc.Location = new Point(30, 80);
            lblDesc.AutoSize = true;

            placeholder.Controls.Add(lblTitle);
            placeholder.Controls.Add(lblDesc);
            contentPanel.Controls.Add(placeholder);
        }

        private void ShowHome()
        {
            if (activeForm != null)
                activeForm.Close();

            contentPanel.Controls.Clear();

            if (webView != null && webView.CoreWebView2 != null)
            {
                contentPanel.Controls.Add(webView);
                string webUIPath = @"C:\Users\Daniil\Desktop\4\kursovaya3\kursovaya\WebUI";
                string htmlPath = Path.Combine(webUIPath, "menu.html");
                if (File.Exists(htmlPath))
                {
                    webView.CoreWebView2.Navigate($"file:///{htmlPath.Replace('\\', '/')}");
                }
            }
        }

        private void ShowDashboard()
        {
            ShowHome();
        }
    }
}
