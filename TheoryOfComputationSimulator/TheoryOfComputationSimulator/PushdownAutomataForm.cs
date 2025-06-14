
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

using Formatting = Newtonsoft.Json.Formatting;

namespace TheoryOfComputationSimulator
{
    

        public partial class PushdownAutomataForm : Form
        {
            // عناصر التحكم
            private TabControl mainTabs;
            private Panel inputPanel;
            private Panel stackPanel;
            private Label lblCurrentState;
            private Button btnStep;
            private Button btnRun;
            private Button btnReset;
            private Button btnConfigure;
            private Button btnStop;
            private TextBox txtInput;
            private ListBox lstTransitions;
            private PictureBox stateDiagramBox;
            private TrackBar speedTrackBar;
            private ToolStrip toolStrip;
            private ComboBox cmbExamples;
            private Label lblStatus;
            private ToolStripButton btnSaveConfig;
            private ToolStripButton btnLoadConfig;

            // نموذج الآلة
            private PushdownAutomata pda = new PushdownAutomata();

            // متغيرات الرسم البياني
            private Dictionary<string, Point> statePositions = new Dictionary<string, Point>();
            private int stateRadius = 22;
            private int stateSpacing = 150;
            private float diagramScale = 1.0f;
            private Point diagramOffset = Point.Empty;
            private Point dragStart;
            private bool isDragging = false;

            // محاكاة
            private CancellationTokenSource cancellationTokenSource;
            private int simulationSpeed = 500;
            private bool isRunning = false;

            public PushdownAutomataForm()
            {
                InitializeComponent();
                InitializeTabs();
                InitializeToolStrip();
                LoadExamples();
                EnableAdvancedRendering();
            }

            private void InitializeComponent()
            {
                this.Text = "محاكي آلة الدفع الذكية - للدراسات العليا";
                this.Size = new Size(1400, 900);
                this.StartPosition = FormStartPosition.CenterScreen;
                this.BackColor = Color.FromArgb(245, 245, 250);
                this.Font = new Font("Segoe UI", 9);
                this.DoubleBuffered = true;
                this.FormClosing += MainForm_FormClosing;
            }

            private void InitializeTabs()
            {
                mainTabs = new TabControl
                {
                    Dock = DockStyle.Fill,
                    ItemSize = new Size(120, 30),
                    SizeMode = TabSizeMode.Fixed,
                    Appearance = TabAppearance.FlatButtons
                };

                TabPage simTab = new TabPage("المحاكاة") { BackColor = Color.White };
                simTab.Controls.Add(CreateSimulationPanel());

                TabPage configTab = new TabPage("التهيئة") { BackColor = Color.White };
                configTab.Controls.Add(CreateConfigurationPanel());

                TabPage graphTab = new TabPage("الرسم البياني") { BackColor = Color.White };
                graphTab.Controls.Add(CreateGraphPanel());

                mainTabs.TabPages.Add(simTab);
                mainTabs.TabPages.Add(configTab);
                mainTabs.TabPages.Add(graphTab);

                this.Controls.Add(mainTabs);
            }

            private void InitializeToolStrip()
            {
                toolStrip = new ToolStrip
                {
                    Dock = DockStyle.Top,
                    GripStyle = ToolStripGripStyle.Hidden,
                    BackColor = Color.FromArgb(240, 240, 245),
                    Padding = new Padding(5),
                    Renderer = new CustomToolStripRenderer()
                };

                // أمثلة
                toolStrip.Items.Add(new ToolStripLabel("أمثلة:") { Font = new Font("Segoe UI", 9, FontStyle.Bold) });
                cmbExamples = new ComboBox
                {
                    DropDownStyle = ComboBoxStyle.DropDownList,
                    Width = 250,
                    Font = new Font("Segoe UI", 9)
                };
                cmbExamples.SelectedIndexChanged += CmbExamples_SelectedIndexChanged;
                toolStrip.Items.Add(new ToolStripControlHost(cmbExamples));

                toolStrip.Items.Add(new ToolStripSeparator());

                // السرعة
                toolStrip.Items.Add(new ToolStripLabel("السرعة:") { Font = new Font("Segoe UI", 9, FontStyle.Bold) });
                speedTrackBar = new TrackBar
                {
                    Minimum = 100,
                    Maximum = 2000,
                    Value = simulationSpeed,
                    Width = 150,
                    TickStyle = TickStyle.None
                };
                speedTrackBar.ValueChanged += (s, e) => simulationSpeed = speedTrackBar.Value;
                toolStrip.Items.Add(new ToolStripControlHost(speedTrackBar));

                toolStrip.Items.Add(new ToolStripSeparator());

                // حفظ/تحميل التكوين
                btnSaveConfig = new ToolStripButton("حفظ التكوين", null, (s, e) => SaveConfiguration())
                {
                    DisplayStyle = ToolStripItemDisplayStyle.Text,
                    Font = new Font("Segoe UI", 9, FontStyle.Bold),
                    BackColor = Color.FromArgb(220, 237, 200)
                };
                btnLoadConfig = new ToolStripButton("تحميل التكوين", null, (s, e) => LoadConfiguration())
                {
                    DisplayStyle = ToolStripItemDisplayStyle.Text,
                    Font = new Font("Segoe UI", 9, FontStyle.Bold),
                    BackColor = Color.FromArgb(200, 230, 250)
                };
                toolStrip.Items.Add(btnSaveConfig);
                toolStrip.Items.Add(btnLoadConfig);

                toolStrip.Items.Add(new ToolStripSeparator());

                // حالة النظام
                lblStatus = new Label
                {
                    Text = "جاهز",
                    Font = new Font("Segoe UI", 9, FontStyle.Bold),
                    ForeColor = Color.DarkSlateBlue,
                    AutoSize = false,
                    Width = 300,
                    TextAlign = ContentAlignment.MiddleRight
                };
                toolStrip.Items.Add(new ToolStripControlHost(lblStatus));

                this.Controls.Add(toolStrip);
            }

