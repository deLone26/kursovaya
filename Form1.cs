using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Npgsql;
using System;                     // Базовые типы .NET
using System.Data;                // Работа с таблицами DataTable
using System.Drawing;             // Цвета для подсветки строк
using System.Windows.Forms;       // Windows Forms
using Npgsql;                     // Работа с PostgreSQL

namespace WindowsFormsApp1
{
    public partial class Form1 : Form
    {
        // ================== СТРОКА ПОДКЛЮЧЕНИЯ К БД ==================
        private readonly string connectionString =
            "Host=localhost;Port=5432;Database=boiler_system;Username=postgres;Password=43898362Dd+-;";

        // ================== ID ВЫБРАННОЙ ЗАПИСИ ==================
        private int selectedId = -1;

        // ================== КОНСТРУКТОР ФОРМЫ ==================
        public Form1()
        {
            InitializeComponent();   // Инициализация формы и элементов

            LoadStatuses();          // Загружаем статусы в ComboBox
            LoadData();              // Загружаем данные в таблицу
        }

        // ================== ЗАГРУЗКА СТАТУСОВ ==================
        private void LoadStatuses()
        {
            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open(); // Открываем соединение с БД

                    string sql = "SELECT id, nazvanie FROM status_oborudovaniya ORDER BY id";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    using (var adapter = new NpgsqlDataAdapter(cmd))
                    {
                        DataTable dt = new DataTable();
                        adapter.Fill(dt);

                        cmbStatus.DisplayMember = "nazvanie"; // Текст статуса
                        cmbStatus.ValueMember = "id";         // ID статуса
                        cmbStatus.DataSource = dt;            // Источник данных
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки статусов: " + ex.Message);
            }
        }

        // ================== ЗАГРУЗКА ДАННЫХ ==================
        private void LoadData(string filter = "")
        {
            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();

                    string sql = @"
                        SELECT 
                            o.id,
                            o.nazvanie,
                            o.tip,
                            o.model,
                            o.seriinomer,
                            o.mesto,
                            o.moshnost,
                            o.davlenie,
                            o.proizvoditel,
                            o.data_ustanovki,
                            s.nazvanie AS status,
                            o.status_id
                        FROM oborudovanie o
                        LEFT JOIN status_oborudovaniya s ON o.status_id = s.id
                    ";

                    if (!string.IsNullOrWhiteSpace(filter))
                    {
                        sql += @" WHERE o.nazvanie ILIKE @filter
                                  OR o.tip ILIKE @filter
                                  OR o.model ILIKE @filter
                                  OR o.proizvoditel ILIKE @filter";
                    }

                    sql += " ORDER BY o.id";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        if (!string.IsNullOrWhiteSpace(filter))
                            cmd.Parameters.AddWithValue("@filter", "%" + filter + "%");

                        using (var adapter = new NpgsqlDataAdapter(cmd))
                        {
                            DataTable table = new DataTable();
                            adapter.Fill(table);
                            dataGridView1.DataSource = table;
                        }
                    }
                }

