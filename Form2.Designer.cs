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
            btnRefresh.Location = new System.Drawing.Point(18, 369);
            btnRefresh.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            btnRefresh.Name = "btnRefresh";
            btnRefresh.Size = new System.Drawing.Size(398, 27);
            btnRefresh.TabIndex = 69;
            btnRefresh.Text = "Обновить таблицу";
            btnRefresh.UseVisualStyleBackColor = true;
            btnRefresh.Click += btnRefresh_Click;
            // 
            // txtSearch
            // 
            txtSearch.Location = new System.Drawing.Point(255, 332);
            txtSearch.Margin = new System.Windows.Forms.Padding(2);
            txtSearch.Name = "txtSearch";
            txtSearch.Size = new System.Drawing.Size(70, 23);
            txtSearch.TabIndex = 68;
            // 
            // label19
            // 
            label19.AutoSize = true;
            label19.Location = new System.Drawing.Point(227, 334);
            label19.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            label19.Name = "label19";
            label19.Size = new System.Drawing.Size(21, 15);
            label19.TabIndex = 67;
            label19.Text = "ID:";
            // 
            // btnSearch
            // 
            btnSearch.Location = new System.Drawing.Point(337, 331);
            btnSearch.Margin = new System.Windows.Forms.Padding(2);
            btnSearch.Name = "btnSearch";
            btnSearch.Size = new System.Drawing.Size(78, 20);
            btnSearch.TabIndex = 66;
            btnSearch.Text = "Найти";
            btnSearch.UseVisualStyleBackColor = true;
            btnSearch.Click += btnSearch_Click;
            // 
            // btnDelete
            // 
            btnDelete.Location = new System.Drawing.Point(18, 331);
            btnDelete.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            btnDelete.Name = "btnDelete";
            btnDelete.Size = new System.Drawing.Size(183, 21);
            btnDelete.TabIndex = 63;
            btnDelete.Text = "Удалить";
            btnDelete.UseVisualStyleBackColor = true;
            btnDelete.Click += btnDelete_Click;
            // 
            // btnUpdate
            // 
            btnUpdate.Location = new System.Drawing.Point(442, 372);
            btnUpdate.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            btnUpdate.Name = "btnUpdate";
            btnUpdate.Size = new System.Drawing.Size(356, 27);
            btnUpdate.TabIndex = 45;
            btnUpdate.Text = "Изменить запись";
            btnUpdate.UseVisualStyleBackColor = true;
            btnUpdate.Click += btnUpdate_Click;
            // 
            // dataGridView1
            // 
            dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridView1.Location = new System.Drawing.Point(18, 14);
            dataGridView1.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            dataGridView1.Name = "dataGridView1";
            dataGridView1.RowHeadersWidth = 62;
            dataGridView1.Size = new System.Drawing.Size(405, 306);
            dataGridView1.TabIndex = 44;
            dataGridView1.CellClick += dataGridView1_CellClick;
            dataGridView1.CellContentClick += dataGridView1_CellContentClick;
            // 
            // btnAdd
            // 
            btnAdd.Location = new System.Drawing.Point(442, 328);
            btnAdd.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            btnAdd.Name = "btnAdd";
            btnAdd.Size = new System.Drawing.Size(356, 27);
            btnAdd.TabIndex = 43;
            btnAdd.Text = "Добавить";
            btnAdd.UseVisualStyleBackColor = true;
            btnAdd.Click += btnAdd_Click;
            // 
            // txtEmail
            // 
            txtEmail.Location = new System.Drawing.Point(566, 179);
            txtEmail.Margin = new System.Windows.Forms.Padding(2);
            txtEmail.Name = "txtEmail";
            txtEmail.Size = new System.Drawing.Size(232, 23);
            txtEmail.TabIndex = 86;
            // 
            // txtTelefon
            // 
            txtTelefon.Location = new System.Drawing.Point(566, 152);
            txtTelefon.Margin = new System.Windows.Forms.Padding(2);
            txtTelefon.Name = "txtTelefon";
            txtTelefon.Size = new System.Drawing.Size(232, 23);
            txtTelefon.TabIndex = 85;
            // 
            // txtDolzhnost
            // 
            txtDolzhnost.Location = new System.Drawing.Point(566, 127);
            txtDolzhnost.Margin = new System.Windows.Forms.Padding(2);
            txtDolzhnost.Name = "txtDolzhnost";
            txtDolzhnost.Size = new System.Drawing.Size(232, 23);
            txtDolzhnost.TabIndex = 84;
            // 
            // txtOtchestvo
            // 
            txtOtchestvo.Location = new System.Drawing.Point(566, 102);
            txtOtchestvo.Margin = new System.Windows.Forms.Padding(2);
            txtOtchestvo.Name = "txtOtchestvo";
            txtOtchestvo.Size = new System.Drawing.Size(232, 23);
            txtOtchestvo.TabIndex = 83;
            // 
            // txtImya
            // 
            txtImya.Location = new System.Drawing.Point(566, 77);
            txtImya.Margin = new System.Windows.Forms.Padding(2);
            txtImya.Name = "txtImya";
            txtImya.Size = new System.Drawing.Size(232, 23);
            txtImya.TabIndex = 82;
            // 
            // txtFamiliya
            // 
            txtFamiliya.Location = new System.Drawing.Point(566, 52);
            txtFamiliya.Margin = new System.Windows.Forms.Padding(2);
            txtFamiliya.Name = "txtFamiliya";
            txtFamiliya.Size = new System.Drawing.Size(232, 23);
            txtFamiliya.TabIndex = 81;
            // 
            // label8
            // 
            label8.AutoSize = true;
            label8.Location = new System.Drawing.Point(442, 178);
            label8.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            label8.Name = "label8";
            label8.Size = new System.Drawing.Size(36, 15);
            label8.TabIndex = 80;
            label8.Text = "email";
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.Location = new System.Drawing.Point(442, 154);
            label7.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            label7.Name = "label7";
            label7.Size = new System.Drawing.Size(101, 15);
            label7.TabIndex = 79;
            label7.Text = "Номер телефона";
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Location = new System.Drawing.Point(442, 130);
            label6.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            label6.Name = "label6";
            label6.Size = new System.Drawing.Size(69, 15);
            label6.TabIndex = 78;
            label6.Text = "Должность";
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new System.Drawing.Point(442, 104);
            label5.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            label5.Name = "label5";
            label5.Size = new System.Drawing.Size(58, 15);
            label5.TabIndex = 77;
            label5.Text = "Отчество";
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new System.Drawing.Point(441, 80);
            label4.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            label4.Name = "label4";
            label4.Size = new System.Drawing.Size(31, 15);
            label4.TabIndex = 76;
            label4.Text = "Имя";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new System.Drawing.Point(441, 55);
            label3.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            label3.Name = "label3";
            label3.Size = new System.Drawing.Size(58, 15);
            label3.TabIndex = 75;
            label3.Text = "Фамилия";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new System.Drawing.Point(566, 5);
            label2.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            label2.Name = "label2";
            label2.Size = new System.Drawing.Size(102, 15);
            label2.TabIndex = 74;
            label2.Text = "Добавить запись ";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new System.Drawing.Point(442, 29);
            label1.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(84, 15);
            label1.TabIndex = 73;
            label1.Text = "ID сотрудника";
            // 
            // txtId
            // 
            txtId.Location = new System.Drawing.Point(566, 28);
            txtId.Margin = new System.Windows.Forms.Padding(2);
            txtId.Name = "txtId";
            txtId.Size = new System.Drawing.Size(232, 23);
            txtId.TabIndex = 72;
            // 
            // txtLogin
            // 
            txtLogin.Location = new System.Drawing.Point(566, 209);
            txtLogin.Margin = new System.Windows.Forms.Padding(2);
            txtLogin.Name = "txtLogin";
            txtLogin.Size = new System.Drawing.Size(232, 23);
            txtLogin.TabIndex = 87;
            // 
            // txtPassword
            // 
            txtPassword.Location = new System.Drawing.Point(566, 239);
            txtPassword.Margin = new System.Windows.Forms.Padding(2);
            txtPassword.Name = "txtPassword";
            txtPassword.Size = new System.Drawing.Size(232, 23);
            txtPassword.TabIndex = 88;
            txtPassword.UseSystemPasswordChar = true;
            // 
            // label9
            // 
            label9.AutoSize = true;
            label9.Location = new System.Drawing.Point(442, 211);
            label9.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            label9.Name = "label9";
            label9.Size = new System.Drawing.Size(41, 15);
            label9.TabIndex = 89;
            label9.Text = "Логин";
            // 
            // label10
            // 
            label10.AutoSize = true;
            label10.Location = new System.Drawing.Point(442, 239);
            label10.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            label10.Name = "label10";
            label10.Size = new System.Drawing.Size(49, 15);
            label10.TabIndex = 90;
            label10.Text = "Пароль";
            // 
            // cmbRole
            // 
            cmbRole.FormattingEnabled = true;
            cmbRole.Items.AddRange(new object[] { "AddRange(roles)" });
            cmbRole.Location = new System.Drawing.Point(566, 269);
            cmbRole.Margin = new System.Windows.Forms.Padding(2);
            cmbRole.Name = "cmbRole";
            cmbRole.Size = new System.Drawing.Size(232, 23);
            cmbRole.TabIndex = 91;
            // 
            // label11
            // 
            label11.AutoSize = true;
            label11.Location = new System.Drawing.Point(442, 271);
            label11.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            label11.Name = "label11";
            label11.Size = new System.Drawing.Size(69, 15);
            label11.TabIndex = 92;
            label11.Text = "Должность";
            // 
            // Form2
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(891, 516);
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
            Margin = new System.Windows.Forms.Padding(2);
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