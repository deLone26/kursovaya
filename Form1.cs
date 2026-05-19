using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Web.WebView2.WinForms;
using Microsoft.Web.WebView2.Core;
using Npgsql;

namespace WindowsFormsApp1
{
    public partial class Form1 : Form
    {
        // ================== СТРОКА ПОДКЛЮЧЕНИЯ К БД ==================
        private readonly string connectionString =
            "Host=localhost;Port=5432;Database=boiler_system;Username=postgres;Password=43898362Dd+-;";

        // ================== WebView2 ==================
        private WebView2 webView;
        private string webUIPath;
        private bool isWebViewInitialized = false;

        // ================== ID ВЫБРАННОЙ ЗАПИСИ ==================
        private int selectedId = -1;

        public Form1()
        {
            InitializeComponent();

            // Путь к папке WebUI
            webUIPath = @"C:\Users\Daniil\Desktop\4\kursovaya3\kursovaya\WebUI";

            // Настройка формы
            this.Text = "Управление оборудованием";
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

                // Создаем папку для данных WebView2
                string userDataFolder = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WebView2_Equipment_" + this.GetHashCode());

                var env = await CoreWebView2Environment.CreateAsync(null, userDataFolder);
                await webView.EnsureCoreWebView2Async(env);

                webView.CoreWebView2.Settings.IsScriptEnabled = true;
                webView.CoreWebView2.Settings.IsWebMessageEnabled = true;
                webView.CoreWebView2.Settings.AreDefaultScriptDialogsEnabled = true;

                webView.CoreWebView2.WebMessageReceived += OnWebMessageReceived;

                string htmlPath = Path.Combine(webUIPath, "equipment.html");

                if (File.Exists(htmlPath))
                {
                    webView.CoreWebView2.Navigate($"file:///{htmlPath.Replace('\\', '/')}");
                    isWebViewInitialized = true;

                    webView.CoreWebView2.NavigationCompleted += async (s, e) =>
                    {
                        if (e.IsSuccess)
                        {
                            System.Diagnostics.Debug.WriteLine("Навигация успешно завершена");
                            await LoadEquipment();
                            await LoadStatuses();
                        }
                    };
                }
                else
                {
                    MessageBox.Show($"Файл не найден: {htmlPath}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка инициализации WebView2: {ex.Message}");
            }
        }



        // ================== ЗАГРУЗКА СТАТУСОВ ==================
        private async Task LoadStatuses()
        {
            try
            {
                var statuses = new List<object>();

                using (var conn = new NpgsqlConnection(connectionString))
                {
                    await conn.OpenAsync();

                    string sql = "SELECT id, nazvanie FROM status_oborudovaniya ORDER BY id";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            statuses.Add(new
                            {
                                id = Convert.ToInt32(reader["id"]),
                                nazvanie = reader["nazvanie"].ToString()
                            });
                        }
                    }
                }

                SendToJavaScript(new
                {
                    action = "statusesLoaded",
                    data = statuses
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки статусов: {ex.Message}");
            }
        }

        // ================== ЗАГРУЗКА ОБОРУДОВАНИЯ ==================
        private async Task LoadEquipment(string filter = "")
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("========== НАЧАЛО ЗАГРУЗКИ ОБОРУДОВАНИЯ ==========");
                var equipment = new List<object>();

