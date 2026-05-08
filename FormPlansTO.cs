using Microsoft.Web.WebView2.WinForms;
using Microsoft.Web.WebView2.Core;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using Npgsql;

namespace WindowsFormsApp1
{
    public partial class FormPlansTO : Form
    {
        private string connectionString;
        private int currentUserId;
        private WebView2 webView;
        private string webUIPath = @"C:\Users\Daniil\Desktop\4\kursovaya3\kursovaya\WebUI";

        public FormPlansTO(string userConnectionString, int userId)
        {
            this.connectionString = userConnectionString;
            this.currentUserId = userId;

            this.AutoScaleMode = AutoScaleMode.Dpi;
            this.BackColor = Color.White;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Text = "Планы ТО - Графики ремонтов и напоминания";
            this.WindowState = FormWindowState.Maximized;

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

                await webView.CoreWebView2.Profile.ClearBrowsingDataAsync();
                webView.CoreWebView2.Settings.IsStatusBarEnabled = false;
                webView.CoreWebView2.Settings.AreDefaultScriptDialogsEnabled = true;

                string htmlPath = Path.Combine(webUIPath, "plans_to.html");

                if (!File.Exists(htmlPath))
                {
                    MessageBox.Show($"Файл не найден: {htmlPath}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    this.Close();
                    return;
                }

                webView.CoreWebView2.Navigate($"file:///{htmlPath.Replace('\\', '/')}");

                webView.CoreWebView2.WebMessageReceived += (s, e) =>
                {
                    string message = e.TryGetWebMessageAsString();
                    HandleWebMessage(message);
                };

                webView.CoreWebView2.NavigationCompleted += (s, e) =>
                {
                    Task.Delay(500).ContinueWith(_ =>
                    {
                        if (this.IsHandleCreated)
                        {
                            this.Invoke(new Action(() =>
                            {
                                SendInitialData();
                            }));
                        }
                    });
                };
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка инициализации WebView2: {ex.Message}");
                this.Close();
            }
        }

        private void HandleWebMessage(string message)
        {
            try
            {
                using (JsonDocument json = JsonDocument.Parse(message))
                {
                    JsonElement root = json.RootElement;
                    string action = root.GetProperty("action").GetString();

                    switch (action)
                    {
                        case "getInitialData":
                            SendInitialData();
                            break;
                        case "getEmployees":
                            SendEmployees();
                            break;
                        case "getPlans":
                            SendPlans(root);
                            break;
                        case "getUrgentReminders":
                            SendUrgentReminders();
                            break;
                        case "exportToExcel":
                            ExportToExcel();
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                SendToWebView("showError", ex.Message);
            }
        }

        private void SendInitialData()
        {
            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();
                    var employees = GetEmployeesList(conn);
                    var plans = GetPlansList(conn);
                    var urgent = GetUrgentRemindersList(conn);

                    var data = new { employees = employees, plans = plans, urgent = urgent };
                    string jsonData = JsonSerializer.Serialize(data);
                    SendToWebView("initialData", jsonData);
                }
            }
            catch (Exception ex)
            {
                SendToWebView("showError", $"Ошибка загрузки данных: {ex.Message}");
            }
        }

