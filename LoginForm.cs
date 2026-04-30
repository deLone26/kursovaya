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
            "Host=localhost;Port=5432;Database=boiler_system;Username=postgres;Password=43898362Dd+-;Include Error Detail=true";

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

        // ===== ПЕРЕМЕЩЕНИЕ ОКНА =====

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

            string passwordHash = HashPassword(password);
            int employeeId = -1;
            string role = "";
            string dolzhnost = "";

            using (var conn = new NpgsqlConnection(connectionString))
            {
                conn.Open();

                string sql = @"
                    SELECT u.sotrudnik_id, u.role, s.dolzhnost 
                    FROM users u
                    JOIN sotrudniki s ON u.sotrudnik_id = s.id
                    WHERE u.login = @login 
                      AND u.password_hash = @hash 
                      AND u.is_active = true";

                using (var cmd = new NpgsqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@login", login);
                    cmd.Parameters.AddWithValue("@hash", passwordHash);

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            employeeId = reader.GetInt32(0);
                            role = reader.GetString(1);
                            dolzhnost = reader.GetString(2);
                        }
                    }
                }
            }

            if (employeeId == -1)
            {
                MessageBox.Show("Неверный логин или пароль, либо учетная запись неактивна!");
                return;
            }

            if (string.IsNullOrEmpty(role))
            {
                MessageBox.Show("У пользователя нет назначенной роли!");
                return;
            }

            OpenFormByRole(role, employeeId, dolzhnost, login);
            this.Hide();
        }

        private void OpenFormByRole(string role, int employeeId, string dolzhnost, string login)
        {
            Form form = null;

            switch (role)
            {
                case "app_admin":
                    MessageBox.Show($"Вы вошли как Администратор ({dolzhnost})");
                    form = new MainForm(connectionString, employeeId);
                    break;

                case "app_boss":
                    MessageBox.Show($"Вы вошли как Начальник котельной ({dolzhnost})");
                    form = new MainForm(connectionString, employeeId);
                    break;

                case "app_operator":
                    MessageBox.Show($"Вы вошли как Оператор ({dolzhnost})");
                    form = new FormAccidents(employeeId, login, role);
                    break;

                case "app_slesar":
                    MessageBox.Show($"Вы вошли как Слесарь ({dolzhnost})");
                    // ПЕРЕДАЕМ ВСЕ 5 ПАРАМЕТРОВ
                    form = new FormRepairs(connectionString, employeeId, login, role, employeeId);
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

        private void label1_Click(object sender, EventArgs e) { }
        private void pictureBox1_Click(object sender, EventArgs e) { }
    }
}