                HighlightRows(); // Подсветка строк
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки данных: " + ex.Message);
            }
        }

        // ================== ПОДСВЕТКА СТРОК ==================
        private void HighlightRows()
        {
            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                if (row.Cells["status"].Value != null)
                {
                    string status = row.Cells["status"].Value.ToString();

                    if (status == "Просрочено ТО")
                        row.DefaultCellStyle.BackColor = Color.LightCoral;
                    else if (status == "На ремонте")
                        row.DefaultCellStyle.BackColor = Color.LightYellow;
                    else if (status == "Работает")
                        row.DefaultCellStyle.BackColor = Color.LightGreen;
                }
            }
        }

        // ================== КЛИК ПО СТРОКЕ ТАБЛИЦЫ ==================
        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            DataGridViewRow row = dataGridView1.Rows[e.RowIndex];

            selectedId = Convert.ToInt32(row.Cells["id"].Value);
            txtSelectedId.Text = selectedId.ToString();

            txtNazvanie.Text = row.Cells["nazvanie"].Value.ToString();
            txtTip.Text = row.Cells["tip"].Value.ToString();
            txtModel.Text = row.Cells["model"].Value.ToString();
            txtSeria.Text = row.Cells["seriinomer"].Value.ToString();
            txtMesto.Text = row.Cells["mesto"].Value.ToString();
            txtMoshnost.Text = row.Cells["moshnost"].Value.ToString();
            txtDavlenie.Text = row.Cells["davlenie"].Value.ToString();
            txtProizvoditel.Text = row.Cells["proizvoditel"].Value.ToString();

            // ===== ДАТА УСТАНОВКИ =====
            if (row.Cells["data_ustanovki"].Value != DBNull.Value)
                txtDataUstanov.Text = Convert.ToDateTime(row.Cells["data_ustanovki"].Value).ToString("yyyy-MM-dd");

            // ===== СТАТУС =====
            if (row.Cells["status_id"].Value != DBNull.Value)
                cmbStatus.SelectedValue = Convert.ToInt32(row.Cells["status_id"].Value);
        }

        // ================== ПРОВЕРКА ВВОДА ==================
        private bool ValidateInput()
        {
            if (string.IsNullOrWhiteSpace(txtNazvanie.Text))
            {
                MessageBox.Show("Введите название!");
                return false;
            }

            if (string.IsNullOrWhiteSpace(txtTip.Text))
            {
                MessageBox.Show("Введите тип!");
                return false;
            }

            if (!decimal.TryParse(txtMoshnost.Text, out _))
            {
                MessageBox.Show("Мощность должна быть числом!");
                return false;
            }

            if (!decimal.TryParse(txtDavlenie.Text, out _))
            {
                MessageBox.Show("Давление должно быть числом!");
                return false;
            }

            if (!DateTime.TryParse(txtDataUstanov.Text, out _))
            {
                MessageBox.Show("Введите корректную дату установки!");
                return false;
            }

            return true;
        }

        // ================== ДОБАВЛЕНИЕ ==================
        private void button1_Click(object sender, EventArgs e)
        {
            if (!ValidateInput()) return;

            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();

                    string sql = @"
                        INSERT INTO oborudovanie
                        (nazvanie, tip, model, seriinomer, mesto, moshnost, davlenie, proizvoditel, data_ustanovki, status_id)
                        VALUES
                        (@nazvanie, @tip, @model, @seria, @mesto, @moshnost, @davlenie, @proizvoditel, @data, @status_id)
                    ";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@nazvanie", txtNazvanie.Text);
                        cmd.Parameters.AddWithValue("@tip", txtTip.Text);
                        cmd.Parameters.AddWithValue("@model", txtModel.Text);
                        cmd.Parameters.AddWithValue("@seria", txtSeria.Text);
                        cmd.Parameters.AddWithValue("@mesto", txtMesto.Text);
                        cmd.Parameters.AddWithValue("@moshnost", decimal.Parse(txtMoshnost.Text));
                        cmd.Parameters.AddWithValue("@davlenie", decimal.Parse(txtDavlenie.Text));
                        cmd.Parameters.AddWithValue("@proizvoditel", txtProizvoditel.Text);
                        cmd.Parameters.AddWithValue("@data", DateTime.Parse(txtDataUstanov.Text));
                        cmd.Parameters.AddWithValue("@status_id", (int)cmbStatus.SelectedValue);

                        cmd.ExecuteNonQuery();
                    }
                }

                LoadData();
                ClearFields();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка добавления: " + ex.Message);
            }
        }

        // ================== ОБНОВЛЕНИЕ ==================
        private void button2_Click(object sender, EventArgs e)
        {
            if (selectedId == -1)
            {
                MessageBox.Show("Выберите запись!");
                return;
            }

            if (!ValidateInput()) return;

            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();

                    string sql = @"
                        UPDATE oborudovanie SET
                        nazvanie=@nazvanie,
                        tip=@tip,
                        model=@model,
                        seriinomer=@seria,
                        mesto=@mesto,
                        moshnost=@moshnost,
                        davlenie=@davlenie,
                        proizvoditel=@proizvoditel,
                        data_ustanovki=@data,
                        status_id=@status_id
                        WHERE id=@id
                    ";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", selectedId);
                        cmd.Parameters.AddWithValue("@nazvanie", txtNazvanie.Text);
                        cmd.Parameters.AddWithValue("@tip", txtTip.Text);
                        cmd.Parameters.AddWithValue("@model", txtModel.Text);
                        cmd.Parameters.AddWithValue("@seria", txtSeria.Text);
                        cmd.Parameters.AddWithValue("@mesto", txtMesto.Text);
                        cmd.Parameters.AddWithValue("@moshnost", decimal.Parse(txtMoshnost.Text));
                        cmd.Parameters.AddWithValue("@davlenie", decimal.Parse(txtDavlenie.Text));
                        cmd.Parameters.AddWithValue("@proizvoditel", txtProizvoditel.Text);
                        cmd.Parameters.AddWithValue("@data", DateTime.Parse(txtDataUstanov.Text));
                        cmd.Parameters.AddWithValue("@status_id", (int)cmbStatus.SelectedValue);

                        cmd.ExecuteNonQuery();
                    }
                }

                LoadData();
                ClearFields();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка обновления: " + ex.Message);
            }
        }

        // ================== УДАЛЕНИЕ ==================
        private void button3_Click(object sender, EventArgs e)
        {
            if (selectedId == -1)
            {
                MessageBox.Show("Выберите запись!");
                return;
            }

            if (MessageBox.Show("Удалить запись?", "Подтверждение",
                MessageBoxButtons.YesNo) != DialogResult.Yes)
                return;

            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();

                    string sql = "DELETE FROM oborudovanie WHERE id=@id";

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
                MessageBox.Show("Ошибка удаления: " + ex.Message);
            }
        }

        // ================== ПОИСК ==================
        private void txtSearch_TextChanged(object sender, EventArgs e)
        {
            LoadData(txtSearch.Text);
        }

        // ================== ОБНОВЛЕНИЕ ТАБЛИЦЫ ==================
        private void button5_Click(object sender, EventArgs e)
        {
            LoadData();
        }

        // ================== ОЧИСТКА ПОЛЕЙ ==================
        private void ClearFields()
        {
            selectedId = -1;
            txtSelectedId.Clear();

            txtNazvanie.Clear();
            txtTip.Clear();
            txtModel.Clear();
            txtSeria.Clear();
            txtMesto.Clear();
            txtMoshnost.Clear();
            txtDavlenie.Clear();
            txtProizvoditel.Clear();
            txtDataUstanov.Clear();

            if (cmbStatus.Items.Count > 0)
                cmbStatus.SelectedIndex = 0;
        }

        private void Form1_Load(object sender, EventArgs e) { }
        private void label14_Click(object sender, EventArgs e) { }
        private void button4_Click(object sender, EventArgs e) 
        {
            // Проверяем: введён ли ID
            if (string.IsNullOrWhiteSpace(txtSearch.Text))
            {
                MessageBox.Show("Введите ID записи!");
                return;
            }

            // Проверяем: число ли это
            if (!int.TryParse(txtSearch.Text, out int id))
            {
                MessageBox.Show("ID должен быть числом!");
                return;
            }

            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();

                    // SQL-запрос: ищем запись по ID
                    string sql = @"SELECT id, nazvanie, tip, model, seriinomer, mesto, 
                                  moshnost, davlenie, proizvoditel, data_ustanovki, status_id
                           FROM oborudovanie
                           WHERE id = @id";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", id);

                        using (var adapter = new NpgsqlDataAdapter(cmd))
                        {
                            DataTable table = new DataTable();
                            adapter.Fill(table);

                            // Если запись найдена — показываем её
                            if (table.Rows.Count > 0)
                            {
                                dataGridView1.DataSource = table;
                            }
                            else
                            {
                                // Если запись не найдена — очищаем таблицу
                                dataGridView1.DataSource = null;
                                MessageBox.Show("Запись с таким ID не найдена!");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка поиска: " + ex.Message);
            }
        }
        private void label10_Click(object sender, EventArgs e) { }
    }
}