        private void SendEmployees()
        {
            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();
                    var employees = GetEmployeesList(conn);
                    string jsonData = JsonSerializer.Serialize(employees);
                    SendToWebView("employeesData", jsonData);
                }
            }
            catch (Exception ex)
            {
                SendToWebView("showError", ex.Message);
            }
        }

        private void SendPlans(JsonElement json)
        {
            try
            {
                string startDate = json.GetProperty("startDate").GetString();
                string view = json.GetProperty("view").GetString();
                int? employeeId = json.TryGetProperty("employeeId", out var empProp) ? empProp.GetInt32() : (int?)null;

                using (var conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();
                    var plans = GetPlansList(conn, startDate, view, employeeId);
                    string jsonData = JsonSerializer.Serialize(plans);
                    SendToWebView("plansData", jsonData);
                }
            }
            catch (Exception ex)
            {
                SendToWebView("showError", ex.Message);
            }
        }

        private void SendUrgentReminders()
        {
            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();
                    var urgent = GetUrgentRemindersList(conn);
                    string jsonData = JsonSerializer.Serialize(urgent);
                    SendToWebView("urgentData", jsonData);
                }
            }
            catch (Exception ex)
            {
                SendToWebView("showError", ex.Message);
            }
        }

        private void ExportToExcel()
        {
            try
            {
                SaveFileDialog save = new SaveFileDialog();
                save.Filter = "CSV files (*.csv)|*.csv";
                save.FileName = $"Планы_ТО_{DateTime.Now:yyyy-MM-dd_HH-mm}.csv";
                save.Title = "Сохранить отчет";

                if (save.ShowDialog() == DialogResult.OK)
                {
                    using (var conn = new NpgsqlConnection(connectionString))
                    {
                        conn.Open();

                        string sql = @"
                            SELECT 
                                p.id,
                                COALESCE(o.nazvanie, 'Не указано') AS Оборудование,
                                COALESCE(t.nazvanie, 'Не указан') AS Тип_ТО,
                                p.data_nachala AS Дата_начала,
                                p.data_okonchaniya AS Дата_окончания,
                                COALESCE(CONCAT(s.familiya, ' ', s.imya), 'Не назначен') AS Ответственный,
                                COALESCE(p.status, 'Не указан') AS Статус
                            FROM plan_to p
                            LEFT JOIN oborudovanie o ON p.oborudovanie_id = o.id
                            LEFT JOIN tip_to t ON p.tip_to_id = t.id
                            LEFT JOIN sotrudniki s ON p.otvetstvenniy_id = s.id
                            ORDER BY p.id";

                        using (var cmd = new NpgsqlCommand(sql, conn))
                        using (var reader = cmd.ExecuteReader())
                        using (StreamWriter sw = new StreamWriter(save.FileName, false, Encoding.UTF8))
                        {
                            sw.WriteLine("ID;Оборудование;Тип ТО;Дата начала;Дата окончания;Ответственный;Статус");

                            while (reader.Read())
                            {
                                string line = $"{reader.GetInt32(0)};" +
                                             $"{EscapeCsv(reader.GetString(1))};" +
                                             $"{EscapeCsv(reader.GetString(2))};" +
                                             $"{reader.GetDateTime(3):dd.MM.yyyy};" +
                                             $"{(reader.IsDBNull(4) ? "" : reader.GetDateTime(4).ToString("dd.MM.yyyy"))};" +
                                             $"{EscapeCsv(reader.GetString(5))};" +
                                             $"{EscapeCsv(reader.GetString(6))}";
                                sw.WriteLine(line);
                            }
                        }
                    }

                    MessageBox.Show($"Отчет успешно сохранен!", "Успешно", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка экспорта: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private string EscapeCsv(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            if (value.Contains(";") || value.Contains("\"") || value.Contains("\n") || value.Contains("\r"))
            {
                value = value.Replace("\"", "\"\"");
                return $"\"{value}\"";
            }
            return value;
        }

        private List<object> GetEmployeesList(NpgsqlConnection conn)
        {
            var list = new List<object>();

            // Получаем только слесарей из базы данных
            string sql = @"
                SELECT 
                    s.id, 
                    s.familiya, 
                    s.imya, 
                    s.otchestvo,
                    COUNT(p.id) as tasks_count
                FROM sotrudniki s
                LEFT JOIN plan_to p ON p.otvetstvenniy_id = s.id AND (p.status IS NULL OR p.status NOT IN ('Завершен', 'Отменен'))
                WHERE s.dolzhnost ILIKE '%слесар%' OR s.dolzhnost ILIKE '%Слесар%'
                GROUP BY s.id, s.familiya, s.imya, s.otchestvo
                ORDER BY s.familiya";

            using (var cmd = new NpgsqlCommand(sql, conn))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    int id = reader.GetInt32(0);
                    string familiya = reader.IsDBNull(1) ? "" : reader.GetString(1);
                    string imya = reader.IsDBNull(2) ? "" : reader.GetString(2);
                    string otchestvo = reader.IsDBNull(3) ? "" : reader.GetString(3);
                    string fio = $"{familiya} {imya} {otchestvo}".Trim();
                    if (string.IsNullOrEmpty(fio)) fio = $"Сотрудник {id}";

                    int tasksCount = reader.GetInt32(4);

                    list.Add(new { id = id, fio = fio, tasks_count = tasksCount });
                }
            }

            // Добавляем "Без ответственного" только если есть задачи без ответственного
            string checkSql = "SELECT COUNT(*) FROM plan_to WHERE otvetstvenniy_id IS NULL AND (status IS NULL OR status NOT IN ('Завершен', 'Отменен'))";
            using (var cmd = new NpgsqlCommand(checkSql, conn))
            {
                int orphanTasks = Convert.ToInt32(cmd.ExecuteScalar());
                if (orphanTasks > 0)
                {
                    list.Add(new { id = -1, fio = "Без ответственного", tasks_count = orphanTasks });
                }
            }

            return list;
        }

        private List<object> GetPlansList(NpgsqlConnection conn, string startDate = null, string view = null, int? employeeId = null)
        {
            var list = new List<object>();

            var equipmentDict = new Dictionary<int, string>();
            using (var cmd = new NpgsqlCommand("SELECT id, nazvanie FROM oborudovanie", conn))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read()) equipmentDict[reader.GetInt32(0)] = reader.GetString(1);
            }

            var tipDict = new Dictionary<int, string>();
            using (var cmd = new NpgsqlCommand("SELECT id, nazvanie FROM tip_to", conn))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read()) tipDict[reader.GetInt32(0)] = reader.GetString(1);
            }

            var employeeDict = new Dictionary<int, string>();
            using (var cmd = new NpgsqlCommand("SELECT id, familiya, imya, otchestvo FROM sotrudniki", conn))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    int id = reader.GetInt32(0);
                    string familiya = reader.IsDBNull(1) ? "" : reader.GetString(1);
                    string imya = reader.IsDBNull(2) ? "" : reader.GetString(2);
                    string otchestvo = reader.IsDBNull(3) ? "" : reader.GetString(3);
                    string fio = $"{familiya} {imya} {otchestvo}".Trim();
                    if (string.IsNullOrEmpty(fio)) fio = $"Сотрудник {id}";
                    employeeDict[id] = fio;
                }
            }

            string sql = "SELECT id, oborudovanie_id, tip_to_id, data_nachala, data_okonchaniya, otvetstvenniy_id, status, is_overdue FROM plan_to";
            using (var cmd = new NpgsqlCommand(sql, conn))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    int id = reader.GetInt32(0);
                    int oborudovanieId = reader.GetInt32(1);
                    int? tipId = reader.IsDBNull(2) ? (int?)null : reader.GetInt32(2);
                    DateTime start = reader.GetDateTime(3);
                    DateTime? end = reader.IsDBNull(4) ? (DateTime?)null : reader.GetDateTime(4);
                    int? responsibleId = reader.IsDBNull(5) ? (int?)null : reader.GetInt32(5);
                    string status = reader.IsDBNull(6) ? "Не указан" : reader.GetString(6);
                    bool isOverdueDb = reader.IsDBNull(7) ? false : reader.GetBoolean(7);

                    string equipmentName = equipmentDict.ContainsKey(oborudovanieId) ? equipmentDict[oborudovanieId] : "Неизвестно";
                    string tipName = (tipId.HasValue && tipDict.ContainsKey(tipId.Value)) ? tipDict[tipId.Value] : "Не указан";
                    string responsibleName = (responsibleId.HasValue && employeeDict.ContainsKey(responsibleId.Value)) ? employeeDict[responsibleId.Value] : "Не назначен";

                    int daysLeft = 999;
                    if (end.HasValue)
                    {
                        daysLeft = (int)(end.Value.Date - DateTime.Now.Date).TotalDays;
                    }
                    bool isOverdue = isOverdueDb || (daysLeft < 0 && status != "Завершен" && status != "Отменен");

                    list.Add(new
                    {
                        id = id,
                        equipment = equipmentName,
                        equipment_id = oborudovanieId,
                        tip = tipName,
                        tip_id = tipId ?? 0,
                        start_date = start.ToString("yyyy-MM-dd"),
                        end_date = end.HasValue ? end.Value.ToString("yyyy-MM-dd") : "",
                        responsible = responsibleName,
                        responsible_id = responsibleId ?? 0,
                        status = status,
                        is_overdue = isOverdue,
                        days_left = daysLeft
                    });
                }
            }
            return list;
        }

        private List<object> GetUrgentRemindersList(NpgsqlConnection conn)
        {
            var list = new List<object>();
            var equipmentDict = new Dictionary<int, string>();
            using (var cmd = new NpgsqlCommand("SELECT id, nazvanie FROM oborudovanie", conn))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read()) equipmentDict[reader.GetInt32(0)] = reader.GetString(1);
            }

            string sql = @"
                SELECT id, oborudovanie_id, data_okonchaniya, is_overdue 
                FROM plan_to 
                WHERE data_okonchaniya IS NOT NULL 
                  AND (status IS NULL OR status NOT IN ('Завершен', 'Отменен'))
                  AND data_okonchaniya <= CURRENT_DATE + INTERVAL '7 days'
                ORDER BY data_okonchaniya 
                LIMIT 20";

            using (var cmd = new NpgsqlCommand(sql, conn))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    int id = reader.GetInt32(0);
                    int oborudovanieId = reader.GetInt32(1);
                    DateTime endDate = reader.GetDateTime(2);
                    bool isOverdue = reader.IsDBNull(3) ? false : reader.GetBoolean(3);
                    string equipmentName = equipmentDict.ContainsKey(oborudovanieId) ? equipmentDict[oborudovanieId] : "Неизвестно";
                    int daysLeft = (int)(endDate.Date - DateTime.Now.Date).TotalDays;

                    string tip = "ТО";
                    list.Add(new
                    {
                        id = id,
                        equipment = equipmentName,
                        tip = tip,
                        end_date = endDate.ToString("yyyy-MM-dd"),
                        days_left = daysLeft,
                        is_overdue = isOverdue || daysLeft < 0
                    });
                }
            }
            return list;
        }

        private void SendToWebView(string command, string data)
        {
            if (webView?.CoreWebView2 != null)
            {
                try
                {
                    string js = $"window.receiveFromCSharp('{command}', {data})";
                    webView.CoreWebView2.ExecuteScriptAsync(js);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Ошибка отправки: {ex.Message}");
                }
            }
        }
    }
}
