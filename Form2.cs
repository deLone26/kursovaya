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

using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using Npgsql;

namespace WindowsFormsApp1
{
    public partial class Form2 : Form
    {
        // ================== СТРОКА ПОДКЛЮЧЕНИЯ К БД ==================
        private readonly string connectionString =
            "Host=localhost;Port=5432;Database=boiler_system;Username=postgres;Password=43898362Dd+-;";

        // ================== ID ВЫБРАННОГО СОТРУДНИКА ==================
        private int selectedId = -1;

        public Form2()
        {
            InitializeComponent();

            // Автоматическая загрузка сотрудников при открытии формы
            LoadData();

            cmbRole.Items.Clear();
            cmbRole.Items.Add("admin");
            cmbRole.Items.Add("boss");
            cmbRole.Items.Add("slesar");
            cmbRole.Items.Add("operator");

            cmbRole.SelectedIndex = 3;
        }

       
        

        // ================== ЗАГРУЗКА ДАННЫХ ИЗ ТАБЛИЦЫ sotrudniki ==================
        private void LoadData(string filterId = "")
        {
            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();

                    // Базовый SQL-запрос
                    string sql = @"SELECT id, familiya, imya, otchestvo, dolzhnost, telefon, email
                                   FROM sotrudniki";

                    // Если введён ID — фильтрация по ID
                    if (!string.IsNullOrEmpty(filterId))
                    {
                        sql += " WHERE id = @id";
                    }

                    sql += " ORDER BY id";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        if (!string.IsNullOrEmpty(filterId))
                        {
                            cmd.Parameters.AddWithValue("@id", int.Parse(filterId));
                        }

                        using (var adapter = new NpgsqlDataAdapter(cmd))
                        {
                            DataTable table = new DataTable();
                            adapter.Fill(table);

                            // Очистка таблицы перед загрузкой новых данных
                            dataGridView1.DataSource = null;
                            dataGridView1.DataSource = table;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки сотрудников: " + ex.Message);
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


        // ================== КЛИК ПО СТРОКЕ В ТАБЛИЦЕ ==================
        private void dataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            // Если кликнули по заголовку таблицы — ничего не делаем
            if (e.RowIndex < 0) return;

            // Получаем выбранную строку
            DataGridViewRow row = dataGridView1.Rows[e.RowIndex];

            // Запоминаем ID выбранного сотрудника
            selectedId = Convert.ToInt32(row.Cells["id"].Value);

            
            // Заполняем поля формы данными сотрудника
            txtFamiliya.Text = row.Cells["familiya"].Value.ToString();
            txtImya.Text = row.Cells["imya"].Value.ToString();
            txtOtchestvo.Text = row.Cells["otchestvo"].Value.ToString();
            txtDolzhnost.Text = row.Cells["dolzhnost"].Value.ToString();
            txtTelefon.Text = row.Cells["telefon"].Value.ToString();
            txtEmail.Text = row.Cells["email"].Value.ToString();
        }

        // ================== ПРОВЕРКА ВВОДА ДАННЫХ ==================
        private bool ValidateInput()
        {
            if (string.IsNullOrWhiteSpace(txtFamiliya.Text))
            {
                MessageBox.Show("Введите фамилию!");
                return false;
            }

            if (string.IsNullOrWhiteSpace(txtImya.Text))
            {
                MessageBox.Show("Введите имя!");
                return false;
            }

            return true;
        }

        // ================== ДОБАВЛЕНИЕ СОТРУДНИКА ==================
        private void btnAdd_Click(object sender, EventArgs e)
        {
            if (!ValidateInput()) return;

            string login = txtLogin.Text;
            string password = txtPassword.Text;

            if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Введите логин и пароль!");
                return;
            }

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
                        (familiya, imya, otchestvo, dolzhnost, telefon, email)
                        VALUES (@familiya, @imya, @otchestvo, @dolzhnost, @telefon, @email)
                        RETURNING id";

                            int sotrudnikId;

                            using (var cmd = new NpgsqlCommand(sqlSotrudnik, conn))
                            {
                                cmd.Parameters.AddWithValue("@familiya", txtFamiliya.Text);
                                cmd.Parameters.AddWithValue("@imya", txtImya.Text);
                                cmd.Parameters.AddWithValue("@otchestvo", txtOtchestvo.Text);
                                cmd.Parameters.AddWithValue("@dolzhnost", txtDolzhnost.Text);
                                cmd.Parameters.AddWithValue("@telefon", txtTelefon.Text);
                                cmd.Parameters.AddWithValue("@email", txtEmail.Text);

                                sotrudnikId = Convert.ToInt32(cmd.ExecuteScalar());
                            }

                            // 2️⃣ Проверяем уникальность логина
                            string checkLogin = "SELECT COUNT(*) FROM users WHERE login=@login";

                            using (var checkCmd = new NpgsqlCommand(checkLogin, conn))
                            {
                                checkCmd.Parameters.AddWithValue("@login", login);
                                int exists = Convert.ToInt32(checkCmd.ExecuteScalar());

                                if (exists > 0)
                                    throw new Exception("Такой логин уже существует!");
                            }

                            // 3️⃣ Добавляем пользователя
                            string sqlUser = @"INSERT INTO users
                        (login, password_hash, role, sotrudnik_id, is_active)
                        VALUES (@login, @password_hash, @role, @sotrudnik_id, true)";

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

                MessageBox.Show("Сотрудник и пользователь успешно добавлены!");
                LoadData();
                ClearFields();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка: " + ex.Message);
            }
        }