            private Panel CreateSimulationPanel()
            {
                Panel panel = new Panel
                {
                    Dock = DockStyle.Fill,
                    Padding = new Padding(15),
                    BackColor = Color.White
                };

                // مجموعة شريط الإدخال
                GroupBox inputGroup = new GroupBox
                {
                    Text = "شريط الإدخال",
                    Font = new Font("Segoe UI", 10, FontStyle.Bold),
                    ForeColor = Color.DarkSlateBlue,
                    Dock = DockStyle.Top,
                    Height = 120
                };

                txtInput = new TextBox
                {
                    Dock = DockStyle.Top,
                    Height = 40,
                    Font = new Font("Segoe UI", 12),
                    TextAlign = HorizontalAlignment.Center,
                    Text = "aabb"
                };

                inputPanel = new FlowLayoutPanel
                {
                    Dock = DockStyle.Top,
                    Height = 60,
                    BackColor = Color.WhiteSmoke,
                    BorderStyle = BorderStyle.FixedSingle,
                    AutoScroll = true,
                    WrapContents = false
                };

                inputGroup.Controls.Add(inputPanel);
                inputGroup.Controls.Add(txtInput);

                // مجموعة المكدس
                GroupBox stackGroup = new GroupBox
                {
                    Text = "المكدس",
                    Font = new Font("Segoe UI", 10, FontStyle.Bold),
                    ForeColor = Color.DarkSlateBlue,
                    Dock = DockStyle.Top,
                    Height = 150
                };

                stackPanel = new Panel
                {
                    Dock = DockStyle.Fill,
                    BackColor = Color.WhiteSmoke,
                    BorderStyle = BorderStyle.FixedSingle,
                    AutoScroll = true
                };

                stackGroup.Controls.Add(stackPanel);

                // عرض الحالة الحالية
                lblCurrentState = new Label
                {
                    Dock = DockStyle.Top,
                    Height = 40,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Font = new Font("Segoe UI", 12, FontStyle.Bold),
                    ForeColor = Color.DarkGreen,
                    BackColor = Color.FromArgb(230, 255, 230),
                    Text = "الحالة الحالية: "
                };

                // لوحة أزرار التحكم
                Panel controlPanel = new Panel
                {
                    Dock = DockStyle.Top,
                    Height = 60,
                    Padding = new Padding(10, 5, 10, 5)
                };

                btnStep = new Button
                {
                    Text = "خطوة",
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.FromArgb(220, 237, 200),
                    Size = new Size(100, 40),
                    Anchor = AnchorStyles.Top | AnchorStyles.Left,
                    Font = new Font("Segoe UI", 10)
                };
                btnStep.Click += BtnStep_Click;

                btnRun = new Button
                {
                    Text = "تشغيل",
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.FromArgb(200, 230, 250),
                    Size = new Size(100, 40),
                    Anchor = AnchorStyles.Top | AnchorStyles.Left,
                    Left = btnStep.Right + 10,
                    Font = new Font("Segoe UI", 10)
                };
                btnRun.Click += BtnRun_Click;

                btnStop = new Button
                {
                    Text = "إيقاف",
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.FromArgb(250, 200, 200),
                    Size = new Size(100, 40),
                    Anchor = AnchorStyles.Top | AnchorStyles.Left,
                    Left = btnRun.Right + 10,
                    Enabled = false,
                    Font = new Font("Segoe UI", 10)
                };
                btnStop.Click += BtnStop_Click;

                btnReset = new Button
                {
                    Text = "إعادة تعيين",
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.FromArgb(255, 235, 156),
                    Size = new Size(120, 40),
                    Anchor = AnchorStyles.Top | AnchorStyles.Left,
                    Left = btnStop.Right + 10,
                    Font = new Font("Segoe UI", 10)
                };
                btnReset.Click += BtnReset_Click;

                btnConfigure = new Button
                {
                    Text = "تهيئة الآلة",
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.FromArgb(200, 200, 250),
                    Size = new Size(120, 40),
                    Anchor = AnchorStyles.Top | AnchorStyles.Left,
                    Left = btnReset.Right + 10,
                    Font = new Font("Segoe UI", 10)
                };
                btnConfigure.Click += (s, e) => ShowConfigurationForm();

                controlPanel.Controls.AddRange(new Control[] { btnStep, btnRun, btnStop, btnReset, btnConfigure });

                // قائمة الانتقالات
                lstTransitions = new ListBox
                {
                    Dock = DockStyle.Fill,
                    Font = new Font("Consolas", 10),
                    BorderStyle = BorderStyle.FixedSingle,
                    BackColor = Color.WhiteSmoke,
                    DrawMode = DrawMode.OwnerDrawVariable
                };

                lstTransitions.DrawItem += (s, e) =>
                {
                    e.DrawBackground();
                    bool isHeader = e.Index < 2 || lstTransitions.Items[e.Index].ToString().StartsWith("=====");

                    using (var brush = new SolidBrush(isHeader ? Color.DarkBlue : e.ForeColor))
                    {
                        e.Graphics.DrawString(lstTransitions.Items[e.Index].ToString(),
                            new Font(e.Font, isHeader ? FontStyle.Bold : FontStyle.Regular),
                            brush, e.Bounds);
                    }
                };

                panel.Controls.AddRange(new Control[] {
                lstTransitions,
                controlPanel,
                lblCurrentState,
                stackGroup,
                inputGroup
            });

                return panel;
            }

            private Panel CreateConfigurationPanel()
            {
                Panel panel = new Panel
                {
                    Dock = DockStyle.Fill,
                    Padding = new Padding(15),
                    BackColor = Color.White
                };

                Label lblInfo = new Label
                {
                    Text = "استخدم زر 'تهيئة الآلة' لتعريف آلة الدفع الذكية",
                    Location = new Point(20, 20),
                    AutoSize = true,
                    Font = new Font("Segoe UI", 12),
                    ForeColor = Color.DarkSlateBlue
                };

                Button btnOpenConfig = new Button
                {
                    Text = "فتح نافذة التهيئة",
                    Location = new Point(20, 60),
                    Size = new Size(200, 40),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.FromArgb(200, 200, 250),
                    Font = new Font("Segoe UI", 10)
                };
                btnOpenConfig.Click += (s, e) => ShowConfigurationForm();

                panel.Controls.Add(lblInfo);
                panel.Controls.Add(btnOpenConfig);

                return panel;
            }

            private Panel CreateGraphPanel()
            {
                Panel panel = new Panel
                {
                    Dock = DockStyle.Fill,
                    Padding = new Padding(15),
                    BackColor = Color.White
                };

                stateDiagramBox = new PictureBox
                {
                    Dock = DockStyle.Fill,
                    BackColor = Color.White,
                    BorderStyle = BorderStyle.FixedSingle,
                    Cursor = Cursors.Hand
                };
                stateDiagramBox.Paint += StateDiagramBox_Paint;

                // تفاعل الماوس
                stateDiagramBox.MouseDown += (s, e) =>
                {
                    if (e.Button == MouseButtons.Left)
                    {
                        dragStart = e.Location;
                        isDragging = true;
                        stateDiagramBox.Cursor = Cursors.SizeAll;
                    }
                };

                stateDiagramBox.MouseMove += (s, e) =>
                {
                    if (isDragging)
                    {
                        diagramOffset.X += e.X - dragStart.X;
                        diagramOffset.Y += e.Y - dragStart.Y;
                        dragStart = e.Location;
                        stateDiagramBox.Invalidate();
                    }
                };

                stateDiagramBox.MouseUp += (s, e) =>
                {
                    isDragging = false;
                    stateDiagramBox.Cursor = Cursors.Hand;
                };

                stateDiagramBox.MouseWheel += (s, e) =>
                {
                    float oldScale = diagramScale;
                    diagramScale = Math.Max(0.5f, Math.Min(2.0f, diagramScale + e.Delta * 0.001f));

                    float scaleChange = diagramScale / oldScale;
                    diagramOffset.X = (int)((diagramOffset.X - e.X) * scaleChange + e.X);
                    diagramOffset.Y = (int)((diagramOffset.Y - e.Y) * scaleChange + e.Y);

                    stateDiagramBox.Invalidate();
                };

                Button btnResetView = new Button
                {
                    Text = "إعادة ضبط العرض",
                    Dock = DockStyle.Bottom,
                    Height = 40,
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.FromArgb(220, 220, 230),
                    Font = new Font("Segoe UI", 10)
                };
                btnResetView.Click += (s, e) =>
                {
                    diagramScale = 1.0f;
                    diagramOffset = Point.Empty;
                    stateDiagramBox.Invalidate();
                };

                panel.Controls.Add(stateDiagramBox);
                panel.Controls.Add(btnResetView);

                return panel;
            }

