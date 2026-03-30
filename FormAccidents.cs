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
    public partial class FormAccidents : Form
    {
        // ================== СТРОКА ПОДКЛЮЧЕНИЯ К БД ==================
        private readonly string connectionString =
            "Host=localhost;Port=5432;Database=boiler_system;Username=postgres;Password=43898362Dd+-;";

        // ================== WebView2 ==================
        private WebView2 webView;
        private string webUIPath;
        private bool isWebViewInitialized = false;

        public FormAccidents()
        {
            InitializeComponent();

            // Путь к папке WebUI
            webUIPath = @"C:\Users\Daniil\Desktop\4\kursovaya3\kursovaya\WebUI";

            // Настройка формы
            this.Text = "Журнал аварий";
            this.WindowState = FormWindowState.Maximized;
            this.StartPosition = FormStartPosition.CenterScreen;

            // Очищаем все старые элементы
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

                string htmlPath = Path.Combine(webUIPath, "accidents.html");
                System.Diagnostics.Debug.WriteLine($"Загрузка HTML из: {htmlPath}");

                if (File.Exists(htmlPath))
                {
                    webView.CoreWebView2.Navigate($"file:///{htmlPath}");
                    isWebViewInitialized = true;

                    webView.CoreWebView2.NavigationCompleted += async (s, e) =>
                    {
                        System.Diagnostics.Debug.WriteLine("Навигация завершена");
                        await Task.Delay(500);
                        await LoadInitialData();
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

        private async Task LoadInitialData()
        {
            await LoadEquipment();
            await LoadAccidents();
            await LoadStatistics();
        }

        private async Task LoadEquipment()
        {
            try
            {
                var equipmentList = new List<object>();

                using (var conn = new NpgsqlConnection(connectionString))
                {
                    await conn.OpenAsync();
                    string sql = "SELECT id, nazvanie FROM oborudovanie ORDER BY nazvanie";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            equipmentList.Add(new
                            {
                                id = reader.GetInt32(0),
                                name = reader.GetString(1)
                            });
                        }
                    }
                }

                string json = JsonSerializer.Serialize(equipmentList, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                SendToWebView("fillEquipment", json);
            }
            catch (Exception ex)
            {
                SendToWebView("showError", "Ошибка загрузки оборудования: " + ex.Message);
            }
        }

        private async Task LoadAccidents(string startDate = "", string endDate = "", bool showAll = false)
        {
            try
            {
                var accidents = new List<object>();

                using (var conn = new NpgsqlConnection(connectionString))
                {
                    await conn.OpenAsync();

                    string sql = @"
                        SELECT 
                            a.id,
                            o.nazvanie AS equipment,
                            a.data_avarii AS date,
                            a.opisanie AS description,
                            COALESCE(a.posledstviya, '') AS consequences,
                            COALESCE(a.status, 'Зарегистрирована') AS status,
                            CASE WHEN p.id IS NOT NULL THEN '✅' ELSE '❌' END AS has_plan
                        FROM avariya a
                        JOIN oborudovanie o ON a.oborudovanie_id = o.id
                        LEFT JOIN plan_to p ON a.id = p.avariya_id";

                    if (!showAll && !string.IsNullOrEmpty(startDate) && !string.IsNullOrEmpty(endDate))
                    {
                        sql += " WHERE DATE(a.data_avarii) BETWEEN @start AND @end";
                    }

                    sql += " ORDER BY a.data_avarii DESC";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        if (!showAll && !string.IsNullOrEmpty(startDate) && !string.IsNullOrEmpty(endDate))
                        {
                            cmd.Parameters.AddWithValue("@start", DateTime.Parse(startDate).Date);
                            cmd.Parameters.AddWithValue("@end", DateTime.Parse(endDate).Date.AddDays(1).AddSeconds(-1));
                        }

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                accidents.Add(new
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
                        }
                    }
                }

                string json = JsonSerializer.Serialize(accidents, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                SendToWebView("displayAccidents", json);
                await LoadStatistics();
            }
            catch (Exception ex)
            {
                SendToWebView("showError", "Ошибка загрузки аварий: " + ex.Message);
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
                            COUNT(CASE WHEN status = 'В работе' THEN 1 END) AS in_progress,
                            COUNT(CASE WHEN status = 'Завершена' THEN 1 END) AS completed,
                            COUNT(CASE WHEN status = 'Требует ремонта' THEN 1 END) AS need_plan
                        FROM avariya";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            var stats = new
                            {
                                total = reader.GetInt32(0),
                                inProgress = reader.GetInt32(1),
                                completed = reader.GetInt32(2),
                                needPlan = reader.GetInt32(3)
                            };

                            string json = JsonSerializer.Serialize(stats, new JsonSerializerOptions
                            {
                                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                            });

                            SendToWebView("updateAccidentStatistics", json);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка статистики: {ex.Message}");
            }
        }

        // ================== ДОБАВЛЕНИЕ АВАРИИ ==================
        private async Task AddAccident(JsonElement root)
        {
            try
            {
                int equipmentId = root.GetProperty("equipment").GetInt32();
                string dateTime = root.GetProperty("dateTime").GetString();
                string description = root.GetProperty("description").GetString();
                string consequences = root.GetProperty("consequences").GetString();
                string status = root.GetProperty("status").GetString();

                // Замените мягкий знак на твердый, если нужно
                // или приведите к правильному формату
                string correctStatus = status switch
                {
                    "Завершена" => "Завершена",  // если в БД "Завершена" (без мягкого знака)
                    "В работе" => "В работе",
                    "Зарегистрирована" => "Зарегистрирована",
                    "Требует ремонта" => "Требует ремонта",
                    _ => "Зарегистрирована"
                };

                DateTime accidentDateTime;
                if (!DateTime.TryParse(dateTime, out accidentDateTime))
                {
                    accidentDateTime = DateTime.Now;
                }

                using (var conn = new NpgsqlConnection(connectionString))
                {
                    await conn.OpenAsync();

                    string sql = @"
                INSERT INTO avariya 
                (oborudovanie_id, data_avarii, opisanie, posledstviya, status)
                VALUES 
                (@oborudovanie_id, @data_avarii, @opisanie, @posledstviya, @status)";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@oborudovanie_id", equipmentId);
                        cmd.Parameters.AddWithValue("@data_avarii", accidentDateTime);
                        cmd.Parameters.AddWithValue("@opisanie", description);
                        cmd.Parameters.AddWithValue("@posledstviya", consequences ?? "");
                        cmd.Parameters.AddWithValue("@status", correctStatus);

                        int rowsAffected = await cmd.ExecuteNonQueryAsync();

                        if (rowsAffected > 0)
                        {
                            SendToWebView("showSuccess", "Авария успешно добавлена!");
                            await LoadAccidents();
                        }
                        else
                        {
                            SendToWebView("showError", "Не удалось добавить запись");
                        }
                    }
                }
            }
            catch (PostgresException pgEx)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка PostgreSQL: {pgEx.Message}");
                SendToWebView("showError", $"Ошибка БД: {pgEx.Message}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка: {ex.Message}");
                SendToWebView("showError", $"Ошибка: {ex.Message}");
            }
        }

        // ================== ОБНОВЛЕНИЕ АВАРИИ ==================
        private async Task UpdateAccident(JsonElement root)
        {
            try
            {
                int id = root.GetProperty("id").GetInt32();
                int equipmentId = root.GetProperty("equipment").GetInt32();
                string dateTime = root.GetProperty("dateTime").GetString();
                string description = root.GetProperty("description").GetString();
                string consequences = root.GetProperty("consequences").GetString();
                string status = root.GetProperty("status").GetString();

                DateTime accidentDateTime = DateTime.Parse(dateTime);

                using (var conn = new NpgsqlConnection(connectionString))
                {
                    await conn.OpenAsync();

                    string sql = @"
                        UPDATE avariya SET
                            oborudovanie_id = @oborudovanie_id,
                            data_avarii = @data_avarii,
                            opisanie = @opisanie,
                            posledstviya = @posledstviya,
                            status = @status
                        WHERE id = @id";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        cmd.Parameters.AddWithValue("@oborudovanie_id", equipmentId);
                        cmd.Parameters.AddWithValue("@data_avarii", accidentDateTime);
                        cmd.Parameters.AddWithValue("@opisanie", description);
                        cmd.Parameters.AddWithValue("@posledstviya", consequences ?? "");
                        cmd.Parameters.AddWithValue("@status", status ?? "Зарегистрирована");

                        int rowsAffected = await cmd.ExecuteNonQueryAsync();
                        System.Diagnostics.Debug.WriteLine($"Обновлено строк: {rowsAffected}");
                    }
                }

                SendToWebView("showSuccess", "Авария успешно обновлена!");
                await LoadAccidents();
            }
            catch (Exception ex)
            {
                SendToWebView("showError", "Ошибка обновления: " + ex.Message);
            }
        }

        // ================== УДАЛЕНИЕ АВАРИИ ==================
        private async Task DeleteAccident(int id)
        {
            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    await conn.OpenAsync();
                    string sql = "DELETE FROM avariya WHERE id = @id";
                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        await cmd.ExecuteNonQueryAsync();
                    }
                }

                SendToWebView("showSuccess", "Авария успешно удалена!");
                await LoadAccidents();
            }
            catch (Exception ex)
            {
                SendToWebView("showError", "Ошибка удаления: " + ex.Message);
            }
        }

        // ================== ОТПРАВКА СООБЩЕНИЙ В JAVASCRIPT ==================
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

        // ================== ОБРАБОТКА СООБЩЕНИЙ ОТ JAVASCRIPT ==================
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
                    case "loadEquipment":
                        await LoadEquipment();
                        break;

                    case "loadAccidents":
                        string startDate = root.TryGetProperty("startDate", out var s) ? s.GetString() : "";
                        string endDate = root.TryGetProperty("endDate", out var eDate) ? eDate.GetString() : "";
                        bool showAll = root.TryGetProperty("showAll", out var all) && all.GetBoolean();
                        await LoadAccidents(startDate, endDate, showAll);
                        break;

                    case "addAccident":
                        await AddAccident(root);
                        break;

                    case "updateAccident":
                        await UpdateAccident(root);
                        break;

                    case "deleteAccident":
                        int deleteId = root.GetProperty("id").GetInt32();
                        await DeleteAccident(deleteId);
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