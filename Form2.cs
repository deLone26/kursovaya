using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Security.Cryptography;
using Microsoft.Web.WebView2.WinForms;
using Microsoft.Web.WebView2.Core;
using Npgsql;
using System.Text.Json.Serialization;

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
        private string currentUserRole;
        private int currentUserId;

        public Form2(string userRole = "admin", int userId = 0)
        {
            InitializeComponent();
            currentUserRole = userRole;
            currentUserId = userId;
            webUIPath = @"C:\Users\Daniil\Desktop\4\kursovaya3\kursovaya\WebUI";
            this.Text = "Управление сотрудниками";
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

                string userDataFolder = Path.Combine(Path.GetTempPath(), "WebView2_Employees_" + this.GetHashCode());
                var env = await CoreWebView2Environment.CreateAsync(null, userDataFolder);
                await webView.EnsureCoreWebView2Async(env);

                webView.CoreWebView2.Settings.IsScriptEnabled = true;
                webView.CoreWebView2.Settings.IsWebMessageEnabled = true;
                webView.CoreWebView2.Settings.AreDefaultScriptDialogsEnabled = true;
                webView.CoreWebView2.WebMessageReceived += OnWebMessageReceived;

                string htmlPath = Path.Combine(webUIPath, "employees.html");
                System.Diagnostics.Debug.WriteLine($"Загрузка HTML из: {htmlPath}");

                if (File.Exists(htmlPath))
                {
                    webView.CoreWebView2.Navigate($"file:///{htmlPath.Replace('\\', '/')}");
                    isWebViewInitialized = true;

                    webView.CoreWebView2.NavigationCompleted += async (s, e) =>
                    {
                        if (e.IsSuccess)
                        {
                            System.Diagnostics.Debug.WriteLine("Навигация завершена");
                            await Task.Delay(500);
                            await SetUserRole();
                            await LoadEmployees();
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

        private async Task SetUserRole()
        {
            if (webView?.CoreWebView2 != null && isWebViewInitialized)
            {
                // Передаем роль и ID текущего пользователя
                string script = $"if(typeof setCurrentUserRole === 'function') setCurrentUserRole('{currentUserRole}', {currentUserId});";
                System.Diagnostics.Debug.WriteLine($"Отправляем роль: {script}");
                await webView.CoreWebView2.ExecuteScriptAsync(script);
            }
        }

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

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            int sotrudnikId = Convert.ToInt32(reader["id"]);
                            string login = reader["login"]?.ToString() ?? "";
                            string role = "operator";

                            if (!string.IsNullOrEmpty(login))
                            {
                                role = await GetUserRole(login);
                            }

                            employees.Add(new
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
                            });
                        }
                    }
                }

                System.Diagnostics.Debug.WriteLine($"Всего загружено сотрудников: {employees.Count}");

                var result = new { employees = employees };
                string json = JsonSerializer.Serialize(result, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
                await ExecuteJsFunction("updateEmployees", json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки: {ex.Message}");
                await ExecuteJsFunction("showMessage", $"Ошибка загрузки: {ex.Message}", "error");
            }
        }

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
        private async Task<string> GetUserRole(string login)
        {
            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    await conn.OpenAsync();
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
                            if (result != null) return role;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка определения роли: {ex.Message}");
            }
            return "operator";
        }

        private async Task ExecuteJsFunction(string function, string data = null, string type = null)
        {
            if (webView?.CoreWebView2 != null && isWebViewInitialized)
            {
                try
                {
                    string js;
                    if (string.IsNullOrEmpty(data))
                        js = $"if(window.{function}) window.{function}();";
                    else if (type != null)
                        js = $"if(window.{function}) window.{function}('{data}', '{type}');";
                    else
                        js = $"if(window.{function}) window.{function}({data});";

                    await webView.CoreWebView2.ExecuteScriptAsync(js);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Ошибка выполнения скрипта: {ex.Message}");
                }
            }
        }

        private string GetDolzhnostByRole(string role)
        {
            switch (role)
            {
                case "admin":
                    return "Администратор";
                case "boss":
                    return "Начальник котельной";
                case "slesar":
                    return "Слесарь";
                case "operator":
                    return "Оператор";
                default:
                    return "Сотрудник";
            }
        }
        private async void OnWebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            string message = e.TryGetWebMessageAsString();
            System.Diagnostics.Debug.WriteLine($"Получено от JS: {message}");

            try
            {
                using (JsonDocument doc = JsonDocument.Parse(message))
                {
                    JsonElement root = doc.RootElement;
                    string action = root.GetProperty("action").GetString();

                    switch (action)
                    {
                        case "loadEmployees":
                            await LoadEmployees();
                            break;
                        case "addEmployee":
                            // Только администратор может добавлять
                            if (currentUserRole != "admin")
                            {
                                await ExecuteJsFunction("showMessage", "У вас нет прав на добавление сотрудников", "error");
                                break;
                            }
                            var newEmployee = JsonSerializer.Deserialize<EmployeeData>(root.GetProperty("data").GetRawText());
                            await AddEmployee(newEmployee);
                            break;
                        case "updateEmployee":
                            // Начальник может обновлять только телефон и email (не роль и не логин)
                            var updateData = JsonSerializer.Deserialize<EmployeeData>(root.GetProperty("data").GetRawText());

                            // Проверка: нельзя менять роль на admin или boss
                            if (updateData.Role == "admin" || updateData.Role == "boss")
                            {
                                // Проверяем, не пытается ли кто-то назначить админа или начальника
                                if (currentUserRole != "admin")
                                {
                                    await ExecuteJsFunction("showMessage", "Назначение роли Администратор или Начальник запрещено!", "error");
                                    break;
                                }
                            }

                            // Проверка смены роли существующего пользователя
                            if (updateData.Id > 0)
                            {
                                string currentRole = await GetUserRoleByEmployeeId(updateData.Id);
                                if ((currentRole == "admin" || currentRole == "boss") && currentUserRole != "admin")
                                {
                                    await ExecuteJsFunction("showMessage", "Нельзя изменять данные Администратора или Начальника", "error");
                                    break;
                                }
                            }

                            await UpdateEmployee(updateData);
                            break;
                        case "deleteEmployee":
                            if (currentUserRole != "admin")
                            {
                                await ExecuteJsFunction("showMessage", "У вас нет прав на удаление сотрудников", "error");
                                break;
                            }
                            int deleteId = root.GetProperty("id").GetInt32();
                            await DeleteEmployee(deleteId);
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка: {ex.Message}");
                await ExecuteJsFunction("showMessage", ex.Message, "error");
            }
        }

        private async Task<string> GetUserRoleByEmployeeId(int employeeId)
        {
            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    await conn.OpenAsync();
                    string sql = "SELECT login FROM users WHERE sotrudnik_id = @id";
                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", employeeId);
                        string login = await cmd.ExecuteScalarAsync() as string;
                        if (!string.IsNullOrEmpty(login))
                        {
                            return await GetUserRole(login);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка: {ex.Message}");
            }
            return "operator";
        }

        private async Task AddEmployee(EmployeeData employee)
        {
            try
            {
                // Запрещаем создание админов и начальников для обычных пользователей
                if ((employee.Role == "admin" || employee.Role == "boss") && currentUserRole != "admin")
                {
                    await ExecuteJsFunction("showMessage", "Создание пользователей с ролью Администратор или Начальник запрещено!", "error");
                    return;
                }

                // Проверка на существование логина
                using (var checkConn = new NpgsqlConnection(connectionString))
                {
                    await checkConn.OpenAsync();
                    string checkSql = "SELECT COUNT(*) FROM users WHERE login = @login";
                    using (var checkCmd = new NpgsqlCommand(checkSql, checkConn))
                    {
                        checkCmd.Parameters.AddWithValue("@login", employee.Login);
                        int exists = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());
                        if (exists > 0)
                        {
                            await ExecuteJsFunction("showMessage", $"Логин '{employee.Login}' уже существует!", "error");
                            return;
                        }
                    }
                }

                // Автоматически определяем должность по роли
                string dolzhnost = GetDolzhnostByRole(employee.Role);
                string passwordHash = HashPassword(employee.Password);

                int sotrudnikId;

                using (var conn = new NpgsqlConnection(connectionString))
                {
                    await conn.OpenAsync();
                    using (var transaction = await conn.BeginTransactionAsync())
                    {
                        string sqlSotrudnik = @"
                    INSERT INTO sotrudniki (familiya, imya, otchestvo, dolzhnost, telefon, email)
                    VALUES (@familiya, @imya, @otchestvo, @dolzhnost, @telefon, @email)
                    RETURNING id";

                        using (var cmd = new NpgsqlCommand(sqlSotrudnik, conn))
                        {
                            cmd.Parameters.AddWithValue("@familiya", employee.Familiya ?? "");
                            cmd.Parameters.AddWithValue("@imya", employee.Imya ?? "");
                            cmd.Parameters.AddWithValue("@otchestvo", employee.Otchestvo ?? "");
                            cmd.Parameters.AddWithValue("@dolzhnost", dolzhnost);
                            cmd.Parameters.AddWithValue("@telefon", employee.Telefon ?? "");
                            cmd.Parameters.AddWithValue("@email", employee.Email ?? "");

                            sotrudnikId = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                        }

                        string sqlUser = @"
                    INSERT INTO users (login, created_at, is_active, sotrudnik_id, password_hash, role)
                    VALUES (@login, @created_at, @is_active, @sotrudnik_id, @password_hash, @role)";

                        using (var cmd = new NpgsqlCommand(sqlUser, conn))
                        {
                            cmd.Parameters.AddWithValue("@login", employee.Login);
                            cmd.Parameters.AddWithValue("@created_at", DateTime.Now);
                            cmd.Parameters.AddWithValue("@is_active", true);
                            cmd.Parameters.AddWithValue("@sotrudnik_id", sotrudnikId);
                            cmd.Parameters.AddWithValue("@password_hash", passwordHash);
                            cmd.Parameters.AddWithValue("@role", $"app_{employee.Role}");

                            await cmd.ExecuteNonQueryAsync();
                        }

                        await transaction.CommitAsync();
                    }
                }

                // Создание пользователя в PostgreSQL
                try
                {
                    await CreatePostgresUser(employee.Login, employee.Password, employee.Role);
                }
                catch (Exception pgEx)
                {
                    await ExecuteJsFunction("showMessage", $"Сотрудник добавлен, но ошибка создания пользователя БД: {pgEx.Message}", "warning");
                    await LoadEmployees();
                    return;
                }

                await ExecuteJsFunction("showMessage", "Сотрудник успешно добавлен!", "success");
                await LoadEmployees();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка добавления: {ex.Message}");
                await ExecuteJsFunction("showMessage", $"Ошибка добавления: {ex.Message}", "error");
            }
        }

        private async Task UpdateEmployee(EmployeeData employee)
        {
            try
            {
                if (employee == null || employee.Id <= 0)
                {
                    await ExecuteJsFunction("showMessage", "Неверные данные сотрудника", "error");
                    return;
                }

                // Автоматически определяем должность по роли
                string dolzhnost = GetDolzhnostByRole(employee.Role);

                using (var conn = new NpgsqlConnection(connectionString))
                {
                    await conn.OpenAsync();
                    using (var transaction = await conn.BeginTransactionAsync())
                    {
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
                            cmd.Parameters.AddWithValue("@familiya", employee.Familiya ?? "");
                            cmd.Parameters.AddWithValue("@imya", employee.Imya ?? "");
                            cmd.Parameters.AddWithValue("@otchestvo", employee.Otchestvo ?? "");
                            cmd.Parameters.AddWithValue("@dolzhnost", dolzhnost);
                            cmd.Parameters.AddWithValue("@telefon", employee.Telefon ?? "");
                            cmd.Parameters.AddWithValue("@email", employee.Email ?? "");

                            await cmd.ExecuteNonQueryAsync();
                        }

                        // Обновляем пользователя
                        if (!string.IsNullOrEmpty(employee.Password))
                        {
                            string passwordHash = HashPassword(employee.Password);
                            string sqlUser = "UPDATE users SET login = @login, password_hash = @password_hash, role = @role WHERE sotrudnik_id = @id";
                            using (var cmd = new NpgsqlCommand(sqlUser, conn))
                            {
                                cmd.Parameters.AddWithValue("@id", employee.Id);
                                cmd.Parameters.AddWithValue("@login", employee.Login ?? "");
                                cmd.Parameters.AddWithValue("@password_hash", passwordHash);
                                cmd.Parameters.AddWithValue("@role", $"app_{employee.Role}");
                                await cmd.ExecuteNonQueryAsync();
                            }
                        }
                        else
                        {
                            string sqlUser = "UPDATE users SET login = @login, role = @role WHERE sotrudnik_id = @id";
                            using (var cmd = new NpgsqlCommand(sqlUser, conn))
                            {
                                cmd.Parameters.AddWithValue("@id", employee.Id);
                                cmd.Parameters.AddWithValue("@login", employee.Login ?? "");
                                cmd.Parameters.AddWithValue("@role", $"app_{employee.Role}");
                                await cmd.ExecuteNonQueryAsync();
                            }
                        }

                        await transaction.CommitAsync();
                    }
                }

                // Обновляем роль в PostgreSQL
                if (!string.IsNullOrEmpty(employee.Password))
                    await UpdatePostgresUserRole(employee.Login, employee.Role, employee.Password);
                else
                    await UpdatePostgresUserRole(employee.Login, employee.Role);

                await ExecuteJsFunction("showMessage", "Данные сотрудника обновлены!", "success");
                await LoadEmployees();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка обновления: {ex.Message}");
                await ExecuteJsFunction("showMessage", $"Ошибка обновления: {ex.Message}", "error");
            }
        }

        private async Task DeleteEmployee(int id)
        {
            try
            {
                // Запрещаем удаление админов и начальников
                string userRole = await GetUserRoleByEmployeeId(id);
                if (userRole == "admin" || userRole == "boss")
                {
                    await ExecuteJsFunction("showMessage", "Нельзя удалить Администратора или Начальника!", "error");
                    return;
                }

                string login = "";
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    await conn.OpenAsync();
                    string sql = "SELECT login FROM users WHERE sotrudnik_id = @id";
                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        var result = await cmd.ExecuteScalarAsync();
                        if (result != null) login = result.ToString();
                    }
                }

                using (var conn = new NpgsqlConnection(connectionString))
                {
                    await conn.OpenAsync();
                    using (var transaction = await conn.BeginTransactionAsync())
                    {
                        string sqlUser = "DELETE FROM users WHERE sotrudnik_id = @id";
                        using (var cmd = new NpgsqlCommand(sqlUser, conn))
                        {
                            cmd.Parameters.AddWithValue("@id", id);
                            await cmd.ExecuteNonQueryAsync();
                        }

                        string sqlSotrudnik = "DELETE FROM sotrudniki WHERE id = @id";
                        using (var cmd = new NpgsqlCommand(sqlSotrudnik, conn))
                        {
                            cmd.Parameters.AddWithValue("@id", id);
                            await cmd.ExecuteNonQueryAsync();
                        }

                        await transaction.CommitAsync();
                    }
                }

                if (!string.IsNullOrEmpty(login))
                {
                    try
                    {
                        using (var adminConn = new NpgsqlConnection(adminConnectionString))
                        {
                            await adminConn.OpenAsync();
                            string[] roles = { "admin", "boss", "slesar", "operator" };
                            foreach (string role in roles)
                            {
                                try
                                {
                                    using (var cmd = new NpgsqlCommand($"REVOKE app_{role} FROM \"{login}\"", adminConn))
                                    { await cmd.ExecuteNonQueryAsync(); }
                                }
                                catch { }
                            }
                            using (var cmd = new NpgsqlCommand($"DROP USER IF EXISTS \"{login}\"", adminConn))
                            { await cmd.ExecuteNonQueryAsync(); }
                        }
                    }
                    catch (Exception pgEx)
                    {
                        await ExecuteJsFunction("showMessage", $"Сотрудник удален, но ошибка удаления пользователя БД: {pgEx.Message}", "warning");
                        return;
                    }
                }

                await ExecuteJsFunction("showMessage", "Сотрудник удален!", "success");
                await LoadEmployees();
            }
            catch (Exception ex)
            {
                await ExecuteJsFunction("showMessage", $"Ошибка удаления: {ex.Message}", "error");
            }
        }

        private async Task CreatePostgresUser(string login, string password, string role)
        {
            using (var adminConn = new NpgsqlConnection(adminConnectionString))
            {
                await adminConn.OpenAsync();

                using (var cmdCheck = new NpgsqlCommand("SELECT 1 FROM pg_roles WHERE rolname = @login", adminConn))
                {
                    cmdCheck.Parameters.AddWithValue("@login", login);
                    var exists = await cmdCheck.ExecuteScalarAsync();

                    if (exists == null)
                    {
                        using (var cmdCreate = new NpgsqlCommand($"CREATE USER \"{login}\" WITH PASSWORD @password", adminConn))
                        {
                            cmdCreate.Parameters.AddWithValue("@password", password);
                            await cmdCreate.ExecuteNonQueryAsync();
                        }
                    }
                }

                string[] roles = { "admin", "boss", "slesar", "operator" };
                foreach (string r in roles)
                {
                    try
                    {
                        using (var cmd = new NpgsqlCommand($"REVOKE app_{r} FROM \"{login}\"", adminConn))
                        { await cmd.ExecuteNonQueryAsync(); }
                    }
                    catch { }
                }

                using (var cmdGrant = new NpgsqlCommand($"GRANT app_{role} TO \"{login}\"", adminConn))
                { await cmdGrant.ExecuteNonQueryAsync(); }
            }
        }

        private async Task UpdatePostgresUserRole(string login, string role, string password = null)
        {
            using (var adminConn = new NpgsqlConnection(adminConnectionString))
            {
                await adminConn.OpenAsync();

                if (!string.IsNullOrEmpty(password))
                {
                    using (var cmdPass = new NpgsqlCommand($"ALTER USER \"{login}\" WITH PASSWORD @password", adminConn))
                    {
                        cmdPass.Parameters.AddWithValue("@password", password);
                        await cmdPass.ExecuteNonQueryAsync();
                    }
                }

                string[] roles = { "admin", "boss", "slesar", "operator" };
                foreach (string r in roles)
                {
                    try
                    {
                        using (var cmd = new NpgsqlCommand($"REVOKE app_{r} FROM \"{login}\"", adminConn))
                        { await cmd.ExecuteNonQueryAsync(); }
                    }
                    catch { }
                }

                using (var cmdGrant = new NpgsqlCommand($"GRANT app_{role} TO \"{login}\"", adminConn))
                { await cmdGrant.ExecuteNonQueryAsync(); }
            }
        }

        public class EmployeeData
        {
            [JsonPropertyName("id")]
            public int Id { get; set; }

            [JsonPropertyName("familiya")]
            public string Familiya { get; set; }

            [JsonPropertyName("imya")]
            public string Imya { get; set; }

            [JsonPropertyName("otchestvo")]
            public string Otchestvo { get; set; }

            [JsonPropertyName("dolzhnost")]
            public string Dolzhnost { get; set; }

            [JsonPropertyName("telefon")]
            public string Telefon { get; set; }

            [JsonPropertyName("email")]
            public string Email { get; set; }

            [JsonPropertyName("login")]
            public string Login { get; set; }

            [JsonPropertyName("password")]
            public string Password { get; set; }

            [JsonPropertyName("role")]
            public string Role { get; set; }

            [JsonPropertyName("isActive")]
            public bool IsActive { get; set; }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            webView?.Dispose();
            base.OnFormClosing(e);
        }
    }
}