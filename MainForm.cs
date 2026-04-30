using Microsoft.Web.WebView2.WinForms;
using Microsoft.Web.WebView2.Core;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using Npgsql;

namespace WindowsFormsApp1
{
    public partial class MainForm : Form
    {
        private Panel sidePanel;
        private Panel contentPanel;
        private WebView2 webView;
        private Form activeForm = null;
        private WebView2 activeWebView = null;

        private bool isCollapsed = false;
        private int expandedWidth = 250;
        private int collapsedWidth = 70;

        private string connectionString;
        private int employeeId;
        private string userLogin;
        private string userRole;
        private string webUIPath = @"C:\Users\Daniil\Desktop\4\kursovaya3\kursovaya\WebUI";

        public MainForm(string connString, int userId)
        {
            this.connectionString = connString;
            this.employeeId = userId;
            this.userLogin = GetLoginByEmployeeId(userId);
            this.userRole = GetUserRole();

            InitializeComponent();
            InitializeLayout();
            CreateMenuButtons();
            this.Load += async (s, e) => await InitializeWebView();
            ShowHome();
        }

        private string GetLoginByEmployeeId(int empId)
        {
            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();
                    string sql = "SELECT login FROM users WHERE sotrudnik_id = @id";
                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", empId);
                        return cmd.ExecuteScalar()?.ToString() ?? "user";
                    }
                }
            }
            catch { return "user"; }
        }

        private int GetEmployeeIdByLogin(string login)
        {
            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();
                    string sql = "SELECT sotrudnik_id FROM users WHERE login = @login";
                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@login", login);
                        var result = cmd.ExecuteScalar();
                        return result != null ? Convert.ToInt32(result) : -1;
                    }
                }
            }
            catch { return -1; }
        }

        private string GetUserRole()
        {
            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();
                    string sql = @"
                        SELECT g.rolname
                        FROM pg_roles u
                        JOIN pg_auth_members m ON u.oid = m.member
                        JOIN pg_roles g ON m.roleid = g.oid
                        WHERE u.rolname = @login AND g.rolname LIKE 'app_%'";
                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@login", userLogin);
                        var result = cmd.ExecuteScalar();
                        if (result?.ToString() == "app_admin") return "admin";
                        if (result?.ToString() == "app_boss") return "boss";
                        if (result?.ToString() == "app_slesar") return "slesar";
                        return "operator";
                    }
                }
            }
            catch { return "operator"; }
        }

        private bool IsSectionAllowed(string page)
        {
            // Для оператора
            if (userRole == "operator")
                return page == "home" || page == "dashboard" || page == "accidents";

            // Для слесаря
            if (userRole == "slesar")
                return page == "home" || page == "dashboard" || page == "repairs" || page == "plans";

            // Для начальника (boss) — всё, кроме accidents и repairs
            if (userRole == "boss")
                return page != "accidents" && page != "repairs";

            // Для администратора — всё
            if (userRole == "admin")
                return true;

            // По умолчанию всё разрешено
            return true;
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

            AddMenuButtonIfAllowed(tlp, "🏠", "Главная", "home", ref row);
            AddMenuButtonIfAllowed(tlp, "📊", "Дашборд", "dashboard", ref row);

            tlp.Controls.Add(CreateSeparator(), 0, row++);

            // ОБОРУДОВАНИЕ
            tlp.RowCount = row + 1;
            tlp.Controls.Add(CreateHeaderLabel("ОБОРУДОВАНИЕ"), 0, row);
            tlp.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            row++;

            AddMenuButtonIfAllowed(tlp, "🔧", "Всё оборудование", "equipment", ref row);

            // Для начальника (boss) НЕ показываем Аварии и Ремонты
            if (userRole != "boss")
            {
                AddMenuButtonIfAllowed(tlp, "⚠️", "Аварии", "accidents", ref row);
                AddMenuButtonIfAllowed(tlp, "🔨", "Ремонты", "repairs", ref row);
            }

            AddMenuButtonIfAllowed(tlp, "📋", "Паспорта", "passports", ref row);

            tlp.Controls.Add(CreateSeparator(), 0, row++);

            // ПЛАНИРОВАНИЕ
            tlp.RowCount = row + 1;
            tlp.Controls.Add(CreateHeaderLabel("ПЛАНИРОВАНИЕ"), 0, row);
            tlp.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            row++;

            AddMenuButtonIfAllowed(tlp, "📅", "Планы ТО", "plans", ref row);
            AddMenuButtonIfAllowed(tlp, "📈", "Графики", "schedules", ref row);

            tlp.Controls.Add(CreateSeparator(), 0, row++);

            // РУКОВОДСТВО
            tlp.RowCount = row + 1;
            tlp.Controls.Add(CreateHeaderLabel("РУКОВОДСТВО"), 0, row);
            tlp.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            row++;

            AddMenuButtonIfAllowed(tlp, "👑", "Панель начальника", "boss", ref row);
            AddMenuButtonIfAllowed(tlp, "👥", "Сотрудники", "employees", ref row);
            AddMenuButtonIfAllowed(tlp, "💰", "Бюджет", "budget", ref row);

            tlp.Controls.Add(CreateSeparator(), 0, row++);

            // ВЫХОД
            tlp.RowCount = row + 1;
            Button btnExit = CreateMenuButton("🚪", "Выход", "exit");
            btnExit.BackColor = Color.FromArgb(220, 53, 69);
            tlp.Controls.Add(btnExit, 0, row);
            tlp.RowStyles.Add(new RowStyle(SizeType.Absolute, 45));

            sidePanel.Controls.Add(tlp);
        }

        private void AddMenuButtonIfAllowed(TableLayoutPanel tlp, string icon, string text, string tag, ref int row)
        {
            if (IsSectionAllowed(tag))
            {
                tlp.RowCount = row + 1;
                tlp.Controls.Add(CreateMenuButton(icon, text, tag), 0, row);
                tlp.RowStyles.Add(new RowStyle(SizeType.Absolute, 45));
                row++;
            }
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

        private async Task InitializeWebView()
        {
            try
            {
                activeWebView = new WebView2();
                activeWebView.Dock = DockStyle.Fill;
                contentPanel.Controls.Clear();
                contentPanel.Controls.Add(activeWebView);

                await activeWebView.EnsureCoreWebView2Async(null);

                activeWebView.CoreWebView2.Settings.IsScriptEnabled = true;
                activeWebView.CoreWebView2.Settings.IsWebMessageEnabled = true;

                string htmlPath = Path.Combine(webUIPath, "menu.html");

                if (File.Exists(htmlPath))
                {
                    activeWebView.CoreWebView2.Navigate($"file:///{htmlPath.Replace('\\', '/')}");
                }
                else
                {
                    MessageBox.Show($"Файл меню не найден: {htmlPath}");
                }

                activeWebView.CoreWebView2.WebMessageReceived += (s, e) =>
                {
                    string message = e.TryGetWebMessageAsString();
                    if (this.IsHandleCreated)
                    {
                        this.BeginInvoke(new Action(() => HandleWebViewMessage(message)));
                    }
                };

                activeWebView.CoreWebView2.NavigationCompleted += async (s, e) =>
                {
                    await Task.Delay(500);
                    await SetUserRoleInWebView();
                };
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка WebView2: {ex.Message}");
            }
        }

        private void HandleWebViewMessage(string message)
        {
            try
            {
                if (message.StartsWith("{"))
                {
                    using (JsonDocument doc = JsonDocument.Parse(message))
                    {
                        JsonElement root = doc.RootElement;
                        string action = root.GetProperty("action").GetString();

                        switch (action)
                        {
                            case "openPlansTO":
                                OpenPlansTOForm();
                                break;
                            case "openEmployees":
                                OpenChildForm(new Form2());
                                break;
                            case "openEquipment":
                                OpenChildForm(new Form1());
                                break;
                            case "openAccidents":
                                OpenChildForm(new FormAccidents(employeeId, userLogin, userRole));
                                break;
                            case "openRepairs":
                                OpenChildForm(new FormRepairs(connectionString, employeeId, userLogin, userRole, GetEmployeeIdByLogin(userLogin)));
                                break;
                            case "openReports":
                                ShowPlaceholder("Отчеты");
                                break;
                            case "openUsers":
                                ShowPlaceholder("Управление пользователями");
                                break;
                            default:
                                HandleMenuClick(message);
                                break;
                        }
                    }
                }
                else
                {
                    HandleMenuClick(message);
                }
            }
            catch (JsonException)
            {
                HandleMenuClick(message);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка обработки сообщения: {ex.Message}");
            }
        }

        private void OpenPlansTOForm()
        {
            try
            {
                FormPlansTO plansForm = new FormPlansTO(connectionString, employeeId);

                if (activeForm != null)
                {
                    activeForm.Close();
                    activeForm = null;
                }

                contentPanel.Controls.Clear();
                plansForm.TopLevel = false;
                plansForm.FormBorderStyle = FormBorderStyle.None;
                plansForm.Dock = DockStyle.Fill;
                contentPanel.Controls.Add(plansForm);
                plansForm.Show();
                activeForm = plansForm;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка открытия формы Планов ТО: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task SetUserRoleInWebView()
        {
            if (activeWebView?.CoreWebView2 != null)
            {
                string script = $"if(typeof setUserRole === 'function') setUserRole('{userRole}');";
                await activeWebView.CoreWebView2.ExecuteScriptAsync(script);
            }
        }

        private void HandleMenuClick(string page)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action<string>(HandleMenuClick), page);
                return;
            }

            if (this.IsDisposed) return;

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
                    OpenChildForm(new FormAccidents(employeeId, userLogin, userRole));
                    break;
                case "repairs":
                    OpenChildForm(new FormRepairs(connectionString, employeeId, userLogin, userRole, GetEmployeeIdByLogin(userLogin)));
                    break;
                case "passports":
                    ShowPlaceholder("Паспорта оборудования");
                    break;
                case "plans":
                    OpenPlansTOForm();
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
            {
                activeForm.Close();
                activeForm = null;
            }

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
            {
                activeForm.Close();
                activeForm = null;
            }

            contentPanel.Controls.Clear();

            if (activeWebView != null && activeWebView.CoreWebView2 != null)
            {
                contentPanel.Controls.Add(activeWebView);
                string htmlPath = Path.Combine(webUIPath, "menu.html");
                if (File.Exists(htmlPath))
                {
                    activeWebView.CoreWebView2.Navigate($"file:///{htmlPath.Replace('\\', '/')}");
                }
            }
        }

        private void ShowDashboard()
        {
            ShowHome();
        }
    }
}
