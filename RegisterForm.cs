using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Npgsql;
using System.Security.Cryptography;
using System.Text;



namespace WindowsFormsApp1
{
    public partial class RegisterForm : Form
    {
        public RegisterForm()
        {
            InitializeComponent();

            userNameField.Text = "Введите имя";
            userNameField.ForeColor = Color.Gray;

            userSurnameField.Text = "Введите фамилию";
            userSurnameField.ForeColor = Color.Gray;

            userOtchestvo.Text = "Введите отчество";
            userOtchestvo.ForeColor = Color.Gray;
        }

        Point lastPoint;

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

        private readonly string connectionString = "Host=localhost;Port=5432;Database=boiler_system;Username=postgres;Password=43898362Dd+-;";


        private void panel1_MouseMove_1(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                this.Left += e.X - lastPoint.X;
                this.Top += e.Y - lastPoint.Y;
            }
        }

        private void panel1_MouseDown(object sender, MouseEventArgs e)
        {
            lastPoint = new Point(e.X, e.Y);
        }

        private void closeButton_MouseEnter(object sender, EventArgs e)
        {
            closeButton.ForeColor = Color.Red;
        }

        private void closeButton_MouseLeave(object sender, EventArgs e)
        {
            closeButton.ForeColor = Color.White;
        }

        private void closeButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void label1_MouseDown(object sender, MouseEventArgs e)
        {
            lastPoint = new Point();
        }



        //должно быть не лейбл а панель, лейбл просто растянут на всю площадь панели

        private void label1_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                this.Left += e.X;
                this.Top += e.Y;
            }
        }

        private void RegistrButton1_Click(object sender, EventArgs e)
        {
            string name = userNameField.Text;
            string surname = userSurnameField.Text;
            string otchestvo = userOtchestvo.Text;
            string login = loginField.Text;
            string password = loginField.Text;

            if (string.IsNullOrWhiteSpace(name) ||
                string.IsNullOrWhiteSpace(surname) ||
                string.IsNullOrWhiteSpace(login) ||
                string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Заполните все обязательные поля!");
                return;
            }

            string checkUser = "SELECT COUNT(*) FROM users WHERE login=@login";


            string passwordHash = HashPassword(password);

            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();

                    using (var transaction = conn.BeginTransaction())
                    {
                        try
                        {
                            // 1️⃣ Добавляем сотрудника
                            string sqlSotrudnik = @"INSERT INTO sotrudniki
                        (familiya, imya, otchestvo)
                        VALUES (@familiya, @imya, @otchestvo)
                        RETURNING id";

                            int sotrudnikId;

                            using (var cmd = new NpgsqlCommand(sqlSotrudnik, conn))
                            {
                                cmd.Parameters.AddWithValue("@familiya", surname);
                                cmd.Parameters.AddWithValue("@imya", name);
                                cmd.Parameters.AddWithValue("@otchestvo", otchestvo);

                                sotrudnikId = Convert.ToInt32(cmd.ExecuteScalar());
                            }

                            // 2️⃣ Добавляем пользователя
                            string sqlUser = @"INSERT INTO users
                        (login, password_hash, role, sotrudnik_id)
                        VALUES (@login, @password_hash, @role, @sotrudnik_id)";

                            using (var cmd = new NpgsqlCommand(sqlUser, conn))
                            {
                                cmd.Parameters.AddWithValue("@login", login);
                                cmd.Parameters.AddWithValue("@password_hash", passwordHash);
                                cmd.Parameters.AddWithValue("@role", "user");
                                cmd.Parameters.AddWithValue("@sotrudnik_id", sotrudnikId);

                                cmd.ExecuteNonQuery();
                            }

                            transaction.Commit();
                        }
                        catch
                        {
                            transaction.Rollback();
                            throw;
                        }
                    }
                }

                MessageBox.Show("Регистрация успешна!");
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка регистрации: " + ex.Message);
            }
        }

        private void loginField_TextChanged(object sender, EventArgs e)
        {

        }

        // Настройка цвета текста
        private void userNameField_Enter(object sender, EventArgs e)
        {
            if (userNameField.Text == "Введите имя")
            {
                userNameField.Text = "";
                userNameField.ForeColor = Color.Black;
            }
        }

        private void userNameField_Leave(object sender, EventArgs e)
        {
            if (userNameField.Text == "")
            {
                userNameField.Text = "Введите имя";
                userNameField.ForeColor = Color.Gray;
            }
        }

        private void userSurnameField_Enter(object sender, EventArgs e)
        {
            if (userSurnameField.Text == "Введите фамилию")
            {
                userSurnameField.Text = "";
                userSurnameField.ForeColor = Color.Black;
            }
        }

        private void userSurnameField_Leave(object sender, EventArgs e)
        {
            if (userSurnameField.Text == "")
            {
                userSurnameField.Text = "Введите фамилию";
                userSurnameField.ForeColor = Color.Gray;
            }
        }
        
        private void userOtchestvo_Enter(object sender, EventArgs e)
        {
            if(userOtchestvo.Text == "Введите отчество")
            {
                userOtchestvo.Text = "";
                userOtchestvo.ForeColor = Color.Black;
            }
        }
        
        private void userOtchestvo_Leave(object sender, EventArgs e)
        {
            if(userOtchestvo.Text == "")
            {
                userOtchestvo.Text = "Введите отчество";
                userOtchestvo.ForeColor = Color.Gray;
            }
        }
    }
}
