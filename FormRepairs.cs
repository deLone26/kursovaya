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
    public partial class FormRepairs : Form
    {
        private readonly string connectionString;
        private int currentEmployeeId;
        private string currentUserLogin;
        private string currentUserRole;
        private string currentEmployeeFullName;

        private WebView2 webView;
        private string webUIPath;
        private bool isWebViewInitialized = false;

        public FormRepairs(string connString, int userId, string userLogin, string userRole, int employeeId)
        {
            this.connectionString = connString;
            this.currentEmployeeId = employeeId;
            this.currentUserLogin = userLogin;
            this.currentUserRole = userRole;
            this.currentEmployeeFullName = GetFullName(employeeId);

            InitializeComponent();

            webUIPath = @"C:\Users\Daniil\Desktop\4\kursovaya3\kursovaya\WebUI";

            this.Text = "Мои задания - Слесарь";
            this.WindowState = FormWindowState.Maximized;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Controls.Clear();

            InitializeWebView();
        }

        private string GetFullName(int employeeId)
        {
            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();
                    string sql = "SELECT CONCAT(familiya, ' ', LEFT(imya, 1), '.', LEFT(otchestvo, 1), '.') FROM sotrudniki WHERE id = @id";
                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", employeeId);
                        var result = cmd.ExecuteScalar();
                        return result?.ToString() ?? "Слесарь";
                    }
                }
            }
            catch
            {
                return "Слесарь";
            }
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
                    webView.CoreWebView2.Navigate($"file:///{htmlPath.Replace('\\', '/')}");
                    isWebViewInitialized = true;

                    webView.CoreWebView2.NavigationCompleted += async (s, e) =>
                    {
                        await Task.Delay(500);
                        await SetCurrentUserInWebView();
                        await LoadMyTasks("", "", false);
                        await LoadStatistics();
                        await LoadEquipment("");
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
                string script = $"setCurrentUser({currentEmployeeId}, '{currentUserLogin}', '{currentUserRole}', '{currentEmployeeFullName}');";
                await webView.CoreWebView2.ExecuteScriptAsync(script);
            }
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
                            TO_CHAR(p.data_nachala, 'DD.MM.YYYY') as start_date,
                            p.status
                        FROM plan_to p
                        JOIN oborudovanie o ON p.oborudovanie_id = o.id
                        LEFT JOIN tip_to t ON p.tip_to_id = t.id
                        WHERE p.otvetstvenniy_id = @employee_id";

                    if (!showAll && !string.IsNullOrEmpty(startDate) && !string.IsNullOrEmpty(endDate))
                    {
                        sql += " AND DATE(p.data_nachala) BETWEEN @start AND @end";
                    }

                    sql += " ORDER BY p.id DESC";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@employee_id", currentEmployeeId);

                        if (!showAll && !string.IsNullOrEmpty(startDate) && !string.IsNullOrEmpty(endDate))
                        {
                            cmd.Parameters.AddWithValue("@start", DateTime.Parse(startDate).Date);
                            cmd.Parameters.AddWithValue("@end", DateTime.Parse(endDate).Date);
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
                                    start_date = reader.GetString(3),
                                    status = reader.GetString(4)
                                });
                            }
                        }
                    }
                }

                string json = JsonSerializer.Serialize(tasks, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                await ExecuteJsFunction("displayTasks", json);
                await LoadStatistics();
            }
            catch (Exception ex)
            {
                await ExecuteJsFunction("showError", $"Ошибка: {ex.Message}");
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

                                await ExecuteJsFunction("updateStatistics", json);
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

        private async Task LoadEquipment(string filter = "")
        {
            try
            {
                var equipment = new List<object>();

                using (var conn = new NpgsqlConnection(connectionString))
                {
                    await conn.OpenAsync();

                    string sql = @"
                        SELECT 
                            o.id, 
                            o.nazvanie, 
                            COALESCE(o.tip, '') as tip, 
                            COALESCE(o.model, '') as model, 
                            COALESCE(o.mesto, '') as mesto,
                            COALESCE(s.nazvanie, 'В работе') as status_name
                        FROM oborudovanie o
                        LEFT JOIN status_oborudovaniya s ON o.status_id = s.id
                        WHERE 1=1";

                    if (!string.IsNullOrEmpty(filter))
                    {
                        sql += " AND (o.nazvanie ILIKE @filter OR o.tip ILIKE @filter)";
                    }

                    sql += " ORDER BY o.nazvanie";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        if (!string.IsNullOrEmpty(filter))
                        {
                            cmd.Parameters.AddWithValue("@filter", $"%{filter}%");
                        }

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                equipment.Add(new
                                {
                                    id = reader.GetInt32(0),
                                    nazvanie = reader.GetString(1),
                                    tip = reader.GetString(2),
                                    model = reader.GetString(3),
                                    mesto = reader.GetString(4),
                                    status_name = reader.GetString(5)
                                });
                            }
                        }
                    }
                }

                string json = JsonSerializer.Serialize(equipment, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                await ExecuteJsFunction("displayEquipment", json);
            }
            catch (Exception ex)
            {
                await ExecuteJsFunction("showError", $"Ошибка: {ex.Message}");
            }
        }

        private async Task LoadPassport(int equipmentId)
        {
            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    await conn.OpenAsync();

                    string sql = @"
                        SELECT 
                            o.nazvanie, 
                            COALESCE(o.tip, '') as tip, 
                            COALESCE(o.model, '') as model, 
                            COALESCE(o.seriionmer, '') as seriionmer, 
                            COALESCE(o.mesto, '') as mesto,
                            COALESCE(o.moshnost, 0) as moshnost,
                            COALESCE(o.davlenie, 0) as davlenie,
                            COALESCE(o.proizvoditel, '') as proizvoditel,
                            TO_CHAR(o.data_ustanovki, 'DD.MM.YYYY') as data_ustanovki,
                            COALESCE(s.nazvanie, 'В работе') as status_name
                        FROM oborudovanie o
                        LEFT JOIN status_oborudovaniya s ON o.status_id = s.id
                        WHERE o.id = @id";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", equipmentId);

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                var passportData = new
                                {
                                    nazvanie = reader.GetString(0),
                                    tip = reader.GetString(1),
                                    model = reader.GetString(2),
                                    seriionmer = reader.GetString(3),
                                    mesto = reader.GetString(4),
                                    moshnost = reader.GetDecimal(5),
                                    davlenie = reader.GetDecimal(6),
                                    proizvoditel = reader.GetString(7),
                                    dataUstanovki = reader.IsDBNull(8) ? "" : reader.GetString(8),
                                    statusName = reader.GetString(9)
                                };

                                string json = JsonSerializer.Serialize(passportData, new JsonSerializerOptions
                                {
                                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                                });

                                await ExecuteJsFunction("showPassport", json);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                await ExecuteJsFunction("showError", $"Ошибка: {ex.Message}");
            }
        }

        private async Task CompleteTask(int planId, string description, string actualStartDate, string actualEndDate)
        {
            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    await conn.OpenAsync();

                    using (var transaction = await conn.BeginTransactionAsync())
                    {
                        string sqlPlan = "UPDATE plan_to SET status = 'Завершен' WHERE id = @id";
                        using (var cmd = new NpgsqlCommand(sqlPlan, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@id", planId);
                            await cmd.ExecuteNonQueryAsync();
                        }

                        string sqlRemont = @"
                            INSERT INTO remont (oborudovanie_id, sotrudnik_id, data_nachala, data_okonchaniya, opisanie, plan_id)
                            SELECT oborudovanie_id, @sotrudnik_id, @start, @end, @opisanie, @plan_id
                            FROM plan_to WHERE id = @plan_id";

                        using (var cmd = new NpgsqlCommand(sqlRemont, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@sotrudnik_id", currentEmployeeId);
                            cmd.Parameters.AddWithValue("@start", DateTime.Parse(actualStartDate));
                            cmd.Parameters.AddWithValue("@end", string.IsNullOrEmpty(actualEndDate) ? DBNull.Value : DateTime.Parse(actualEndDate));
                            cmd.Parameters.AddWithValue("@opisanie", description);
                            cmd.Parameters.AddWithValue("@plan_id", planId);
                            await cmd.ExecuteNonQueryAsync();
                        }

                        await transaction.CommitAsync();
                    }
                }

                await ExecuteJsFunction("showSuccess", "Задание выполнено!");
                await LoadMyTasks("", "", false);
                await LoadStatistics();
            }
            catch (Exception ex)
            {
                await ExecuteJsFunction("showError", $"Ошибка: {ex.Message}");
            }
        }

        private async Task ExecuteJsFunction(string function, string data)
        {
            if (isWebViewInitialized && webView?.CoreWebView2 != null)
            {
                string js = $"if(window.{function}) window.{function}({data});";
                await webView.CoreWebView2.ExecuteScriptAsync(js);
            }
        }

        private async void OnWebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            string message = e.TryGetWebMessageAsString();
            System.Diagnostics.Debug.WriteLine($"Получено: {message}");

            try
            {
                using (var jsonDoc = JsonDocument.Parse(message))
                {
                    var root = jsonDoc.RootElement;
                    string action = root.GetProperty("action").GetString();

                    switch (action)
                    {
                        case "loadTasks":
                            string startDate = root.TryGetProperty("startDate", out var s) ? s.GetString() : "";
                            string endDate = root.TryGetProperty("endDate", out var ed) ? ed.GetString() : "";
                            bool showAll = root.TryGetProperty("showAll", out var all) && all.GetBoolean();
                            await LoadMyTasks(startDate, endDate, showAll);
                            break;

                        case "completeTask":
                            int planId = root.GetProperty("id").GetInt32();
                            string desc = root.GetProperty("description").GetString();
                            string start = root.GetProperty("actualStartDate").GetString();
                            string end = root.GetProperty("actualEndDate").GetString();
                            await CompleteTask(planId, desc, start, end);
                            break;

                        case "loadEquipment":
                            string filter = root.TryGetProperty("filter", out var f) ? f.GetString() : "";
                            await LoadEquipment(filter);
                            break;

                        case "loadPassport":
                            int eqId = root.GetProperty("id").GetInt32();
                            await LoadPassport(eqId);
                            break;

                        case "logout":
                            this.Invoke(new Action(() =>
                            {
                                var loginForm = new LoginForm();
                                loginForm.Show();
                                this.Close();
                            }));
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка: {ex.Message}");
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            webView?.Dispose();
            base.OnFormClosing(e);
        }
    }
}
