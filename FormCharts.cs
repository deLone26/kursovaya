using Microsoft.Web.WebView2.WinForms;
using Microsoft.Web.WebView2.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using Npgsql;

namespace WindowsFormsApp1
{
    public partial class FormCharts : Form
    {
        private readonly string connectionString;
        private int currentEmployeeId;
        private string currentUserLogin;
        private string currentUserRole;

        private WebView2 webView;
        private string webUIPath;
        private bool isWebViewInitialized = false;

        public FormCharts(string connString, int employeeId, string userLogin, string userRole)
        {
            this.connectionString = connString;
            this.currentEmployeeId = employeeId;
            this.currentUserLogin = userLogin;
            this.currentUserRole = userRole;

            InitializeComponent();

            webUIPath = @"C:\Users\Daniil\Desktop\4\kursovaya3\kursovaya\WebUI";

            this.Text = "Графики и аналитика";
            this.WindowState = FormWindowState.Maximized;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Controls.Clear();

            InitializeWebView();
        }

        private async void InitializeWebView()
        {
            try
            {
                webView = new WebView2();
                webView.Dock = DockStyle.Fill;
                this.Controls.Add(webView);

                string userDataFolder = Path.Combine(Path.GetTempPath(), "WebView2Charts_" + DateTime.Now.Ticks);
                var env = await CoreWebView2Environment.CreateAsync(null, userDataFolder);
                await webView.EnsureCoreWebView2Async(env);

                webView.CoreWebView2.Settings.IsScriptEnabled = true;
                webView.CoreWebView2.Settings.IsWebMessageEnabled = true;
                webView.CoreWebView2.WebMessageReceived += OnWebMessageReceived;

                string htmlPath = Path.Combine(webUIPath, "charts.html");

                if (File.Exists(htmlPath))
                {
                    webView.CoreWebView2.Navigate($"file:///{htmlPath.Replace('\\', '/')}");
                    isWebViewInitialized = true;

                    webView.CoreWebView2.NavigationCompleted += async (s, e) =>
                    {
                        if (e.IsSuccess)
                        {
                            await Task.Delay(500);
                            await LoadEmployees();
                            await LoadChartData(GetSixMonthsAgo(), GetCurrentDate(), 0);
                        }
                    };
                }
                else
                {
                    MessageBox.Show($"Файл не найден: {htmlPath}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка WebView2: {ex.Message}");
            }
        }

        private string GetCurrentDate()
        {
            return DateTime.Now.ToString("yyyy-MM-dd");
        }

        private string GetSixMonthsAgo()
        {
            return new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddMonths(-5).ToString("yyyy-MM-dd");
        }

        private string GetMonthName(int month)
        {
            string[] months = { "Янв", "Фев", "Мар", "Апр", "Май", "Июн", "Июл", "Авг", "Сен", "Окт", "Ноя", "Дек" };
            return months[month - 1];
        }

        private async Task LoadEmployees()
        {
            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    await conn.OpenAsync();
                    string sql = @"
                        SELECT id, familiya || ' ' || LEFT(imya, 1) || '.' || LEFT(COALESCE(otchestvo, ''), 1) || '.' as name
                        FROM sotrudniki 
                        WHERE dolzhnost ILIKE '%слесар%'
                        ORDER BY familiya";

                    var employees = new List<object>();
                    using (var cmd = new NpgsqlCommand(sql, conn))
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            employees.Add(new { id = reader.GetInt32(0), name = reader.GetString(1) });
                        }
                    }
                    string json = JsonSerializer.Serialize(employees);
                    SendToWebView("fillEmployees", json);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadEmployees error: {ex.Message}");
            }
        }

        private async Task LoadChartData(string startDate, string endDate, int employeeId)
        {
            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    await conn.OpenAsync();

                    if (string.IsNullOrEmpty(startDate)) startDate = GetSixMonthsAgo();
                    if (string.IsNullOrEmpty(endDate)) endDate = GetCurrentDate();

                    string employeeFilter = "";
                    if (employeeId > 0)
                    {
                        employeeFilter = " AND p.otvetstvenniy_id = " + employeeId;
                    }

                    // 1. ТЕКУЩИЕ АКТИВНЫЕ ПЛАНЫ ТО (для круговой диаграммы) - без параметров дат
                    string activePlansSql = $@"
                        SELECT 
                            COUNT(CASE WHEN p.status = 'Зарегистрирован' THEN 1 END) as registered,
                            COUNT(CASE WHEN p.status = 'В работе' THEN 1 END) as in_progress,
                            COUNT(CASE WHEN p.status = 'Просрочен' THEN 1 END) as overdue
                        FROM plan_to p
                        WHERE p.status IN ('Зарегистрирован', 'В работе', 'Просрочен')
                          AND p.avariya_id IS NULL
                          {employeeFilter}";

                    int registered = 0, inProgress = 0, overdue = 0;

                    using (var cmd = new NpgsqlCommand(activePlansSql, conn))
                    {
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                registered = reader.GetInt32(0);
                                inProgress = reader.GetInt32(1);
                                overdue = reader.GetInt32(2);
                            }
                        }
                    }

                    int totalPlans = registered + inProgress + overdue;

                    // 2. Общая статистика по авариям (из истории аварий)
                    string accidentStatsSql = @"
                        SELECT 
                            COUNT(*) as total,
                            COUNT(CASE WHEN p.data_nachala < r.data_okonchaniya THEN 1 END) as overdue
                        FROM avariya a
                        JOIN plan_to p ON a.id = p.avariya_id
                        JOIN remont r ON p.id = r.plan_id
                        WHERE a.status = 'Завершена'
                          AND DATE(r.data_okonchaniya) BETWEEN @start AND @end";

                    int totalAccidents = 0, overdueAccidents = 0;
                    using (var cmd = new NpgsqlCommand(accidentStatsSql, conn))
                    {
                        DateTime start = DateTime.Parse(startDate);
                        DateTime end = DateTime.Parse(endDate);
                        cmd.Parameters.AddWithValue("@start", start);
                        cmd.Parameters.AddWithValue("@end", end);
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                totalAccidents = reader.GetInt32(0);
                                overdueAccidents = reader.GetInt32(1);
                            }
                        }
                    }

                    // 3. Статистика по сотрудникам (количество активных задач) - без параметров дат
                    string workerStatsSql = $@"
                        SELECT 
                            COALESCE(s.familiya || ' ' || LEFT(s.imya, 1) || '.' || LEFT(COALESCE(s.otchestvo, ''), 1) || '.', 'Не назначен') as name,
                            COUNT(p.id) as assigned
                        FROM plan_to p
                        LEFT JOIN sotrudniki s ON p.otvetstvenniy_id = s.id
                        WHERE p.status IN ('Зарегистрирован', 'В работе', 'Просрочен')
                          AND p.avariya_id IS NULL
                          {employeeFilter}
                        GROUP BY s.familiya, s.imya, s.otchestvo
                        ORDER BY assigned DESC
                        LIMIT 10";

                    var workerStats = new List<object>();
                    using (var cmd = new NpgsqlCommand(workerStatsSql, conn))
                    {
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                workerStats.Add(new { name = reader.GetString(0), assigned = reader.GetInt32(1) });
                            }
                        }
                    }

                    // 4. Статистика по сотрудникам с разбивкой по статусам - без параметров дат
                    string workerStatusSql = $@"
                        SELECT 
                            COALESCE(s.familiya || ' ' || LEFT(s.imya, 1) || '.' || LEFT(COALESCE(s.otchestvo, ''), 1) || '.', 'Не назначен') as name,
                            COUNT(CASE WHEN p.status = 'Зарегистрирован' THEN 1 END) as registered,
                            COUNT(CASE WHEN p.status = 'В работе' THEN 1 END) as in_progress,
                            COUNT(CASE WHEN p.status = 'Просрочен' THEN 1 END) as overdue
                        FROM plan_to p
                        LEFT JOIN sotrudniki s ON p.otvetstvenniy_id = s.id
                        WHERE p.status IN ('Зарегистрирован', 'В работе', 'Просрочен')
                          AND p.avariya_id IS NULL
                          {employeeFilter}
                        GROUP BY s.familiya, s.imya, s.otchestvo
                        ORDER BY (COUNT(CASE WHEN p.status = 'Просрочен' THEN 1 END)) DESC
                        LIMIT 10";

                    var workerStatusStats = new List<object>();
                    using (var cmd = new NpgsqlCommand(workerStatusSql, conn))
                    {
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                workerStatusStats.Add(new
                                {
                                    name = reader.GetString(0),
                                    registered = reader.GetInt32(1),
                                    inProgress = reader.GetInt32(2),
                                    overdue = reader.GetInt32(3)
                                });
                            }
                        }
                    }

                    // 5. Динамика просрочек ТО по месяцам (из истории ремонтов)
                    string monthlySql = @"
                        SELECT 
                            DATE_TRUNC('month', p.data_nachala) as month,
                            COUNT(*) as total,
                            COUNT(CASE WHEN p.data_nachala < r.data_okonchaniya THEN 1 END) as overdue_count,
                            ROUND(CASE WHEN COUNT(*) > 0 THEN COUNT(CASE WHEN p.data_nachala >= r.data_okonchaniya THEN 1 END) * 100.0 / COUNT(*) ELSE 0 END, 1) as completion_percent
                        FROM remont r
                        JOIN plan_to p ON r.plan_id = p.id
                        WHERE DATE(p.data_nachala) BETWEEN @start AND @end
                        GROUP BY DATE_TRUNC('month', p.data_nachala)
                        ORDER BY month ASC";

                    var monthlyData = new List<object>();
                    using (var cmd = new NpgsqlCommand(monthlySql, conn))
                    {
                        DateTime start = DateTime.Parse(startDate);
                        DateTime end = DateTime.Parse(endDate);
                        cmd.Parameters.AddWithValue("@start", start);
                        cmd.Parameters.AddWithValue("@end", end);
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                DateTime monthDate = reader.GetDateTime(0);
                                string monthName = GetMonthName(monthDate.Month) + " " + monthDate.Year;
                                monthlyData.Add(new
                                {
                                    month = monthName,
                                    total = reader.GetInt32(1),
                                    overdueCount = reader.GetInt32(2),
                                    percent = reader.GetDecimal(3)
                                });
                            }
                        }
                    }

                    // 6. Динамика просрочек аварий по месяцам (из истории аварий)
                    string accidentMonthlySql = @"
                        SELECT 
                            DATE_TRUNC('month', r.data_okonchaniya) as month,
                            COUNT(*) as total,
                            COUNT(CASE WHEN p.data_nachala < r.data_okonchaniya THEN 1 END) as overdue_count
                        FROM avariya a
                        JOIN plan_to p ON a.id = p.avariya_id
                        JOIN remont r ON p.id = r.plan_id
                        WHERE a.status = 'Завершена'
                          AND DATE(r.data_okonchaniya) BETWEEN @start AND @end
                        GROUP BY DATE_TRUNC('month', r.data_okonchaniya)
                        ORDER BY month ASC";

                    var accidentMonthlyData = new List<object>();
                    using (var cmd = new NpgsqlCommand(accidentMonthlySql, conn))
                    {
                        DateTime start = DateTime.Parse(startDate);
                        DateTime end = DateTime.Parse(endDate);
                        cmd.Parameters.AddWithValue("@start", start);
                        cmd.Parameters.AddWithValue("@end", end);
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                DateTime monthDate = reader.GetDateTime(0);
                                string monthName = GetMonthName(monthDate.Month) + " " + monthDate.Year;
                                accidentMonthlyData.Add(new
                                {
                                    month = monthName,
                                    total = reader.GetInt32(1),
                                    overdueCount = reader.GetInt32(2)
                                });
                            }
                        }
                    }

                    // 7. Топ оборудования по авариям
                    string accidentsSql = @"
                        SELECT o.nazvanie, COUNT(*) as cnt
                        FROM avariya a
                        JOIN oborudovanie o ON a.oborudovanie_id = o.id
                        WHERE DATE(a.data_avarii) BETWEEN @start AND @end
                        GROUP BY o.nazvanie
                        ORDER BY cnt DESC
                        LIMIT 5";

                    var topAccidents = new List<object>();
                    using (var cmd = new NpgsqlCommand(accidentsSql, conn))
                    {
                        DateTime start = DateTime.Parse(startDate);
                        DateTime end = DateTime.Parse(endDate);
                        cmd.Parameters.AddWithValue("@start", start);
                        cmd.Parameters.AddWithValue("@end", end);
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                topAccidents.Add(new { name = reader.GetString(0), count = reader.GetInt32(1) });
                            }
                        }
                    }

                    var result = new
                    {
                        totalPlans,
                        registeredPlans = registered,
                        inProgressPlans = inProgress,
                        overduePlans = overdue,
                        totalAccidents = totalAccidents,
                        overdueAccidents = overdueAccidents,
                        workerStats,
                        workerStatusStats,
                        monthlyData,
                        accidentMonthlyData,
                        topAccidentsByEquipment = topAccidents
                    };

                    string jsonData = JsonSerializer.Serialize(result, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    });

                    SendToWebView("chartData", jsonData);
                }
            }
            catch (Exception ex)
            {
                SendToWebView("showError", $"Ошибка загрузки данных: {ex.Message}");
            }
        }

        private void SendToWebView(string command, string data)
        {
            try
            {
                if (isWebViewInitialized && webView?.CoreWebView2 != null)
                {
                    string escapedData = data?.Replace("\\", "\\\\").Replace("'", "\\'") ?? "null";
                    string js = $"window.receiveFromCSharp('{command}', '{escapedData}');";
                    webView.CoreWebView2.ExecuteScriptAsync(js);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка отправки: {ex.Message}");
            }
        }

        private async void OnWebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            string message = e.TryGetWebMessageAsString();
            System.Diagnostics.Debug.WriteLine($"Получено от JS: {message}");

            try
            {
                using (var jsonDoc = JsonDocument.Parse(message))
                {
                    var root = jsonDoc.RootElement;
                    string action = root.GetProperty("action").GetString();

                    switch (action)
                    {
                        case "getChartData":
                            string startDate = root.TryGetProperty("startDate", out var s) ? s.GetString() : "";
                            string endDate = root.TryGetProperty("endDate", out var ed) ? ed.GetString() : "";
                            int employeeId = root.TryGetProperty("employeeId", out var emp) ? emp.GetInt32() : 0;
                            await LoadChartData(startDate, endDate, employeeId);
                            break;
                        case "getEmployees":
                            await LoadEmployees();
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка парсинга: {ex.Message}");
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            webView?.Dispose();
            base.OnFormClosing(e);
        }
    }
}