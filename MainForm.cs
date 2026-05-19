using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public partial class MainForm : Form
    {
        private Panel sidePanel;
        private Panel contentPanel;
        private WebView2 webView;
        private Form activeForm = null;
        private TaskCompletionSource<bool> webViewInitTask = new TaskCompletionSource<bool>();
        private bool isWebViewReady = false;

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

            this.Load += async (s, e) =>
            {
                await InitializeWebView();
                ShowHome();
            };
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

            // Для начальника (boss) — убираем dashboard, passports, budget
            if (userRole == "boss")
                return page == "home" || page == "equipment" || page == "plans" || page == "charts" || page == "boss" || page == "employees";

            // Для администратора — всё
            if (userRole == "admin")
                return true;

            return true;
        }

        private void InitializeLayout()
        {
            this.WindowState = FormWindowState.Maximized;
            this.BackColor = Color.FromArgb(240, 242, 245);
            this.Text = "Информационная система для автоматизации, планирования и учета технического обслуживания котельного оборудования";

            sidePanel = new Panel();
            sidePanel.Dock = DockStyle.Left;
            sidePanel.Width = 250;
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

            // Дашборд только для оператора, слесаря и админа (НЕ для начальника)
            if (userRole != "boss")
            {
                AddMenuButtonIfAllowed(tlp, "📊", "Дашборд", "dashboard", ref row);
            }

            tlp.Controls.Add(CreateSeparator(), 0, row++);

            // ОБОРУДОВАНИЕ
            tlp.RowCount = row + 1;
            tlp.Controls.Add(CreateHeaderLabel("ОБОРУДОВАНИЕ"), 0, row);
            tlp.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            row++;

            AddMenuButtonIfAllowed(tlp, "🔧", "Всё оборудование", "equipment", ref row);

            // Аварии и ремонты НЕ показываем начальнику
            if (userRole != "boss")
            {
                AddMenuButtonIfAllowed(tlp, "⚠️", "Аварии", "accidents", ref row);
                AddMenuButtonIfAllowed(tlp, "🔨", "Ремонты", "repairs", ref row);
            }

            // Паспорта НЕ показываем начальнику
            if (userRole != "boss")
            {
                AddMenuButtonIfAllowed(tlp, "📋", "Паспорта", "passports", ref row);
            }

            tlp.Controls.Add(CreateSeparator(), 0, row++);

            // ПЛАНИРОВАНИЕ
            tlp.RowCount = row + 1;
            tlp.Controls.Add(CreateHeaderLabel("ПЛАНИРОВАНИЕ"), 0, row);
            tlp.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            row++;

            AddMenuButtonIfAllowed(tlp, "📋", "Планы ТО", "plans", ref row);
            AddMenuButtonIfAllowed(tlp, "📈", "Графики", "charts", ref row);

            tlp.Controls.Add(CreateSeparator(), 0, row++);

            // РУКОВОДСТВО
            tlp.RowCount = row + 1;
            tlp.Controls.Add(CreateHeaderLabel("РУКОВОДСТВО"), 0, row);
            tlp.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            row++;

            AddMenuButtonIfAllowed(tlp, "👑", "Панель начальника", "boss", ref row);
            AddMenuButtonIfAllowed(tlp, "👥", "Сотрудники", "employees", ref row);

            // Бюджет НЕ показываем начальнику
            if (userRole != "boss")
            {
                AddMenuButtonIfAllowed(tlp, "💰", "Бюджет", "budget", ref row);
            }

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
                webView = new WebView2();
                webView.Dock = DockStyle.Fill;

                string userDataFolder = Path.Combine(Path.GetTempPath(), "WebView2_Data_" + Guid.NewGuid().ToString());

                var env = await CoreWebView2Environment.CreateAsync(null, userDataFolder);
                await webView.EnsureCoreWebView2Async(env);

                webView.CoreWebView2.Settings.IsScriptEnabled = true;
                webView.CoreWebView2.Settings.IsWebMessageEnabled = true;

                isWebViewReady = true;
                webViewInitTask.TrySetResult(true);

                webView.CoreWebView2.WebMessageReceived += (s, e) =>
                {
                    string message = e.TryGetWebMessageAsString();
                    if (this.IsHandleCreated)
                    {
                        this.BeginInvoke(new Action(() => HandleWebViewMessage(message)));
                    }
                };

                webView.CoreWebView2.NavigationCompleted += async (s, e) =>
                {
                    if (e.IsSuccess)
                    {
                        await SetUserRoleInWebView();
                    }
                };
            }
            catch (Exception ex)
            {
                isWebViewReady = false;
                webViewInitTask.TrySetResult(false);
                MessageBox.Show($"Ошибка инициализации WebView2: {ex.Message}\n\n" +
                    "Пожалуйста, установите Microsoft Edge WebView2 Runtime.\n" +
                    "Скачать можно по ссылке: https://developer.microsoft.com/ru-ru/microsoft-edge/webview2/",
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task LoadMainStatistics()
        {
            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    await conn.OpenAsync();

                    string sql = @"
                        SELECT 
                            COUNT(*) as total_plans,
                            SUM(CASE WHEN status = 'Завершен' THEN 1 ELSE 0 END) as completed_plans,
                            SUM(CASE WHEN status NOT IN ('Завершен', 'Отменен') AND data_okonchaniya < CURRENT_DATE THEN 1 ELSE 0 END) as overdue_plans
                        FROM plan_to
                        WHERE status != 'Отменен'";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            var stats = new
                            {
                                totalPlans = reader.GetInt32(0),
                                completedPlans = reader.GetInt32(1),
                                overduePlans = reader.GetInt32(2)
                            };
                            string json = JsonSerializer.Serialize(stats);
                            await ExecuteJsFunction("displayMainStatistics", json);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка статистики: {ex.Message}");
            }
        }

        private async Task ExecuteJsFunction(string function, string data = null)
        {
            if (webView?.CoreWebView2 != null && isWebViewReady)
            {
                try
                {
                    string js;
                    if (string.IsNullOrEmpty(data))
                        js = $"if(window.{function}) window.{function}(null);";
                    else
                    {
                        string escapedData = data.Replace("\\", "\\\\").Replace("'", "\\'");
                        js = $"if(window.{function}) window.{function}('{escapedData}');";
                    }
                    await webView.CoreWebView2.ExecuteScriptAsync(js);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Ошибка ExecuteJsFunction: {ex.Message}");
                }
            }
        }

        private async Task WaitForWebViewReady()
        {
            if (!isWebViewReady)
            {
                await webViewInitTask.Task;
            }
        }

        private async void HandleWebViewMessage(string message)
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
                            case "mainPageReady":
                                await SetUserRoleInWebView();
                                await LoadMainStatistics();
                                break;
                            case "openSection":
                                string section = root.GetProperty("section").GetString();
                                HandleMenuClick(section);
                                break;
                            case "openReport":
                                string reportType = root.GetProperty("reportType").GetString();
                                if (reportType == "spareParts")
                                {
                                    OpenChildForm(new FormBoss(connectionString, employeeId));
                                }
                                else if (reportType == "plans" || reportType == "avariya")
                                {
                                    OpenChildForm(new FormBoss(connectionString, employeeId));
                                }
                                break;
                            case "openReference":
                                string refType = root.GetProperty("refType").GetString();
                                switch (refType)
                                {
                                    case "equipment":
                                        OpenChildForm(new Form1());
                                        break;
                                    case "employees":
                                        OpenChildForm(new Form2());
                                        break;
                                }
                                break;
                            case "loadMainStatistics":
                                await LoadMainStatistics();
                                break;
                            case "exportReportDirectly":
                                string reportTypeParam = root.GetProperty("reportType").GetString();
                                await ExportReportDirectly(reportTypeParam);
                                break;
                            case "openUsers":
                                ShowPlaceholder("Управление пользователями");
                                break;
                            case "pageReady":
                                break;
                            default:
                                HandleMenuClick(action);
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

        private async Task ExportReportDirectly(string reportTypeParam)
        {
            try
            {
                // Устанавливаем даты (последний месяц)
                string endDate = DateTime.Now.ToString("yyyy-MM-dd");
                string startDate = DateTime.Now.AddMonths(-1).ToString("yyyy-MM-dd");

                SaveFileDialog save = new SaveFileDialog();
                save.Filter = "CSV файлы (*.csv)|*.csv";

                string sql = "";
                string fileName = "";
                string[] headers = null;

                switch (reportTypeParam)
                {
                    case "spareParts":
                        fileName = $"Отчет_по_запчастям_{DateTime.Now:yyyy-MM-dd_HH-mm}.csv";
                        sql = $@"
                    SELECT 
                        sp.naimenovanie as Наименование,
                        COALESCE(sp.artikul, '') as Артикул,
                        COALESCE(sp.edinica, 'шт') as Ед_изм,
                        1 as Количество,
                        o.nazvanie as Оборудование,
                        TO_CHAR(r.data_okonchaniya, 'DD.MM.YYYY') as Дата_использования,
                        COALESCE(r.opisanie, '') as Описание_работ
                    FROM remont r
                    JOIN plan_to p ON r.plan_id = p.id
                    JOIN oborudovanie o ON r.oborudovanie_id = o.id
                    CROSS JOIN LATERAL unnest(string_to_array(r.zamennaya_detal, ',')) as det
                    JOIN spare_parts sp ON sp.naimenovanie ILIKE TRIM(det)
                    WHERE r.zamennaya_detal IS NOT NULL AND r.zamennaya_detal != ''
                      AND DATE(r.data_okonchaniya) BETWEEN '{startDate}' AND '{endDate}'
                    ORDER BY r.data_okonchaniya DESC";
                        headers = new[] { "Наименование", "Артикул", "Ед.изм", "Количество", "Оборудование", "Дата использования", "Описание работ" };
                        break;

                    case "plans":
                        fileName = $"Отчет_по_планам_ТО_{DateTime.Now:yyyy-MM-dd_HH-mm}.csv";
                        sql = $@"
                    SELECT p.id, o.nazvanie, COALESCE(t.nazvanie,'Не указан'), 
                           TO_CHAR(p.data_nachala,'DD.MM.YYYY'), TO_CHAR(p.data_okonchaniya,'DD.MM.YYYY'), 
                           COALESCE(s.familiya||' '||s.imya||' '||s.otchestvo,'Не назначен'), p.status
                    FROM plan_to p 
                    JOIN oborudovanie o ON p.oborudovanie_id = o.id 
                    LEFT JOIN tip_to t ON p.tip_to_id = t.id 
                    LEFT JOIN sotrudniki s ON p.otvetstvenniy_id = s.id 
                    WHERE p.data_nachala BETWEEN '{startDate}' AND '{endDate}'
                    ORDER BY p.data_nachala DESC";
                        headers = new[] { "ID", "Оборудование", "Тип ТО", "Дата начала", "Дата окончания", "Ответственный", "Статус" };
                        break;

                    case "avariya":
                        fileName = $"Отчет_по_авариям_{DateTime.Now:yyyy-MM-dd_HH-mm}.csv";
                        sql = $@"
                    SELECT a.id, o.nazvanie, TO_CHAR(a.data_avarii,'DD.MM.YYYY'), 
                           COALESCE(a.opisanie,''), COALESCE(a.posledstviya,''), a.status
                    FROM avariya a
                    JOIN oborudovanie o ON a.oborudovanie_id = o.id
                    WHERE DATE(a.data_avarii) BETWEEN '{startDate}' AND '{endDate}'
                    ORDER BY a.data_avarii DESC";
                        headers = new[] { "ID", "Оборудование", "Дата аварии", "Описание", "Последствия", "Статус" };
                        break;

                    case "repairs":
                        fileName = $"Отчет_по_ремонтам_{DateTime.Now:yyyy-MM-dd_HH-mm}.csv";
                        sql = $@"
                    SELECT COALESCE(r.equipment_name,o.nazvanie), COALESCE(r.tip_name,'Не указан'),
                           TO_CHAR(p.data_nachala,'DD.MM.YYYY'), TO_CHAR(r.data_okonchaniya,'DD.MM.YYYY'),
                           COALESCE(r.sotrudnik_name,'Не указан'), COALESCE(r.opisanie,'')
                    FROM remont r
                    JOIN plan_to p ON r.plan_id = p.id
                    JOIN oborudovanie o ON p.oborudovanie_id = o.id
                    WHERE r.data_okonchaniya BETWEEN '{startDate}' AND '{endDate}'
                    ORDER BY r.data_okonchaniya DESC";
                        headers = new[] { "Оборудование", "Тип ТО", "Плановая дата", "Дата выполнения", "Исполнитель", "Описание работ" };
                        break;

                    default:
                        await ExecuteJsFunction("showError", "Неизвестный тип отчета");
                        return;
                }

                save.FileName = fileName;

                if (save.ShowDialog() == DialogResult.OK)
                {
                    using (var conn = new NpgsqlConnection(connectionString))
                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        await conn.OpenAsync();
                        using (var reader = await cmd.ExecuteReaderAsync())
                        using (var sw = new StreamWriter(save.FileName, false, Encoding.UTF8))
                        {
                            sw.WriteLine(string.Join(";", headers));
                            while (await reader.ReadAsync())
                            {
                                var values = new List<string>();
                                for (int i = 0; i < reader.FieldCount; i++)
                                {
                                    string val = reader[i]?.ToString() ?? "";
                                    if (val.Contains(";") || val.Contains("\""))
                                        val = $"\"{val.Replace("\"", "\"\"")}\"";
                                    values.Add(val);
                                }
                                sw.WriteLine(string.Join(";", values));
                            }
                        }
                    }

                    MessageBox.Show($"Отчет '{fileName}' успешно сохранен!", "Успех",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка экспорта: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            if (webView?.CoreWebView2 != null && isWebViewReady)
            {
                string roleForJs = userRole == "admin" ? "app_admin" :
                                   (userRole == "boss" ? "app_boss" :
                                   (userRole == "slesar" ? "app_slesar" : "app_operator"));

                string script = $"if(typeof setUserRole === 'function') setUserRole('{roleForJs}');";
                try
                {
                    await webView.CoreWebView2.ExecuteScriptAsync(script);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Ошибка выполнения скрипта: {ex.Message}");
                }
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
                case "charts":
                    OpenChildForm(new FormCharts(connectionString, employeeId, userLogin, userRole));
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
                default:
                    ShowHome();
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

        private void ShowDashboard()
        {
            OpenChildForm(new FormCharts(connectionString, employeeId, userLogin, userRole));
        }

        private async void ShowHome()
        {
            if (activeForm != null)
            {
                activeForm.Close();
                activeForm = null;
            }

            contentPanel.Controls.Clear();

            await WaitForWebViewReady();

            if (isWebViewReady && webView != null && webView.CoreWebView2 != null)
            {
                if (contentPanel.Controls.Contains(webView))
                    contentPanel.Controls.Remove(webView);

                contentPanel.Controls.Add(webView);

                string htmlPath = Path.Combine(webUIPath, "main.html");
                if (File.Exists(htmlPath))
                {
                    webView.CoreWebView2.Navigate($"file:///{htmlPath.Replace('\\', '/')}");
                }
                else
                {
                    ShowPlaceholder("Главное меню - файл не найден");
                }
            }
            else
            {
                ShowPlaceholder("Загрузка главного меню...");
            }
        }
    }
}