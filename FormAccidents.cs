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
        private readonly string connectionString =
            "Host=localhost;Port=5432;Database=boiler_system;Username=postgres;Password=43898362Dd+-;Include Error Detail=true";

        private WebView2 webView;
        private string webUIPath;
        private bool isWebViewInitialized = false;

        private int currentEmployeeId;
        private string currentUserLogin;
        private string currentUserRole;
        private string currentEmployeeFullName;

        public FormAccidents(int employeeId, string userLogin, string userRole)
        {
            InitializeComponent();

            this.currentEmployeeId = employeeId;
            this.currentUserLogin = userLogin;
            this.currentUserRole = userRole;
            this.currentEmployeeFullName = GetFullName(employeeId);

            webUIPath = @"C:\Users\Daniil\Desktop\4\kursovaya3\kursovaya\WebUI";

            this.Text = "Журнал аварий";
            this.WindowState = FormWindowState.Maximized;
            this.StartPosition = FormStartPosition.CenterScreen;

            this.Controls.Clear();
            InitializeWebView();
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
                        return result?.ToString() ?? "Оператор";
                    }
                }
            }
            catch { return "Оператор"; }
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

                if (File.Exists(htmlPath))
                {
                    webView.CoreWebView2.Navigate($"file:///{htmlPath.Replace('\\', '/')}");
                    isWebViewInitialized = true;

                    webView.CoreWebView2.NavigationCompleted += async (s, e) =>
                    {
                        await Task.Delay(500);
                        await SetCurrentUserInWebView();
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

        private async Task SetCurrentUserInWebView()
        {
            if (isWebViewInitialized && webView?.CoreWebView2 != null)
            {
                string script = $"setCurrentUser({currentEmployeeId}, '{currentUserLogin}', '{currentUserRole}', '{currentEmployeeFullName}');";
                await webView.CoreWebView2.ExecuteScriptAsync(script);
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
                            TO_CHAR(a.data_avarii, 'DD.MM.YYYY HH24:MI') as date,
                            COALESCE(a.opisanie, '') AS description,
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
                            cmd.Parameters.AddWithValue("@end", DateTime.Parse(endDate).Date);
                        }

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                accidents.Add(new
                                {
                                    id = reader.GetInt32(0),
                                    equipment = reader.GetString(1),
                                    date = reader.GetString(2),
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

        private async Task AddAccident(JsonElement root)
        {
            try
            {
                int equipmentId = root.GetProperty("equipment").GetInt32();
                string date = root.GetProperty("date").GetString();
                string time = root.GetProperty("time").GetString();
                string description = root.GetProperty("description").GetString();
                string consequences = root.GetProperty("consequences").GetString();

                DateTime accidentDateTime = DateTime.Parse($"{date} {time}");

                // Проверка: нельзя регистрировать аварию на прошедшую дату
                if (accidentDateTime.Date < DateTime.Now.Date)
                {
                    SendToWebView("showError", "Нельзя зарегистрировать аварию на прошедшую дату!");
                    return;
                }

                using (var conn = new NpgsqlConnection(connectionString))
                {
                    await conn.OpenAsync();

                    string sql = @"
                INSERT INTO avariya 
                (oborudovanie_id, data_avarii, opisanie, posledstviya, status)
                VALUES 
                (@oborudovanie_id, @data_avarii, @opisanie, @posledstviya, 'Зарегистрирована')";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@oborudovanie_id", equipmentId);
                        cmd.Parameters.AddWithValue("@data_avarii", accidentDateTime);
                        cmd.Parameters.AddWithValue("@opisanie", description);
                        cmd.Parameters.AddWithValue("@posledstviya", consequences ?? "");
                        await cmd.ExecuteNonQueryAsync();
                    }
                }

                SendToWebView("showSuccess", "Авария зарегистрирована!");
                await LoadAccidents();
            }
            catch (Exception ex)
            {
                SendToWebView("showError", "Ошибка добавления: " + ex.Message);
            }
        }

        private async Task UpdateAccident(JsonElement root)
        {
            try
            {
                int id = root.GetProperty("id").GetInt32();
                int equipmentId = root.GetProperty("equipment").GetInt32();
                string date = root.GetProperty("date").GetString();
                string time = root.GetProperty("time").GetString();
                string description = root.GetProperty("description").GetString();
                string consequences = root.GetProperty("consequences").GetString();

                DateTime accidentDateTime = DateTime.Parse($"{date} {time}");

                using (var conn = new NpgsqlConnection(connectionString))
                {
                    await conn.OpenAsync();

                    // При обновлении не меняем статус (статус меняется только когда начальник создаёт план)
                    string sql = @"
                        UPDATE avariya SET
                            oborudovanie_id = @oborudovanie_id,
                            data_avarii = @data_avarii,
                            opisanie = @opisanie,
                            posledstviya = @posledstviya
                        WHERE id = @id";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        cmd.Parameters.AddWithValue("@oborudovanie_id", equipmentId);
                        cmd.Parameters.AddWithValue("@data_avarii", accidentDateTime);
                        cmd.Parameters.AddWithValue("@opisanie", description);
                        cmd.Parameters.AddWithValue("@posledstviya", consequences ?? "");
                        await cmd.ExecuteNonQueryAsync();
                    }
                }

                SendToWebView("showSuccess", "Авария обновлена!");
                await LoadAccidents();
            }
            catch (Exception ex)
            {
                SendToWebView("showError", "Ошибка обновления: " + ex.Message);
            }
        }

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

                SendToWebView("showSuccess", "Авария удалена!");
                await LoadAccidents();
            }
            catch (Exception ex)
            {
                SendToWebView("showError", "Ошибка удаления: " + ex.Message);
            }
        }

        // Метод для автоматического обновления статуса аварии при создании плана
        public async Task UpdateAccidentStatusOnPlanCreated(int accidentId)
        {
            if (accidentId <= 0) return;
            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    await conn.OpenAsync();
                    string sql = "UPDATE avariya SET status = 'В работе' WHERE id = @id AND status = 'Зарегистрирована'";
                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", accidentId);
                        await cmd.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка обновления статуса аварии: {ex.Message}");
            }
        }

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

        private async void OnWebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            string message = e.TryGetWebMessageAsString();
            System.Diagnostics.Debug.WriteLine($"Получено от JS: {message}");

            try
            {
                using (var jsonDoc = JsonDocument.Parse(message))
                {
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

                        case "logout":
                            this.Invoke(new Action(() =>
                            {
                                LoginForm loginForm = new LoginForm();
                                loginForm.Show();
                                this.Close();
                            }));
                            break;
                    }
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