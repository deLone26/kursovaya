using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public partial class FormBoss : Form
    {
        private string connectionString;
        private int currentUserId;
        private WebView2 webView;
        private int selectedPlanId = -1;
        private int selectedAvariyaId = -1;
        private string currentReportType = "Все планы";

        public FormBoss(string userConnectionString, int userId)
        {
            this.connectionString = userConnectionString;
            this.currentUserId = userId;

            InitializeComponent();
            this.WindowState = FormWindowState.Maximized;
            this.Text = "Панель начальника - Планирование ремонтов";

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

                string webUIPath = @"C:\Users\Daniil\Desktop\4\kursovaya3\kursovaya\WebUI";
                string htmlPath = Path.Combine(webUIPath, "boss.html");

                if (File.Exists(htmlPath))
                {
                    webView.CoreWebView2.Navigate($"file:///{htmlPath.Replace('\\', '/')}");
                }
                else
                {
                    MessageBox.Show($"Файл не найден: {htmlPath}");
                    return;
                }

                webView.CoreWebView2.WebMessageReceived += (s, e) =>
                {
                    string message = e.TryGetWebMessageAsString();
                    HandleWebMessage(message);
                };

                webView.CoreWebView2.NavigationCompleted += (s, e) =>
                {
                    System.Threading.Tasks.Task.Delay(500).ContinueWith(_ =>
                    {
                        if (this.IsHandleCreated)
                        {
                            this.Invoke(new Action(() =>
                            {
                                LoadInitialData();
                            }));
                        }
                    });
                };
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка WebView2: {ex.Message}");
            }
        }

        private void LoadInitialData()
        {
            try
            {
                LoadEquipment();
                LoadTipTypes();
                LoadResponsible();
                LoadPlans();
                LoadAvariya();
                LoadStatistics();
                LoadRepairHistory();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}");
            }
        }

        private void HandleWebMessage(string message)
        {
            try
            {
                if (message.StartsWith("{"))
                {
                    var json = JsonDocument.Parse(message).RootElement;
                    string action = json.GetProperty("action").GetString();

                    switch (action)
                    {
                        case "loadPlans":
                            LoadPlans(json);
                            break;
                        case "loadAvariya":
                            LoadAvariya(json);
                            break;
                        case "loadHistory":
                            LoadRepairHistory(json);
                            break;
                        case "addPlan":
                            AddPlan(json);
                            break;
                        case "updatePlan":
                            UpdatePlan(json);
                            break;
                        case "deletePlan":
                            DeletePlan(json);
                            break;
                        case "createPlanFromAvariya":
                            CreatePlanFromAvariya(json);
                            break;
                        case "exportToExcel":
                            ExportToExcel();
                            break;
                        case "exportToWord":
                            ExportToWord();
                            break;
                        case "previewReport":
                            PreviewReport();
                            break;
                    }
                }
                else
                {
                    switch (message)
                    {
                        case "loadEquipment":
                            LoadEquipment();
                            break;
                        case "loadTipTypes":
                            LoadTipTypes();
                            break;
                        case "loadResponsible":
                            LoadResponsible();
                            break;
                        case "loadStatistics":
                            LoadStatistics();
                            break;
                        case "loadHistory":
                            LoadRepairHistory();
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                ShowError("Ошибка обработки: " + ex.Message);
            }
        }

        private void CheckOverduePlans()
        {
            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();

                    // Обновляем просроченные планы
                    string sql = @"
                UPDATE plan_to 
                SET is_overdue = CASE 
                    WHEN data_okonchaniya < CURRENT_DATE 
                         AND status NOT IN ('Завершен', 'Отменен') 
                    THEN TRUE 
                    ELSE FALSE 
                END,
                status = CASE 
                    WHEN data_okonchaniya < CURRENT_DATE 
                         AND status = 'Запланирован' 
                    THEN 'Просрочен'
                    WHEN data_okonchaniya < CURRENT_DATE 
                         AND status = 'В работе' 
                    THEN 'Просрочен'
                    ELSE status 
                END
                WHERE data_okonchaniya IS NOT NULL";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    // Добавляем в историю все завершённые планы, которых ещё нет в истории
                    string addHistorySql = @"
                INSERT INTO remont (plan_id, oborudovanie_id, sotrudnik_id, data_nachala, data_okonchaniya, opisanie, stoimost, equipment_name, tip_name, sotrudnik_name)
                SELECT 
                    p.id,
                    p.oborudovanie_id,
                    p.otvetstvenniy_id,
                    p.data_nachala,
                    p.data_okonchaniya,
                    COALESCE(r.opisanie, 'Автоматически завершён'),
                    p.stoimost,
                    o.nazvanie,
                    COALESCE(t.nazvanie, 'Не указан'),
                    CONCAT(s.familiya, ' ', LEFT(s.imya, 1), '.', LEFT(s.otchestvo, 1), '.')
                FROM plan_to p
                JOIN oborudovanie o ON p.oborudovanie_id = o.id
                LEFT JOIN tip_to t ON p.tip_to_id = t.id
                JOIN sotrudniki s ON p.otvetstvenniy_id = s.id
                LEFT JOIN remont r ON p.id = r.plan_id
                WHERE p.status = 'Завершен' AND r.id IS NULL";

                    using (var cmd = new NpgsqlCommand(addHistorySql, conn))
                    {
                        int added = cmd.ExecuteNonQuery();
                        if (added > 0)
                        {
                            System.Diagnostics.Debug.WriteLine($"Добавлено {added} записей в историю ремонтов");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка проверки просрочек: {ex.Message}");
            }
        }

        private void LoadEquipment()
        {
            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();
                    string sql = "SELECT id, nazvanie FROM oborudovanie ORDER BY nazvanie";
                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            var list = new List<object>();

                            while (reader.Read())
                            {
                                list.Add(new
                                {
                                    id = reader.GetInt32(0),
                                    name = reader.GetString(1)
                                });
                            }

                            string jsonData = JsonSerializer.Serialize(list);
                            SendToWebView("fillEquipment", jsonData);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ShowError("Ошибка загрузки оборудования: " + ex.Message);
            }
        }

        private void LoadTipTypes()
        {
            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();
                    string sql = "SELECT id, nazvanie FROM tip_to ORDER BY nazvanie";
                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            var list = new List<object>();

                            while (reader.Read())
                            {
                                list.Add(new
                                {
                                    id = reader.GetInt32(0),
                                    name = reader.GetString(1)
                                });
                            }

                            string jsonData = JsonSerializer.Serialize(list);
                            SendToWebView("fillTipTypes", jsonData);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ShowError("Ошибка загрузки типов ТО: " + ex.Message);
            }
        }

        private void LoadResponsible()
        {
            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();
                    string sql = @"
                        SELECT id, familiya || ' ' || imya || ' ' || otchestvo AS fio 
                        FROM sotrudniki 
                        WHERE dolzhnost ILIKE '%слесар%' OR dolzhnost ILIKE '%Слесар%'
                        ORDER BY familiya";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            var list = new List<object>();

                            while (reader.Read())
                            {
                                list.Add(new
                                {
                                    id = reader.GetInt32(0),
                                    name = reader.GetString(1)
                                });
                            }

                            string jsonData = JsonSerializer.Serialize(list);
                            SendToWebView("fillResponsible", jsonData);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ShowError("Ошибка загрузки ответственных: " + ex.Message);
            }
        }

        private void LoadPlans(JsonElement json = default)
        {
            try
            {
                CheckOverduePlans();

                using (var conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();

                    StringBuilder sql = new StringBuilder(@"
                SELECT 
                    p.id,
                    o.nazvanie AS equipment,
                    COALESCE(t.nazvanie, 'Не указан') AS tip,
                    TO_CHAR(p.data_nachala, 'DD.MM.YYYY') as start_date,
                    TO_CHAR(p.data_okonchaniya, 'DD.MM.YYYY') as end_date,
                    COALESCE(s.familiya || ' ' || s.imya || ' ' || s.otchestvo, 'Не назначен') AS responsible,
                    CASE 
                        WHEN p.status = 'Просрочен' THEN '🔴 Просрочен'
                        WHEN p.status = 'Завершен' THEN '✅ Завершен'
                        WHEN p.status = 'В работе' THEN '⚙️ В работе'
                        WHEN p.status = 'Запланирован' THEN '📋 Запланирован'
                        ELSE COALESCE(p.status, 'Не указан')
                    END AS status,
                    CASE WHEN p.avariya_id IS NOT NULL THEN '✅' ELSE '❌' END AS has_avariya,
                    COALESCE(p.avariya_id, 0) AS avariya_id,
                    COALESCE(p.stoimost, 0) AS cost,
                    p.is_overdue
                FROM plan_to p
                JOIN oborudovanie o ON p.oborudovanie_id = o.id
                LEFT JOIN tip_to t ON p.tip_to_id = t.id
                LEFT JOIN sotrudniki s ON p.otvetstvenniy_id = s.id
                WHERE p.status != 'Завершен'");

                    if (json.ValueKind != JsonValueKind.Undefined)
                    {
                        bool showAll = json.GetProperty("showAll").GetBoolean();
                        if (!showAll)
                        {
                            string startDate = json.GetProperty("startDate").GetString();
                            string endDate = json.GetProperty("endDate").GetString();
                            if (!string.IsNullOrEmpty(startDate) && !string.IsNullOrEmpty(endDate))
                            {
                                sql.Append($" AND DATE(p.data_nachala) BETWEEN '{startDate}' AND '{endDate}'");
                            }
                        }
                    }

                    sql.Append(" ORDER BY p.data_nachala DESC");

                    using (var cmd = new NpgsqlCommand(sql.ToString(), conn))
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            var list = new List<object>();
                            decimal uncompletedCost = 0;

                            while (reader.Read())
                            {
                                string status = reader.GetString(6);
                                decimal cost = reader.GetDecimal(9);

                                if (status != "✅ Завершен" && status != "Завершен")
                                {
                                    uncompletedCost += cost;
                                }

                                list.Add(new
                                {
                                    id = reader.GetInt32(0),
                                    equipment = reader.GetString(1),
                                    tip = reader.GetString(2),
                                    start_date = reader.GetString(3),
                                    end_date = reader.GetString(4),
                                    responsible = reader.GetString(5),
                                    status = status,
                                    has_avariya = reader.GetString(7),
                                    avariya_id = reader.GetInt32(8),
                                    cost = cost.ToString("N2"),
                                    is_overdue = reader.GetBoolean(10)
                                });
                            }

                            var result = new { plans = list, uncompletedCost = uncompletedCost.ToString("N2") };
                            string jsonData = JsonSerializer.Serialize(result, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
                            SendToWebView("displayPlans", jsonData);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ShowError("Ошибка загрузки планов: " + ex.Message);
            }
        }

        private void LoadAvariya(JsonElement json = default)
        {
            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();

                    StringBuilder sql = new StringBuilder(@"
                        SELECT 
                            a.id,
                            o.nazvanie AS equipment,
                            a.data_avarii AS date,
                            COALESCE(a.opisanie, '') AS description,
                            COALESCE(a.posledstviya, '') AS consequences,
                            COALESCE(a.status, '') AS status,
                            CASE WHEN p.id IS NOT NULL THEN '✅' ELSE '❌' END AS has_plan
                        FROM avariya a
                        JOIN oborudovanie o ON a.oborudovanie_id = o.id
                        LEFT JOIN plan_to p ON a.id = p.avariya_id");

                    if (json.ValueKind != JsonValueKind.Undefined)
                    {
                        bool showAll = json.GetProperty("showAll").GetBoolean();
                        if (!showAll)
                        {
                            string startDate = json.GetProperty("startDate").GetString();
                            string endDate = json.GetProperty("endDate").GetString();
                            sql.Append($" WHERE DATE(a.data_avarii) BETWEEN '{startDate}' AND '{endDate}'");
                        }
                    }

                    sql.Append(" ORDER BY a.data_avarii DESC");

                    using (var cmd = new NpgsqlCommand(sql.ToString(), conn))
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            var list = new List<object>();

                            while (reader.Read())
                            {
                                list.Add(new
                                {
                                    id = reader.GetInt32(0),
                                    equipment = reader.GetString(1),
                                    date = reader.GetDateTime(2).ToString("yyyy-MM-dd HH:mm"),
                                    description = reader.GetString(3),
                                    consequences = reader.GetString(4),
                                    status = reader.GetString(5),
                                    has_plan = reader.GetString(6)
                                });
                            }

                            string jsonData = JsonSerializer.Serialize(list);
                            SendToWebView("displayAvariya", jsonData);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ShowError("Ошибка загрузки аварий: " + ex.Message);
            }
        }

        private async Task LoadRepairHistory(JsonElement json = default)
        {
            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    await conn.OpenAsync();

                    StringBuilder sql = new StringBuilder(@"
                SELECT 
                    COALESCE(r.equipment_name, o.nazvanie) as equipment_name,
                    COALESCE(r.tip_name, COALESCE(t.nazvanie, 'Не указан')) as tip_name,
                    TO_CHAR(p.data_nachala, 'DD.MM.YYYY') as plan_date,
                    TO_CHAR(r.data_okonchaniya, 'DD.MM.YYYY') as completed_date,
                    COALESCE(r.sotrudnik_name, CONCAT(s.familiya, ' ', LEFT(s.imya, 1), '.', LEFT(s.otchestvo, 1), '.')) as sotrudnik_name,
                    COALESCE(r.opisanie, '') as opisanie,
                    COALESCE(p.stoimost, 0) as cost
                FROM remont r
                JOIN plan_to p ON r.plan_id = p.id
                JOIN oborudovanie o ON p.oborudovanie_id = o.id
                LEFT JOIN tip_to t ON p.tip_to_id = t.id
                JOIN sotrudniki s ON r.sotrudnik_id = s.id
                WHERE 1=1");

                    if (json.ValueKind != JsonValueKind.Undefined)
                    {
                        string startDate = json.GetProperty("startDate").GetString();
                        string endDate = json.GetProperty("endDate").GetString();
                        if (!string.IsNullOrEmpty(startDate) && !string.IsNullOrEmpty(endDate))
                        {
                            sql.Append($" AND DATE(r.data_okonchaniya) BETWEEN '{startDate}' AND '{endDate}'");
                        }
                    }

                    sql.Append(" ORDER BY r.data_okonchaniya DESC");

                    using (var cmd = new NpgsqlCommand(sql.ToString(), conn))
                    {
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            var list = new List<object>();
                            decimal totalCost = 0;
                            int totalCount = 0;

                            while (await reader.ReadAsync())
                            {
                                decimal cost = reader.GetDecimal(6);
                                totalCost += cost;
                                totalCount++;

                                list.Add(new
                                {
                                    equipment_name = reader.GetString(0),
                                    tip_name = reader.GetString(1),
                                    plan_date = reader.GetString(2),
                                    completed_date = reader.IsDBNull(3) ? "" : reader.GetString(3),
                                    sotrudnik_name = reader.GetString(4),
                                    opisanie = reader.GetString(5),
                                    cost = cost.ToString("N2")
                                });
                            }

                            var result = new
                            {
                                history = list,
                                totalCost = totalCost.ToString("N2"),
                                totalCount = totalCount
                            };

                            string jsonData = JsonSerializer.Serialize(result, new JsonSerializerOptions
                            {
                                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                            });

                            System.Diagnostics.Debug.WriteLine($"Sending displayHistory with {totalCount} records, totalCost: {totalCost}");
                            SendToWebView("displayHistory", jsonData);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in LoadRepairHistory: {ex.Message}");
                ShowError("Ошибка загрузки истории ремонтов: " + ex.Message);
            }
        }

        private void LoadStatistics()
        {
            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();

                    CheckOverduePlans();

                    DateTime currentMonth = DateTime.Now;
                    string firstDayOfMonth = new DateTime(currentMonth.Year, currentMonth.Month, 1).ToString("yyyy-MM-dd");
                    string lastDayOfMonth = new DateTime(currentMonth.Year, currentMonth.Month, DateTime.DaysInMonth(currentMonth.Year, currentMonth.Month)).ToString("yyyy-MM-dd");

                    string sql = @"
                SELECT 
                    (SELECT COUNT(*) FROM oborudovanie) as total_equipment,
                    (SELECT COUNT(*) FROM avariya) as total_avariya,
                    (SELECT COUNT(*) FROM plan_to WHERE status != 'Завершен') as total_plans,
                    (SELECT COUNT(*) FROM plan_to WHERE status = 'Завершен') as completed_plans,
                    (SELECT COUNT(*) FROM plan_to WHERE status = 'Просрочен') as overdue_plans,
                    (SELECT COUNT(*) FROM plan_to WHERE status = 'В работе') as in_progress_plans,
                    (SELECT COALESCE(SUM(stoimost), 0) FROM plan_to) as total_cost,
                    (SELECT COALESCE(SUM(stoimost), 0) FROM plan_to WHERE DATE(data_nachala) BETWEEN '" + firstDayOfMonth + "' AND '" + lastDayOfMonth + "') as monthly_cost";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                var stats = new
                                {
                                    totalEquipment = reader.GetInt32(0),
                                    totalAvariya = reader.GetInt32(1),
                                    totalPlans = reader.GetInt32(2),
                                    completedPlans = reader.GetInt32(3),
                                    overduePlans = reader.GetInt32(4),
                                    inProgressPlans = reader.GetInt32(5),
                                    totalCost = reader.GetDecimal(6).ToString("N2"),
                                    monthlyCost = reader.GetDecimal(7).ToString("N2")
                                };
                                string json = JsonSerializer.Serialize(stats);
                                SendToWebView("updateStatistics", json);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка статистики: " + ex.Message);
            }
        }

        private void AddPlan(JsonElement json)
        {
            try
            {
                int equipmentId = json.GetProperty("equipment").GetInt32();
                int tipId = json.GetProperty("tip").GetInt32();
                string startDate = json.GetProperty("startDate").GetString();
                string endDate = json.GetProperty("endDate").GetString();
                int responsibleId = json.GetProperty("responsible").GetInt32();
                string status = json.GetProperty("status").GetString();
                decimal cost = json.GetProperty("cost").GetDecimal();

                using (var conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();

                    string sql = @"
                INSERT INTO plan_to 
                (oborudovanie_id, tip_to_id, data_nachala, data_okonchaniya, otvetstvenniy_id, status, stoimost, avariya_id)
                VALUES 
                (@oborudovanie_id, @tip_to_id, @data_nachala, @data_okonchaniya, @otvetstvenniy_id, @status, @stoimost, @avariya_id)";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@oborudovanie_id", equipmentId);
                        cmd.Parameters.AddWithValue("@tip_to_id", tipId);
                        cmd.Parameters.AddWithValue("@data_nachala", DateTime.Parse(startDate));
                        cmd.Parameters.AddWithValue("@data_okonchaniya", DateTime.Parse(endDate));
                        cmd.Parameters.AddWithValue("@otvetstvenniy_id", responsibleId);
                        cmd.Parameters.AddWithValue("@status", status);
                        cmd.Parameters.AddWithValue("@stoimost", cost);

                        if (selectedAvariyaId != -1)
                        {
                            cmd.Parameters.AddWithValue("@avariya_id", selectedAvariyaId);

                            // ОБНОВЛЯЕМ СТАТУС АВАРИИ НА "В работе"
                            string updateAvariyaSql = "UPDATE avariya SET status = 'В работе' WHERE id = @avariya_id";
                            using (var updateCmd = new NpgsqlCommand(updateAvariyaSql, conn))
                            {
                                updateCmd.Parameters.AddWithValue("@avariya_id", selectedAvariyaId);
                                updateCmd.ExecuteNonQuery();
                            }
                        }
                        else
                        {
                            cmd.Parameters.AddWithValue("@avariya_id", DBNull.Value);
                        }

                        cmd.ExecuteNonQuery();
                    }
                }

                SendToWebView("showSuccess", "План успешно добавлен!");
                LoadPlans();
                LoadStatistics();
                selectedAvariyaId = -1;
            }
            catch (PostgresException pgEx)
            {
                ShowError($"Ошибка БД: {pgEx.Message}");
            }
            catch (Exception ex)
            {
                ShowError("Ошибка добавления: " + ex.Message);
            }
        }

        private async Task<decimal> GetTotalCompletedCost()
        {
            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    await conn.OpenAsync();
                    string sql = "SELECT COALESCE(SUM(stoimost), 0) FROM remont";
                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        return Convert.ToDecimal(await cmd.ExecuteScalarAsync());
                    }
                }
            }
            catch
            {
                return 0;
            }
        }

        private void AddToRepairHistory(int planId)
        {
            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();

                    // Проверяем, есть ли уже запись
                    string checkSql = "SELECT COUNT(*) FROM remont WHERE plan_id = @plan_id";
                    using (var checkCmd = new NpgsqlCommand(checkSql, conn))
                    {
                        checkCmd.Parameters.AddWithValue("@plan_id", planId);
                        int exists = Convert.ToInt32(checkCmd.ExecuteScalar());
                        if (exists > 0) return;
                    }

                    // Вставляем с правильной стоимостью из plan_to
                    string sql = @"
                INSERT INTO remont (plan_id, oborudovanie_id, sotrudnik_id, data_nachala, data_okonchaniya, opisanie, stoimost, equipment_name, tip_name, sotrudnik_name)
                SELECT 
                    p.id,
                    p.oborudovanie_id,
                    p.otvetstvenniy_id,
                    p.data_nachala,
                    p.data_okonchaniya,
                    COALESCE(r.opisanie, 'Завершено вручную'),
                    p.stoimost,
                    o.nazvanie,
                    COALESCE(t.nazvanie, 'Не указан'),
                    CONCAT(s.familiya, ' ', LEFT(s.imya, 1), '.', LEFT(s.otchestvo, 1), '.')
                FROM plan_to p
                JOIN oborudovanie o ON p.oborudovanie_id = o.id
                LEFT JOIN tip_to t ON p.tip_to_id = t.id
                JOIN sotrudniki s ON p.otvetstvenniy_id = s.id
                LEFT JOIN remont r ON p.id = r.plan_id
                WHERE p.id = @plan_id";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@plan_id", planId);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка добавления в историю: {ex.Message}");
            }
        }
        private void UpdatePlan(JsonElement json)
        {
            try
            {
                int id = json.GetProperty("id").GetInt32();
                int equipmentId = json.GetProperty("equipment").GetInt32();
                int tipId = json.GetProperty("tip").GetInt32();
                string startDate = json.GetProperty("startDate").GetString();
                string endDate = json.GetProperty("endDate").GetString();
                int responsibleId = json.GetProperty("responsible").GetInt32();
                string status = json.GetProperty("status").GetString();
                decimal cost = json.GetProperty("cost").GetDecimal();

                // Получаем СТАРЫЙ статус до обновления
                string oldStatus = "";
                using (var connCheck = new NpgsqlConnection(connectionString))
                {
                    connCheck.Open();
                    string checkSql = "SELECT status FROM plan_to WHERE id = @id";
                    using (var cmdCheck = new NpgsqlCommand(checkSql, connCheck))
                    {
                        cmdCheck.Parameters.AddWithValue("@id", id);
                        var result = cmdCheck.ExecuteScalar();
                        if (result != null) oldStatus = result.ToString();
                    }
                }

                // Обновляем план
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();
                    string sql = @"
                UPDATE plan_to SET
                    oborudovanie_id = @oborudovanie_id,
                    tip_to_id = @tip_to_id,
                    data_nachala = @data_nachala,
                    data_okonchaniya = @data_okonchaniya,
                    otvetstvenniy_id = @otvetstvenniy_id,
                    status = @status,
                    stoimost = @stoimost
                WHERE id = @id";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        cmd.Parameters.AddWithValue("@oborudovanie_id", equipmentId);
                        cmd.Parameters.AddWithValue("@tip_to_id", tipId);
                        cmd.Parameters.AddWithValue("@data_nachala", DateTime.Parse(startDate));
                        cmd.Parameters.AddWithValue("@data_okonchaniya", DateTime.Parse(endDate));
                        cmd.Parameters.AddWithValue("@otvetstvenniy_id", responsibleId);
                        cmd.Parameters.AddWithValue("@status", status);
                        cmd.Parameters.AddWithValue("@stoimost", cost);

                        cmd.ExecuteNonQuery();
                    }
                }

                // ЕСЛИ СТАТУС ИЗМЕНИЛСЯ НА "Завершен" - добавляем в историю ремонтов
                if (status == "Завершен" && oldStatus != "Завершен")
                {
                    AddToRepairHistory(id);
                }

                SendToWebView("showSuccess", "План успешно обновлен!");
                LoadPlans();
                LoadStatistics();
                LoadRepairHistory();
            }
            catch (Exception ex)
            {
                ShowError("Ошибка обновления: " + ex.Message);
            }
        }

        private void AddToRepairHistory(int planId, int responsibleId, string startDate, string endDate)
        {
            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();

                    string sql = @"
                        INSERT INTO remont (plan_id, oborudovanie_id, sotrudnik_id, data_nachala, data_okonchaniya, opisanie, stoimost, equipment_name, tip_name, sotrudnik_name)
                        SELECT 
                            p.id,
                            p.oborudovanie_id,
                            p.otvetstvenniy_id,
                            p.data_nachala,
                            p.data_okonchaniya,
                            COALESCE(r.opisanie, 'Завершено вручную'),
                            p.stoimost,
                            o.nazvanie,
                            COALESCE(t.nazvanie, 'Не указан'),
                            CONCAT(s.familiya, ' ', LEFT(s.imya, 1), '.', LEFT(s.otchestvo, 1), '.')
                        FROM plan_to p
                        JOIN oborudovanie o ON p.oborudovanie_id = o.id
                        LEFT JOIN tip_to t ON p.tip_to_id = t.id
                        JOIN sotrudniki s ON p.otvetstvenniy_id = s.id
                        LEFT JOIN remont r ON p.id = r.plan_id
                        WHERE p.id = @id AND r.id IS NULL";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", planId);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка добавления в историю: {ex.Message}");
            }
        }

        private void DeletePlan(JsonElement json)
        {
            try
            {
                int id = json.GetProperty("id").GetInt32();

                using (var conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();
                    string sql = "DELETE FROM plan_to WHERE id = @id";
                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        cmd.ExecuteNonQuery();
                    }
                }

                SendToWebView("showSuccess", "План успешно удален!");
                LoadPlans();
                LoadStatistics();
            }
            catch (Exception ex)
            {
                ShowError("Ошибка удаления: " + ex.Message);
            }
        }

        private void CreatePlanFromAvariya(JsonElement json)
        {
            int avariyaId = json.GetProperty("id").GetInt32();

            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();
                    string sql = "SELECT oborudovanie_id FROM avariya WHERE id = @id";
                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", avariyaId);
                        var result = cmd.ExecuteScalar();

                        if (result != null)
                        {
                            int oborudovanieId = Convert.ToInt32(result);
                            selectedAvariyaId = avariyaId;

                            string js = $"selectEquipmentById({oborudovanieId}); switchToPlansTab();";
                            webView.CoreWebView2.ExecuteScriptAsync(js);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ShowError("Ошибка создания плана: " + ex.Message);
            }
        }

        private async void ExportToExcel()
        {
            try
            {
                string reportType = await GetReportTypeFromWebView();
                ExportToExcelWithType(reportType);
            }
            catch (Exception ex)
            {
                ShowError("Ошибка экспорта: " + ex.Message);
            }
        }

        private async Task<string> GetReportTypeFromWebView()
        {
            if (webView?.CoreWebView2 != null)
            {
                try
                {
                    string script = "document.getElementById('reportTypeSelect')?.value || 'all'";
                    string result = await webView.CoreWebView2.ExecuteScriptAsync(script);
                    // Получаем значение из JS
                    string value = result.Trim('"');

                    // Преобразуем значение в текст
                    switch (value)
                    {
                        case "planned": return "Запланированные";
                        case "inprogress": return "В работе";
                        case "completed": return "Завершенные";
                        case "overdue": return "Просроченные";
                        case "avariya": return "Аварии";
                        case "history": return "История ремонтов";
                        default: return "Все планы";
                    }
                }
                catch
                {
                    return currentReportType;
                }
            }
            return currentReportType;
        }

        private void ExportToWordWithType(string reportType)
        {
            try
            {
                SaveFileDialog save = new SaveFileDialog();
                save.Filter = "Rich Text Format (*.rtf)|*.rtf";

                string fileName = "";
                string sql = "";
                string[] headers = null;

                switch (reportType)
                {
                    case "Аварии":
                        fileName = $"Отчет_об_авариях_{DateTime.Now:dd-MM-yyyy}.rtf";
                        sql = @"
                    SELECT 
                        a.id,
                        o.nazvanie AS equipment,
                        a.data_avarii AS date,
                        COALESCE(a.opisanie, '') AS description,
                        COALESCE(a.posledstviya, '') AS consequences,
                        COALESCE(a.status, '') AS status,
                        CASE WHEN p.id IS NOT NULL THEN 'Да' ELSE 'Нет' END AS has_plan
                    FROM avariya a
                    JOIN oborudovanie o ON a.oborudovanie_id = o.id
                    LEFT JOIN plan_to p ON a.id = p.avariya_id
                    ORDER BY a.data_avarii DESC";
                        headers = new string[] { "ID", "Оборудование", "Дата аварии", "Описание", "Последствия", "Статус", "Наличие плана" };
                        break;

                    case "История ремонтов":
                        fileName = $"Отчет_об_истории_ремонтов_{DateTime.Now:dd-MM-yyyy}.rtf";
                        sql = @"
                    SELECT 
                        COALESCE(r.equipment_name, o.nazvanie) as equipment_name,
                        COALESCE(r.tip_name, COALESCE(t.nazvanie, 'Не указан')) as tip_name,
                        TO_CHAR(p.data_nachala, 'DD.MM.YYYY') as plan_date,
                        TO_CHAR(r.data_okonchaniya, 'DD.MM.YYYY') as completed_date,
                        COALESCE(r.sotrudnik_name, CONCAT(s.familiya, ' ', LEFT(s.imya, 1), '.', LEFT(s.otchestvo, 1), '.')) as sotrudnik_name,
                        COALESCE(r.opisanie, '') as opisanie,
                        COALESCE(p.stoimost, 0) as cost
                    FROM remont r
                    JOIN plan_to p ON r.plan_id = p.id
                    JOIN oborudovanie o ON p.oborudovanie_id = o.id
                    LEFT JOIN tip_to t ON p.tip_to_id = t.id
                    JOIN sotrudniki s ON r.sotrudnik_id = s.id
                    ORDER BY r.data_okonchaniya DESC";
                        headers = new string[] { "Оборудование", "Тип ТО", "Плановая дата", "Дата выполнения", "Исполнитель", "Описание работ", "Стоимость (руб)" };
                        break;

                    case "Завершенные":
                    case "Запланированные":
                    case "В работе":
                    case "Просроченные":
                    default: // "Все планы"
                        fileName = $"Отчет_о_планах_ремонтов_{DateTime.Now:dd-MM-yyyy}.rtf";
                        sql = @"
                    SELECT 
                        p.id,
                        o.nazvanie AS equipment,
                        COALESCE(t.nazvanie, 'Не указан') AS tip,
                        TO_CHAR(p.data_nachala, 'DD.MM.YYYY') as start_date,
                        TO_CHAR(p.data_okonchaniya, 'DD.MM.YYYY') as end_date,
                        COALESCE(s.familiya || ' ' || s.imya || ' ' || s.otchestvo, 'Не назначен') AS responsible,
                        p.status,
                        CASE WHEN p.avariya_id IS NOT NULL THEN 'Да' ELSE 'Нет' END AS has_avariya,
                        p.stoimost AS cost
                    FROM plan_to p
                    JOIN oborudovanie o ON p.oborudovanie_id = o.id
                    LEFT JOIN tip_to t ON p.tip_to_id = t.id
                    LEFT JOIN sotrudniki s ON p.otvetstvenniy_id = s.id";

                        if (reportType == "Завершенные")
                        {
                            fileName = $"Отчет_о_завершенных_планах_{DateTime.Now:dd-MM-yyyy}.rtf";
                            sql += " WHERE p.status = 'Завершен' ORDER BY p.data_okonchaniya DESC";
                        }
                        else if (reportType == "Запланированные")
                        {
                            fileName = $"Отчет_о_запланированных_планах_{DateTime.Now:dd-MM-yyyy}.rtf";
                            sql += " WHERE p.status = 'Запланирован' ORDER BY p.data_nachala ASC";
                        }
                        else if (reportType == "В работе")
                        {
                            fileName = $"Отчет_о_планах_в_работе_{DateTime.Now:dd-MM-yyyy}.rtf";
                            sql += " WHERE p.status = 'В работе' ORDER BY p.data_nachala ASC";
                        }
                        else if (reportType == "Просроченные")
                        {
                            fileName = $"Отчет_о_просроченных_планах_{DateTime.Now:dd-MM-yyyy}.rtf";
                            sql += " WHERE p.status = 'Просрочен' ORDER BY p.data_okonchaniya ASC";
                        }
                        else
                        {
                            sql += " ORDER BY p.data_nachala DESC";
                        }

                        headers = new string[] { "ID", "Оборудование", "Тип ТО", "Дата начала", "Дата окончания", "Ответственный", "Статус", "Связь с аварией", "Стоимость (руб)" };
                        break;
                }

                save.FileName = fileName;

                if (save.ShowDialog() == DialogResult.OK)
                {
                    using (var conn = new NpgsqlConnection(connectionString))
                    {
                        conn.Open();

                        using (var cmd = new NpgsqlCommand(sql, conn))
                        {
                            using (var reader = cmd.ExecuteReader())
                            {
                                using (StreamWriter sw = new StreamWriter(save.FileName, false, Encoding.UTF8))
                                {
                                    // RTF заголовок
                                    sw.WriteLine(@"{\rtf1\ansi\deff0");
                                    sw.WriteLine(@"{\fonttbl{\f0 Times New Roman;}{\f1 Arial;}}");
                                    sw.WriteLine(@"\f0\fs24");

                                    // Заголовок отчета
                                    sw.WriteLine(@"\pard\qc\b\fs32 Отчет: " + reportType + @"\b0\fs24\par");
                                    sw.WriteLine(@"\pard\qc\fs20 Дата формирования: " + DateTime.Now.ToString("dd.MM.yyyy HH:mm") + @"\par");
                                    sw.WriteLine(@"\par");

                                    int columnCount = headers.Length;

                                    // Создаем таблицу
                                    sw.WriteLine(@"\trowd");
                                    for (int i = 0; i < columnCount; i++)
                                        sw.WriteLine(@"\cellx" + ((i + 1) * 2000));

                                    sw.WriteLine(@"\clbrdrt\brdrw10\brdrs");
                                    sw.WriteLine(@"\clbrdrl\brdrw10\brdrs");
                                    sw.WriteLine(@"\clbrdrb\brdrw10\brdrs");
                                    sw.WriteLine(@"\clbrdrr\brdrw10\brdrs");
                                    sw.WriteLine(@"\clcbpat8\cell");
                                    sw.WriteLine(@"\intbl\b\fs20 ");

                                    // Заголовки столбцов (русские)
                                    for (int i = 0; i < headers.Length; i++)
                                    {
                                        sw.Write(headers[i] + @" \cell ");
                                    }
                                    sw.WriteLine(@"\row\b0");

                                    // Данные
                                    int count = 0;
                                    while (reader.Read())
                                    {
                                        sw.WriteLine(@"\trowd");
                                        for (int i = 0; i < columnCount; i++)
                                            sw.WriteLine(@"\cellx" + ((i + 1) * 2000));

                                        sw.WriteLine(@"\clbrdrt\brdrw10\brdrs");
                                        sw.WriteLine(@"\clbrdrl\brdrw10\brdrs");
                                        sw.WriteLine(@"\clbrdrb\brdrw10\brdrs");
                                        sw.WriteLine(@"\clbrdrr\brdrw10\brdrs");

                                        if (count % 2 == 0)
                                            sw.WriteLine(@"\clcbpat1");

                                        sw.WriteLine(@"\cell");
                                        sw.WriteLine(@"\intbl\fs20 ");

                                        for (int i = 0; i < reader.FieldCount; i++)
                                        {
                                            string value = reader[i]?.ToString() ?? "";
                                            sw.Write(value + @" \cell ");
                                        }
                                        sw.WriteLine(@"\row");
                                        count++;
                                    }

                                    sw.WriteLine(@"\pard\qr\fs20 Всего записей: " + count + @"\par");
                                    sw.WriteLine(@"}");
                                }
                            }
                        }
                    }

                    MessageBox.Show($"Отчет сохранен", "Успешно", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                ShowError("Ошибка экспорта в Word: " + ex.Message);
            }
        }

        private void ExportToExcelWithType(string reportType)
        {
            try
            {
                SaveFileDialog save = new SaveFileDialog();
                save.Filter = "CSV files (*.csv)|*.csv";

                string fileName = "";
                string sql = "";
                string[] headers = null;

                switch (reportType)
                {
                    case "Аварии":
                        fileName = $"Отчет_об_авариях_{DateTime.Now:dd-MM-yyyy}.csv";
                        sql = @"
                    SELECT 
                        a.id,
                        o.nazvanie AS equipment,
                        a.data_avarii AS date,
                        COALESCE(a.opisanie, '') AS description,
                        COALESCE(a.posledstviya, '') AS consequences,
                        COALESCE(a.status, '') AS status,
                        CASE WHEN p.id IS NOT NULL THEN 'Да' ELSE 'Нет' END AS has_plan
                    FROM avariya a
                    JOIN oborudovanie o ON a.oborudovanie_id = o.id
                    LEFT JOIN plan_to p ON a.id = p.avariya_id
                    ORDER BY a.data_avarii DESC";
                        headers = new string[] { "ID", "Оборудование", "Дата аварии", "Описание", "Последствия", "Статус", "Наличие плана" };
                        break;

                    case "История ремонтов":
                        fileName = $"Отчет_об_истории_ремонтов_{DateTime.Now:dd-MM-yyyy}.csv";
                        sql = @"
                    SELECT 
                        COALESCE(r.equipment_name, o.nazvanie) as equipment_name,
                        COALESCE(r.tip_name, COALESCE(t.nazvanie, 'Не указан')) as tip_name,
                        TO_CHAR(p.data_nachala, 'DD.MM.YYYY') as plan_date,
                        TO_CHAR(r.data_okonchaniya, 'DD.MM.YYYY') as completed_date,
                        COALESCE(r.sotrudnik_name, CONCAT(s.familiya, ' ', LEFT(s.imya, 1), '.', LEFT(s.otchestvo, 1), '.')) as sotrudnik_name,
                        COALESCE(r.opisanie, '') as opisanie,
                        COALESCE(p.stoimost, 0) as cost
                    FROM remont r
                    JOIN plan_to p ON r.plan_id = p.id
                    JOIN oborudovanie o ON p.oborudovanie_id = o.id
                    LEFT JOIN tip_to t ON p.tip_to_id = t.id
                    JOIN sotrudniki s ON r.sotrudnik_id = s.id
                    ORDER BY r.data_okonchaniya DESC";
                        headers = new string[] { "Оборудование", "Тип ТО", "Плановая дата", "Дата выполнения", "Исполнитель", "Описание работ", "Стоимость (руб)" };
                        break;

                    case "Завершенные":
                        fileName = $"Отчет_о_завершенных_планах_{DateTime.Now:dd-MM-yyyy}.csv";
                        sql = @"
                    SELECT 
                        p.id,
                        o.nazvanie AS equipment,
                        COALESCE(t.nazvanie, 'Не указан') AS tip,
                        TO_CHAR(p.data_nachala, 'DD.MM.YYYY') as start_date,
                        TO_CHAR(p.data_okonchaniya, 'DD.MM.YYYY') as end_date,
                        COALESCE(s.familiya || ' ' || s.imya || ' ' || s.otchestvo, 'Не назначен') AS responsible,
                        p.status,
                        CASE WHEN p.avariya_id IS NOT NULL THEN 'Да' ELSE 'Нет' END AS has_avariya,
                        p.stoimost AS cost
                    FROM plan_to p
                    JOIN oborudovanie o ON p.oborudovanie_id = o.id
                    LEFT JOIN tip_to t ON p.tip_to_id = t.id
                    LEFT JOIN sotrudniki s ON p.otvetstvenniy_id = s.id
                    WHERE p.status = 'Завершен'
                    ORDER BY p.data_okonchaniya DESC";
                        headers = new string[] { "ID", "Оборудование", "Тип ТО", "Дата начала", "Дата окончания", "Ответственный", "Статус", "Связь с аварией", "Стоимость (руб)" };
                        break;

                    case "Запланированные":
                        fileName = $"Отчет_о_запланированных_планах_{DateTime.Now:dd-MM-yyyy}.csv";
                        sql = @"
                    SELECT 
                        p.id,
                        o.nazvanie AS equipment,
                        COALESCE(t.nazvanie, 'Не указан') AS tip,
                        TO_CHAR(p.data_nachala, 'DD.MM.YYYY') as start_date,
                        TO_CHAR(p.data_okonchaniya, 'DD.MM.YYYY') as end_date,
                        COALESCE(s.familiya || ' ' || s.imya || ' ' || s.otchestvo, 'Не назначен') AS responsible,
                        p.status,
                        CASE WHEN p.avariya_id IS NOT NULL THEN 'Да' ELSE 'Нет' END AS has_avariya,
                        p.stoimost AS cost
                    FROM plan_to p
                    JOIN oborudovanie o ON p.oborudovanie_id = o.id
                    LEFT JOIN tip_to t ON p.tip_to_id = t.id
                    LEFT JOIN sotrudniki s ON p.otvetstvenniy_id = s.id
                    WHERE p.status = 'Запланирован'
                    ORDER BY p.data_nachala ASC";
                        headers = new string[] { "ID", "Оборудование", "Тип ТО", "Дата начала", "Дата окончания", "Ответственный", "Статус", "Связь с аварией", "Стоимость (руб)" };
                        break;

                    case "В работе":
                        fileName = $"Отчет_о_планах_в_работе_{DateTime.Now:dd-MM-yyyy}.csv";
                        sql = @"
                    SELECT 
                        p.id,
                        o.nazvanie AS equipment,
                        COALESCE(t.nazvanie, 'Не указан') AS tip,
                        TO_CHAR(p.data_nachala, 'DD.MM.YYYY') as start_date,
                        TO_CHAR(p.data_okonchaniya, 'DD.MM.YYYY') as end_date,
                        COALESCE(s.familiya || ' ' || s.imya || ' ' || s.otchestvo, 'Не назначен') AS responsible,
                        p.status,
                        CASE WHEN p.avariya_id IS NOT NULL THEN 'Да' ELSE 'Нет' END AS has_avariya,
                        p.stoimost AS cost
                    FROM plan_to p
                    JOIN oborudovanie o ON p.oborudovanie_id = o.id
                    LEFT JOIN tip_to t ON p.tip_to_id = t.id
                    LEFT JOIN sotrudniki s ON p.otvetstvenniy_id = s.id
                    WHERE p.status = 'В работе'
                    ORDER BY p.data_nachala ASC";
                        headers = new string[] { "ID", "Оборудование", "Тип ТО", "Дата начала", "Дата окончания", "Ответственный", "Статус", "Связь с аварией", "Стоимость (руб)" };
                        break;

                    case "Просроченные":
                        fileName = $"Отчет_о_просроченных_планах_{DateTime.Now:dd-MM-yyyy}.csv";
                        sql = @"
                    SELECT 
                        p.id,
                        o.nazvanie AS equipment,
                        COALESCE(t.nazvanie, 'Не указан') AS tip,
                        TO_CHAR(p.data_nachala, 'DD.MM.YYYY') as start_date,
                        TO_CHAR(p.data_okonchaniya, 'DD.MM.YYYY') as end_date,
                        COALESCE(s.familiya || ' ' || s.imya || ' ' || s.otchestvo, 'Не назначен') AS responsible,
                        p.status,
                        CASE WHEN p.avariya_id IS NOT NULL THEN 'Да' ELSE 'Нет' END AS has_avariya,
                        p.stoimost AS cost
                    FROM plan_to p
                    JOIN oborudovanie o ON p.oborudovanie_id = o.id
                    LEFT JOIN tip_to t ON p.tip_to_id = t.id
                    LEFT JOIN sotrudniki s ON p.otvetstvenniy_id = s.id
                    WHERE p.status = 'Просрочен'
                    ORDER BY p.data_okonchaniya ASC";
                        headers = new string[] { "ID", "Оборудование", "Тип ТО", "Дата начала", "Дата окончания", "Ответственный", "Статус", "Связь с аварией", "Стоимость (руб)" };
                        break;

                    default: // "Все планы"
                        fileName = $"Отчет_о_планах_ремонтов_{DateTime.Now:dd-MM-yyyy}.csv";
                        sql = @"
                    SELECT 
                        p.id,
                        o.nazvanie AS equipment,
                        COALESCE(t.nazvanie, 'Не указан') AS tip,
                        TO_CHAR(p.data_nachala, 'DD.MM.YYYY') as start_date,
                        TO_CHAR(p.data_okonchaniya, 'DD.MM.YYYY') as end_date,
                        COALESCE(s.familiya || ' ' || s.imya || ' ' || s.otchestvo, 'Не назначен') AS responsible,
                        p.status,
                        CASE WHEN p.avariya_id IS NOT NULL THEN 'Да' ELSE 'Нет' END AS has_avariya,
                        p.stoimost AS cost
                    FROM plan_to p
                    JOIN oborudovanie o ON p.oborudovanie_id = o.id
                    LEFT JOIN tip_to t ON p.tip_to_id = t.id
                    LEFT JOIN sotrudniki s ON p.otvetstvenniy_id = s.id
                    ORDER BY p.data_nachala DESC";
                        headers = new string[] { "ID", "Оборудование", "Тип ТО", "Дата начала", "Дата окончания", "Ответственный", "Статус", "Связь с аварией", "Стоимость (руб)" };
                        break;
                }

                save.FileName = fileName;

                if (save.ShowDialog() == DialogResult.OK)
                {
                    using (var conn = new NpgsqlConnection(connectionString))
                    {
                        conn.Open();

                        using (var cmd = new NpgsqlCommand(sql, conn))
                        {
                            using (var reader = cmd.ExecuteReader())
                            {
                                using (StreamWriter sw = new StreamWriter(save.FileName, false, Encoding.UTF8))
                                {
                                    // Заголовок отчета
                                    sw.WriteLine($"Отчет: {reportType}");
                                    sw.WriteLine($"Дата формирования: {DateTime.Now:dd.MM.yyyy HH:mm}");
                                    sw.WriteLine();

                                    // Записываем русские заголовки столбцов
                                    for (int i = 0; i < headers.Length; i++)
                                    {
                                        if (i > 0) sw.Write(";");
                                        sw.Write(headers[i]);
                                    }
                                    sw.WriteLine();

                                    // Записываем данные
                                    int count = 0;
                                    while (reader.Read())
                                    {
                                        for (int i = 0; i < reader.FieldCount; i++)
                                        {
                                            if (i > 0) sw.Write(";");
                                            string value = reader[i]?.ToString() ?? "";
                                            sw.Write(value);
                                        }
                                        sw.WriteLine();
                                        count++;
                                    }

                                    sw.WriteLine();
                                    sw.WriteLine($"Всего записей: {count}");
                                }
                            }
                        }
                    }

                    MessageBox.Show($"Отчет сохранен", "Успешно", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                ShowError("Ошибка экспорта: " + ex.Message);
            }
        }

        private async void ExportToWord()
        {
            try
            {
                string reportType = await GetReportTypeFromWebView();
                ExportToWordWithType(reportType);
            }
            catch (Exception ex)
            {
                ShowError("Ошибка экспорта: " + ex.Message);
            }
        }

        private void PreviewReport()
        {
            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();

                    string planStats = @"
                        SELECT 
                            COUNT(*) as total,
                            COUNT(CASE WHEN status = 'Завершен' THEN 1 END) as completed,
                            COUNT(CASE WHEN status = 'В работе' THEN 1 END) as in_progress,
                            COUNT(CASE WHEN status = 'Запланирован' THEN 1 END) as planned,
                            COUNT(CASE WHEN status = 'Просрочен' THEN 1 END) as overdue,
                            COALESCE(SUM(stoimost), 0) as total_cost
                        FROM plan_to";

                    int totalPlans = 0, completed = 0, inProgress = 0, planned = 0, overdue = 0;
                    decimal totalCost = 0;

                    using (var cmd = new NpgsqlCommand(planStats, conn))
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                totalPlans = reader.GetInt32(0);
                                completed = reader.GetInt32(1);
                                inProgress = reader.GetInt32(2);
                                planned = reader.GetInt32(3);
                                overdue = reader.GetInt32(4);
                                totalCost = reader.GetDecimal(5);
                            }
                        }
                    }

                    string avariyaStats = @"
                        SELECT 
                            COUNT(*) as total,
                            COUNT(CASE WHEN p.id IS NOT NULL THEN 1 END) as with_plan
                        FROM avariya a
                        LEFT JOIN plan_to p ON a.id = p.avariya_id";

                    int totalAvariya = 0, withPlan = 0;

                    using (var cmd = new NpgsqlCommand(avariyaStats, conn))
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                totalAvariya = reader.GetInt32(0);
                                withPlan = reader.GetInt32(1);
                            }
                        }
                    }

                    int historyCount = GetHistoryCount();

                    int withoutPlan = totalAvariya - withPlan;
                    int percent = totalPlans > 0 ? (completed * 100 / totalPlans) : 0;

                    DateTime currentMonth = DateTime.Now;
                    string monthName = currentMonth.ToString("MMMM yyyy");

                    string message = $"╔══════════════════════════════════════════════════════════╗\n" +
                                    $"║           ПРЕДПРОСМОТР ОТЧЕТА                           ║\n" +
                                    $"╠══════════════════════════════════════════════════════════╣\n" +
                                    $"║ Проект: Котельная                                        ║\n" +
                                    $"║ Дата: {DateTime.Now:dd.MM.yyyy HH:mm}                              ║\n" +
                                    $"╟──────────────────────────────────────────────────────────╢\n" +
                                    $"║ СТАТИСТИКА:                                              ║\n" +
                                    $"║ Всего планов: {totalPlans,-38} ║\n" +
                                    $"║ ├─ Завершено: {completed,-37} ║\n" +
                                    $"║ ├─ В работе: {inProgress,-38} ║\n" +
                                    $"║ ├─ Запланировано: {planned,-34} ║\n" +
                                    $"║ └─ Просрочено: {overdue,-38} ║\n" +
                                    $"║ Процент выполнения: {percent,-3}%                                     ║\n" +
                                    $"║ Общая стоимость: {totalCost:N2} руб.                     ║\n" +
                                    $"║ Стоимость за {monthName}: {GetMonthlyCost():N2} руб.              ║\n" +
                                    $"╟──────────────────────────────────────────────────────────╢\n" +
                                    $"║ АВАРИИ:                                                  ║\n" +
                                    $"║ Всего аварий: {totalAvariya,-39} ║\n" +
                                    $"║ С планом: {withPlan,-42} ║\n" +
                                    $"║ Без плана: {withoutPlan,-41} ║\n" +
                                    $"╟──────────────────────────────────────────────────────────╢\n" +
                                    $"║ ИСТОРИЯ РЕМОНТОВ:                                        ║\n" +
                                    $"║ Выполненных ремонтов: {historyCount,-33} ║\n" +
                                    $"╚══════════════════════════════════════════════════════════╝";

                    MessageBox.Show(message, "Предпросмотр отчета", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                ShowError("Ошибка предпросмотра: " + ex.Message);
            }
        }

        private int GetHistoryCount()
        {
            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();
                    string sql = "SELECT COUNT(*) FROM remont";
                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        return Convert.ToInt32(cmd.ExecuteScalar());
                    }
                }
            }
            catch
            {
                return 0;
            }
        }

        private decimal GetMonthlyCost()
        {
            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();
                    DateTime currentMonth = DateTime.Now;
                    string firstDay = new DateTime(currentMonth.Year, currentMonth.Month, 1).ToString("yyyy-MM-dd");
                    string lastDay = new DateTime(currentMonth.Year, currentMonth.Month, DateTime.DaysInMonth(currentMonth.Year, currentMonth.Month)).ToString("yyyy-MM-dd");

                    string sql = $"SELECT COALESCE(SUM(stoimost), 0) FROM plan_to WHERE DATE(data_nachala) BETWEEN '{firstDay}' AND '{lastDay}'";
                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        return Convert.ToDecimal(cmd.ExecuteScalar());
                    }
                }
            }
            catch
            {
                return 0;
            }
        }

        private string EscapeJson(string s)
        {
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r");
        }

        private void SendToWebView(string command, string data)
        {
            if (webView?.CoreWebView2 != null)
            {
                try
                {
                    string js;
                    if (data.StartsWith("[") || data.StartsWith("{"))
                    {
                        js = $"window.receiveFromCSharp('{command}', {data})";
                    }
                    else
                    {
                        js = $"window.receiveFromCSharp('{command}', '{EscapeJson(data)}')";
                    }

                    webView.CoreWebView2.ExecuteScriptAsync(js);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка отправки: {ex.Message}");
                }
            }
        }

        private void ShowError(string message)
        {
            if (webView?.CoreWebView2 != null)
            {
                string js = $"alert('{EscapeJson(message)}')";
                webView.CoreWebView2.ExecuteScriptAsync(js);
            }
            else
            {
                MessageBox.Show(message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}

