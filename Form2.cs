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

            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();

                    string sql = @"INSERT INTO sotrudniki 
                    (familiya, imya, otchestvo, dolzhnost, telefon, email)
                    VALUES (@familiya, @imya, @otchestvo, @dolzhnost, @telefon, @email)";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@familiya", txtFamiliya.Text);
                        cmd.Parameters.AddWithValue("@imya", txtImya.Text);
                        cmd.Parameters.AddWithValue("@otchestvo", txtOtchestvo.Text);
                        cmd.Parameters.AddWithValue("@dolzhnost", txtDolzhnost.Text);
                        cmd.Parameters.AddWithValue("@telefon", txtTelefon.Text);
                        cmd.Parameters.AddWithValue("@email", txtEmail.Text);

                        cmd.ExecuteNonQuery();
                    }
                }

                LoadData();
                ClearFields();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка добавления сотрудника: " + ex.Message);
            }
        }

        // ================== ОБНОВЛЕНИЕ СОТРУДНИКА ==================
        private void btnUpdate_Click(object sender, EventArgs e)
        {
            if (selectedId == -1)
            {
                MessageBox.Show("Выберите сотрудника!");
                return;
            }

            if (!ValidateInput()) return;

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
                }

                LoadData();
                ClearFields();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка обновления сотрудника: " + ex.Message);
            }
        }

        // ================== УДАЛЕНИЕ СОТРУДНИКА ==================
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

                    string sql = "DELETE FROM sotrudniki WHERE id=@id";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", selectedId);
                        cmd.ExecuteNonQuery();
                    }
                }

                LoadData();
                ClearFields();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка удаления сотрудника: " + ex.Message);
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
            selectedId = -1;

            txtFamiliya.Clear();
            txtImya.Clear();
            txtOtchestvo.Clear();
            txtDolzhnost.Clear();
            txtTelefon.Clear();
            txtEmail.Clear();
        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }
    }
}


