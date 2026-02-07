namespace WindowsFormsApp1
{
    partial class Form1
    {
        /// <summary>
        /// Обязательная переменная конструктора.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Освободить все используемые ресурсы.
        /// </summary>
        /// <param name="disposing">истинно, если управляемый ресурс должен быть удален; иначе ложно.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Код, автоматически созданный конструктором форм Windows

        /// <summary>
        /// Требуемый метод для поддержки конструктора — не изменяйте 
        /// содержимое этого метода с помощью редактора кода.
        /// </summary>
        private void InitializeComponent()
        {
            btnAdd = new System.Windows.Forms.Button();
            dataGridView1 = new System.Windows.Forms.DataGridView();
            txtNazvanie = new System.Windows.Forms.TextBox();
            label1 = new System.Windows.Forms.Label();
            label2 = new System.Windows.Forms.Label();
            label3 = new System.Windows.Forms.Label();
            label4 = new System.Windows.Forms.Label();
            label5 = new System.Windows.Forms.Label();
            label6 = new System.Windows.Forms.Label();
            label7 = new System.Windows.Forms.Label();
            label8 = new System.Windows.Forms.Label();
            txtTip = new System.Windows.Forms.TextBox();
            txtModel = new System.Windows.Forms.TextBox();
            txtSeria = new System.Windows.Forms.TextBox();
            txtMesto = new System.Windows.Forms.TextBox();
            txtMoshnost = new System.Windows.Forms.TextBox();
            txtDavlenie = new System.Windows.Forms.TextBox();
            btnUpdate = new System.Windows.Forms.Button();
            label14 = new System.Windows.Forms.Label();
            label15 = new System.Windows.Forms.Label();
            label16 = new System.Windows.Forms.Label();
            txtProizvoditel = new System.Windows.Forms.TextBox();
            label17 = new System.Windows.Forms.Label();
            txtSelectedId = new System.Windows.Forms.TextBox();
            btnDelete = new System.Windows.Forms.Button();
            button4 = new System.Windows.Forms.Button();
            label19 = new System.Windows.Forms.Label();
            btnRefresh = new System.Windows.Forms.Button();
            fileSystemWatcher1 = new System.IO.FileSystemWatcher();
            cmbStatus = new System.Windows.Forms.ComboBox();
            txtSearch = new System.Windows.Forms.TextBox();
            label9 = new System.Windows.Forms.Label();
            label10 = new System.Windows.Forms.Label();
            txtDataUstanov = new System.Windows.Forms.TextBox();
            ((System.ComponentModel.ISupportInitialize)dataGridView1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)fileSystemWatcher1).BeginInit();
            SuspendLayout();
            // 
            // btnAdd
            // 
            btnAdd.Location = new System.Drawing.Point(679, 490);
            btnAdd.Margin = new System.Windows.Forms.Padding(6, 5, 6, 5);
            btnAdd.Name = "btnAdd";
            btnAdd.Size = new System.Drawing.Size(509, 45);
            btnAdd.TabIndex = 0;
            btnAdd.Text = "Добавить";
            btnAdd.UseVisualStyleBackColor = true;
            btnAdd.Click += button1_Click;
            // 
            // dataGridView1
            // 
            dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridView1.Location = new System.Drawing.Point(14, 33);
            dataGridView1.Margin = new System.Windows.Forms.Padding(6, 5, 6, 5);
            dataGridView1.Name = "dataGridView1";
            dataGridView1.RowHeadersWidth = 62;
            dataGridView1.Size = new System.Drawing.Size(579, 510);
            dataGridView1.TabIndex = 1;
            dataGridView1.CellContentClick += dataGridView1_CellContentClick;
            // 
            // txtNazvanie
            // 
            txtNazvanie.Location = new System.Drawing.Point(830, 75);
            txtNazvanie.Name = "txtNazvanie";
            txtNazvanie.Size = new System.Drawing.Size(330, 31);
            txtNazvanie.TabIndex = 2;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new System.Drawing.Point(623, 163);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(76, 25);
            label1.TabIndex = 3;
            label1.Text = "Модель";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new System.Drawing.Point(787, 9);
            label2.Name = "label2";
            label2.Size = new System.Drawing.Size(154, 25);
            label2.TabIndex = 4;
            label2.Text = "Добавить запись ";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new System.Drawing.Point(632, 81);
            label3.Name = "label3";
            label3.Size = new System.Drawing.Size(90, 25);
            label3.TabIndex = 5;
            label3.Text = "Название";
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new System.Drawing.Point(623, 121);
            label4.Name = "label4";
            label4.Size = new System.Drawing.Size(167, 25);
            label4.TabIndex = 6;
            label4.Text = "Тип оборудования";
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new System.Drawing.Point(623, 202);
            label5.Name = "label5";
            label5.Size = new System.Drawing.Size(0, 25);
            label5.TabIndex = 7;
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Location = new System.Drawing.Point(623, 245);
            label6.Name = "label6";
            label6.Size = new System.Drawing.Size(63, 25);
            label6.TabIndex = 8;
            label6.Text = "Место";
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.Location = new System.Drawing.Point(623, 287);
            label7.Name = "label7";
            label7.Size = new System.Drawing.Size(99, 25);
            label7.TabIndex = 9;
            label7.Text = "Мощность";
            // 
            // label8
            // 
            label8.AutoSize = true;
            label8.Location = new System.Drawing.Point(623, 330);
            label8.Name = "label8";
            label8.Size = new System.Drawing.Size(90, 25);
            label8.TabIndex = 10;
            label8.Text = "Давление";
            // 
            // txtTip
            // 
            txtTip.Location = new System.Drawing.Point(830, 115);
            txtTip.Name = "txtTip";
            txtTip.Size = new System.Drawing.Size(330, 31);
            txtTip.TabIndex = 11;
            // 
            // txtModel
            // 
            txtModel.Location = new System.Drawing.Point(830, 160);
            txtModel.Name = "txtModel";
            txtModel.Size = new System.Drawing.Size(330, 31);
            txtModel.TabIndex = 12;
            // 
            // txtSeria
            // 
            txtSeria.Location = new System.Drawing.Point(830, 198);
            txtSeria.Name = "txtSeria";
            txtSeria.Size = new System.Drawing.Size(330, 31);
            txtSeria.TabIndex = 13;
            // 
            // txtMesto
            // 
            txtMesto.Location = new System.Drawing.Point(830, 242);
            txtMesto.Name = "txtMesto";
            txtMesto.Size = new System.Drawing.Size(330, 31);
            txtMesto.TabIndex = 14;
            // 
            // txtMoshnost
            // 
            txtMoshnost.Location = new System.Drawing.Point(830, 283);
            txtMoshnost.Name = "txtMoshnost";
            txtMoshnost.Size = new System.Drawing.Size(330, 31);
            txtMoshnost.TabIndex = 15;
            // 
            // txtDavlenie
            // 
            txtDavlenie.Location = new System.Drawing.Point(830, 327);
            txtDavlenie.Name = "txtDavlenie";
            txtDavlenie.Size = new System.Drawing.Size(330, 31);
            txtDavlenie.TabIndex = 16;
            // 
            // btnUpdate
            // 
            btnUpdate.Location = new System.Drawing.Point(632, 623);
            btnUpdate.Margin = new System.Windows.Forms.Padding(6, 5, 6, 5);
            btnUpdate.Name = "btnUpdate";
            btnUpdate.Size = new System.Drawing.Size(509, 45);
            btnUpdate.TabIndex = 17;
            btnUpdate.Text = "Изменить запись";
            btnUpdate.UseVisualStyleBackColor = true;
            btnUpdate.Click += button2_Click;
            // 
            // label14
            // 
            label14.AutoSize = true;
            label14.Location = new System.Drawing.Point(636, 449);
            label14.Name = "label14";
            label14.Size = new System.Drawing.Size(63, 25);
            label14.TabIndex = 21;
            label14.Text = "Статус";
            label14.Click += label14_Click;
            // 
            // label15
            // 
            label15.AutoSize = true;
            label15.Location = new System.Drawing.Point(776, 549);
            label15.Name = "label15";
            label15.Size = new System.Drawing.Size(157, 25);
            label15.TabIndex = 20;
            label15.Text = "Обновить запись ";
            // 
            // label16
            // 
            label16.AutoSize = true;
            label16.Location = new System.Drawing.Point(605, 370);
            label16.Name = "label16";
            label16.Size = new System.Drawing.Size(140, 25);
            label16.TabIndex = 19;
            label16.Text = "Производитель";
            // 
            // txtProizvoditel
            // 
            txtProizvoditel.Location = new System.Drawing.Point(830, 364);
            txtProizvoditel.Name = "txtProizvoditel";
            txtProizvoditel.Size = new System.Drawing.Size(330, 31);
            txtProizvoditel.TabIndex = 18;
            // 
            // label17
            // 
            label17.AutoSize = true;
            label17.Location = new System.Drawing.Point(623, 39);
            label17.Name = "label17";
            label17.Size = new System.Drawing.Size(129, 25);
            label17.TabIndex = 33;
            label17.Text = "Номер записи";
            // 
            // txtSelectedId
            // 
            txtSelectedId.Location = new System.Drawing.Point(833, 33);
            txtSelectedId.Name = "txtSelectedId";
            txtSelectedId.Size = new System.Drawing.Size(328, 31);
            txtSelectedId.TabIndex = 34;
            // 
            // btnDelete
            // 
            btnDelete.Location = new System.Drawing.Point(15, 562);
            btnDelete.Margin = new System.Windows.Forms.Padding(6, 5, 6, 5);
            btnDelete.Name = "btnDelete";
            btnDelete.Size = new System.Drawing.Size(269, 33);
            btnDelete.TabIndex = 35;
            btnDelete.Text = "Удалить запись";
            btnDelete.UseVisualStyleBackColor = true;
            btnDelete.Click += button3_Click;
            // 
            // button4
            // 
            button4.Location = new System.Drawing.Point(471, 562);
            button4.Name = "button4";
            button4.Size = new System.Drawing.Size(111, 33);
            button4.TabIndex = 38;
            button4.Text = "Найти";
            button4.UseVisualStyleBackColor = true;
            button4.Click += button4_Click;
            // 
            // label19
            // 
            label19.AutoSize = true;
            label19.Location = new System.Drawing.Point(314, 565);
            label19.Name = "label19";
            label19.Size = new System.Drawing.Size(34, 25);
            label19.TabIndex = 39;
            label19.Text = "ID:";
            // 
            // btnRefresh
            // 
            btnRefresh.Location = new System.Drawing.Point(14, 623);
            btnRefresh.Margin = new System.Windows.Forms.Padding(6, 5, 6, 5);
            btnRefresh.Name = "btnRefresh";
            btnRefresh.Size = new System.Drawing.Size(569, 45);
            btnRefresh.TabIndex = 41;
            btnRefresh.Text = "Обновить таблицу";
            btnRefresh.UseVisualStyleBackColor = true;
            btnRefresh.Click += button5_Click;
            // 
            // fileSystemWatcher1
            // 
            fileSystemWatcher1.EnableRaisingEvents = true;
            fileSystemWatcher1.SynchronizingObject = this;
            // 
            // cmbStatus
            // 
            cmbStatus.FormattingEnabled = true;
            cmbStatus.Location = new System.Drawing.Point(834, 449);
            cmbStatus.Name = "cmbStatus";
            cmbStatus.Size = new System.Drawing.Size(329, 33);
            cmbStatus.TabIndex = 43;
            // 
            // txtSearch
            // 
            txtSearch.Location = new System.Drawing.Point(352, 560);
            txtSearch.Name = "txtSearch";
            txtSearch.Size = new System.Drawing.Size(113, 31);
            txtSearch.TabIndex = 44;
            // 
            // label9
            // 
            label9.AutoSize = true;
            label9.Location = new System.Drawing.Point(623, 204);
            label9.Name = "label9";
            label9.Size = new System.Drawing.Size(123, 25);
            label9.TabIndex = 45;
            label9.Text = "Серия\\номер";
            // 
            // label10
            // 
            label10.AutoSize = true;
            label10.Location = new System.Drawing.Point(615, 410);
            label10.Name = "label10";
            label10.Size = new System.Drawing.Size(137, 25);
            label10.TabIndex = 46;
            label10.Text = "Дата установки";
            label10.Click += label10_Click;
            // 
            // txtDataUstanov
            // 
            txtDataUstanov.Location = new System.Drawing.Point(832, 407);
            txtDataUstanov.Name = "txtDataUstanov";
            txtDataUstanov.Size = new System.Drawing.Size(329, 31);
            txtDataUstanov.TabIndex = 47;
            // 
            // Form1
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(10F, 25F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(1214, 865);
            Controls.Add(txtDataUstanov);
            Controls.Add(label10);
            Controls.Add(label9);
            Controls.Add(txtSearch);
            Controls.Add(cmbStatus);
            Controls.Add(btnRefresh);
            Controls.Add(label19);
            Controls.Add(button4);
            Controls.Add(btnDelete);
            Controls.Add(txtSelectedId);
            Controls.Add(label17);
            Controls.Add(label14);
            Controls.Add(label15);
            Controls.Add(label16);
            Controls.Add(txtProizvoditel);
            Controls.Add(btnUpdate);
            Controls.Add(txtDavlenie);
            Controls.Add(txtMoshnost);
            Controls.Add(txtMesto);
            Controls.Add(txtSeria);
            Controls.Add(txtModel);
            Controls.Add(txtTip);
            Controls.Add(label8);
            Controls.Add(label7);
            Controls.Add(label6);
            Controls.Add(label5);
            Controls.Add(label4);
            Controls.Add(label3);
            Controls.Add(label2);
            Controls.Add(label1);
            Controls.Add(txtNazvanie);
            Controls.Add(dataGridView1);
            Controls.Add(btnAdd);
            Margin = new System.Windows.Forms.Padding(6, 5, 6, 5);
            Name = "Form1";
            Text = "Form1";
            Load += Form1_Load;
            ((System.ComponentModel.ISupportInitialize)dataGridView1).EndInit();
            ((System.ComponentModel.ISupportInitialize)fileSystemWatcher1).EndInit();
            ResumeLayout(false);
            PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnAdd;
        private System.Windows.Forms.DataGridView dataGridView1;
        private System.Windows.Forms.TextBox txtNazvanie;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.TextBox txtTip;
        private System.Windows.Forms.TextBox txtModel;
        private System.Windows.Forms.TextBox txtSeria;
        private System.Windows.Forms.TextBox txtMesto;
        private System.Windows.Forms.TextBox txtMoshnost;
        private System.Windows.Forms.TextBox txtDavlenie;
        private System.Windows.Forms.Button btnUpdate;
        private System.Windows.Forms.Label label14;
        private System.Windows.Forms.Label label15;
        private System.Windows.Forms.Label label16;
        private System.Windows.Forms.TextBox txtProizvoditel;
        private System.Windows.Forms.Label label17;
        private System.Windows.Forms.TextBox txtSelectedId;
        private System.Windows.Forms.Button btnDelete;
        private System.Windows.Forms.Button button4;
        private System.Windows.Forms.Label label19;
        private System.Windows.Forms.Button btnRefresh;
        private System.IO.FileSystemWatcher fileSystemWatcher1;
        private System.Windows.Forms.ComboBox cmbStatus;
        private System.Windows.Forms.TextBox txtSearch;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.TextBox txtDataUstanov;
    }
}

