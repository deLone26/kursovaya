using System;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Npgsql;
using System.IO;

namespace WindowsFormsApp1
{
    public partial class FormBoss : Form
    {
        private string connectionString;
        private int currentUserId;
        private int selectedPlanId = -1;
        private int selectedAvariyaId = -1;

        public FormBoss(string userConnectionString, int userId)
        {
            InitializeComponent();

            // Привязка событий
            this.btnFilter.Click += BtnPlanFilter_Click;
            this.chkAll.CheckedChanged += ChkPlanAll_CheckedChanged;
            this.btnAdd.Click += BtnPlanAdd_Click;
            this.btnUpdate.Click += BtnPlanUpdate_Click;
            this.btnDelete.Click += BtnPlanDelete_Click;
            this.btnClear.Click += BtnPlanClear_Click;
            this.btnExcel.Click += BtnExcel_Click;
            this.btnWord.Click += BtnWord_Click;
            this.btnPreview.Click += BtnPreview_Click;
            this.dgvPlans.CellClick += DgvPlans_CellClick;

            // Привязка событий для вкладки Аварии
            this.btnAvariyaFilter.Click += BtnAvariyaFilter_Click;
            this.chkAvariyaAll.CheckedChanged += ChkAvariyaAll_CheckedChanged;
            this.dgvAvariya.CellClick += DgvAvariya_CellClick;
            this.dgvAvariya.CellFormatting += DgvAvariya_CellFormatting;
            this.btnCreatePlanFromAvariya.Click += BtnCreatePlanFromAvariya_Click;

            this.connectionString = userConnectionString;
            this.currentUserId = userId;

            string login = GetLoginByEmployeeId(userId);
            this.Text = $"Панель начальника котельной - Планирование ремонтов ({login})";

            // Настройка элементов
            SetupDataGridView(dgvPlans);
            SetupDataGridView(dgvAvariya);

            // Загрузка данных
            LoadEquipment();
            LoadTipTypes();
            LoadResponsible();
            LoadStatuses();
            LoadReportTypes();

            LoadPlans();
            LoadAvariya();
            LoadStatistics();
        }

        // ========== ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ ==========

