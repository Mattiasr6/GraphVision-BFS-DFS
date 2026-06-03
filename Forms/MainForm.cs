using System.Drawing.Drawing2D;
using GraphTraversal.Algorithms;
using GraphTraversal.Models;
using GraphTraversal.Utils;

namespace GraphTraversal.Forms;

public class MainForm : Form
{
    // Datos
    private Graph _graph = null!;
    private int? _selectedStartNodeId;
    private int? _edgeSourceNode;
    private TraversalResult? _lastResult;
    private string _lastAlgorithm = "";

    // Editor
    private bool _dragging;
    private int _dragNodeId;
    private Point _dragOffset;

    // Pan & Zoom
    private float _zoom = 1.0f;
    private PointF _panOffset = PointF.Empty;
    private bool _isPanning;
    private Point _lastMousePan;

    // Controles
    private Panel _canvas = null!;
    private ComboBox _cmbGraph = null!;
    private ComboBox _cmbAlgo = null!;
    private ComboBox _cmbLayout = null!;
    private ComboBox _cmbSpeed = null!;
    private Button _btnExecute = null!;
    private Button _btnPause = null!;
    private Button _btnReset = null!;
    private Button _btnPrev = null!;
    private Button _btnNext = null!;
    private RichTextBox _outputText = null!;
    private Label _lblInfo = null!;
    private Label _lblStepDesc = null!;

    // Animacion async
    private CancellationTokenSource? _cts;
    private bool _paused;
    private int _stepIndex;
    private bool _animating;
    private float _pulsePhase;

    // Paneles
    private Panel _structurePanel = null!;
    private Label _lblStructureTitle = null!;
    private Label _lblStats = null!;
    private Label _lblOrder = null!;

    // PALETA
    private static readonly Color
        Bg =        Color.FromArgb(11, 11, 26),
        PanelBg =   Color.FromArgb(20, 20, 42),
        Cyan =      Color.FromArgb(0, 229, 255),
        Green =     Color.FromArgb(0, 255, 136),
        Gold =      Color.FromArgb(255, 184, 0),
        Magenta =   Color.FromArgb(255, 45, 120),
        Purple =    Color.FromArgb(123, 47, 247),
        Fg =        Color.FromArgb(230, 230, 245),
        TextMuted = Color.FromArgb(130, 130, 170),
        NodeGray =  Color.FromArgb(55, 55, 90),
        BorderDim = Color.FromArgb(35, 35, 65);
    private const int NR = 22;

    public MainForm()
    {
        Text = "Graph Traversal \u2014 BFS & DFS  |  Estructura de Datos";
        Size = new Size(1380, 800);
        MinimumSize = new Size(1100, 600);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Bg;
        Font = new Font("Segoe UI", 10);

        var pulse = new System.Windows.Forms.Timer { Interval = 50 };
        pulse.Tick += (_, _) => { _pulsePhase += 0.12f; if (_animating) _canvas.Invalidate(); };
        pulse.Start();

        BuildUI();
        LoadGraph("Arbol");
        // Double-buffering global
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
    }

