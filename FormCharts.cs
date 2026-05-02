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

                await webView.EnsureCoreWebView2Async(null);

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
                        await Task.Delay(500);
                        await LoadChartData(GetSixMonthsAgo(), GetCurrentDate());
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

        private async Task LoadChartData(string startDate, string endDate)
        {
            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    await conn.OpenAsync();

                    if (string.IsNullOrEmpty(startDate)) startDate = GetSixMonthsAgo();
                    if (string.IsNullOrEmpty(endDate)) endDate = GetCurrentDate();

                    // 1. Общая статистика
                    string statsSql = @"
                        SELECT 
                            COUNT(*) as total_plans,
                            COUNT(CASE WHEN status = 'Завершен' THEN 1 END) as completed,
                            COUNT(CASE WHEN status = 'В работе' THEN 1 END) as in_progress,
                            COUNT(CASE WHEN status = 'Просрочен' THEN 1 END) as overdue
                        FROM plan_to
                        WHERE DATE(data_nachala) BETWEEN @start AND @end";

                    int totalPlans = 0, completed = 0, inProgress = 0, overdue = 0;

                    using (var cmd = new NpgsqlCommand(statsSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@start", DateTime.Parse(startDate));
                        cmd.Parameters.AddWithValue("@end", DateTime.Parse(endDate));
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                totalPlans = reader.GetInt32(0);
                                completed = reader.GetInt32(1);
                                inProgress = reader.GetInt32(2);
                                overdue = reader.GetInt32(3);
                            }
                        }
                    }

                    // 2. Статистика по сотрудникам
                    string workerStatsSql = @"
                        SELECT 
                            COALESCE(s.familiya || ' ' || s.imya, 'Не назначен') as name,
                            COUNT(p.id) as assigned,
                            COUNT(CASE WHEN p.status = 'Завершен' THEN 1 END) as completed
                        FROM plan_to p
                        LEFT JOIN sotrudniki s ON p.otvetstvenniy_id = s.id
                        WHERE DATE(p.data_nachala) BETWEEN @start AND @end
                        GROUP BY s.familiya, s.imya
                        ORDER BY assigned DESC
                        LIMIT 5";

                    var workerStats = new List<object>();
                    using (var cmd = new NpgsqlCommand(workerStatsSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@start", DateTime.Parse(startDate));
                        cmd.Parameters.AddWithValue("@end", DateTime.Parse(endDate));
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                workerStats.Add(new { name = reader.GetString(0), assigned = reader.GetInt32(1), completed = reader.GetInt32(2) });
                            }
                        }
                    }

                    // 3. Динамика по месяцам
                    string monthlySql = @"
                        SELECT 
                            TO_CHAR(DATE_TRUNC('month', data_nachala), 'YYYY-MM') as month,
                            COUNT(*) as planned_total,
                            COUNT(CASE WHEN status = 'Завершен' THEN 1 END) as completed,
                            ROUND(CASE WHEN COUNT(*) > 0 THEN COUNT(CASE WHEN status = 'Завершен' THEN 1 END) * 100.0 / COUNT(*) ELSE 0 END, 1) as completion_percent,
                            COALESCE(SUM(stoimost), 0) as total_cost
                        FROM plan_to
                        WHERE DATE(data_nachala) BETWEEN @start AND @end
                        GROUP BY DATE_TRUNC('month', data_nachala)
                        ORDER BY month ASC";

                    var monthlyData = new List<object>();
                    using (var cmd = new NpgsqlCommand(monthlySql, conn))
                    {
                        cmd.Parameters.AddWithValue("@start", DateTime.Parse(startDate));
                        cmd.Parameters.AddWithValue("@end", DateTime.Parse(endDate));
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                string monthStr = reader.GetString(0);
                                string[] parts = monthStr.Split('-');
                                string monthName = GetMonthName(int.Parse(parts[1])) + " " + parts[0];
                                monthlyData.Add(new
                                {
                                    month = monthName,
                                    planned = reader.GetInt32(1),
                                    completed = reader.GetInt32(2),
                                    percent = reader.GetDecimal(3),
                                    cost = reader.GetDecimal(4)
                                });
                            }
                        }
                    }

                    // 4. Топ оборудования по авариям
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
                        cmd.Parameters.AddWithValue("@start", DateTime.Parse(startDate));
                        cmd.Parameters.AddWithValue("@end", DateTime.Parse(endDate));
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
                        completedPlans = completed,
                        inProgressPlans = inProgress,
                        overduePlans = overdue,
                        workerStats,
                        monthlyData,
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
                    string js = $"window.receiveFromCSharp('{command}', {data})";
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
                            await LoadChartData(startDate, endDate);
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