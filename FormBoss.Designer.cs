namespace WindowsFormsApp1
{
    partial class FormBoss
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
            dgvPlans = new System.Windows.Forms.DataGridView();
            label1 = new System.Windows.Forms.Label();
            label2 = new System.Windows.Forms.Label();
            btnFilter = new System.Windows.Forms.Button();
            btnAdd = new System.Windows.Forms.Button();
            btnUpdate = new System.Windows.Forms.Button();
            btnDelete = new System.Windows.Forms.Button();
            label3 = new System.Windows.Forms.Label();
            label4 = new System.Windows.Forms.Label();
            label5 = new System.Windows.Forms.Label();
            label6 = new System.Windows.Forms.Label();
            label8 = new System.Windows.Forms.Label();
            label10 = new System.Windows.Forms.Label();
            cmbEquipment = new System.Windows.Forms.ComboBox();
            label11 = new System.Windows.Forms.Label();
            cmbResponsible = new System.Windows.Forms.ComboBox();
            dtpStart = new System.Windows.Forms.DateTimePicker();
            dtpEnd = new System.Windows.Forms.DateTimePicker();
            chkAll = new System.Windows.Forms.CheckBox();
            btnExcel = new System.Windows.Forms.Button();
            btnWord = new System.Windows.Forms.Button();
            btnPreview = new System.Windows.Forms.Button();
            dtpStartRepair = new System.Windows.Forms.DateTimePicker();
            btnClear = new System.Windows.Forms.Button();
            label12 = new System.Windows.Forms.Label();
            label13 = new System.Windows.Forms.Label();
            cmbReportType = new System.Windows.Forms.ComboBox();
            lblTotal = new System.Windows.Forms.Label();
            lblCompleted = new System.Windows.Forms.Label();
            lblInProgress = new System.Windows.Forms.Label();
            lblPlanned = new System.Windows.Forms.Label();
            label14 = new System.Windows.Forms.Label();
            cmbTip = new System.Windows.Forms.ComboBox();
            cmbStatus = new System.Windows.Forms.ComboBox();
            dtpEndRepair = new System.Windows.Forms.DateTimePicker();
            label15 = new System.Windows.Forms.Label();
            tabControl1 = new System.Windows.Forms.TabControl();
            tabPlans = new System.Windows.Forms.TabPage();
            tabAvariya = new System.Windows.Forms.TabPage();
            lblAvariyaCompleted = new System.Windows.Forms.Label();
            lblAvariyaInProgress = new System.Windows.Forms.Label();
            lblAvariyaNeedPlan = new System.Windows.Forms.Label();
            lblAvariyaTotal = new System.Windows.Forms.Label();
            btnCreatePlanFromAvariya = new System.Windows.Forms.Button();
            chkAvariyaAll = new System.Windows.Forms.CheckBox();
            btnAvariyaFilter = new System.Windows.Forms.Button();
            dtpAvariyaEnd = new System.Windows.Forms.DateTimePicker();
            dtpAvariyaStart = new System.Windows.Forms.DateTimePicker();
            dgvAvariya = new System.Windows.Forms.DataGridView();
            label7 = new System.Windows.Forms.Label();
            label9 = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)dgvPlans).BeginInit();
            tabControl1.SuspendLayout();
            tabPlans.SuspendLayout();
            tabAvariya.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvAvariya).BeginInit();
            SuspendLayout();
            // 
            // dgvPlans
            // 
            dgvPlans.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvPlans.Location = new System.Drawing.Point(49, 80);
            dgvPlans.Name = "dgvPlans";
            dgvPlans.Size = new System.Drawing.Size(685, 277);
            dgvPlans.TabIndex = 0;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new System.Drawing.Point(49, 27);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(109, 15);
            label1.TabIndex = 1;
            label1.Text = "Фильтр по дате :  с";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new System.Drawing.Point(330, 27);
            label2.Name = "label2";
            label2.Size = new System.Drawing.Size(21, 15);
            label2.TabIndex = 2;
            label2.Text = "по";
            // 
            // btnFilter
            // 
            btnFilter.Location = new System.Drawing.Point(525, 23);
            btnFilter.Name = "btnFilter";
            btnFilter.Size = new System.Drawing.Size(161, 23);
            btnFilter.TabIndex = 3;
            btnFilter.Text = "Применить фильтр";
            btnFilter.UseVisualStyleBackColor = true;
            // 
            // btnAdd
            // 
            btnAdd.Location = new System.Drawing.Point(304, 577);
            btnAdd.Name = "btnAdd";
            btnAdd.Size = new System.Drawing.Size(143, 23);
            btnAdd.TabIndex = 4;
            btnAdd.Text = "Добавить";
            btnAdd.UseVisualStyleBackColor = true;
            // 
            // btnUpdate
            // 
            btnUpdate.Location = new System.Drawing.Point(454, 577);
            btnUpdate.Name = "btnUpdate";
            btnUpdate.Size = new System.Drawing.Size(143, 23);
            btnUpdate.TabIndex = 5;
            btnUpdate.Text = "Обновить";
            btnUpdate.UseVisualStyleBackColor = true;
            // 
            // btnDelete
            // 
            btnDelete.Location = new System.Drawing.Point(603, 577);
            btnDelete.Name = "btnDelete";
            btnDelete.Size = new System.Drawing.Size(143, 23);
            btnDelete.TabIndex = 6;
            btnDelete.Text = "Удалить";
            btnDelete.UseVisualStyleBackColor = true;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new System.Drawing.Point(49, 402);
            label3.Name = "label3";
            label3.Size = new System.Drawing.Size(94, 15);
            label3.TabIndex = 7;
            label3.Text = "Оборудование :";
            label3.Click += label3_Click;
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new System.Drawing.Point(52, 433);
            label4.Name = "label4";
            label4.Size = new System.Drawing.Size(10, 15);
            label4.TabIndex = 8;
            label4.Text = " ";
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new System.Drawing.Point(52, 486);
            label5.Name = "label5";
            label5.Size = new System.Drawing.Size(97, 15);
            label5.TabIndex = 9;
            label5.Text = "Ответственный :";
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Location = new System.Drawing.Point(52, 433);
            label6.Name = "label6";
            label6.Size = new System.Drawing.Size(51, 15);
            label6.TabIndex = 10;
            label6.Text = "Тип ТО :";
            // 
            // label8
            // 
            label8.AutoSize = true;
            label8.Location = new System.Drawing.Point(54, 519);
            label8.Name = "label8";
            label8.Size = new System.Drawing.Size(67, 15);
            label8.TabIndex = 12;
            label8.Text = "Статус ТО :";
            // 
            // label10
            // 
            label10.AutoSize = true;
            label10.Location = new System.Drawing.Point(301, 371);
            label10.Name = "label10";
            label10.Size = new System.Drawing.Size(216, 15);
            label10.TabIndex = 20;
            label10.Text = "ПЛАНИРОВАНИЕ НОВОГО РЕМОНТА";
            // 
            // cmbEquipment
            // 
            cmbEquipment.FormattingEnabled = true;
            cmbEquipment.Location = new System.Drawing.Point(149, 399);
            cmbEquipment.Name = "cmbEquipment";
            cmbEquipment.Size = new System.Drawing.Size(278, 23);
            cmbEquipment.TabIndex = 21;
            // 
            // label11
            // 
            label11.AutoSize = true;
            label11.Location = new System.Drawing.Point(333, 434);
            label11.Name = "label11";
            label11.Size = new System.Drawing.Size(0, 15);
            label11.TabIndex = 22;
            // 
            // cmbResponsible
            // 
            cmbResponsible.FormattingEnabled = true;
            cmbResponsible.Location = new System.Drawing.Point(149, 483);
            cmbResponsible.Name = "cmbResponsible";
            cmbResponsible.Size = new System.Drawing.Size(278, 23);
            cmbResponsible.TabIndex = 23;
            // 
            // dtpStart
            // 
            dtpStart.Location = new System.Drawing.Point(174, 23);
            dtpStart.Name = "dtpStart";
            dtpStart.Size = new System.Drawing.Size(137, 23);
            dtpStart.TabIndex = 24;
            // 
            // dtpEnd
            // 
            dtpEnd.Location = new System.Drawing.Point(372, 23);
            dtpEnd.Name = "dtpEnd";
            dtpEnd.Size = new System.Drawing.Size(137, 23);
            dtpEnd.TabIndex = 25;
            // 
            // chkAll
            // 
            chkAll.AutoSize = true;
            chkAll.Location = new System.Drawing.Point(49, 55);
            chkAll.Name = "chkAll";
            chkAll.Size = new System.Drawing.Size(150, 19);
            chkAll.TabIndex = 26;
            chkAll.Text = "Показать все ремонты";
            chkAll.UseVisualStyleBackColor = true;
            // 
            // btnExcel
            // 
            btnExcel.Location = new System.Drawing.Point(128, 733);
            btnExcel.Name = "btnExcel";
            btnExcel.Size = new System.Drawing.Size(168, 23);
            btnExcel.TabIndex = 30;
            btnExcel.Text = "Экспорт в EXEL";
            btnExcel.UseVisualStyleBackColor = true;
            // 
            // btnWord
            // 
            btnWord.Location = new System.Drawing.Point(330, 733);
            btnWord.Name = "btnWord";
            btnWord.Size = new System.Drawing.Size(168, 23);
            btnWord.TabIndex = 31;
            btnWord.Text = "Экспорт в WORD";
            btnWord.UseVisualStyleBackColor = true;
            // 
            // btnPreview
            // 
            btnPreview.Location = new System.Drawing.Point(517, 733);
            btnPreview.Name = "btnPreview";
            btnPreview.Size = new System.Drawing.Size(168, 23);
            btnPreview.TabIndex = 32;
            btnPreview.Text = "Предпросмотр";
            btnPreview.UseVisualStyleBackColor = true;
            // 
            // dtpStartRepair
            // 
            dtpStartRepair.Location = new System.Drawing.Point(149, 457);
            dtpStartRepair.Name = "dtpStartRepair";
            dtpStartRepair.Size = new System.Drawing.Size(137, 23);
            dtpStartRepair.TabIndex = 33;
            // 
            // btnClear
            // 
            btnClear.Location = new System.Drawing.Point(517, 794);
            btnClear.Name = "btnClear";
            btnClear.Size = new System.Drawing.Size(168, 23);
            btnClear.TabIndex = 35;
            btnClear.Text = "Очистить форму";
            btnClear.UseVisualStyleBackColor = true;
            // 
            // label12
            // 
            label12.AutoSize = true;
            label12.Location = new System.Drawing.Point(49, 674);
            label12.Name = "label12";
            label12.Size = new System.Drawing.Size(72, 15);
            label12.TabIndex = 36;
            label12.Text = "Тип отчета :";
            // 
            // label13
            // 
            label13.AutoSize = true;
            label13.Location = new System.Drawing.Point(330, 644);
            label13.Name = "label13";
            label13.Size = new System.Drawing.Size(114, 15);
            label13.TabIndex = 37;
            label13.Text = "ЭКСПОРТ ОТЧЕТОВ";
            // 
            // cmbReportType
            // 
            cmbReportType.FormattingEnabled = true;
            cmbReportType.Location = new System.Drawing.Point(128, 672);
            cmbReportType.Name = "cmbReportType";
            cmbReportType.Size = new System.Drawing.Size(259, 23);
            cmbReportType.TabIndex = 38;
            // 
            // lblTotal
            // 
            lblTotal.AutoSize = true;
            lblTotal.Location = new System.Drawing.Point(116, 872);
            lblTotal.Name = "lblTotal";
            lblTotal.Size = new System.Drawing.Size(44, 15);
            lblTotal.TabIndex = 40;
            lblTotal.Text = "label14";
            // 
            // lblCompleted
            // 
            lblCompleted.AutoSize = true;
            lblCompleted.Location = new System.Drawing.Point(286, 872);
            lblCompleted.Name = "lblCompleted";
            lblCompleted.Size = new System.Drawing.Size(44, 15);
            lblCompleted.TabIndex = 41;
            lblCompleted.Text = "label15";
            // 
            // lblInProgress
            // 
            lblInProgress.AutoSize = true;
            lblInProgress.Location = new System.Drawing.Point(451, 872);
            lblInProgress.Name = "lblInProgress";
            lblInProgress.Size = new System.Drawing.Size(44, 15);
            lblInProgress.TabIndex = 42;
            lblInProgress.Text = "label16";
            // 
            // lblPlanned
            // 
            lblPlanned.AutoSize = true;
            lblPlanned.Location = new System.Drawing.Point(587, 872);
            lblPlanned.Name = "lblPlanned";
            lblPlanned.Size = new System.Drawing.Size(44, 15);
            lblPlanned.TabIndex = 43;
            lblPlanned.Text = "label17";
            // 
            // label14
            // 
            label14.AutoSize = true;
            label14.Location = new System.Drawing.Point(52, 462);
            label14.Name = "label14";
            label14.Size = new System.Drawing.Size(80, 15);
            label14.TabIndex = 10;
            label14.Text = "Дата начала :";
            // 
            // cmbTip
            // 
            cmbTip.FormattingEnabled = true;
            cmbTip.Location = new System.Drawing.Point(149, 428);
            cmbTip.Name = "cmbTip";
            cmbTip.Size = new System.Drawing.Size(278, 23);
            cmbTip.TabIndex = 44;
            // 
            // cmbStatus
            // 
            cmbStatus.FormattingEnabled = true;
            cmbStatus.Location = new System.Drawing.Point(149, 516);
            cmbStatus.Name = "cmbStatus";
            cmbStatus.Size = new System.Drawing.Size(278, 23);
            cmbStatus.TabIndex = 45;
            // 
            // dtpEndRepair
            // 
            dtpEndRepair.Location = new System.Drawing.Point(414, 456);
            dtpEndRepair.Name = "dtpEndRepair";
            dtpEndRepair.Size = new System.Drawing.Size(137, 23);
            dtpEndRepair.TabIndex = 46;
            // 
            // label15
            // 
            label15.AutoSize = true;
            label15.Location = new System.Drawing.Point(307, 462);
            label15.Name = "label15";
            label15.Size = new System.Drawing.Size(101, 15);
            label15.TabIndex = 47;
            label15.Text = "Дата окончания :";
            // 
            // tabControl1
            // 
            tabControl1.Controls.Add(tabPlans);
            tabControl1.Controls.Add(tabAvariya);
            tabControl1.Location = new System.Drawing.Point(2, 1);
            tabControl1.Name = "tabControl1";
            tabControl1.SelectedIndex = 0;
            tabControl1.Size = new System.Drawing.Size(838, 961);
            tabControl1.TabIndex = 48;
            // 
            // tabPlans
            // 
            tabPlans.Controls.Add(dgvPlans);
            tabPlans.Controls.Add(label15);
            tabPlans.Controls.Add(label1);
            tabPlans.Controls.Add(dtpEndRepair);
            tabPlans.Controls.Add(label2);
            tabPlans.Controls.Add(cmbStatus);
            tabPlans.Controls.Add(btnFilter);
            tabPlans.Controls.Add(cmbTip);
            tabPlans.Controls.Add(btnAdd);
            tabPlans.Controls.Add(lblPlanned);
            tabPlans.Controls.Add(btnUpdate);
            tabPlans.Controls.Add(lblInProgress);
            tabPlans.Controls.Add(btnDelete);
            tabPlans.Controls.Add(lblCompleted);
            tabPlans.Controls.Add(label3);
            tabPlans.Controls.Add(lblTotal);
            tabPlans.Controls.Add(label4);
            tabPlans.Controls.Add(cmbReportType);
            tabPlans.Controls.Add(label5);
            tabPlans.Controls.Add(label13);
            tabPlans.Controls.Add(label6);
            tabPlans.Controls.Add(label12);
            tabPlans.Controls.Add(label14);
            tabPlans.Controls.Add(btnClear);
            tabPlans.Controls.Add(label8);
            tabPlans.Controls.Add(dtpStartRepair);
            tabPlans.Controls.Add(label10);
            tabPlans.Controls.Add(btnPreview);
            tabPlans.Controls.Add(cmbEquipment);
            tabPlans.Controls.Add(btnWord);
            tabPlans.Controls.Add(label11);
            tabPlans.Controls.Add(btnExcel);
            tabPlans.Controls.Add(cmbResponsible);
            tabPlans.Controls.Add(chkAll);
            tabPlans.Controls.Add(dtpStart);
            tabPlans.Controls.Add(dtpEnd);
            tabPlans.Location = new System.Drawing.Point(4, 24);
            tabPlans.Name = "tabPlans";
            tabPlans.Padding = new System.Windows.Forms.Padding(3);
            tabPlans.Size = new System.Drawing.Size(830, 933);
            tabPlans.TabIndex = 0;
            tabPlans.Text = "Планы";
            tabPlans.UseVisualStyleBackColor = true;
            // 
            // tabAvariya
            // 
            tabAvariya.Controls.Add(label9);
            tabAvariya.Controls.Add(label7);
            tabAvariya.Controls.Add(lblAvariyaCompleted);
            tabAvariya.Controls.Add(lblAvariyaInProgress);
            tabAvariya.Controls.Add(lblAvariyaNeedPlan);
            tabAvariya.Controls.Add(lblAvariyaTotal);
            tabAvariya.Controls.Add(btnCreatePlanFromAvariya);
            tabAvariya.Controls.Add(chkAvariyaAll);
            tabAvariya.Controls.Add(btnAvariyaFilter);
            tabAvariya.Controls.Add(dtpAvariyaEnd);
            tabAvariya.Controls.Add(dtpAvariyaStart);
            tabAvariya.Controls.Add(dgvAvariya);
            tabAvariya.Location = new System.Drawing.Point(4, 24);
            tabAvariya.Name = "tabAvariya";
            tabAvariya.Padding = new System.Windows.Forms.Padding(3);
            tabAvariya.Size = new System.Drawing.Size(830, 933);
            tabAvariya.TabIndex = 1;
            tabAvariya.Text = "Аварии";
            tabAvariya.UseVisualStyleBackColor = true;
            // 
            // lblAvariyaCompleted
            // 
            lblAvariyaCompleted.AutoSize = true;
            lblAvariyaCompleted.Location = new System.Drawing.Point(640, 856);
            lblAvariyaCompleted.Name = "lblAvariyaCompleted";
            lblAvariyaCompleted.Size = new System.Drawing.Size(44, 15);
            lblAvariyaCompleted.TabIndex = 9;
            lblAvariyaCompleted.Text = "label17";
            // 
            // lblAvariyaInProgress
            // 
            lblAvariyaInProgress.AutoSize = true;
            lblAvariyaInProgress.Location = new System.Drawing.Point(451, 856);
            lblAvariyaInProgress.Name = "lblAvariyaInProgress";
            lblAvariyaInProgress.Size = new System.Drawing.Size(44, 15);
            lblAvariyaInProgress.TabIndex = 8;
            lblAvariyaInProgress.Text = "label16";
            // 
            // lblAvariyaNeedPlan
            // 
            lblAvariyaNeedPlan.AutoSize = true;
            lblAvariyaNeedPlan.Location = new System.Drawing.Point(280, 856);
            lblAvariyaNeedPlan.Name = "lblAvariyaNeedPlan";
            lblAvariyaNeedPlan.Size = new System.Drawing.Size(38, 15);
            lblAvariyaNeedPlan.TabIndex = 7;
            lblAvariyaNeedPlan.Text = "label9";
            // 
            // lblAvariyaTotal
            // 
            lblAvariyaTotal.AutoSize = true;
            lblAvariyaTotal.Location = new System.Drawing.Point(120, 856);
            lblAvariyaTotal.Name = "lblAvariyaTotal";
            lblAvariyaTotal.Size = new System.Drawing.Size(38, 15);
            lblAvariyaTotal.TabIndex = 6;
            lblAvariyaTotal.Text = "label7";
            // 
            // btnCreatePlanFromAvariya
            // 
            btnCreatePlanFromAvariya.Location = new System.Drawing.Point(589, 732);
            btnCreatePlanFromAvariya.Name = "btnCreatePlanFromAvariya";
            btnCreatePlanFromAvariya.Size = new System.Drawing.Size(166, 28);
            btnCreatePlanFromAvariya.TabIndex = 5;
            btnCreatePlanFromAvariya.Text = "Создать план из аварии";
            btnCreatePlanFromAvariya.UseVisualStyleBackColor = true;
            // 
            // chkAvariyaAll
            // 
            chkAvariyaAll.AutoSize = true;
            chkAvariyaAll.Location = new System.Drawing.Point(33, 110);
            chkAvariyaAll.Name = "chkAvariyaAll";
            chkAvariyaAll.Size = new System.Drawing.Size(139, 19);
            chkAvariyaAll.TabIndex = 4;
            chkAvariyaAll.Text = "Показать все аварии";
            chkAvariyaAll.UseVisualStyleBackColor = true;
            // 
            // btnAvariyaFilter
            // 
            btnAvariyaFilter.Location = new System.Drawing.Point(569, 70);
            btnAvariyaFilter.Name = "btnAvariyaFilter";
            btnAvariyaFilter.Size = new System.Drawing.Size(152, 23);
            btnAvariyaFilter.TabIndex = 3;
            btnAvariyaFilter.Text = "Применить фильтр";
            btnAvariyaFilter.UseVisualStyleBackColor = true;
            // 
            // dtpAvariyaEnd
            // 
            dtpAvariyaEnd.Location = new System.Drawing.Point(402, 68);
            dtpAvariyaEnd.Name = "dtpAvariyaEnd";
            dtpAvariyaEnd.Size = new System.Drawing.Size(135, 23);
            dtpAvariyaEnd.TabIndex = 2;
            // 
            // dtpAvariyaStart
            // 
            dtpAvariyaStart.Location = new System.Drawing.Point(176, 68);
            dtpAvariyaStart.Name = "dtpAvariyaStart";
            dtpAvariyaStart.Size = new System.Drawing.Size(142, 23);
            dtpAvariyaStart.TabIndex = 1;
            // 
            // dgvAvariya
            // 
            dgvAvariya.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvAvariya.Location = new System.Drawing.Point(25, 162);
            dgvAvariya.Name = "dgvAvariya";
            dgvAvariya.Size = new System.Drawing.Size(774, 543);
            dgvAvariya.TabIndex = 0;
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.Location = new System.Drawing.Point(33, 72);
            label7.Name = "label7";
            label7.Size = new System.Drawing.Size(106, 15);
            label7.TabIndex = 10;
            label7.Text = "Фильтр по дате:  с";
            // 
            // label9
            // 
            label9.AutoSize = true;
            label9.Location = new System.Drawing.Point(349, 74);
            label9.Name = "label9";
            label9.Size = new System.Drawing.Size(21, 15);
            label9.TabIndex = 11;
            label9.Text = "по";
            // 
            // FormBoss
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(835, 958);
            Controls.Add(tabControl1);
            Name = "FormBoss";
            Text = "FormBoss";
            Load += FormBoss_Load;
            ((System.ComponentModel.ISupportInitialize)dgvPlans).EndInit();
            tabControl1.ResumeLayout(false);
            tabPlans.ResumeLayout(false);
            tabPlans.PerformLayout();
            tabAvariya.ResumeLayout(false);
            tabAvariya.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)dgvAvariya).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.DataGridView dgvPlans;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button btnFilter;
        private System.Windows.Forms.Button btnAdd;
        private System.Windows.Forms.Button btnUpdate;
        private System.Windows.Forms.Button btnDelete;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.TextBox textBox2;
        private System.Windows.Forms.TextBox textBox3;
        private System.Windows.Forms.TextBox textBox5;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.ComboBox cmbEquipment;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.ComboBox cmbResponsible;
        private System.Windows.Forms.DateTimePicker dtpStart;
        private System.Windows.Forms.DateTimePicker dtpEnd;
        private System.Windows.Forms.CheckBox chkAll;
        private System.Windows.Forms.Button btnExcel;
        private System.Windows.Forms.Button btnWord;
        private System.Windows.Forms.Button btnPreview;
        private System.Windows.Forms.DateTimePicker dtpStartRepair;
        private System.Windows.Forms.Button btnClear;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.Label label13;
        private System.Windows.Forms.ComboBox cmbReportType;
        private System.Windows.Forms.Label lblTotal;
        private System.Windows.Forms.Label lblCompleted;
        private System.Windows.Forms.Label lblInProgress;
        private System.Windows.Forms.Label lblPlanned;
        private System.Windows.Forms.Label label14;
        private System.Windows.Forms.ComboBox cmbTip;
        private System.Windows.Forms.ComboBox cmbStatus;
        private System.Windows.Forms.DateTimePicker dtpEndRepair;
        private System.Windows.Forms.Label label15;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabPlans;
        private System.Windows.Forms.TabPage tabAvariya;
        private System.Windows.Forms.CheckBox chkAvariyaAll;
        private System.Windows.Forms.Button btnAvariyaFilter;
        private System.Windows.Forms.DateTimePicker dtpAvariyaEnd;
        private System.Windows.Forms.DateTimePicker dtpAvariyaStart;
        private System.Windows.Forms.DataGridView dgvAvariya;
        private System.Windows.Forms.Label lblAvariyaCompleted;
        private System.Windows.Forms.Label lblAvariyaInProgress;
        private System.Windows.Forms.Label lblAvariyaNeedPlan;
        private System.Windows.Forms.Label lblAvariyaTotal;
        private System.Windows.Forms.Button btnCreatePlanFromAvariya;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.Label label7;
    }
}