    private void BuildUI()
    {
        // HEADER
        var header = new Panel { Height = 110, Dock = DockStyle.Top, BackColor = PanelBg };
        header.Paint += (_, e) =>
        {
            if (header.Width <= 0) return;
            using var b = new LinearGradientBrush(new Point(0,0), new Point(header.Width,0), Cyan, Purple);
            e.Graphics.FillRectangle(b, 0, header.Height - 2, header.Width, 2);
        };

        int y1 = 12, y2 = 52, y3 = 80, bh = 30;

        header.Controls.Add(TLabel("Grafo", 16, y1, 55));
        _cmbGraph = TCombo(74, y1, 155, new[] { "Arbol", "Grafo con ciclos", "Aleatorio (10)", "Aleatorio (15)" });
        _cmbGraph.SelectedIndex = 0;
        _cmbGraph.SelectedIndexChanged += (_, _) => LoadGraph(_cmbGraph.Text);
        header.Controls.Add(_cmbGraph);

        header.Controls.Add(TLabel("Layout", 240, y1, 50));
        _cmbLayout = TCombo(292, y1, 120, new[] { "Circular", "Arbol", "Force-Directed" });
        _cmbLayout.SelectedIndex = 0;
        _cmbLayout.SelectedIndexChanged += (_, _) => ApplyLayout();
        header.Controls.Add(_cmbLayout);

        header.Controls.Add(TLabel("Algoritmo", 425, y1, 70));
        _cmbAlgo = TCombo(498, y1, 175, new[] { "BFS (Anchura)", "DFS Iterativo", "DFS Recursivo" });
        _cmbAlgo.SelectedIndex = 0;
        header.Controls.Add(_cmbAlgo);

        _btnExecute = TButton("\u25b6  Ejecutar", 693, y1 - 2, 118, bh + 4, Cyan, async (_, _) => await ExecuteAlgorithmAsync());
        header.Controls.Add(_btnExecute);

        _btnPause = TButton("\u23f8  Pausa", 825, y1 - 2, 105, bh + 4, Gold, (_, _) => TogglePause());
        _btnPause.Enabled = false;
        header.Controls.Add(_btnPause);

        _btnReset = TButton("\u2715  Detener", 944, y1 - 2, 110, bh + 4, Magenta, (_, _) => ResetVisualization());
        header.Controls.Add(_btnReset);

        header.Controls.Add(TLabel("Vel", 1070, y1, 30));
        _cmbSpeed = TCombo(1098, y1, 85, new[] { "0.5x", "1x", "2x", "4x" });
        _cmbSpeed.SelectedIndex = 1;
        header.Controls.Add(_cmbSpeed);

        // Fila 2: info
        _lblInfo = new Label
        {
            Text = "  Seleccione un nodo como origen  \u00b7  Ejecute BFS o DFS  \u00b7  Vea la cola/pila en tiempo real",
            ForeColor = TextMuted, Location = new Point(10, y2), Size = new Size(1100, 32),
            Font = new Font("Segoe UI", 9, FontStyle.Italic), TextAlign = ContentAlignment.MiddleLeft
        };
        header.Controls.Add(_lblInfo);

        // Fila 3: step navigation
        var stepPanel = new Panel { Location = new Point(10, y3), Size = new Size(800, 28), BackColor = Color.Transparent };

        _btnPrev = TButton("\u25c0  Anterior", 2, 0, 100, 26, Color.FromArgb(100,100,160), (_, _) => StepTo(_stepIndex - 1));
        _btnPrev.Enabled = false;
        stepPanel.Controls.Add(_btnPrev);

        _btnNext = TButton("Siguiente  \u25b6", 108, 0, 110, 26, Color.FromArgb(100,100,160), (_, _) => StepTo(_stepIndex + 1));
        _btnNext.Enabled = false;
        stepPanel.Controls.Add(_btnNext);

        header.Controls.Add(stepPanel);
        Controls.Add(header);

        // SPLITTER — ancho del panel derecho fijo en ~400px
        var split = new SplitContainer { Dock = DockStyle.Fill, BackColor = PanelBg, SplitterWidth = 2, SplitterDistance = 960 };

        // CANVAS con DoubleBuffered
        _canvas = new Panel { Dock = DockStyle.Fill, BackColor = Bg };
        typeof(Panel).GetProperty("DoubleBuffered", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            ?.SetValue(_canvas, true);
        _canvas.Paint += Canvas_Paint!;
        _canvas.MouseClick += Canvas_MouseClick!;
        _canvas.MouseDown += Canvas_MouseDown!;
        _canvas.MouseMove += Canvas_MouseMove!;
        _canvas.MouseUp += Canvas_MouseUp!;
        _canvas.MouseWheel += Canvas_MouseWheel!;
        split.Panel1.Controls.Add(_canvas);

        // PANEL DERECHO — TableLayoutPanel de 3 filas
        var right = new Panel { Dock = DockStyle.Fill, BackColor = PanelBg, Padding = new Padding(14, 8, 14, 8) };

        // Título "RESULTADOS" sobre el TLP
        right.Controls.Add(new Label
        {
            Text = " RESULTADOS", ForeColor = Fg,
            Font = new Font("Segoe UI", 12, FontStyle.Bold),
            Dock = DockStyle.Top, Height = 28
        });

        // TableLayoutPanel: 1 columna, 3 filas
        var tlp = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = Color.Transparent,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        tlp.RowStyles.Add(new RowStyle(SizeType.AutoSize));          // fila 0: labels
        tlp.RowStyles.Add(new RowStyle(SizeType.Absolute, 130F));   // fila 1: structurePanel
        tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));    // fila 2: outputText