                using (var conn = new NpgsqlConnection(connectionString))
                {
                    await conn.OpenAsync();

                    string sql = @"
                        SELECT 
                            o.id, 
                            o.nazvanie, 
                            o.tip, 
                            o.model, 
                            o.seriinomer,
                            o.mesto, 
                            o.moshnost, 
                            o.davlenie, 
                            o.proizvoditel, 
                            o.data_ustanovki,
                            o.status_id,
                            s.nazvanie as status_name
                        FROM oborudovanie o
                        LEFT JOIN status_oborudovaniya s ON o.status_id = s.id";

                    if (!string.IsNullOrWhiteSpace(filter))
                    {
                        sql += @" WHERE o.nazvanie ILIKE @filter
                                  OR o.tip ILIKE @filter
                                  OR o.model ILIKE @filter
                                  OR o.proizvoditel ILIKE @filter";
                    }

                    sql += " ORDER BY o.id";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        if (!string.IsNullOrWhiteSpace(filter))
                            cmd.Parameters.AddWithValue("@filter", "%" + filter + "%");

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var eq = new
                                {
                                    id = Convert.ToInt32(reader["id"]),
                                    nazvanie = reader["nazvanie"]?.ToString() ?? "",
                                    tip = reader["tip"]?.ToString() ?? "",
                                    model = reader["model"]?.ToString() ?? "",
                                    seriinomer = reader["seriinomer"]?.ToString() ?? "",
                                    mesto = reader["mesto"]?.ToString() ?? "",
                                    moshnost = reader["moshnost"] != DBNull.Value ? Convert.ToDecimal(reader["moshnost"]) : 0,
                                    davlenie = reader["davlenie"] != DBNull.Value ? Convert.ToDecimal(reader["davlenie"]) : 0,
                                    proizvoditel = reader["proizvoditel"]?.ToString() ?? "",
                                    data_ustanovki = reader["data_ustanovki"]?.ToString() ?? "",
                                    status_id = reader["status_id"] != DBNull.Value ? Convert.ToInt32(reader["status_id"]) : 1,
                                    status_name = reader["status_name"]?.ToString() ?? "Работает"
                                };
                                equipment.Add(eq);
                            }
                        }
                    }
                }

                System.Diagnostics.Debug.WriteLine($"Загружено оборудования: {equipment.Count}");

                SendToJavaScript(new
                {
                    action = "equipmentLoaded",
                    data = equipment,
                    filter = filter
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ОШИБКА: {ex.Message}");
                SendToJavaScript(new
                {
                    action = "error",
                    message = "Ошибка загрузки данных: " + ex.Message
                });
            }
        }

        // ================== ОТПРАВКА В JAVASCRIPT ==================
        private void SendToJavaScript(object data)
        {
            try
            {
                if (isWebViewInitialized && webView?.CoreWebView2 != null)
                {
                    var options = new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    };
                    string json = JsonSerializer.Serialize(data, options);
                    webView.CoreWebView2.PostWebMessageAsString(json);
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
                        string filter = "";
                        if (root.TryGetProperty("filter", out var filterElement))
                            filter = filterElement.GetString();
                        await LoadEquipment(filter);
                        break;

                    case "getEquipment":
                        int getId = root.GetProperty("id").GetInt32();
                        await GetEquipmentById(getId);
                        break;

                    case "addEquipment":
                        var newEquipment = JsonSerializer.Deserialize<EquipmentData>(root.GetProperty("data").GetRawText());
                        await AddEquipment(newEquipment);
                        break;

                    case "updateEquipment":
                        var updateData = JsonSerializer.Deserialize<EquipmentData>(root.GetProperty("data").GetRawText());
                        await UpdateEquipment(updateData);
                        break;

                    case "loadStatuses":
                        await LoadStatuses();
                        break;

                    case "deleteEquipment":
                        int deleteId = root.GetProperty("id").GetInt32();
                        await DeleteEquipment(deleteId);
                        break;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка парсинга: {ex.Message}");
                SendToJavaScript(new
                {
                    action = "error",
                    message = "Ошибка обработки запроса: " + ex.Message
                });
            }
        }

        // ================== ПОЛУЧЕНИЕ ОБОРУДОВАНИЯ ПО ID ==================
        private async Task GetEquipmentById(int id)
        {
            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    await conn.OpenAsync();

                    string sql = @"
                        SELECT 
                            o.id, 
                            o.nazvanie, 
                            o.tip, 
                            o.model, 
                            o.seriinomer,
                            o.mesto, 
                            o.moshnost, 
                            o.davlenie, 
                            o.proizvoditel, 
                            o.data_ustanovki,
                            o.status_id
                        FROM oborudovanie o
                        WHERE o.id = @id";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", id);

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                var eq = new
                                {
                                    id = Convert.ToInt32(reader["id"]),
                                    nazvanie = reader["nazvanie"]?.ToString() ?? "",
                                    tip = reader["tip"]?.ToString() ?? "",
                                    model = reader["model"]?.ToString() ?? "",
                                    seriinomer = reader["seriinomer"]?.ToString() ?? "",
                                    mesto = reader["mesto"]?.ToString() ?? "",
                                    moshnost = reader["moshnost"] != DBNull.Value ? Convert.ToDecimal(reader["moshnost"]) : 0,
                                    davlenie = reader["davlenie"] != DBNull.Value ? Convert.ToDecimal(reader["davlenie"]) : 0,
                                    proizvoditel = reader["proizvoditel"]?.ToString() ?? "",
                                    data_ustanovki = reader["data_ustanovki"]?.ToString() ?? "",
                                    status_id = reader["status_id"] != DBNull.Value ? Convert.ToInt32(reader["status_id"]) : 1
                                };

                                SendToJavaScript(new
                                {
                                    action = "equipmentLoaded",
                                    data = new[] { eq }
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                SendToJavaScript(new
                {
                    action = "error",
                    message = "Ошибка получения данных: " + ex.Message
                });
            }
        }

        // ================== ДОБАВЛЕНИЕ ОБОРУДОВАНИЯ ==================
        private async Task AddEquipment(EquipmentData equipment)
        {
            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    await conn.OpenAsync();

                    string sql = @"
                        INSERT INTO oborudovanie
                        (nazvanie, tip, model, seriinomer, mesto, moshnost, davlenie, proizvoditel, data_ustanovki, status_id)
                        VALUES
                        (@nazvanie, @tip, @model, @seriinomer, @mesto, @moshnost, @davlenie, @proizvoditel, @data_ustanovki, @status_id)";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@nazvanie", equipment.Nazvanie);
                        cmd.Parameters.AddWithValue("@tip", equipment.Tip);
                        cmd.Parameters.AddWithValue("@model", equipment.Model);
                        cmd.Parameters.AddWithValue("@seriinomer", equipment.Seriinomer ?? "");
                        cmd.Parameters.AddWithValue("@mesto", equipment.Mesto ?? "");
                        cmd.Parameters.AddWithValue("@moshnost", equipment.Moshnost);
                        cmd.Parameters.AddWithValue("@davlenie", equipment.Davlenie);
                        cmd.Parameters.AddWithValue("@proizvoditel", equipment.Proizvoditel ?? "");

                        if (string.IsNullOrEmpty(equipment.DataUstanovki))
                            cmd.Parameters.AddWithValue("@data_ustanovki", DBNull.Value);
                        else
                            cmd.Parameters.AddWithValue("@data_ustanovki", DateTime.Parse(equipment.DataUstanovki));

                        cmd.Parameters.AddWithValue("@status_id", equipment.StatusId);

                        await cmd.ExecuteNonQueryAsync();
                    }
                }

                SendToJavaScript(new
                {
                    action = "success",
                    message = "Оборудование успешно добавлено!"
                });

                await LoadEquipment();
            }
            catch (Exception ex)
            {
                SendToJavaScript(new
                {
                    action = "error",
                    message = "Ошибка добавления: " + ex.Message
                });
            }
        }

        // ================== ОБНОВЛЕНИЕ ОБОРУДОВАНИЯ ==================
        private async Task UpdateEquipment(EquipmentData equipment)
        {
            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    await conn.OpenAsync();

                    string sql = @"
                        UPDATE oborudovanie SET
                            nazvanie = @nazvanie,
                            tip = @tip,
                            model = @model,
                            seriinomer = @seriinomer,
                            mesto = @mesto,
                            moshnost = @moshnost,
                            davlenie = @davlenie,
                            proizvoditel = @proizvoditel,
                            data_ustanovki = @data_ustanovki,
                            status_id = @status_id
                        WHERE id = @id";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", equipment.Id);
                        cmd.Parameters.AddWithValue("@nazvanie", equipment.Nazvanie);
                        cmd.Parameters.AddWithValue("@tip", equipment.Tip);
                        cmd.Parameters.AddWithValue("@model", equipment.Model);
                        cmd.Parameters.AddWithValue("@seriinomer", equipment.Seriinomer ?? "");
                        cmd.Parameters.AddWithValue("@mesto", equipment.Mesto ?? "");
                        cmd.Parameters.AddWithValue("@moshnost", equipment.Moshnost);
                        cmd.Parameters.AddWithValue("@davlenie", equipment.Davlenie);
                        cmd.Parameters.AddWithValue("@proizvoditel", equipment.Proizvoditel ?? "");

                        if (string.IsNullOrEmpty(equipment.DataUstanovki))
                            cmd.Parameters.AddWithValue("@data_ustanovki", DBNull.Value);
                        else
                            cmd.Parameters.AddWithValue("@data_ustanovki", DateTime.Parse(equipment.DataUstanovki));

                        cmd.Parameters.AddWithValue("@status_id", equipment.StatusId);

                        await cmd.ExecuteNonQueryAsync();
                    }
                }

                SendToJavaScript(new
                {
                    action = "success",
                    message = "Оборудование успешно обновлено!"
                });

                await LoadEquipment();
            }
            catch (Exception ex)
            {
                SendToJavaScript(new
                {
                    action = "error",
                    message = "Ошибка обновления: " + ex.Message
                });
            }
        }

        // ================== УДАЛЕНИЕ ОБОРУДОВАНИЯ ==================
        private async Task DeleteEquipment(int id)
        {
            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    await conn.OpenAsync();

                    string sql = "DELETE FROM oborudovanie WHERE id = @id";
                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        await cmd.ExecuteNonQueryAsync();
                    }
                }

                SendToJavaScript(new
                {
                    action = "success",
                    message = "Оборудование успешно удалено!"
                });

                await LoadEquipment();
            }
            catch (Exception ex)
            {
                SendToJavaScript(new
                {
                    action = "error",
                    message = "Ошибка удаления: " + ex.Message
                });
            }
        }

        // ================== КЛАСС ДЛЯ ДАННЫХ ОБОРУДОВАНИЯ ==================
        public class EquipmentData
        {
            public int Id { get; set; }
            public string Nazvanie { get; set; }
            public string Tip { get; set; }
            public string Model { get; set; }
            public string Seriinomer { get; set; }
            public string Mesto { get; set; }
            public decimal Moshnost { get; set; }
            public decimal Davlenie { get; set; }
            public string Proizvoditel { get; set; }
            public string DataUstanovki { get; set; }
            public int StatusId { get; set; }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            webView?.Dispose();
            base.OnFormClosing(e);
        }
    }
}