            private void LoadExamples()
            {
                cmbExamples.Items.Add("-- اختر مثالًا --");
                cmbExamples.Items.Add("تعريف a^n b^n");
                cmbExamples.Items.Add("تعريف a^n b^m c^n");
                cmbExamples.Items.Add("تعريف الأقواس المتوازنة");
                cmbExamples.SelectedIndex = 0;
            }

            private void CmbExamples_SelectedIndexChanged(object sender, EventArgs e)
            {
                if (cmbExamples.SelectedIndex <= 0) return;

                string selectedExample = cmbExamples.SelectedItem.ToString();
                LoadExamplePDA(selectedExample);
            }

            private void LoadExamplePDA(string exampleName)
            {
                try
                {
                    lblStatus.Text = "جاري تحميل المثال...";
                    Application.DoEvents();

                    switch (exampleName)
                    {
                        case "تعريف a^n b^n":
                            LoadAnBnMachine();
                            break;
                        case "تعريف a^n b^m c^n":
                            LoadAnBmCnMachine();
                            break;
                        case "تعريف الأقواس المتوازنة":
                            LoadBalancedParenthesesMachine();
                            break;
                    }

                    CalculateStatePositions();
                    UpdateUI();
                    lblStatus.Text = "مثال محمل بنجاح";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"خطأ في تحميل المثال: {ex.Message}", "خطأ",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    lblStatus.Text = "خطأ في تحميل المثال";
                }
            }

            private void LoadAnBnMachine()
            {
                pda = new PushdownAutomata();
                pda.States = new List<string> { "q0", "q1", "qf" };
                pda.FinalStates = new List<string> { "qf" };
                pda.InputAlphabet = new List<char> { 'a', 'b' };
                pda.StackAlphabet = new List<char> { 'Z', 'A' };
                pda.StackStartSymbol = 'Z';
                pda.CurrentState = "q0";

                pda.AddTransition("q0", 'a', 'Z', "q0", "AZ");
                pda.AddTransition("q0", 'a', 'A', "q0", "AA");
                pda.AddTransition("q0", 'b', 'A', "q1", "ε");
                pda.AddTransition("q1", 'b', 'A', "q1", "ε");
                pda.AddTransition("q1", null, 'Z', "qf", "Z");

                txtInput.Text = "aabb";
                pda.Initialize(txtInput.Text);
            }

            private void LoadAnBmCnMachine()
            {
                pda = new PushdownAutomata();
                pda.States = new List<string> { "q0", "q1", "q2", "qf" };
                pda.FinalStates = new List<string> { "qf" };
                pda.InputAlphabet = new List<char> { 'a', 'b', 'c' };
                pda.StackAlphabet = new List<char> { 'Z', 'A', 'B' };
                pda.StackStartSymbol = 'Z';
                pda.CurrentState = "q0";

                pda.AddTransition("q0", 'a', 'Z', "q0", "AZ");
                pda.AddTransition("q0", 'a', 'A', "q0", "AA");
                pda.AddTransition("q0", 'b', 'A', "q1", "BA");
                pda.AddTransition("q1", 'b', 'B', "q1", "BB");
                pda.AddTransition("q1", 'c', 'B', "q2", "ε");
                pda.AddTransition("q2", 'c', 'B', "q2", "ε");
                pda.AddTransition("q2", null, 'A', "q2", "A");
                pda.AddTransition("q2", null, 'Z', "qf", "Z");

                txtInput.Text = "aabbbccc";
                pda.Initialize(txtInput.Text);
            }

            private void LoadBalancedParenthesesMachine()
            {
                pda = new PushdownAutomata();
                pda.States = new List<string> { "q0", "qf" };
                pda.FinalStates = new List<string> { "qf" };
                pda.InputAlphabet = new List<char> { '(', ')', '[', ']' };
                pda.StackAlphabet = new List<char> { 'Z', 'P', 'B' };
                pda.StackStartSymbol = 'Z';
                pda.CurrentState = "q0";

                pda.AddTransition("q0", '(', 'Z', "q0", "PZ");
                pda.AddTransition("q0", '(', 'P', "q0", "PP");
                pda.AddTransition("q0", '(', 'B', "q0", "PB");
                pda.AddTransition("q0", '[', 'Z', "q0", "BZ");
                pda.AddTransition("q0", '[', 'P', "q0", "BP");
                pda.AddTransition("q0", '[', 'B', "q0", "BB");
                pda.AddTransition("q0", ')', 'P', "q0", "ε");
                pda.AddTransition("q0", ']', 'B', "q0", "ε");
                pda.AddTransition("q0", null, 'Z', "qf", "Z");

                txtInput.Text = "([()[]])";
                pda.Initialize(txtInput.Text);
            }

            private void ShowConfigurationForm()
            {
                using (var configForm = new ConfigurationForm1())
                {
                    configForm.PDAConfig = pda;
                    if (configForm.ShowDialog() == DialogResult.OK)
                    {
                        pda = configForm.PDAConfig;
                        pda.Initialize(txtInput.Text);
                        CalculateStatePositions();
                        UpdateUI();
                        lblStatus.Text = "تم تهيئة الآلة بنجاح";
                    }
                }
            }

            private void CalculateStatePositions()
            {
                statePositions.Clear();
                int cols = (int)Math.Ceiling(Math.Sqrt(pda.States.Count));
                int rows = (int)Math.Ceiling((double)pda.States.Count / cols);

                int centerX = stateDiagramBox.Width / 2;
                int centerY = stateDiagramBox.Height / 2;

                int startX = centerX - (cols * stateSpacing) / 2;
                int startY = centerY - (rows * stateSpacing) / 2;

                for (int i = 0; i < pda.States.Count; i++)
                {
                    int row = i / cols;
                    int col = i % cols;

                    int x = startX + col * stateSpacing;
                    int y = startY + row * stateSpacing;

                    statePositions[pda.States[i]] = new Point(x, y);
                }

                // تطبيق خوارزمية تخطيط محسنة
                Task.Run(() => ApplyOptimizedForceDirectedLayout());
            }

