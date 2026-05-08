using Microsoft.Web.WebView2.WinForms;
using Microsoft.Web.WebView2.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using Npgsql;
using System.Text.Json.Serialization;
using System.Linq;

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
        private HashSet<int> notifiedTasks = new HashSet<int>();
        private Timer notificationTimer;

        public FormRepairs(string connString, int userId, string userLogin, string userRole, int employeeId)
        {
            this.connectionString = connString;
            this.currentEmployeeId = employeeId;
            this.currentUserLogin = userLogin;
            this.currentUserRole = userRole;
            this.currentEmployeeFullName = GetFullName(employeeId);

            InitializeComponent();
            this.Text = "Мои задания - Слесарь";
            this.WindowState = FormWindowState.Maximized;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Controls.Clear();

            webUIPath = @"C:\Users\Daniil\Desktop\4\kursovaya3\kursovaya\WebUI";

            InitializeWebView();
            StartNotificationTimer();
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

        private void StartNotificationTimer()
        {
            notificationTimer = new Timer();
            notificationTimer.Interval = 15000;
            notificationTimer.Tick += async (s, e) => await CheckNewTasks();
            notificationTimer.Start();
        }

        private async void InitializeWebView()
        {
            try
            {
                webView = new WebView2();
                webView.Dock = DockStyle.Fill;
                this.Controls.Add(webView);

                await webView.EnsureCoreWebView2Async(null);

                webView.CoreWebView2.Settings.IsStatusBarEnabled = false;
                webView.CoreWebView2.Settings.AreDefaultScriptDialogsEnabled = true;

                string htmlPath = Path.Combine(webUIPath, "repairs.html");

                if (File.Exists(htmlPath))
                {
                    webView.CoreWebView2.Navigate($"file:///{htmlPath.Replace('\\', '/')}");
                }
                else
                {
                    MessageBox.Show($"Файл не найден: {htmlPath}");
                    return;
                }

                webView.CoreWebView2.WebMessageReceived += HandleWebMessage;
                webView.CoreWebView2.NavigationCompleted += async (s, e) =>
                {
                    await Task.Delay(500);
                    await LoadInitialData();
                };
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка WebView2: {ex.Message}");
            }
        }

        private async Task LoadInitialData()
        {
            await SetCurrentUserInWebView();
            await LoadTasks();
            await LoadStatistics();
            await LoadHistory("", "");
        }

        private async Task SetCurrentUserInWebView()
        {
            string script = $"setCurrentUser({currentEmployeeId}, '{currentUserLogin}', '{currentUserRole}', '{currentEmployeeFullName}');";
            await webView.CoreWebView2.ExecuteScriptAsync(script);
        }

        private async void HandleWebMessage(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            string message = e.TryGetWebMessageAsString();
            System.Diagnostics.Debug.WriteLine($"Получено: {message}");

            try
            {
                if (message.StartsWith("{"))
                {
                    var json = JsonDocument.Parse(message).RootElement;
                    string action = json.GetProperty("action").GetString();

                    switch (action)
                    {
                        case "loadTasks":
                            await LoadTasks();
                            break;
                        case "loadHistory":
                            string startDate = json.TryGetProperty("startDate", out var s) ? s.GetString() : "";
                            string endDate = json.TryGetProperty("endDate", out var ed) ? ed.GetString() : "";
                            await LoadHistory(startDate, endDate);
                            break;
                        case "loadStatistics":
                            await LoadStatistics();
                            break;
                        case "loadSpareParts":
                            int equipmentId = json.GetProperty("equipmentId").GetInt32();
                            await LoadSpareParts(equipmentId);
                            break;
                        case "submitReport":
                            int taskId = json.GetProperty("taskId").GetInt32();
                            int sparePartId = json.GetProperty("sparePartId").GetInt32();
                            string description = json.GetProperty("description").GetString();
                            string reportStartDate = json.GetProperty("startDate").GetString();
                            string reportEndDate = json.GetProperty("endDate").GetString();
                            await SubmitReport(taskId, sparePartId, description, reportStartDate, reportEndDate);
                            break;
                        case "logout":
                            this.Invoke(new Action(() =>
                            {
                                notificationTimer?.Stop();
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

        private async Task LoadTasks()
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
                            o.nazvanie AS equipment_name,
                            o.id AS equipment_id,
                            COALESCE(p.opisanie, '') AS description,
                            TO_CHAR(p.data_nachala, 'YYYY-MM-DD') as due_date,
                            p.status
                        FROM plan_to p
                        JOIN oborudovanie o ON p.oborudovanie_id = o.id
                        WHERE p.otvetstvenniy_id = @employee_id
                          AND p.status IN ('В работе', 'Назначена', 'Просрочен')
                        ORDER BY p.data_nachala ASC";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@employee_id", currentEmployeeId);

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                tasks.Add(new
                                {
                                    id = reader.GetInt32(0),
                                    equipment_name = reader.GetString(1),
                                    equipment_id = reader.GetInt32(2),
                                    description = reader.IsDBNull(3) ? "" : reader.GetString(3),
                                    due_date = reader.IsDBNull(4) ? "" : reader.GetString(4),
                                    status = reader.GetString(5)
                                });
                            }
                        }
                    }
                }

                string json = JsonSerializer.Serialize(tasks);
                await webView.CoreWebView2.ExecuteScriptAsync($"displayTasks('{json.Replace("'", "\\'")}')");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadTasks error: {ex.Message}");
            }
        }

        private async Task LoadHistory(string startDate = "", string endDate = "")
        {
            try
            {
                var history = new List<object>();

                using (var conn = new NpgsqlConnection(connectionString))
                {
                    await conn.OpenAsync();

                    string sql = @"
                        SELECT 
                            TO_CHAR(p.data_okonchaniya, 'DD.MM.YYYY') as completion_date,
                            o.nazvanie as equipment,
                            COALESCE(p.opisanie, '') as description,
                            COALESCE(r.zamennaya_detal, '') as replaced_part,
                            CONCAT(s.familiya, ' ', LEFT(s.imya, 1), '.') as employee_name
                        FROM plan_to p
                        JOIN oborudovanie o ON p.oborudovanie_id = o.id
                        JOIN sotrudniki s ON p.otvetstvenniy_id = s.id
                        LEFT JOIN remont r ON p.id = r.plan_id
                        WHERE p.otvetstvenniy_id = @employee_id
                          AND p.status = 'Завершен'";

                    if (!string.IsNullOrEmpty(startDate) && !string.IsNullOrEmpty(endDate))
                    {
                        sql += " AND DATE(p.data_okonchaniya) BETWEEN @start AND @end";
                    }

                    sql += " ORDER BY p.data_okonchaniya DESC";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@employee_id", currentEmployeeId);

                        if (!string.IsNullOrEmpty(startDate) && !string.IsNullOrEmpty(endDate))
                        {
                            cmd.Parameters.AddWithValue("@start", DateTime.Parse(startDate).Date);
                            cmd.Parameters.AddWithValue("@end", DateTime.Parse(endDate).Date);
                        }

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                history.Add(new
                                {
                                    completion_date = reader.GetString(0),
                                    equipment = reader.GetString(1),
                                    description = reader.GetString(2),
                                    replaced_part = reader.GetString(3),
                                    employee_name = reader.GetString(4)
                                });
                            }
                        }
                    }
                }

                string json = JsonSerializer.Serialize(history);
                await webView.CoreWebView2.ExecuteScriptAsync($"displayHistory('{json.Replace("'", "\\'")}')");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadHistory error: {ex.Message}");
            }
        }

        private async Task LoadStatistics()
        {
            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    await conn.OpenAsync();

                    // 1. Аварии за месяц
                    int emergencyCount = 0;
                    string sqlEmergency = @"
                SELECT COUNT(*) 
                FROM avariya a
                JOIN plan_to p ON a.id = p.avariya_id
                WHERE p.otvetstvenniy_id = @employee_id
                  AND a.data_avarii > NOW() - INTERVAL '30 days'";

                    using (var cmd = new NpgsqlCommand(sqlEmergency, conn))
                    {
                        cmd.Parameters.AddWithValue("@employee_id", currentEmployeeId);
                        var result = await cmd.ExecuteScalarAsync();
                        emergencyCount = result != null ? Convert.ToInt32(result) : 0;
                    }

                    // 2. Аварии за год
                    int yearEmergencyCount = 0;
                    string sqlYearEmergency = @"
                SELECT COUNT(*) 
                FROM avariya a
                JOIN plan_to p ON a.id = p.avariya_id
                WHERE p.otvetstvenniy_id = @employee_id
                  AND a.data_avarii > NOW() - INTERVAL '1 year'";

                    using (var cmd = new NpgsqlCommand(sqlYearEmergency, conn))
                    {
                        cmd.Parameters.AddWithValue("@employee_id", currentEmployeeId);
                        var result = await cmd.ExecuteScalarAsync();
                        yearEmergencyCount = result != null ? Convert.ToInt32(result) : 0;
                    }

                    // 3. Основная статистика по задачам
                    int totalTasks = 0;
                    int completedTasks = 0;
                    int inProgressTasks = 0;
                    int overdueTasks = 0;
                    int completionRate = 0;

                    string sqlStats = @"
                SELECT 
                    COUNT(*) as total,
                    COUNT(CASE WHEN status = 'Завершен' THEN 1 END) as completed,
                    COUNT(CASE WHEN status = 'В работе' THEN 1 END) as in_progress,
                    COUNT(CASE WHEN status = 'Просрочен' THEN 1 END) as overdue
                FROM plan_to
                WHERE otvetstvenniy_id = @employee_id";

                    using (var cmd = new NpgsqlCommand(sqlStats, conn))
                    {
                        cmd.Parameters.AddWithValue("@employee_id", currentEmployeeId);
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                totalTasks = reader.GetInt32(0);
                                completedTasks = reader.GetInt32(1);
                                inProgressTasks = reader.GetInt32(2);
                                overdueTasks = reader.GetInt32(3);
                                if (totalTasks > 0) completionRate = (completedTasks * 100) / totalTasks;
                            }
                        }
                    }

                    // 4. Заменённые запчасти
                    int partsReplaced = 0;
                    string sqlParts = @"
                SELECT COUNT(*) 
                FROM remont
                WHERE sotrudnik_id = @employee_id
                  AND zamennaya_detal IS NOT NULL
                  AND zamennaya_detal != ''";

                    using (var cmd = new NpgsqlCommand(sqlParts, conn))
                    {
                        cmd.Parameters.AddWithValue("@employee_id", currentEmployeeId);
                        var result = await cmd.ExecuteScalarAsync();
                        partsReplaced = result != null ? Convert.ToInt32(result) : 0;
                    }

                    // 5. Самая часто заменяемая деталь
                    string topPart = "—";
                    string sqlTopPart = @"
                SELECT zamennaya_detal, COUNT(*) as cnt 
                FROM remont 
                WHERE sotrudnik_id = @employee_id 
                  AND zamennaya_detal IS NOT NULL 
                  AND zamennaya_detal != ''
                GROUP BY zamennaya_detal 
                ORDER BY cnt DESC 
                LIMIT 1";

                    using (var cmd = new NpgsqlCommand(sqlTopPart, conn))
                    {
                        cmd.Parameters.AddWithValue("@employee_id", currentEmployeeId);
                        var result = await cmd.ExecuteScalarAsync();
                        if (result != null && result != DBNull.Value) topPart = result.ToString();
                    }

                    // 6. Временные показатели (вычисляем в C#, а не в SQL, чтобы избежать ошибок)
                    int avgCompletionDays = 0;
                    int minCompletionDays = 0;
                    int maxCompletionDays = 0;
                    int avgFixTime = 0;

                    // Получаем все завершённые задачи
                    string sqlTasksData = @"
                SELECT 
                    data_nachala,
                    data_okonchaniya
                FROM plan_to
                WHERE otvetstvenniy_id = @employee_id
                  AND status = 'Завершен'
                  AND data_okonchaniya IS NOT NULL
                  AND data_nachala IS NOT NULL";

                    var taskDurations = new List<int>();
                    using (var cmd = new NpgsqlCommand(sqlTasksData, conn))
                    {
                        cmd.Parameters.AddWithValue("@employee_id", currentEmployeeId);
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                DateTime start = reader.GetDateTime(0);
                                DateTime end = reader.GetDateTime(1);
                                int workDays = CalculateWorkingDays(start, end);
                                taskDurations.Add(workDays);
                            }
                        }
                    }

                    if (taskDurations.Count > 0)
                    {
                        avgCompletionDays = (int)Math.Round(taskDurations.Average());
                        minCompletionDays = taskDurations.Min();
                        maxCompletionDays = taskDurations.Max();
                    }

                    // 7. Среднее время устранения аварий (вычисляем в C#)
                    string sqlAvariyaData = @"
                SELECT 
                    a.data_avarii,
                    p.data_okonchaniya
                FROM avariya a
                JOIN plan_to p ON a.id = p.avariya_id
                WHERE p.otvetstvenniy_id = @employee_id
                  AND p.status = 'Завершен'
                  AND p.data_okonchaniya IS NOT NULL
                  AND a.data_avarii IS NOT NULL";

                    var fixDurations = new List<int>();
                    using (var cmd = new NpgsqlCommand(sqlAvariyaData, conn))
                    {
                        cmd.Parameters.AddWithValue("@employee_id", currentEmployeeId);
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                DateTime avariyaDate = reader.GetDateTime(0);
                                DateTime completionDate = reader.GetDateTime(1);
                                int workDays = CalculateWorkingDays(avariyaDate, completionDate);
                                fixDurations.Add(workDays);
                            }
                        }
                    }

                    if (fixDurations.Count > 0)
                    {
                        avgFixTime = (int)Math.Round(fixDurations.Average());
                    }

                    // Формируем объект со всеми данными
                    var stats = new
                    {
                        emergencyCount = emergencyCount,
                        yearEmergencyCount = yearEmergencyCount,
                        totalTasks = totalTasks,
                        completedTasks = completedTasks,
                        inProgressTasks = inProgressTasks,
                        overdueTasks = overdueTasks,
                        completionRate = completionRate,
                        avgCompletionDays = avgCompletionDays,
                        minCompletionDays = minCompletionDays,
                        maxCompletionDays = maxCompletionDays,
                        partsReplaced = partsReplaced,
                        topPart = topPart,
                        avgFixTime = avgFixTime
                    };

                    string json = JsonSerializer.Serialize(stats);
                    System.Diagnostics.Debug.WriteLine($"Статистика: {json}");
                    await webView.CoreWebView2.ExecuteScriptAsync($"updateStatistics('{json.Replace("'", "\\'")}')");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadStatistics error: {ex.Message}");
                var emptyStats = new
                {
                    emergencyCount = 0,
                    yearEmergencyCount = 0,
                    totalTasks = 0,
                    completedTasks = 0,
                    inProgressTasks = 0,
                    overdueTasks = 0,
                    completionRate = 0,
                    avgCompletionDays = 0,
                    minCompletionDays = 0,
                    maxCompletionDays = 0,
                    partsReplaced = 0,
                    topPart = "—",
                    avgFixTime = 0
                };
                string json = JsonSerializer.Serialize(emptyStats);
                await webView.CoreWebView2.ExecuteScriptAsync($"updateStatistics('{json.Replace("'", "\\'")}')");
            }
        }

        // Функция для подсчёта рабочих дней (без выходных)
        private int CalculateWorkingDays(DateTime startDate, DateTime endDate)
        {
            if (startDate > endDate) return 0;

            int workingDays = 0;
            DateTime current = startDate.Date;
            DateTime end = endDate.Date;

            while (current <= end)
            {
                // Понедельник = 1, Воскресенье = 7
                if (current.DayOfWeek != DayOfWeek.Saturday && current.DayOfWeek != DayOfWeek.Sunday)
                {
                    workingDays++;
                }
                current = current.AddDays(1);
            }

            return workingDays;
        }

        private async Task LoadSpareParts(int equipmentId)
        {
            try
            {
                var parts = new List<object>();

                using (var conn = new NpgsqlConnection(connectionString))
                {
                    await conn.OpenAsync();

                    string sql = @"
                        SELECT DISTINCT
                            sp.id,
                            sp.name
                        FROM spare_parts sp
                        LEFT JOIN equipment_spare_parts esp ON sp.id = esp.spare_part_id
                        WHERE sp.is_common = true
                           OR esp.equipment_id = @equipment_id
                        ORDER BY sp.name";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@equipment_id", equipmentId);

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                parts.Add(new
                                {
                                    id = reader.GetInt32(0),
                                    name = reader.GetString(1)
                                });
                            }
                        }
                    }
                }

                string json = JsonSerializer.Serialize(parts);
                await webView.CoreWebView2.ExecuteScriptAsync($"displaySpareParts('{json.Replace("'", "\\'")}')");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadSpareParts error: {ex.Message}");
                await webView.CoreWebView2.ExecuteScriptAsync($"displaySpareParts('[]')");
            }
        }

        private async Task SubmitReport(int taskId, int sparePartId, string description, string startDate, string endDate)
        {
            try
            {
                string sparePartName = "";
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    await conn.OpenAsync();
                    string sqlPart = "SELECT name FROM spare_parts WHERE id = @id";
                    using (var cmd = new NpgsqlCommand(sqlPart, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", sparePartId);
                        var result = await cmd.ExecuteScalarAsync();
                        if (result != null) sparePartName = result.ToString();
                    }
                }

                using (var conn = new NpgsqlConnection(connectionString))
                {
                    await conn.OpenAsync();
                    using (var transaction = await conn.BeginTransactionAsync())
                    {
                        // Получаем оборудование
                        int equipmentId = 0;
                        string sqlGetEquipment = "SELECT oborudovanie_id FROM plan_to WHERE id = @task_id";
                        using (var cmd = new NpgsqlCommand(sqlGetEquipment, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@task_id", taskId);
                            var result = await cmd.ExecuteScalarAsync();
                            if (result != null) equipmentId = Convert.ToInt32(result);
                        }

                        // Обновляем статус задачи
                        string sqlUpdateTask = "UPDATE plan_to SET status = 'Завершен', data_okonchaniya = @end_date WHERE id = @task_id";
                        using (var cmd = new NpgsqlCommand(sqlUpdateTask, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@task_id", taskId);
                            cmd.Parameters.AddWithValue("@end_date", string.IsNullOrEmpty(endDate) ? DateTime.Now : DateTime.Parse(endDate));
                            await cmd.ExecuteNonQueryAsync();
                        }

                        // Добавляем запись в ремонт
                        string sqlInsertRemont = @"
                            INSERT INTO remont (oborudovanie_id, sotrudnik_id, data_nachala, data_okonchaniya, opisanie, plan_id, zamennaya_detal)
                            VALUES (@equipment_id, @sotrudnik_id, @start_date, @end_date, @description, @plan_id, @spare_part)";

                        using (var cmd = new NpgsqlCommand(sqlInsertRemont, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@equipment_id", equipmentId);
                            cmd.Parameters.AddWithValue("@sotrudnik_id", currentEmployeeId);
                            cmd.Parameters.AddWithValue("@start_date", DateTime.Parse(startDate));
                            cmd.Parameters.AddWithValue("@end_date", string.IsNullOrEmpty(endDate) ? DateTime.Now : DateTime.Parse(endDate));
                            cmd.Parameters.AddWithValue("@description", description);
                            cmd.Parameters.AddWithValue("@plan_id", taskId);
                            cmd.Parameters.AddWithValue("@spare_part", sparePartName);
                            await cmd.ExecuteNonQueryAsync();
                        }

                        await transaction.CommitAsync();
                    }
                }

                await webView.CoreWebView2.ExecuteScriptAsync("showSuccess('Отчёт отправлен начальнику!')");
                await LoadTasks();
                await LoadStatistics();

                string today = DateTime.Now.ToString("yyyy-MM-dd");
                string monthAgo = DateTime.Now.AddMonths(-1).ToString("yyyy-MM-dd");
                await LoadHistory(monthAgo, today);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SubmitReport error: {ex.Message}");
                await webView.CoreWebView2.ExecuteScriptAsync($"showError('Ошибка: {ex.Message}')");
            }
        }

        private async Task CheckNewTasks()
        {
            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    await conn.OpenAsync();

                    string sqlTasks = @"
                        SELECT 
                            p.id,
                            o.nazvanie AS equipment,
                            COALESCE(p.opisanie, '') as description,
                            TO_CHAR(p.data_nachala, 'DD.MM.YYYY') as due_date
                        FROM plan_to p
                        JOIN oborudovanie o ON p.oborudovanie_id = o.id
                        WHERE p.otvetstvenniy_id = @employee_id
                          AND p.status IN ('В работе', 'Назначена')
                          AND p.data_nachala >= CURRENT_DATE";

                    using (var cmd = new NpgsqlCommand(sqlTasks, conn))
                    {
                        cmd.Parameters.AddWithValue("@employee_id", currentEmployeeId);

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                int taskId = reader.GetInt32(0);

                                if (!notifiedTasks.Contains(taskId))
                                {
                                    notifiedTasks.Add(taskId);

                                    var task = new
                                    {
                                        id = taskId,
                                        equipment = reader.GetString(1),
                                        description = reader.IsDBNull(2) ? "" : reader.GetString(2),
                                        due_date = reader.GetString(3)
                                    };
                                    string json = JsonSerializer.Serialize(task);
                                    await webView.CoreWebView2.ExecuteScriptAsync($"onNewTask('{json.Replace("'", "\\'")}')");
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CheckNewTasks error: {ex.Message}");
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            notificationTimer?.Stop();
            notificationTimer?.Dispose();
            webView?.Dispose();
            base.OnFormClosing(e);
        }
    }
}