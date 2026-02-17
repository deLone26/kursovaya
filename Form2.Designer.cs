namespace WindowsFormsApp1
{
    partial class Form2
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            btnRefresh = new System.Windows.Forms.Button();
            txtSearch = new System.Windows.Forms.TextBox();
            label19 = new System.Windows.Forms.Label();
            btnSearch = new System.Windows.Forms.Button();
            btnDelete = new System.Windows.Forms.Button();
            btnUpdate = new System.Windows.Forms.Button();
            dataGridView1 = new System.Windows.Forms.DataGridView();
            btnAdd = new System.Windows.Forms.Button();
            txtEmail = new System.Windows.Forms.TextBox();
            txtTelefon = new System.Windows.Forms.TextBox();
            txtDolzhnost = new System.Windows.Forms.TextBox();
            txtOtchestvo = new System.Windows.Forms.TextBox();
            txtImya = new System.Windows.Forms.TextBox();
            txtFamiliya = new System.Windows.Forms.TextBox();
            label8 = new System.Windows.Forms.Label();
            label7 = new System.Windows.Forms.Label();
            label6 = new System.Windows.Forms.Label();
            label5 = new System.Windows.Forms.Label();
            label4 = new System.Windows.Forms.Label();
            label3 = new System.Windows.Forms.Label();
            label2 = new System.Windows.Forms.Label();
            label1 = new System.Windows.Forms.Label();
            txtId = new System.Windows.Forms.TextBox();
            txtLogin = new System.Windows.Forms.TextBox();
            txtPassword = new System.Windows.Forms.TextBox();
            label9 = new System.Windows.Forms.Label();
            label10 = new System.Windows.Forms.Label();
            cmbRole = new System.Windows.Forms.ComboBox();
            label11 = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)dataGridView1).BeginInit();
            SuspendLayout();
            // 
            // btnRefresh
            // 
            btnRefresh.Location = new System.Drawing.Point(25, 615);
            btnRefresh.Margin = new System.Windows.Forms.Padding(6, 5, 6, 5);
            btnRefresh.Name = "btnRefresh";
            btnRefresh.Size = new System.Drawing.Size(569, 45);
            btnRefresh.TabIndex = 69;
            btnRefresh.Text = "Обновить таблицу";
            btnRefresh.UseVisualStyleBackColor = true;
            btnRefresh.Click += btnRefresh_Click;
            // 
            // txtSearch
            // 
            txtSearch.Location = new System.Drawing.Point(364, 553);
            txtSearch.Name = "txtSearch";
            txtSearch.Size = new System.Drawing.Size(99, 31);
            txtSearch.TabIndex = 68;
            // 
            // label19
            // 
            label19.AutoSize = true;
            label19.Location = new System.Drawing.Point(324, 556);
            label19.Name = "label19";
            label19.Size = new System.Drawing.Size(34, 25);
            label19.TabIndex = 67;
            label19.Text = "ID:";
            // 
            // btnSearch
            // 
            btnSearch.Location = new System.Drawing.Point(482, 552);
            btnSearch.Name = "btnSearch";
            btnSearch.Size = new System.Drawing.Size(112, 34);
            btnSearch.TabIndex = 66;
            btnSearch.Text = "Найти";
            btnSearch.UseVisualStyleBackColor = true;
            btnSearch.Click += btnSearch_Click;
            // 
            // btnDelete
            // 
            btnDelete.Location = new System.Drawing.Point(25, 551);
            btnDelete.Margin = new System.Windows.Forms.Padding(6, 5, 6, 5);
            btnDelete.Name = "btnDelete";
            btnDelete.Size = new System.Drawing.Size(262, 35);
            btnDelete.TabIndex = 63;
            btnDelete.Text = "Удалить";
            btnDelete.UseVisualStyleBackColor = true;
            btnDelete.Click += btnDelete_Click;
            // 
            // btnUpdate
            // 
            btnUpdate.Location = new System.Drawing.Point(649, 630);
            btnUpdate.Margin = new System.Windows.Forms.Padding(6, 5, 6, 5);
            btnUpdate.Name = "btnUpdate";
            btnUpdate.Size = new System.Drawing.Size(508, 45);
            btnUpdate.TabIndex = 45;
            btnUpdate.Text = "Изменить запись";
            btnUpdate.UseVisualStyleBackColor = true;
            btnUpdate.Click += btnUpdate_Click;
            // 
            // dataGridView1
            // 
            dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridView1.Location = new System.Drawing.Point(25, 24);
            dataGridView1.Margin = new System.Windows.Forms.Padding(6, 5, 6, 5);
            dataGridView1.Name = "dataGridView1";
            dataGridView1.RowHeadersWidth = 62;
            dataGridView1.Size = new System.Drawing.Size(578, 510);
            dataGridView1.TabIndex = 44;
            dataGridView1.CellClick += dataGridView1_CellClick;
            dataGridView1.CellContentClick += dataGridView1_CellContentClick;
            // 
            // btnAdd
            // 
            btnAdd.Location = new System.Drawing.Point(649, 556);
            btnAdd.Margin = new System.Windows.Forms.Padding(6, 5, 6, 5);
            btnAdd.Name = "btnAdd";
            btnAdd.Size = new System.Drawing.Size(508, 45);
            btnAdd.TabIndex = 43;
            btnAdd.Text = "Добавить";
            btnAdd.UseVisualStyleBackColor = true;
            btnAdd.Click += btnAdd_Click;
            // 
            // txtEmail
            // 
            txtEmail.Location = new System.Drawing.Point(809, 299);
            txtEmail.Name = "txtEmail";
            txtEmail.Size = new System.Drawing.Size(330, 31);
            txtEmail.TabIndex = 86;
            // 
            // txtTelefon
            // 
            txtTelefon.Location = new System.Drawing.Point(809, 254);
            txtTelefon.Name = "txtTelefon";
            txtTelefon.Size = new System.Drawing.Size(330, 31);
            txtTelefon.TabIndex = 85;
            // 
            // txtDolzhnost
            // 
            txtDolzhnost.Location = new System.Drawing.Point(809, 213);
            txtDolzhnost.Name = "txtDolzhnost";
            txtDolzhnost.Size = new System.Drawing.Size(330, 31);
            txtDolzhnost.TabIndex = 84;
            // 
            // txtOtchestvo
            // 
            txtOtchestvo.Location = new System.Drawing.Point(809, 170);
            txtOtchestvo.Name = "txtOtchestvo";
            txtOtchestvo.Size = new System.Drawing.Size(330, 31);
            txtOtchestvo.TabIndex = 83;
            // 
            // txtImya
            // 
            txtImya.Location = new System.Drawing.Point(809, 131);
            txtImya.Name = "txtImya";
            txtImya.Size = new System.Drawing.Size(330, 31);
            txtImya.TabIndex = 82;
            // 
            // txtFamiliya
            // 
            txtFamiliya.Location = new System.Drawing.Point(809, 86);
            txtFamiliya.Name = "txtFamiliya";
            txtFamiliya.Size = new System.Drawing.Size(330, 31);
            txtFamiliya.TabIndex = 81;
            // 
            // label8
            // 
            label8.AutoSize = true;
            label8.Location = new System.Drawing.Point(631, 297);
            label8.Name = "label8";
            label8.Size = new System.Drawing.Size(54, 25);
            label8.TabIndex = 80;
            label8.Text = "email";
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.Location = new System.Drawing.Point(631, 257);
            label7.Name = "label7";
            label7.Size = new System.Drawing.Size(150, 25);
            label7.TabIndex = 79;
            label7.Text = "Номер телефона";
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Location = new System.Drawing.Point(631, 216);
            label6.Name = "label6";
            label6.Size = new System.Drawing.Size(102, 25);
            label6.TabIndex = 78;
            label6.Text = "Должность";
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new System.Drawing.Point(631, 173);
            label5.Name = "label5";
            label5.Size = new System.Drawing.Size(88, 25);
            label5.TabIndex = 77;
            label5.Text = "Отчество";
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new System.Drawing.Point(630, 134);
            label4.Name = "label4";
            label4.Size = new System.Drawing.Size(47, 25);
            label4.TabIndex = 76;
            label4.Text = "Имя";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new System.Drawing.Point(630, 92);
            label3.Name = "label3";
            label3.Size = new System.Drawing.Size(85, 25);
            label3.TabIndex = 75;
            label3.Text = "Фамилия";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new System.Drawing.Point(809, 9);
            label2.Name = "label2";
            label2.Size = new System.Drawing.Size(154, 25);
            label2.TabIndex = 74;
            label2.Text = "Добавить запись ";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new System.Drawing.Point(631, 49);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(129, 25);
            label1.TabIndex = 73;
            label1.Text = "ID сотрудника";
            // 
            // txtId
            // 
            txtId.Location = new System.Drawing.Point(809, 46);
            txtId.Name = "txtId";
            txtId.Size = new System.Drawing.Size(330, 31);
            txtId.TabIndex = 72;
            // 
            // txtLogin
            // 
            txtLogin.Location = new System.Drawing.Point(809, 348);
            txtLogin.Name = "txtLogin";
            txtLogin.Size = new System.Drawing.Size(330, 31);
            txtLogin.TabIndex = 87;
            // 
            // txtPassword
            // 
            txtPassword.Location = new System.Drawing.Point(809, 399);
            txtPassword.Name = "txtPassword";
            txtPassword.Size = new System.Drawing.Size(330, 31);
            txtPassword.TabIndex = 88;
            txtPassword.UseSystemPasswordChar = true;
            // 
            // label9
            // 
            label9.AutoSize = true;
            label9.Location = new System.Drawing.Point(631, 351);
            label9.Name = "label9";
            label9.Size = new System.Drawing.Size(62, 25);
            label9.TabIndex = 89;
            label9.Text = "Логин";
            // 
            // label10
            // 
            label10.AutoSize = true;
            label10.Location = new System.Drawing.Point(631, 399);
            label10.Name = "label10";
            label10.Size = new System.Drawing.Size(74, 25);
            label10.TabIndex = 90;
            label10.Text = "Пароль";
            // 
            // cmbRole
            // 
            cmbRole.FormattingEnabled = true;
            cmbRole.Items.AddRange(new object[] { "AddRange(roles)" });
            cmbRole.Location = new System.Drawing.Point(809, 449);
            cmbRole.Name = "cmbRole";
            cmbRole.Size = new System.Drawing.Size(329, 33);
            cmbRole.TabIndex = 91;
            // 
            // label11
            // 
            label11.AutoSize = true;
            label11.Location = new System.Drawing.Point(631, 452);
            label11.Name = "label11";
            label11.Size = new System.Drawing.Size(102, 25);
            label11.TabIndex = 92;
            label11.Text = "Должность";
            // 
            // Form2
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(10F, 25F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(1273, 860);
            Controls.Add(label11);
            Controls.Add(cmbRole);
            Controls.Add(label10);
            Controls.Add(label9);
            Controls.Add(txtPassword);
            Controls.Add(txtLogin);
            Controls.Add(txtEmail);
            Controls.Add(txtTelefon);
            Controls.Add(txtDolzhnost);
            Controls.Add(txtOtchestvo);
            Controls.Add(txtImya);
            Controls.Add(txtFamiliya);
            Controls.Add(label8);
            Controls.Add(label7);
            Controls.Add(label6);
            Controls.Add(label5);
            Controls.Add(label4);
            Controls.Add(label3);
            Controls.Add(label2);
            Controls.Add(label1);
            Controls.Add(txtId);
            Controls.Add(btnRefresh);
            Controls.Add(txtSearch);
            Controls.Add(label19);
            Controls.Add(btnSearch);
            Controls.Add(btnDelete);
            Controls.Add(btnUpdate);
            Controls.Add(dataGridView1);
            Controls.Add(btnAdd);
            Name = "Form2";
            Text = "Form2";
            ((System.ComponentModel.ISupportInitialize)dataGridView1).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private System.Windows.Forms.Button btnRefresh;
        private System.Windows.Forms.TextBox txtSearch;
        private System.Windows.Forms.Label label19;
        private System.Windows.Forms.Button btnSearch;
        private System.Windows.Forms.Button btnDelete;
        private System.Windows.Forms.Button btnUpdate;
        private System.Windows.Forms.DataGridView dataGridView1;
        private System.Windows.Forms.Button btnAdd;
        private System.Windows.Forms.TextBox txtEmail;
        private System.Windows.Forms.TextBox txtTelefon;
        private System.Windows.Forms.TextBox txtDolzhnost;
        private System.Windows.Forms.TextBox txtOtchestvo;
        private System.Windows.Forms.TextBox txtImya;
        private System.Windows.Forms.TextBox txtFamiliya;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox txtId;
        private System.Windows.Forms.TextBox txtLogin;
        private System.Windows.Forms.TextBox txtPassword;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.ComboBox cmbRole;
        private System.Windows.Forms.Label label11;
    }
}