            private async Task ApplyOptimizedForceDirectedLayout()
            {
                const int ITERATIONS = 500;
                const double K = 0.8;
                double t = stateDiagramBox.Width / 8.0;
                double coolingFactor = t / ITERATIONS;

                var positions = statePositions.ToDictionary(
                    k => k.Key,
                    v => new PointF(v.Value.X, v.Value.Y));

                for (int iter = 0; iter < ITERATIONS; iter++)
                {
                    var forces = new Dictionary<string, PointF>();
                    foreach (var state in pda.States)
                        forces[state] = PointF.Empty;

                    // تحسين حساب قوى التنافر
                    Parallel.For(0, pda.States.Count, i =>
                    {
                        for (int j = i + 1; j < pda.States.Count; j++)
                        {
                            string state1 = pda.States[i];
                            string state2 = pda.States[j];

                            PointF delta = new PointF(
                                positions[state2].X - positions[state1].X,
                                positions[state2].Y - positions[state1].Y
                            );

                            float distance = Math.Max((float)Math.Sqrt(delta.X * delta.X + delta.Y * delta.Y), 0.1f);
                            float repulse = (float)(K * K / distance);

                            lock (forces)
                            {
                                forces[state1] = new PointF(
                                    forces[state1].X - repulse * delta.X / distance,
                                    forces[state1].Y - repulse * delta.Y / distance
                                );

                                forces[state2] = new PointF(
                                    forces[state2].X + repulse * delta.X / distance,
                                    forces[state2].Y + repulse * delta.Y / distance
                                );
                            }
                        }
                    });

                    // تحديث المواقع مع التبريد التدريجي
                    foreach (var state in pda.States)
                    {
                        PointF force = forces[state];
                        float magnitude = Math.Max((float)Math.Sqrt(force.X * force.X + force.Y * force.Y), 0.1f);

                        PointF displacement = new PointF(
                            force.X / magnitude * (float)Math.Min(magnitude, t),
                            force.Y / magnitude * (float)Math.Min(magnitude, t)
                        );

                        positions[state] = new PointF(
                            Math.Max(50, Math.Min(stateDiagramBox.Width - 50, positions[state].X + displacement.X)),
                            Math.Max(50, Math.Min(stateDiagramBox.Height - 50, positions[state].Y + displacement.Y))
                        );
                    }

                    t -= coolingFactor;

                    // تحديث الرسم كل 50 تكرار
                    if (iter % 50 == 0)
                    {
                        statePositions = positions.ToDictionary(k => k.Key, v => Point.Round(v.Value));
                        stateDiagramBox.Invalidate();
                        await Task.Delay(1);
                    }
                }

                statePositions = positions.ToDictionary(k => k.Key, v => Point.Round(v.Value));
                stateDiagramBox.Invalidate();
            }

            private void UpdateUI()
            {
                RenderInputTape();
                RenderStack();
                lblCurrentState.Text = $"الحالة الحالية: {pda.CurrentState}";
                RenderTransitions();
                stateDiagramBox.Invalidate();
            }

            private void RenderInputTape()
            {
                inputPanel.Controls.Clear();
                int cellSize = 35;
                int cellSpacing = 2;
                int startX = 10;

                for (int i = 0; i < pda.InputTape.Count; i++)
                {
                    var cell = new Panel
                    {
                        Size = new Size(cellSize, cellSize),
                        Location = new Point(startX + i * (cellSize + cellSpacing), 10),
                        BorderStyle = BorderStyle.FixedSingle,
                        BackColor = (i == pda.InputHead) ?
                            Color.FromArgb(255, 235, 156) :
                            Color.White
                    };

                    var label = new Label
                    {
                        Text = pda.InputTape[i]?.ToString() ?? "ε",
                        Dock = DockStyle.Fill,
                        TextAlign = ContentAlignment.MiddleCenter,
                        Font = new Font("Arial", 10, FontStyle.Bold),
                        ForeColor = Color.Black
                    };

                    label.Paint += (s, e) =>
                    {
                        TextRenderer.DrawText(e.Graphics, label.Text, label.Font,
                            new Point(label.Width / 2 - 1, label.Height / 2 - 1),
                            Color.FromArgb(50, 0, 0, 0));

                        TextRenderer.DrawText(e.Graphics, label.Text, label.Font,
                            new Point(label.Width / 2, label.Height / 2),
                            label.ForeColor);
                    };

                    cell.Controls.Add(label);
                    inputPanel.Controls.Add(cell);
                }

                if (pda.InputHead * (cellSize + cellSpacing) > inputPanel.Width - 50)
                {
                    inputPanel.AutoScrollPosition = new Point(
                        Math.Max(0, pda.InputHead * (cellSize + cellSpacing) - inputPanel.Width / 2),
                        0
                    );
                }
            }

            private void RenderStack()
            {
                stackPanel.Controls.Clear();
                int cellHeight = 25;
                int cellWidth = 35;
                int startY = stackPanel.Height - cellHeight - 10;

                // رسم قاعدة المكدس
                var basePanel = new Panel
                {
                    Size = new Size(70, 8),
                    Location = new Point(stackPanel.Width / 2 - 35, stackPanel.Height - 10),
                    BackColor = Color.FromArgb(139, 69, 19)
                };
                stackPanel.Controls.Add(basePanel);

                // رسم عناصر المكدس
                for (int i = 0; i < pda.Stack.Count; i++)
                {
                    var cell = new Panel
                    {
                        Size = new Size(cellWidth, cellHeight),
                        Location = new Point(stackPanel.Width / 2 - cellWidth / 2,
                                           startY - i * cellHeight),
                        BorderStyle = BorderStyle.FixedSingle,
                        BackColor = Color.FromArgb(173, 216, 230)
                    };

                    var label = new Label
                    {
                        Text = pda.Stack.ElementAt(i).ToString(),
                        Dock = DockStyle.Fill,
                        TextAlign = ContentAlignment.MiddleCenter,
                        Font = new Font("Arial", 9, FontStyle.Bold),
                        ForeColor = Color.Black
                    };

                    cell.Controls.Add(label);
                    stackPanel.Controls.Add(cell);

                    // رسم سهم المؤشر
                    if (i == 0)
                    {
                        var pointer = new Label
                        {
                            Text = "▲",
                            Location = new Point(stackPanel.Width / 2 - 10,
                                               startY - i * cellHeight - 20),
                            Size = new Size(20, 20),
                            Font = new Font("Arial", 10),
                            ForeColor = Color.Red
                        };
                        stackPanel.Controls.Add(pointer);
                    }
                }
            }

            private void RenderTransitions()
            {
                lstTransitions.Items.Clear();
                lstTransitions.Items.Add("===== قواعد الانتقال =====");

                foreach (var t in pda.GetAllTransitions())
                {
                    lstTransitions.Items.Add(t);
                }

                lstTransitions.Items.Add("");
                lstTransitions.Items.Add("===== سجل التنفيذ =====");

                foreach (var t in pda.TransitionHistory)
                {
                    lstTransitions.Items.Add(t);
                }

                if (lstTransitions.Items.Count > 0)
                    lstTransitions.TopIndex = lstTransitions.Items.Count - 1;
            }

