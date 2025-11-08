namespace WindowsFormsApp1
{
    partial class RegisterForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(RegisterForm));
            panel1 = new System.Windows.Forms.Panel();
            userOtchestvo = new System.Windows.Forms.TextBox();
            textBox2 = new System.Windows.Forms.TextBox();
            userSurnameField = new System.Windows.Forms.TextBox();
            RegistrButton1 = new System.Windows.Forms.Button();
            passField = new System.Windows.Forms.TextBox();
            pictureBox2 = new System.Windows.Forms.PictureBox();
            userNameField = new System.Windows.Forms.TextBox();
            pictureBox1 = new System.Windows.Forms.PictureBox();
            panel2 = new System.Windows.Forms.Panel();
            closeButton = new System.Windows.Forms.Label();
            label1 = new System.Windows.Forms.Label();
            panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox2).BeginInit();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            panel2.SuspendLayout();
            SuspendLayout();
            // 
            // panel1
            // 
            panel1.BackColor = System.Drawing.Color.FromArgb(70, 8, 24);
            panel1.Controls.Add(userOtchestvo);
            panel1.Controls.Add(textBox2);
            panel1.Controls.Add(userSurnameField);
            panel1.Controls.Add(RegistrButton1);
            panel1.Controls.Add(passField);
            panel1.Controls.Add(pictureBox2);
            panel1.Controls.Add(userNameField);
            panel1.Controls.Add(pictureBox1);
            panel1.Controls.Add(panel2);
            panel1.Dock = System.Windows.Forms.DockStyle.Fill;
            panel1.Location = new System.Drawing.Point(0, 0);
            panel1.Margin = new System.Windows.Forms.Padding(2);
            panel1.Name = "panel1";
            panel1.Size = new System.Drawing.Size(800, 450);
            panel1.TabIndex = 1;
            panel1.MouseDown += panel1_MouseDown;
            panel1.MouseMove += panel1_MouseMove_1;
            // 
            // userOtchestvo
            // 
            userOtchestvo.Font = new System.Drawing.Font("Times New Roman", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 204);
            userOtchestvo.Location = new System.Drawing.Point(100, 268);
            userOtchestvo.Margin = new System.Windows.Forms.Padding(2);
            userOtchestvo.Multiline = true;
            userOtchestvo.Name = "userOtchestvo";
            userOtchestvo.Size = new System.Drawing.Size(217, 38);
            userOtchestvo.TabIndex = 8;
            userOtchestvo.Enter += userOtchestvo_Enter;
            userOtchestvo.Leave += userOtchestvo_Leave;
            // 
            // textBox2
            // 
            textBox2.Font = new System.Drawing.Font("Times New Roman", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 204);
            textBox2.Location = new System.Drawing.Point(446, 210);
            textBox2.Margin = new System.Windows.Forms.Padding(2);
            textBox2.Name = "textBox2";
            textBox2.Size = new System.Drawing.Size(217, 26);
            textBox2.TabIndex = 7;
            textBox2.UseSystemPasswordChar = true;
            // 
            // userSurnameField
            // 
            userSurnameField.Font = new System.Drawing.Font("Times New Roman", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 204);
            userSurnameField.Location = new System.Drawing.Point(100, 210);
            userSurnameField.Margin = new System.Windows.Forms.Padding(2);
            userSurnameField.Multiline = true;
            userSurnameField.Name = "userSurnameField";
            userSurnameField.Size = new System.Drawing.Size(217, 38);
            userSurnameField.TabIndex = 6;
            userSurnameField.Enter += userSurnameField_Enter;
            userSurnameField.Leave += userSurnameField_Leave;
            // 
            // RegistrButton1
            // 
            RegistrButton1.BackColor = System.Drawing.Color.FromArgb(30, 117, 43);
            RegistrButton1.Cursor = System.Windows.Forms.Cursors.Hand;
            RegistrButton1.FlatAppearance.BorderSize = 0;
            RegistrButton1.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(5, 48, 8);
            RegistrButton1.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(64, 61, 7);
            RegistrButton1.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            RegistrButton1.Font = new System.Drawing.Font("Segoe UI", 15F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 204);
            RegistrButton1.ForeColor = System.Drawing.Color.White;
            RegistrButton1.Location = new System.Drawing.Point(277, 347);
            RegistrButton1.Margin = new System.Windows.Forms.Padding(2);
            RegistrButton1.Name = "RegistrButton1";
            RegistrButton1.Size = new System.Drawing.Size(241, 44);
            RegistrButton1.TabIndex = 5;
            RegistrButton1.Text = "Зарегистрироваться";
            RegistrButton1.UseVisualStyleBackColor = false;
            RegistrButton1.Click += RegistrButton1_Click;
            // 
            // passField
            // 
            passField.Font = new System.Drawing.Font("Times New Roman", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 204);
            passField.Location = new System.Drawing.Point(446, 153);
            passField.Margin = new System.Windows.Forms.Padding(2);
            passField.Name = "passField";
            passField.Size = new System.Drawing.Size(217, 26);
            passField.TabIndex = 4;
            passField.UseSystemPasswordChar = true;
            // 
            // pictureBox2
            // 
            pictureBox2.Image = (System.Drawing.Image)resources.GetObject("pictureBox2.Image");
            pictureBox2.Location = new System.Drawing.Point(387, 210);
            pictureBox2.Margin = new System.Windows.Forms.Padding(2);
            pictureBox2.Name = "pictureBox2";
            pictureBox2.Size = new System.Drawing.Size(45, 38);
            pictureBox2.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            pictureBox2.TabIndex = 3;
            pictureBox2.TabStop = false;
            // 
            // userNameField
            // 
            userNameField.Font = new System.Drawing.Font("Times New Roman", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 204);
            userNameField.Location = new System.Drawing.Point(100, 153);
            userNameField.Margin = new System.Windows.Forms.Padding(2);
            userNameField.Multiline = true;
            userNameField.Name = "userNameField";
            userNameField.Size = new System.Drawing.Size(217, 38);
            userNameField.TabIndex = 2;
            userNameField.TextChanged += loginField_TextChanged;
            userNameField.Enter += userNameField_Enter;
            userNameField.Leave += userNameField_Leave;
            // 
            // pictureBox1
            // 
            pictureBox1.Image = (System.Drawing.Image)resources.GetObject("pictureBox1.Image");
            pictureBox1.Location = new System.Drawing.Point(40, 210);
            pictureBox1.Margin = new System.Windows.Forms.Padding(2);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new System.Drawing.Size(45, 38);
            pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            pictureBox1.TabIndex = 1;
            pictureBox1.TabStop = false;
            // 
            // panel2
            // 
            panel2.BackColor = System.Drawing.Color.FromArgb(61, 21, 185);
            panel2.Controls.Add(closeButton);
            panel2.Controls.Add(label1);
            panel2.Dock = System.Windows.Forms.DockStyle.Top;
            panel2.Location = new System.Drawing.Point(0, 0);
            panel2.Margin = new System.Windows.Forms.Padding(2);
            panel2.Name = "panel2";
            panel2.Size = new System.Drawing.Size(800, 56);
            panel2.TabIndex = 0;
            // 
            // closeButton
            // 
            closeButton.Cursor = System.Windows.Forms.Cursors.Hand;
            closeButton.Font = new System.Drawing.Font("Segoe UI", 14F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 204);
            closeButton.ForeColor = System.Drawing.Color.White;
            closeButton.Location = new System.Drawing.Point(774, 0);
            closeButton.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            closeButton.Name = "closeButton";
            closeButton.Size = new System.Drawing.Size(26, 29);
            closeButton.TabIndex = 1;
            closeButton.Text = "x";
            closeButton.TextAlign = System.Drawing.ContentAlignment.TopRight;
            closeButton.Click += closeButton_Click;
            closeButton.MouseEnter += closeButton_MouseEnter;
            closeButton.MouseLeave += closeButton_MouseLeave;
            // 
            // label1
            // 
            label1.Dock = System.Windows.Forms.DockStyle.Fill;
            label1.Font = new System.Drawing.Font("Times New Roman", 24F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 204);
            label1.ForeColor = System.Drawing.Color.FromArgb(224, 224, 224);
            label1.Location = new System.Drawing.Point(0, 0);
            label1.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(800, 56);
            label1.TabIndex = 0;
            label1.Text = "Регистрация";
            label1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            label1.MouseDown += label1_MouseDown;
            label1.MouseMove += label1_MouseMove;
            // 
            // RegisterForm
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(800, 450);
            Controls.Add(panel1);
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            Name = "RegisterForm";
            Text = "RegisterForm";
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox2).EndInit();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
            panel2.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Button RegistrButton1;
        private System.Windows.Forms.TextBox passField;
        private System.Windows.Forms.PictureBox pictureBox2;
        private System.Windows.Forms.TextBox userNameField;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.Label closeButton;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox textBox2;
        private System.Windows.Forms.TextBox userSurnameField;
        private System.Windows.Forms.TextBox userOtchestvo;
    }
}