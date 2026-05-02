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
    public partial class FormBoss : Form
    {
        private string connectionString;
        private int currentUserId;
        private WebView2 webView;
        private int selectedPlanId = -1;
        private int selectedAvariyaId = -1;

        // Строка подключения (замените на свои данные)
        private readonly string connString = "Host=localhost;Port=5432;Database=boiler_system;Username=postgres;Password=43898362Dd+-;Include Error Detail=true";

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

                webView.CoreWebView2.WebMessageReceived += HandleWebMessage;
                webView.CoreWebView2.NavigationCompleted += async (s, e) =>
                {
                    await Task.Delay(500);
                    LoadInitialData();
                };
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка WebView2: {ex.Message}");
            }
        }

        private void HandleWebMessage(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            string message = e.TryGetWebMessageAsString();
            try
            {
                if (message.StartsWith("{"))
                {
                    var json = JsonDocument.Parse(message).RootElement;
                    string action = json.GetProperty("action").GetString();

                    switch (action)
                    {
                        case "loadPlans": LoadPlans(json); break;
                        case "loadAvariya": LoadAvariya(json); break;
                        case "loadHistory": LoadRepairHistory(json); break;
                        case "addPlan": AddPlan(json); break;
                        case "updatePlan": UpdatePlan(json); break;
                        case "deletePlan": DeletePlan(json); break;
                        case "createPlanFromAvariya": CreatePlanFromAvariya(json); break;
                        case "exportToExcel": ExportToExcel(); break;
                        case "exportToWord": ExportToWord(); break;
                        case "previewReport": PreviewReport(); break;
                    }
                }
                else
                {
                    switch (message)
                    {
                        case "loadEquipment": LoadEquipment(); break;
                        case "loadTipTypes": LoadTipTypes(); break;
                        case "loadResponsible": LoadResponsible(); break;
                        case "loadStatistics": LoadStatistics(); break;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка: {ex.Message}");
            }
        }

        private void LoadInitialData()
        {
            LoadEquipment();
            LoadTipTypes();
            LoadResponsible();
            LoadPlans();
            LoadAvariya();
            LoadStatistics();
            LoadRepairHistory();
        }

        private void LoadEquipment()
        {
            try
            {
                using (var conn = new NpgsqlConnection(connString))
                {
                    conn.Open();
                    string sql = "SELECT id, nazvanie FROM oborudovanie ORDER BY nazvanie";
                    using (var cmd = new NpgsqlCommand(sql, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        var list = new List<object>();
                        while (reader.Read())
                            list.Add(new { id = reader.GetInt32(0), name = reader.GetString(1) });
                        SendToWebView("fillEquipment", JsonSerializer.Serialize(list));
                    }
                }
            }
            catch (Exception ex) { ShowError("Ошибка загрузки оборудования: " + ex.Message); }
        }

        private void LoadTipTypes()
        {
            try
            {
                using (var conn = new NpgsqlConnection(connString))
                {
                    conn.Open();
                    string sql = "SELECT id, nazvanie FROM tip_to ORDER BY nazvanie";
                    using (var cmd = new NpgsqlCommand(sql, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        var list = new List<object>();
                        while (reader.Read())
                            list.Add(new { id = reader.GetInt32(0), name = reader.GetString(1) });
                        SendToWebView("fillTipTypes", JsonSerializer.Serialize(list));
                    }
                }
            }
            catch (Exception ex) { ShowError("Ошибка загрузки типов ТО: " + ex.Message); }
        }

        private void LoadResponsible()
        {
            try
            {
                using (var conn = new NpgsqlConnection(connString))
                {
                    conn.Open();
                    string sql = @"
                        SELECT id, familiya || ' ' || imya || ' ' || otchestvo AS fio 
                        FROM sotrudniki 
                        WHERE dolzhnost ILIKE '%слесар%' 
                        ORDER BY familiya";
                    using (var cmd = new NpgsqlCommand(sql, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        var list = new List<object>();
                        while (reader.Read())
                            list.Add(new { id = reader.GetInt32(0), name = reader.GetString(1) });
                        SendToWebView("fillResponsible", JsonSerializer.Serialize(list));
                    }
                }
            }
            catch (Exception ex) { ShowError("Ошибка загрузки ответственных: " + ex.Message); }
        }

        private void LoadPlans(JsonElement json = default)
        {
            try
            {
                using (var conn = new NpgsqlConnection(connString))
                {
                    conn.Open();
                    var sql = new StringBuilder(@"
                        SELECT 
                            p.id, o.nazvanie AS equipment, COALESCE(t.nazvanie, 'Не указан') AS tip,
                            TO_CHAR(p.data_nachala, 'DD.MM.YYYY') as start_date,
                            TO_CHAR(p.data_okonchaniya, 'DD.MM.YYYY') as end_date,
                            COALESCE(s.familiya || ' ' || s.imya || ' ' || s.otchestvo, 'Не назначен') AS responsible,
                            CASE 
                                WHEN p.status = 'Просрочен' THEN '🔴 Просрочен'
                                WHEN p.status = 'Завершен' THEN '✅ Завершен'
                                WHEN p.status = 'В работе' THEN '⚙️ В работе'
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
                        if (!showAll && json.TryGetProperty("startDate", out var s) && json.TryGetProperty("endDate", out var e))
                        {
                            string start = s.GetString(), end = e.GetString();
                            if (!string.IsNullOrEmpty(start) && !string.IsNullOrEmpty(end))
                                sql.Append($" AND DATE(p.data_nachala) BETWEEN '{start}' AND '{end}'");
                        }
                    }
                    sql.Append(" ORDER BY p.data_nachala DESC");

                    using (var cmd = new NpgsqlCommand(sql.ToString(), conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        var list = new List<object>();
                        decimal uncompletedCost = 0;
                        while (reader.Read())
                        {
                            string status = reader.GetString(6);
                            decimal cost = reader.GetDecimal(9);
                            if (status != "✅ Завершен" && status != "Завершен") uncompletedCost += cost;
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
                        SendToWebView("displayPlans", JsonSerializer.Serialize(result));
                    }
                }
            }
            catch (Exception ex) { ShowError("Ошибка загрузки планов: " + ex.Message); }
        }

        private void LoadAvariya(JsonElement json = default)
        {
            try
            {
                using (var conn = new NpgsqlConnection(connString))
                {
                    conn.Open();
                    var sql = new StringBuilder(@"
                        SELECT a.id, o.nazvanie AS equipment, a.data_avarii AS date,
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
                        if (!showAll && json.TryGetProperty("startDate", out var s) && json.TryGetProperty("endDate", out var e))
                        {
                            string start = s.GetString(), end = e.GetString();
                            if (!string.IsNullOrEmpty(start) && !string.IsNullOrEmpty(end))
                                sql.Append($" WHERE DATE(a.data_avarii) BETWEEN '{start}' AND '{end}'");
                        }
                    }
                    sql.Append(" ORDER BY a.data_avarii DESC");

                    using (var cmd = new NpgsqlCommand(sql.ToString(), conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        var list = new List<object>();
                        while (reader.Read())
                        {
                            list.Add(new
                            {
                                id = reader.GetInt32(0),
                                equipment = reader.GetString(1),
                                date = reader.GetDateTime(2).ToString("dd.MM.yyyy HH:mm"),
                                description = reader.GetString(3),
                                consequences = reader.GetString(4),
                                status = reader.GetString(5),
                                has_plan = reader.GetString(6)
                            });
                        }
                        SendToWebView("displayAvariya", JsonSerializer.Serialize(list));
                    }
                }
            }
            catch (Exception ex) { ShowError("Ошибка загрузки аварий: " + ex.Message); }
        }

        private async Task LoadRepairHistory(JsonElement json = default)
        {
            try
            {
                using (var conn = new NpgsqlConnection(connString))
                {
                    await conn.OpenAsync();
                    var sql = new StringBuilder(@"
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

                    if (json.ValueKind != JsonValueKind.Undefined && json.TryGetProperty("startDate", out var s) && json.TryGetProperty("endDate", out var e))
                    {
                        string start = s.GetString(), end = e.GetString();
                        if (!string.IsNullOrEmpty(start) && !string.IsNullOrEmpty(end))
                            sql.Append($" AND DATE(r.data_okonchaniya) BETWEEN '{start}' AND '{end}'");
                    }
                    sql.Append(" ORDER BY r.data_okonchaniya DESC");

                    using (var cmd = new NpgsqlCommand(sql.ToString(), conn))
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        var list = new List<object>();
                        decimal totalCost = 0;
                        while (await reader.ReadAsync())
                        {
                            decimal cost = reader.GetDecimal(6);
                            totalCost += cost;
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
                        var result = new { history = list, totalCost = totalCost.ToString("N2") };
                        SendToWebView("displayHistory", JsonSerializer.Serialize(result));
                    }
                }
            }
            catch (Exception ex) { ShowError("Ошибка загрузки истории: " + ex.Message); }
        }

        private void LoadStatistics()
        {
            try
            {
                using (var conn = new NpgsqlConnection(connString))
                {
                    conn.Open();
                    string sql = @"
                        SELECT 
                            (SELECT COUNT(*) FROM oborudovanie) as total_equipment,
                            (SELECT COUNT(*) FROM avariya) as total_avariya,
                            (SELECT COUNT(*) FROM plan_to WHERE status != 'Завершен') as total_plans,
                            (SELECT COUNT(*) FROM plan_to WHERE status = 'Завершен') as completed_plans,
                            (SELECT COUNT(*) FROM plan_to WHERE status = 'Просрочен') as overdue_plans,
                            (SELECT COUNT(*) FROM plan_to WHERE status = 'В работе') as in_progress_plans,
                            (SELECT COALESCE(SUM(stoimost), 0) FROM plan_to) as total_cost,
                            (SELECT COALESCE(SUM(stoimost), 0) FROM plan_to WHERE EXTRACT(MONTH FROM data_nachala) = EXTRACT(MONTH FROM CURRENT_DATE)) as monthly_cost";
                    using (var cmd = new NpgsqlCommand(sql, conn))
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
                            SendToWebView("updateStatistics", JsonSerializer.Serialize(stats));
                        }
                    }
                }
            }
            catch (Exception ex) { Console.WriteLine("Ошибка статистики: " + ex.Message); }
        }

        private void AddPlan(JsonElement json)
        {
            try
            {
                int equipment = json.GetProperty("equipment").GetInt32();
                int tip = json.GetProperty("tip").GetInt32();
                string startDate = json.GetProperty("startDate").GetString();
                string endDate = json.GetProperty("endDate").GetString();
                int responsible = json.GetProperty("responsible").GetInt32();
                string status = json.GetProperty("status").GetString();
                decimal cost = json.GetProperty("cost").GetDecimal();

                using (var conn = new NpgsqlConnection(connString))
                {
                    conn.Open();
                    string sql = @"
                        INSERT INTO plan_to (oborudovanie_id, tip_to_id, data_nachala, data_okonchaniya, otvetstvenniy_id, status, stoimost, avariya_id)
                        VALUES (@oborudovanie_id, @tip_to_id, @data_nachala, @data_okonchaniya, @otvetstvenniy_id, @status, @stoimost, @avariya_id)";
                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@oborudovanie_id", equipment);
                        cmd.Parameters.AddWithValue("@tip_to_id", tip);
                        cmd.Parameters.AddWithValue("@data_nachala", DateTime.Parse(startDate));
                        cmd.Parameters.AddWithValue("@data_okonchaniya", DateTime.Parse(endDate));
                        cmd.Parameters.AddWithValue("@otvetstvenniy_id", responsible);
                        cmd.Parameters.AddWithValue("@status", status);
                        cmd.Parameters.AddWithValue("@stoimost", cost);
                        cmd.Parameters.AddWithValue("@avariya_id", selectedAvariyaId != -1 ? (object)selectedAvariyaId : DBNull.Value);
                        cmd.ExecuteNonQuery();
                    }
                }
                ShowSuccess("План успешно добавлен!");
                LoadPlans();
                LoadStatistics();
                selectedAvariyaId = -1;
            }
            catch (Exception ex) { ShowError("Ошибка добавления: " + ex.Message); }
        }

        private void UpdatePlan(JsonElement json)
        {
            try
            {
                int id = json.GetProperty("id").GetInt32();
                int equipment = json.GetProperty("equipment").GetInt32();
                int tip = json.GetProperty("tip").GetInt32();
                string startDate = json.GetProperty("startDate").GetString();
                string endDate = json.GetProperty("endDate").GetString();
                int responsible = json.GetProperty("responsible").GetInt32();
                string status = json.GetProperty("status").GetString();
                decimal cost = json.GetProperty("cost").GetDecimal();

                using (var conn = new NpgsqlConnection(connString))
                {
                    conn.Open();
                    string sql = @"UPDATE plan_to SET oborudovanie_id=@oborudovanie_id, tip_to_id=@tip_to_id, data_nachala=@data_nachala, data_okonchaniya=@data_okonchaniya, otvetstvenniy_id=@otvetstvenniy_id, status=@status, stoimost=@stoimost WHERE id=@id";
                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        cmd.Parameters.AddWithValue("@oborudovanie_id", equipment);
                        cmd.Parameters.AddWithValue("@tip_to_id", tip);
                        cmd.Parameters.AddWithValue("@data_nachala", DateTime.Parse(startDate));
                        cmd.Parameters.AddWithValue("@data_okonchaniya", DateTime.Parse(endDate));
                        cmd.Parameters.AddWithValue("@otvetstvenniy_id", responsible);
                        cmd.Parameters.AddWithValue("@status", status);
                        cmd.Parameters.AddWithValue("@stoimost", cost);
                        cmd.ExecuteNonQuery();
                    }
                }
                ShowSuccess("План успешно обновлен!");
                LoadPlans();
                LoadStatistics();
            }
            catch (Exception ex) { ShowError("Ошибка обновления: " + ex.Message); }
        }

        private void DeletePlan(JsonElement json)
        {
            try
            {
                int id = json.GetProperty("id").GetInt32();
                using (var conn = new NpgsqlConnection(connString))
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand("DELETE FROM plan_to WHERE id=@id", conn))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        cmd.ExecuteNonQuery();
                    }
                }
                ShowSuccess("План успешно удален!");
                LoadPlans();
                LoadStatistics();
            }
            catch (Exception ex) { ShowError("Ошибка удаления: " + ex.Message); }
        }

        private void CreatePlanFromAvariya(JsonElement json)
        {
            int avariyaId = json.GetProperty("id").GetInt32();
            try
            {
                using (var conn = new NpgsqlConnection(connString))
                {
                    conn.Open();
                    string sql = "SELECT oborudovanie_id FROM avariya WHERE id=@id";
                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", avariyaId);
                        var result = cmd.ExecuteScalar();
                        if (result != null)
                        {
                            selectedAvariyaId = avariyaId;
                            string js = $"selectEquipmentById({Convert.ToInt32(result)}); switchToPlansTab();";
                            webView.CoreWebView2.ExecuteScriptAsync(js);
                        }
                    }
                }
            }
            catch (Exception ex) { ShowError("Ошибка создания плана: " + ex.Message); }
        }

        private string GetReportTypeFromSelect()
        {
            if (webView?.CoreWebView2 != null)
            {
                try
                {
                    string script = "document.getElementById('reportTypeSelect')?.value || 'all'";
                    var task = webView.CoreWebView2.ExecuteScriptAsync(script);
                    task.Wait();
                    string value = task.Result?.Trim('"') ?? "all";
                    switch (value)
                    {
                        case "all": return "Все планы";
                        case "inprogress": return "В работе";
                        case "completed": return "Завершенные";
                        case "overdue": return "Просроченные";
                        case "history": return "История ремонтов";
                        case "avariya": return "Аварии";
                        default: return "Все планы";
                    }
                }
                catch { return "Все планы"; }
            }
            return "Все планы";
        }

        private void ExportToExcel()
        {
            try
            {
                string reportType = GetReportTypeFromSelect();
                var save = new SaveFileDialog { Filter = "CSV files (*.csv)|*.csv", RestoreDirectory = true };
                string fileName = "", sql = "";
                string[] headers = null;

                switch (reportType)
                {
                    case "Аварии":
                        fileName = $"Отчет_об_авариях_{DateTime.Now:dd-MM-yyyy}.csv";
                        sql = @"SELECT a.id, o.nazvanie, a.data_avarii, COALESCE(a.opisanie,''), COALESCE(a.posledstviya,''), COALESCE(a.status,''), CASE WHEN p.id IS NOT NULL THEN 'Да' ELSE 'Нет' END FROM avariya a JOIN oborudovanie o ON a.oborudovanie_id=o.id LEFT JOIN plan_to p ON a.id=p.avariya_id ORDER BY a.data_avarii DESC";
                        headers = new[] { "ID", "Оборудование", "Дата аварии", "Описание", "Последствия", "Статус", "Наличие плана" };
                        break;
                    case "История ремонтов":
                        fileName = $"Отчет_об_истории_ремонтов_{DateTime.Now:dd-MM-yyyy}.csv";
                        sql = @"SELECT COALESCE(r.equipment_name,o.nazvanie), COALESCE(r.tip_name,COALESCE(t.nazvanie,'Не указан')), TO_CHAR(p.data_nachala,'DD.MM.YYYY'), TO_CHAR(r.data_okonchaniya,'DD.MM.YYYY'), COALESCE(r.sotrudnik_name,CONCAT(s.familiya,' ',LEFT(s.imya,1),'.',LEFT(s.otchestvo,1),'.')), COALESCE(r.opisanie,''), COALESCE(p.stoimost,0) FROM remont r JOIN plan_to p ON r.plan_id=p.id JOIN oborudovanie o ON p.oborudovanie_id=o.id LEFT JOIN tip_to t ON p.tip_to_id=t.id JOIN sotrudniki s ON r.sotrudnik_id=s.id ORDER BY r.data_okonchaniya DESC";
                        headers = new[] { "Оборудование", "Тип ТО", "Плановая дата", "Дата выполнения", "Исполнитель", "Описание работ", "Стоимость (руб)" };
                        break;
                    case "Завершенные":
                        fileName = $"Отчет_о_завершенных_планах_{DateTime.Now:dd-MM-yyyy}.csv";
                        sql = @"SELECT p.id, o.nazvanie, COALESCE(t.nazvanie,'Не указан'), TO_CHAR(p.data_nachala,'DD.MM.YYYY'), TO_CHAR(p.data_okonchaniya,'DD.MM.YYYY'), COALESCE(s.familiya||' '||s.imya||' '||s.otchestvo,'Не назначен'), p.status, CASE WHEN p.avariya_id IS NOT NULL THEN 'Да' ELSE 'Нет' END, p.stoimost FROM plan_to p JOIN oborudovanie o ON p.oborudovanie_id=o.id LEFT JOIN tip_to t ON p.tip_to_id=t.id LEFT JOIN sotrudniki s ON p.otvetstvenniy_id=s.id WHERE p.status='Завершен' ORDER BY p.data_okonchaniya DESC";
                        headers = new[] { "ID", "Оборудование", "Тип ТО", "Дата начала", "Дата окончания", "Ответственный", "Статус", "Связь с аварией", "Стоимость (руб)" };
                        break;
                    case "В работе":
                        fileName = $"Отчет_о_планах_в_работе_{DateTime.Now:dd-MM-yyyy}.csv";
                        sql = @"SELECT p.id, o.nazvanie, COALESCE(t.nazvanie,'Не указан'), TO_CHAR(p.data_nachala,'DD.MM.YYYY'), TO_CHAR(p.data_okonchaniya,'DD.MM.YYYY'), COALESCE(s.familiya||' '||s.imya||' '||s.otchestvo,'Не назначен'), p.status, CASE WHEN p.avariya_id IS NOT NULL THEN 'Да' ELSE 'Нет' END, p.stoimost FROM plan_to p JOIN oborudovanie o ON p.oborudovanie_id=o.id LEFT JOIN tip_to t ON p.tip_to_id=t.id LEFT JOIN sotrudniki s ON p.otvetstvenniy_id=s.id WHERE p.status='В работе' ORDER BY p.data_nachala ASC";
                        headers = new[] { "ID", "Оборудование", "Тип ТО", "Дата начала", "Дата окончания", "Ответственный", "Статус", "Связь с аварией", "Стоимость (руб)" };
                        break;
                    case "Просроченные":
                        fileName = $"Отчет_о_просроченных_планах_{DateTime.Now:dd-MM-yyyy}.csv";
                        sql = @"SELECT p.id, o.nazvanie, COALESCE(t.nazvanie,'Не указан'), TO_CHAR(p.data_nachala,'DD.MM.YYYY'), TO_CHAR(p.data_okonchaniya,'DD.MM.YYYY'), COALESCE(s.familiya||' '||s.imya||' '||s.otchestvo,'Не назначен'), p.status, CASE WHEN p.avariya_id IS NOT NULL THEN 'Да' ELSE 'Нет' END, p.stoimost FROM plan_to p JOIN oborudovanie o ON p.oborudovanie_id=o.id LEFT JOIN tip_to t ON p.tip_to_id=t.id LEFT JOIN sotrudniki s ON p.otvetstvenniy_id=s.id WHERE p.status='Просрочен' ORDER BY p.data_okonchaniya ASC";
                        headers = new[] { "ID", "Оборудование", "Тип ТО", "Дата начала", "Дата окончания", "Ответственный", "Статус", "Связь с аварией", "Стоимость (руб)" };
                        break;
                    default:
                        fileName = $"Отчет_о_планах_ремонтов_{DateTime.Now:dd-MM-yyyy}.csv";
                        sql = @"SELECT p.id, o.nazvanie, COALESCE(t.nazvanie,'Не указан'), TO_CHAR(p.data_nachala,'DD.MM.YYYY'), TO_CHAR(p.data_okonchaniya,'DD.MM.YYYY'), COALESCE(s.familiya||' '||s.imya||' '||s.otchestvo,'Не назначен'), p.status, CASE WHEN p.avariya_id IS NOT NULL THEN 'Да' ELSE 'Нет' END, p.stoimost FROM plan_to p JOIN oborudovanie o ON p.oborudovanie_id=o.id LEFT JOIN tip_to t ON p.tip_to_id=t.id LEFT JOIN sotrudniki s ON p.otvetstvenniy_id=s.id ORDER BY p.data_nachala DESC";
                        headers = new[] { "ID", "Оборудование", "Тип ТО", "Дата начала", "Дата окончания", "Ответственный", "Статус", "Связь с аварией", "Стоимость (руб)" };
                        break;
                }

                save.FileName = fileName;
                if (save.ShowDialog() == DialogResult.OK)
                {
                    using (var conn = new NpgsqlConnection(connString))
                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        conn.Open();
                        using (var reader = cmd.ExecuteReader())
                        using (var sw = new StreamWriter(save.FileName, false, Encoding.UTF8))
                        {
                            sw.WriteLine($"Отчет: {reportType}");
                            sw.WriteLine($"Дата формирования: {DateTime.Now:dd.MM.yyyy HH:mm}");
                            sw.WriteLine();
                            sw.WriteLine(string.Join(";", headers));
                            int count = 0;
                            while (reader.Read())
                            {
                                for (int i = 0; i < reader.FieldCount; i++)
                                {
                                    if (i > 0) sw.Write(";");
                                    sw.Write(reader[i]?.ToString() ?? "");
                                }
                                sw.WriteLine();
                                count++;
                            }
                            sw.WriteLine();
                            sw.WriteLine($"Всего записей: {count}");
                        }
                    }
                    MessageBox.Show($"Отчет сохранен", "Успешно", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex) { ShowError("Ошибка экспорта: " + ex.Message); }
        }

        private void ExportToWord()
        {
            try
            {
                string reportType = GetReportTypeFromSelect();
                var save = new SaveFileDialog { Filter = "Rich Text Format (*.rtf)|*.rtf", RestoreDirectory = true };
                string fileName = $"Отчет_о_планах_ремонтов_{DateTime.Now:dd-MM-yyyy}.rtf";
                string sql = @"SELECT p.id, o.nazvanie, COALESCE(t.nazvanie,'Не указан'), TO_CHAR(p.data_nachala,'DD.MM.YYYY'), TO_CHAR(p.data_okonchaniya,'DD.MM.YYYY'), COALESCE(s.familiya||' '||s.imya||' '||s.otchestvo,'Не назначен'), p.status, CASE WHEN p.avariya_id IS NOT NULL THEN 'Да' ELSE 'Нет' END, p.stoimost FROM plan_to p JOIN oborudovanie o ON p.oborudovanie_id=o.id LEFT JOIN tip_to t ON p.tip_to_id=t.id LEFT JOIN sotrudniki s ON p.otvetstvenniy_id=s.id ORDER BY p.data_nachala DESC";
                string[] headers = new[] { "ID", "Оборудование", "Тип ТО", "Дата начала", "Дата окончания", "Ответственный", "Статус", "Связь с аварией", "Стоимость (руб)" };

                if (reportType == "Завершенные")
                {
                    fileName = $"Отчет_о_завершенных_планах_{DateTime.Now:dd-MM-yyyy}.rtf";
                    sql = @"SELECT p.id, o.nazvanie, COALESCE(t.nazvanie,'Не указан'), TO_CHAR(p.data_nachala,'DD.MM.YYYY'), TO_CHAR(p.data_okonchaniya,'DD.MM.YYYY'), COALESCE(s.familiya||' '||s.imya||' '||s.otchestvo,'Не назначен'), p.status, CASE WHEN p.avariya_id IS NOT NULL THEN 'Да' ELSE 'Нет' END, p.stoimost FROM plan_to p JOIN oborudovanie o ON p.oborudovanie_id=o.id LEFT JOIN tip_to t ON p.tip_to_id=t.id LEFT JOIN sotrudniki s ON p.otvetstvenniy_id=s.id WHERE p.status='Завершен' ORDER BY p.data_okonchaniya DESC";
                }
                else if (reportType == "В работе")
                {
                    fileName = $"Отчет_о_планах_в_работе_{DateTime.Now:dd-MM-yyyy}.rtf";
                    sql = @"SELECT p.id, o.nazvanie, COALESCE(t.nazvanie,'Не указан'), TO_CHAR(p.data_nachala,'DD.MM.YYYY'), TO_CHAR(p.data_okonchaniya,'DD.MM.YYYY'), COALESCE(s.familiya||' '||s.imya||' '||s.otchestvo,'Не назначен'), p.status, CASE WHEN p.avariya_id IS NOT NULL THEN 'Да' ELSE 'Нет' END, p.stoimost FROM plan_to p JOIN oborudovanie o ON p.oborudovanie_id=o.id LEFT JOIN tip_to t ON p.tip_to_id=t.id LEFT JOIN sotrudniki s ON p.otvetstvenniy_id=s.id WHERE p.status='В работе' ORDER BY p.data_nachala ASC";
                }
                else if (reportType == "Просроченные")
                {
                    fileName = $"Отчет_о_просроченных_планах_{DateTime.Now:dd-MM-yyyy}.rtf";
                    sql = @"SELECT p.id, o.nazvanie, COALESCE(t.nazvanie,'Не указан'), TO_CHAR(p.data_nachala,'DD.MM.YYYY'), TO_CHAR(p.data_okonchaniya,'DD.MM.YYYY'), COALESCE(s.familiya||' '||s.imya||' '||s.otchestvo,'Не назначен'), p.status, CASE WHEN p.avariya_id IS NOT NULL THEN 'Да' ELSE 'Нет' END, p.stoimost FROM plan_to p JOIN oborudovanie o ON p.oborudovanie_id=o.id LEFT JOIN tip_to t ON p.tip_to_id=t.id LEFT JOIN sotrudniki s ON p.otvetstvenniy_id=s.id WHERE p.status='Просрочен' ORDER BY p.data_okonchaniya ASC";
                }
                else if (reportType == "Аварии")
                {
                    fileName = $"Отчет_об_авариях_{DateTime.Now:dd-MM-yyyy}.rtf";
                    sql = @"SELECT a.id, o.nazvanie, a.data_avarii, COALESCE(a.opisanie,''), COALESCE(a.posledstviya,''), COALESCE(a.status,''), CASE WHEN p.id IS NOT NULL THEN 'Да' ELSE 'Нет' END FROM avariya a JOIN oborudovanie o ON a.oborudovanie_id=o.id LEFT JOIN plan_to p ON a.id=p.avariya_id ORDER BY a.data_avarii DESC";
                    headers = new[] { "ID", "Оборудование", "Дата аварии", "Описание", "Последствия", "Статус", "Наличие плана" };
                }
                else if (reportType == "История ремонтов")
                {
                    fileName = $"Отчет_об_истории_ремонтов_{DateTime.Now:dd-MM-yyyy}.rtf";
                    sql = @"SELECT COALESCE(r.equipment_name,o.nazvanie), COALESCE(r.tip_name,COALESCE(t.nazvanie,'Не указан')), TO_CHAR(p.data_nachala,'DD.MM.YYYY'), TO_CHAR(r.data_okonchaniya,'DD.MM.YYYY'), COALESCE(r.sotrudnik_name,CONCAT(s.familiya,' ',LEFT(s.imya,1),'.',LEFT(s.otchestvo,1),'.')), COALESCE(r.opisanie,''), COALESCE(p.stoimost,0) FROM remont r JOIN plan_to p ON r.plan_id=p.id JOIN oborudovanie o ON p.oborudovanie_id=o.id LEFT JOIN tip_to t ON p.tip_to_id=t.id JOIN sotrudniki s ON r.sotrudnik_id=s.id ORDER BY r.data_okonchaniya DESC";
                    headers = new[] { "Оборудование", "Тип ТО", "Плановая дата", "Дата выполнения", "Исполнитель", "Описание работ", "Стоимость (руб)" };
                }

                save.FileName = fileName;
                if (save.ShowDialog() == DialogResult.OK)
                {
                    using (var conn = new NpgsqlConnection(connString))
                    using (var cmd = new NpgsqlCommand(sql, conn))
                    using (var sw = new StreamWriter(save.FileName, false, Encoding.UTF8))
                    {
                        conn.Open();
                        using (var reader = cmd.ExecuteReader())
                        {
                            sw.WriteLine(@"{\rtf1\ansi\deff0{\fonttbl{\f0 Times New Roman;}}\f0\fs24");
                            sw.WriteLine(@"\pard\qc\b\fs32 Отчет: " + reportType + @"\b0\fs24\par");
                            sw.WriteLine(@"\pard\qc\fs20 Дата формирования: " + DateTime.Now.ToString("dd.MM.yyyy HH:mm") + @"\par\par");

                            int colCount = headers.Length;
                            sw.WriteLine(@"\trowd");
                            for (int i = 0; i < colCount; i++) sw.WriteLine(@"\cellx" + ((i + 1) * 2000));
                            sw.WriteLine(@"\clbrdrt\brdrw10\brdrs\clbrdrl\brdrw10\brdrs\clbrdrb\brdrw10\brdrs\clbrdrr\brdrw10\brdrs\clcbpat8\cell\intbl\b\fs20 ");
                            foreach (var h in headers) sw.Write(h + @" \cell ");
                            sw.WriteLine(@"\row\b0");

                            int count = 0;
                            while (reader.Read())
                            {
                                sw.WriteLine(@"\trowd");
                                for (int i = 0; i < colCount; i++) sw.WriteLine(@"\cellx" + ((i + 1) * 2000));
                                sw.WriteLine(@"\clbrdrt\brdrw10\brdrs\clbrdrl\brdrw10\brdrs\clbrdrb\brdrw10\brdrs\clbrdrr\brdrw10\brdrs");
                                if (count % 2 == 0) sw.WriteLine(@"\clcbpat1");
                                sw.WriteLine(@"\cell\intbl\fs20 ");
                                for (int i = 0; i < reader.FieldCount; i++) sw.Write(reader[i]?.ToString() + @" \cell ");
                                sw.WriteLine(@"\row");
                                count++;
                            }
                            sw.WriteLine(@"\pard\qr\fs20 Всего записей: " + count + @"\par}");
                        }
                    }
                    MessageBox.Show($"Отчет сохранен", "Успешно", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex) { ShowError("Ошибка экспорта: " + ex.Message); }
        }

        private void PreviewReport()
        {
            try
            {
                using (var conn = new NpgsqlConnection(connString))
                {
                    conn.Open();
                    string sqlPlans = "SELECT COUNT(*) as total, COUNT(CASE WHEN status='Завершен' THEN 1 END) as completed, COUNT(CASE WHEN status='В работе' THEN 1 END) as in_progress, COUNT(CASE WHEN status='Просрочен' THEN 1 END) as overdue, COALESCE(SUM(stoimost),0) as total_cost FROM plan_to";
                    int totalPlans = 0, completed = 0, inProgress = 0, overdue = 0;
                    decimal totalCost = 0;
                    using (var cmd = new NpgsqlCommand(sqlPlans, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            totalPlans = reader.GetInt32(0);
                            completed = reader.GetInt32(1);
                            inProgress = reader.GetInt32(2);
                            overdue = reader.GetInt32(3);
                            totalCost = reader.GetDecimal(4);
                        }
                    }

                    string sqlAvariya = "SELECT COUNT(*) as total, COUNT(CASE WHEN p.id IS NOT NULL THEN 1 END) as with_plan FROM avariya a LEFT JOIN plan_to p ON a.id=p.avariya_id";
                    int totalAvariya = 0, withPlan = 0;
                    using (var cmd = new NpgsqlCommand(sqlAvariya, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            totalAvariya = reader.GetInt32(0);
                            withPlan = reader.GetInt32(1);
                        }
                    }

                    string sqlHistory = "SELECT COUNT(*) FROM remont";
                    int historyCount = 0;
                    using (var cmd = new NpgsqlCommand(sqlHistory, conn))
                        historyCount = Convert.ToInt32(cmd.ExecuteScalar());

                    int percent = totalPlans > 0 ? completed * 100 / totalPlans : 0;
                    string msg = $"╔══════════════════════════════════════════════════════════╗\n" +
                                 $"║           ПРЕДПРОСМОТР ОТЧЕТА                           ║\n" +
                                 $"╠══════════════════════════════════════════════════════════╣\n" +
                                 $"║ Проект: Котельная                                        ║\n" +
                                 $"║ Дата: {DateTime.Now:dd.MM.yyyy HH:mm}                              ║\n" +
                                 $"╟──────────────────────────────────────────────────────────╢\n" +
                                 $"║ СТАТИСТИКА:                                              ║\n" +
                                 $"║ Всего планов: {totalPlans,-38} ║\n" +
                                 $"║ ├─ Выполнено: {completed,-38} ║\n" +
                                 $"║ ├─ В работе: {inProgress,-39} ║\n" +
                                 $"║ └─ Просрочено: {overdue,-38} ║\n" +
                                 $"║ Процент выполнения: {percent,-3}%                                     ║\n" +
                                 $"║ Общая стоимость: {totalCost:N2} руб.                     ║\n" +
                                 $"╟──────────────────────────────────────────────────────────╢\n" +
                                 $"║ АВАРИИ:                                                  ║\n" +
                                 $"║ Всего аварий: {totalAvariya,-39} ║\n" +
                                 $"║ С планом: {withPlan,-42} ║\n" +
                                 $"║ Без плана: {totalAvariya - withPlan,-41} ║\n" +
                                 $"╟──────────────────────────────────────────────────────────╢\n" +
                                 $"║ ИСТОРИЯ РЕМОНТОВ:                                        ║\n" +
                                 $"║ Выполненных ремонтов: {historyCount,-33} ║\n" +
                                 $"╚══════════════════════════════════════════════════════════╝";
                    MessageBox.Show(msg, "Предпросмотр отчета", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex) { ShowError("Ошибка предпросмотра: " + ex.Message); }
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
                catch { }
            }
        }

        private void ShowSuccess(string msg) => SendToWebView("showSuccess", $"'{msg}'");
        private void ShowError(string msg) => SendToWebView("showError", $"'{msg}'");
    }
}

