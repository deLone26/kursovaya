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

                            if (dt.Rows.Count > 0)
                                cmbEquipment.SelectedIndex = 0;
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

                            if (dt.Rows.Count > 0)
                                cmbTip.SelectedIndex = 0;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки типов ТО: " + ex.Message);

                // Запасной вариант
                cmbTip.Items.Clear();
                cmbTip.Items.Add("Ежедневное ТО");
                cmbTip.Items.Add("Еженедельное ТО");
                cmbTip.Items.Add("Месячное ТО");
                cmbTip.Items.Add("Квартальное ТО");
                cmbTip.Items.Add("Годовое ТО");
                cmbTip.Items.Add("Текущий ремонт");
                cmbTip.Items.Add("Капитальный ремонт");
                cmbTip.Items.Add("Аварийный ремонт");
                cmbTip.Items.Add("Проверка КИП");
                cmbTip.Items.Add("Регулировка");
                cmbTip.SelectedIndex = 0;
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
                        WHERE dolzhnost ILIKE '%слесар%' OR dolzhnost ILIKE '%Слесар%'
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

                            if (dt.Rows.Count > 0)
                                cmbResponsible.SelectedIndex = 0;
                            else
                                MessageBox.Show("В системе нет слесарей!");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки сотрудников: " + ex.Message);

                // Запасной вариант
                cmbResponsible.Items.Clear();
                cmbResponsible.Items.Add("Иванов Иван Иванович");
                cmbResponsible.Items.Add("Петров Петр Петрович");
                cmbResponsible.Items.Add("Сидоров Сидор Сидорович");
                cmbResponsible.SelectedIndex = 0;
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
                            a.status AS Статус,
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
                            (SELECT COUNT(*) FROM avariya WHERE status != 'Завершена') as need_plan,
                            (SELECT COUNT(*) FROM avariya WHERE status = 'В работе') as avariya_in_progress,
                            (SELECT COUNT(*) FROM avariya WHERE status = 'Завершена') as avariya_completed,
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
                                int total = Convert.ToInt32(reader["total_avariya"]) + Convert.ToInt32(reader["total_plans"]);
                                lblTotal.Text = $"Всего записей: {total}";

                                lblAvariyaTotal.Text = $"Всего аварий: {reader["total_avariya"]}";
                                lblAvariyaNeedPlan.Text = $"Требуют плана: {reader["need_plan"]}";
                                lblAvariyaInProgress.Text = $"В работе: {reader["avariya_in_progress"]}";
                                lblAvariyaCompleted.Text = $"Завершено: {reader["avariya_completed"]}";

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

        // ========== МЕТОДЫ ДЛЯ РАБОТЫ С КОМБОБОКСАМИ ==========

        private int GetSelectedId(ComboBox comboBox, string columnName)
        {
            try
            {
                if (comboBox.SelectedItem == null)
                    return -1;

                if (comboBox.SelectedItem is DataRowView rowView)
                {
                    return Convert.ToInt32(rowView[columnName]);
                }
                return -1;
            }
            catch
            {
                return -1;
            }
        }

        private void SelectEquipmentByName(string equipmentName)
        {
            try
            {
                if (string.IsNullOrEmpty(equipmentName)) return;

                if (cmbEquipment.DataSource is DataTable dt)
                {
                    foreach (DataRowView item in cmbEquipment.Items)
                    {
                        if (item["nazvanie"].ToString() == equipmentName)
                        {
                            cmbEquipment.SelectedItem = item;
                            return;
                        }
                    }
                }

                foreach (var item in cmbEquipment.Items)
                {
                    if (item.ToString() == equipmentName)
                    {
                        cmbEquipment.SelectedItem = item;
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка выбора оборудования: {ex.Message}");
            }
        }

        private void SelectTipByName(string tipName)
        {
            try
            {
                if (string.IsNullOrEmpty(tipName)) return;

                if (cmbTip.DataSource is DataTable dt)
                {
                    foreach (DataRowView item in cmbTip.Items)
                    {
                        if (item["nazvanie"].ToString() == tipName)
                        {
                            cmbTip.SelectedItem = item;
                            return;
                        }
                    }
                }

                foreach (var item in cmbTip.Items)
                {
                    if (item.ToString() == tipName)
                    {
                        cmbTip.SelectedItem = item;
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка выбора типа ТО: {ex.Message}");
            }
        }

        private void SelectResponsibleByName(string responsibleName)
        {
            try
            {
                if (string.IsNullOrEmpty(responsibleName)) return;

                if (cmbResponsible.DataSource is DataTable dt)
                {
                    foreach (DataRowView item in cmbResponsible.Items)
                    {
                        if (item["fio"].ToString() == responsibleName)
                        {
                            cmbResponsible.SelectedItem = item;
                            return;
                        }
                    }
                }

                foreach (var item in cmbResponsible.Items)
                {
                    if (item.ToString() == responsibleName)
                    {
                        cmbResponsible.SelectedItem = item;
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка выбора ответственного: {ex.Message}");
            }
        }

        private void SelectEquipmentById(int equipmentId)
        {
            try
            {
                if (equipmentId <= 0) return;

                try
                {
                    cmbEquipment.SelectedValue = equipmentId;
                    return;
                }
                catch { }

                if (cmbEquipment.DataSource is DataTable dt)
                {
                    foreach (DataRowView item in cmbEquipment.Items)
                    {
                        if (Convert.ToInt32(item["id"]) == equipmentId)
                        {
                            cmbEquipment.SelectedItem = item;
                            return;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка выбора оборудования: {ex.Message}");
            }
        }

        // ========== ОБРАБОТЧИКИ ДЛЯ ПЛАНОВ ==========

        private void DgvPlans_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            try
            {
                DataGridViewRow row = dgvPlans.Rows[e.RowIndex];
                selectedPlanId = Convert.ToInt32(row.Cells["id"].Value);

                string equipment = row.Cells["Оборудование"].Value?.ToString() ?? "";
                string tipTo = row.Cells["Тип_ТО"].Value?.ToString() ?? "";
                string responsible = row.Cells["Ответственный"].Value?.ToString() ?? "";
                string status = row.Cells["Статус"].Value?.ToString() ?? "";

                if (!string.IsNullOrEmpty(equipment))
                    SelectEquipmentByName(equipment);

                if (!string.IsNullOrEmpty(tipTo))
                    SelectTipByName(tipTo);

                if (!string.IsNullOrEmpty(responsible))
                    SelectResponsibleByName(responsible);

                if (!string.IsNullOrEmpty(status))
                {
                    for (int i = 0; i < cmbStatus.Items.Count; i++)
                    {
                        if (cmbStatus.Items[i].ToString() == status)
                        {
                            cmbStatus.SelectedIndex = i;
                            break;
                        }
                    }
                }

                if (row.Cells["Дата_начала"].Value != null)
                    dtpStartRepair.Value = Convert.ToDateTime(row.Cells["Дата_начала"].Value);

                if (row.Cells["Дата_окончания"].Value != null && row.Cells["Дата_окончания"].Value != DBNull.Value)
                    dtpEndRepair.Value = Convert.ToDateTime(row.Cells["Дата_окончания"].Value);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при выборе строки: {ex.Message}");
            }
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

                                tabControl1.SelectedTab = tabPlans;
                                SelectEquipmentById(oborudovanieId);

                                dtpStartRepair.Value = DateTime.Now;
                                dtpEndRepair.Value = DateTime.Now.AddDays(7);
                                cmbStatus.SelectedIndex = 0;

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
            try
            {
                if (dgvPlans.Rows.Count == 0 && dgvAvariya.Rows.Count == 0)
                {
                    MessageBox.Show("Нет данных для экспорта!");
                    return;
                }

                SaveFileDialog save = new SaveFileDialog();
                save.Filter = "CSV files (*.csv)|*.csv";
                save.FileName = $"Отчет_о_планах_ремонтов_проект_Котельная_{DateTime.Now:dd-MM-yyyy}.csv";

                if (save.ShowDialog() == DialogResult.OK)
                {
                    ExportToCsvWithFormatting(save.FileName);
                    MessageBox.Show($"Отчет сохранен!\nФайл: {save.FileName}\n\nОтчет можно открыть в Excel.",
                        "Успешно", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка экспорта: " + ex.Message);
            }
        }

        private void ExportToCsvWithFormatting(string fileName)
        {
            using (StreamWriter sw = new StreamWriter(fileName, false, Encoding.UTF8))
            {
                sw.WriteLine("Отчет о планах ремонтов по проекту: Котельная");
                sw.WriteLine($"Дата составления отчета: {DateTime.Now:dd.MM.yyyy}");

                int totalPlans = dgvPlans.Rows.Count;
                int completedPlans = 0, inProgressPlans = 0, plannedPlans = 0;

                foreach (DataGridViewRow row in dgvPlans.Rows)
                {
                    if (row.IsNewRow) continue;
                    string status = row.Cells["Статус"].Value?.ToString() ?? "";
                    if (status == "Завершен") completedPlans++;
                    else if (status == "В работе") inProgressPlans++;
                    else if (status == "Запланирован") plannedPlans++;
                }

                int totalAvariya = dgvAvariya?.Rows.Count ?? 0;
                int avariyaWithoutPlan = 0;

                if (dgvAvariya != null)
                {
                    foreach (DataGridViewRow row in dgvAvariya.Rows)
                    {
                        if (row.IsNewRow) continue;
                        string hasPlan = row.Cells["План"].Value?.ToString() ?? "";
                        if (hasPlan == "❌") avariyaWithoutPlan++;
                    }
                }

                sw.WriteLine($"Всего планов: {totalPlans}");
                sw.WriteLine($"Выполнено: {completedPlans}");
                sw.WriteLine($"В работе: {inProgressPlans}");
                sw.WriteLine($"Запланировано: {plannedPlans}");
                sw.WriteLine($"Процент выполнения: {(totalPlans > 0 ? (completedPlans * 100 / totalPlans) : 0)}%");
                sw.WriteLine($"Всего аварий: {totalAvariya}");
                sw.WriteLine($"Аварий без плана: {avariyaWithoutPlan}");
                sw.WriteLine();

                sw.WriteLine("ПЛАНЫ РЕМОНТОВ");
                sw.WriteLine();

                string headers = "ID;Оборудование;Тип ТО;Дата начала;Дата окончания;Ответственный;Статус;Связь с аварией";
                sw.WriteLine(headers);

                foreach (DataGridViewRow row in dgvPlans.Rows)
                {
                    if (row.IsNewRow) continue;

                    string startDate = Convert.ToDateTime(row.Cells["Дата_начала"].Value).ToString("dd.MM.yyyy");
                    string endDate = row.Cells["Дата_окончания"].Value != null ?
                        Convert.ToDateTime(row.Cells["Дата_окончания"].Value).ToString("dd.MM.yyyy") : "";

                    string line = $"{row.Cells["id"].Value};" +
                                 $"{row.Cells["Оборудование"].Value};" +
                                 $"{row.Cells["Тип_ТО"].Value};" +
                                 $"{startDate};" +
                                 $"{endDate};" +
                                 $"{row.Cells["Ответственный"].Value};" +
                                 $"{row.Cells["Статус"].Value};" +
                                 $"{row.Cells["Связь_с_аварией"].Value}";

                    sw.WriteLine(line);
                }

                sw.WriteLine();
                sw.WriteLine();

                if (dgvAvariya != null && dgvAvariya.Rows.Count > 0)
                {
                    sw.WriteLine("АВАРИИ");
                    sw.WriteLine();

                    string avHeaders = "ID;Оборудование;Дата;Описание;Последствия;Статус;План";
                    sw.WriteLine(avHeaders);

                    foreach (DataGridViewRow row in dgvAvariya.Rows)
                    {
                        if (row.IsNewRow) continue;

                        string avDate = Convert.ToDateTime(row.Cells["Дата"].Value).ToString("dd.MM.yyyy HH:mm");

                        string line = $"{row.Cells["id"].Value};" +
                                     $"{row.Cells["Оборудование"].Value};" +
                                     $"{avDate};" +
                                     $"{row.Cells["Описание"].Value};" +
                                     $"{row.Cells["Последствия"].Value};" +
                                     $"{row.Cells["Статус"].Value};" +
                                     $"{row.Cells["План"].Value}";

                        sw.WriteLine(line);
                    }
                }

                sw.WriteLine();
                sw.WriteLine($"Отчет сформирован: {DateTime.Now:dd.MM.yyyy HH:mm}");
            }
        }

        private void BtnWord_Click(object sender, EventArgs e)
        {
            try
            {
                if (dgvPlans.Rows.Count == 0)
                {
                    MessageBox.Show("Нет данных для экспорта!");
                    return;
                }

                SaveFileDialog save = new SaveFileDialog();
                save.Filter = "Rich Text Format (*.rtf)|*.rtf";
                save.FileName = $"Отчет_о_планах_ремонтов_{DateTime.Now:dd-MM-yyyy}.rtf";

                if (save.ShowDialog() == DialogResult.OK)
                {
                    ExportToRtfWithFormatting(save.FileName);
                    MessageBox.Show($"Отчет сохранен в формате RTF!\nФайл: {save.FileName}",
                        "Успешно", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    try { System.Diagnostics.Process.Start(save.FileName); } catch { }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка экспорта в Word: " + ex.Message);
            }
        }

        private void ExportToRtfWithFormatting(string fileName)
        {
            using (StreamWriter sw = new StreamWriter(fileName, false, Encoding.UTF8))
            {
                sw.WriteLine(@"{\rtf1\ansi\deff0");
                sw.WriteLine(@"{\fonttbl{\f0 Times New Roman;}{\f1 Arial;}{\f2 Courier New;}}");
                sw.WriteLine(@"\f0\fs24");

                sw.WriteLine(@"\pard\qc\b\fs32 Отчет о планах ремонтов по проекту: Котельная\b0\fs24\par");
                sw.WriteLine(@"\pard\qc\fs20 Дата составления отчета: " + DateTime.Now.ToString("dd.MM.yyyy") + @"\par");
                sw.WriteLine(@"\par");

                int totalPlans = dgvPlans.Rows.Count;
                int completedPlans = 0, inProgressPlans = 0, plannedPlans = 0;

                foreach (DataGridViewRow row in dgvPlans.Rows)
                {
                    if (row.IsNewRow) continue;
                    string status = row.Cells["Статус"].Value?.ToString() ?? "";
                    if (status == "Завершен") completedPlans++;
                    else if (status == "В работе") inProgressPlans++;
                    else if (status == "Запланирован") plannedPlans++;
                }

                sw.WriteLine(@"\pard\box\brdrs\brdrw10 ");
                sw.WriteLine(@"Статистика:\line ");
                sw.WriteLine($"Всего планов: {totalPlans}\\line ");
                sw.WriteLine($"Выполнено: {completedPlans}\\line ");
                sw.WriteLine($"В работе: {inProgressPlans}\\line ");
                sw.WriteLine($"Запланировано: {plannedPlans}\\line ");
                sw.WriteLine($"Процент выполнения: {(totalPlans > 0 ? (completedPlans * 100 / totalPlans) : 0)}%\\par ");
                sw.WriteLine(@"\par\par");

                sw.WriteLine(@"\pard\b\fs28 ПЛАНЫ РЕМОНТОВ\b0\fs24\par");
                sw.WriteLine(@"\par");

                sw.WriteLine(@"\trowd");
                for (int i = 0; i < 8; i++)
                {
                    sw.WriteLine(@"\cellx" + ((i + 1) * 2000));
                }
                sw.WriteLine(@"\clbrdrt\brdrw10\brdrs");
                sw.WriteLine(@"\clbrdrl\brdrw10\brdrs");
                sw.WriteLine(@"\clbrdrb\brdrw10\brdrs");
                sw.WriteLine(@"\clbrdrr\brdrw10\brdrs");
                sw.WriteLine(@"\clcbpat8\cell");
                sw.WriteLine(@"\intbl\b\fs20 ");
                sw.Write(@"ID \cell Оборудование \cell Тип ТО \cell Дата начала \cell Дата окончания \cell Ответственный \cell Статус \cell Связь \cell ");
                sw.WriteLine(@"\row\b0");

                int rowCount = 0;
                foreach (DataGridViewRow row in dgvPlans.Rows)
                {
                    if (row.IsNewRow) continue;

                    sw.WriteLine(@"\trowd");
                    for (int i = 0; i < 8; i++)
                    {
                        sw.WriteLine(@"\cellx" + ((i + 1) * 2000));
                    }
                    sw.WriteLine(@"\clbrdrt\brdrw10\brdrs");
                    sw.WriteLine(@"\clbrdrl\brdrw10\brdrs");
                    sw.WriteLine(@"\clbrdrb\brdrw10\brdrs");
                    sw.WriteLine(@"\clbrdrr\brdrw10\brdrs");

                    if (rowCount % 2 == 0)
                        sw.WriteLine(@"\clcbpat1");

                    sw.WriteLine(@"\cell");
                    sw.WriteLine(@"\intbl\fs20 ");

                    string startDate = Convert.ToDateTime(row.Cells["Дата_начала"].Value).ToString("dd.MM.yyyy");
                    string endDate = row.Cells["Дата_окончания"].Value != null ?
                        Convert.ToDateTime(row.Cells["Дата_окончания"].Value).ToString("dd.MM.yyyy") : "";

                    sw.Write($"{row.Cells["id"].Value} \\cell ");
                    sw.Write($"{row.Cells["Оборудование"].Value} \\cell ");
                    sw.Write($"{row.Cells["Тип_ТО"].Value} \\cell ");
                    sw.Write($"{startDate} \\cell ");
                    sw.Write($"{endDate} \\cell ");
                    sw.Write($"{row.Cells["Ответственный"].Value} \\cell ");
                    sw.Write($"{row.Cells["Статус"].Value} \\cell ");
                    sw.Write($"{row.Cells["Связь_с_аварией"].Value} \\cell ");

                    sw.WriteLine(@"\row");
                    rowCount++;
                }

                sw.WriteLine(@"\trowd");
                for (int i = 0; i < 8; i++)
                {
                    sw.WriteLine(@"\cellx" + ((i + 1) * 2000));
                }
                sw.WriteLine(@"\clbrdrt\brdrw10\brdrs");
                sw.WriteLine(@"\clbrdrl\brdrw10\brdrs");
                sw.WriteLine(@"\clbrdrb\brdrw10\brdrs");
                sw.WriteLine(@"\clbrdrr\brdrw10\brdrs");
                sw.WriteLine(@"\clcbpat8\cell");
                sw.WriteLine(@"\intbl\b\fs20 ");
                sw.Write($"Итого записей: {rowCount} \\cell ");
                for (int i = 1; i < 8; i++)
                {
                    sw.Write(@" \cell ");
                }
                sw.WriteLine(@"\row");

                sw.WriteLine(@"\pard\qr\fs20 Отчет сформирован: " + DateTime.Now.ToString("dd.MM.yyyy HH:mm") + @"\par");
                sw.WriteLine(@"}");
            }
        }

        private void BtnPreview_Click(object sender, EventArgs e)
        {
            int totalPlans = dgvPlans.Rows.Count;
            int completedPlans = 0, inProgressPlans = 0, plannedPlans = 0;

            foreach (DataGridViewRow row in dgvPlans.Rows)
            {
                if (row.IsNewRow) continue;
                string status = row.Cells["Статус"].Value?.ToString() ?? "";
                if (status == "Завершен") completedPlans++;
                else if (status == "В работе") inProgressPlans++;
                else if (status == "Запланирован") plannedPlans++;
            }

            int totalAvariya = dgvAvariya?.Rows.Count ?? 0;
            int avariyaWithoutPlan = 0;

            if (dgvAvariya != null)
            {
                foreach (DataGridViewRow row in dgvAvariya.Rows)
                {
                    if (row.IsNewRow) continue;
                    string hasPlan = row.Cells["План"].Value?.ToString() ?? "";
                    if (hasPlan == "❌") avariyaWithoutPlan++;
                }
            }

            string message = $"╔══════════════════════════════════════════════════════════╗\n" +
                            $"║           ПРЕДПРОСМОТР ОТЧЕТА                           ║\n" +
                            $"╠══════════════════════════════════════════════════════════╣\n" +
                            $"║ Проект: Котельная                                        ║\n" +
                            $"║ Дата: {DateTime.Now:dd.MM.yyyy HH:mm}                              ║\n" +
                            $"╟──────────────────────────────────────────────────────────╢\n" +
                            $"║ СТАТИСТИКА:                                              ║\n" +
                            $"║ Всего планов: {totalPlans,-38} ║\n" +
                            $"║ ├─ Завершено: {completedPlans,-37} ║\n" +
                            $"║ ├─ В работе: {inProgressPlans,-38} ║\n" +
                            $"║ └─ Запланировано: {plannedPlans,-34} ║\n" +
                            $"║ Процент выполнения: {(totalPlans > 0 ? (completedPlans * 100 / totalPlans) : 0),-3}%                                     ║\n" +
                            $"╟──────────────────────────────────────────────────────────╢\n" +
                            $"║ АВАРИИ:                                                  ║\n" +
                            $"║ Всего аварий: {totalAvariya,-39} ║\n" +
                            $"║ Аварий без плана: {avariyaWithoutPlan,-35} ║\n" +
                            $"╟──────────────────────────────────────────────────────────╢\n" +
                            $"║ ФОРМАТЫ ЭКСПОРТА:                                        ║\n" +
                            $"║ Excel: CSV файл (открывается в Excel)                    ║\n" +
                            $"║ Word:  RTF файл (открывается в Word)                     ║\n" +
                            $"╚══════════════════════════════════════════════════════════╝";

            MessageBox.Show(message, "Предпросмотр отчета",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
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

            try { cmbEquipment.SelectedIndex = -1; } catch { }
            try { cmbTip.SelectedIndex = -1; } catch { }
            try { cmbResponsible.SelectedIndex = -1; } catch { }

            if (cmbStatus.Items.Count > 0)
                cmbStatus.SelectedIndex = 0;

            dtpStartRepair.Value = DateTime.Now;
            dtpEndRepair.Value = DateTime.Now.AddDays(7);
        }

        // ========== МЕТОДЫ ИЗ ДИЗАЙНЕРА ==========
        private void label3_Click(object sender, EventArgs e) { }

        private void FormBoss_Load(object sender, EventArgs e)
        {
            dtpStart.Value = DateTime.Now.AddMonths(-1);
            dtpEnd.Value = DateTime.Now;
            dtpAvariyaStart.Value = DateTime.Now.AddMonths(-1);
            dtpAvariyaEnd.Value = DateTime.Now;
        }
    }
}