            private void StateDiagramBox_Paint(object sender, PaintEventArgs e)
            {
                if (pda.States.Count == 0) return;

                Graphics g = e.Graphics;
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
                g.Clear(Color.White);

                // تطبيق التحويلات
                var transform = g.Transform;
                transform.Translate(diagramOffset.X, diagramOffset.Y);
                transform.Scale(diagramScale, diagramScale);
                g.Transform = transform;

                // رسم الأسهم أولاً
                DrawEnhancedTransitions(g);

                // رسم الحالات
                DrawOptimizedStates(g);
            }

            private void DrawOptimizedStates(Graphics g)
            {
                Font stateFont = new Font("Segoe UI", 8, FontStyle.Bold);
                StringFormat sf = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                };

                foreach (var state in pda.States)
                {
                    if (!statePositions.ContainsKey(state)) continue;

                    Point pos = statePositions[state];
                    bool isCurrent = state == pda.CurrentState;
                    bool isFinal = pda.FinalStates.Contains(state);

                    // إنشاء فرشاة متدرجة للحالة
                    using (var stateBrush = new LinearGradientBrush(
                        new Rectangle(pos.X - stateRadius, pos.Y - stateRadius,
                                     stateRadius * 2, stateRadius * 2),
                        isCurrent ? Color.FromArgb(180, 230, 180) : Color.FromArgb(180, 180, 230),
                        isCurrent ? Color.FromArgb(100, 180, 100) : Color.FromArgb(100, 100, 180),
                        LinearGradientMode.ForwardDiagonal))
                    {
                        g.FillEllipse(stateBrush, pos.X - stateRadius, pos.Y - stateRadius,
                                      stateRadius * 2, stateRadius * 2);
                    }

                    // رسم حدود الحالة
                    using (var statePen = new Pen(isCurrent ? Color.DarkGreen : Color.DarkBlue, 1.5f))
                    {
                        g.DrawEllipse(statePen, pos.X - stateRadius, pos.Y - stateRadius,
                                     stateRadius * 2, stateRadius * 2);

                        // دائرة مزدوجة للحالات النهائية
                        if (isFinal)
                        {
                            g.DrawEllipse(statePen,
                                pos.X - stateRadius + 3, pos.Y - stateRadius + 3,
                                stateRadius * 2 - 6, stateRadius * 2 - 6);
                        }
                    }

                    // اسم الحالة مع تأثير الظل
                    TextRenderer.DrawText(g, state, stateFont,
                        new Rectangle(pos.X - stateRadius, pos.Y - stateRadius,
                                    stateRadius * 2, stateRadius * 2),
                        Color.Black, Color.Empty,
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                }
            }

            private void DrawEnhancedTransitions(Graphics g)
            {
                using (var arrowPen = new Pen(Color.FromArgb(80, 80, 120), 1.5f))
                {
                    arrowPen.CustomEndCap = new AdjustableArrowCap(5, 5);

                    foreach (var trans in pda.Transitions)
                    {
                        if (!statePositions.ContainsKey(trans.CurrentState)) continue;
                        if (!statePositions.ContainsKey(trans.NewState)) continue;

                        Point start = statePositions[trans.CurrentState];
                        Point end = statePositions[trans.NewState];

                        // حساب اتجاه السهم
                        double angle = Math.Atan2(end.Y - start.Y, end.X - start.X);

                        // ضبط نقاط البداية والنهاية على محيط الدوائر
                        Point adjustedStart = new Point(
                            (int)(start.X + stateRadius * Math.Cos(angle)),
                            (int)(start.Y + stateRadius * Math.Sin(angle)));

                        Point adjustedEnd = new Point(
                            (int)(end.X - stateRadius * Math.Cos(angle)),
                            (int)(end.Y - stateRadius * Math.Sin(angle)));

                        // تحديد لون الانتقال بناءً على العملية
                        if (trans.StackPush == "ε")
                            arrowPen.Color = Color.FromArgb(200, 80, 80); // أحمر للإزالة
                        else if (trans.StackPop.ToString() == "ε")
                            arrowPen.Color = Color.FromArgb(80, 200, 80); // أخضر للإضافة
                        else
                            arrowPen.Color = Color.FromArgb(80, 80, 200); // أزرق للتعديل

                        // رسم خط منحني للانتقالات
                        if (trans.CurrentState == trans.NewState)
                        {
                            // رسم حلقة ذاتية
                            int loopSize = 30;
                            Rectangle loopRect = new Rectangle(
                                start.X - loopSize,
                                start.Y - stateRadius - loopSize * 2,
                                loopSize * 2,
                                loopSize * 2);

                            g.DrawArc(arrowPen, loopRect, 0, 360);
                        }
                        else
                        {
                            // رسم خط منحني لتحسين المظهر
                            Point controlPoint = CalculateControlPoint(start, end);
                            g.DrawBezier(arrowPen, adjustedStart, controlPoint, controlPoint, adjustedEnd);
                        }

                        // رسم نص الانتقال
                        DrawTransitionLabel(g, trans, start, end, angle);
                    }
                }
            }

            private Point CalculateControlPoint(Point start, Point end)
            {
                int deltaX = end.X - start.X;
                int deltaY = end.Y - start.Y;

                // حساب نقطة التحكم لإنشاء منحنى لطيف
                return new Point(
                    start.X + deltaX / 2 - deltaY / 3,
                    start.Y + deltaY / 2 + deltaX / 3
                );
            }

        private void DrawTransitionLabel(Graphics g, PushdownAutomata.PDATransition trans, Point start, Point end, double angle)
        {
            string input = trans.Input == null ? "ε" : trans.Input.ToString();
            string label = $"{input},{trans.StackPop}/{trans.StackPush}";

            Font labelFont = new Font("Segoe UI", 7, FontStyle.Bold);
            SizeF textSize = g.MeasureString(label, labelFont);

            // Calculate label position
            PointF labelPos;
            if (trans.CurrentState == trans.NewState)
            {
                // Position the label above the self-loop
                labelPos = new PointF(start.X, start.Y - 45);
            }
            else
            {
                // Position the label in the middle of the line with a vertical offset
                labelPos = new PointF(
                    (start.X + end.X) / 2 - textSize.Width / 2,
                    (start.Y + end.Y) / 2 - textSize.Height / 2
                );

                // Offset the label away from the line
                float offset = 15;
                labelPos = new PointF(
                    labelPos.X - (float)(offset * Math.Sin(angle)),
                    labelPos.Y + (float)(offset * Math.Cos(angle))
                );
            }

            // Draw label background
            using (var backBrush = new SolidBrush(Color.FromArgb(240, 240, 240)))
            {
                g.FillRectangle(backBrush,
                    labelPos.X - 2, labelPos.Y - 2,
                    textSize.Width + 4, textSize.Height + 4);
            }

            // Draw label border
            g.DrawRectangle(Pens.LightGray,
                labelPos.X - 2, labelPos.Y - 2,
                textSize.Width + 4, textSize.Height + 4);

            // Draw label text
            g.DrawString(label, labelFont, Brushes.DarkSlateBlue, labelPos);
        }

