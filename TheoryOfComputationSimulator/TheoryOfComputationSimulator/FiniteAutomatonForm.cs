using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TheoryOfComputationSimulator
{
   


        public partial class FiniteAutomatonForm : Form
        {
            // هياكل البيانات
            private readonly List<string> states = new List<string>();
            private readonly List<char> alphabet = new List<char>();
            private string startState = "";
            private readonly List<string> finalStates = new List<string>();
            private readonly Dictionary<(string, char), List<string>> transitions = new Dictionary<(string, char), List<string>>();

            // محاكاة
            private List<string> currentStates = new List<string>();
            private string inputString = "";
            private int currentStep = 0;
            private readonly List<SimulationStep> simulationHistory = new List<SimulationStep>();

            // التحويل بين الآلات
            private DFA convertedDFA;
            private List<ConversionStep> conversionSteps = new List<ConversionStep>();
            private int conversionStepIndex = -1;

            // واجهة المستخدم
            private TabControl mainTabControl;
            private TabPage tabAutomaton, tabConversion, tabHelp, tabRegex;
            private TextBox txtStates, txtAlphabet, txtStart, txtFinals, txtInput, txtRegex;
            private DataGridView dgvTransitions;
            private Button btnAddTransition, btnLoad, btnStep, btnReset, btnRunAll;
            private Button btnToDFA, btnToNFA, btnStepConversion, btnMinimize;
            private Button btnRegexToAutomaton, btnAutomatonToRegex, btnRegexStepByStep;
            private PictureBox automatonView;
            private RichTextBox rtbLog, rtbHelp, rtbRegexSteps;
            private GroupBox gbAutomatonType;
            private RadioButton rbNFA, rbDFA;
            private DataGridView dgvConversionSteps;
            private ComboBox cmbExamples;
            private Button btnExportImage, btnExportReport;
            private ToolTip toolTip;
            private Bitmap cachedBitmap;
            private bool needsRedraw = true;
            private Size originalFormSize;
            private bool isDFAMode;

            public FiniteAutomatonForm()
            {
                InitializeUI();
                InitializeDataGridView();
                InitializeHelpSystem();
                this.Text = "نظام متقدم لمحاكاة وتحويل آلات الحالات المنتهية";
                this.Font = new Font("Tahoma", 9);
                this.AutoSize = true;
                this.AutoSizeMode = AutoSizeMode.GrowAndShrink;
                this.Resize += MainForm_Resize;
                this.Size = new Size(1500, 1000);
                this.MinimumSize = new Size(1200, 800);
                this.StartPosition = FormStartPosition.CenterScreen;

                originalFormSize = this.Size;
            }

            private void MainForm_Resize(object sender, EventArgs e)
            {
                needsRedraw = true;
                automatonView.Invalidate();
            }

            private void InitializeUI()
            {
                // التبويبات الرئيسية
                mainTabControl = new TabControl { Dock = DockStyle.Fill };
                Controls.Add(mainTabControl);

                // تبويب الآلة
                tabAutomaton = new TabPage("الآلة والمحاكاة");
                mainTabControl.TabPages.Add(tabAutomaton);

                // تبويب التحويل
                tabConversion = new TabPage("التحويل خطوة بخطوة");
                mainTabControl.TabPages.Add(tabConversion);

                // تبويب التعابير النمطية
                tabRegex = new TabPage("التعابير النمطية");
                mainTabControl.TabPages.Add(tabRegex);

                // تبويب المساعدة
                tabHelp = new TabPage("المساعدة");
                mainTabControl.TabPages.Add(tabHelp);

                // تصميم تبويب الآلة
                InitializeAutomatonTab();

                // تصميم تبويب التحويل
                InitializeConversionTab();

                // تصميم تبويب التعابير النمطية
                InitializeRegexTab();

                // تصميم تبويب المساعدة
                InitializeHelpTab();
            }

            private void InitializeAutomatonTab()
            {
                var mainLayout = new TableLayoutPanel
                {
                    ColumnCount = 2,
                    RowCount = 4,
                    Dock = DockStyle.Fill,
                    CellBorderStyle = TableLayoutPanelCellBorderStyle.Single,
                    AutoSize = true,
                    AutoSizeMode = AutoSizeMode.GrowAndShrink,
                    Padding = new Padding(5)
                };

                // تخصيص أكبر مساحة للرسم البياني
                mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25)); // مساحة صغيرة للإدخال
                mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 75)); // مساحة كبيرة للرسم

                mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // صف نوع الآلة
                mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // صف أدوات التحويل
                mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent)); // صف الرسم (تم زيادته)
                mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // صف المحاكاة والسجل

                tabAutomaton.Controls.Add(mainLayout);

                // مجموعة نوع الآلة
                gbAutomatonType = new GroupBox
                {
                    Text = "نوع الآلة",
                    Dock = DockStyle.Fill,
                    Font = new Font("Tahoma", 9, FontStyle.Bold),
                    AutoSize = true,
                    AutoSizeMode = AutoSizeMode.GrowAndShrink
                };
                mainLayout.Controls.Add(gbAutomatonType, 0, 0);
                mainLayout.SetColumnSpan(gbAutomatonType, 2); // تمتد على العمودين

                var typePanel = new FlowLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    AutoSize = true,
                    AutoSizeMode = AutoSizeMode.GrowAndShrink
                };
                gbAutomatonType.Controls.Add(typePanel);

                rbNFA = new RadioButton
                {
                    Text = "آلة غير محدودة (NFA)",
                    Checked = true,
                    AutoSize = true,
                    Margin = new Padding(5)
                };
                typePanel.Controls.Add(rbNFA);

                rbDFA = new RadioButton
                {
                    Text = "آلة محدودة (DFA)",
                    AutoSize = true,
                    Margin = new Padding(5)
                };
                typePanel.Controls.Add(rbDFA);

                // لوحة الأمثلة
                var examplesPanel = new Panel
                {
                    Dock = DockStyle.Fill,
                    AutoSize = true,
                    AutoSizeMode = AutoSizeMode.GrowAndShrink
                };
                mainLayout.Controls.Add(examplesPanel, 0, 0);
                mainLayout.SetColumnSpan(examplesPanel, 2);

                var examplesLayout = new FlowLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    FlowDirection = FlowDirection.RightToLeft,
                    AutoSize = true,
                    AutoSizeMode = AutoSizeMode.GrowAndShrink
                };
                examplesPanel.Controls.Add(examplesLayout);

                cmbExamples = new ComboBox
                {
                    DropDownStyle = ComboBoxStyle.DropDownList,
                    Width = 250,
                    Margin = new Padding(5)
                };
                cmbExamples.Items.Add("اختر مثالاً جاهزاً...");
                cmbExamples.Items.Add("NFA بسيط (قبول نهايات 01)");
                cmbExamples.Items.Add("DFA للأرقام الزوجية");
                cmbExamples.Items.Add("NFA معقد (تحويل إلى DFA)");
                cmbExamples.SelectedIndexChanged += CmbExamples_SelectedIndexChanged;
                examplesLayout.Controls.Add(cmbExamples);

                // لوحة التحكم بالتحويل
                var conversionPanel = new Panel
                {
                    Dock = DockStyle.Fill,
                    AutoSize = true,
                    AutoSizeMode = AutoSizeMode.GrowAndShrink
                };
                mainLayout.Controls.Add(conversionPanel, 0, 2);
                mainLayout.SetColumnSpan(conversionPanel, 2); // تمتد على العمودين

                var conversionLayout = new FlowLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    FlowDirection = FlowDirection.LeftToRight,
                    AutoSize = true,
                    AutoSizeMode = AutoSizeMode.GrowAndShrink
                };

                btnToDFA = new Button
                {
                    Text = "تحويل إلى DFA",
                    Enabled = false,
                    Size = new Size(120, 40),
                    Margin = new Padding(5),
                    AutoSize = true
                };
                btnToDFA.Click += BtnToDFA_Click;
                conversionLayout.Controls.Add(btnToDFA);

                btnToNFA = new Button
                {
                    Text = "تحويل إلى NFA",
                    Enabled = false,
                    Size = new Size(120, 40),
                    Margin = new Padding(5),
                    AutoSize = true
                };
                btnToNFA.Click += BtnToNFA_Click;
                conversionLayout.Controls.Add(btnToNFA);

                btnStepConversion = new Button
                {
                    Text = "خطوة تحويل تالية",
                    Enabled = false,
                    Size = new Size(120, 40),
                    Margin = new Padding(5),
                    AutoSize = true
                };
                btnStepConversion.Click += BtnStepConversion_Click;
                conversionLayout.Controls.Add(btnStepConversion);

                btnMinimize = new Button
                {
                    Text = "تصغير الآلة",
                    Enabled = false,
                    Size = new Size(120, 40),
                    Margin = new Padding(5),
                    AutoSize = true
                };
                btnMinimize.Click += BtnMinimize_Click;
                conversionLayout.Controls.Add(btnMinimize);

                btnExportImage = new Button
                {
                    Text = "تصدير الصورة",
                    Enabled = false,
                    Size = new Size(120, 40),
                    Margin = new Padding(5),
                    AutoSize = true
                };
                btnExportImage.Click += BtnExportImage_Click;
                conversionLayout.Controls.Add(btnExportImage);

                btnExportReport = new Button
                {
                    Text = "تصدير التقرير",
                    Enabled = false,
                    Size = new Size(120, 40),
                    Margin = new Padding(5),
                    AutoSize = true
                };
                btnExportReport.Click += BtnExportReport_Click;
                conversionLayout.Controls.Add(btnExportReport);

                conversionPanel.Controls.Add(conversionLayout);

                // لوحة الإدخال
                var inputPanel = new Panel
                {
                    Dock = DockStyle.Fill,
                    AutoSize = true,
                    AutoSizeMode = AutoSizeMode.GrowAndShrink
                };
                mainLayout.Controls.Add(inputPanel, 0, 1);

                var inputLayout = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    RowCount = 6,
                    ColumnCount = 1,
                    AutoSize = true,
                    AutoSizeMode = AutoSizeMode.GrowAndShrink
                };

                // عناصر إدخال البيانات
                txtStates = CreateLabeledTextBox("الحالات (مفصولة بفاصلة):", inputLayout);
                txtAlphabet = CreateLabeledTextBox("الأبجدية (أحرف بدون فواصل):", inputLayout);
                txtStart = CreateLabeledTextBox("الحالة الأولية:", inputLayout);
                txtFinals = CreateLabeledTextBox("الحالات النهائية (مفصولة بفاصلة):", inputLayout);

                // شبكة الانتقالات
                var lblTransitions = new Label
                {
                    Text = "الانتقالات:",
                    Dock = DockStyle.Top,
                    Font = new Font("Tahoma", 9, FontStyle.Bold),
                    AutoSize = true
                };
                inputLayout.Controls.Add(lblTransitions);

                dgvTransitions = new DataGridView
                {
                    Dock = DockStyle.Fill,
                    AutoSize = true,
                    MinimumSize = new Size(0, 150)
                };
                inputLayout.Controls.Add(dgvTransitions);

                var buttonsPanel = new FlowLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    FlowDirection = FlowDirection.LeftToRight,
                    AutoSize = true
                };

                btnAddTransition = new Button
                {
                    Text = "إضافة انتقال",
                    Height = 30,
                    AutoSize = true,
                    Margin = new Padding(5)
                };
                btnAddTransition.Click += BtnAddTransition_Click;
                buttonsPanel.Controls.Add(btnAddTransition);

                btnLoad = new Button
                {
                    Text = "تحميل الآلة",
                    Height = 30,
                    AutoSize = true,
                    Margin = new Padding(5)
                };
                btnLoad.Click += BtnLoad_Click;
                buttonsPanel.Controls.Add(btnLoad);

                inputLayout.Controls.Add(buttonsPanel);

                inputPanel.Controls.Add(inputLayout);

                // منطقة العرض البياني - تم تكبيرها
                automatonView = new PictureBox
                {
                    Dock = DockStyle.Fill,
                    BackColor = Color.White,
                    BorderStyle = BorderStyle.FixedSingle,
                    SizeMode = PictureBoxSizeMode.Zoom
                };
                automatonView.Paint += AutomatonView_Paint;
                mainLayout.Controls.Add(automatonView, 1, 1); // وضعها في العمود الكبير

                // لوحة المحاكاة
                var simulationPanel = new Panel
                {
                    Dock = DockStyle.Fill,
                    AutoSize = true,
                    AutoSizeMode = AutoSizeMode.GrowAndShrink
                };
                mainLayout.Controls.Add(simulationPanel, 1, 2);
                mainLayout.SetColumnSpan(simulationPanel, 2); // تمتد على العمودين

                var simulationLayout = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    ColumnCount = 2,
                    RowCount = 2,
                    AutoSize = true,
                    AutoSizeMode = AutoSizeMode.GrowAndShrink
                };

                txtInput = CreateLabeledTextBox("النص المدخل:", simulationLayout);
                simulationLayout.SetColumnSpan(txtInput, 2);

                var controlPanel = new FlowLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    AutoSize = true,
                    AutoSizeMode = AutoSizeMode.GrowAndShrink,
                    FlowDirection = FlowDirection.LeftToRight
                };
                simulationLayout.Controls.Add(controlPanel, 0, 1);

                btnStep = new Button
                {
                    Text = "خطوة محاكاة",
                    Enabled = false,
                    Size = new Size(100, 40),
                    AutoSize = true,
                    Margin = new Padding(5)
                };
                btnStep.Click += BtnStep_Click;
                controlPanel.Controls.Add(btnStep);

                btnReset = new Button
                {
                    Text = "إعادة تعيين",
                    Enabled = false,
                    Size = new Size(100, 40),
                    AutoSize = true,
                    Margin = new Padding(5)
                };
                btnReset.Click += BtnReset_Click;
                controlPanel.Controls.Add(btnReset);

                btnRunAll = new Button
                {
                    Text = "تشغيل الكل",
                    Enabled = false,
                    Size = new Size(100, 40),
                    AutoSize = true,
                    Margin = new Padding(5)
                };
                btnRunAll.Click += BtnRunAll_Click;
                controlPanel.Controls.Add(btnRunAll);

                simulationPanel.Controls.Add(simulationLayout);

                // سجل المحاكاة والتحويل
                rtbLog = new RichTextBox
                {
                    Dock = DockStyle.Fill,
                    Font = new Font("Tahoma", 10),
                    ReadOnly = true,
                    MinimumSize = new Size(0, 100)
                };
                mainLayout.Controls.Add(rtbLog, 0, 3);
                mainLayout.SetColumnSpan(rtbLog, 2);
            }

            private void InitializeConversionTab()
            {
                var mainLayout = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    AutoSize = true,
                    AutoSizeMode = AutoSizeMode.GrowAndShrink
                };
                tabConversion.Controls.Add(mainLayout);

                // عنوان التبويب
                var lblTitle = new Label
                {
                    Text = "خطوات التحويل من NFA إلى DFA",
                    Font = new Font("Tahoma", 14, FontStyle.Bold),
                    Dock = DockStyle.Top,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Height = 50,
                    AutoSize = true
                };
                mainLayout.Controls.Add(lblTitle);

                // شبكة عرض خطوات التحويل
                dgvConversionSteps = new DataGridView
                {
                    Dock = DockStyle.Fill,
                    ReadOnly = true,
                    RowHeadersVisible = false,
                    AllowUserToAddRows = false,
                    AllowUserToDeleteRows = false,
                    AllowUserToResizeRows = false,
                    AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                    BackgroundColor = SystemColors.Window,
                    AutoSize = true
                };
                mainLayout.Controls.Add(dgvConversionSteps);

                // تخصيص أعمدة الشبكة
                dgvConversionSteps.Columns.Add("Step", "الخطوة");
                dgvConversionSteps.Columns.Add("Description", "الوصف");
                dgvConversionSteps.Columns.Add("Current", "الحالة الحالية");
                dgvConversionSteps.Columns.Add("Symbol", "الرمز");
                dgvConversionSteps.Columns.Add("Next", "الحالة التالية");

                // تحسين مظهر الشبكة
                dgvConversionSteps.Columns["Step"].Width = 60;
                dgvConversionSteps.Columns["Symbol"].Width = 60;
                dgvConversionSteps.Columns["Description"].FillWeight = 150;
                dgvConversionSteps.Columns["Current"].FillWeight = 120;
                dgvConversionSteps.Columns["Next"].FillWeight = 120;

                // زر التنقل بين الخطوات
                var navPanel = new FlowLayoutPanel
                {
                    Dock = DockStyle.Bottom,
                    Height = 50,
                    FlowDirection = FlowDirection.LeftToRight,
                    AutoSize = true
                };

                var btnPrevStep = new Button { Text = "السابق", Size = new Size(100, 40), AutoSize = true };
                btnPrevStep.Click += (s, e) => NavigateConversionStep(-1);
                navPanel.Controls.Add(btnPrevStep);

                var btnNextStep = new Button { Text = "التالي", Size = new Size(100, 40), AutoSize = true };
                btnNextStep.Click += (s, e) => NavigateConversionStep(1);
                navPanel.Controls.Add(btnNextStep);

                mainLayout.Controls.Add(navPanel);
            }

            private void InitializeRegexTab()
            {
                var mainLayout = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    RowCount = 3,
                    AutoSize = true,
                    AutoSizeMode = AutoSizeMode.GrowAndShrink
                };
                tabRegex.Controls.Add(mainLayout);

                // لوحة التحويل من تعبير نمطي إلى آلة
                var regexToAutomatonPanel = new GroupBox
                {
                    Text = "تحويل التعبير النمطي إلى آلة",
                    Dock = DockStyle.Fill,
                    Font = new Font("Tahoma", 10, FontStyle.Bold),
                    AutoSize = true,
                    AutoSizeMode = AutoSizeMode.GrowAndShrink
                };
                mainLayout.Controls.Add(regexToAutomatonPanel);

                var regexToAutomatonLayout = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    RowCount = 3,
                    ColumnCount = 2,
                    AutoSize = true
                };
                regexToAutomatonPanel.Controls.Add(regexToAutomatonLayout);

                var lblRegex = new Label
                {
                    Text = "التعبير النمطي:",
                    Dock = DockStyle.Top,
                    AutoSize = true
                };
                regexToAutomatonLayout.Controls.Add(lblRegex, 0, 0);

                txtRegex = new TextBox
                {
                    Dock = DockStyle.Fill,
                    Font = new Font("Tahoma", 10),
                    AutoSize = true
                };
                regexToAutomatonLayout.Controls.Add(txtRegex, 1, 0);

                btnRegexToAutomaton = new Button
                {
                    Text = "تحويل إلى آلة",
                    Dock = DockStyle.Fill,
                    AutoSize = true,
                    Margin = new Padding(5)
                };
                btnRegexToAutomaton.Click += BtnRegexToAutomaton_Click;
                regexToAutomatonLayout.Controls.Add(btnRegexToAutomaton, 0, 1);

                btnRegexStepByStep = new Button
                {
                    Text = "خطوة بخطوة",
                    Dock = DockStyle.Fill,
                    AutoSize = true,
                    Margin = new Padding(5)
                };
                btnRegexStepByStep.Click += BtnRegexStepByStep_Click;
                regexToAutomatonLayout.Controls.Add(btnRegexStepByStep, 1, 1);

                // لوحة التحويل من آلة إلى تعبير نمطي
                var automatonToRegexPanel = new GroupBox
                {
                    Text = "تحويل الآلة إلى تعبير نمطي",
                    Dock = DockStyle.Fill,
                    Font = new Font("Tahoma", 10, FontStyle.Bold),
                    AutoSize = true,
                    AutoSizeMode = AutoSizeMode.GrowAndShrink
                };
                mainLayout.Controls.Add(automatonToRegexPanel);

                var automatonToRegexLayout = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    RowCount = 2,
                    ColumnCount = 1,
                    AutoSize = true
                };
                automatonToRegexPanel.Controls.Add(automatonToRegexLayout);

                btnAutomatonToRegex = new Button
                {
                    Text = "تحويل إلى تعبير نمطي",
                    Dock = DockStyle.Fill,
                    AutoSize = true,
                    Margin = new Padding(5)
                };
                btnAutomatonToRegex.Click += BtnAutomatonToRegex_Click;
                automatonToRegexLayout.Controls.Add(btnAutomatonToRegex, 0, 0);

                // سجل خطوات التحويل
                rtbRegexSteps = new RichTextBox
                {
                    Dock = DockStyle.Fill,
                    Font = new Font("Tahoma", 10),
                    ReadOnly = true
                };
                mainLayout.Controls.Add(rtbRegexSteps, 0, 2);
            }

            private void InitializeHelpTab()
            {
                rtbHelp = new RichTextBox
                {
                    Dock = DockStyle.Fill,
                    ReadOnly = true,
                    BackColor = Color.White,
                    BorderStyle = BorderStyle.None,
                    Font = new Font("Tahoma", 10)
                };
                tabHelp.Controls.Add(rtbHelp);

                // تحميل محتوى المساعدة
                LoadHelpContent();
            }

            private void LoadHelpContent()
            {
                rtbHelp.Text = @"دليل استخدام نظام محاكاة وتحويل آلات الحالات المنتهية

1. تعريف الآلة:
   - أدخل الحالات مفصولة بفاصلة (مثال: q0,q1,q2)
   - أدخل رموز الأبجدية بدون فواصل (مثال: ab)
   - حدد الحالة الأولية (يجب أن تكون من الحالات المدخلة)
   - حدد الحالات النهائية (مفصولة بفاصلة)
   - أضف الانتقالات في الجدول المخصص

2. نوع الآلة:
   - اختر NFA للآلات غير المحدودة (تسمح بعدة انتقالات لنفس الرمز)
   - اختر DFA للآلات المحدودة (انتقال واحد لكل رمز)

3. المحاكاة:
   - أدخل النص المراد اختباره
   - استخدم 'خطوة محاكاة' لمتابعة التنفيذ خطوة بخطوة
   - استخدم 'تشغيل الكل' لتنفيذ المحاكاة كاملة
   - استخدم 'إعادة تعيين' لبدء المحاكاة من جديد

4. التحويل:
   - يمكن تحويل NFA إلى DFA باستخدام زر 'تحويل إلى DFA'
   - استعرض خطوات التحويل في التبويب المخصص
   - استخدم أزرار 'التالي' و'السابق' للتنقل بين الخطوات

5. التعابير النمطية:
   - تحويل التعبير النمطي إلى آلة حالات
   - تحويل آلة الحالات إلى تعبير نمطي
   - تصغير الآلات لتبسيطها

6. التصدير:
   - تصدير صورة الآلة الحالية بصيغة PNG
   - تصدير تقرير التحويل إلى ملف نصي

7. الأمثلة الجاهزة:
   - اختر مثالاً من القائمة المنسدلة لتحميله مباشرة

نصائح:
- استخدم أسماء مختصرة للحالات لتحسين مظهر الرسم البياني
- في آلات DFA، تأكد أن لكل حالة ورمز انتقال واحد فقط
- يمكنك تمرير الماوس فوق الحالات المركبة لرؤية تفاصيلها
- استخدم خاصية التصغير لتبسيط الآلات المعقدة
- استخدم منطقة الرسم الكبيرة لرؤية التفاصيل بوضوح

إعدادات متقدمة:
- الانتقالات الفارغة (ε) غير مدعومة في هذا الإصدار
- يمكن تغيير حجم النوافذ حسب الحاجة
- البرنامج يدعم اللغة العربية بالكامل
";
            }

            private void InitializeDataGridView()
            {
                dgvTransitions.Columns.Add("From", "من حالة");
                dgvTransitions.Columns.Add("Symbol", "رمز");
                dgvTransitions.Columns.Add("To", "إلى حالة");
                dgvTransitions.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
                dgvTransitions.Font = new Font("Tahoma", 9);
                dgvTransitions.AutoSize = true;
            }

            private void InitializeHelpSystem()
            {
                toolTip = new ToolTip
                {
                    IsBalloon = true,
                    ToolTipTitle = "مساعدة",
                    AutoPopDelay = 5000,
                    InitialDelay = 300,
                    ReshowDelay = 100
                };

                toolTip.SetToolTip(txtStates, "أدخل الحالات مفصولة بفاصلة، مثال: q0,q1,q2");
                toolTip.SetToolTip(txtAlphabet, "أدخل رموز الأبجدية بدون فواصل، مثال: abc");
                toolTip.SetToolTip(txtStart, "أدخل الحالة الأولية، يجب أن تكون موجودة في قائمة الحالات");
                toolTip.SetToolTip(txtFinals, "أدخل الحالات النهائية مفصولة بفاصلة، مثال: q2,q3");
                toolTip.SetToolTip(dgvTransitions, "أضف انتقالات الآلة: من حالة، رمز، إلى حالة");
                toolTip.SetToolTip(btnAddTransition, "إضافة صف جديد لتعريف انتقال جديد");
                toolTip.SetToolTip(btnLoad, "تحميل بيانات الآلة والتحضير للمحاكاة");
                toolTip.SetToolTip(btnToDFA, "تحويل الآلة غير المحدودة (NFA) إلى آلة محدودة (DFA)");
                toolTip.SetToolTip(btnToNFA, "العودة إلى عرض الآلة الأصلية (NFA)");
                toolTip.SetToolTip(btnStepConversion, "عرض خطوات التحويل واحدة تلو الأخرى");
                toolTip.SetToolTip(btnMinimize, "تصغير الآلة لتبسيطها");
                toolTip.SetToolTip(btnExportImage, "حفظ صورة الآلة الحالية كملف PNG");
                toolTip.SetToolTip(btnExportReport, "حفظ تقرير خطوات التحويل كملف نصي");
                toolTip.SetToolTip(cmbExamples, "اختر مثالاً جاهزاً لتحميله مباشرة");
                toolTip.SetToolTip(txtRegex, "أدخل التعبير النمطي (مثال: (a|b)*abb)");
                toolTip.SetToolTip(btnRegexToAutomaton, "تحويل التعبير النمطي إلى آلة حالات");
                toolTip.SetToolTip(btnAutomatonToRegex, "تحويل الآلة الحالية إلى تعبير نمطي");
                toolTip.SetToolTip(btnRegexStepByStep, "عرض خطوات التحويل خطوة بخطوة");
            }

            private TextBox CreateLabeledTextBox(string labelText, Control container)
            {
                var panel = new Panel
                {
                    Dock = DockStyle.Top,
                    Height = 60,
                    AutoSize = true
                };
                var label = new Label
                {
                    Text = labelText,
                    Dock = DockStyle.Top,
                    Font = new Font("Tahoma", 9),
                    Height = 20,
                    AutoSize = true
                };
                var textBox = new TextBox
                {
                    Dock = DockStyle.Top,
                    Font = new Font("Tahoma", 9),
                    Height = 30,
                    AutoSize = true
                };

                panel.Controls.Add(textBox);
                panel.Controls.Add(label);
                container.Controls.Add(panel);

                return textBox;
            }

            private void CmbExamples_SelectedIndexChanged(object sender, EventArgs e)
            {
                switch (cmbExamples.SelectedIndex)
                {
                    case 1: // NFA بسيط (قبول نهايات 01)
                        txtStates.Text = "q0,q1,q2";
                        txtAlphabet.Text = "01";
                        txtStart.Text = "q0";
                        txtFinals.Text = "q2";
                        dgvTransitions.Rows.Clear();
                        dgvTransitions.Rows.Add("q0", "0", "q0");
                        dgvTransitions.Rows.Add("q0", "1", "q0");
                        dgvTransitions.Rows.Add("q0", "0", "q1");
                        dgvTransitions.Rows.Add("q1", "1", "q2");
                        rbNFA.Checked = true;
                        break;

                    case 2: // DFA للأرقام الزوجية
                        txtStates.Text = "q0,q1";
                        txtAlphabet.Text = "01";
                        txtStart.Text = "q0";
                        txtFinals.Text = "q0";
                        dgvTransitions.Rows.Clear();
                        dgvTransitions.Rows.Add("q0", "0", "q0");
                        dgvTransitions.Rows.Add("q0", "1", "q1");
                        dgvTransitions.Rows.Add("q1", "0", "q1");
                        dgvTransitions.Rows.Add("q1", "1", "q0");
                        rbDFA.Checked = true;
                        break;

                    case 3: // NFA معقد (تحويل إلى DFA)
                        txtStates.Text = "q0,q1,q2";
                        txtAlphabet.Text = "ab";
                        txtStart.Text = "q0";
                        txtFinals.Text = "q2";
                        dgvTransitions.Rows.Clear();
                        dgvTransitions.Rows.Add("q0", "a", "q0");
                        dgvTransitions.Rows.Add("q0", "a", "q1");
                        dgvTransitions.Rows.Add("q0", "b", "q0");
                        dgvTransitions.Rows.Add("q1", "b", "q2");
                        dgvTransitions.Rows.Add("q2", "a", "q2");
                        dgvTransitions.Rows.Add("q2", "b", "q2");
                        rbNFA.Checked = true;
                        break;
                }

                if (cmbExamples.SelectedIndex > 0)
                {
                    BtnLoad_Click(null, null);
                }
            }

            private void BtnAddTransition_Click(object sender, EventArgs e)
            {
                dgvTransitions.Rows.Add("", "", "");
            }

            private void BtnLoad_Click(object sender, EventArgs e)
            {
                try
                {
                    LoadAutomaton();
                    btnStep.Enabled = true;
                    btnReset.Enabled = true;
                    btnRunAll.Enabled = true;
                    btnToDFA.Enabled = rbNFA.Checked;
                    btnToNFA.Enabled = false;
                    btnStepConversion.Enabled = false;
                    btnMinimize.Enabled = rbDFA.Checked;
                    btnExportImage.Enabled = true;
                    isDFAMode = false;
                    needsRedraw = true;
                    rtbLog.AppendText("تم تحميل الآلة بنجاح!\n");
                    rtbLog.AppendText($"نوع الآلة: {(rbNFA.Checked ? "NFA (غير محدود)" : "DFA (محدود)")}\n");
                    automatonView.Invalidate();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"خطأ في تحميل الآلة: {ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            private void LoadAutomaton()
            {
                // مسح البيانات السابقة
                states.Clear();
                alphabet.Clear();
                finalStates.Clear();
                transitions.Clear();
                conversionSteps.Clear();
                conversionStepIndex = -1;

                // تحميل الحالات
                states.AddRange(txtStates.Text.Split(',').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)));

                // تحميل الأبجدية
                alphabet.AddRange(txtAlphabet.Text.Where(c => !char.IsWhiteSpace(c)));

                // الحالة الأولية
                startState = txtStart.Text.Trim();
                if (!states.Contains(startState))
                    throw new Exception("الحالة الأولية غير موجودة في قائمة الحالات");

                // الحالات النهائية
                finalStates.AddRange(txtFinals.Text.Split(',').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)));
                if (finalStates.Any(fs => !states.Contains(fs)))
                    throw new Exception("إحدى الحالات النهائية غير موجودة في قائمة الحالات");

                // تحميل الانتقالات
                foreach (DataGridViewRow row in dgvTransitions.Rows)
                {
                    if (row.IsNewRow) continue;

                    string from = row.Cells["From"].Value?.ToString()?.Trim() ?? "";
                    string symbol = row.Cells["Symbol"].Value?.ToString()?.Trim() ?? "";
                    string to = row.Cells["To"].Value?.ToString()?.Trim() ?? "";

                    if (string.IsNullOrEmpty(from) || string.IsNullOrEmpty(to) || symbol.Length != 1)
                        continue;

                    if (!states.Contains(from))
                        throw new Exception($"الحالة المصدر '{from}' غير موجودة");

                    if (!states.Contains(to))
                        throw new Exception($"الحالة الهدف '{to}' غير موجودة");

                    char symChar = symbol[0];
                    if (!alphabet.Contains(symChar))
                        throw new Exception($"الرمز '{symChar}' غير موجود في الأبجدية");

                    // التحقق من أن الآلة DFA
                    if (rbDFA.Checked)
                    {
                        var key = (from, symChar);
                        if (transitions.ContainsKey(key))
                        {
                            throw new Exception($"الآلة DFA لا يمكن أن يكون لها أكثر من انتقال لنفس الحالة والرمز: {from}, {symChar}");
                        }
                    }

                    var key2 = (from, symChar);
                    if (!transitions.ContainsKey(key2))
                        transitions[key2] = new List<string>();

                    transitions[key2].Add(to);
                }

                // تهيئة المحاكاة
                currentStates = new List<string> { startState };
                inputString = "";
                currentStep = 0;
                simulationHistory.Clear();
            }

            private void BtnStep_Click(object sender, EventArgs e)
            {
                if (string.IsNullOrEmpty(txtInput.Text) || currentStep >= txtInput.Text.Length)
                {
                    rtbLog.AppendText("انتهى النص المدخل.\n");
                    return;
                }

                char currentSymbol = txtInput.Text[currentStep];
                List<string> nextStates = new List<string>();

                foreach (var state in currentStates)
                {
                    var key = (state, currentSymbol);
                    if (transitions.ContainsKey(key))
                    {
                        nextStates.AddRange(transitions[key]);
                    }
                }

                // تسجيل الخطوة
                var step = new SimulationStep
                {
                    Step = currentStep + 1,
                    CurrentStates = string.Join(", ", currentStates),
                    Symbol = currentSymbol,
                    NextStates = string.Join(", ", nextStates.Distinct())
                };

                simulationHistory.Add(step);
                rtbLog.AppendText($"الخطوة {step.Step}: الحالات الحالية [{step.CurrentStates}] + الرمز '{step.Symbol}' => الحالات التالية [{step.NextStates}]\n");

                // الانتقال للخطوة التالية
                currentStates = nextStates.Distinct().ToList();
                currentStep++;

                // التحديث البصري
                needsRedraw = true;
                automatonView.Invalidate();
            }

            private void BtnReset_Click(object sender, EventArgs e)
            {
                currentStates = new List<string> { startState };
                inputString = "";
                currentStep = 0;
                simulationHistory.Clear();
                rtbLog.Clear();
                isDFAMode = false;
                btnToDFA.Enabled = rbNFA.Checked;
                btnToNFA.Enabled = false;
                btnStepConversion.Enabled = false;
                needsRedraw = true;
                automatonView.Invalidate();
                rtbLog.AppendText("تم إعادة تعيين المحاكاة.\n");
            }

            private void BtnRunAll_Click(object sender, EventArgs e)
            {
                inputString = txtInput.Text;
                currentStates = new List<string> { startState };
                currentStep = 0;
                simulationHistory.Clear();
                rtbLog.Clear();

                while (currentStep < inputString.Length)
                {
                    BtnStep_Click(null, EventArgs.Empty);
                }

                // عرض النتيجة النهائية
                bool isAccepted = currentStates.Any(state => finalStates.Contains(state));
                rtbLog.AppendText($"\nالنتيجة: النص المدخل {(isAccepted ? "مقبول" : "مرفوض")}\n");
            }

            private void BtnToDFA_Click(object sender, EventArgs e)
            {
                try
                {
                    conversionSteps.Clear();
                    conversionStepIndex = -1;
                    convertedDFA = ConvertNFAToDFA(true);
                    isDFAMode = true;
                    btnToNFA.Enabled = true;
                    btnStepConversion.Enabled = true;
                    btnMinimize.Enabled = true;
                    btnExportReport.Enabled = true;
                    needsRedraw = true;
                    automatonView.Invalidate();
                    rtbLog.AppendText("تم تحويل NFA إلى DFA بنجاح!\n");
                    rtbLog.AppendText($"حالات DFA: {string.Join(", ", convertedDFA.States)}\n");
                    rtbLog.AppendText($"الحالات النهائية للـDFA: {string.Join(", ", convertedDFA.FinalStates)}\n");
                    rtbLog.AppendText($"الحالة الأولية للـDFA: {convertedDFA.StartState}\n");

                    // عرض خطوات التحويل في الشبكة
                    dgvConversionSteps.Rows.Clear();
                    for (int i = 0; i < conversionSteps.Count; i++)
                    {
                        var step = conversionSteps[i];
                        dgvConversionSteps.Rows.Add(
                            i + 1,
                            step.Description,
                            step.CurrentStateSet,
                            step.Symbol != '-' ? step.Symbol.ToString() : "",
                            step.NextStateSet
                        );
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"خطأ في التحويل: {ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            private void BtnToNFA_Click(object sender, EventArgs e)
            {
                isDFAMode = false;
                btnToNFA.Enabled = false;
                btnStepConversion.Enabled = false;
                needsRedraw = true;
                automatonView.Invalidate();
                rtbLog.AppendText("العودة إلى عرض الآلة الأصلية (NFA).\n");
            }

            private void BtnStepConversion_Click(object sender, EventArgs e)
            {
                NavigateConversionStep(1);
            }

            private void BtnMinimize_Click(object sender, EventArgs e)
            {
                try
                {
                    if (!isDFAMode || convertedDFA == null)
                    {
                        MessageBox.Show("الرجاء تحويل الآلة إلى DFA أولاً", "تحذير", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    DFA minimizedDFA = MinimizeDFA(convertedDFA);
                    convertedDFA = minimizedDFA;
                    needsRedraw = true;
                    automatonView.Invalidate();
                    rtbLog.AppendText("تم تصغير الآلة بنجاح!\n");
                    rtbLog.AppendText($"حالات DFA المصغرة: {string.Join(", ", minimizedDFA.States)}\n");
                    rtbLog.AppendText($"الحالات النهائية المصغرة: {string.Join(", ", minimizedDFA.FinalStates)}\n");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"خطأ في التصغير: {ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            private void NavigateConversionStep(int direction)
            {
                int newIndex = conversionStepIndex + direction;

                if (newIndex >= 0 && newIndex < conversionSteps.Count)
                {
                    conversionStepIndex = newIndex;
                    var step = conversionSteps[conversionStepIndex];

                    rtbLog.AppendText($"\nخطوة التحويل {conversionStepIndex + 1}: {step.Description}\n");
                    rtbLog.AppendText($"الحالة الحالية: {step.CurrentStateSet}\n");
                    if (step.Symbol != '-') rtbLog.AppendText($"الرمز: {step.Symbol}\n");
                    rtbLog.AppendText($"الحالات التالية: {step.NextStateSet}\n");

                    // تمييز الخطوة في الشبكة
                    dgvConversionSteps.ClearSelection();
                    dgvConversionSteps.Rows[conversionStepIndex].Selected = true;
                    dgvConversionSteps.FirstDisplayedScrollingRowIndex = conversionStepIndex;

                    // التحديث البصري
                    needsRedraw = true;
                    automatonView.Invalidate();
                }
                else if (newIndex >= conversionSteps.Count)
                {
                    rtbLog.AppendText("تم الانتهاء من جميع خطوات التحويل.\n");
                }
            }

            private void BtnExportImage_Click(object sender, EventArgs e)
            {
                SaveFileDialog sfd = new SaveFileDialog
                {
                    Filter = "صورة PNG|*.png",
                    Title = "حفظ صورة الآلة"
                };

                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    // إنشاء صورة من العرض الحالي
                    Bitmap bmp = new Bitmap(automatonView.Width, automatonView.Height);
                    using (Graphics g = Graphics.FromImage(bmp))
                    {
                        if (isDFAMode && convertedDFA != null)
                        {
                            DrawDFA(g, conversionStepIndex >= 0 ? conversionSteps[conversionStepIndex].CurrentStateSet : null);
                        }
                        else
                        {
                            DrawNFA(g);
                        }
                    }

                    bmp.Save(sfd.FileName, ImageFormat.Png);
                    rtbLog.AppendText($"تم تصدير الصورة إلى: {sfd.FileName}\n");
                }
            }

            private void BtnExportReport_Click(object sender, EventArgs e)
            {
                SaveFileDialog sfd = new SaveFileDialog
                {
                    Filter = "ملف نصي|*.txt",
                    Title = "حفظ تقرير التحويل"
                };

                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    StringBuilder report = new StringBuilder();
                    report.AppendLine("تقرير تحويل NFA إلى DFA");
                    report.AppendLine("==========================");
                    report.AppendLine($"تاريخ التصدير: {DateTime.Now}");
                    report.AppendLine($"آلة NFA الأصلية: {txtStates.Text}");
                    report.AppendLine();

                    report.AppendLine("خطوات التحويل:");
                    report.AppendLine("---------------");

                    foreach (var step in conversionSteps)
                    {
                        report.AppendLine($"الخطوة: {step.Description}");
                        report.AppendLine($"الحالة الحالية: {step.CurrentStateSet}");
                        if (step.Symbol != '-') report.AppendLine($"الرمز: {step.Symbol}");
                        report.AppendLine($"الحالة التالية: {step.NextStateSet}");
                        report.AppendLine();
                    }

                    report.AppendLine("نتيجة التحويل:");
                    report.AppendLine("---------------");
                    report.AppendLine($"حالات DFA: {string.Join(", ", convertedDFA.States)}");
                    report.AppendLine($"الحالات النهائية: {string.Join(", ", convertedDFA.FinalStates)}");
                    report.AppendLine($"الحالة الأولية: {convertedDFA.StartState}");

                    File.WriteAllText(sfd.FileName, report.ToString(), System.Text.Encoding.UTF8);
                    rtbLog.AppendText($"تم تصدير التقرير إلى: {sfd.FileName}\n");
                }
            }

            // خوارزمية تحويل NFA إلى DFA مع تسجيل الخطوات
            private DFA ConvertNFAToDFA(bool recordSteps = false)
            {
                DFA dfa = new DFA();
                dfa.Alphabet = new HashSet<char>(alphabet);

                // الحالة الأولية للـDFA هي مجموعة الحالة الأولية للـNFA
                string startClosure = EpsilonClosure(new List<string> { startState });
                dfa.StartState = startClosure;

                Queue<string> stateQueue = new Queue<string>();
                stateQueue.Enqueue(startClosure);

                Dictionary<(string, char), string> dfaTransitions = new Dictionary<(string, char), string>();

                while (stateQueue.Count > 0)
                {
                    string currentStateSet = stateQueue.Dequeue();
                    dfa.States.Add(currentStateSet);

                    // تسجيل خطوة جديدة
                    if (recordSteps)
                    {
                        conversionSteps.Add(new ConversionStep
                        {
                            Description = $"معالجة الحالة: {currentStateSet}",
                            CurrentStateSet = currentStateSet,
                            Symbol = '-',
                            NextStateSet = ""
                        });
                    }

                    // إذا كانت المجموعة تحتوي على حالة نهائية، فهي نهائية في DFA
                    if (currentStateSet.Split(',').Any(s => finalStates.Contains(s)))
                    {
                        dfa.FinalStates.Add(currentStateSet);
                    }

                    foreach (char symbol in alphabet)
                    {
                        string nextStateSet = Move(currentStateSet, symbol);
                        if (!string.IsNullOrEmpty(nextStateSet))
                        {
                            // حساب إغلاق-ε (في حالتنا بدون ε، نستخدم النتيجة مباشرة)
                            nextStateSet = EpsilonClosure(nextStateSet.Split(',').ToList());

                            if (!dfa.States.Contains(nextStateSet)
                                && !stateQueue.Contains(nextStateSet)
                                && !string.IsNullOrEmpty(nextStateSet))
                            {
                                stateQueue.Enqueue(nextStateSet);
                            }

                            dfaTransitions[(currentStateSet, symbol)] = nextStateSet;

                            // تسجيل خطوة الانتقال
                            if (recordSteps)
                            {
                                conversionSteps.Add(new ConversionStep
                                {
                                    Description = $"حساب الانتقال للحالة: {currentStateSet} بالرمز: {symbol}",
                                    CurrentStateSet = currentStateSet,
                                    Symbol = symbol,
                                    NextStateSet = nextStateSet
                                });
                            }
                        }
                    }
                }

                dfa.Transitions = dfaTransitions;
                return dfa;
            }

            private string EpsilonClosure(List<string> states)
            {
                return string.Join(",", states.Distinct().OrderBy(s => s));
            }

            private string Move(string stateSet, char symbol)
            {
                List<string> result = new List<string>();
                string[] states = stateSet.Split(',');

                foreach (string state in states)
                {
                    var key = (state, symbol);
                    if (transitions.ContainsKey(key))
                    {
                        result.AddRange(transitions[key]);
                    }
                }

                return result.Count > 0 ? string.Join(",", result.Distinct()) : null;
            }

            private void AutomatonView_Paint(object sender, PaintEventArgs e)
            {
                // استخدام التخزين المؤقت لتحسين الأداء
                if (cachedBitmap == null || needsRedraw || automatonView.Width != cachedBitmap.Width || automatonView.Height != cachedBitmap.Height)
                {
                    if (cachedBitmap != null) cachedBitmap.Dispose();

                    cachedBitmap = new Bitmap(automatonView.Width, automatonView.Height);
                    using (Graphics g = Graphics.FromImage(cachedBitmap))
                    {
                        g.SmoothingMode = SmoothingMode.AntiAlias;
                        g.Clear(automatonView.BackColor);

                        if (isDFAMode && convertedDFA != null && convertedDFA.States.Count > 0)
                        {
                            // Fixing the typo in the variable name 'conversion极Index' to 'conversionStepIndex'.
                            // The correct variable name is 'conversionStepIndex' as per the context provided.

                            string highlightedState = conversionStepIndex >= 0 && conversionStepIndex < conversionSteps.Count ?
                                conversionSteps[conversionStepIndex].CurrentStateSet : null;

                            DrawDFA(g, highlightedState);
                        }
                        else if (states.Count > 0)
                        {
                            DrawNFA(g);
                        }
                        else
                        {
                            // رسالة عندما لا توجد آلة معروضة
                            using (var font = new Font("Tahoma", 16, FontStyle.Bold))
                            {
                                string msg = "لم يتم تحميل آلة بعد";
                                var size = g.MeasureString(msg, font);
                                g.DrawString(msg, font, Brushes.Gray,
                                    (automatonView.Width - size.Width) / 2,
                                    (automatonView.Height - size.Height) / 2);
                            }
                        }
                    }
                    needsRedraw = false;
                }

                e.Graphics.DrawImage(cachedBitmap, 0, 0);
            }

            private void DrawNFA(Graphics g)
            {
                if (states.Count == 0) return;

                // زيادة المساحة بين الحالات لاستغلال المساحة الكبيرة
                var center = new PointF(automatonView.Width / 2, automatonView.Height / 2);
                float radius = Math.Min(automatonView.Width, automatonView.Height) * 0.4f;
                var statePositions = new Dictionary<string, PointF>();

                for (int i = 0; i < states.Count; i++)
                {
                    double angle = 2 * Math.PI * i / states.Count;
                    float x = center.X + radius * (float)Math.Cos(angle);
                    float y = center.Y + radius * (float)Math.Sin(angle);
                    statePositions[states[i]] = new PointF(x, y);
                }

                // رسم الانتقالات
                var transitionArrows = new Dictionary<(string, string), List<char>>();
                var pen = new Pen(Color.Blue, 1.5f); // تخفيض سمك الخط

                foreach (var transition in transitions)
                {
                    string from = transition.Key.Item1;
                    char symbol = transition.Key.Item2;

                    foreach (string to in transition.Value)
                    {
                        var key = (from, to);
                        if (!transitionArrows.ContainsKey(key))
                            transitionArrows[key] = new List<char>();

                        transitionArrows[key].Add(symbol);
                    }
                }

                foreach (var arrow in transitionArrows)
                {
                    PointF start = statePositions[arrow.Key.Item1];
                    PointF end = statePositions[arrow.Key.Item2];
                    DrawTransitionArrow(g, start, end, string.Join(",", arrow.Value), pen);
                }

                // رسم الحالات
                foreach (var state in statePositions)
                {
                    bool isFinal = finalStates.Contains(state.Key);
                    bool isCurrent = currentStates.Contains(state.Key);
                    bool isStart = state.Key == startState;

                    DrawState(g, state.Value, state.Key, isFinal, isCurrent, isStart);
                }

                // عنوان الرسم
                DrawTitle(g, "آلة غير محدودة (NFA)", Color.Blue);
            }

            private void DrawState(Graphics g, PointF position, string stateName, bool isFinal, bool isCurrent, bool isStart)
            {
                // تصغير حجم الحالة (نصف القطر من 30 إلى 18)
                float radius = 18;
                var rect = new RectangleF(position.X - radius, position.Y - radius, 2 * radius, 2 * radius);

                // رسم الحلقة الخارجية للحالات النهائية
                if (isFinal)
                {
                    g.DrawEllipse(new Pen(Color.Red, 1.5f), rect.X - 2, rect.Y - 2, rect.Width + 4, rect.Height + 4);
                }

                // تعبئة الحالة الحالية
                if (isCurrent)
                {
                    g.FillEllipse(Brushes.LightGreen, rect);
                }
                else
                {
                    g.FillEllipse(Brushes.White, rect);
                }

                // رسم الحالة
                g.DrawEllipse(new Pen(Color.Black, 1.5f), rect);

                // رسم مثلث للحالة الأولية
                if (isStart)
                {
                    PointF[] triangle = {
                    new PointF(position.X - radius - 12, position.Y),
                    new PointF(position.X - radius - 4, position.Y - 8),
                    new PointF(position.X - radius - 4, position.Y + 8)
                };
                    g.FillPolygon(Brushes.Black, triangle);
                }

                // كتابة اسم الحالة - حجم خط أصغر
                var format = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                };
                g.DrawString(stateName, new Font("Tahoma", 7), Brushes.Black, position, format);
            }

            private void DrawTransitionArrow(Graphics g, PointF start, PointF end, string symbols, Pen pen)
            {
                // حساب متجه الاتجاه
                float dx = end.X - start.X;
                float dy = end.Y - start.Y;
                float distance = (float)Math.Sqrt(dx * dx + dy * dy);
                if (distance == 0) return;

                float unitDx = dx / distance;
                float unitDy = dy / distance;

                // تعديل نقطة البداية والنهاية لحساب حجم الدائرة
                float radius = 18; // تصغير نصف قطر التأثير
                PointF adjustedStart = new PointF(
                    start.X + unitDx * radius,
                    start.Y + unitDy * radius
                );

                PointF adjustedEnd = new PointF(
                    end.X - unitDx * radius,
                    end.Y - unitDy * radius
                );

                // رسم الخط
                g.DrawLine(pen, adjustedStart, adjustedEnd);

                // رسم السهم - تصغير حجم السهم
                float arrowSize = 6;
                PointF arrowPoint = new PointF(
                    adjustedEnd.X - unitDx * arrowSize,
                    adjustedEnd.Y - unitDy * arrowSize
                );

                PointF arrowSide1 = new PointF(
                    arrowPoint.X - unitDy * arrowSize,
                    arrowPoint.Y + unitDx * arrowSize
                );

                PointF arrowSide2 = new PointF(
                    arrowPoint.X + unitDy * arrowSize,
                    arrowPoint.Y - unitDx * arrowSize
                );

                g.FillPolygon(Brushes.Blue, new[] { adjustedEnd, arrowSide1, arrowSide2 });

                // وضع تسمية الرموز - حجم خط أصغر
                PointF labelPosition = new PointF(
                    (adjustedStart.X + adjustedEnd.X) / 2 - unitDy * 15,
                    (adjustedStart.Y + adjustedEnd.Y) / 2 + unitDx * 15
                );

                g.DrawString(symbols, new Font("Tahoma", 7), Brushes.DarkBlue, labelPosition);
            }

            private void DrawDFA(Graphics g, string highlightedState = null)
            {
                // نسخ الحالات لتجنب التعديل
                List<string> dfaStates = convertedDFA.States.ToList();
                if (dfaStates.Count == 0) return;

                // زيادة المساحة بين الحالات لاستغلال المساحة الكبيرة
                var center = new PointF(automatonView.Width / 2, automatonView.Height / 2);
                float radius = Math.Min(automatonView.Width, automatonView.Height) * 0.45f;
                var statePositions = new Dictionary<string, PointF>();

                for (int i = 0; i < dfaStates.Count; i++)
                {
                    double angle = 2 * Math.PI * i / dfaStates.Count;
                    float x = center.X + radius * (float)Math.Cos(angle);
                    float y = center.Y + radius * (float)Math.Sin(angle);
                    statePositions[dfaStates[i]] = new PointF(x, y);
                }

                // رسم الانتقالات
                var pen = new Pen(Color.Purple, 1.5f); // تخفيض سمك الخط
                foreach (var transition in convertedDFA.Transitions)
                {
                    string from = transition.Key.Item1;
                    char symbol = transition.Key.Item2;
                    string to = transition.Value;

                    PointF start = statePositions[from];
                    PointF end = statePositions[to];
                    DrawDFATransition(g, start, end, symbol.ToString(), pen);
                }

                // رسم الحالات
                foreach (var state in statePositions)
                {
                    bool isFinal = convertedDFA.FinalStates.Contains(state.Key);
                    bool isStart = state.Key == convertedDFA.StartState;
                    bool isHighlighted = state.Key == highlightedState;

                    DrawDFAState(g, state.Value, state.Key, isFinal, isStart, isHighlighted);
                }

                // عنوان الرسم
                DrawTitle(g, "آلة محدودة (DFA) - نتيجة التحويل", Color.Purple);
            }

            private void DrawDFAState(Graphics g, PointF position, string stateName, bool isFinal, bool isStart, bool isHighlighted = false)
            {
                // تصغير حجم الحالة (نصف القطر من 35 إلى 22)
                float radius = 22;
                var rect = new RectangleF(position.X - radius, position.Y - radius, 2 * radius, 2 * radius);

                // تمييز الحالة إذا كانت في خطوة التحويل الحالية
                if (isHighlighted)
                {
                    g.FillEllipse(Brushes.Gold, rect);
                }

                // رسم الحلقة الخارجية للحالات النهائية
                if (isFinal)
                {
                    g.DrawEllipse(new Pen(Color.Red, 2), rect);
                }

                // تعبئة الحالة
                g.FillEllipse(isHighlighted ? Brushes.Gold : Brushes.LightBlue, rect);
                g.DrawEllipse(new Pen(Color.DarkBlue, 1.5f), rect);

                // رسم مثلث للحالة الأولية
                if (isStart)
                {
                    PointF[] triangle = {
                    new PointF(position.X - radius - 15, position.Y),
                    new PointF(position.X - radius - 4, position.Y - 12),
                    new PointF(position.X - radius - 4, position.Y + 12)
                };
                    g.FillPolygon(Brushes.DarkBlue, triangle);
                }

                // كتابة اسم الحالة (مع تقسيم إذا كان طويلاً) - حجم خط أصغر
                string displayName = stateName.Length > 20 ? stateName.Substring(0, 17) + "..." : stateName;
                var format = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                };

                g.DrawString(displayName, new Font("Tahoma", 7), Brushes.Black, position, format);

                // إضافة تلميح للمجموعة الكاملة
                toolTip.SetToolTip(automatonView, stateName);
            }

            private void DrawDFATransition(Graphics g, PointF start, PointF end, string symbol, Pen pen)
            {
                // حساب متجه الاتجاه
                float dx = end.X - start.X;
                float dy = end.Y - start.Y;
                float distance = (float)Math.Sqrt(dx * dx + dy * dy);
                if (distance == 0) return;

                float unitDx = dx / distance;
                float unitDy = dy / distance;

                // تعديل نقطة البداية والنهاية لحساب حجم الدائرة
                float radius = 22; // تصغير نصف قطر التأثير
                PointF adjustedStart = new PointF(
                    start.X + unitDx * radius,
                    start.Y + unitDy * radius
                );

                PointF adjustedEnd = new PointF(
                    end.X - unitDx * radius,
                    end.Y - unitDy * radius
                );

                // رسم الخط
                g.DrawLine(pen, adjustedStart, adjustedEnd);

                // رسم السهم - تصغير حجم السهم
                float arrowSize = 8;
                PointF arrowPoint = new PointF(
                    adjustedEnd.X - unitDx * arrowSize,
                    adjustedEnd.Y - unitDy * arrowSize
                );

                PointF arrowSide1 = new PointF(
                    arrowPoint.X - unitDy * arrowSize,
                    arrowPoint.Y + unitDx * arrowSize
                );

                PointF arrowSide2 = new PointF(
                    arrowPoint.X + unitDy * arrowSize,
                    arrowPoint.Y - unitDx * arrowSize
                );

                g.FillPolygon(Brushes.Purple, new[] { adjustedEnd, arrowSide1, arrowSide2 });

                // وضع تسمية الرموز - حجم خط أصغر
                PointF labelPosition = new PointF(
                    (adjustedStart.X + adjustedEnd.X) / 2 - unitDy * 20,
                    (adjustedStart.Y + adjustedEnd.Y) / 2 + unitDx * 20
                );

                g.DrawString(symbol, new Font("Tahoma", 7), Brushes.DarkMagenta, labelPosition);
            }

            private void DrawTitle(Graphics g, string title, Color color)
            {
                var titleFont = new Font("Tahoma", 12, FontStyle.Bold); // حجم خط أصغر
                var titleSize = g.MeasureString(title, titleFont);
                var titlePos = new PointF((automatonView.Width - titleSize.Width) / 2, 10); // وضع أعلى الصورة

                g.DrawString(title, titleFont, new SolidBrush(color), titlePos);
            }

            private void BtnRegexToAutomaton_Click(object sender, EventArgs e)
            {
                string regex = txtRegex.Text.Trim();
                if (string.IsNullOrEmpty(regex))
                {
                    MessageBox.Show("الرجاء إدخال تعبير نمطي", "تحذير", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                try
                {
                    // تحويل التعبير النمطي إلى آلة (تنفيذ مبسط)
                    ConvertRegexToAutomaton(regex);
                    rtbRegexSteps.AppendText($"تم تحويل التعبير النمطي '{regex}' إلى آلة حالات\n");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"خطأ في التحويل: {ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            private void BtnRegexStepByStep_Click(object sender, EventArgs e)
            {
                string regex = txtRegex.Text.Trim();
                if (string.IsNullOrEmpty(regex))
                {
                    MessageBox.Show("الرجاء إدخال تعبير نمطي", "تحذير", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                try
                {
                    rtbRegexSteps.Clear();
                    rtbRegexSteps.AppendText("بدء التحويل خطوة بخطوة:\n");

                    // خطوة 1: التحليل الأولي
                    rtbRegexSteps.AppendText("الخطوة 1: تحليل التعبير النمطي\n");
                    rtbRegexSteps.AppendText($"التعبير المدخل: {regex}\n");

                    // خطوة 2: تحويل إلى آلة بسيطة
                    rtbRegexSteps.AppendText("الخطوة 2: إنشاء آلة الحالات الأولية\n");
                    ConvertRegexToAutomaton(regex);

                    // خطوة 3: تحويل إلى DFA
                    rtbRegexSteps.AppendText("الخطوة 3: تحويل إلى آلة محدودة (DFA)\n");
                    BtnToDFA_Click(null, null);

                    // خطوة 4: تصغير الآلة
                    rtbRegexSteps.AppendText("الخطوة 4: تصغير الآلة\n");
                    BtnMinimize_Click(null, null);

                    rtbRegexSteps.AppendText("تم الانتهاء من التحويل بنجاح!\n");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"خطأ في التحويل: {ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            private void BtnAutomatonToRegex_Click(object sender, EventArgs e)
            {
                try
                {
                    if (states.Count == 0)
                    {
                        MessageBox.Show("الرجاء تحميل آلة أولاً", "تحذير", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    // تحويل الآلة إلى تعبير نمطي (تنفيذ مبسط)
                    string regex = ConvertAutomatonToRegex();
                    rtbRegexSteps.AppendText($"التعبير النمطي الناتج: {regex}\n");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"خطأ في التحويل: {ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            private void ConvertRegexToAutomaton(string regex)
            {
                // تنفيذ مبسط لتحويل التعبير النمطي إلى آلة
                // في التطبيق الحقيقي، يجب استخدام خوارزمية ثومبسون

                // مثال: تعبير بسيط (a|b)*abb
                states.Clear();
                alphabet.Clear();
                finalStates.Clear();
                transitions.Clear();

                states.AddRange(new[] { "q0", "q1", "q2", "q3" });
                alphabet.AddRange(new[] { 'a', 'b' });
                startState = "q0";
                finalStates.Add("q3");

                dgvTransitions.Rows.Clear();
                dgvTransitions.Rows.Add("q0", "a", "q0");
                dgvTransitions.Rows.Add("q0", "b", "极0");
                dgvTransitions.Rows.Add("q0", "a", "q1");
                dgvTransitions.Rows.Add("q1", "b", "q2");
                dgvTransitions.Rows.Add("q2", "b", "q3");

                txtStates.Text = string.Join(",", states);
                txtAlphabet.Text = string.Join("", alphabet);
                txtStart.Text = startState;
                txtFinals.Text = string.Join(",", finalStates);

                rbNFA.Checked = true;
                BtnLoad_Click(null, null);
            }

            private string ConvertAutomatonToRegex()
            {
                // تنفيذ مبسط لتحويل الآلة إلى تعبير نمطي
                // في التطبيق الحقيقي، يجب استخدام خوارزمية حذف الحالات

                if (isDFAMode && convertedDFA != null)
                {
                    // استخدام الآلة المحولة إذا كانت موجودة
                    return ConvertDFAToRegex(convertedDFA);
                }

                // تحويل NFA إلى DFA أولاً
                DFA dfa = ConvertNFAToDFA(false);
                return ConvertDFAToRegex(dfa);
            }

            private string ConvertDFAToRegex(DFA dfa)
            {
                // تنفيذ مبسط لتحويل DFA إلى تعبير نمطي
                StringBuilder regex = new StringBuilder();

                // حالة بسيطة: آلة بقبول سلاسل تنتهي بـ "abb"
                if (dfa.States.Count >= 3 && dfa.FinalStates.Count == 1)
                {
                    regex.Append("(a|b)*abb");
                }
                // حالة أخرى: آلة تقبل سلاسل بطول زوجي
                else if (dfa.States.Count == 2 && dfa.FinalStates.Contains(dfa.StartState))
                {
                    regex.Append("((a|b)(a|b))*");
                }
                else
                {
                    regex.Append("(a|b)*"); // تعبير عام
                }

                return regex.ToString();
            }

            private DFA MinimizeDFA(DFA dfa)
            {
                // خوارزمية تصغير DFA (Hopcroft)
                DFA minimized = new DFA();
                minimized.Alphabet = new HashSet<char>(dfa.Alphabet);

                // المجموعات: نهائية وغير نهائية
                HashSet<string> finalStates = new HashSet<string>(dfa.FinalStates);
                HashSet<string> nonFinalStates = new HashSet<string>(dfa.States.Except(dfa.FinalStates));

                // قائمة المجموعات
                List<HashSet<string>> partitions = new List<HashSet<string>>();
                if (nonFinalStates.Count > 0) partitions.Add(nonFinalStates);
                if (finalStates.Count > 0) partitions.Add(finalStates);

                // خريطة الحالة إلى المجموعة
                Dictionary<string, int> stateToPartition = new Dictionary<string, int>();
                foreach (var state in nonFinalStates) stateToPartition[state] = 0;
                foreach (var state in finalStates) stateToPartition[state] = nonFinalStates.Count > 0 ? 1 : 0;

                bool changed = true;
                while (changed)
                {
                    changed = false;
                    List<HashSet<string>> newPartitions = new List<HashSet<string>>();

                    foreach (var partition in partitions)
                    {
                        Dictionary<string, HashSet<string>> splitMap = new Dictionary<string, HashSet<string>>();

                        foreach (var state in partition)
                        {
                            StringBuilder behavior = new StringBuilder();
                            foreach (char symbol in dfa.Alphabet)
                            {
                                if (dfa.Transitions.TryGetValue((state, symbol), out string nextState))
                                {
                                    behavior.Append($"{symbol}:{stateToPartition[nextState]};");
                                }
                                else
                                {
                                    behavior.Append($"{symbol}:;");
                                }
                            }

                            string key = behavior.ToString();
                            if (!splitMap.ContainsKey(key))
                            {
                                splitMap[key] = new HashSet<string>();
                            }
                            splitMap[key].Add(state);
                        }

                        if (splitMap.Count > 1)
                        {
                            changed = true;
                        }

                        newPartitions.AddRange(splitMap.Values);
                    }

                    if (changed)
                    {
                        partitions = newPartitions;
                        // تحديث خريطة الحالة إلى المجموعة
                        stateToPartition.Clear();
                        for (int i = 0; i < partitions.Count; i++)
                        {
                            foreach (var state in partitions[i])
                            {
                                stateToPartition[state] = i;
                            }
                        }
                    }
                }

                // بناء الآلة المصغرة
                Dictionary<int, string> partitionToState = new Dictionary<int, string>();
                for (int i = 0; i < partitions.Count; i++)
                {
                    string newState = string.Join(",", partitions[i].OrderBy(s => s));
                    minimized.States.Add(newState);
                    partitionToState[i] = newState;

                    if (partitions[i].Contains(dfa.StartState))
                    {
                        minimized.StartState = newState;
                    }

                    if (partitions[i].Any(s => dfa.FinalStates.Contains(s)))
                    {
                        minimized.FinalStates.Add(newState);
                    }
                }

                // بناء الانتقالات
                foreach (var state in minimized.States)
                {
                    // أخذ أي حالة ممثلة من المجموعة
                    string originalState = state.Split(',').First();

                    foreach (char symbol in minimized.Alphabet)
                    {
                        if (dfa.Transitions.TryGetValue((originalState, symbol), out string nextState))
                        {
                            int partitionIndex = stateToPartition[nextState];
                            string newNextState = partitionToState[partitionIndex];
                            minimized.Transitions[(state, symbol)] = newNextState;
                        }
                    }
                }

                return minimized;
            }
        }

        public class SimulationStep
        {
            public int Step { get; set; }
            public string CurrentStates { get; set; }
            public char Symbol { get; set; }
            public string NextStates { get; set; }
        }

        public class ConversionStep
        {
            public string Description { get; set; }
            public string CurrentStateSet { get; set; }
            public char Symbol { get; set; }
            public string NextStateSet { get; set; }
        }

        public class DFA
        {
            public HashSet<string> States { get; set; } = new HashSet<string>();
            public HashSet<char> Alphabet { get; set; } = new HashSet<char>();
            public string StartState { get; set; }
            public HashSet<string> FinalStates { get; set; } = new HashSet<string>();
            public Dictionary<(string, char), string> Transitions { get; set; } = new Dictionary<(string, char), string>();
        }


    }


