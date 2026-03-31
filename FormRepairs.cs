using Microsoft.Web.WebView2.WinForms;
using Microsoft.Web.WebView2.Core;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using Npgsql;

namespace WindowsFormsApp1
{
    public partial class FormRepairs : Form
    {
        private readonly string connectionString;
        private int currentUserId;
        private string currentUserLogin;
        private string currentUserRole;
        private int currentEmployeeId;

        private WebView2 webView;
        private string webUIPath;
        private bool isWebViewInitialized = false;

        public FormRepairs(string connString, int userId, string userLogin, string userRole, int employeeId)
        {
            this.connectionString = connString;
            this.currentUserId = userId;
            this.currentUserLogin = userLogin;
            this.currentUserRole = userRole;
            this.currentEmployeeId = employeeId;

            InitializeComponent();

            webUIPath = @"C:\Users\Daniil\Desktop\4\kursovaya3\kursovaya\WebUI";
            this.Text = "Мои задания - Слесарь";
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

                string htmlPath = Path.Combine(webUIPath, "repairs.html");

                if (File.Exists(htmlPath))
                {
                    webView.CoreWebView2.Navigate($"file:///{htmlPath}");
                    isWebViewInitialized = true;

                    webView.CoreWebView2.NavigationCompleted += async (s, e) =>
                    {
                        await Task.Delay(500);
                        await LoadInitialData();
                        await SetCurrentUserInWebView();
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

        private async Task SetCurrentUserInWebView()
        {
            if (isWebViewInitialized && webView?.CoreWebView2 != null)
            {
                string script = $"setCurrentUser({currentEmployeeId}, '{currentUserLogin}', '{currentUserRole}');";
                await webView.CoreWebView2.ExecuteScriptAsync(script);
            }
        }

        private async Task LoadInitialData()
        {
            await LoadMyTasks();
            await LoadStatistics();
        }

        private async Task LoadMyTasks(string startDate = "", string endDate = "", bool showAll = false)
        {
            try
            {
                var tasks = new List<object>();

                using (var conn = new NpgsqlConnection(connectionString))
                {
                    await conn.OpenAsync();

                    string sql = @"
                        SELECT 
                            p.id,
                            o.nazvanie AS equipment,
                            COALESCE(t.nazvanie, 'Не указан') AS tip,
                            p.data_nachala AS start_date,
                            p.data_okonchaniya AS end_date,
                            CASE 
                                WHEN p.status = 'Просрочен' THEN '🔴 Просрочен'
                                WHEN p.status = 'Завершен' THEN '✅ Завершен'
                                WHEN p.status = 'В работе' THEN '⚙️ В работе'
                                WHEN p.status = 'Запланирован' THEN '📋 Запланирован'
                                ELSE COALESCE(p.status, 'Не указан')
                            END AS status,
                            p.stoimost AS cost,
                            r.id AS repair_id,
                            r.opisanie AS completion_description,
                            r.data_nachala AS actual_start,
                            r.data_okonchaniya AS actual_end,
                            p.is_overdue
                        FROM plan_to p
                        JOIN oborudovanie o ON p.oborudovanie_id = o.id
                        LEFT JOIN tip_to t ON p.tip_to_id = t.id
                        LEFT JOIN remont r ON p.id = r.plan_id
                        WHERE p.otvetstvenniy_id = @employee_id";

                    if (!showAll && !string.IsNullOrEmpty(startDate) && !string.IsNullOrEmpty(endDate))
                    {
                        sql += " AND DATE(p.data_nachala) BETWEEN @start AND @end";
                    }

                    sql += " ORDER BY p.is_overdue DESC, p.data_nachala DESC";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@employee_id", currentEmployeeId);

                        if (!showAll && !string.IsNullOrEmpty(startDate) && !string.IsNullOrEmpty(endDate))
                        {
                            cmd.Parameters.AddWithValue("@start", DateTime.Parse(startDate).Date);
                            cmd.Parameters.AddWithValue("@end", DateTime.Parse(endDate).Date.AddDays(1).AddSeconds(-1));
                        }

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                tasks.Add(new
                                {
                                    id = reader.GetInt32(0),
                                    equipment = reader.GetString(1),
                                    tip = reader.GetString(2),
                                    start_date = reader.GetDateTime(3).ToString("dd.MM.yyyy"),
                                    end_date = reader.IsDBNull(4) ? "" : reader.GetDateTime(4).ToString("dd.MM.yyyy"),
                                    status = reader.GetString(5),
                                    cost = reader.GetDecimal(6).ToString("N2"),
                                    repair_id = reader.IsDBNull(7) ? 0 : reader.GetInt32(7),
                                    completion_description = reader.IsDBNull(8) ? "" : reader.GetString(8),
                                    actual_start = reader.IsDBNull(9) ? "" : reader.GetDateTime(9).ToString("dd.MM.yyyy"),
                                    actual_end = reader.IsDBNull(10) ? "" : reader.GetDateTime(10).ToString("dd.MM.yyyy"),
                                    is_overdue = reader.GetBoolean(11)
                                });
                            }
                        }
                    }
                }

                string json = JsonSerializer.Serialize(tasks, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                SendToWebView("displayTasks", json);
                await LoadStatistics();
            }
            catch (Exception ex)
            {
                SendToWebView("showError", "Ошибка загрузки заданий: " + ex.Message);
            }
        }

        private async Task LoadStatistics()
        {
            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    await conn.OpenAsync();
                    string sql = @"
                        SELECT 
                            COUNT(*) AS total,
                            COUNT(CASE WHEN status = 'Завершен' THEN 1 END) AS completed,
                            COUNT(CASE WHEN status = 'В работе' THEN 1 END) AS in_progress,
                            COUNT(CASE WHEN status = 'Просрочен' THEN 1 END) AS overdue
                        FROM plan_to
                        WHERE otvetstvenniy_id = @employee_id";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@employee_id", currentEmployeeId);

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                var stats = new
                                {
                                    total = reader.GetInt32(0),
                                    completed = reader.GetInt32(1),
                                    inProgress = reader.GetInt32(2),
                                    overdue = reader.GetInt32(3)
                                };

                                string json = JsonSerializer.Serialize(stats, new JsonSerializerOptions
                                {
                                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                                });

                                SendToWebView("updateStatistics", json);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка статистики: {ex.Message}");
            }
        }