            private void EnableAdvancedRendering()
            {
                typeof(PictureBox).GetProperty("DoubleBuffered",
                    System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.NonPublic)
                    .SetValue(stateDiagramBox, true, null);
            }

            private void BtnStep_Click(object sender, EventArgs e)
            {
                if (pda.States.Count == 0)
                {
                    MessageBox.Show("الرجاء تهيئة الآلة أولاً", "تحذير",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (pda.IsHalted)
                {
                    lblStatus.Text = "الآلة توقفت - لا يمكن تنفيذ خطوات أخرى";
                    return;
                }

                pda.Step();
                UpdateUI();
                lblStatus.Text = "تم تنفيذ خطوة واحدة";
            }

            private async void BtnRun_Click(object sender, EventArgs e)
            {
                if (pda.States.Count == 0)
                {
                    MessageBox.Show("الرجاء تهيئة الآلة أولاً", "تحذير",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (pda.IsHalted)
                {
                    lblStatus.Text = "الآلة توقفت - لا يمكن التشغيل";
                    return;
                }

                btnRun.Enabled = false;
                btnStep.Enabled = false;
                btnStop.Enabled = true;
                btnReset.Enabled = false;
                isRunning = true;
                cancellationTokenSource = new CancellationTokenSource();

                try
                {
                    lblStatus.Text = "جاري التشغيل...";

                    await Task.Run(() =>
                    {
                        while (!pda.IsHalted && !cancellationTokenSource.Token.IsCancellationRequested)
                        {
                            pda.Step();
                            this.Invoke((MethodInvoker)UpdateUI);
                            Thread.Sleep(simulationSpeed);
                        }
                    }, cancellationTokenSource.Token);

                    lblStatus.Text = pda.IsHalted ? "اكتمل التشغيل بنجاح" : "تم إيقاف التشغيل";
                }
                catch (TaskCanceledException)
                {
                    lblStatus.Text = "تم إيقاف التشغيل";
                }
                finally
                {
                    btnRun.Enabled = true;
                    btnStep.Enabled = true;
                    btnStop.Enabled = false;
                    btnReset.Enabled = true;
                    isRunning = false;
                }
            }

            private void BtnStop_Click(object sender, EventArgs e)
            {
                cancellationTokenSource?.Cancel();
                btnStop.Enabled = false;
                btnRun.Enabled = true;
                btnStep.Enabled = true;
                btnReset.Enabled = true;
                isRunning = false;
            }

            private void BtnReset_Click(object sender, EventArgs e)
            {
                if (isRunning)
                {
                    MessageBox.Show("الرجاء إيقاف المحاكاة أولاً", "تحذير",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (pda.States.Count == 0)
                {
                    MessageBox.Show("الرجاء تهيئة الآلة أولاً", "تحذير",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                pda.Initialize(txtInput.Text);
                UpdateUI();
                lblStatus.Text = "تم إعادة تعيين الآلة";
            }

            private void SaveConfiguration()
            {
                SaveFileDialog sfd = new SaveFileDialog
                {
                    Filter = "تكوين آلة الدفع الذكية|*.pda",
                    Title = "حفظ تكوين الآلة"
                };

                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        var config = new PDAConfig
                        {
                            States = pda.States,
                            FinalStates = pda.FinalStates,
                            InputAlphabet = pda.InputAlphabet,
                            StackAlphabet = pda.StackAlphabet,
                            StackStartSymbol = pda.StackStartSymbol,
                            InitialState = pda.CurrentState,
                            SampleInput = txtInput.Text,
                            Transitions = pda.Transitions.Select(t => new TransitionConfig1
                            {
                                CurrentState = t.CurrentState,
                                Input = t.Input,
                                StackPop = t.StackPop,
                                NewState = t.NewState,
                                StackPush = t.StackPush
                            }).ToList()
                        };

                        string json = JsonConvert.SerializeObject(config, Formatting.Indented);
                        File.WriteAllText(sfd.FileName, json);

                        MessageBox.Show("تم حفظ التكوين بنجاح", "نجاح",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                        lblStatus.Text = "تم حفظ التكوين";
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"خطأ في الحفظ: {ex.Message}", "خطأ",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                        lblStatus.Text = "خطأ في الحفظ";
                    }
                }
            }

            private void LoadConfiguration()
            {
                OpenFileDialog ofd = new OpenFileDialog
                {
                    Filter = "تكوين آلة الدفع الذكية|*.pda",
                    Title = "تحميل تكوين الآلة"
                };

                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        string json = File.ReadAllText(ofd.FileName);
                        var config = JsonConvert.DeserializeObject<PDAConfig>(json);

                        pda = new PushdownAutomata();
                        pda.States = config.States;
                        pda.FinalStates = config.FinalStates;
                        pda.InputAlphabet = config.InputAlphabet;
                        pda.StackAlphabet = config.StackAlphabet;
                        pda.StackStartSymbol = config.StackStartSymbol;
                        pda.CurrentState = config.InitialState;

                        foreach (var t in config.Transitions)
                        {
                            pda.AddTransition(
                                t.CurrentState,
                                t.Input,
                                t.StackPop,
                                t.NewState,
                                t.StackPush
                            );
                        }

                        txtInput.Text = config.SampleInput;
                        pda.Initialize(txtInput.Text);

                        CalculateStatePositions();
                        UpdateUI();

                        MessageBox.Show("تم تحميل التكوين بنجاح", "نجاح",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                        lblStatus.Text = "تم تحميل التكوين";
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"خطأ في التحميل: {ex.Message}", "خطأ",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                        lblStatus.Text = "خطأ في التحميل";
                    }
                }
            }

            private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
            {
                if (isRunning)
                {
                    var result = MessageBox.Show("المحاكاة قيد التشغيل، هل تريد الخروج؟",
                        "تأكيد الخروج", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

                    if (result == DialogResult.No)
                    {
                        e.Cancel = true;
                    }
                    else
                    {
                        cancellationTokenSource?.Cancel();
                    }
                }
            }

            public class CustomToolStripRenderer : ToolStripProfessionalRenderer
            {
                protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
                {
                    // لا نرسم حدودًا للشريط
                }

                protected override void OnRenderButtonBackground(ToolStripItemRenderEventArgs e)
                {
                    if (e.Item.Selected || e.Item.Pressed)
                    {
                        var rect = new Rectangle(0, 0, e.Item.Width - 1, e.Item.Height - 1);
                        e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(220, 220, 230)), rect);
                        e.Graphics.DrawRectangle(new Pen(Color.FromArgb(180, 180, 200)), rect);
                    }
                }
            }
        }

        public class PushdownAutomata
        {
            public List<string> States { get; set; } = new List<string>();
            public List<string> FinalStates { get; set; } = new List<string>();
            public List<char> InputAlphabet { get; set; } = new List<char>();
            public List<char> StackAlphabet { get; set; } = new List<char>();
            public char StackStartSymbol { get; set; } = 'Z';

            public string CurrentState { get; set; } = "";
            public Stack<char> Stack { get; set; } = new Stack<char>();
            public List<char?> InputTape { get; set; } = new List<char?>();
            public int InputHead { get; set; } = 0;
            public bool IsHalted => FinalStates.Contains(CurrentState) || !HasValidTransition();

            public List<PDATransition> Transitions { get; } = new List<PDATransition>();
            public List<string> TransitionHistory { get; } = new List<string>();

            public class PDATransition
            {
                public string CurrentState { get; set; }
                public char? Input { get; set; }
                public char StackPop { get; set; }
                public string NewState { get; set; }
                public string StackPush { get; set; }

                public override string ToString()
                {
                    string input = Input == null ? "ε" : Input.ToString();
                    return $"{CurrentState}, {input}, {StackPop} → {NewState}, {StackPush}";
                }
            }

            public void AddTransition(
                string currentState,
                char? input,
                char stackPop,
                string newState,
                string stackPush)
            {
                Transitions.Add(new PDATransition
                {
                    CurrentState = currentState,
                    Input = input,
                    StackPop = stackPop,
                    NewState = newState,
                    StackPush = stackPush
                });
            }

            public void Initialize(string input)
            {
                InputTape = new List<char?>();
                foreach (char c in input)
                {
                    InputTape.Add(c);
                }
                InputTape.Add(null);

                Stack = new Stack<char>();
                Stack.Push(StackStartSymbol);

                InputHead = 0;
                TransitionHistory.Clear();
            }

            public void Step()
            {
                if (string.IsNullOrEmpty(CurrentState)) return;
                if (IsHalted) return;

                char? currentInput = InputHead < InputTape.Count ? InputTape[InputHead] : null;
                char currentStackTop = Stack.Count > 0 ? Stack.Peek() : '\0';

                var transition = Transitions.FirstOrDefault(t =>
                    t.CurrentState == CurrentState &&
                    (t.Input == currentInput || (t.Input == null && currentInput != null)) &&
                    t.StackPop == currentStackTop);

                if (transition != null)
                {
                    string inputStr = currentInput == null ? "ε" : currentInput.ToString();
                    TransitionHistory.Add($"{CurrentState}, {inputStr}, {currentStackTop} → {transition.NewState}, {transition.StackPush}");

                    if (transition.Input != null)
                    {
                        InputHead++;
                    }

                    if (Stack.Count > 0) Stack.Pop();

                    if (transition.StackPush != "ε")
                    {
                        for (int i = transition.StackPush.Length - 1; i >= 0; i--)
                        {
                            Stack.Push(transition.StackPush[i]);
                        }
                    }

                    CurrentState = transition.NewState;
                }
            }

            private bool HasValidTransition()
            {
                char? currentInput = InputHead < InputTape.Count ? InputTape[InputHead] : null;
                char currentStackTop = Stack.Count > 0 ? Stack.Peek() : '\0';

                return Transitions.Any(t =>
                    t.CurrentState == CurrentState &&
                    (t.Input == currentInput || (t.Input == null && currentInput != null)) &&
                    t.StackPop == currentStackTop);
            }

            public List<string> GetAllTransitions()
            {
                return Transitions.Select(t => t.ToString()).ToList();
            }
        }

        public class PDAConfig
        {
            public List<string> States { get; set; }
            public List<string> FinalStates { get; set; }
            public List<char> InputAlphabet { get; set; }
            public List<char> StackAlphabet { get; set; }
            public char StackStartSymbol { get; set; }
            public string InitialState { get; set; }
            public string SampleInput { get; set; }
            public List<TransitionConfig1> Transitions { get; set; }
        }

        public class TransitionConfig1
        {
            public string CurrentState { get; set; }
            public char? Input { get; set; }
            public char StackPop { get; set; }
            public string NewState { get; set; }
            public string StackPush { get; set; }
        }

        public class ConfigurationForm1 : Form
        {
            private TextBox txtStates;
            private TextBox txtFinalStates;
            private TextBox txtInitialState;
            private TextBox txtInputAlphabet;
            private TextBox txtStackAlphabet;
            private TextBox txtStackStart;
            private DataGridView dgvTransitions;
            private Button btnSave;
            private Button btnCancel;

            public PushdownAutomata PDAConfig { get; set; } = new PushdownAutomata();

            public ConfigurationForm1()
            {
                InitializeComponents();
                this.Size = new Size(850, 600);
                this.Text = "تكوين آلة الدفع الذكية";
                this.StartPosition = FormStartPosition.CenterParent;
                this.BackColor = Color.White;
                this.Font = new Font("Segoe UI", 9);
            }

            private void InitializeComponents()
            {
                int y = 10;
                int labelWidth = 200;
                int textBoxWidth = 300;

                // الحالات
                Label lblStates = new Label
                {
                    Text = "الحالات (مفصولة بفاصلة):",
                    Location = new Point(20, y),
                    Width = labelWidth,
                    AutoSize = false,
                    Font = new Font("Segoe UI", 9)
                };
                txtStates = new TextBox
                {
                    Location = new Point(230, y),
                    Size = new Size(textBoxWidth, 30),
                    Text = "q0, q1, qf",
                    Font = new Font("Segoe UI", 9)
                };
                y += 40;

                // الحالات النهائية
                Label lblFinalStates = new Label
                {
                    Text = "الحالات النهائية (مفصولة بفاصلة):",
                    Location = new Point(20, y),
                    Width = labelWidth,
                    AutoSize = false,
                    Font = new Font("Segoe UI", 9)
                };
                txtFinalStates = new TextBox
                {
                    Location = new Point(230, y),
                    Size = new Size(textBoxWidth, 30),
                    Text = "qf",
                    Font = new Font("Segoe UI", 9)
                };
                y += 40;

                // الحالة الأولية
                Label lblInitialState = new Label
                {
                    Text = "الحالة الأولية:",
                    Location = new Point(20, y),
                    Width = labelWidth,
                    AutoSize = false,
                    Font = new Font("Segoe UI", 9)
                };
                txtInitialState = new TextBox
                {
                    Location = new Point(230, y),
                    Size = new Size(150, 30),
                    Text = "q0",
                    Font = new Font("Segoe UI", 9)
                };
                y += 40;

                // أبجدية الإدخال
                Label lblInputAlphabet = new Label
                {
                    Text = "أبجدية الإدخال (مفصولة بفاصلة):",
                    Location = new Point(20, y),
                    Width = labelWidth,
                    AutoSize = false,
                    Font = new Font("Segoe UI", 9)
                };
                txtInputAlphabet = new TextBox
                {
                    Location = new Point(230, y),
                    Size = new Size(textBoxWidth, 30),
                    Text = "a, b",
                    Font = new Font("Segoe UI", 9)
                };
                y += 40;

                // أبجدية المكدس
                Label lblStackAlphabet = new Label
                {
                    Text = "أبجدية المكدس (مفصولة بفاصلة):",
                    Location = new Point(20, y),
                    Width = labelWidth,
                    AutoSize = false,
                    Font = new Font("Segoe UI", 9)
                };
                txtStackAlphabet = new TextBox
                {
                    Location = new Point(230, y),
                    Size = new Size(textBoxWidth, 30),
                    Text = "Z, A",
                    Font = new Font("Segoe UI", 9)
                };
                y += 40;

                // رمز بداية المكدس
                Label lblStackStart = new Label
                {
                    Text = "رمز بداية المكدس:",
                    Location = new Point(20, y),
                    Width = labelWidth,
                    AutoSize = false,
                    Font = new Font("Segoe UI", 9)
                };
                txtStackStart = new TextBox
                {
                    Location = new Point(230, y),
                    Size = new Size(50, 30),
                    Text = "Z",
                    Font = new Font("Segoe UI", 9)
                };
                y += 50;

                // جدول الانتقالات
                Label lblTransitions = new Label
                {
                    Text = "قواعد الانتقال:",
                    Location = new Point(20, y),
                    AutoSize = true,
                    Font = new Font("Segoe UI", 10, FontStyle.Bold)
                };
                y += 30;

                dgvTransitions = new DataGridView
                {
                    Location = new Point(20, y),
                    Size = new Size(800, 250),
                    ColumnCount = 6,
                    AllowUserToAddRows = true,
                    AllowUserToDeleteRows = true,
                    AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                    BackgroundColor = Color.White,
                    BorderStyle = BorderStyle.FixedSingle,
                    Font = new Font("Segoe UI", 9)
                };

                dgvTransitions.Columns[0].Name = "الحالة الحالية";
                dgvTransitions.Columns[1].Name = "الإدخال (ε للفارغ)";
                dgvTransitions.Columns[2].Name = "إزالة من المكدس";
                dgvTransitions.Columns[3].Name = "الحالة الجديدة";
                dgvTransitions.Columns[4].Name = "إضافة للمكدس (ε للفارغ)";
                dgvTransitions.Columns[5].Name = "ملاحظات";

                // إضافة أمثلة
                dgvTransitions.Rows.Add("q0", "a", "Z", "q0", "AZ", "دفع A للمكدس");
                dgvTransitions.Rows.Add("q0", "b", "A", "q1", "ε", "إزالة A من المكدس");
                dgvTransitions.Rows.Add("q1", "ε", "Z", "qf", "Z", "الانتقال للحالة النهائية");

                y += 260;

                // أزرار الحفظ والإلغاء
                btnSave = new Button
                {
                    Text = "حفظ",
                    Location = new Point(150, y),
                    Size = new Size(100, 40),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.FromArgb(220, 237, 200),
                    Font = new Font("Segoe UI", 10)
                };
                btnSave.Click += BtnSave_Click;

                btnCancel = new Button
                {
                    Text = "إلغاء",
                    Location = new Point(300, y),
                    Size = new Size(100, 40),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.FromArgb(250, 200, 200),
                    Font = new Font("Segoe UI", 10)
                };
                btnCancel.Click += (s, e) => this.DialogResult = DialogResult.Cancel;

                // إضافة عناصر التحكم
                this.Controls.Add(lblStates);
                this.Controls.Add(txtStates);
                this.Controls.Add(lblFinalStates);
                this.Controls.Add(txtFinalStates);
                this.Controls.Add(lblInitialState);
                this.Controls.Add(txtInitialState);
                this.Controls.Add(lblInputAlphabet);
                this.Controls.Add(txtInputAlphabet);
                this.Controls.Add(lblStackAlphabet);
                this.Controls.Add(txtStackAlphabet);
                this.Controls.Add(lblStackStart);
                this.Controls.Add(txtStackStart);
                this.Controls.Add(lblTransitions);
                this.Controls.Add(dgvTransitions);
                this.Controls.Add(btnSave);
                this.Controls.Add(btnCancel);
            }

            private void BtnSave_Click(object sender, EventArgs e)
            {
                try
                {
                    var states = txtStates.Text.Split(',')
                        .Select(s => s.Trim())
                        .Where(s => !string.IsNullOrEmpty(s))
                        .ToList();

                    var finalStates = txtFinalStates.Text.Split(',')
                        .Select(s => s.Trim())
                        .Where(s => !string.IsNullOrEmpty(s))
                        .ToList();

                    string initialState = txtInitialState.Text.Trim();
                    if (!states.Contains(initialState))
                        throw new Exception("الحالة الأولية غير موجودة في قائمة الحالات");

                    var inputAlphabet = txtInputAlphabet.Text.Split(',')
                        .Select(s => s.Trim())
                        .Where(s => !string.IsNullOrEmpty(s))
                        .Select(s => s[0])
                        .ToList();

                    var stackAlphabet = txtStackAlphabet.Text.Split(',')
                        .Select(s => s.Trim())
                        .Where(s => !string.IsNullOrEmpty(s))
                        .Select(s => s[0])
                        .ToList();

                    char stackStart = string.IsNullOrEmpty(txtStackStart.Text) ? 'Z' : txtStackStart.Text[0];

                    PDAConfig.Transitions.Clear();
                    foreach (DataGridViewRow row in dgvTransitions.Rows)
                    {
                        if (row.IsNewRow) continue;

                        string currentState = row.Cells[0].Value?.ToString()?.Trim();
                        string inputStr = row.Cells[1].Value?.ToString()?.Trim();
                        char? input = string.IsNullOrEmpty(inputStr) || inputStr == "ε" ? null : (char?)inputStr[0];
                        char stackPop = row.Cells[2].Value?.ToString()?.Trim()[0] ?? ' ';
                        string newState = row.Cells[3].Value?.ToString()?.Trim();
                        string stackPush = row.Cells[4].Value?.ToString()?.Trim();

                        if (!string.IsNullOrEmpty(currentState) && !string.IsNullOrEmpty(newState))
                        {
                            PDAConfig.AddTransition(
                                currentState,
                                input,
                                stackPop,
                                newState,
                                stackPush
                            );
                        }
                    }

                    PDAConfig.States = states;
                    PDAConfig.FinalStates = finalStates;
                    PDAConfig.InputAlphabet = inputAlphabet;
                    PDAConfig.StackAlphabet = stackAlphabet;
                    PDAConfig.StackStartSymbol = stackStart;
                    PDAConfig.CurrentState = initialState;

                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"خطأ في الإدخال: {ex.Message}", "خطأ",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }


