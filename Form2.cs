using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Security.Cryptography;
using Microsoft.Web.WebView2.WinForms;
using Microsoft.Web.WebView2.Core;
using Npgsql;

namespace WindowsFormsApp1
{
    public partial class Form2 : Form
    {
        // ================== СТРОКИ ПОДКЛЮЧЕНИЯ К БД ==================
        private readonly string connectionString =
            "Host=localhost;Port=5432;Database=boiler_system;Username=postgres;Password=43898362Dd+-;";

        private readonly string adminConnectionString =
            "Host=localhost;Username=postgres;Password=43898362Dd+-;Database=boiler_system;";

        // ================== WebView2 ==================
        private WebView2 webView;
        private string webUIPath;
        private bool isWebViewInitialized = false;

        public Form2()
        {
            InitializeComponent();

            // Путь к папке WebUI
            webUIPath = @"C:\Users\Daniil\Desktop\4\kursovaya2\kursovaya\WebUI";

            // Настройка формы
            this.Text = "Управление сотрудниками";
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

                // Разрешаем скрипты и сообщения
                webView.CoreWebView2.Settings.IsScriptEnabled = true;
                webView.CoreWebView2.Settings.IsWebMessageEnabled = true;

                // Регистрируем обработчик для сообщений от JavaScript
                webView.CoreWebView2.WebMessageReceived += OnWebMessageReceived;

                string htmlPath = Path.Combine(webUIPath, "employees.html");
                System.Diagnostics.Debug.WriteLine($"Загрузка HTML из: {htmlPath}");

                if (File.Exists(htmlPath))
                {
                    webView.CoreWebView2.Navigate($"file:///{htmlPath}");
                    isWebViewInitialized = true;

                    webView.CoreWebView2.NavigationCompleted += async (s, e) =>
                    {
                        System.Diagnostics.Debug.WriteLine("Навигация завершена");
                        await Task.Delay(1000);
                        await LoadEmployees();
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

        // ================== ЗАГРУЗКА ВСЕХ СОТРУДНИКОВ ==================
        private async Task LoadEmployees()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("========== НАЧАЛО ЗАГРУЗКИ ==========");
                var employees = new List<object>();

                using (var conn = new NpgsqlConnection(connectionString))
                {
                    await conn.OpenAsync();
                    System.Diagnostics.Debug.WriteLine("1. Подключение открыто");

                    string sql = @"
                        SELECT 
                            s.id, 
                            s.familiya, 
                            s.imya, 
                            s.otchestvo, 
                            s.dolzhnost, 
                            s.telefon, 
                            s.email,
                            u.login
                        FROM sotrudniki s
                        LEFT JOIN users u ON s.id = u.sotrudnik_id
                        ORDER BY s.id";

                    System.Diagnostics.Debug.WriteLine($"2. Выполняем запрос: {sql}");

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        System.Diagnostics.Debug.WriteLine("3. Читаем данные...");

                        while (await reader.ReadAsync())
                        {
                            int sotrudnikId = Convert.ToInt32(reader["id"]);
                            string login = reader["login"]?.ToString() ?? "";
                            string role = "operator"; // роль по умолчанию

                            // Если есть логин, определяем роль
                            if (!string.IsNullOrEmpty(login))
                            {
                                role = await GetUserRole(login);
                            }

                            var emp = new
                            {
                                id = sotrudnikId,
                                familiya = reader["familiya"]?.ToString() ?? "",
                                imya = reader["imya"]?.ToString() ?? "",
                                otchestvo = reader["otchestvo"]?.ToString() ?? "",
                                dolzhnost = reader["dolzhnost"]?.ToString() ?? "",
                                telefon = reader["telefon"]?.ToString() ?? "",
                                email = reader["email"]?.ToString() ?? "",
                                role = role,
                                login = login
                            };
                            employees.Add(emp);
                            System.Diagnostics.Debug.WriteLine($"   - Загружен: ID={emp.id}, {emp.familiya} {emp.imya}, роль={emp.role}");
                        }
                    }
                }

                System.Diagnostics.Debug.WriteLine($"4. Всего загружено сотрудников: {employees.Count}");

                // Отправляем данные в JavaScript
                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                string json = JsonSerializer.Serialize(new { employees = employees }, options);
                System.Diagnostics.Debug.WriteLine($"5. JSON для отправки: {json}");

                // Вызываем JavaScript функцию для обновления таблицы
                string script = $"window.updateEmployees({json});";
                System.Diagnostics.Debug.WriteLine($"6. Выполняем скрипт: {script}");

                if (isWebViewInitialized && webView?.CoreWebView2 != null)
                {
                    await webView.CoreWebView2.ExecuteScriptAsync(script);
                    System.Diagnostics.Debug.WriteLine("7. Скрипт выполнен");
                }

                System.Diagnostics.Debug.WriteLine("========== КОНЕЦ ЗАГРУЗКИ ==========");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"!!!!!!!!!! ОШИБКА: {ex.Message}");
                MessageBox.Show($"Ошибка загрузки данных:\n{ex.Message}", "Ошибка");
            }
        }

        // ================== ОПРЕДЕЛЕНИЕ РОЛИ ПОЛЬЗОВАТЕЛЯ ==================
        private async Task<string> GetUserRole(string login)
        {
            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    await conn.OpenAsync();

                    // Проверяем членство в группах ролей
                    string[] roles = { "admin", "boss", "slesar", "operator" };

                    foreach (string role in roles)
                    {
                        string sql = @"
                            SELECT 1 
                            FROM pg_auth_members m
                            JOIN pg_roles r ON m.roleid = r.oid
                            JOIN pg_roles u ON m.member = u.oid
                            WHERE u.rolname = @login AND r.rolname = @role";

                        using (var cmd = new NpgsqlCommand(sql, conn))
                        {
                            cmd.Parameters.AddWithValue("@login", login);
                            cmd.Parameters.AddWithValue("@role", $"app_{role}");

                            var result = await cmd.ExecuteScalarAsync();
                            if (result != null)
                            {
                                return role;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка определения роли для {login}: {ex.Message}");
            }

            return "operator";
        }

        // ================== ОТПРАВКА СООБЩЕНИЙ В JAVASCRIPT ==================
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
        private void OnWebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
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
                    case "loadEmployees":
                        _ = LoadEmployees();
                        break;

                    case "addEmployee":
                        var newEmployee = JsonSerializer.Deserialize<EmployeeData>(root.GetProperty("data").GetRawText());
                        _ = AddEmployee(newEmployee);
                        break;

                    case "updateEmployee":
                        var updateData = JsonSerializer.Deserialize<EmployeeData>(root.GetProperty("data").GetRawText());
                        _ = UpdateEmployee(updateData);
                        break;

                    case "deleteEmployee":
                        int deleteId = root.GetProperty("id").GetInt32();
                        _ = DeleteEmployee(deleteId);
                        break;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка парсинга: {ex.Message}");
            }
        }

