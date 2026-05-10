using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using Npgsql;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public partial class FormRepairs : Form
    {
        private readonly string connectionString;

        private int currentEmployeeId;
        private string currentUserLogin;
        private string currentUserRole;
        private string currentEmployeeFullName;

        private WebView2 webView;
        private string webUIPath;

        private bool isWebViewInitialized = false;

        private Timer notificationTimer;

        private HashSet<int> notifiedTasks = new HashSet<int>();

        public FormRepairs(
            string connString,
            int userId,
            string userLogin,
            string userRole,
            int employeeId)
        {
            connectionString = connString;

            currentEmployeeId = employeeId;
            currentUserLogin = userLogin;
            currentUserRole = userRole;

            currentEmployeeFullName = GetFullName(employeeId);

            InitializeComponent();

            SetupForm();

            InitializeWebView();

            StartNotificationTimer();
        }

        private void SetupForm()
        {
            Text = "Рабочее место слесаря";

            WindowState = FormWindowState.Maximized;

            StartPosition = FormStartPosition.CenterScreen;

            Controls.Clear();
        }

        private string GetFullName(int employeeId)
        {
            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();

                    string sql = @"
                        SELECT CONCAT(
                            familiya,
                            ' ',
                            LEFT(imya,1),
                            '.',
                            LEFT(COALESCE(otchestvo,''),1),
                            '.')
                        FROM sotrudniki
                        WHERE id = @id";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", employeeId);

                        var result = cmd.ExecuteScalar();

                        return result?.ToString() ?? "Слесарь";
                    }
                }
            }
            catch
            {
                return "Слесарь";
            }
        }

        private void StartNotificationTimer()
        {
            notificationTimer = new Timer();

            notificationTimer.Interval = 30000;

            notificationTimer.Tick += async (s, e) =>
            {
                await CheckNewTasks();
                await CheckOverdueTasks();
                await LoadNotifications();
            };

            notificationTimer.Start();
        }

        private async void InitializeWebView()
        {
            try
            {
                webView = new WebView2();

                webView.Dock = DockStyle.Fill;

                Controls.Add(webView);

                string userDataFolder =
                    Path.Combine(
                        Path.GetTempPath(),
                        "WebView2Repairs_" + DateTime.Now.Ticks);

                var env =
                    await CoreWebView2Environment.CreateAsync(
                        null,
                        userDataFolder);

                await webView.EnsureCoreWebView2Async(env);

                webView.CoreWebView2.Settings.IsScriptEnabled = true;

                webView.CoreWebView2.Settings.IsWebMessageEnabled = true;

                webView.CoreWebView2.WebMessageReceived += OnWebMessageReceived;

                webUIPath =
                    @"C:\Users\Daniil\Desktop\4\kursovaya3\kursovaya\WebUI";

                string htmlPath =
                    Path.Combine(webUIPath, "repairs.html");

                if (!File.Exists(htmlPath))
                {
                    MessageBox.Show("Файл repairs.html не найден");
                    return;
                }

                webView.Source = new Uri(htmlPath);

                isWebViewInitialized = true;

                webView.CoreWebView2.NavigationCompleted += async (s, e) =>
                {
                    if (e.IsSuccess)
                    {
                        await Task.Delay(1000);

                        await SetCurrentUser();

                        await LoadTasks();

                        await LoadHistory("", "");

                        await LoadAccidentHistory();

                        await LoadStatistics();

                        await LoadNotifications();

                        await ShowTodayTasksNotification();

                        await CheckNewTasks();

                        await LoadAccidents();
                    }
                };
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private async Task SetCurrentUser()
        {
            string script =
                $"setCurrentUser(" +
                $"{currentEmployeeId}," +
                $"'{currentUserLogin}'," +
                $"'{currentUserRole}'," +
                $"'{currentEmployeeFullName}');";

            await webView.CoreWebView2.ExecuteScriptAsync(script);
        }

        private async Task ExecuteJsFunction(
            string function,
            string data = null)
        {
            if (!isWebViewInitialized) return;

            try
            {
                string js;

                if (string.IsNullOrEmpty(data))
                {
                    js =
                        $"window.receiveFromCSharp('{function}', null);";
                }
                else
                {
                    string escaped =
                        data
                        .Replace("\\", "\\\\")
                        .Replace("'", "\\'")
                        .Replace("\r", "")
                        .Replace("\n", "\\n");

                    js =
                        $"window.receiveFromCSharp('{function}', '{escaped}');";
                }

                await webView.CoreWebView2.ExecuteScriptAsync(js);
            }
            catch
            {

            }
        }

        private async Task LoadTasks()
        {
            try
            {
                var tasks = new List<object>();

                using (var conn = new NpgsqlConnection(connectionString))
                {
                    await conn.OpenAsync();

                    string sql = @"
    SELECT
        p.id,
        o.nazvanie,
        o.id,
        COALESCE(p.opisanie,''),
        TO_CHAR(p.data_nachala,'DD.MM.YYYY'),
        p.status,
        COALESCE(p.is_urgent,false),
        CASE
            WHEN p.avariya_id IS NULL THEN false
            ELSE true
        END,
        TO_CHAR(p.data_nachala,'YYYY-MM-DD""T""HH24:MI')
    FROM plan_to p
    JOIN oborudovanie o
        ON p.oborudovanie_id = o.id
    WHERE p.otvetstvenniy_id = @emp
      AND p.status <> 'Завершен'
    ORDER BY
        p.is_urgent DESC,
        p.data_nachala ASC";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@emp", currentEmployeeId);

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                tasks.Add(new
                                {
                                    id = reader.GetInt32(0),

                                    equipment_name =
        reader.GetString(1),

                                    equipment_id =
        reader.GetInt32(2),

                                    description =
        reader.GetString(3),

                                    due_date =
        reader.GetString(4),

                                    status =
        reader.GetString(5),

                                    is_urgent =
        reader.GetBoolean(6),

                                    is_accident =
        reader.GetBoolean(7),

                                    start_work_date =
        reader.IsDBNull(8)
        ? ""
        : reader.GetString(8)
                                });
                            }
                        }
                    }
                }

                string json = JsonSerializer.Serialize(tasks);

                await ExecuteJsFunction("displayTasks", json);
            }
            catch (Exception ex)
            {
                await ExecuteJsFunction(
                    "showError",
                    ex.Message);
            }
        }

        private async Task LoadHistory(
            string startDate,
            string endDate)
        {
            try
            {
                var history = new List<object>();

                using (var conn = new NpgsqlConnection(connectionString))
                {
                    await conn.OpenAsync();

                    string sql = @"
    SELECT
        TO_CHAR(r.data_okonchaniya,'DD.MM.YYYY'),
        o.nazvanie,
        COALESCE(r.opisanie,''),
        COALESCE(r.zamennaya_detal,''),
        r.data_okonchaniya
    FROM remont r
    JOIN oborudovanie o
        ON r.oborudovanie_id = o.id
    WHERE r.sotrudnik_id = @emp";

                    if (!string.IsNullOrEmpty(startDate)
                        && !string.IsNullOrEmpty(endDate))
                    {
                        sql +=
                            @" AND DATE(r.data_okonchaniya)
                               BETWEEN @start AND @end";
                    }

                    sql += " ORDER BY r.data_okonchaniya DESC";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@emp", currentEmployeeId);

                        if (!string.IsNullOrEmpty(startDate)
                            && !string.IsNullOrEmpty(endDate))
                        {
                            cmd.Parameters.AddWithValue(
                                "@start",
                                DateTime.Parse(startDate));

                            cmd.Parameters.AddWithValue(
                                "@end",
                                DateTime.Parse(endDate));
                        }

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                history.Add(new
                                {
                                    completion_date =
        reader.GetString(0),

                                    equipment =
        reader.GetString(1),

                                    description =
        reader.GetString(2),

                                    replaced_part =
        reader.GetString(3),

                                    sort_date =
        reader.GetDateTime(4)
                                });
                            }
                        }
                    }
                }

                string json = JsonSerializer.Serialize(history);

                await ExecuteJsFunction(
                    "displayHistory",
                    json);
            }
            catch (Exception ex)
            {
                await ExecuteJsFunction(
                    "showError",
                    ex.Message);
            }
        }

        private async Task LoadAccidentHistory()
        {
            try
            {
                var accidents = new List<object>();

                using (var conn = new NpgsqlConnection(connectionString))
                {
                    await conn.OpenAsync();

                    string sql = @"
                        SELECT
                            TO_CHAR(a.data_avarii,'DD.MM.YYYY HH24:MI'),
                            o.nazvanie,
                            COALESCE(a.opisanie,''),
                            a.status
                        FROM avariya a
                        JOIN oborudovanie o
                            ON a.oborudovanie_id = o.id
                        ORDER BY a.data_avarii DESC";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                accidents.Add(new
                                {
                                    date =
                                        reader.GetString(0),

                                    equipment =
                                        reader.GetString(1),

                                    description =
                                        reader.GetString(2),

                                    status =
                                        reader.GetString(3)
                                });
                            }
                        }
                    }
                }

                string json = JsonSerializer.Serialize(accidents);

                await ExecuteJsFunction(
                    "displayAccidents",
                    json);
            }
            catch (Exception ex)
            {
                await ExecuteJsFunction(
                    "showError",
                    ex.Message);
            }
        }

        private async Task LoadStatistics()
        {
            try
            {
                object stats;

                using (var conn = new NpgsqlConnection(connectionString))
                {
                    await conn.OpenAsync();

                    string sql = @"
                SELECT
                    (SELECT COUNT(*) FROM plan_to WHERE otvetstvenniy_id = @emp) AS total,
                    (SELECT COUNT(*) FROM plan_to WHERE otvetstvenniy_id = @emp AND status = 'Завершен') AS completed,
                    (SELECT COUNT(*) FROM plan_to WHERE otvetstvenniy_id = @emp AND status = 'В работе') AS inwork,
                    (SELECT COUNT(*) FROM plan_to WHERE otvetstvenniy_id = @emp AND status <> 'Завершен' AND data_nachala < CURRENT_DATE) AS overdue,
                    (SELECT COUNT(*) FROM plan_to WHERE otvetstvenniy_id = @emp AND is_urgent = true AND status <> 'Завершен') AS urgent,
                    (SELECT COUNT(*) FROM plan_to WHERE otvetstvenniy_id = @emp AND DATE(data_nachala) = CURRENT_DATE AND status <> 'Завершен') AS today";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@emp", currentEmployeeId);

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            await reader.ReadAsync();

                            int total = reader.GetInt32(0);
                            int completed = reader.GetInt32(1);
                            int inwork = reader.GetInt32(2);
                            int overdue = reader.GetInt32(3);
                            int urgent = reader.GetInt32(4);
                            int today = reader.GetInt32(5);
                            double percent = total > 0 ? Math.Round((double)completed / total * 100, 1) : 0;

                            // ВЫЗЫВАЕМ МЕТОД для расчёта среднего времени ремонта
                            double avgHours = await GetAverageRepairTime(conn);

                            stats = new
                            {
                                total = total,
                                completed = completed,
                                inwork = inwork,
                                overdue = overdue,
                                urgent = urgent,
                                today = today,
                                percent = percent,
                                avg = avgHours
                            };
                        }
                    }
                }

                string json = JsonSerializer.Serialize(stats);
                await ExecuteJsFunction("displayStats", json);
            }
            catch (Exception ex)
            {
                await ExecuteJsFunction("showError", ex.Message);
            }
        }

        private async Task LoadNotifications()
        {
            try
            {
                var notifications = new List<object>();

                using (var conn = new NpgsqlConnection(connectionString))
                {
                    await conn.OpenAsync();

                    string sql = @"
                        SELECT
                            p.id,
                            o.nazvanie,
                            COALESCE(p.opisanie,''),
                            COALESCE(p.is_urgent,false),
                            CASE
                                WHEN p.avariya_id IS NULL THEN 'ТО'
                                ELSE 'Авария'
                            END
                        FROM plan_to p
                        JOIN oborudovanie o
                            ON p.oborudovanie_id=o.id
                        WHERE p.otvetstvenniy_id=@emp
                          AND p.status<>'Завершен'
                        ORDER BY
                            p.is_urgent DESC,
                            p.id DESC
                        LIMIT 10";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@emp", currentEmployeeId);

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                notifications.Add(new
                                {
                                    id = reader.GetInt32(0),
                                    equipment = reader.GetString(1),
                                    description = reader.GetString(2),
                                    urgent = reader.GetBoolean(3),
                                    type = reader.GetString(4)
                                });
                            }
                        }
                    }
                }

                string json = JsonSerializer.Serialize(notifications);

                await ExecuteJsFunction(
                    "displayNotifications",
                    json);
            }
            catch
            {

            }
        }

        private async Task ChangeStatus(int taskId)
        {
            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    await conn.OpenAsync();

                    string checkSql =
                        @"SELECT status
                  FROM plan_to
                  WHERE id=@id";

                    string currentStatus = "";

                    using (var cmd = new NpgsqlCommand(checkSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", taskId);

                        var result = await cmd.ExecuteScalarAsync();

                        currentStatus = result?.ToString() ?? "";
                    }

                    if (currentStatus == "Зарегистрирован")
                    {
                        string sql = @"
                    UPDATE plan_to
                    SET status = 'В работе',
                        data_nachala = NOW()
                    WHERE id = @id";

                        using (var cmd = new NpgsqlCommand(sql, conn))
                        {
                            cmd.Parameters.AddWithValue("@id", taskId);

                            await cmd.ExecuteNonQueryAsync();
                        }

                        await ExecuteJsFunction(
                            "showSuccess",
                            "Задача переведена в работу");
                    }
                    else
                    {
                        await ExecuteJsFunction(
                            "showError",
                            "Задача уже находится в работе");
                    }
                }

                await LoadTasks();
            }
            catch (Exception ex)
            {
                await ExecuteJsFunction(
                    "showError",
                    ex.Message);
            }
        }

        private async Task LoadSpareParts(int equipmentId)
        {
            try
            {
                var parts = new List<object>();

                using (var conn = new NpgsqlConnection(connectionString))
                {
                    await conn.OpenAsync();

                    string sql = @"
                SELECT DISTINCT
                    sp.id_zp as id,
                    sp.naimenovanie as name
                FROM spare_parts sp
                LEFT JOIN equipment_spare_parts esp ON sp.id_zp = esp.id_zp
                WHERE sp.obshaya = true
                   OR esp.id_oborudovaniya = @equipment_id
                ORDER BY sp.naimenovanie";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@equipment_id", equipmentId);

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                parts.Add(new
                                {
                                    id = reader.GetInt32(0),
                                    name = reader.GetString(1)
                                });
                            }
                        }
                    }
                }

                string json = JsonSerializer.Serialize(parts);
                await ExecuteJsFunction("displaySpareParts", json);
            }
            catch (Exception ex)
            {
                await ExecuteJsFunction("showError", ex.Message);
            }
        }

        private async Task OpenReportModal(int taskId)
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
                    TO_CHAR(CURRENT_TIMESTAMP, 'yyyy-MM-ddTHH24:MI'),
                    TO_CHAR(CURRENT_TIMESTAMP, 'yyyy-MM-ddTHH24:MI')
                FROM plan_to p
                JOIN oborudovanie o
                    ON p.oborudovanie_id = o.id
                WHERE p.id = @id";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", taskId);

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                int equipmentId = reader.GetInt32(0);
                                string equipmentName = reader.GetString(1);
                                string startDate = reader.GetString(2);
                                string endDate = reader.GetString(3);

                                var data = new
                                {
                                    taskId = taskId,
                                    equipmentId = equipmentId,
                                    equipment = equipmentName,
                                    startDate = startDate,
                                    endDate = endDate
                                };

                                string json = JsonSerializer.Serialize(data);
                                await ExecuteJsFunction("openReportModal", json);

                                // Загружаем запчасти
                                await LoadSpareParts(equipmentId);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                await ExecuteJsFunction("showError", ex.Message);
            }
        }




        private async Task SubmitReport(
            int taskId,
            List<int> sparePartIds,
            string description,
            string startDate,
            string endDate)
        {
            try
            {
                string parts = "";

                using (var conn = new NpgsqlConnection(connectionString))
                {
                    await conn.OpenAsync();

                    foreach (int id in sparePartIds)
                    {
                        string sql =
                            "SELECT naimenovanie FROM spare_parts WHERE id_zp=@id";

                        using (var cmd = new NpgsqlCommand(sql, conn))
                        {
                            cmd.Parameters.AddWithValue("@id", id);

                            var result = await cmd.ExecuteScalarAsync();

                            if (result != null)
                            {
                                if (parts.Length > 0)
                                    parts += ", ";

                                parts += result.ToString();
                            }
                        }
                    }
                }

                using (var conn = new NpgsqlConnection(connectionString))
                {
                    await conn.OpenAsync();

                    using (var tr = await conn.BeginTransactionAsync())
                    {
                        int equipmentId = 0;

                        string sqlEq =
                            "SELECT oborudovanie_id FROM plan_to WHERE id=@id";

                        using (var cmd = new NpgsqlCommand(sqlEq, conn))
                        {
                            cmd.Parameters.AddWithValue("@id", taskId);

                            equipmentId =
                                Convert.ToInt32(
                                    await cmd.ExecuteScalarAsync());
                        }

                        string sqlUpdate =
                            @"UPDATE plan_to
                              SET status='Завершен',
                                  data_okonchaniya=@end
                              WHERE id=@id";

                        using (var cmd = new NpgsqlCommand(sqlUpdate, conn))
                        {
                            cmd.Parameters.AddWithValue("@id", taskId);

                            cmd.Parameters.AddWithValue(
                                "@end",
                                DateTime.Parse(endDate));

                            await cmd.ExecuteNonQueryAsync();
                        }

                        string sqlInsert = @"
                            INSERT INTO remont
                            (
                                oborudovanie_id,
                                sotrudnik_id,
                                data_nachala,
                                data_okonchaniya,
                                opisanie,
                                plan_id,
                                zamennaya_detal
                            )
                            VALUES
                            (
                                @eq,
                                @emp,
                                @start,
                                @end,
                                @desc,
                                @plan,
                                @parts
                            )";

                        using (var cmd = new NpgsqlCommand(sqlInsert, conn))
                        {
                            cmd.Parameters.AddWithValue("@eq", equipmentId);

                            cmd.Parameters.AddWithValue(
                                "@emp",
                                currentEmployeeId);

                            cmd.Parameters.AddWithValue(
                                "@start",
                                DateTime.Parse(startDate));

                            cmd.Parameters.AddWithValue(
                                "@end",
                                DateTime.Parse(endDate));

                            cmd.Parameters.AddWithValue(
                                "@desc",
                                description);

                            cmd.Parameters.AddWithValue(
                                "@plan",
                                taskId);

                            cmd.Parameters.AddWithValue(
                                "@parts",
                                parts);

                            await cmd.ExecuteNonQueryAsync();
                        }

                        await tr.CommitAsync();
                    }
                }

                await ExecuteJsFunction(
                    "showSuccess",
                    "Отчёт отправлен");

                await LoadTasks();

                await LoadHistory("", "");

                await LoadStatistics();

                await LoadAccidents();
            }
            catch (Exception ex)
            {
                await ExecuteJsFunction(
                    "showError",
                    ex.Message);
            }
        }

        private async Task ShowTodayTasksNotification()
        {
            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    await conn.OpenAsync();

                    string sql = @"
                        SELECT COUNT(*)
                        FROM plan_to
                        WHERE otvetstvenniy_id=@emp
                        AND DATE(data_nachala)=CURRENT_DATE
                        AND status<>'Завершен'";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@emp", currentEmployeeId);

                        int count =
                            Convert.ToInt32(
                                await cmd.ExecuteScalarAsync());

                        if (count > 0)
                        {
                            await ExecuteJsFunction(
                                "showSuccess",
                                $"Сегодня задач: {count}");
                        }
                    }
                }
            }
            catch
            {

            }
        }

        private async Task CheckOverdueTasks()
        {
            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    await conn.OpenAsync();

                    string sql = @"
                        SELECT COUNT(*)
                        FROM plan_to
                        WHERE otvetstvenniy_id=@emp
                          AND status<>'Завершен'
                          AND data_nachala<CURRENT_DATE";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@emp", currentEmployeeId);

                        int count =
                            Convert.ToInt32(
                                await cmd.ExecuteScalarAsync());

                        if (count > 0)
                        {
                            await ExecuteJsFunction(
                                "showError",
                                $"Просроченных задач: {count}");
                        }
                    }
                }
            }
            catch
            {

            }
        }

        private async Task CheckNewTasks()
        {
            if (!isWebViewInitialized || webView?.CoreWebView2 == null) return;

            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    await conn.OpenAsync();

                    // Проверяем только новые задачи ТО (не аварии)
                    string sqlTasks = @"
                SELECT 
                    p.id,
                    o.nazvanie AS equipment,
                    COALESCE(p.opisanie, '') as description,
                    TO_CHAR(p.data_nachala, 'DD.MM.YYYY') as due_date,
                    COALESCE(p.is_urgent, false) as is_urgent,
                    'ТО' as type
                FROM plan_to p
                JOIN oborudovanie o ON p.oborudovanie_id = o.id
                WHERE p.otvetstvenniy_id = @employee_id
                  AND p.status = 'Зарегистрирован'
                  AND p.avariya_id IS NULL
                  AND p.created_at > NOW() - INTERVAL '1 day'";

                    using (var cmd = new NpgsqlCommand(sqlTasks, conn))
                    {
                        cmd.Parameters.AddWithValue("@employee_id", currentEmployeeId);

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            var newTasks = new List<object>();
                            while (await reader.ReadAsync())
                            {
                                int taskId = reader.GetInt32(0);
                                if (!notifiedTasks.Contains(taskId))
                                {
                                    notifiedTasks.Add(taskId);
                                    newTasks.Add(new
                                    {
                                        id = taskId,
                                        equipment = reader.GetString(1),
                                        description = reader.IsDBNull(2) ? "" : reader.GetString(2),
                                        due_date = reader.GetString(3),
                                        is_urgent = reader.GetBoolean(4),
                                        type = reader.GetString(5)
                                    });
                                }
                            }

                            if (newTasks.Count > 0)
                            {
                                string json = JsonSerializer.Serialize(newTasks);
                                await ExecuteJsFunction("showNewTasks", json);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Просто логируем, но не показываем пользователю
                System.Diagnostics.Debug.WriteLine($"CheckNewTasks error: {ex.Message}");
            }
        }

        private async void OnWebMessageReceived(
            object sender,
            CoreWebView2WebMessageReceivedEventArgs e)
        {
            try
            {
                string message = e.TryGetWebMessageAsString();

                using (var json = JsonDocument.Parse(message))
                {
                    var root = json.RootElement;

                    string action =
                        root.GetProperty("action").GetString();

                    switch (action)
                    {
                        case "loadTasks":

                            await LoadTasks();

                            break;


                        case "loadHistory":

                            string start =
                                root.TryGetProperty(
                                    "startDate",
                                    out var s)
                                    ? s.GetString()
                                    : "";

                            string end =
                                root.TryGetProperty(
                                    "endDate",
                                    out var ed)
                                    ? ed.GetString()
                                    : "";

                            await LoadHistory(start, end);

                            break;

                        case "changeStatus":

                            int taskId =
                                root.GetProperty("taskId").GetInt32();

                            await ChangeStatus(taskId);

                            break;

                        case "openReport":

                            int openTaskId =
                                root.GetProperty("taskId").GetInt32();

                            await OpenReportModal(openTaskId);

                            break;

                        case "loadSpareParts":
                            int equipId = root.GetProperty("equipmentId").GetInt32();
                            await LoadSpareParts(equipId);
                            break;

                        case "submitReport":

                            int reportTaskId =
                                root.GetProperty("taskId").GetInt32();

                            List<int> parts = new List<int>();

                            if (root.TryGetProperty(
                                "sparePartIds",
                                out var arr))
                            {
                                foreach (var item in arr.EnumerateArray())
                                {
                                    parts.Add(item.GetInt32());
                                }
                            }

                            string desc =
                                root.GetProperty("description").GetString();

                            string startDate =
                                root.GetProperty("startDate").GetString();

                            string endDate =
                                root.GetProperty("endDate").GetString();

                            await SubmitReport(
                                reportTaskId,
                                parts,
                                desc,
                                startDate,
                                endDate);

                            break;

                        case "logout":

                            Invoke(new Action(() =>
                            {
                                notificationTimer?.Stop();

                                LoginForm form = new LoginForm();

                                form.Show();

                                Close();
                            }));

                            break;

                    }
                }
            }
            catch (Exception ex)
            {
                await ExecuteJsFunction(
                    "showError",
                    ex.Message);
            }
        }

        private async Task SendNotification(string title, string message, string type)
        {
            var notification = new { title = title, message = message, type = type };
            string json = JsonSerializer.Serialize(notification);
            await ExecuteJsFunction("showCustomNotification", json);
        }

        private async Task GetTaskDetails(int taskId)
        {
            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    await conn.OpenAsync();

                    string sql = @"
                SELECT 
                    p.id,
                    o.nazvanie,
                    COALESCE(p.opisanie, ''),
                    TO_CHAR(p.data_nachala, 'DD.MM.YYYY'),
                    p.status
                FROM plan_to p
                JOIN oborudovanie o ON p.oborudovanie_id = o.id
                WHERE p.id = @id";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", taskId);

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                var task = new
                                {
                                    id = reader.GetInt32(0),
                                    equipment = reader.GetString(1),
                                    description = reader.GetString(2),
                                    date = reader.GetString(3),
                                    status = reader.GetString(4)
                                };

                                string json = JsonSerializer.Serialize(task);
                                await ExecuteJsFunction("showTaskDetails", json);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                await ExecuteJsFunction("showError", ex.Message);
            }
        }

        private async Task LoadAccidents()
        {
            try
            {
                var accidents = new List<object>();

                using (var conn = new NpgsqlConnection(connectionString))
                {
                    await conn.OpenAsync();

                    string sql = @"
                SELECT
                    TO_CHAR(a.data_avarii,'DD.MM.YYYY'),
                    o.nazvanie,
                    COALESCE(a.opisanie,''),
                    a.status,
                    a.data_avarii
                FROM avariya a
                JOIN oborudovanie o
                    ON a.oborudovanie_id = o.id
                JOIN plan_to p
                    ON p.avariya_id = a.id
                WHERE p.otvetstvenniy_id=@emp
                ORDER BY a.data_avarii DESC";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@emp", currentEmployeeId);

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                accidents.Add(new
                                {
                                    date =
                                        reader.GetString(0),

                                    equipment =
                                        reader.GetString(1),

                                    description =
                                        reader.GetString(2),

                                    status =
                                        reader.GetString(3),

                                    sort_date =
                                        reader.GetDateTime(4)
                                });
                            }
                        }
                    }
                }

                string json = JsonSerializer.Serialize(accidents);

                await ExecuteJsFunction(
                    "displayAccidents",
                    json);
            }
            catch (Exception ex)
            {
                await ExecuteJsFunction(
                    "showError",
                    ex.Message);
            }
        }


        private async Task<double> GetAverageRepairTime(NpgsqlConnection conn)
        {
            try
            {
                string sql = @"
            SELECT 
                data_nachala,
                data_okonchaniya
            FROM plan_to
            WHERE otvetstvenniy_id = @emp 
              AND status = 'Завершен'
              AND data_okonchaniya IS NOT NULL 
              AND data_nachala IS NOT NULL";

                using (var cmd = new NpgsqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@emp", currentEmployeeId);

                    var durations = new List<double>();

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            DateTime start = reader.GetDateTime(0);
                            DateTime end = reader.GetDateTime(1);

                            // Количество рабочих дней между датами
                            double workDays = CalculateWorkingDays(start, end);

                            // Переводим рабочие дни в часы (8-часовой рабочий день)
                            double workHours = workDays * 8;
                            durations.Add(workHours);
                        }
                    }

                    if (durations.Count > 0)
                    {
                        return durations.Average();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetAverageRepairTime error: {ex.Message}");
            }

            return 0;
        }

        /// <summary>
        /// Подсчёт количества рабочих дней между двумя датами (исключая выходные)
        /// </summary>
        private double CalculateWorkingDays(DateTime startDate, DateTime endDate)
        {
            if (startDate > endDate)
            {
                var temp = startDate;
                startDate = endDate;
                endDate = temp;
            }

            int workingDays = 0;
            DateTime current = startDate.Date;
            DateTime end = endDate.Date;

            while (current <= end)
            {
                // Понедельник = 1, Воскресенье = 7
                if (current.DayOfWeek != DayOfWeek.Saturday && current.DayOfWeek != DayOfWeek.Sunday)
                {
                    workingDays++;
                }
                current = current.AddDays(1);
            }

            return workingDays;
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