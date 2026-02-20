using Microsoft.Web.WebView2.WinForms;
using Microsoft.Web.WebView2.Core;
using System;
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

            contentPanel = new Panel();
            contentPanel.Dock = DockStyle.Fill;
            contentPanel.BackColor = Color.FromArgb(245, 247, 250);
            contentPanel.Padding = new Padding(20);

            this.Controls.Add(contentPanel);
            this.Controls.Add(sidePanel);
        }

        private async System.Threading.Tasks.Task InitializeWebView()
        {
            try
            {
                webView = new WebView2();
                webView.Dock = DockStyle.Fill;
                sidePanel.Controls.Add(webView);

                await webView.EnsureCoreWebView2Async(null);

                string htmlPath = Path.Combine(Application.StartupPath, "WebUI", "menu.html");

                if (File.Exists(htmlPath))
                {
                    webView.CoreWebView2.Navigate($"file:///{htmlPath.Replace('\\', '/')}");
                }

                webView.CoreWebView2.WebMessageReceived += (s, e) =>
                {
                    string page = e.TryGetWebMessageAsString();
                    HandleMenuClick(page);
                };

                webView.CoreWebView2.NavigationCompleted += (s, e) =>
                {
                    string role = CurrentUser.Role?.ToLower() ?? "user";
                    webView.CoreWebView2.ExecuteScriptAsync($"setUserRole('{role}')");

                    var data = new { accidentsCount = 3, notificationsCount = 5 };
                    string json = System.Text.Json.JsonSerializer.Serialize(data);
                    webView.CoreWebView2.ExecuteScriptAsync($"updateBadges({json})");
                };
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка WebView2: {ex.Message}\n\nПроверьте установку WebView2 Runtime");
                CreateBackupMenu();
            }
        }

        private void CreateBackupMenu()
        {
            Button btnHome = new Button();
            btnHome.Text = "Главная";
            btnHome.Dock = DockStyle.Top;
            btnHome.Height = 40;
            btnHome.FlatStyle = FlatStyle.Flat;
            btnHome.ForeColor = Color.White;
            btnHome.BackColor = Color.FromArgb(24, 28, 40);
            btnHome.Click += (s, e) => ShowHome();
            sidePanel.Controls.Add(btnHome);
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
                case "passports":
                    OpenChildForm(new Form1());
                    break;
                case "accidents":
                    ShowPlaceholder("Журнал аварий");
                    break;
                case "repairs":
                    ShowPlaceholder("Учет ремонтов");
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

            Panel homePanel = new Panel();
            homePanel.Dock = DockStyle.Fill;
            homePanel.BackColor = Color.FromArgb(245, 247, 250);
            homePanel.Padding = new Padding(30);
            homePanel.AutoScroll = true;

            // Заголовок
            Label lblMainTitle = new Label();
            lblMainTitle.Text = "Информационная система для автоматизации,\nпланирования и учета технического обслуживания\nкотельного оборудования";
            lblMainTitle.Font = new Font("Segoe UI", 28, FontStyle.Bold);
            lblMainTitle.ForeColor = Color.FromArgb(15, 23, 42);
            lblMainTitle.Location = new Point(30, 30);
            lblMainTitle.AutoSize = true;

            // Подзаголовок
            Label lblSub = new Label();
            lblSub.Text = "Организация пищевого производства • ООО «Промконсервы»";
            lblSub.Font = new Font("Segoe UI", 14);
            lblSub.ForeColor = Color.FromArgb(71, 85, 105);
            lblSub.Location = new Point(30, 150);
            lblSub.AutoSize = true;

            // Статистика
            FlowLayoutPanel statsPanel = new FlowLayoutPanel();
            statsPanel.Location = new Point(30, 200);
            statsPanel.Size = new Size(1100, 150);
            statsPanel.FlowDirection = FlowDirection.LeftToRight;

            string[,] stats = {
                { "🔧", "24", "Единиц оборудования" },
                { "⚠️", "3", "Аварии за месяц" },
                { "📅", "12", "Планов ТО" },
                { "✅", "8", "Выполнено" },
                { "👥", "15", "Сотрудников" },
                { "💰", "1.2M", "Бюджет" }
            };

            for (int i = 0; i < 6; i++)
            {
                Panel card = CreateStatCard(stats[i, 0], stats[i, 1], stats[i, 2]);
                statsPanel.Controls.Add(card);
            }

            // Последние события
            Label lblEvents = new Label();
            lblEvents.Text = "📋 Последние события";
            lblEvents.Font = new Font("Segoe UI", 18, FontStyle.Bold);
            lblEvents.Location = new Point(30, 370);
            lblEvents.AutoSize = true;

            // Таблица последних событий
            DataGridView eventsGrid = new DataGridView();
            eventsGrid.Location = new Point(30, 410);
            eventsGrid.Size = new Size(1100, 200);
            eventsGrid.BackgroundColor = Color.White;
            eventsGrid.BorderStyle = BorderStyle.None;
            eventsGrid.ColumnHeadersHeight = 40;
            eventsGrid.RowTemplate.Height = 35;
            eventsGrid.AllowUserToAddRows = false;
            eventsGrid.ReadOnly = true;
            eventsGrid.EnableHeadersVisualStyles = false;
            eventsGrid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(15, 23, 42);
            eventsGrid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            eventsGrid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10, FontStyle.Bold);

            eventsGrid.Columns.Add("date", "Дата");
            eventsGrid.Columns.Add("event", "Событие");
            eventsGrid.Columns.Add("equipment", "Оборудование");
            eventsGrid.Columns.Add("status", "Статус");

            eventsGrid.Rows.Add("20.02.2026", "Плановое ТО", "Котел ДКВР 10-13", "Выполнено");
            eventsGrid.Rows.Add("19.02.2026", "Аварийный ремонт", "Насос ПЭ 580-185", "В работе");
            eventsGrid.Rows.Add("18.02.2026", "Диагностика", "Горелка Weishaupt", "Завершено");
            eventsGrid.Rows.Add("17.02.2026", "Замена фильтров", "Водоподготовка", "Запланировано");

            eventsGrid.Columns[0].Width = 100;
            eventsGrid.Columns[1].Width = 200;
            eventsGrid.Columns[2].Width = 250;
            eventsGrid.Columns[3].Width = 150;

            homePanel.Controls.Add(lblMainTitle);
            homePanel.Controls.Add(lblSub);
            homePanel.Controls.Add(statsPanel);
            homePanel.Controls.Add(lblEvents);
            homePanel.Controls.Add(eventsGrid);

            contentPanel.Controls.Add(homePanel);
        }

        private void ShowDashboard()
        {
            ShowHome(); // Пока просто показываем главную
        }

        private Panel CreateStatCard(string icon, string number, string text)
        {
            Panel card = new Panel();
            card.Size = new Size(170, 100);
            card.BackColor = Color.White;
            card.Margin = new Padding(10);
            card.Padding = new Padding(15);

            // Тень
            card.Paint += (s, e) =>
            {
                Control c = (Control)s;
                using (Pen pen = new Pen(Color.FromArgb(20, 0, 0, 0), 1))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, c.Width - 1, c.Height - 1);
                }
            };

            Label lblIcon = new Label();
            lblIcon.Text = icon;
            lblIcon.Font = new Font("Segoe UI", 24);
            lblIcon.Location = new Point(10, 10);
            lblIcon.AutoSize = true;

            Label lblNumber = new Label();
            lblNumber.Text = number;
            lblNumber.Font = new Font("Segoe UI", 20, FontStyle.Bold);
            lblNumber.ForeColor = Color.FromArgb(59, 130, 246);
            lblNumber.Location = new Point(10, 45);
            lblNumber.AutoSize = true;

            Label lblText = new Label();
            lblText.Text = text;
            lblText.Font = new Font("Segoe UI", 9);
            lblText.ForeColor = Color.Gray;
            lblText.Location = new Point(10, 75);
            lblText.AutoSize = true;

            card.Controls.Add(lblIcon);
            card.Controls.Add(lblNumber);
            card.Controls.Add(lblText);

            return card;
        }
    }
}
