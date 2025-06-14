using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;

namespace TheoryOfComputationSimulator
{
    public partial class MainForm : Form
    {
        private readonly List<string> teamMembers = new List<string>
        {
            "أسامه الوجيه", "محمود أغا", "مصعب الجعشني", "مامون عياش",
            "محمد العواضي", "ادريس عبد الهادي", "بسام سمير",
            "بلال البريهي", "لؤي ردمان"
        };

        public MainForm()
        {
            //InitializeComponent();
            SetupUI();
        }

        private void SetupUI()
        {
            // إعدادات النافذة الرئيسية
            this.Text = "نظام محاكاة النظرية الحسابية";
            this.Size = new Size(1000, 700);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.White;
            this.Font = new Font("Tahoma", 10);
            this.Padding = new Padding(10);
            this.AutoScroll = true; // تمكين التمرير إذا لزم الأمر

            // لوحة رئيسية للتنظيم
            TableLayoutPanel mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                AutoSize = true,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding = new Padding(10)
            };
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 15));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 25));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 10));
            this.Controls.Add(mainLayout);

            // رأس الصفحة
            Panel headerPanel = CreateHeaderPanel();
            mainLayout.Controls.Add(headerPanel, 0, 0);

            // لوحة معلومات المشروع
            Panel projectPanel = CreateProjectPanel();
            mainLayout.Controls.Add(projectPanel, 0, 1);

            // لوحة أزرار الآلات
            Panel machinesPanel = CreateMachinesPanel();
            mainLayout.Controls.Add(machinesPanel, 0, 2);

            // لوحة الأزرار الإضافية
            Panel footerPanel = CreateFooterPanel();
            mainLayout.Controls.Add(footerPanel, 0, 3);
        }

        private Panel CreateHeaderPanel()
        {
            Panel headerPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(240, 245, 255),
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(10)
            };

            // عنوان المشروع
            Label lblTitle = new Label
            {
                Text = "محاكاة النظرية الحسابية",
                Font = new Font("Tahoma", 24, FontStyle.Bold),
                ForeColor = Color.DarkBlue,
                AutoSize = true,
                Location = new Point(20, 10)
            };
            headerPanel.Controls.Add(lblTitle);

            // المشرف
            Label lblSupervisor = new Label
            {
                Text = "تحت إشراف: الدكتور خالد الكحسه",
                Font = new Font("Tahoma", 14, FontStyle.Regular),
                ForeColor = Color.DarkSlateBlue,
                AutoSize = true,
                Location = new Point(20, 50)
            };
            headerPanel.Controls.Add(lblSupervisor);

            // اسم المجموعة
            Label lblGroup = new Label
            {
                Text = "المجموعة السادسة",
                Font = new Font("Tahoma", 14, FontStyle.Bold),
                ForeColor = Color.DarkCyan,
                AutoSize = true,
                Location = new Point(20, 80)
            };
            headerPanel.Controls.Add(lblGroup);

            return headerPanel;
        }

        private Panel CreateProjectPanel()
        {
            Panel projectPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(250, 252, 255),
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(15)
            };

            // أعضاء الفريق
            Label lblTeam = new Label
            {
                Text = "أعضاء الفريق:",
                Font = new Font("Tahoma", 14, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(20, 10)
            };
            projectPanel.Controls.Add(lblTeam);

            // عرض أسماء الطلاب في قائمة منظمة
            TableLayoutPanel teamTable = new TableLayoutPanel
            {
                Location = new Point(40, 50),
                Size = new Size(900, 150),
                ColumnCount = 3,
                RowCount = 4,
                AutoSize = true
            };
            teamTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33));
            teamTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33));
            teamTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34));

            foreach (string member in teamMembers)
            {
                Label memberLabel = new Label
                {
                    Text = "• " + member,
                    Font = new Font("Tahoma", 12),
                    AutoSize = true,
                    Margin = new Padding(10, 8, 20, 8)
                };
                teamTable.Controls.Add(memberLabel);
            }
            projectPanel.Controls.Add(teamTable);

            // وصف المشروع
            Label lblDescription = new Label
            {
                Text = "يهدف هذا المشروع إلى محاكاة النماذج الحاسوبية الأساسية في نظرية الحوسبة، بما في ذلك " +
                       "آلات الحالات المنتهية، آلات الدفع الذاتي، وآلات تورنغ. يتيح البرنامج للمستخدمين تعريف " +
                       "هذه الآلات ومحاكاتها خطوة بخطوة.",
                Font = new Font("Tahoma", 12),
                AutoSize = true,
                Location = new Point(20, 220),
                MaximumSize = new Size(900, 0)
            };
            projectPanel.Controls.Add(lblDescription);

            return projectPanel;
        }

        private Panel CreateMachinesPanel()
        {
            Panel machinesPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(245, 250, 255),
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(20)
            };

            // عنوان قسم الآلات
            Label lblMachines = new Label
            {
                Text = "اختر نوع الآلة للمحاكاة:",
                Font = new Font("Tahoma", 16, FontStyle.Bold),
                ForeColor = Color.DarkBlue,
                AutoSize = true,
                Location = new Point(20, 10)
            };
            machinesPanel.Controls.Add(lblMachines);

            // لوحة أزرار الآلات
            FlowLayoutPanel buttonsPanel = new FlowLayoutPanel
            {
                Location = new Point(50, 50),
                Size = new Size(880, 250),
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoScroll = true
            };

            // زر آلة الحالات المنتهية
            Button btnFiniteStateMachine = CreateMachineButton(
                "آلة الحالات المنتهية",
                Properties.Resources.FiniteStateIcon
            );
            btnFiniteStateMachine.Click += BtnFiniteStateMachine_Click;
            buttonsPanel.Controls.Add(btnFiniteStateMachine);

            // زر آلة الدفع الذاتي
            Button btnPushdownAutomata = CreateMachineButton(
                "آلة الدفع الذاتي",
                Properties.Resources.PushdownIcon
            );
            btnPushdownAutomata.Click += BtnPushdownAutomata_Click;
            buttonsPanel.Controls.Add(btnPushdownAutomata);

            // زر آلة تورنغ
            Button btnTuringMachine = CreateMachineButton(
                "آلة تورنغ",
                Properties.Resources.TuringIcon
            );
            btnTuringMachine.Click += BtnTuringMachine_Click;
            buttonsPanel.Controls.Add(btnTuringMachine);

            machinesPanel.Controls.Add(buttonsPanel);

            return machinesPanel;
        }

        private Button CreateMachineButton(string text, Image icon)
        {
            return new Button
            {
                Text = text,
                Size = new Size(260, 200),
                Font = new Font("Tahoma", 14, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.SteelBlue,
                TextImageRelation = TextImageRelation.ImageAboveText,
                Image = icon,
                ImageAlign = ContentAlignment.MiddleCenter,
                TextAlign = ContentAlignment.BottomCenter,
                FlatStyle = FlatStyle.Flat,
                Padding = new Padding(10),
                Margin = new Padding(20)
            };
        }

        private Panel CreateFooterPanel()
        {
            Panel footerPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(240, 245, 255),
                Padding = new Padding(10)
            };

            // زر المساعدة
            Button btnHelp = new Button
            {
                Text = "مساعدة",
                Size = new Size(120, 40),
                Location = new Point(200, 10),
                Font = new Font("Tahoma", 12, FontStyle.Bold),
                ForeColor = Color.DarkGreen,
                BackColor = Color.FromArgb(220, 255, 220),
                FlatStyle = FlatStyle.Flat,
                Image = Properties.Resources.HelpIcon,
                ImageAlign = ContentAlignment.MiddleLeft,
                TextImageRelation = TextImageRelation.TextBeforeImage
            };
            btnHelp.Click += BtnHelp_Click;
            footerPanel.Controls.Add(btnHelp);

            // زر التقرير
            Button btnReport = new Button
            {
                Text = "تقرير المشروع",
                Size = new Size(150, 40),
                Location = new Point(400, 10),
                Font = new Font("Tahoma", 12, FontStyle.Bold),
                ForeColor = Color.DarkBlue,
                BackColor = Color.FromArgb(220, 230, 255),
                FlatStyle = FlatStyle.Flat,
                Image = Properties.Resources.ReportIcon,
                ImageAlign = ContentAlignment.MiddleLeft,
                TextImageRelation = TextImageRelation.TextBeforeImage
            };
            btnReport.Click += BtnReport_Click;
            footerPanel.Controls.Add(btnReport);

            // زر الخروج
            Button btnExit = new Button
            {
                Text = "خروج",
                Size = new Size(120, 40),
                Location = new Point(650, 10),
                Font = new Font("Tahoma", 12, FontStyle.Bold),
                ForeColor = Color.DarkRed,
                BackColor = Color.FromArgb(255, 220, 220),
                FlatStyle = FlatStyle.Flat,
                Image = Properties.Resources.ExitIcon,
                ImageAlign = ContentAlignment.MiddleLeft,
                TextImageRelation = TextImageRelation.TextBeforeImage
            };
            btnExit.Click += (s, e) => this.Close();
            footerPanel.Controls.Add(btnExit);

            return footerPanel;
        }

        private void BtnFiniteStateMachine_Click(object sender, EventArgs e)
        {
            FiniteAutomatonForm fsmForm = new FiniteAutomatonForm();
            fsmForm.Show();
        }

        private void BtnPushdownAutomata_Click(object sender, EventArgs e)
        {
            PushdownAutomataForm pdaForm = new PushdownAutomataForm();
            pdaForm.Show();
        }

        private void BtnTuringMachine_Click(object sender, EventArgs e)
        {
            TuringMachineForm tmForm = new TuringMachineForm();
            tmForm.Show();
        }

        private void BtnHelp_Click(object sender, EventArgs e)
        {
            // نافذة المساعدة المبسطة
            string helpText = "كيفية استخدام البرنامج:\n\n" +
                "1. اختر نوع الآلة من الأزرار الرئيسية\n" +
                "2. في النافذة الجديدة، قم بتعريف خصائص الآلة:\n" +
                "   - الحالات والرموز والانتقالات\n" +
                "3. أدخل سلسلة الإدخال للمحاكاة\n" +
                "4. استخدم أزرار التحكم لتشغيل المحاكاة\n" +
                "5. تتبع النتائج في لوحة الإخراج\n\n" +
                "ميزات متقدمة:\n" +
                "- تحويل بين أنواع الآلات\n" +
                "- تصدير النتائج كصور أو تقارير\n" +
                "- أمثلة جاهزة للاختبار السريع";

            MessageBox.Show(helpText, "دليل المستخدم",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnReport_Click(object sender, EventArgs e)
        {
            // نافذة تقرير المشروع
            string reportText = "تقرير مشروع محاكاة النظرية الحسابية\n\n" +
                "المشرف: الدكتور خالد الكحسو\n" +
                "المجموعة: السادسة\n" +
                "تاريخ التسليم: يونيو 2024\n\n" +
                "أهداف المشروع:\n" +
                "- تقديم أداة تعليمية لمحاكاة النماذج الحاسوبية\n" +
                "- تبسيط مفاهيم نظرية الحوسبة للطلاب\n" +
                "- تمكين التجارب التفاعلية مع الآلات النظرية\n\n" +
                "الميزات الرئيسية:\n" +
                "1. محاكاة آلات الحالات المنتهية\n" +
                "2. محاكاة آلات الدفع الذاتي\n" +
                "3. محاكاة آلات تورنغ\n" +
                "4. تصدير النتائج والتقارير\n\n" +
                "أعضاء الفريق:\n" +
                string.Join("\n", teamMembers);

            MessageBox.Show(reportText, "تقرير المشروع",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}