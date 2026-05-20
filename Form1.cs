using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Web.WebView2.WinForms;
using Microsoft.Web.WebView2.Core;
using Npgsql;

namespace WindowsFormsApp1
{
    public partial class Form1 : Form
    {
        private readonly string connectionString = "Host=localhost;Port=5432;Database=boiler_system;Username=postgres;Password=43898362Dd+-;";
        private WebView2 webView;
        private string webUIPath;
        private bool isWebViewInitialized = false;

        public Form1()
        {
            InitializeComponent();
            webUIPath = @"C:\Users\Daniil\Desktop\4\kursovaya3\kursovaya\WebUI";
            this.Text = "Управление оборудованием";
            this.WindowState = FormWindowState.Maximized;
            this.StartPosition = FormStartPosition.CenterScreen;
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

                string userDataFolder = Path.Combine(Path.GetTempPath(), "WebView2_Equipment_" + this.GetHashCode());
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
                            string filter = root.TryGetProperty("filter", out var f) ? f.GetString() : "";
                            await LoadEquipment(filter);
                            break;
                        case "loadStatuses":
                            await LoadStatuses();
                            break;
                        case "addEquipment":
                            var newEquipment = JsonSerializer.Deserialize<EquipmentData>(root.GetProperty("data").GetRawText());
                            await AddEquipment(newEquipment);
                            break;
                        case "updateEquipment":
                            var updateData = JsonSerializer.Deserialize<EquipmentData>(root.GetProperty("data").GetRawText());
                            System.Diagnostics.Debug.WriteLine($"Десериализовано: ID={updateData?.Id}, Name={updateData?.Nazvanie}");
                            if (updateData != null)
                            {
                                await UpdateEquipment(updateData);
                            }
                            else
                            {
                                SendToJavaScript(new { action = "error", message = "Ошибка десериализации данных" });
                            }
                            break;
                        case "deleteEquipment":
                            int deleteId = root.GetProperty("id").GetInt32();
                            await DeleteEquipment(deleteId);
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка: {ex.Message}");
                SendToJavaScript(new { action = "error", message = ex.Message });
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
                        SELECT o.id, o.nazvanie, o.tip, o.model, o.seriinomer, o.mesto, 
                               o.moshnost, o.davlenie, o.proizvoditel, o.data_ustanovki,
                               o.status_id, s.nazvanie as status_name
                        FROM oborudovanie o
                        LEFT JOIN status_oborudovaniya s ON o.status_id = s.id";

                    if (!string.IsNullOrWhiteSpace(filter))
                        sql += " WHERE o.nazvanie ILIKE @filter OR o.tip ILIKE @filter OR o.model ILIKE @filter";
                    sql += " ORDER BY o.id";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        if (!string.IsNullOrWhiteSpace(filter))
                            cmd.Parameters.AddWithValue("@filter", "%" + filter + "%");

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                equipment.Add(new
                                {
                                    id = reader.GetInt32(0),
                                    nazvanie = reader.IsDBNull(1) ? "" : reader.GetString(1),
                                    tip = reader.IsDBNull(2) ? "" : reader.GetString(2),
                                    model = reader.IsDBNull(3) ? "" : reader.GetString(3),
                                    seriinomer = reader.IsDBNull(4) ? "" : reader.GetString(4),
                                    mesto = reader.IsDBNull(5) ? "" : reader.GetString(5),
                                    moshnost = reader.IsDBNull(6) ? 0 : reader.GetDecimal(6),
                                    davlenie = reader.IsDBNull(7) ? 0 : reader.GetDecimal(7),
                                    proizvoditel = reader.IsDBNull(8) ? "" : reader.GetString(8),
                                    data_ustanovki = reader.IsDBNull(9) ? "" : reader.GetDateTime(9).ToString("yyyy-MM-dd"),
                                    status_id = reader.IsDBNull(10) ? 1 : reader.GetInt32(10),
                                    status_name = reader.IsDBNull(11) ? "Работает" : reader.GetString(11)
                                });
                            }
                        }
                    }
                }
                SendToJavaScript(new { action = "equipmentLoaded", data = equipment });
            }
            catch (Exception ex)
            {
                SendToJavaScript(new { action = "error", message = ex.Message });
            }
        }

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
                            statuses.Add(new { id = reader.GetInt32(0), nazvanie = reader.GetString(1) });
                        }
                    }
                }
                SendToJavaScript(new { action = "statusesLoaded", data = statuses });
            }
            catch (Exception ex)
            {
                SendToJavaScript(new { action = "error", message = ex.Message });
            }
        }

        private async Task AddEquipment(EquipmentData eq)
        {
            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    await conn.OpenAsync();
                    string sql = @"
                        INSERT INTO oborudovanie (nazvanie, tip, model, seriinomer, mesto, moshnost, davlenie, proizvoditel, data_ustanovki, status_id)
                        VALUES (@nazvanie, @tip, @model, @seriinomer, @mesto, @moshnost, @davlenie, @proizvoditel, @data_ustanovki, @status_id)";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@nazvanie", eq.Nazvanie ?? "");
                        cmd.Parameters.AddWithValue("@tip", eq.Tip ?? "");
                        cmd.Parameters.AddWithValue("@model", eq.Model ?? "");
                        cmd.Parameters.AddWithValue("@seriinomer", eq.Seriinomer ?? "");
                        cmd.Parameters.AddWithValue("@mesto", eq.Mesto ?? "");
                        cmd.Parameters.AddWithValue("@moshnost", eq.Moshnost);
                        cmd.Parameters.AddWithValue("@davlenie", eq.Davlenie);
                        cmd.Parameters.AddWithValue("@proizvoditel", eq.Proizvoditel ?? "");

                        if (string.IsNullOrEmpty(eq.DataUstanovki))
                            cmd.Parameters.AddWithValue("@data_ustanovki", DBNull.Value);
                        else
                            cmd.Parameters.AddWithValue("@data_ustanovki", DateTime.Parse(eq.DataUstanovki));

                        cmd.Parameters.AddWithValue("@status_id", eq.StatusId);

                        await cmd.ExecuteNonQueryAsync();
                    }
                }
                SendToJavaScript(new { action = "success", message = "Оборудование добавлено!" });
                await LoadEquipment();
            }
            catch (Exception ex)
            {
                SendToJavaScript(new { action = "error", message = ex.Message });
            }
        }

        private async Task UpdateEquipment(EquipmentData equipment)
        {
            try
            {
                if (equipment == null)
                {
                    SendToJavaScript(new { action = "error", message = "Нет данных для обновления" });
                    return;
                }

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
                        cmd.Parameters.AddWithValue("@nazvanie", equipment.Nazvanie ?? "");
                        cmd.Parameters.AddWithValue("@tip", equipment.Tip ?? "");
                        cmd.Parameters.AddWithValue("@model", equipment.Model ?? "");
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

                        int rowsAffected = await cmd.ExecuteNonQueryAsync();

                        if (rowsAffected > 0)
                        {
                            SendToJavaScript(new
                            {
                                action = "success",
                                message = "Оборудование успешно обновлено!"
                            });
                            await LoadEquipment();
                        }
                        else
                        {
                            SendToJavaScript(new
                            {
                                action = "error",
                                message = $"Оборудование с ID={equipment.Id} не найдено!"
                            });
                        }
                    }
                }
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
                SendToJavaScript(new { action = "success", message = "Оборудование удалено!" });
                await LoadEquipment();
            }
            catch (Exception ex)
            {
                SendToJavaScript(new { action = "error", message = ex.Message });
            }
        }

        private void SendToJavaScript(object data)
        {
            if (isWebViewInitialized && webView?.CoreWebView2 != null)
            {
                string json = JsonSerializer.Serialize(data, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
                System.Diagnostics.Debug.WriteLine($"Отправляем в JS: {json}");
                webView.CoreWebView2.PostWebMessageAsString(json);
            }
        }

        // Класс с атрибутами JsonPropertyName для правильной десериализации
        public class EquipmentData
        {
            [JsonPropertyName("id")]
            public int Id { get; set; }

            [JsonPropertyName("nazvanie")]
            public string Nazvanie { get; set; }

            [JsonPropertyName("tip")]
            public string Tip { get; set; }

            [JsonPropertyName("model")]
            public string Model { get; set; }

            [JsonPropertyName("seriinomer")]
            public string Seriinomer { get; set; }

            [JsonPropertyName("mesto")]
            public string Mesto { get; set; }

            [JsonPropertyName("moshnost")]
            public decimal Moshnost { get; set; }

            [JsonPropertyName("davlenie")]
            public decimal Davlenie { get; set; }

            [JsonPropertyName("proizvoditel")]
            public string Proizvoditel { get; set; }

            [JsonPropertyName("data_ustanovki")]
            public string DataUstanovki { get; set; }

            [JsonPropertyName("status_id")]
            public int StatusId { get; set; }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            webView?.Dispose();
            base.OnFormClosing(e);
        }
    }
}