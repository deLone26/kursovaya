using Microsoft.Web.WebView2.WinForms;
using Microsoft.Web.WebView2.Core;
using System;
using System.Data;
using System.Drawing;
using System.IO;
using System.Text;
using System.Text.Json;
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
                    }
                }
            }
            catch (Exception ex)
            {
                ShowError("Ошибка обработки: " + ex.Message);
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
                            var list = new System.Collections.Generic.List<object>();

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
                            var list = new System.Collections.Generic.List<object>();

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
                        WHERE dolzhnost ILIKE '%слесар%' OR dolzhnost ILIKE '%Слесар%' OR dolzhnost ILIKE '%механик%'
                        ORDER BY familiya";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            var list = new System.Collections.Generic.List<object>();

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
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();

                    StringBuilder sql = new StringBuilder(@"
                        SELECT 
                            p.id,
                            o.nazvanie AS equipment,
                            COALESCE(t.nazvanie, 'Не указан') AS tip,
                            p.data_nachala AS start_date,
                            p.data_okonchaniya AS end_date,
                            COALESCE(s.familiya || ' ' || s.imya || ' ' || s.otchestvo, 'Не назначен') AS responsible,
                            COALESCE(p.status, 'Не указан') AS status,
                            CASE WHEN p.avariya_id IS NOT NULL THEN '✅' ELSE '❌' END AS has_avariya
                        FROM plan_to p
                        JOIN oborudovanie o ON p.oborudovanie_id = o.id
                        LEFT JOIN tip_to t ON p.tip_to_id = t.id
                        LEFT JOIN sotrudniki s ON p.otvetstvenniy_id = s.id");

                    if (json.ValueKind != JsonValueKind.Undefined)
                    {
                        bool showAll = json.GetProperty("showAll").GetBoolean();
                        if (!showAll)
                        {
                            string startDate = json.GetProperty("startDate").GetString();
                            string endDate = json.GetProperty("endDate").GetString();
                            sql.Append($" WHERE DATE(p.data_nachala) BETWEEN '{startDate}' AND '{endDate}'");
                        }
                    }

                    sql.Append(" ORDER BY p.data_nachala DESC");

                    using (var cmd = new NpgsqlCommand(sql.ToString(), conn))
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            var list = new System.Collections.Generic.List<object>();

                            while (reader.Read())
                            {
                                var plan = new
                                {
                                    id = reader.GetInt32(0),
                                    equipment = reader.GetString(1),
                                    tip = reader.GetString(2),
                                    start_date = reader.GetDateTime(3).ToString("yyyy-MM-dd"),
                                    end_date = reader.IsDBNull(4) ? "" : reader.GetDateTime(4).ToString("yyyy-MM-dd"),
                                    responsible = reader.GetString(5),
                                    status = reader.GetString(6),
                                    has_avariya = reader.GetString(7)
                                };
                                list.Add(plan);
                            }

                            string jsonData = JsonSerializer.Serialize(list);
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
                            var list = new System.Collections.Generic.List<object>();

                            while (reader.Read())
                            {
                                var avariya = new
                                {
                                    id = reader.GetInt32(0),
                                    equipment = reader.GetString(1),
                                    date = reader.GetDateTime(2).ToString("yyyy-MM-dd HH:mm"),
                                    description = reader.GetString(3),
                                    consequences = reader.GetString(4),
                                    status = reader.GetString(5),
                                    has_plan = reader.GetString(6)
                                };
                                list.Add(avariya);
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

        private void LoadStatistics()
        {
            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();
                    string sql = @"
                        SELECT 
                            (SELECT COUNT(*) FROM oborudovanie) as total_equipment,
                            (SELECT COUNT(*) FROM avariya) as total_avariya,
                            (SELECT COUNT(*) FROM plan_to) as total_plans,
                            (SELECT COUNT(*) FROM plan_to WHERE status = 'Завершен') as completed_plans
                    ";

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
                                    completedPlans = reader.GetInt32(3)
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

                using (var conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();
                    string sql = @"
                        INSERT INTO plan_to 
                        (oborudovanie_id, tip_to_id, data_nachala, data_okonchaniya, otvetstvenniy_id, status)
                        VALUES 
                        (@oborudovanie_id, @tip_to_id, @data_nachala, @data_okonchaniya, @otvetstvenniy_id, @status)";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@oborudovanie_id", equipmentId);
                        cmd.Parameters.AddWithValue("@tip_to_id", tipId);
                        cmd.Parameters.AddWithValue("@data_nachala", DateTime.Parse(startDate));
                        cmd.Parameters.AddWithValue("@data_okonchaniya", DateTime.Parse(endDate));
                        cmd.Parameters.AddWithValue("@otvetstvenniy_id", responsibleId);
                        cmd.Parameters.AddWithValue("@status", status);

                        cmd.ExecuteNonQuery();
                    }
                }

                SendToWebView("showSuccess", "План успешно добавлен!");
                LoadPlans();
                LoadStatistics();
            }
            catch (Exception ex)
            {
                ShowError("Ошибка добавления: " + ex.Message);
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
                            status = @status
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

                        cmd.ExecuteNonQuery();
                    }
                }

                SendToWebView("showSuccess", "План успешно обновлен!");
                LoadPlans();
                LoadStatistics();
            }
            catch (Exception ex)
            {
                ShowError("Ошибка обновления: " + ex.Message);
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

        private void ExportToExcel()
        {
            try
            {
                SaveFileDialog save = new SaveFileDialog();
                save.Filter = "CSV files (*.csv)|*.csv";
                save.FileName = $"Отчет_о_планах_ремонтов_{DateTime.Now:dd-MM-yyyy}.csv";

                if (save.ShowDialog() == DialogResult.OK)
                {
                    using (var conn = new NpgsqlConnection(connectionString))
                    {
                        conn.Open();

                        string sql = @"
                            SELECT 
                                p.id,
                                o.nazvanie AS equipment,
                                COALESCE(t.nazvanie, 'Не указан') AS tip,
                                p.data_nachala AS start_date,
                                p.data_okonchaniya AS end_date,
                                COALESCE(s.familiya || ' ' || s.imya || ' ' || s.otchestvo, 'Не назначен') AS responsible,
                                p.status,
                                CASE WHEN p.avariya_id IS NOT NULL THEN 'Да' ELSE 'Нет' END AS has_avariya
                            FROM plan_to p
                            JOIN oborudovanie o ON p.oborudovanie_id = o.id
                            LEFT JOIN tip_to t ON p.tip_to_id = t.id
                            LEFT JOIN sotrudniki s ON p.otvetstvenniy_id = s.id
                            ORDER BY p.data_nachala DESC";

                        using (var cmd = new NpgsqlCommand(sql, conn))
                        {
                            using (var reader = cmd.ExecuteReader())
                            {
                                using (StreamWriter sw = new StreamWriter(save.FileName, false, Encoding.UTF8))
                                {
                                    sw.WriteLine("Отчет о планах ремонтов");
                                    sw.WriteLine($"Дата формирования: {DateTime.Now:dd.MM.yyyy HH:mm}");
                                    sw.WriteLine();

                                    sw.WriteLine("ID;Оборудование;Тип ТО;Дата начала;Дата окончания;Ответственный;Статус;Связь с аварией");

                                    int count = 0;
                                    while (reader.Read())
                                    {
                                        string line = $"{reader.GetInt32(0)};" +
                                                     $"{reader.GetString(1)};" +
                                                     $"{reader.GetString(2)};" +
                                                     $"{reader.GetDateTime(3):dd.MM.yyyy};" +
                                                     $"{(reader.IsDBNull(4) ? "" : reader.GetDateTime(4).ToString("dd.MM.yyyy"))};" +
                                                     $"{reader.GetString(5)};" +
                                                     $"{reader.GetString(6)};" +
                                                     $"{reader.GetString(7)}";
                                        sw.WriteLine(line);
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
                ShowError("Ошибка экспорта в Excel: " + ex.Message);
            }
        }

        private void ExportToWord()
        {
            try
            {
                SaveFileDialog save = new SaveFileDialog();
                save.Filter = "Rich Text Format (*.rtf)|*.rtf";
                save.FileName = $"Отчет_о_планах_ремонтов_{DateTime.Now:dd-MM-yyyy}.rtf";

                if (save.ShowDialog() == DialogResult.OK)
                {
                    using (var conn = new NpgsqlConnection(connectionString))
                    {
                        conn.Open();

                        string sql = @"
                            SELECT 
                                p.id,
                                o.nazvanie AS equipment,
                                COALESCE(t.nazvanie, 'Не указан') AS tip,
                                p.data_nachala AS start_date,
                                p.data_okonchaniya AS end_date,
                                COALESCE(s.familiya || ' ' || s.imya || ' ' || s.otchestvo, 'Не назначен') AS responsible,
                                p.status,
                                CASE WHEN p.avariya_id IS NOT NULL THEN 'Да' ELSE 'Нет' END AS has_avariya
                            FROM plan_to p
                            JOIN oborudovanie o ON p.oborudovanie_id = o.id
                            LEFT JOIN tip_to t ON p.tip_to_id = t.id
                            LEFT JOIN sotrudniki s ON p.otvetstvenniy_id = s.id
                            ORDER BY p.data_nachala DESC";

                        using (var cmd = new NpgsqlCommand(sql, conn))
                        {
                            using (var reader = cmd.ExecuteReader())
                            {
                                using (StreamWriter sw = new StreamWriter(save.FileName, false, Encoding.UTF8))
                                {
                                    sw.WriteLine(@"{\rtf1\ansi\deff0");
                                    sw.WriteLine(@"{\fonttbl{\f0 Times New Roman;}{\f1 Arial;}}");
                                    sw.WriteLine(@"\f0\fs24");

                                    sw.WriteLine(@"\pard\qc\b\fs32 Отчет о планах ремонтов\b0\fs24\par");
                                    sw.WriteLine(@"\pard\qc\fs20 Дата формирования: " + DateTime.Now.ToString("dd.MM.yyyy HH:mm") + @"\par");
                                    sw.WriteLine(@"\par");

                                    sw.WriteLine(@"\trowd");
                                    for (int i = 0; i < 8; i++)
                                        sw.WriteLine(@"\cellx" + ((i + 1) * 2000));

                                    sw.WriteLine(@"\clbrdrt\brdrw10\brdrs");
                                    sw.WriteLine(@"\clbrdrl\brdrw10\brdrs");
                                    sw.WriteLine(@"\clbrdrb\brdrw10\brdrs");
                                    sw.WriteLine(@"\clbrdrr\brdrw10\brdrs");
                                    sw.WriteLine(@"\clcbpat8\cell");
                                    sw.WriteLine(@"\intbl\b\fs20 ");
                                    sw.Write(@"ID \cell Оборудование \cell Тип ТО \cell Дата начала \cell Дата окончания \cell Ответственный \cell Статус \cell Связь \cell ");
                                    sw.WriteLine(@"\row\b0");

                                    int count = 0;
                                    while (reader.Read())
                                    {
                                        sw.WriteLine(@"\trowd");
                                        for (int i = 0; i < 8; i++)
                                            sw.WriteLine(@"\cellx" + ((i + 1) * 2000));

                                        sw.WriteLine(@"\clbrdrt\brdrw10\brdrs");
                                        sw.WriteLine(@"\clbrdrl\brdrw10\brdrs");
                                        sw.WriteLine(@"\clbrdrb\brdrw10\brdrs");
                                        sw.WriteLine(@"\clbrdrr\brdrw10\brdrs");

                                        if (count % 2 == 0)
                                            sw.WriteLine(@"\clcbpat1");

                                        sw.WriteLine(@"\cell");
                                        sw.WriteLine(@"\intbl\fs20 ");

                                        sw.Write($"{reader.GetInt32(0)} \\cell ");
                                        sw.Write($"{reader.GetString(1)} \\cell ");
                                        sw.Write($"{reader.GetString(2)} \\cell ");
                                        sw.Write($"{reader.GetDateTime(3):dd.MM.yyyy} \\cell ");
                                        sw.Write($"{(reader.IsDBNull(4) ? "" : reader.GetDateTime(4).ToString("dd.MM.yyyy"))} \\cell ");
                                        sw.Write($"{reader.GetString(5)} \\cell ");
                                        sw.Write($"{reader.GetString(6)} \\cell ");
                                        sw.Write($"{reader.GetString(7)} \\cell ");

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
                            COUNT(CASE WHEN status = 'Запланирован' THEN 1 END) as planned
                        FROM plan_to";

                    int totalPlans = 0, completed = 0, inProgress = 0, planned = 0;

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

                    int withoutPlan = totalAvariya - withPlan;
                    int percent = totalPlans > 0 ? (completed * 100 / totalPlans) : 0;

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
                                    $"║ └─ Запланировано: {planned,-34} ║\n" +
                                    $"║ Процент выполнения: {percent,-3}%                                     ║\n" +
                                    $"╟──────────────────────────────────────────────────────────╢\n" +
                                    $"║ АВАРИИ:                                                  ║\n" +
                                    $"║ Всего аварий: {totalAvariya,-39} ║\n" +
                                    $"║ С планом: {withPlan,-42} ║\n" +
                                    $"║ Без плана: {withoutPlan,-41} ║\n" +
                                    $"╚══════════════════════════════════════════════════════════╝";

                    MessageBox.Show(message, "Предпросмотр отчета", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                ShowError("Ошибка предпросмотра: " + ex.Message);
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