        // ================== ХЕШИРОВАНИЕ ПАРОЛЯ ==================
        private string HashPassword(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                StringBuilder builder = new StringBuilder();
                foreach (byte b in bytes)
                    builder.Append(b.ToString("x2"));
                return builder.ToString();
            }
        }

        // ================== ДОБАВЛЕНИЕ СОТРУДНИКА ==================
        private async Task AddEmployee(EmployeeData employee)
        {
            try
            {
                int sotrudnikId;

                using (var conn = new NpgsqlConnection(connectionString))
                {
                    await conn.OpenAsync();

                    using (var transaction = await conn.BeginTransactionAsync())
                    {
                        try
                        {
                            // Добавляем сотрудника
                            string sqlSotrudnik = @"
                                INSERT INTO sotrudniki
                                (familiya, imya, otchestvo, dolzhnost, telefon, email)
                                VALUES (@familiya, @imya, @otchestvo, @dolzhnost, @telefon, @email)
                                RETURNING id";

                            using (var cmd = new NpgsqlCommand(sqlSotrudnik, conn))
                            {
                                cmd.Parameters.AddWithValue("@familiya", employee.Familiya);
                                cmd.Parameters.AddWithValue("@imya", employee.Imya);
                                cmd.Parameters.AddWithValue("@otchestvo", employee.Otchestvo ?? "");
                                cmd.Parameters.AddWithValue("@dolzhnost", employee.Dolzhnost ?? "");
                                cmd.Parameters.AddWithValue("@telefon", employee.Telefon ?? "");
                                cmd.Parameters.AddWithValue("@email", employee.Email ?? "");

                                sotrudnikId = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                            }

                            // Добавляем пользователя
                            string sqlUser = @"
                                INSERT INTO users 
                                (login, created_at, is_active, sotrudnik_id)
                                VALUES 
                                (@login, @created_at, @is_active, @sotrudnik_id)";

                            using (var cmd = new NpgsqlCommand(sqlUser, conn))
                            {
                                cmd.Parameters.AddWithValue("@login", employee.Login);
                                cmd.Parameters.AddWithValue("@created_at", DateTime.Now);
                                cmd.Parameters.AddWithValue("@is_active", true);
                                cmd.Parameters.AddWithValue("@sotrudnik_id", sotrudnikId);
                                await cmd.ExecuteNonQueryAsync();
                            }

                            await transaction.CommitAsync();
                        }
                        catch
                        {
                            await transaction.RollbackAsync();
                            throw;
                        }
                    }
                }

                // Создаем пользователя PostgreSQL и назначаем роль
                try
                {
                    await CreatePostgresUser(employee.Login, employee.Password, employee.Role);
                }
                catch (Exception pgEx)
                {
                    SendToJavaScript(new
                    {
                        action = "warning",
                        message = "Сотрудник добавлен, но ошибка создания пользователя БД: " + pgEx.Message
                    });

                    return;
                }

                SendToJavaScript(new
                {
                    action = "success",
                    message = "Сотрудник успешно добавлен!"
                });

                await LoadEmployees();
            }
            catch (Exception ex)
            {
                SendToJavaScript(new
                {
                    action = "error",
                    message = "Ошибка добавления сотрудника: " + ex.Message
                });
            }
        }

        // ================== ОБНОВЛЕНИЕ СОТРУДНИКА ==================
        private async Task UpdateEmployee(EmployeeData employee)
        {
            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    await conn.OpenAsync();

                    using (var transaction = await conn.BeginTransactionAsync())
                    {
                        try
                        {
                            // Обновляем сотрудника
                            string sqlSotrudnik = @"
                                UPDATE sotrudniki SET 
                                    familiya = @familiya,
                                    imya = @imya,
                                    otchestvo = @otchestvo,
                                    dolzhnost = @dolzhnost,
                                    telefon = @telefon,
                                    email = @email
                                WHERE id = @id";

                            using (var cmd = new NpgsqlCommand(sqlSotrudnik, conn))
                            {
                                cmd.Parameters.AddWithValue("@id", employee.Id);
                                cmd.Parameters.AddWithValue("@familiya", employee.Familiya);
                                cmd.Parameters.AddWithValue("@imya", employee.Imya);
                                cmd.Parameters.AddWithValue("@otchestvo", employee.Otchestvo ?? "");
                                cmd.Parameters.AddWithValue("@dolzhnost", employee.Dolzhnost ?? "");
                                cmd.Parameters.AddWithValue("@telefon", employee.Telefon ?? "");
                                cmd.Parameters.AddWithValue("@email", employee.Email ?? "");
                                await cmd.ExecuteNonQueryAsync();
                            }

                            // Обновляем логин
                            string sqlUser = @"
                                UPDATE users SET 
                                    login = @login
                                WHERE sotrudnik_id = @id";

                            using (var cmd = new NpgsqlCommand(sqlUser, conn))
                            {
                                cmd.Parameters.AddWithValue("@id", employee.Id);
                                cmd.Parameters.AddWithValue("@login", employee.Login);
                                await cmd.ExecuteNonQueryAsync();
                            }

                            await transaction.CommitAsync();
                        }
                        catch
                        {
                            await transaction.RollbackAsync();
                            throw;
                        }
                    }
                }

                // Обновляем роль в PostgreSQL
                if (!string.IsNullOrEmpty(employee.Password))
                {
                    await UpdatePostgresUserRole(employee.Login, employee.Role, employee.Password);
                }
                else
                {
                    await UpdatePostgresUserRole(employee.Login, employee.Role);
                }

                SendToJavaScript(new
                {
                    action = "success",
                    message = "Данные сотрудника обновлены!"
                });

                await LoadEmployees();
            }
            catch (Exception ex)
            {
                SendToJavaScript(new
                {
                    action = "error",
                    message = "Ошибка обновления сотрудника: " + ex.Message
                });
            }
        }

        // ================== УДАЛЕНИЕ СОТРУДНИКА ==================
        private async Task DeleteEmployee(int id)
        {
            try
            {
                // Получаем логин пользователя перед удалением
                string login = "";
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    await conn.OpenAsync();
                    string sql = "SELECT login FROM users WHERE sotrudnik_id = @id";
                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        var result = await cmd.ExecuteScalarAsync();
                        if (result != null)
                            login = result.ToString();
                    }
                }

                using (var conn = new NpgsqlConnection(connectionString))
                {
                    await conn.OpenAsync();

                    using (var transaction = await conn.BeginTransactionAsync())
                    {
                        try
                        {
                            // Удаляем из users
                            string sqlUser = "DELETE FROM users WHERE sotrudnik_id = @id";
                            using (var cmd = new NpgsqlCommand(sqlUser, conn))
                            {
                                cmd.Parameters.AddWithValue("@id", id);
                                await cmd.ExecuteNonQueryAsync();
                            }

                            // Удаляем из sotrudniki
                            string sqlSotrudnik = "DELETE FROM sotrudniki WHERE id = @id";
                            using (var cmd = new NpgsqlCommand(sqlSotrudnik, conn))
                            {
                                cmd.Parameters.AddWithValue("@id", id);
                                await cmd.ExecuteNonQueryAsync();
                            }

                            await transaction.CommitAsync();
                        }
                        catch
                        {
                            await transaction.RollbackAsync();
                            throw;
                        }
                    }
                }

                // Удаляем пользователя PostgreSQL
                if (!string.IsNullOrEmpty(login))
                {
                    try
                    {
                        using (var adminConn = new NpgsqlConnection(adminConnectionString))
                        {
                            await adminConn.OpenAsync();

                            // Отзываем все роли
                            string[] roles = { "admin", "boss", "slesar", "operator" };
                            foreach (string role in roles)
                            {
                                try
                                {
                                    using (var cmd = new NpgsqlCommand($"REVOKE app_{role} FROM \"{login}\"", adminConn))
                                    {
                                        await cmd.ExecuteNonQueryAsync();
                                    }
                                }
                                catch { }
                            }

                            // Удаляем пользователя
                            using (var cmd = new NpgsqlCommand($"DROP USER IF EXISTS \"{login}\"", adminConn))
                            {
                                await cmd.ExecuteNonQueryAsync();
                            }
                        }
                    }
                    catch (Exception pgEx)
                    {
                        SendToJavaScript(new
                        {
                            action = "warning",
                            message = "Сотрудник удален, но ошибка удаления пользователя БД: " + pgEx.Message
                        });
                        return;
                    }
                }

                SendToJavaScript(new
                {
                    action = "success",
                    message = "Сотрудник удален!"
                });

                await LoadEmployees();
            }
            catch (Exception ex)
            {
                SendToJavaScript(new
                {
                    action = "error",
                    message = "Ошибка удаления сотрудника: " + ex.Message
                });
            }
        }

        // ================== СОЗДАНИЕ ПОЛЬЗОВАТЕЛЯ POSTGRESQL ==================
        private async Task CreatePostgresUser(string login, string password, string role)
        {
            using (var adminConn = new NpgsqlConnection(adminConnectionString))
            {
                await adminConn.OpenAsync();

                // Проверяем существование пользователя
                using (var cmdCheck = new NpgsqlCommand())
                {
                    cmdCheck.Connection = adminConn;
                    cmdCheck.CommandText = "SELECT 1 FROM pg_roles WHERE rolname = @login";
                    cmdCheck.Parameters.AddWithValue("@login", login);
                    var exists = await cmdCheck.ExecuteScalarAsync();

                    if (exists == null)
                    {
                        // Создаем пользователя
                        using (var cmdCreate = new NpgsqlCommand())
                        {
                            cmdCreate.Connection = adminConn;
                            cmdCreate.CommandText = $"CREATE USER \"{login}\" WITH PASSWORD @password";
                            cmdCreate.Parameters.AddWithValue("@password", password);
                            await cmdCreate.ExecuteNonQueryAsync();
                        }
                    }

                    // Назначаем роль (сначала отзываем все, потом назначаем нужную)
                    string[] roles = { "admin", "boss", "slesar", "operator" };
                    foreach (string r in roles)
                    {
                        try
                        {
                            using (var cmdRevoke = new NpgsqlCommand($"REVOKE app_{r} FROM \"{login}\"", adminConn))
                            {
                                await cmdRevoke.ExecuteNonQueryAsync();
                            }
                        }
                        catch { }
                    }

                    // Назначаем новую роль
                    using (var cmdGrant = new NpgsqlCommand($"GRANT app_{role} TO \"{login}\"", adminConn))
                    {
                        await cmdGrant.ExecuteNonQueryAsync();
                    }
                }
            }
        }

        // ================== ОБНОВЛЕНИЕ РОЛИ ПОЛЬЗОВАТЕЛЯ POSTGRESQL ==================
        private async Task UpdatePostgresUserRole(string login, string role, string password = null)
        {
            using (var adminConn = new NpgsqlConnection(adminConnectionString))
            {
                await adminConn.OpenAsync();

                // Обновляем пароль, если передан
                if (!string.IsNullOrEmpty(password))
                {
                    using (var cmdPass = new NpgsqlCommand($"ALTER USER \"{login}\" WITH PASSWORD @password", adminConn))
                    {
                        cmdPass.Parameters.AddWithValue("@password", password);
                        await cmdPass.ExecuteNonQueryAsync();
                    }
                }

                // Обновляем роль
                string[] roles = { "admin", "boss", "slesar", "operator" };
                foreach (string r in roles)
                {
                    try
                    {
                        using (var cmdRevoke = new NpgsqlCommand($"REVOKE app_{r} FROM \"{login}\"", adminConn))
                        {
                            await cmdRevoke.ExecuteNonQueryAsync();
                        }
                    }
                    catch { }
                }

                using (var cmdGrant = new NpgsqlCommand($"GRANT app_{role} TO \"{login}\"", adminConn))
                {
                    await cmdGrant.ExecuteNonQueryAsync();
                }
            }
        }

        // ================== КЛАСС ДЛЯ ДАННЫХ СОТРУДНИКА ==================
        public class EmployeeData
        {
            public int Id { get; set; }
            public string Familiya { get; set; }
            public string Imya { get; set; }
            public string Otchestvo { get; set; }
            public string Dolzhnost { get; set; }
            public string Telefon { get; set; }
            public string Email { get; set; }
            public string Login { get; set; }
            public string Password { get; set; }
            public string Role { get; set; }
            public bool IsActive { get; set; }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            webView?.Dispose();
            base.OnFormClosing(e);
        }
    }
}


