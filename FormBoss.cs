using Microsoft.Web.WebView2.WinForms;
using Microsoft.Web.WebView2.Core;
using System;
using System.Collections.Generic;
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
        private WebView2 webView;
        private readonly string connectionString;
        private int currentUserId;
        private int selectedAvariyaId = -1;
        private Timer notificationTimer;
        private HashSet<int> notifiedAvariyaIds = new HashSet<int>();

        public FormBoss(string connString, int userId)
        {
            this.connectionString = connString;
            this.currentUserId = userId;
            InitializeComponent();
            SetupForm();
            InitializeWebView();
            StartNotificationTimer();
        }

        private void SetupForm()
        {
            this.Text = "Панель начальника";
            this.WindowState = FormWindowState.Maximized;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Controls.Clear();
        }

        private void StartNotificationTimer()
        {
            notificationTimer = new Timer();
            notificationTimer.Interval = 15000;
            notificationTimer.Tick += async (s, e) => await CheckNewAvariya();
            notificationTimer.Start();
        }

        private async Task CheckNewAvariya()
        {
            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    await conn.OpenAsync();
                    string sql = @"
                        SELECT a.id, o.nazvanie AS equipment, a.data_avarii, COALESCE(a.opisanie, '') as description
                        FROM avariya a
                        JOIN oborudovanie o ON a.oborudovanie_id = o.id
                        WHERE a.data_avarii > NOW() - INTERVAL '1 minute'
                        ORDER BY a.data_avarii DESC";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            int id = reader.GetInt32(0);
                            if (!notifiedAvariyaIds.Contains(id))
                            {
                                notifiedAvariyaIds.Add(id);
                                string equipment = reader.GetString(1);
                                DateTime date = reader.GetDateTime(2);
                                string description = reader.GetString(3);

                                await ExecuteJsFunction("showNewAvariya", JsonSerializer.Serialize(new
                                {
                                    id = id,
                                    equipment = equipment,
                                    date = date.ToString("dd.MM.yyyy HH:mm"),
                                    description = description
                                }));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка проверки аварий: {ex.Message}");
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
                

                string webUIPath = @"C:\Users\Daniil\Desktop\4\kursovaya3\kursovaya\WebUI";
                string htmlPath = Path.Combine(webUIPath, "boss.html");

                if (File.Exists(htmlPath))
                {
                    webView.CoreWebView2.Navigate($"file:///{htmlPath.Replace('\\', '/')}");
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


        private async void OnWebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            string message = e.TryGetWebMessageAsString();
            System.Diagnostics.Debug.WriteLine($"Получено: {message}");

            try
            {
                using (JsonDocument doc = JsonDocument.Parse(message))
                {
                    JsonElement root = doc.RootElement;
                    string action = root.GetProperty("action").GetString();

                    switch (action)
                    {
                        case "loadEquipment":
                            await LoadEquipment();
                            break;
                        case "loadTipTypes":
                            await LoadTipTypes();
                            break;
                        case "loadResponsible":
                            await LoadResponsible();
                            break;
                        case "loadPlans":
                            await LoadPlans(root);
                            break;
                        case "loadAvariya":
                            await LoadAvariya(root);
                            break;
                        case "loadHistory":
                            await LoadHistory(root);
                            break;
                        case "loadStatistics":
                            await LoadStatistics();
                            break;
                        case "addPlan":
                            await AddPlan(root);
                            break;
                        case "updatePlan":
                            await UpdatePlan(root);
                            break;
                        case "deletePlan":
                            await DeletePlan(root);
                            break;
                        case "createPlanFromAvariya":
                            await CreatePlanFromAvariya(root);
                            break;
                        case "exportToExcel":
                            await ExportToExcel(root);
                            break;
                        case "exportToWord":
                            await ExportToWord(root);
                            break;
                        case "previewReport":
                            await PreviewReport();
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

        private async Task ExecuteJsFunction(string function, string data = null)
        {
            if (webView?.CoreWebView2 != null)
            {
                string js;
                if (string.IsNullOrEmpty(data))
                    js = $"if(window.receiveFromCSharp) window.receiveFromCSharp('{function}', null);";
                else
                {
                    string escapedData = data.Replace("\\", "\\\\").Replace("'", "\\'");
                    js = $"if(window.receiveFromCSharp) window.receiveFromCSharp('{function}', '{escapedData}');";
                }
                await webView.CoreWebView2.ExecuteScriptAsync(js);
            }
        }

        private async Task LoadEquipment()
        {
            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    await conn.OpenAsync();
                    string sql = "SELECT id, nazvanie FROM oborudovanie ORDER BY nazvanie";
                    using (var cmd = new NpgsqlCommand(sql, conn))
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        var list = new List<object>();
                        list.Add(new { id = 0, name = "Все оборудование" });
                        while (await reader.ReadAsync())
                        {
                            list.Add(new { id = reader.GetInt32(0), name = reader.GetString(1) });
                        }
                        string json = JsonSerializer.Serialize(list);
                        await ExecuteJsFunction("fillEquipment", json);
                    }
                }
            }
            catch (Exception ex)
            {
                await ExecuteJsFunction("showError", $"Ошибка загрузки оборудования: {ex.Message}");
            }
        }

        private async Task LoadTipTypes()
        {
            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    await conn.OpenAsync();
                    string sql = "SELECT id, nazvanie FROM tip_to ORDER BY nazvanie";
                    using (var cmd = new NpgsqlCommand(sql, conn))
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        var list = new List<object>();
                        while (await reader.ReadAsync())
                        {
                            list.Add(new { id = reader.GetInt32(0), name = reader.GetString(1) });
                        }
                        string json = JsonSerializer.Serialize(list);
                        await ExecuteJsFunction("fillTipTypes", json);
                    }
                }
            }
            catch (Exception ex)
            {
                await ExecuteJsFunction("showError", $"Ошибка загрузки типов ТО: {ex.Message}");
            }
        }

        private async Task LoadResponsible()
        {
            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    await conn.OpenAsync();
                    string sql = @"
                        SELECT id, familiya || ' ' || imya || ' ' || otchestvo AS fio 
                        FROM sotrudniki 
                        WHERE dolzhnost ILIKE '%слесар%' 
                        ORDER BY familiya";
                    using (var cmd = new NpgsqlCommand(sql, conn))
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        var list = new List<object>();
                        while (await reader.ReadAsync())
                        {
                            list.Add(new { id = reader.GetInt32(0), name = reader.GetString(1) });
                        }
                        string json = JsonSerializer.Serialize(list);
                        await ExecuteJsFunction("fillResponsible", json);
                    }
                }
            }
            catch (Exception ex)
            {
                await ExecuteJsFunction("showError", $"Ошибка загрузки ответственных: {ex.Message}");
            }
        }

        private async Task LoadPlans(JsonElement json)
        {
            try
            {
                string equipmentFilter = "";
                string statusFilter = "";
                string startDate = "";
                string endDate = "";

                if (json.TryGetProperty("equipmentFilter", out var eq))
                    equipmentFilter = eq.GetString() ?? "";
                if (json.TryGetProperty("statusFilter", out var st))
                    statusFilter = st.GetString() ?? "";
                if (json.TryGetProperty("startDate", out var sd))
                    startDate = sd.GetString() ?? "";
                if (json.TryGetProperty("endDate", out var ed))
                    endDate = ed.GetString() ?? "";

                using (var conn = new NpgsqlConnection(connectionString))
                {
                    await conn.OpenAsync();
                    var sql = new StringBuilder(@"
                        SELECT 
                            p.id, 
                            o.nazvanie AS equipment, 
                            COALESCE(t.nazvanie, 'Не указан') AS tip,
                            TO_CHAR(p.data_nachala, 'DD.MM.YYYY') as start_date,
                            TO_CHAR(p.data_okonchaniya, 'DD.MM.YYYY') as end_date,
                            COALESCE(s.familiya || ' ' || s.imya || ' ' || s.otchestvo, 'Не назначен') AS responsible,
                            COALESCE(p.status, 'Не указан') AS status,
                            CASE WHEN p.avariya_id IS NOT NULL THEN 'Да' ELSE 'Нет' END AS has_avariya,
                            p.oborudovanie_id as equipment_id,
                            COALESCE(p.tip_to_id, 0) as tip_id,
                            COALESCE(p.otvetstvenniy_id, 0) as responsible_id
                        FROM plan_to p
                        JOIN oborudovanie o ON p.oborudovanie_id = o.id
                        LEFT JOIN tip_to t ON p.tip_to_id = t.id
                        LEFT JOIN sotrudniki s ON p.otvetstvenniy_id = s.id
                        WHERE 1=1");

                    if (!string.IsNullOrEmpty(equipmentFilter) && equipmentFilter != "0")
                        sql.Append($" AND p.oborudovanie_id = {equipmentFilter}");
                    if (!string.IsNullOrEmpty(statusFilter))
                        sql.Append($" AND p.status = '{statusFilter}'");
                    if (!string.IsNullOrEmpty(startDate))
                        sql.Append($" AND p.data_nachala >= '{startDate}'");
                    if (!string.IsNullOrEmpty(endDate))
                        sql.Append($" AND p.data_nachala <= '{endDate}'");

                    sql.Append(" ORDER BY p.data_nachala DESC");

                    using (var cmd = new NpgsqlCommand(sql.ToString(), conn))
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        var list = new List<object>();
                        while (await reader.ReadAsync())
                        {
                            list.Add(new
                            {
                                id = reader.GetInt32(0),
                                equipment = reader.GetString(1),
                                tip = reader.GetString(2),
                                start_date = reader.GetString(3),
                                end_date = reader.GetString(4),
                                responsible = reader.GetString(5),
                                status = reader.GetString(6),
                                has_avariya = reader.GetString(7),
                                equipment_id = reader.GetInt32(8),
                                tip_id = reader.GetInt32(9),
                                responsible_id = reader.GetInt32(10)
                            });
                        }
                        var result = new { plans = list };
                        string jsonResult = JsonSerializer.Serialize(result);
                        await ExecuteJsFunction("displayPlans", jsonResult);
                    }
                }
            }
            catch (Exception ex)
            {
                await ExecuteJsFunction("showError", $"Ошибка загрузки планов: {ex.Message}");
            }
        }

        private async Task LoadAvariya(JsonElement json)
        {
            try
            {
                string startDate = "";
                string endDate = "";

                if (json.TryGetProperty("startDate", out var sd))
                    startDate = sd.GetString() ?? "";
                if (json.TryGetProperty("endDate", out var ed))
                    endDate = ed.GetString() ?? "";

                using (var conn = new NpgsqlConnection(connectionString))
                {
                    await conn.OpenAsync();
                    var sql = new StringBuilder(@"
                        SELECT a.id, o.nazvanie AS equipment, a.data_avarii AS date,
                               COALESCE(a.opisanie, '') AS description,
                               COALESCE(a.posledstviya, '') AS consequences,
                               COALESCE(a.status, '') AS status,
                               CASE WHEN p.id IS NOT NULL THEN 'Да' ELSE 'Нет' END AS has_plan
                        FROM avariya a
                        JOIN oborudovanie o ON a.oborudovanie_id = o.id
                        LEFT JOIN plan_to p ON a.id = p.avariya_id
                        WHERE 1=1");

                    if (!string.IsNullOrEmpty(startDate))
                        sql.Append($" AND a.data_avarii >= '{startDate}'");
                    if (!string.IsNullOrEmpty(endDate))
                        sql.Append($" AND a.data_avarii <= '{endDate}'");

                    sql.Append(" ORDER BY a.data_avarii DESC");

                    using (var cmd = new NpgsqlCommand(sql.ToString(), conn))
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        var list = new List<object>();
                        while (await reader.ReadAsync())
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
                        string jsonResult = JsonSerializer.Serialize(list);
                        await ExecuteJsFunction("displayAvariya", jsonResult);
                    }
                }
            }
            catch (Exception ex)
            {
                await ExecuteJsFunction("showError", $"Ошибка загрузки аварий: {ex.Message}");
            }
        }

        private async Task LoadHistory(JsonElement json)
        {
            try
            {
                string startDate = "";
                string endDate = "";

                if (json.TryGetProperty("startDate", out var sd))
                    startDate = sd.GetString() ?? "";
                if (json.TryGetProperty("endDate", out var ed))
                    endDate = ed.GetString() ?? "";

                using (var conn = new NpgsqlConnection(connectionString))
                {
                    await conn.OpenAsync();
                    var sql = new StringBuilder(@"
                        SELECT 
                            COALESCE(r.equipment_name, o.nazvanie) as equipment_name,
                            COALESCE(r.tip_name, COALESCE(t.nazvanie, 'Не указан')) as tip_name,
                            TO_CHAR(p.data_nachala, 'DD.MM.YYYY') as plan_date,
                            TO_CHAR(r.data_okonchaniya, 'DD.MM.YYYY') as completed_date,
                            COALESCE(r.sotrudnik_name, CONCAT(s.familiya, ' ', LEFT(s.imya, 1), '.', LEFT(s.otchestvo, 1), '.')) as sotrudnik_name,
                            COALESCE(r.opisanie, '') as opisanie
                        FROM remont r
                        JOIN plan_to p ON r.plan_id = p.id
                        JOIN oborudovanie o ON p.oborudovanie_id = o.id
                        LEFT JOIN tip_to t ON p.tip_to_id = t.id
                        JOIN sotrudniki s ON r.sotrudnik_id = s.id
                        WHERE 1=1");

                    if (!string.IsNullOrEmpty(startDate))
                        sql.Append($" AND r.data_okonchaniya >= '{startDate}'");
                    if (!string.IsNullOrEmpty(endDate))
                        sql.Append($" AND r.data_okonchaniya <= '{endDate}'");

                    sql.Append(" ORDER BY r.data_okonchaniya DESC");

                    using (var cmd = new NpgsqlCommand(sql.ToString(), conn))
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        var list = new List<object>();
                        while (await reader.ReadAsync())
                        {
                            list.Add(new
                            {
                                equipment_name = reader.GetString(0),
                                tip_name = reader.GetString(1),
                                plan_date = reader.GetString(2),
                                completed_date = reader.IsDBNull(3) ? "" : reader.GetString(3),
                                sotrudnik_name = reader.GetString(4),
                                opisanie = reader.GetString(5)
                            });
                        }
                        var result = new { history = list };
                        string jsonResult = JsonSerializer.Serialize(result);
                        await ExecuteJsFunction("displayHistory", jsonResult);
                    }
                }
            }
            catch (Exception ex)
            {
                await ExecuteJsFunction("showError", $"Ошибка загрузки истории: {ex.Message}");
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
                            (SELECT COUNT(*) FROM oborudovanie) as total_equipment,
                            (SELECT COUNT(*) FROM avariya) as total_avariya,
                            (SELECT COUNT(*) FROM plan_to WHERE status NOT IN ('Завершен', 'Отменен')) as total_plans,
                            (SELECT COUNT(*) FROM plan_to WHERE status = 'Завершен') as completed_plans,
                            (SELECT COUNT(*) FROM plan_to WHERE status = 'Просрочен') as overdue_plans,
                            (SELECT COUNT(*) FROM plan_to WHERE status = 'В работе') as in_progress_plans";
                    using (var cmd = new NpgsqlCommand(sql, conn))
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            var stats = new
                            {
                                totalEquipment = reader.GetInt32(0),
                                totalAvariya = reader.GetInt32(1),
                                totalPlans = reader.GetInt32(2),
                                completedPlans = reader.GetInt32(3),
                                overduePlans = reader.GetInt32(4),
                                inProgressPlans = reader.GetInt32(5)
                            };
                            string json = JsonSerializer.Serialize(stats);
                            await ExecuteJsFunction("updateStatistics", json);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка статистики: {ex.Message}");
            }
        }

        private async Task AddPlan(JsonElement json)
        {
            try
            {
                int equipment = json.GetProperty("equipment").GetInt32();
                int tip = json.GetProperty("tip").GetInt32();
                string startDate = json.GetProperty("startDate").GetString();
                string endDate = json.GetProperty("endDate").GetString();
                int responsible = json.GetProperty("responsible").GetInt32();

                using (var conn = new NpgsqlConnection(connectionString))
                {
                    await conn.OpenAsync();
                    string sql = @"
                        INSERT INTO plan_to (oborudovanie_id, tip_to_id, data_nachala, data_okonchaniya, otvetstvenniy_id, status, avariya_id)
                        VALUES (@oborudovanie_id, @tip_to_id, @data_nachala, @data_okonchaniya, @otvetstvenniy_id, 'В работе', @avariya_id)";
                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@oborudovanie_id", equipment);
                        cmd.Parameters.AddWithValue("@tip_to_id", tip);
                        cmd.Parameters.AddWithValue("@data_nachala", DateTime.Parse(startDate));
                        cmd.Parameters.AddWithValue("@data_okonchaniya", DateTime.Parse(endDate));
                        cmd.Parameters.AddWithValue("@otvetstvenniy_id", responsible);
                        cmd.Parameters.AddWithValue("@avariya_id", selectedAvariyaId != -1 ? (object)selectedAvariyaId : DBNull.Value);
                        await cmd.ExecuteNonQueryAsync();
                    }
                }
                await ExecuteJsFunction("showSuccess", "План успешно добавлен!");
                await LoadPlans(JsonDocument.Parse("{}").RootElement);
                await LoadStatistics();
                selectedAvariyaId = -1;
            }
            catch (Exception ex)
            {
                await ExecuteJsFunction("showError", $"Ошибка добавления: {ex.Message}");
            }
        }

        private async Task UpdatePlan(JsonElement json)
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

                using (var conn = new NpgsqlConnection(connectionString))
                {
                    await conn.OpenAsync();
                    string sql = @"UPDATE plan_to SET oborudovanie_id=@oborudovanie_id, tip_to_id=@tip_to_id, data_nachala=@data_nachala, data_okonchaniya=@data_okonchaniya, otvetstvenniy_id=@otvetstvenniy_id, status=@status WHERE id=@id";
                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        cmd.Parameters.AddWithValue("@oborudovanie_id", equipment);
                        cmd.Parameters.AddWithValue("@tip_to_id", tip);
                        cmd.Parameters.AddWithValue("@data_nachala", DateTime.Parse(startDate));
                        cmd.Parameters.AddWithValue("@data_okonchaniya", DateTime.Parse(endDate));
                        cmd.Parameters.AddWithValue("@otvetstvenniy_id", responsible);
                        cmd.Parameters.AddWithValue("@status", status);
                        await cmd.ExecuteNonQueryAsync();
                    }
                }
                await ExecuteJsFunction("showSuccess", "План успешно обновлен!");
                await LoadPlans(JsonDocument.Parse("{}").RootElement);
                await LoadStatistics();
            }
            catch (Exception ex)
            {
                await ExecuteJsFunction("showError", $"Ошибка обновления: {ex.Message}");
            }
        }

        private async Task DeletePlan(JsonElement json)
        {
            try
            {
                int id = json.GetProperty("id").GetInt32();
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    await conn.OpenAsync();
                    using (var cmd = new NpgsqlCommand("DELETE FROM plan_to WHERE id=@id", conn))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        await cmd.ExecuteNonQueryAsync();
                    }
                }
                await ExecuteJsFunction("showSuccess", "План успешно удален!");
                await LoadPlans(JsonDocument.Parse("{}").RootElement);
                await LoadStatistics();
            }
            catch (Exception ex)
            {
                await ExecuteJsFunction("showError", $"Ошибка удаления: {ex.Message}");
            }
        }

        private async Task CreatePlanFromAvariya(JsonElement json)
        {
            int avariyaId = json.GetProperty("id").GetInt32();
            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    await conn.OpenAsync();
                    string sql = "SELECT oborudovanie_id FROM avariya WHERE id=@id";
                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", avariyaId);
                        var result = await cmd.ExecuteScalarAsync();
                        if (result != null)
                        {
                            selectedAvariyaId = avariyaId;
                            await webView.CoreWebView2.ExecuteScriptAsync("switchToPlansTab();");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                await ExecuteJsFunction("showError", $"Ошибка создания плана: {ex.Message}");
            }
        }

        private async Task ExportToExcel(JsonElement json)
        {
            try
            {
                string reportType = json.GetProperty("reportType").GetString();
                string sql = "";
                string fileName = $"Отчет_{DateTime.Now:yyyy-MM-dd_HH-mm}.csv";
                string[] headers = null;

                switch (reportType)
                {
                    case "avariya":
                        fileName = $"Отчет_об_авариях_{DateTime.Now:yyyy-MM-dd_HH-mm}.csv";
                        sql = @"SELECT a.id, o.nazvanie, a.data_avarii, COALESCE(a.opisanie,''), COALESCE(a.posledstviya,''), COALESCE(a.status,''), CASE WHEN p.id IS NOT NULL THEN 'Да' ELSE 'Нет' END FROM avariya a JOIN oborudovanie o ON a.oborudovanie_id=o.id LEFT JOIN plan_to p ON a.id=p.avariya_id ORDER BY a.data_avarii DESC";
                        headers = new[] { "ID", "Оборудование", "Дата аварии", "Описание", "Последствия", "Статус", "Наличие плана" };
                        break;
                    case "history":
                        fileName = $"Отчет_об_истории_ремонтов_{DateTime.Now:yyyy-MM-dd_HH-mm}.csv";
                        sql = @"SELECT COALESCE(r.equipment_name,o.nazvanie), COALESCE(r.tip_name,COALESCE(t.nazvanie,'Не указан')), TO_CHAR(p.data_nachala,'DD.MM.YYYY'), TO_CHAR(r.data_okonchaniya,'DD.MM.YYYY'), COALESCE(r.sotrudnik_name,CONCAT(s.familiya,' ',LEFT(s.imya,1),'.',LEFT(s.otchestvo,1),'.')), COALESCE(r.opisanie,'') FROM remont r JOIN plan_to p ON r.plan_id=p.id JOIN oborudovanie o ON p.oborudovanie_id=o.id LEFT JOIN tip_to t ON p.tip_to_id=t.id JOIN sotrudniki s ON r.sotrudnik_id=s.id ORDER BY r.data_okonchaniya DESC";
                        headers = new[] { "Оборудование", "Тип ТО", "Плановая дата", "Дата выполнения", "Исполнитель", "Описание работ" };
                        break;
                    case "completed":
                        fileName = $"Отчет_о_завершенных_планах_{DateTime.Now:yyyy-MM-dd_HH-mm}.csv";
                        sql = @"SELECT p.id, o.nazvanie, COALESCE(t.nazvanie,'Не указан'), TO_CHAR(p.data_nachala,'DD.MM.YYYY'), TO_CHAR(p.data_okonchaniya,'DD.MM.YYYY'), COALESCE(s.familiya||' '||s.imya||' '||s.otchestvo,'Не назначен'), p.status, CASE WHEN p.avariya_id IS NOT NULL THEN 'Да' ELSE 'Нет' END FROM plan_to p JOIN oborudovanie o ON p.oborudovanie_id=o.id LEFT JOIN tip_to t ON p.tip_to_id=t.id LEFT JOIN sotrudniki s ON p.otvetstvenniy_id=s.id WHERE p.status='Завершен' ORDER BY p.data_okonchaniya DESC";
                        headers = new[] { "ID", "Оборудование", "Тип ТО", "Дата начала", "Дата окончания", "Ответственный", "Статус", "Связь с аварией" };
                        break;
                    case "inprogress":
                        fileName = $"Отчет_о_планах_в_работе_{DateTime.Now:yyyy-MM-dd_HH-mm}.csv";
                        sql = @"SELECT p.id, o.nazvanie, COALESCE(t.nazvanie,'Не указан'), TO_CHAR(p.data_nachala,'DD.MM.YYYY'), TO_CHAR(p.data_okonchaniya,'DD.MM.YYYY'), COALESCE(s.familiya||' '||s.imya||' '||s.otchestvo,'Не назначен'), p.status, CASE WHEN p.avariya_id IS NOT NULL THEN 'Да' ELSE 'Нет' END FROM plan_to p JOIN oborudovanie o ON p.oborudovanie_id=o.id LEFT JOIN tip_to t ON p.tip_to_id=t.id LEFT JOIN sotrudniki s ON p.otvetstvenniy_id=s.id WHERE p.status='В работе' ORDER BY p.data_nachala ASC";
                        headers = new[] { "ID", "Оборудование", "Тип ТО", "Дата начала", "Дата окончания", "Ответственный", "Статус", "Связь с аварией" };
                        break;
                    case "overdue":
                        fileName = $"Отчет_о_просроченных_планах_{DateTime.Now:yyyy-MM-dd_HH-mm}.csv";
                        sql = @"SELECT p.id, o.nazvanie, COALESCE(t.nazvanie,'Не указан'), TO_CHAR(p.data_nachala,'DD.MM.YYYY'), TO_CHAR(p.data_okonchaniya,'DD.MM.YYYY'), COALESCE(s.familiya||' '||s.imya||' '||s.otchestvo,'Не назначен'), p.status, CASE WHEN p.avariya_id IS NOT NULL THEN 'Да' ELSE 'Нет' END FROM plan_to p JOIN oborudovanie o ON p.oborudovanie_id=o.id LEFT JOIN tip_to t ON p.tip_to_id=t.id LEFT JOIN sotrudniki s ON p.otvetstvenniy_id=s.id WHERE p.status='Просрочен' ORDER BY p.data_okonchaniya ASC";
                        headers = new[] { "ID", "Оборудование", "Тип ТО", "Дата начала", "Дата окончания", "Ответственный", "Статус", "Связь с аварией" };
                        break;
                    default:
                        fileName = $"Отчет_о_планах_ремонтов_{DateTime.Now:yyyy-MM-dd_HH-mm}.csv";
                        sql = @"SELECT p.id, o.nazvanie, COALESCE(t.nazvanie,'Не указан'), TO_CHAR(p.data_nachala,'DD.MM.YYYY'), TO_CHAR(p.data_okonchaniya,'DD.MM.YYYY'), COALESCE(s.familiya||' '||s.imya||' '||s.otchestvo,'Не назначен'), p.status, CASE WHEN p.avariya_id IS NOT NULL THEN 'Да' ELSE 'Нет' END FROM plan_to p JOIN oborudovanie o ON p.oborudovanie_id=o.id LEFT JOIN tip_to t ON p.tip_to_id=t.id LEFT JOIN sotrudniki s ON p.otvetstvenniy_id=s.id ORDER BY p.data_nachala DESC";
                        headers = new[] { "ID", "Оборудование", "Тип ТО", "Дата начала", "Дата окончания", "Ответственный", "Статус", "Связь с аварией" };
                        break;
                }

                SaveFileDialog save = new SaveFileDialog();
                save.Filter = "CSV файлы (*.csv)|*.csv";
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
                    await ExecuteJsFunction("showSuccess", "Отчет успешно сохранен!");
                }
            }
            catch (Exception ex)
            {
                await ExecuteJsFunction("showError", $"Ошибка экспорта: {ex.Message}");
            }
        }

        private async Task ExportToWord(JsonElement json)
        {
            try
            {
                string reportType = json.GetProperty("reportType").GetString();
                string fileName = $"Отчет_{DateTime.Now:yyyy-MM-dd_HH-mm}.rtf";
                string title = "";
                string sql = "";
                string[] headers = null;

                switch (reportType)
                {
                    case "avariya":
                        title = "Отчет об авариях";
                        fileName = $"Отчет_об_авариях_{DateTime.Now:yyyy-MM-dd_HH-mm}.rtf";
                        sql = @"
                    SELECT a.id, o.nazvanie, a.data_avarii, COALESCE(a.opisanie,''), COALESCE(a.posledstviya,''), COALESCE(a.status,''), 
                           CASE WHEN p.id IS NOT NULL THEN 'Да' ELSE 'Нет' END 
                    FROM avariya a 
                    JOIN oborudovanie o ON a.oborudovanie_id = o.id 
                    LEFT JOIN plan_to p ON a.id = p.avariya_id 
                    ORDER BY a.data_avarii DESC";
                        headers = new[] { "ID", "Оборудование", "Дата аварии", "Описание", "Последствия", "Статус", "Наличие плана" };
                        break;
                    case "history":
                        title = "Отчет об истории ремонтов";
                        fileName = $"Отчет_об_истории_ремонтов_{DateTime.Now:yyyy-MM-dd_HH-mm}.rtf";
                        sql = @"
                    SELECT COALESCE(r.equipment_name, o.nazvanie) as equipment_name,
                           COALESCE(r.tip_name, COALESCE(t.nazvanie, 'Не указан')) as tip_name,
                           TO_CHAR(p.data_nachala, 'DD.MM.YYYY') as plan_date,
                           TO_CHAR(r.data_okonchaniya, 'DD.MM.YYYY') as completed_date,
                           COALESCE(r.sotrudnik_name, CONCAT(s.familiya, ' ', LEFT(s.imya, 1), '.', LEFT(s.otchestvo, 1), '.')) as sotrudnik_name,
                           COALESCE(r.opisanie, '') as opisanie,
                           COALESCE(r.zamennaya_detal, '') as spare_part
                    FROM remont r
                    JOIN plan_to p ON r.plan_id = p.id
                    JOIN oborudovanie o ON p.oborudovanie_id = o.id
                    LEFT JOIN tip_to t ON p.tip_to_id = t.id
                    JOIN sotrudniki s ON r.sotrudnik_id = s.id
                    ORDER BY r.data_okonchaniya DESC";
                        headers = new[] { "Оборудование", "Тип ТО", "Плановая дата", "Дата выполнения", "Исполнитель", "Описание работ", "Замененная деталь" };
                        break;
                    case "completed":
                        title = "Отчет о завершенных планах";
                        fileName = $"Отчет_о_завершенных_планах_{DateTime.Now:yyyy-MM-dd_HH-mm}.rtf";
                        sql = @"
                    SELECT p.id, o.nazvanie, COALESCE(t.nazvanie,'Не указан'), 
                           TO_CHAR(p.data_nachala,'DD.MM.YYYY'), TO_CHAR(p.data_okonchaniya,'DD.MM.YYYY'), 
                           COALESCE(s.familiya||' '||s.imya||' '||s.otchestvo,'Не назначен'), p.status,
                           CASE WHEN p.avariya_id IS NOT NULL THEN 'Да' ELSE 'Нет' END
                    FROM plan_to p
                    JOIN oborudovanie o ON p.oborudovanie_id = o.id
                    LEFT JOIN tip_to t ON p.tip_to_id = t.id
                    LEFT JOIN sotrudniki s ON p.otvetstvenniy_id = s.id
                    WHERE p.status = 'Завершен'
                    ORDER BY p.data_okonchaniya DESC";
                        headers = new[] { "ID", "Оборудование", "Тип ТО", "Дата начала", "Дата окончания", "Ответственный", "Статус", "Связь с аварией" };
                        break;
                    case "inprogress":
                        title = "Отчет о планах в работе";
                        fileName = $"Отчет_о_планах_в_работе_{DateTime.Now:yyyy-MM-dd_HH-mm}.rtf";
                        sql = @"
                    SELECT p.id, o.nazvanie, COALESCE(t.nazvanie,'Не указан'), 
                           TO_CHAR(p.data_nachala,'DD.MM.YYYY'), TO_CHAR(p.data_okonchaniya,'DD.MM.YYYY'), 
                           COALESCE(s.familiya||' '||s.imya||' '||s.otchestvo,'Не назначен'), p.status,
                           CASE WHEN p.avariya_id IS NOT NULL THEN 'Да' ELSE 'Нет' END
                    FROM plan_to p
                    JOIN oborudovanie o ON p.oborudovanie_id = o.id
                    LEFT JOIN tip_to t ON p.tip_to_id = t.id
                    LEFT JOIN sotrudniki s ON p.otvetstvenniy_id = s.id
                    WHERE p.status = 'В работе'
                    ORDER BY p.data_nachala ASC";
                        headers = new[] { "ID", "Оборудование", "Тип ТО", "Дата начала", "Дата окончания", "Ответственный", "Статус", "Связь с аварией" };
                        break;
                    case "overdue":
                        title = "Отчет о просроченных планах";
                        fileName = $"Отчет_о_просроченных_планах_{DateTime.Now:yyyy-MM-dd_HH-mm}.rtf";
                        sql = @"
                    SELECT p.id, o.nazvanie, COALESCE(t.nazvanie,'Не указан'), 
                           TO_CHAR(p.data_nachala,'DD.MM.YYYY'), TO_CHAR(p.data_okonchaniya,'DD.MM.YYYY'), 
                           COALESCE(s.familiya||' '||s.imya||' '||s.otchestvo,'Не назначен'), p.status,
                           CASE WHEN p.avariya_id IS NOT NULL THEN 'Да' ELSE 'Нет' END
                    FROM plan_to p
                    JOIN oborudovanie o ON p.oborudovanie_id = o.id
                    LEFT JOIN tip_to t ON p.tip_to_id = t.id
                    LEFT JOIN sotrudniki s ON p.otvetstvenniy_id = s.id
                    WHERE p.status = 'Просрочен'
                    ORDER BY p.data_okonchaniya ASC";
                        headers = new[] { "ID", "Оборудование", "Тип ТО", "Дата начала", "Дата окончания", "Ответственный", "Статус", "Связь с аварией" };
                        break;
                    default:
                        title = "Отчет о планах ремонтов";
                        fileName = $"Отчет_о_планах_ремонтов_{DateTime.Now:yyyy-MM-dd_HH-mm}.rtf";
                        sql = @"
                    SELECT p.id, o.nazvanie, COALESCE(t.nazvanie,'Не указан'), 
                           TO_CHAR(p.data_nachala,'DD.MM.YYYY'), TO_CHAR(p.data_okonchaniya,'DD.MM.YYYY'), 
                           COALESCE(s.familiya||' '||s.imya||' '||s.otchestvo,'Не назначен'), p.status,
                           CASE WHEN p.avariya_id IS NOT NULL THEN 'Да' ELSE 'Нет' END
                    FROM plan_to p
                    JOIN oborudovanie o ON p.oborudovanie_id = o.id
                    LEFT JOIN tip_to t ON p.tip_to_id = t.id
                    LEFT JOIN sotrudniki s ON p.otvetstvenniy_id = s.id
                    ORDER BY p.data_nachala DESC";
                        headers = new[] { "ID", "Оборудование", "Тип ТО", "Дата начала", "Дата окончания", "Ответственный", "Статус", "Связь с аварией" };
                        break;
                }

                SaveFileDialog save = new SaveFileDialog();
                save.Filter = "RTF файлы (*.rtf)|*.rtf";
                save.FileName = fileName;
                save.Title = title;

                if (save.ShowDialog() == DialogResult.OK)
                {
                    using (var conn = new NpgsqlConnection(connectionString))
                    using (var cmd = new NpgsqlCommand(sql, conn))
                    using (var sw = new StreamWriter(save.FileName, false, Encoding.UTF8))
                    {
                        await conn.OpenAsync();
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            // Начало RTF документа
                            sw.WriteLine(@"{\rtf1\ansi\deff0");
                            sw.WriteLine(@"{\fonttbl {\f0 Times New Roman;}{\f1 Arial;}}");
                            sw.WriteLine(@"\f0\fs24");

                            // Заголовок
                            sw.WriteLine($@"\pard\qc\b\fs36 {title}\b0\par");
                            sw.WriteLine($@"\pard\qc\fs20 Дата формирования: {DateTime.Now:dd.MM.yyyy HH:mm}\par\par");

                            // Статистика
                            sw.WriteLine($@"\pard\fs22\b Общая информация:\b0\par");
                            sw.WriteLine($@"\pard\fs20 Всего записей: {await GetTotalCount(conn, sql)}\par");
                            sw.WriteLine($@"\pard\qc\b\fs20\par\b0\par");

                            // Таблица
                            int colCount = headers.Length;

                            // Настройка ширины колонок
                            int baseWidth = 15000 / colCount;
                            sw.WriteLine(@"\trowd");
                            for (int i = 0; i < colCount; i++)
                                sw.WriteLine($@"\cellx{(i + 1) * baseWidth}");

                            // Строка заголовков
                            sw.WriteLine(@"\clbrdrt\brdrw10\brdrs\clbrdrl\brdrw10\brdrs\clbrdrb\brdrw10\brdrs\clbrdrr\brdrw10\brdrs\clcbpat8\cell\intbl\b ");
                            foreach (var h in headers)
                                sw.Write($@"\pard\intbl\qc {h}\cell ");
                            sw.WriteLine(@"\row\b0");

                            // Данные
                            int rowNum = 0;
                            while (await reader.ReadAsync())
                            {
                                sw.WriteLine(@"\trowd");
                                for (int i = 0; i < colCount; i++)
                                    sw.WriteLine($@"\cellx{(i + 1) * baseWidth}");

                                int bgColor = (rowNum % 2 == 0) ? 7 : 16;
                                sw.WriteLine($@"\clbrdrt\brdrw10\brdrs\clbrdrl\brdrw10\brdrs\clbrdrb\brdrw10\brdrs\clbrdrr\brdrw10\brdrs\clcbpat{bgColor}\cell\intbl\fs18 ");

                                for (int i = 0; i < colCount; i++)
                                {
                                    string val = reader[i]?.ToString() ?? "";
                                    val = val.Replace("\\", "\\\\").Replace("{", "\\{").Replace("}", "\\}");
                                    sw.Write($@"\pard\intbl {val}\cell ");
                                }
                                sw.WriteLine(@"\row");
                                rowNum++;
                            }
                        }

                        // Конец RTF документа
                        sw.WriteLine(@"\pard\par");
                        sw.WriteLine(@"}");
                    }
                    await ExecuteJsFunction("showSuccess", "Отчет успешно сохранен!");
                }
            }
            catch (Exception ex)
            {
                await ExecuteJsFunction("showError", $"Ошибка экспорта: {ex.Message}");
            }
        }

        private async Task<int> GetTotalCount(NpgsqlConnection conn, string sql)
        {
            try
            {
                string countSql = $"SELECT COUNT(*) FROM ({sql}) as subquery";
                using (var cmd = new NpgsqlCommand(countSql, conn))
                {
                    var result = await cmd.ExecuteScalarAsync();
                    return result != null ? Convert.ToInt32(result) : 0;
                }
            }
            catch
            {
                return 0;
            }
        }

        private string GetReportTitle(string reportType)
        {
            switch (reportType)
            {
                case "avariya": return "Аварии";
                case "history": return "История ремонтов";
                case "completed": return "Завершенные планы";
                case "inprogress": return "Планы в работе";
                case "overdue": return "Просроченные планы";
                default: return "Все планы";
            }
        }

        private async Task PreviewReport()
        {
            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    await conn.OpenAsync();
                    string sql = @"
                        SELECT 
                            COUNT(*) as total,
                            COUNT(CASE WHEN status = 'Завершен' THEN 1 END) as completed,
                            COUNT(CASE WHEN status = 'В работе' THEN 1 END) as in_progress,
                            COUNT(CASE WHEN status = 'Просрочен' THEN 1 END) as overdue
                        FROM plan_to";

                    int total = 0, completed = 0, inProgress = 0, overdue = 0;
                    using (var cmd = new NpgsqlCommand(sql, conn))
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            total = reader.GetInt32(0);
                            completed = reader.GetInt32(1);
                            inProgress = reader.GetInt32(2);
                            overdue = reader.GetInt32(3);
                        }
                    }

                    string message = $"═══════════════════════════════════════\n" +
                                     $"          ПРЕДПРОСМОТР ОТЧЕТА\n" +
                                     $"═══════════════════════════════════════\n" +
                                     $"Дата: {DateTime.Now:dd.MM.yyyy HH:mm}\n" +
                                     $"\n" +
                                     $"Всего планов: {total}\n" +
                                     $"├─ Выполнено: {completed}\n" +
                                     $"├─ В работе: {inProgress}\n" +
                                     $"└─ Просрочено: {overdue}\n" +
                                     $"\n" +
                                     $"Процент выполнения: {(total > 0 ? completed * 100 / total : 0)}%\n" +
                                     $"═══════════════════════════════════════";

                    MessageBox.Show(message, "Предпросмотр отчета", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                await ExecuteJsFunction("showError", $"Ошибка предпросмотра: {ex.Message}");
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