        // ================== ПОИСК ПО ID ==================
        private void btnSearch_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtSearch.Text))
            {
                LoadData(); // если поле пустое — показываем всех
                return;
            }

            LoadData(txtSearch.Text);
        }

        // ================== ОБНОВЛЕНИЕ ТАБЛИЦЫ ==================
        private void btnRefresh_Click(object sender, EventArgs e)
        {
            LoadData();
        }

        // ================== ОЧИСТКА ПОЛЕЙ ==================
        // ПЕРЕНЕСТИ НА ФОРМУ1!!!!!!
        
            private void ClearFields()
        {
            // Сбрасываем выбранный ID
            selectedId = -1;

            // Очищаем текстовые поля
            txtFamiliya.Clear();
            txtImya.Clear();
            txtOtchestvo.Clear();
            txtDolzhnost.Clear();
            txtTelefon.Clear();
            txtEmail.Clear();

            // Убираем выделение строки в DataGridView
            dataGridView1.ClearSelection();

            // Снимаем текущую ячейку (очень важно)
            dataGridView1.CurrentCell = null;
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            if (selectedId == -1)
            {
                MessageBox.Show("Выберите сотрудника!");
                return;
            }

            if (MessageBox.Show("Удалить сотрудника?", "Подтверждение",
                MessageBoxButtons.YesNo) != DialogResult.Yes)
                return;

            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();

                    using (var transaction = conn.BeginTransaction())
                    {
                        try
                        {
                            // Сначала удаляем пользователя
                            string sqlUser = "DELETE FROM users WHERE sotrudnik_id=@id";

                            using (var cmd = new NpgsqlCommand(sqlUser, conn))
                            {
                                cmd.Parameters.AddWithValue("@id", selectedId);
                                cmd.ExecuteNonQuery();
                            }

                            // Потом сотрудника
                            string sqlSotrudnik = "DELETE FROM sotrudniki WHERE id=@id";

                            using (var cmd = new NpgsqlCommand(sqlSotrudnik, conn))
                            {
                                cmd.Parameters.AddWithValue("@id", selectedId);
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

                MessageBox.Show("Сотрудник удалён!");
                LoadData();
                ClearFields();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка удаления: " + ex.Message);
            }
        }

        private void btnUpdate_Click(object sender, EventArgs e)
        {
            if (selectedId == -1)
            {
                MessageBox.Show("Выберите сотрудника!");
                return;
            }

            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();

                    string sql = @"UPDATE sotrudniki SET
                familiya=@familiya,
                imya=@imya,
                otchestvo=@otchestvo,
                dolzhnost=@dolzhnost,
                telefon=@telefon,
                email=@email
                WHERE id=@id";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", selectedId);
                        cmd.Parameters.AddWithValue("@familiya", txtFamiliya.Text);
                        cmd.Parameters.AddWithValue("@imya", txtImya.Text);
                        cmd.Parameters.AddWithValue("@otchestvo", txtOtchestvo.Text);
                        cmd.Parameters.AddWithValue("@dolzhnost", txtDolzhnost.Text);
                        cmd.Parameters.AddWithValue("@telefon", txtTelefon.Text);
                        cmd.Parameters.AddWithValue("@email", txtEmail.Text);

                        cmd.ExecuteNonQuery();
                    }

                    // Обновляем роль пользователя
                    string sqlRole = @"UPDATE users 
                               SET role=@role
                               WHERE sotrudnik_id=@id";

                    using (var cmd = new NpgsqlCommand(sqlRole, conn))
                    {
                        cmd.Parameters.AddWithValue("@role", cmbRole.SelectedItem.ToString());
                        cmd.Parameters.AddWithValue("@id", selectedId);

                        cmd.ExecuteNonQuery();
                    }
                }

                MessageBox.Show("Данные обновлены!");
                LoadData();
                ClearFields();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка обновления: " + ex.Message);
            }
        }
        
        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }
    }
}


