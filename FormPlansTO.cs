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
        private bool useRealDatabase = true;

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
                if (useRealDatabase)
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
                else
                {
                    SendTestData();
                }
            }
            catch (Exception ex)
            {
                SendTestData();
                SendToWebView("showError", $"Ошибка БД, загружены тестовые данные: {ex.Message}");
            }
        }

        private void SendEmployees()
        {
            try
            {
                if (useRealDatabase)
                {
                    using (var conn = new NpgsqlConnection(connectionString))
                    {
                        conn.Open();
                        var employees = GetEmployeesList(conn);
                        string jsonData = JsonSerializer.Serialize(employees);
                        SendToWebView("employeesData", jsonData);
                    }
                }
                else
                {
                    SendTestData();
                }
            }
            catch (Exception ex)
            {
                SendTestData();
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

                if (useRealDatabase)
                {
                    using (var conn = new NpgsqlConnection(connectionString))
                    {
                        conn.Open();
                        var plans = GetPlansList(conn, startDate, view, employeeId);
                        string jsonData = JsonSerializer.Serialize(plans);
                        SendToWebView("plansData", jsonData);
                    }
                }
                else
                {
                    SendTestData();
                }
            }
            catch (Exception ex)
            {
                SendTestData();
                SendToWebView("showError", ex.Message);
            }
        }

        private void SendUrgentReminders()
        {
            try
            {
                if (useRealDatabase)
                {
                    using (var conn = new NpgsqlConnection(connectionString))
                    {
                        conn.Open();
                        var urgent = GetUrgentRemindersList(conn);
                        string jsonData = JsonSerializer.Serialize(urgent);
                        SendToWebView("urgentData", jsonData);
                    }
                }
                else
                {
                    SendTestData();
                }
            }
            catch (Exception ex)
            {
                SendTestData();
                SendToWebView("showError", ex.Message);
            }
        }

        private void SendTestData()
        {
            var today = DateTime.Now;
            var yesterday = today.AddDays(-1);

            var employees = new[]
            {
                new { id = 1, fio = "Агаев В.", tasks_count = 3 },
                new { id = 2, fio = "Галушкин П.", tasks_count = 2 },
                new { id = 3, fio = "Игорь Р.", tasks_count = 1 },
                new { id = 4, fio = "Конюшин О. Г.", tasks_count = 2 },
                new { id = -1, fio = "Без ответственного", tasks_count = 0 }
            };

            var plans = new[]
            {
                new { id = 1, equipment = "Котел КВ-ГМ-10", equipment_id = 1, tip = "Плановое ТО", tip_id = 1,
                      start_date = today.ToString("yyyy-MM-dd"), end_date = today.ToString("yyyy-MM-dd"),
                      responsible = "Агаев В.", responsible_id = 1, status = "В работе", cost = 15000, is_overdue = false, days_left = 0 },
                new { id = 2, equipment = "Насос ЦНС-180", equipment_id = 2, tip = "Замена подшипников", tip_id = 2,
                      start_date = today.ToString("yyyy-MM-dd"), end_date = today.ToString("yyyy-MM-dd"),
                      responsible = "Галушкин П.", responsible_id = 2, status = "Запланирован", cost = 25000, is_overdue = false, days_left = 0 },
                new { id = 3, equipment = "Дымосос ДН-15", equipment_id = 3, tip = "Ремонт", tip_id = 1,
                      start_date = today.ToString("yyyy-MM-dd"), end_date = yesterday.ToString("yyyy-MM-dd"),
                      responsible = "Игорь Р.", responsible_id = 3, status = "Просрочен", cost = 30000, is_overdue = true, days_left = -1 }
            };

            var urgent = new[]
            {
                new { id = 1, equipment = "Котел КВ-ГМ-10", tip = "Плановое ТО", end_date = today.ToString("yyyy-MM-dd"), days_left = 0, is_overdue = false },
                new { id = 2, equipment = "Дымосос ДН-15", tip = "Ремонт", end_date = yesterday.ToString("yyyy-MM-dd"), days_left = -1, is_overdue = true }
            };

            var data = new { employees = employees, plans = plans, urgent = urgent };
            string jsonData = JsonSerializer.Serialize(data);
            SendToWebView("initialData", jsonData);
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
                                COALESCE(s.familiya || ' ' || COALESCE(s.imya, '') || ' ' || COALESCE(s.otchestvo, ''), 'Не назначен') AS Ответственный,
                                COALESCE(p.status, 'Не указан') AS Статус,
                                COALESCE(p.stoimost, 0) AS Стоимость
                            FROM plan_to p
                            LEFT JOIN oborudovanie o ON p.oborudovanie_id = o.id
                            LEFT JOIN tip_to t ON p.tip_to_id = t.id
                            LEFT JOIN sotrudniki s ON p.otvetstvenniy_id = s.id
                            ORDER BY p.id";

                        using (var cmd = new NpgsqlCommand(sql, conn))
                        using (var reader = cmd.ExecuteReader())
                        using (StreamWriter sw = new StreamWriter(save.FileName, false, Encoding.UTF8))
                        {
                            sw.WriteLine("ID;Оборудование;Тип ТО;Дата начала;Дата окончания;Ответственный;Статус;Стоимость (руб.)");

                            while (reader.Read())
                            {
                                string line = $"{reader.GetInt32(0)};" +
                                             $"{EscapeCsv(reader.GetString(1))};" +
                                             $"{EscapeCsv(reader.GetString(2))};" +
                                             $"{reader.GetDateTime(3):dd.MM.yyyy};" +
                                             $"{(reader.IsDBNull(4) ? "" : reader.GetDateTime(4).ToString("dd.MM.yyyy"))};" +
                                             $"{EscapeCsv(reader.GetString(5))};" +
                                             $"{EscapeCsv(reader.GetString(6))};" +
                                             $"{reader.GetDecimal(7):F2}";
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

            // Получаем только слесарей с количеством активных задач
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

            // Добавляем "Без ответственного" в конец списка
            list.Add(new { id = -1, fio = "Без ответственного", tasks_count = 0 });

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

            string sql = "SELECT id, oborudovanie_id, tip_to_id, data_nachala, data_okonchaniya, otvetstvenniy_id, status, stoimost, is_overdue FROM plan_to";
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
                    decimal cost = reader.IsDBNull(7) ? 0 : reader.GetDecimal(7);
                    bool isOverdueDb = reader.IsDBNull(8) ? false : reader.GetBoolean(8);

                    string equipmentName = equipmentDict.ContainsKey(oborudovanieId) ? equipmentDict[oborudovanieId] : "Неизвестно";
                    string tipName = (tipId.HasValue && tipDict.ContainsKey(tipId.Value)) ? tipDict[tipId.Value] : "Не указан";
                    string responsibleName = (responsibleId.HasValue && employeeDict.ContainsKey(responsibleId.Value)) ? employeeDict[responsibleId.Value] : "Не назначен";

                    int daysLeft = 999;
                    if (end.HasValue) daysLeft = (int)(end.Value.Date - DateTime.Now.Date).TotalDays;
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
                        cost = cost,
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

            string sql = "SELECT id, oborudovanie_id, data_okonchaniya, is_overdue FROM plan_to WHERE data_okonchaniya IS NOT NULL AND (status IS NULL OR status NOT IN ('Завершен', 'Отменен')) ORDER BY data_okonchaniya LIMIT 10";
            using (var cmd = new NpgsqlCommand(sql, conn))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    int id = reader.GetInt32(0);
                    int oborudovanieId = reader.GetInt32(1);
                    DateTime endDate = reader.GetDateTime(2);
                    bool isOverdue = reader.GetBoolean(3);
                    string equipmentName = equipmentDict.ContainsKey(oborudovanieId) ? equipmentDict[oborudovanieId] : "Неизвестно";
                    int daysLeft = (int)(endDate.Date - DateTime.Now.Date).TotalDays;
                    list.Add(new
                    {
                        id = id,
                        equipment = equipmentName,
                        tip = "ТО",
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