        private async Task CompleteTask(JsonElement root)
        {
            try
            {
                int planId = root.GetProperty("id").GetInt32();
                string description = root.GetProperty("description").GetString();
                string actualStartDate = root.GetProperty("actualStartDate").GetString();
                string actualEndDate = root.GetProperty("actualEndDate").GetString();

                using (var conn = new NpgsqlConnection(connectionString))
                {
                    await conn.OpenAsync();

                    using (var transaction = await conn.BeginTransactionAsync())
                    {
                        try
                        {
                            string sqlPlan = @"
                                UPDATE plan_to SET 
                                    status = 'Завершен',
                                    is_overdue = FALSE
                                WHERE id = @id";

                            using (var cmd = new NpgsqlCommand(sqlPlan, conn))
                            {
                                cmd.Parameters.AddWithValue("@id", planId);
                                await cmd.ExecuteNonQueryAsync();
                            }

                            string sqlRemont = @"
                                INSERT INTO remont 
                                (oborudovanie_id, sotrudnik_id, data_nachala, data_okonchaniya, opisanie, plan_id)
                                SELECT 
                                    oborudovanie_id, 
                                    @sotrudnik_id, 
                                    @actual_start, 
                                    @actual_end, 
                                    @opisanie,
                                    @plan_id
                                FROM plan_to 
                                WHERE id = @plan_id";

                            using (var cmd = new NpgsqlCommand(sqlRemont, conn))
                            {
                                cmd.Parameters.AddWithValue("@sotrudnik_id", currentEmployeeId);
                                cmd.Parameters.AddWithValue("@actual_start", DateTime.Parse(actualStartDate));
                                cmd.Parameters.AddWithValue("@actual_end", string.IsNullOrEmpty(actualEndDate) ? DBNull.Value : DateTime.Parse(actualEndDate));
                                cmd.Parameters.AddWithValue("@opisanie", description);
                                cmd.Parameters.AddWithValue("@plan_id", planId);

                                await cmd.ExecuteNonQueryAsync();
                            }

                            await transaction.CommitAsync();
                        }
                        catch
                        {
                            await transaction.RollbackAsync();
                            throw;
                        }
                    }
                }

                SendToWebView("showSuccess", "Задание успешно выполнено!");
                await LoadMyTasks();
            }
            catch (Exception ex)
            {
                SendToWebView("showError", "Ошибка выполнения задания: " + ex.Message);
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
                var jsonDoc = JsonDocument.Parse(message);
                var root = jsonDoc.RootElement;
                string action = root.GetProperty("action").GetString();

                switch (action)
                {
                    case "loadTasks":
                        string startDate = root.TryGetProperty("startDate", out var s) ? s.GetString() : "";
                        string endDate = root.TryGetProperty("endDate", out var eDate) ? eDate.GetString() : "";
                        bool showAll = root.TryGetProperty("showAll", out var all) && all.GetBoolean();
                        await LoadMyTasks(startDate, endDate, showAll);
                        break;

                    case "completeTask":
                        await CompleteTask(root);
                        break;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка парсинга: {ex.Message}");
                SendToWebView("showError", "Ошибка обработки запроса: " + ex.Message);
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            webView?.Dispose();
            base.OnFormClosing(e);
        }
    }
}
