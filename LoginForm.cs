using System;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Npgsql;
using System.Security.Cryptography;

namespace WindowsFormsApp1
{
    public partial class LoginForm : Form
    {
        private readonly string connectionString =
            "Host=localhost;Port=5432;Database=boiler_system;Username=postgres;Password=43898362Dd+-;";

        private Point lastPoint;

        public LoginForm()
        {
            InitializeComponent();
            this.passField.AutoSize = false;
            this.passField.Size = new Size(this.passField.Width, 39);
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

        // ===== МЕТОДЫ ИЗ ДИЗАЙНЕРА =====

        private void label1_Click(object sender, EventArgs e) { }

        private void pictureBox1_Click(object sender, EventArgs e) { }

        private void closeButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void closeButton_MouseEnter(object sender, EventArgs e)
        {
            closeButton.ForeColor = Color.Red;
        }

        private void closeButton_MouseLeave(object sender, EventArgs e)
        {
            closeButton.ForeColor = Color.White;
        }

        private void panel1_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                this.Left += e.X - lastPoint.X;
                this.Top += e.Y - lastPoint.Y;
            }
        }

        private void panel1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                lastPoint = new Point(e.X, e.Y);
            }
        }

        private void label1_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                this.Left += e.X - lastPoint.X;
                this.Top += e.Y - lastPoint.Y;
            }
        }

        private void label1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                lastPoint = new Point(e.X, e.Y);
            }
        }

        private void label2_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                lastPoint = new Point(e.X, e.Y);
            }
        }

        // ===== ГЛАВНЫЙ МЕТОД АВТОРИЗАЦИИ =====

        private void button1_Click(object sender, EventArgs e)
        {
            string login = loginField.Text.Trim();
            string password = passField.Text.Trim();

            if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Введите логин и пароль!");
                return;
            }

            string userConnString =
                $"Host=localhost;Port=5432;Database=boiler_system;Username={login};Password={password};";

            try
            {
                using (var conn = new NpgsqlConnection(userConnString))
                {
                    conn.Open();

                    string role = GetUserRole(login);
                    int employeeId = GetEmployeeId(login);

                    if (string.IsNullOrEmpty(role))
                    {
                        MessageBox.Show("У пользователя нет назначенной роли!");
                        return;
                    }

                    // Передаем оба параметра: connString и employeeId
                    OpenFormByRole(role, userConnString, employeeId);
                    this.Hide();
                }
            }
            catch (NpgsqlException)
            {
                MessageBox.Show("Неверный логин или пароль!");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка: " + ex.Message);
            }
        }

        // ===== ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ =====

        private string GetUserRole(string login)
        {
            using (var conn = new NpgsqlConnection(connectionString))
            {
                conn.Open();
                string sql = @"
                    SELECT g.rolname
                    FROM pg_roles u
                    JOIN pg_auth_members m ON u.oid = m.member
                    JOIN pg_roles g ON m.roleid = g.oid
                    WHERE u.rolname = @login AND g.rolname LIKE 'app_%'";

                using (var cmd = new NpgsqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@login", login);
                    var result = cmd.ExecuteScalar();
                    return result?.ToString() ?? "";
                }
            }
        }

        private int GetEmployeeId(string login)
        {
            using (var conn = new NpgsqlConnection(connectionString))
            {
                conn.Open();
                string sql = "SELECT sotrudnik_id FROM users WHERE login = @login";
                using (var cmd = new NpgsqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@login", login);
                    var result = cmd.ExecuteScalar();
                    return result != null && result != DBNull.Value ? Convert.ToInt32(result) : -1;
                }
            }
        }

        // ===== МЕТОД ОТКРЫТИЯ ФОРМ ПО РОЛЯМ =====

        private void OpenFormByRole(string role, string connString, int employeeId)
        {
            Form form = null;

            switch (role)
            {
                case "app_admin":
                    MessageBox.Show("Вы вошли как Администратор");
                    //  form = new FormAdmin(connString, employeeId);
                    break;

                case "app_boss":
                    // ВАЖНО: передаем оба параметра!
                    form = new MainForm(connString, employeeId);
                    break;

                case "app_operator":
                    MessageBox.Show("Вы вошли как Оператор");
                    //  form = new FormOperator(connString, employeeId);
                    break;

                case "app_slesar":
                    MessageBox.Show("Вы вошли как Слесарь");
                    //    form = new Form2(connString, employeeId);
                    break;

                default:
                    MessageBox.Show($"Неизвестная роль: {role}");
                    return;
            }

            if (form != null)
            {
                form.Show();
            }
        }
    }
}