        private string GetLoginByEmployeeId(int empId)
        {
            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();
                    string sql = "SELECT login FROM users WHERE sotrudnik_id = @id";
                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", empId);
                        var result = cmd.ExecuteScalar();
                        return result?.ToString() ?? "Начальник";
                    }
                }
            }
            catch
            {
                return "Начальник";
            }
        }

        private void SetupDataGridView(DataGridView dgv)
        {
            if (dgv == null) return;
            dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgv.MultiSelect = false;
            dgv.ReadOnly = true;
            dgv.AllowUserToAddRows = false;
            dgv.AllowUserToDeleteRows = false;
        }

        // ========== ЗАГРУЗКА СПРАВОЧНИКОВ ==========

        private void LoadEquipment()
        {
            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();
                    string sql = "SELECT id, nazvanie FROM oborudovanie ORDER BY nazvanie";
                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            DataTable dt = new DataTable();
                            dt.Load(reader);
                            cmbEquipment.DisplayMember = "nazvanie";
                            cmbEquipment.ValueMember = "id";
                            cmbEquipment.DataSource = dt;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки оборудования: " + ex.Message);
            }
        }

        private void LoadTipTypes()
        {
            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();
                    string sql = "SELECT id, nazvanie FROM tip_to ORDER BY nazvanie";
                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            DataTable dt = new DataTable();
                            dt.Load(reader);
                            cmbTip.DisplayMember = "nazvanie";
                            cmbTip.ValueMember = "id";
                            cmbTip.DataSource = dt;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки типов ТО: " + ex.Message);
            }
        }

        private void LoadResponsible()
        {
            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();
                    string sql = @"
                        SELECT id, familiya || ' ' || imya || ' ' || otchestvo AS fio 
                        FROM sotrudniki 
                        WHERE dolzhnost ILIKE '%слесар%'
                        ORDER BY familiya";
                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            DataTable dt = new DataTable();
                            dt.Load(reader);
                            cmbResponsible.DisplayMember = "fio";
                            cmbResponsible.ValueMember = "id";
                            cmbResponsible.DataSource = dt;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки сотрудников: " + ex.Message);
            }
        }

        private void LoadStatuses()
        {
            cmbStatus.Items.Clear();
            cmbStatus.Items.Add("Запланирован");
            cmbStatus.Items.Add("В работе");
            cmbStatus.Items.Add("Завершен");
            cmbStatus.Items.Add("Отменен");
            cmbStatus.SelectedIndex = 0;
        }

        private void LoadReportTypes()
        {
            cmbReportType.Items.Clear();
            cmbReportType.Items.Add("Все планы");
            cmbReportType.Items.Add("Только запланированные");
            cmbReportType.Items.Add("В работе");
            cmbReportType.Items.Add("Завершенные");
            cmbReportType.Items.Add("Аварии");
            cmbReportType.Items.Add("Аварии без плана");
            cmbReportType.SelectedIndex = 0;
        }

        // ========== ЗАГРУЗКА ПЛАНОВ ==========

        private void LoadPlans()
        {
            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();

                    string sql = @"
                        SELECT 
                            p.id,
                            o.nazvanie AS Оборудование,
                            t.nazvanie AS Тип_ТО,
                            p.data_nachala AS Дата_начала,
                            p.data_okonchaniya AS Дата_окончания,
                            s.familiya || ' ' || s.imya || ' ' || s.otchestvo AS Ответственный,
                            p.status AS Статус,
                            CASE WHEN p.avariya_id IS NOT NULL THEN '✅' ELSE '❌' END AS Связь_с_аварией
                        FROM plan_to p
                        JOIN oborudovanie o ON p.oborudovanie_id = o.id
                        LEFT JOIN tip_to t ON p.tip_to_id = t.id
                        LEFT JOIN sotrudniki s ON p.otvetstvenniy_id = s.id";

                    if (!chkAll.Checked)
                    {
                        sql += " WHERE DATE(p.data_nachala) BETWEEN @start AND @end";
                    }

                    sql += " ORDER BY p.data_nachala DESC";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        if (!chkAll.Checked)
                        {
                            cmd.Parameters.AddWithValue("@start", dtpStart.Value.Date);
                            cmd.Parameters.AddWithValue("@end", dtpEnd.Value.Date.AddDays(1).AddSeconds(-1));
                        }

                        using (var adapter = new NpgsqlDataAdapter(cmd))
                        {
                            DataTable dt = new DataTable();
                            adapter.Fill(dt);
                            dgvPlans.DataSource = dt;

                            if (dgvPlans.Columns["id"] != null)
                                dgvPlans.Columns["id"].Visible = false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки планов: " + ex.Message);
            }
        }

        // ========== ЗАГРУЗКА АВАРИЙ ==========

        private void LoadAvariya()
        {
            if (dgvAvariya == null) return;

            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();
                    string sql = @"
                        SELECT 
                            a.id,
                            o.nazvanie AS Оборудование,
                            a.data_avarii AS Дата,
                            a.opisanie AS Описание,
                            a.posledstviya AS Последствия,
                            COALESCE(a.status, 'Зарегистрирована') AS Статус,
                            CASE WHEN p.id IS NOT NULL THEN '✅' ELSE '❌' END AS План
                        FROM avariya a
                        JOIN oborudovanie o ON a.oborudovanie_id = o.id
                        LEFT JOIN plan_to p ON a.id = p.avariya_id";

                    if (!chkAvariyaAll.Checked)
                    {
                        sql += " WHERE DATE(a.data_avarii) BETWEEN @start AND @end";
                    }

                    sql += " ORDER BY a.data_avarii DESC";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        if (!chkAvariyaAll.Checked)
                        {
                            cmd.Parameters.AddWithValue("@start", dtpAvariyaStart.Value.Date);
                            cmd.Parameters.AddWithValue("@end", dtpAvariyaEnd.Value.Date.AddDays(1).AddSeconds(-1));
                        }

                        using (var adapter = new NpgsqlDataAdapter(cmd))
                        {
                            DataTable dt = new DataTable();
                            adapter.Fill(dt);
                            dgvAvariya.DataSource = dt;

                            if (dgvAvariya.Columns["id"] != null)
                                dgvAvariya.Columns["id"].Visible = false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки аварий: " + ex.Message);
            }
        }

        // ========== ЗАГРУЗКА СТАТИСТИКИ ==========

        private void LoadStatistics()
        {
            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();
                    string sql = @"
                        SELECT 
                            (SELECT COUNT(*) FROM avariya) as total_avariya,
                            (SELECT COUNT(*) FROM avariya WHERE COALESCE(status, '') != 'Завершена' AND ustraneno = false) as need_plan,
                            (SELECT COUNT(*) FROM avariya WHERE status = 'В работе') as avariya_in_progress,
                            (SELECT COUNT(*) FROM avariya WHERE status = 'Завершена' OR ustraneno = true) as avariya_completed,
                            (SELECT COUNT(*) FROM plan_to) as total_plans,
                            (SELECT COUNT(*) FROM plan_to WHERE status = 'Завершен') as completed_plans,
                            (SELECT COUNT(*) FROM plan_to WHERE status = 'В работе') as plans_in_progress,
                            (SELECT COUNT(*) FROM plan_to WHERE status = 'Запланирован') as plans_planned
                    ";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                // Общая статистика
                                int total = Convert.ToInt32(reader["total_avariya"]) + Convert.ToInt32(reader["total_plans"]);
                                lblTotal.Text = $"Всего записей: {total}";

                                // Статистика по авариям
                                lblAvariyaTotal.Text = $"Всего аварий: {reader["total_avariya"]}";
                                lblAvariyaNeedPlan.Text = $"Требуют плана: {reader["need_plan"]}";
                                lblAvariyaInProgress.Text = $"В работе: {reader["avariya_in_progress"]}";
                                lblAvariyaCompleted.Text = $"Завершено: {reader["avariya_completed"]}";

                                // Статистика по планам
                                lblInProgress.Text = $"В работе: {reader["plans_in_progress"]}";
                                lblCompleted.Text = $"Завершено: {reader["completed_plans"]}";
                                lblPlanned.Text = $"Запланировано: {reader["plans_planned"]}";
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка статистики: " + ex.Message);
            }
        }

        // ========== ОБРАБОТЧИКИ ДЛЯ ПЛАНОВ ==========

        private void DgvPlans_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            DataGridViewRow row = dgvPlans.Rows[e.RowIndex];
            selectedPlanId = Convert.ToInt32(row.Cells["id"].Value);
        }

        private void BtnPlanFilter_Click(object sender, EventArgs e)
        {
            LoadPlans();
        }

        private void ChkPlanAll_CheckedChanged(object sender, EventArgs e)
        {
            dtpStart.Enabled = !chkAll.Checked;
            dtpEnd.Enabled = !chkAll.Checked;
            btnFilter.Enabled = !chkAll.Checked;
            LoadPlans();
        }

        private void BtnPlanAdd_Click(object sender, EventArgs e)
        {
            if (!ValidatePlanInput()) return;

            try
            {
                int equipmentId = GetSelectedId(cmbEquipment, "id");
                int tipId = GetSelectedId(cmbTip, "id");
                int responsibleId = GetSelectedId(cmbResponsible, "id");

                using (var conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();
                    string sql = @"
                        INSERT INTO plan_to 
                        (oborudovanie_id, tip_to_id, data_nachala, data_okonchaniya, otvetstvenniy_id, status)
                        VALUES 
                        (@oborudovanie_id, @tip_to_id, @data_nachala, @data_okonchaniya, @otvetstvenniy_id, @status)";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@oborudovanie_id", equipmentId);
                        cmd.Parameters.AddWithValue("@tip_to_id", tipId);
                        cmd.Parameters.AddWithValue("@data_nachala", dtpStartRepair.Value.Date);
                        cmd.Parameters.AddWithValue("@data_okonchaniya", dtpEndRepair.Value.Date);
                        cmd.Parameters.AddWithValue("@otvetstvenniy_id", responsibleId);
                        cmd.Parameters.AddWithValue("@status", cmbStatus.Text);

                        cmd.ExecuteNonQuery();
                    }
                }

                MessageBox.Show("План успешно добавлен!");
                LoadPlans();
                LoadStatistics();
                ClearPlanForm();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка добавления: " + ex.Message);
            }
        }

        private void BtnPlanUpdate_Click(object sender, EventArgs e)
        {
            if (selectedPlanId == -1)
            {
                MessageBox.Show("Выберите план для обновления!");
                return;
            }

            if (!ValidatePlanInput()) return;

            DialogResult result = MessageBox.Show("Обновить выбранный план?", "Подтверждение",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result != DialogResult.Yes) return;

            try
            {
                int equipmentId = GetSelectedId(cmbEquipment, "id");
                int tipId = GetSelectedId(cmbTip, "id");
                int responsibleId = GetSelectedId(cmbResponsible, "id");

                using (var conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();
                    string sql = @"
                        UPDATE plan_to SET
                            oborudovanie_id = @oborudovanie_id,
                            tip_to_id = @tip_to_id,
                            data_nachala = @data_nachala,
                            data_okonchaniya = @data_okonchaniya,
                            otvetstvenniy_id = @otvetstvenniy_id,
                            status = @status
                        WHERE id = @id";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", selectedPlanId);
                        cmd.Parameters.AddWithValue("@oborudovanie_id", equipmentId);
                        cmd.Parameters.AddWithValue("@tip_to_id", tipId);
                        cmd.Parameters.AddWithValue("@data_nachala", dtpStartRepair.Value.Date);
                        cmd.Parameters.AddWithValue("@data_okonchaniya", dtpEndRepair.Value.Date);
                        cmd.Parameters.AddWithValue("@otvetstvenniy_id", responsibleId);
                        cmd.Parameters.AddWithValue("@status", cmbStatus.Text);

                        int rowsAffected = cmd.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            MessageBox.Show("План успешно обновлен!");
                            LoadPlans();
                            LoadStatistics();
                            ClearPlanForm();
                        }
                        else
                        {
                            MessageBox.Show("Не удалось обновить план.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка обновления: " + ex.Message);
            }
        }

        private void BtnPlanDelete_Click(object sender, EventArgs e)
        {
            if (selectedPlanId == -1)
            {
                MessageBox.Show("Выберите план для удаления!");
                return;
            }

            DialogResult result = MessageBox.Show("Удалить выбранный план?", "Подтверждение",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result != DialogResult.Yes) return;

            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();
                    string sql = "DELETE FROM plan_to WHERE id = @id";
                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", selectedPlanId);
                        int rowsAffected = cmd.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            MessageBox.Show("План успешно удален!");
                            selectedPlanId = -1;
                            LoadPlans();
                            LoadStatistics();
                            ClearPlanForm();
                        }
                        else
                        {
                            MessageBox.Show("Не удалось удалить план.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка удаления: " + ex.Message);
            }
        }

        private void BtnPlanClear_Click(object sender, EventArgs e)
        {
            ClearPlanForm();
        }

        // ========== ОБРАБОТЧИКИ ДЛЯ АВАРИЙ ==========

        private void DgvAvariya_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || dgvAvariya == null) return;

            DataGridViewRow row = dgvAvariya.Rows[e.RowIndex];
            selectedAvariyaId = Convert.ToInt32(row.Cells["id"].Value);

            string hasPlan = row.Cells["План"].Value?.ToString() ?? "";
            btnCreatePlanFromAvariya.BackColor = hasPlan == "❌" ? Color.Orange : Color.LightGreen;
        }

        private void DgvAvariya_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex >= 0 && dgvAvariya != null)
            {
                DataGridViewRow row = dgvAvariya.Rows[e.RowIndex];
                string hasPlan = row.Cells["План"].Value?.ToString() ?? "";

                if (hasPlan == "❌")
                    row.DefaultCellStyle.BackColor = Color.LightCoral;
                else
                    row.DefaultCellStyle.BackColor = Color.LightGreen;
            }
        }

        private void BtnAvariyaFilter_Click(object sender, EventArgs e)
        {
            LoadAvariya();
        }

        private void ChkAvariyaAll_CheckedChanged(object sender, EventArgs e)
        {
            dtpAvariyaStart.Enabled = !chkAvariyaAll.Checked;
            dtpAvariyaEnd.Enabled = !chkAvariyaAll.Checked;
            btnAvariyaFilter.Enabled = !chkAvariyaAll.Checked;
            LoadAvariya();
        }

        private void BtnCreatePlanFromAvariya_Click(object sender, EventArgs e)
        {
            if (selectedAvariyaId == -1)
            {
                MessageBox.Show("Выберите аварию!");
                return;
            }

            // Проверяем, есть ли уже план
            using (var conn = new NpgsqlConnection(connectionString))
            {
                conn.Open();
                string checkSql = "SELECT id FROM plan_to WHERE avariya_id = @id";
                using (var cmd = new NpgsqlCommand(checkSql, conn))
                {
                    cmd.Parameters.AddWithValue("@id", selectedAvariyaId);
                    var exists = cmd.ExecuteScalar();
                    if (exists != null)
                    {
                        MessageBox.Show("Для этой аварии уже создан план!");
                        return;
                    }
                }
            }

            // Получаем данные аварии
            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();
                    string sql = @"SELECT oborudovanie_id FROM avariya WHERE id = @id";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", selectedAvariyaId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                int oborudovanieId = reader.GetInt32(0);

                                // Переключаемся на вкладку планов
                                tabControl1.SelectedTab = tabPlans;

                                // Устанавливаем оборудование
                                SelectEquipmentById(oborudovanieId);

                                // Устанавливаем даты
                                dtpStartRepair.Value = DateTime.Now;
                                dtpEndRepair.Value = DateTime.Now.AddDays(7);

                                // Статус по умолчанию
                                cmbStatus.SelectedIndex = 0;

                                // Очищаем другие поля
                                cmbTip.SelectedIndex = -1;
                                cmbResponsible.SelectedIndex = -1;

                                MessageBox.Show("Данные аварии перенесены. Выберите тип ТО, ответственного и создайте план.");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка: " + ex.Message);
            }
        }

        // ========== ОБРАБОТЧИКИ ДЛЯ ОТЧЕТОВ ==========

        private void BtnExcel_Click(object sender, EventArgs e)
        {
            DataTable reportData = GetReportData();
            if (reportData.Rows.Count == 0)
            {
                MessageBox.Show("Нет данных для экспорта!");
                return;
            }

            SaveFileDialog save = new SaveFileDialog();
            save.Filter = "CSV files (*.csv)|*.csv";
            save.FileName = $"Отчет_{cmbReportType.Text}_{DateTime.Now:yyyyMMdd_HHmmss}.csv";

            if (save.ShowDialog() == DialogResult.OK)
            {
                ExportToCsv(reportData, save.FileName);
                MessageBox.Show($"Отчет сохранен!\nЗаписей: {reportData.Rows.Count}");
            }
        }

        private void BtnWord_Click(object sender, EventArgs e)
        {
            DataTable reportData = GetReportData();
            if (reportData.Rows.Count == 0)
            {
                MessageBox.Show("Нет данных для экспорта!");
                return;
            }

            SaveFileDialog save = new SaveFileDialog();
            save.Filter = "Rich Text Format (*.rtf)|*.rtf";
            save.FileName = $"Отчет_{cmbReportType.Text}_{DateTime.Now:yyyyMMdd_HHmmss}.rtf";

            if (save.ShowDialog() == DialogResult.OK)
            {
                ExportToRtf(reportData, save.FileName);
                MessageBox.Show($"Отчет сохранен!\nЗаписей: {reportData.Rows.Count}");
            }
        }

        private void BtnPreview_Click(object sender, EventArgs e)
        {
            DataTable reportData = GetReportData();
            MessageBox.Show($"Тип отчета: {cmbReportType.Text}\nВсего записей: {reportData.Rows.Count}",
                "Предпросмотр", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // ========== ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ ==========

        private bool ValidatePlanInput()
        {
            if (cmbEquipment.SelectedItem == null)
            {
                MessageBox.Show("Выберите оборудование!");
                return false;
            }
            if (cmbTip.SelectedItem == null)
            {
                MessageBox.Show("Выберите тип ТО!");
                return false;
            }
            if (cmbResponsible.SelectedItem == null)
            {
                MessageBox.Show("Выберите ответственного!");
                return false;
            }
            if (dtpStartRepair.Value > dtpEndRepair.Value)
            {
                MessageBox.Show("Дата начала не может быть позже даты окончания!");
                return false;
            }
            return true;
        }

        private void ClearPlanForm()
        {
            selectedPlanId = -1;
            if (cmbEquipment.Items.Count > 0) cmbEquipment.SelectedIndex = -1;
            if (cmbTip.Items.Count > 0) cmbTip.SelectedIndex = -1;
            if (cmbResponsible.Items.Count > 0) cmbResponsible.SelectedIndex = -1;
            cmbStatus.SelectedIndex = 0;
            dtpStartRepair.Value = DateTime.Now;
            dtpEndRepair.Value = DateTime.Now.AddDays(7);
        }

        private int GetSelectedId(ComboBox comboBox, string columnName)
        {
            DataRowView selectedItem = comboBox.SelectedItem as DataRowView;
            if (selectedItem != null)
            {
                return Convert.ToInt32(selectedItem[columnName]);
            }
            return -1;
        }

        private void SelectEquipmentById(int equipmentId)
        {
            foreach (var item in cmbEquipment.Items)
            {
                DataRowView row = item as DataRowView;
                if (row != null && Convert.ToInt32(row["id"]) == equipmentId)
                {
                    cmbEquipment.SelectedItem = item;
                    break;
                }
            }
        }

        private DataTable GetReportData()
        {
            DataTable dt = new DataTable();

            if (cmbReportType.SelectedIndex <= 3) // Отчеты по планам
            {
                dt.Columns.Add("ID");
                dt.Columns.Add("Оборудование");
                dt.Columns.Add("Тип ТО");
                dt.Columns.Add("Дата начала");
                dt.Columns.Add("Дата окончания");
                dt.Columns.Add("Ответственный");
                dt.Columns.Add("Статус");

                foreach (DataGridViewRow row in dgvPlans.Rows)
                {
                    if (row.IsNewRow) continue;

                    bool include = false;
                    string status = row.Cells["Статус"].Value?.ToString() ?? "";

                    switch (cmbReportType.SelectedIndex)
                    {
                        case 0: include = true; break;
                        case 1: include = (status == "Запланирован"); break;
                        case 2: include = (status == "В работе"); break;
                        case 3: include = (status == "Завершен"); break;
                    }

                    if (include)
                    {
                        dt.Rows.Add(
                            row.Cells["id"].Value,
                            row.Cells["Оборудование"].Value,
                            row.Cells["Тип_ТО"].Value,
                            row.Cells["Дата_начала"].Value,
                            row.Cells["Дата_окончания"].Value,
                            row.Cells["Ответственный"].Value,
                            row.Cells["Статус"].Value
                        );
                    }
                }
            }
            else // Отчеты по авариям
            {
                dt.Columns.Add("ID");
                dt.Columns.Add("Оборудование");
                dt.Columns.Add("Дата");
                dt.Columns.Add("Описание");
                dt.Columns.Add("Последствия");
                dt.Columns.Add("Статус");
                dt.Columns.Add("План");

                foreach (DataGridViewRow row in dgvAvariya.Rows)
                {
                    if (row.IsNewRow) continue;

                    bool include = false;
                    string hasPlan = row.Cells["План"].Value?.ToString() ?? "";

                    switch (cmbReportType.SelectedIndex)
                    {
                        case 4: include = true; break;
                        case 5: include = (hasPlan == "❌"); break;
                    }

                    if (include)
                    {
                        dt.Rows.Add(
                            row.Cells["id"].Value,
                            row.Cells["Оборудование"].Value,
                            row.Cells["Дата"].Value,
                            row.Cells["Описание"].Value,
                            row.Cells["Последствия"].Value,
                            row.Cells["Статус"].Value,
                            row.Cells["План"].Value
                        );
                    }
                }
            }

            return dt;
        }

        private void ExportToCsv(DataTable data, string fileName)
        {
            using (StreamWriter sw = new StreamWriter(fileName, false, Encoding.UTF8))
            {
                for (int i = 0; i < data.Columns.Count; i++)
                {
                    sw.Write(data.Columns[i].ColumnName);
                    if (i < data.Columns.Count - 1) sw.Write(";");
                }
                sw.WriteLine();

                foreach (DataRow row in data.Rows)
                {
                    for (int i = 0; i < data.Columns.Count; i++)
                    {
                        sw.Write(row[i].ToString());
                        if (i < data.Columns.Count - 1) sw.Write(";");
                    }
                    sw.WriteLine();
                }
            }
        }

        private void ExportToRtf(DataTable data, string fileName)
        {
            using (StreamWriter sw = new StreamWriter(fileName, false, Encoding.UTF8))
            {
                sw.WriteLine(@"{\rtf1\ansi\deff0");
                sw.WriteLine(@"{\fonttbl{\f0 Times New Roman;}}");
                sw.WriteLine(@"\f0\fs24");

                sw.WriteLine(@"\pard\qc\b\fs32 Отчет\b0\fs24\par");
                sw.WriteLine(@"\pard\qc\fs20 Тип отчета: " + cmbReportType.Text + @"\par");
                sw.WriteLine(@"\pard\qc\fs20 Дата: " + DateTime.Now.ToString("dd.MM.yyyy HH:mm") + @"\par");
                sw.WriteLine(@"\par\par");

                int colWidth = 18000 / Math.Max(data.Columns.Count, 1);

                sw.WriteLine(@"\trowd");
                for (int i = 0; i < data.Columns.Count; i++)
                    sw.WriteLine(@"\cellx" + ((i + 1) * colWidth));

                sw.WriteLine(@"\intbl\b ");
                for (int i = 0; i < data.Columns.Count; i++)
                    sw.Write(data.Columns[i].ColumnName + @" \cell ");
                sw.WriteLine(@"\row\b0");

                foreach (DataRow row in data.Rows)
                {
                    sw.WriteLine(@"\trowd");
                    for (int i = 0; i < data.Columns.Count; i++)
                        sw.WriteLine(@"\cellx" + ((i + 1) * colWidth));

                    sw.WriteLine(@"\intbl ");
                    for (int i = 0; i < data.Columns.Count; i++)
                        sw.Write(row[i].ToString() + @" \cell ");
                    sw.WriteLine(@"\row");
                }

                sw.WriteLine(@"}");
            }

            try { System.Diagnostics.Process.Start(fileName); } catch { }
        }

        private void label3_Click(object sender, EventArgs e)
        {
            // Этот метод можно оставить пустым
            // Он нужен только для того, чтобы удовлетворить привязку в дизайнере
        }

        /// <summary>
        /// Обработчик загрузки формы
        /// </summary>
        private void FormBoss_Load(object sender, EventArgs e)
        {
            // Инициализация при загрузке формы
            try
            {
                // Устанавливаем значения по умолчанию для фильтров
                dtpStart.Value = DateTime.Now.AddMonths(-1);
                dtpEnd.Value = DateTime.Now;

                dtpAvariyaStart.Value = DateTime.Now.AddMonths(-1);
                dtpAvariyaEnd.Value = DateTime.Now;

                // Загружаем данные
                LoadPlans();
                LoadAvariya();
                LoadStatistics();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при загрузке формы: " + ex.Message);
            }
        }
    }
}



