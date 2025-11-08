using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public partial class MainForm : Form
    {
        private Panel sidePanel;
        private Panel contentPanel; // Панель для отображения контента
        private bool isPanelExpanded = false;
        private int collapsedWidth = 30;
        private int expandedWidth = 200;

        // Ссылки на ваши формы
        private Form1 form1;
        private Form2 form2;

        public MainForm()
        {
            InitializeComponent();
            CreateContentPanel();
            CreateSidePanel();
            AdjustMainContent();

            // Показываем главный контент по умолчанию
            ShowMainContent();
        }

        // Создаем панель для отображения контента
        private void CreateContentPanel()
        {
            contentPanel = new Panel();
            contentPanel.Dock = DockStyle.Fill;
            contentPanel.BackColor = Color.White;
            this.Controls.Add(contentPanel);

            // Перемещаем существующие кнопки на contentPanel
            contentPanel.Controls.Add(button1);
            contentPanel.Controls.Add(button2);
            if (panel1 != null)
                contentPanel.Controls.Add(panel1);
        }

        private void CreateSidePanel()
        {
            sidePanel = new Panel();
            sidePanel.Dock = DockStyle.Left;
            sidePanel.Width = collapsedWidth;
            sidePanel.BackColor = Color.SteelBlue;

            Button toggleBtn = new Button();
            toggleBtn.Text = "☰";
            toggleBtn.Dock = DockStyle.Top;
            toggleBtn.Height = 30;
            toggleBtn.BackColor = Color.DarkBlue;
            toggleBtn.ForeColor = Color.White;
            toggleBtn.FlatStyle = FlatStyle.Flat;
            toggleBtn.Click += ToggleBtn_Click;

            Panel sideContentPanel = new Panel();
            sideContentPanel.Dock = DockStyle.Fill;
            sideContentPanel.BackColor = Color.LightSteelBlue;
            sideContentPanel.Padding = new Padding(5);

            AddSidePanelContent(sideContentPanel);

            sidePanel.Controls.Add(sideContentPanel);
            sidePanel.Controls.Add(toggleBtn);

            this.Controls.Add(sidePanel);
            sidePanel.BringToFront();
        }

        private void AddSidePanelContent(Panel container)
        {
            Label title = new Label()
            {
                Text = "Навигация",
                Location = new Point(10, 10),
                AutoSize = true,
                ForeColor = Color.DarkBlue,
                Font = new Font("Arial", 9, FontStyle.Bold)
            };

            string[] buttonTexts = { "Главная", "Оборудование", "Записи осмотра", "Настройки", "Справка" };
            EventHandler[] buttonHandlers = {
            ShowMainContent,
            ShowForm1,
            ShowForm2,
            ShowSettings,
            ShowHelp
        };

            for (int i = 0; i < buttonTexts.Length; i++)
            {
                Button btn = new Button()
                {
                    Text = buttonTexts[i],
                    Location = new Point(10, 40 + i * 35),
                    Size = new Size(120, 30),
                    Visible = false,
                    BackColor = Color.White,
                    FlatStyle = FlatStyle.Flat
                };

                btn.Click += buttonHandlers[i];
                container.Controls.Add(btn);
            }

            container.Controls.Add(title);
        }

        // Методы для отображения контента
        private void ShowMainContent(object sender, EventArgs e)
        {
            ShowMainContent();
            CollapsePanel();
        }

        private void ShowMainContent()
        {
            ClearContentPanel();

            // Показываем стандартные элементы
            button1.Visible = true;
            button2.Visible = true;
            if (panel1 != null)
                panel1.Visible = true;

            // Добавляем заголовок
            Label title = new Label()
            {
                Text = "Главная страница",
                Font = new Font("Arial", 16, FontStyle.Bold),
                ForeColor = Color.DarkBlue,
                AutoSize = true,
                Location = new Point(20, 20)
            };
            contentPanel.Controls.Add(title);
        }

        private void ShowForm1(object sender, EventArgs e)
        {
            ClearContentPanel();

            // Создаем Form1 если еще не создана
            if (form1 == null)
            {
                form1 = new Form1();
                PrepareFormForEmbedding(form1);
            }

            // Добавляем Form1 в contentPanel
            form1.TopLevel = false;
            form1.FormBorderStyle = FormBorderStyle.None;
            form1.Dock = DockStyle.Fill;
            form1.Visible = true;

            contentPanel.Controls.Add(form1);
            CollapsePanel();
        }

        private void ShowForm2(object sender, EventArgs e)
        {
            ClearContentPanel();

            // Создаем Form2 если еще не создана
            if (form2 == null)
            {
                form2 = new Form2();
                PrepareFormForEmbedding(form2);
            }

            // Добавляем Form2 в contentPanel
            form2.TopLevel = false;
            form2.FormBorderStyle = FormBorderStyle.None;
            form2.Dock = DockStyle.Fill;
            form2.Visible = true;

            contentPanel.Controls.Add(form2);
            CollapsePanel();
        }

        // Подготовка формы для встраивания
        private void PrepareFormForEmbedding(Form form)
        {
            form.TopLevel = false;
            form.FormBorderStyle = FormBorderStyle.None;
            form.Dock = DockStyle.Fill;
            form.Visible = false;
        }

        private void ShowSettings(object sender, EventArgs e)
        {
            ClearContentPanel();

            Label title = new Label()
            {
                Text = "Настройки приложения",
                Font = new Font("Arial", 16, FontStyle.Bold),
                ForeColor = Color.DarkOrange,
                AutoSize = true,
                Location = new Point(20, 20)
            };

            // Элементы настроек
            CheckBox checkBox1 = new CheckBox()
            {
                Text = "Включить уведомления",
                Location = new Point(20, 60),
                AutoSize = true,
                Checked = true
            };

            Button saveButton = new Button()
            {
                Text = "Сохранить настройки",
                Location = new Point(20, 100),
                Size = new Size(140, 30),
                BackColor = Color.LightBlue
            };
            saveButton.Click += (s, args) => MessageBox.Show("Настройки сохранены!");

            contentPanel.Controls.AddRange(new Control[] { title, checkBox1, saveButton });
            CollapsePanel();
        }

        private void ShowHelp(object sender, EventArgs e)
        {
            ClearContentPanel();

            Label title = new Label()
            {
                Text = "Справка по приложению",
                Font = new Font("Arial", 16, FontStyle.Bold),
                ForeColor = Color.DarkRed,
                AutoSize = true,
                Location = new Point(20, 20)
            };

            TextBox helpText = new TextBox()
            {
                Location = new Point(20, 60),
                Size = new Size(400, 200),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                ReadOnly = true,
                Text = "Добро пожаловать в систему управления ГБО!\n\n" +
                       "Разделы:\n" +
                       "- Оборудование: Управление оборудованием ГБО\n" +
                       "- Записи осмотра: Просмотр и редактирование записей осмотра\n\n" +
                       "Используйте боковую панель для навигации."
            };

            contentPanel.Controls.AddRange(new Control[] { title, helpText });
            CollapsePanel();
        }

        // Очистка панели контента
        private void ClearContentPanel()
        {
            // Скрываем стандартные кнопки
            button1.Visible = false;
            button2.Visible = false;
            if (panel1 != null)
                panel1.Visible = false;

            // Скрываем встроенные формы
            if (form1 != null)
                form1.Visible = false;
            if (form2 != null)
                form2.Visible = false;

            // Удаляем все динамически добавленные контролы
            for (int i = contentPanel.Controls.Count - 1; i >= 0; i--)
            {
                Control control = contentPanel.Controls[i];
                if (control != button1 && control != button2 && control != panel1 &&
                    control != form1 && control != form2)
                {
                    contentPanel.Controls.RemoveAt(i);
                    control.Dispose();
                }
            }

            // Удаляем формы из панели (они будут добавлены заново при показе)
            if (form1 != null && contentPanel.Controls.Contains(form1))
                contentPanel.Controls.Remove(form1);
            if (form2 != null && contentPanel.Controls.Contains(form2))
                contentPanel.Controls.Remove(form2);
        }

        private void AdjustMainContent()
        {
            int panelMargin = collapsedWidth + 10;
            button1.Location = new Point(panelMargin, button1.Location.Y);
            button2.Location = new Point(panelMargin, button2.Location.Y);

            if (panel1 != null)
            {
                panel1.Location = new Point(panelMargin, panel1.Location.Y);
                panel1.Width = this.ClientSize.Width - panelMargin - 10;
            }
        }

        // Обработка изменения размера формы
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            AdjustMainContent();
        }

        // Остальные методы остаются без изменений
        private async void ToggleBtn_Click(object sender, EventArgs e)
        {
            if (isPanelExpanded)
            {
                await SlidePanel(collapsedWidth);
                HidePanelContent();
            }
            else
            {
                await SlidePanel(expandedWidth);
                ShowPanelContent();
            }
            isPanelExpanded = !isPanelExpanded;
        }

        private async Task SlidePanel(int targetWidth)
        {
            int step = 10;
            int currentWidth = sidePanel.Width;

            if (targetWidth > currentWidth)
            {
                for (int w = currentWidth; w <= targetWidth; w += step)
                {
                    sidePanel.Width = w;
                    await Task.Delay(5);
                }
            }
            else
            {
                for (int w = currentWidth; w >= targetWidth; w -= step)
                {
                    sidePanel.Width = w;
                    await Task.Delay(5);
                }
            }
            sidePanel.Width = targetWidth;
        }

        private void ShowPanelContent()
        {
            foreach (Control control in GetContentControls())
            {
                control.Visible = true;
            }
        }

        private void HidePanelContent()
        {
            foreach (Control control in GetContentControls())
            {
                control.Visible = false;
            }
        }

        private IEnumerable<Control> GetContentControls()
        {
            if (sidePanel.Controls.Count > 0)
            {
                var contentPanel = sidePanel.Controls[0];
                foreach (Control control in contentPanel.Controls)
                {
                    if (!(control is Button && control.Dock == DockStyle.Top))
                        yield return control;
                }
            }
        }

        private void CollapsePanel()
        {
            if (isPanelExpanded)
            {
                ToggleBtn_Click(null, null);
            }
        }

        // Ваши существующие методы
        private void button1_Click(object sender, EventArgs e)
        {
            // Показываем Form1 при нажатии на button1
            ShowForm1(sender, e);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            // Показываем Form2 при нажатии на button2
            ShowForm2(sender, e);
        }

        private void panel1_Paint(object sender, PaintEventArgs e)
        {
            // Ваш существующий код
        }

        // Очистка ресурсов при закрытии формы
        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            base.OnFormClosed(e);
            form1?.Dispose();
            form2?.Dispose();
        }
    }
}