        // Fila 0 — FlowLayoutPanel con todos los labels
        var flowLabels = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            WrapContents = false,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 4, 0, 4)
        };

        _lblStructureTitle = new Label
        {
            Text = "Estructura interna:", ForeColor = Gold,
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            AutoSize = true, Margin = new Padding(0, 2, 0, 2)
        };
        flowLabels.Controls.Add(_lblStructureTitle);

        _lblStepDesc = new Label
        {
            Text = "", ForeColor = Cyan,
            Font = new Font("Segoe UI", 9),
            AutoSize = true, Margin = new Padding(0, 2, 0, 2)
        };
        flowLabels.Controls.Add(_lblStepDesc);

        _lblStats = new Label
        {
            Text = "Nodos: 0  \u00b7  Aristas: 0",
            ForeColor = TextMuted,
            Font = new Font("Consolas", 9),
            AutoSize = true, Margin = new Padding(0, 2, 0, 2)
        };
        flowLabels.Controls.Add(_lblStats);

        _lblOrder = new Label
        {
            Text = "Orden de visita:", ForeColor = Fg,
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            AutoSize = true,
            Margin = new Padding(0, 2, 0, 2),
            MinimumSize = new Size(0, 20)
        };
        flowLabels.Controls.Add(_lblOrder);

        tlp.Controls.Add(flowLabels, 0, 0);

        // Fila 1 — Panel de dibujo de cola/pila
        _structurePanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(16, 16, 34),
            BorderStyle = BorderStyle.None,
            Margin = new Padding(0, 4, 0, 4)
        };
        typeof(Panel).GetProperty("DoubleBuffered", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            ?.SetValue(_structurePanel, true);
        _structurePanel.Paint += StructurePanel_Paint!;
        _structurePanel.Paint += (_, e) => { if (_structurePanel.Width <= 0 || _structurePanel.Height <= 0) return; using var p = new Pen(BorderDim, 1); e.Graphics.DrawRectangle(p, 0, 0, _structurePanel.Width - 1, _structurePanel.Height - 1); };
        tlp.Controls.Add(_structurePanel, 0, 1);

        // Fila 2 — RichTextBox
        _outputText = new RichTextBox
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(14, 14, 30),
            ForeColor = Color.FromArgb(180, 210, 255),
            Font = new Font("Consolas", 9),
            BorderStyle = BorderStyle.FixedSingle,
            ReadOnly = true,
            ScrollBars = RichTextBoxScrollBars.Vertical,
            WordWrap = false
        };
        tlp.Controls.Add(_outputText, 0, 2);

        right.Controls.Add(tlp);
        split.Panel2.Controls.Add(right);
        Controls.Add(split);

        KeyPreview = true;
        KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Escape) ResetVisualization();
            if (e.KeyCode == Keys.Space) TogglePause();
            if (e.KeyCode == Keys.Left && _btnPrev.Enabled) StepTo(_stepIndex - 1);
            if (e.KeyCode == Keys.Right && _btnNext.Enabled) StepTo(_stepIndex + 1);
        };

        _graph = new Graph();
    }

    // HELPERS
    private static Label TLabel(string t, int x, int y, int w) =>
        new() { Text = t, ForeColor = TextMuted, Location = new Point(x, y - 2), AutoSize = true, Font = new Font("Segoe UI", 8, FontStyle.Bold) };

    private static ComboBox TCombo(int x, int y, int w, string[] items)
    {
        var cb = new ComboBox { Location = new Point(x, y), Size = new Size(w, 24), DropDownStyle = ComboBoxStyle.DropDownList, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(30, 30, 58), ForeColor = Fg, Font = new Font("Segoe UI", 9), Margin = new Padding(0, 0, 8, 0) };
        cb.Items.AddRange(items);
        return cb;
    }

    private static Button TButton(string t, int x, int y, int w, int h, Color accent, EventHandler click)
    {
        var btn = new Button
        {
            Text = t, Location = new Point(x, y), Size = new Size(w, h),
            FlatStyle = FlatStyle.Flat, FlatAppearance = { BorderSize = 0 },
            ForeColor = Color.White, Cursor = Cursors.Hand,
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            BackColor = Color.FromArgb(30, 30, 60), TextAlign = ContentAlignment.MiddleCenter,
            Margin = new Padding(0, 0, 8, 0)
        };
        btn.Click += click;
        btn.Paint += (_, e) =>
        {
            if (btn.Width <= 4 || btn.Height <= 4) return;
            var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;
            var r = new Rectangle(2, 2, btn.Width - 4, btn.Height - 4);
            using var path = RoundedRect(r, 6);
            using var pen = new Pen(accent, 2); g.DrawPath(pen, path);
        };
        btn.MouseEnter += (_, _) => { btn.BackColor = Color.FromArgb(45, 45, 80); btn.Invalidate(); };
        btn.MouseLeave += (_, _) => { btn.BackColor = Color.FromArgb(30, 30, 60); btn.Invalidate(); };
        btn.EnabledChanged += (_, _) =>
        {
            btn.ForeColor = btn.Enabled ? Color.White : Color.FromArgb(120, 120, 140);
            btn.Invalidate();
        };
        return btn;
    }

    private static GraphicsPath RoundedRect(Rectangle r, int arc)
    {
        var p = new GraphicsPath();
        p.AddArc(r.X, r.Y, arc, arc, 180, 90);
        p.AddArc(r.Right - arc, r.Y, arc, arc, 270, 90);
        p.AddArc(r.Right - arc, r.Bottom - arc, arc, arc, 0, 90);
        p.AddArc(r.X, r.Bottom - arc, arc, arc, 90, 90);
        p.CloseFigure(); return p;
    }

    private int SpeedMs => _cmbSpeed.Text switch { "0.5x" => 800, "1x" => 400, "2x" => 200, "4x" => 100, _ => 400 };

    private void LoadGraph(string t)
    {
        _graph = t switch
        {
            "Arbol" => GraphGenerator.CreateSampleTree(),
            "Grafo con ciclos" => GraphGenerator.CreateSampleGraph(),
            "Aleatorio (10)" => GraphGenerator.CreateRandom(10, 0.35),
            "Aleatorio (15)" => GraphGenerator.CreateRandom(15, 0.25),
            _ => GraphGenerator.CreateSampleTree()
        };
        ApplyLayout(); ClearSel(); UpdateStats(); _canvas.Focus();
    }

    private void ApplyLayout()
    {
        if (_graph == null || _graph.NodeCount == 0) return;
        int cx = _canvas.Width > 0 ? _canvas.Width / 2 : 480;
        int cy = _canvas.Height > 0 ? _canvas.Height / 2 : 290;

        switch (_cmbLayout.Text)
        {
            case "Arbol":
                _graph.CalculateTreeLayout(new Point(cx, cy), 80, 90); break;
            case "Force-Directed":
                _graph.CalculateForceDirectedLayout(new Point(cx, cy), _canvas.Width, _canvas.Height, 80);
                break;
            default: // Circular
                int r = Math.Min(cx, cy) - 70; if (r < 100) r = 100;
                _graph.CalculateCircularLayout(new Point(cx, cy), r); break;
        }
        ClearSel(); _canvas.Invalidate();
    }

    private void ClearSel()
    {
        if (_graph != null) foreach (var n in _graph.Nodes.Values) n.IsSelected = false;
        _selectedStartNodeId = null; _edgeSourceNode = null; _lastResult = null;
        _stepIndex = 0; _animating = false; _paused = false;
        _cts?.Cancel(); _cts = null;
        _btnPause.Enabled = false; _btnPause.Text = "\u23f8  Pausa";
        _btnPrev.Enabled = false; _btnNext.Enabled = false;
        _btnExecute.Enabled = false;
        _outputText.Clear(); _lblStepDesc.Text = "";
        _lblStructureTitle.Text = "Estructura interna:"; _structurePanel.Invalidate();
    }

    private void UpdateStats()
    {
        if (_graph == null) return;
        _lblStats.Text = $"Nodos: {_graph.NodeCount}  \u00b7  Aristas: {_graph.EdgeCount}" + (_graph.NodeCount == 0 ? "" : "  \u00b7  Click vacio: crear nodo");
    }

    // ─── EJECUCION ASYNC ───────────────────────────────────
    private async Task ExecuteAlgorithmAsync()
    {
        try
        {
            if (_selectedStartNodeId == null) { _lblInfo.Text = "Seleccione un nodo origen."; return; }
            if (_graph.NodeCount == 0) return;

            _btnExecute.Enabled = false;
            _cmbGraph.Enabled = false; _cmbLayout.Enabled = false; _cmbAlgo.Enabled = false;
            _btnPrev.Enabled = false; _btnNext.Enabled = false;

            int start = _selectedStartNodeId.Value; _lastAlgorithm = _cmbAlgo.Text;

            // Ejecutar algoritmo (sincronico: captura todos los snapshots)
            _lastResult = _lastAlgorithm switch
            {
                "BFS (Anchura)" => BFS.Execute(_graph, start),
                "DFS Iterativo" => DFS.ExecuteIterative(_graph, start),
                "DFS Recursivo" => DFS.ExecuteRecursive(_graph, start),
                _ => null
            };
            if (_lastResult == null) { MessageBox.Show("El algoritmo no devolvió resultados.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

            // Resumen en output
            _outputText.Clear();
            _outputText.AppendText($"  {_lastAlgorithm}\n");
            _outputText.AppendText($"  Origen: {_graph.GetNode(start)?.Label ?? $"N{start}"}\n");
            _outputText.AppendText($"  Visitados: {_lastResult.NodesVisited}/{_graph.NodeCount}  |  {_lastResult.DurationMs} ms  |  {_lastResult.Steps.Count} pasos\n");
            _outputText.AppendText($"  {new string('\u2550', 34)}\n");
            _outputText.AppendText($"  Recorrido:\n");

            if (_lastResult.Steps.Count == 0)
            {
                MessageBox.Show("El algoritmo se ejecutó pero no generó pasos.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Information);
                FinishAnimation();
                return;
            }

            // Animacion async
            _stepIndex = 0; _animating = true; _paused = false;
            _btnPause.Enabled = true; _btnPause.Text = "\u23f8  Pausa";

            _cts = new CancellationTokenSource();
            var token = _cts.Token;
            _lblInfo.Text = $"Ejecutando {_lastAlgorithm}...";
            _canvas.Invalidate();

            try
            {
                for (int i = 0; i < _lastResult.Steps.Count; i++)
                {
                    token.ThrowIfCancellationRequested();

                    while (_paused)
                    {
                        await Task.Delay(80, token);
                        token.ThrowIfCancellationRequested();
                    }

                    _stepIndex = i;
                    ApplyStep(i);
                    _canvas.Invalidate();
                    await Task.Delay(SpeedMs, token);
                }

                FinishAnimation();
            }
            catch (OperationCanceledException) { }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al ejecutar el algoritmo:\n\n{ex.GetType().Name}: {ex.Message}",
                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            ResetVisualization();
        }
    }

    private void ApplyStep(int idx)
    {
        if (_lastResult == null || idx >= _lastResult.Steps.Count) return;
        var step = _lastResult.Steps[idx];
        _lblStepDesc.Text = $"Paso {idx + 1}/{_lastResult.Steps.Count}: {step.Description}";

        if (idx < _lastResult.VisitOrder.Count)
        {
            int nid = _lastResult.VisitOrder[idx];
            var n = _graph.GetNode(nid);
            string p = _lastAlgorithm.StartsWith("BFS") ? $"  [L{_lastResult.BfsLevels.GetValueOrDefault(nid, 0)}]" : "     ";
            _outputText.AppendText($"  {idx + 1,2}.{p} -> {n?.Label ?? $"N{nid}"}\n");
        }
        _structurePanel.Invalidate();

        // Habilitar navegacion manual mientras se completa
        _btnPrev.Enabled = idx > 0;
        _btnNext.Enabled = idx < _lastResult.Steps.Count - 1;
    }

    private void FinishAnimation()
    {
        _animating = false; _btnPause.Enabled = false;
        _btnExecute.Enabled = true;
        _cmbGraph.Enabled = true; _cmbLayout.Enabled = true; _cmbAlgo.Enabled = true;

        if (_lastResult == null) return;

        _lblStepDesc.Text = $"Completado: {_lastResult.NodesVisited} nodos en {_lastResult.DurationMs} ms";
        _lblInfo.Text = $"{_lastAlgorithm} finalizado. {_lastResult.NodesVisited} nodos visitados.";

        // Mostrar orden completo
        _outputText.Clear();
        _outputText.AppendText($"  {_lastAlgorithm}\n");
        _outputText.AppendText($"  Origen: {_graph.GetNode(_lastResult.StartNode)?.Label ?? $"N{_lastResult.StartNode}"}\n");
        _outputText.AppendText($"  Visitados: {_lastResult.NodesVisited}/{_graph.NodeCount}  |  {_lastResult.DurationMs} ms  |  {_lastResult.Steps.Count} pasos\n");
        _outputText.AppendText($"  {new string('\u2550', 34)}\n");
        _outputText.AppendText($"  Recorrido:\n");
        int c = 1;
        foreach (int id in _lastResult.VisitOrder)
        {
            var n = _graph.GetNode(id);
            string p = _lastAlgorithm.StartsWith("BFS") ? $"  [L{_lastResult.BfsLevels.GetValueOrDefault(id, 0)}]" : "     ";
            _outputText.AppendText($"  {c++,2}.{p} {n?.Label ?? $"N{id}"}\n");
        }
        if (_lastResult.BacktrackEdges.Count > 0 && !_lastAlgorithm.StartsWith("BFS"))
        {
            _outputText.AppendText($"  --- Retrocesos ---\n");
            foreach (var (f, t) in _lastResult.BacktrackEdges)
                _outputText.AppendText($"  {_graph.GetNode(f)?.Label ?? $"N{f}"} -> {_graph.GetNode(t)?.Label ?? $"N{t}"}\n");
        }

        _stepIndex = _lastResult.Steps.Count > 0 ? _lastResult.Steps.Count - 1 : 0;
        _btnPrev.Enabled = _lastResult.Steps.Count > 1;
        _btnNext.Enabled = false;

        _structurePanel.Invalidate(); _canvas.Invalidate();
    }

    // ─── STEP NAVEGACION MANUAL ────────────────────────────
    private void StepTo(int idx)
    {
        if (_lastResult == null || idx < 0 || idx >= _lastResult.Steps.Count) return;
        _stepIndex = idx;
        ApplyStep(idx);
        _btnPrev.Enabled = idx > 0;
        _btnNext.Enabled = idx < _lastResult.Steps.Count - 1;
        _canvas.Invalidate();
    }

    private void TogglePause()
    {
        if (!_animating) return;
        _paused = !_paused;
        _btnPause.Text = _paused ? "\u25b6  Reanudar" : "\u23f8  Pausa";
        _lblInfo.Text = _paused ? "Pausado" : "Reanudado";
    }

    private void ResetVisualization()
    {
        _cts?.Cancel(); _cts = null;
        _animating = false; _paused = false;
        _stepIndex = 0; _lastResult = null;
        _btnPause.Enabled = false; _btnPause.Text = "\u23f8  Pausa";
        _btnPrev.Enabled = false; _btnNext.Enabled = false;
        _btnExecute.Enabled = false;
        _cmbGraph.Enabled = true; _cmbLayout.Enabled = true; _cmbAlgo.Enabled = true;
        _outputText.Clear(); _lblStepDesc.Text = "";
        _lblStructureTitle.Text = "Estructura interna:";
        UpdateStats(); _structurePanel.Invalidate(); _canvas.Invalidate();
        _lblInfo.Text = "Listo. Seleccione un nodo y presione Ejecutar.";
    }

    // ─── PAINT CANVAS ──────────────────────────────────────
    private void Canvas_Paint(object sender, PaintEventArgs e)
    {
        if (_canvas.Width <= 0 || _canvas.Height <= 0) return;

        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.HighQuality;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        // Transformaciones Pan & Zoom
        g.TranslateTransform(_panOffset.X, _panOffset.Y);
        g.ScaleTransform(_zoom, _zoom);

        DrawGrid(g, _canvas.Width, _canvas.Height);

        if (_graph == null || _graph.NodeCount == 0)
        {
            using var f = new Font("Segoe UI", 15, FontStyle.Italic);
            using var b = new SolidBrush(Color.FromArgb(60, 60, 100));
            g.DrawString("Click para crear nodos  \u00b7  Luego ejecute BFS o DFS", f, b,
                _canvas.Width / 2f, _canvas.Height / 2f,
                new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });
            return;
        }

        // Estado de animacion
        var visited = new HashSet<int>(); int active = -1;
        if (_lastResult != null)
        {
            if (_stepIndex > 0 && _stepIndex <= _lastResult.Steps.Count)
            {
                int si = Math.Min(_stepIndex - 1, _lastResult.Steps.Count - 1);
                var snap = _lastResult.Steps[si];
                visited = snap.Visited.ToHashSet();
                active = snap.CurrentNode;
            }
            else
                visited = new HashSet<int>(_lastResult.VisitOrder);
        }

        // Aristas
        foreach (var kv in _graph.AdjacencyList)
        {
            var fn = _graph.GetNode(kv.Key); if (fn == null) continue;
            foreach (int to in kv.Value)
            {
                if (to <= kv.Key) continue;
                var tn = _graph.GetNode(to); if (tn == null) continue;

                bool tree = _lastResult != null && (_lastResult.TreeEdges.Contains((kv.Key, to)) || _lastResult.TreeEdges.Contains((to, kv.Key)));
                bool back = _lastResult != null && (_lastResult.BacktrackEdges.Contains((kv.Key, to)) || _lastResult.BacktrackEdges.Contains((to, kv.Key)));
                bool bothVisited = visited.Contains(kv.Key) && visited.Contains(to);

                Color ec; float ew; DashStyle ds = DashStyle.Solid;

                if (back && bothVisited) { ec = Magenta; ew = 2.5f; ds = DashStyle.Dot; }
                else if (tree && bothVisited) { ec = Cyan; ew = 3f; }
                else if (tree) { ec = Color.FromArgb(0, 160, 200); ew = 2f; }
                else if (bothVisited) { ec = Color.FromArgb(80, 80, 140); ew = 1.5f; }
                else { ec = Color.FromArgb(45, 45, 80); ew = 1f; }

                using var pen = new Pen(ec, ew) { DashStyle = ds };
                g.DrawLine(pen, fn.Position, tn.Position);

                if (tree && bothVisited)
                {
                    using var gp = new Pen(Color.FromArgb(35, Cyan), 8);
                    g.DrawLine(gp, fn.Position, tn.Position);
                }
            }
        }

        // Nodos
        foreach (var node in _graph.Nodes.Values)
        {
            var r = new Rectangle(node.Position.X - NR, node.Position.Y - NR, NR * 2, NR * 2);
            bool sel = _selectedStartNodeId == node.Id;
            bool isActive = node.Id == active;
            bool vis = visited.Contains(node.Id);
            bool inRes = _lastResult != null && _lastResult.VisitOrder.Contains(node.Id);
            bool edgeSrc = _edgeSourceNode == node.Id;
            bool highlight = sel || isActive || edgeSrc;
            bool isNodeSelected = node.IsSelected;

            // Sombra
            var sh = new Rectangle(r.X + 3, r.Y + 4, r.Width, r.Height);
            using (var sb = new SolidBrush(Color.FromArgb(45, 0, 0, 0))) g.FillEllipse(sb, sh);

            // Gradiente radial
            Color baseC, centerC;
            if (isActive) { baseC = Gold; centerC = Color.FromArgb(255, 230, 140); }
            else if (sel || edgeSrc) { baseC = Color.FromArgb(255, 180, 50); centerC = Color.FromArgb(255, 220, 140); }
            else if (vis) { baseC = Green; centerC = Color.FromArgb(100, 255, 200); }
            else if (inRes) { baseC = Color.FromArgb(40, 130, 210); centerC = Color.FromArgb(100, 180, 255); }
            else { baseC = NodeGray; centerC = Color.FromArgb(90, 90, 130); }

            using var path = new GraphicsPath();
            path.AddEllipse(r);
            using var pgb = new PathGradientBrush(path)
            {
                CenterPoint = new PointF(r.X + r.Width * 0.35f, r.Y + r.Height * 0.3f),
                CenterColor = centerC,
                SurroundColors = new[] { baseC }
            };
            g.FillEllipse(pgb, r);

            // Borde
            Color bc;
            float bw;
            if (isNodeSelected)
            {
                bc = Color.FromArgb(0, 255, 100); // verde brillante
                bw = 4f;
            }
            else if (highlight)
            {
                bc = Color.White;
                bw = 3f;
            }
            else
            {
                bc = Color.FromArgb(100, 100, 150);
                bw = 1.5f;
            }
            using var bp = new Pen(bc, bw); g.DrawEllipse(bp, r);

            // Glow activo con pulso
            if (isActive)
            {
                float pulse = 0.4f + 0.3f * (float)Math.Sin(_pulsePhase);
                var glowR = Rectangle.Inflate(r, 6, 6);
                using var gp = new Pen(Color.FromArgb((int)(80 * pulse), Gold), 4);
                g.DrawEllipse(gp, glowR);
            }

            // Label
            using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            using var nf = new Font("Segoe UI", 9, FontStyle.Bold);
            using var tb = new SolidBrush(Color.White);
            g.DrawString(node.Label, nf, tb, r, sf);

            // Nivel BFS
            if (_lastResult != null && _lastResult.BfsLevels.ContainsKey(node.Id) && _lastAlgorithm.StartsWith("BFS"))
            {
                using var lf = new Font("Segoe UI", 7);
                using var lb = new SolidBrush(Color.FromArgb(200, 255, 255));
                g.DrawString($"L{_lastResult.BfsLevels[node.Id]}", lf, lb,
                    new Rectangle(node.Position.X + 14, node.Position.Y - 14, 22, 12), sf);
            }
        }
    }

    // GRID DE FONDO
    private static void DrawGrid(Graphics g, int w, int h)
    {
        if (w <= 0 || h <= 0) return;
        int s = 40;
        using var p = new Pen(Color.FromArgb(10, 50, 70, 100), 0.5f);
        for (int x = 0; x < w; x += s) g.DrawLine(p, x, 0, x, h);
        for (int y = 0; y < h; y += s) g.DrawLine(p, 0, y, w, y);
    }

    // PAINT - COLA/PILA
    private void StructurePanel_Paint(object sender, PaintEventArgs e)
    {
        if (_structurePanel.Width <= 0 || _structurePanel.Height <= 0) return;

        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Color.FromArgb(16, 16, 34));

        if (_lastResult == null || _lastResult.Steps.Count == 0)
        {
            using var f = new Font("Segoe UI", 9, FontStyle.Italic);
            using var b = new SolidBrush(Color.FromArgb(70, 70, 110));
            g.DrawString("Presione Ejecutar para ver el estado\nde la estructura en tiempo real.", f, b, 8, 10);
            return;
        }

        int idx = Math.Max(0, Math.Min(_stepIndex - 1, _lastResult.Steps.Count - 1));
        var step = _lastResult.Steps[idx];
        var state = step.StructureState;
        string st = _lastAlgorithm.StartsWith("BFS") ? "COLA (FIFO)" : "PILA (LIFO)";
        _lblStructureTitle.Text = $"  {st}";

        int bw = 42, bh = 38, gap = 5;
        int tw = state.Count * (bw + gap) - gap;
        int sx = Math.Max(6, (_structurePanel.Width - tw) / 2);
        int sy = (_structurePanel.Height - bh) / 2 - 2;

        if (state.Count == 0)
        {
            using var f = new Font("Segoe UI", 9, FontStyle.Italic);
            using var b = new SolidBrush(Color.FromArgb(70, 70, 110));
            g.DrawString("(vacio)", f, b, 8, sy + 6);
        }

        for (int i = 0; i < state.Count; i++)
        {
            var node = _graph.GetNode(state[i]);
            int x = sx + i * (bw + gap);
            var rect = new Rectangle(x, sy, bw, bh);
            bool vis = step.Visited.Contains(state[i]);

            var c1 = vis ? Color.FromArgb(30, 100, 190) : Color.FromArgb(50, 50, 85);
            var c2 = vis ? Color.FromArgb(50, 160, 230) : Color.FromArgb(70, 70, 110);
            using var lg = new LinearGradientBrush(rect, c2, c1, LinearGradientMode.Vertical);
            g.FillRectangle(lg, rect);

            using var p = new Pen(vis ? Cyan : Color.FromArgb(80, 80, 130), 1.5f);
            g.DrawRectangle(p, rect);

            using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            using var nf = new Font("Segoe UI", 9, FontStyle.Bold);
            using var tb = new SolidBrush(Color.White);
            g.DrawString(node?.Label ?? "?", nf, tb, rect, sf);

            if (i < state.Count - 1)
            {
                int ax = x + bw + gap / 2;
                using var ap = new Pen(Color.FromArgb(100, 100, 160), 1.5f);
                g.DrawLine(ap, ax, sy + bh / 2, ax + 4, sy + bh / 2);
                g.DrawLine(ap, ax + 4, sy + bh / 2, ax + 1, sy + bh / 2 - 4);
                g.DrawLine(ap, ax + 4, sy + bh / 2, ax + 1, sy + bh / 2 + 4);
            }
        }

        string dir = _lastAlgorithm.StartsWith("BFS") ? "entrada  ->  salida" : "tope (entrada/salida)";
        using var inf = new Font("Segoe UI", 7, FontStyle.Italic);
        using var ib = new SolidBrush(Color.FromArgb(80, 80, 130));
        g.DrawString(dir, inf, ib, 8, _structurePanel.Height - 16);
    }

    // ─── PAN & ZOOM ─────────────────────────────────────
    private PointF ScreenToWorld(Point screen)
    {
        return new PointF(
            (screen.X - _panOffset.X) / _zoom,
            (screen.Y - _panOffset.Y) / _zoom
        );
    }

    private void Canvas_MouseWheel(object? sender, MouseEventArgs e)
    {
        float oldZoom = _zoom;
        if (e.Delta > 0)
            _zoom *= 1.1f;
        else
            _zoom /= 1.1f;

        _zoom = Math.Clamp(_zoom, 0.2f, 5.0f);

        // Zoom hacia el cursor: ajustar panOffset para que el punto bajo el mouse no se mueva
        float factor = _zoom / oldZoom;
        _panOffset = new PointF(
            e.X - factor * (e.X - _panOffset.X),
            e.Y - factor * (e.Y - _panOffset.Y)
        );

        _canvas.Invalidate();
    }

    // MOUSE
    private GraphNode? HitTest(Point worldPt)
    {
        foreach (var n in _graph.Nodes.Values)
        {
            int dx = worldPt.X - n.Position.X, dy = worldPt.Y - n.Position.Y;
            if (dx * dx + dy * dy <= (NR + 6) * (NR + 6)) return n;
        }
        return null;
    }

    private void Canvas_MouseClick(object sender, MouseEventArgs e)
    {
        if (_animating) return;
        _canvas.Focus();

        PointF world = ScreenToWorld(e.Location);
        Point worldPt = new Point((int)world.X, (int)world.Y);

        if (e.Button == MouseButtons.Right)
        {
            var h = HitTest(worldPt);
            if (h != null) { if (h.Id == _selectedStartNodeId) _selectedStartNodeId = null; if (h.Id == _edgeSourceNode) _edgeSourceNode = null; _graph.RemoveNode(h.Id); ResetVisualization(); _canvas.Invalidate(); UpdateStats(); }
            return;
        }
        // Botón central: solo para Pan, ignorar cualquier creación/selección
        if (e.Button == MouseButtons.Middle) return;

        // A partir de aquí solo responde el botón izquierdo
        if (e.Button != MouseButtons.Left) return;

        // Limpiar IsSelected de todos los nodos
        foreach (var n in _graph.Nodes.Values) n.IsSelected = false;

        var hit = HitTest(worldPt);
        if (hit == null)
        {
            // Click en vacío: crear nodo en coordenadas world
            int id = _graph.AddNode($"N{_graph.GetNextAvailableId()}");
            var n = _graph.GetNode(id);
            if (n != null) { n.Position = worldPt; _canvas.Invalidate(); UpdateStats(); }
            _selectedStartNodeId = null;
            _edgeSourceNode = null;
            _btnExecute.Enabled = false;
            _lblInfo.Text = "Nodo creado. Click sobre un nodo para seleccionarlo como origen.";
            return;
        }

        // Click sobre nodo existente
        if (_edgeSourceNode == null)
        {
            _edgeSourceNode = hit.Id;
            hit.IsSelected = true;
            _lblInfo.Text = $"\U0001f517 Nodo {hit.Label} como origen de arista. Click en otro nodo para conectar, o clic otra vez en este mismo para elegirlo como origen del recorrido.";
        }
        else if (_edgeSourceNode == hit.Id)
        {
            _selectedStartNodeId = hit.Id;
            hit.IsSelected = true;
            _btnExecute.Enabled = true;
            _edgeSourceNode = null;
            _lblInfo.Text = $"\U0001f3af Nodo {hit.Label} seleccionado como origen. Ahora presione Ejecutar.";
        }
        else
        {
            _graph.AddEdge(_edgeSourceNode.Value, hit.Id);
            _lblInfo.Text = $"\U0001f517 Arista creada: {_graph.GetNode(_edgeSourceNode.Value)?.Label} <-> {hit.Label}";
            _edgeSourceNode = null;
            ResetVisualization();
        }
        _canvas.Invalidate(); UpdateStats();
    }

    private void Canvas_MouseDown(object sender, MouseEventArgs e)
    {
        if (_animating) return;

        // Pan con boton central
        if (e.Button == MouseButtons.Middle)
        {
            _isPanning = true;
            _lastMousePan = e.Location;
            _canvas.Cursor = Cursors.SizeAll;
            return;
        }

        if (e.Button != MouseButtons.Left) return;
        PointF w = ScreenToWorld(e.Location);
        var h = HitTest(new Point((int)w.X, (int)w.Y));
        if (h != null)
        {
            _dragging = true; _dragNodeId = h.Id;
            // Offset en screen-space: diferencia entre el click y el centro del nodo en pantalla
            float nodeScreenX = h.Position.X * _zoom + _panOffset.X;
            float nodeScreenY = h.Position.Y * _zoom + _panOffset.Y;
            _dragOffset = new Point((int)(e.X - nodeScreenX), (int)(e.Y - nodeScreenY));
        }
    }

    private void Canvas_MouseMove(object sender, MouseEventArgs e)
    {
        // Pan tracking
        if (_isPanning)
        {
            _panOffset = new PointF(
                _panOffset.X + (e.X - _lastMousePan.X),
                _panOffset.Y + (e.Y - _lastMousePan.Y)
            );
            _lastMousePan = e.Location;
            _canvas.Invalidate();
            return;
        }

        PointF w = ScreenToWorld(e.Location);
        bool overNode = HitTest(new Point((int)w.X, (int)w.Y)) != null;
        _canvas.Cursor = overNode ? Cursors.Hand : Cursors.Default;

        if (!_dragging) return;
        var n = _graph.GetNode(_dragNodeId); if (n == null) return;
        // Convertir screen del mouse a world del nodo compensando offset
        float worldX = (e.X - _panOffset.X - _dragOffset.X) / _zoom;
        float worldY = (e.Y - _panOffset.Y - _dragOffset.Y) / _zoom;
        n.Position = new Point((int)worldX, (int)worldY);
        _canvas.Invalidate();
    }

    private void Canvas_MouseUp(object sender, MouseEventArgs e)
    {
        if (_isPanning) { _isPanning = false; return; }
        _dragging = false;
    }
}
