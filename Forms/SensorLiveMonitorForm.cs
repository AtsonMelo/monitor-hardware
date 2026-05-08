using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Numerics;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using LibreHardwareMonitor.Hardware;

class SensorLiveMonitorForm : Form
{
    private static readonly Color WindowBackground = Color.FromArgb(17, 19, 22);
    private static readonly Color PanelBackground = Color.FromArgb(24, 27, 31);
    private static readonly Color InputBackground = Color.FromArgb(30, 34, 39);
    private static readonly Color ButtonBackground = Color.FromArgb(36, 41, 47);
    private static readonly Color ButtonBorder = Color.FromArgb(58, 66, 74);
    private static readonly Color MainText = Color.FromArgb(230, 234, 238);
    private static readonly Color MutedText = Color.FromArgb(170, 176, 184);

    private readonly HardwareMonitorService _hardwareMonitor;
    private readonly System.Windows.Forms.Timer _timer;
    private readonly Icon? _ownedIcon;
    private readonly BindingList<SensorSampleRow> _history = new();
    private readonly DataGridView _historyGrid;
    private readonly Panel _contentHost;
    private readonly Panel _summaryView;
    private readonly Panel _bitsView;
    private readonly Panel _oscilloscopeView;
    private readonly Panel _historyView;
    private readonly Button _summaryTabButton;
    private readonly Button _bitsTabButton;
    private readonly Button _oscilloscopeTabButton;
    private readonly Button _historyTabButton;
    private readonly Label _statusLabel;
    private readonly Button _pauseButton;
    private readonly Button _refreshButton;
    private readonly Button _closeButton;
    private readonly Dictionary<string, Label> _valueLabels = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Label> _bitLabels = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, RichTextBox> _bitRichTextBoxes = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Label> _oscilloscopeMetricLabels = new(StringComparer.OrdinalIgnoreCase);
    private readonly string _hardwareName;
    private readonly string _hardwareType;
    private readonly string _hardwareIdentifier;
    private readonly string _sensorName;
    private readonly string _sensorType;
    private readonly string _sensorIdentifier;
    private bool _isPaused;
    private int _isRefreshing;
    private float? _lastValue;
    private float? _currentValue;
    private float? _currentMin;
    private float? _currentMax;
    private float? _currentDelta;
    private DateTime? _lastUpdatedAt;
    private int _sampleCount;
    private uint? _previousFloatBits;
    private readonly OscilloscopePanel _oscilloscopePanel;
    private readonly List<OscilloscopeSample> _oscilloscopeSamples = new();
    private const int OscilloscopeBufferSize = 600;
    private bool _hasOscilloscopeSignal;
    private bool _oscilloscopeRunning = true;
    private bool _oscilloscopeHasRenderedFrame;
    private string _oscilloscopeAxisUnit = "--";
    private Label _oscilloscopeTitleLabel = null!;
    private Label _oscilloscopeSensorLabel = null!;
    private Label _oscilloscopeStateLabel = null!;
    private readonly ComboBox _oscilloscopeCouplingCombo;
    private readonly ComboBox _oscilloscopeTriggerModeCombo;
    private readonly ComboBox _oscilloscopeTriggerEdgeCombo;
    private readonly TrackBar _oscilloscopeTriggerLevelTrackBar;
    private readonly NumericUpDown _oscilloscopeTimebaseUpDown;
    private readonly NumericUpDown _oscilloscopeVoltsPerDivUpDown;
    private readonly Button _oscilloscopeRunStopButton;
    private readonly Button _oscilloscopeResetButton;
    private readonly Label _oscilloscopeTriggerLevelValueLabel;
    private readonly Label _oscilloscopeTimebaseValueLabel;
    private readonly Label _oscilloscopeVoltsPerDivValueLabel;

    public SensorLiveMonitorForm(
        HardwareMonitorService hardwareMonitor,
        string hardwareName,
        string hardwareType,
        string hardwareIdentifier,
        string sensorName,
        string sensorType,
        string sensorIdentifier,
        Icon? windowIcon = null,
        bool openOscilloscopeTab = false)
    {
        _hardwareMonitor = hardwareMonitor ?? throw new ArgumentNullException(nameof(hardwareMonitor));
        _hardwareName = hardwareName ?? "--";
        _hardwareType = hardwareType ?? "--";
        _hardwareIdentifier = hardwareIdentifier ?? "--";
        _sensorName = sensorName ?? "--";
        _sensorType = sensorType ?? "--";
        _sensorIdentifier = sensorIdentifier ?? "--";
        _timer = new System.Windows.Forms.Timer { Interval = 1000 };

        Text = "Monitorar sensor";
        AutoScaleMode = AutoScaleMode.Dpi;
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(980, 680);
        Size = new Size(1280, 860);
        BackColor = WindowBackground;
        ForeColor = MainText;
        Font = new Font("Segoe UI", 10, FontStyle.Regular, GraphicsUnit.Point);
        ShowInTaskbar = false;
        ApplyNotebookWindowMode();

        if (windowIcon != null)
        {
            _ownedIcon = (Icon)windowIcon.Clone();
            Icon = _ownedIcon;
        }

        TableLayoutPanel root = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Padding = new Padding(18),
            BackColor = WindowBackground
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        Label title = new()
        {
            Text = "Monitorar sensor",
            Dock = DockStyle.Fill,
            ForeColor = MainText,
            Font = new Font("Segoe UI", 15, FontStyle.Bold, GraphicsUnit.Point),
            Margin = new Padding(0, 0, 0, 12),
            AutoEllipsis = true
        };

        _historyGrid = new DataGridView
        {
            Dock = DockStyle.Fill,
            BackgroundColor = PanelBackground,
            BorderStyle = BorderStyle.None,
            AutoGenerateColumns = false,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            MultiSelect = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            RowHeadersVisible = false,
            EnableHeadersVisualStyles = false,
            GridColor = Color.FromArgb(52, 60, 69),
            ScrollBars = ScrollBars.Vertical,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            ColumnHeadersDefaultCellStyle = BuildHeaderStyle(),
            DefaultCellStyle = BuildCellStyle(),
            AlternatingRowsDefaultCellStyle = BuildAlternatingStyle()
        };
        _historyGrid.Columns.Add(CreateTextColumn("Horário", nameof(SensorSampleRow.Timestamp)));
        _historyGrid.Columns.Add(CreateTextColumn("Valor bruto", nameof(SensorSampleRow.RawValue)));
        _historyGrid.Columns.Add(CreateTextColumn("Valor formatado", nameof(SensorSampleRow.FormattedValue)));
        _historyGrid.Columns.Add(CreateTextColumn("Mínimo", nameof(SensorSampleRow.Min)));
        _historyGrid.Columns.Add(CreateTextColumn("Máximo", nameof(SensorSampleRow.Max)));
        _historyGrid.Columns.Add(CreateTextColumn("Delta", nameof(SensorSampleRow.Delta)));
        _historyGrid.DataSource = _history;
        _summaryView = BuildContentScrollHost(BuildSummaryPanel());
        _bitsView = BuildContentScrollHost(BuildBitInspectorPanel());
        _oscilloscopePanel = new OscilloscopePanel();
        _oscilloscopeCouplingCombo = BuildOscilloscopeComboBox("DC", "AC");
        _oscilloscopeTriggerModeCombo = BuildOscilloscopeComboBox("Auto", "Normal", "Single");
        _oscilloscopeTriggerEdgeCombo = BuildOscilloscopeComboBox("Subida", "Descida");
        _oscilloscopeTriggerLevelTrackBar = BuildOscilloscopeTrackBar();
        _oscilloscopeTimebaseUpDown = BuildOscilloscopeNumericUpDown(0.5m, 30m, 5m, 0.5m);
        _oscilloscopeVoltsPerDivUpDown = BuildOscilloscopeNumericUpDown(0.05m, 5m, 0.5m, 0.05m);
        _oscilloscopeRunStopButton = BuildDarkButton("Stop");
        _oscilloscopeResetButton = BuildDarkButton("Reset");
        _oscilloscopeTriggerLevelValueLabel = BuildOscilloscopeValueLabel();
        _oscilloscopeTimebaseValueLabel = BuildOscilloscopeValueLabel();
        _oscilloscopeVoltsPerDivValueLabel = BuildOscilloscopeValueLabel();
        _oscilloscopeView = BuildContentScrollHost(BuildOscilloscopePanel());
        _historyView = BuildHistoryGridHost();
        _contentHost = BuildContentHost(_summaryView, _bitsView, _oscilloscopeView, _historyView);
        _summaryTabButton = BuildTabButton("Resumo");
        _bitsTabButton = BuildTabButton("Bits");
        _oscilloscopeTabButton = BuildTabButton("Osciloscópio");
        _historyTabButton = BuildTabButton("Histórico");
        _summaryTabButton.Click += (_, _) => ShowView(_summaryView, _summaryTabButton);
        _bitsTabButton.Click += (_, _) => ShowView(_bitsView, _bitsTabButton);
        _oscilloscopeTabButton.Click += (_, _) => ShowView(_oscilloscopeView, _oscilloscopeTabButton);
        _historyTabButton.Click += (_, _) => ShowView(_historyView, _historyTabButton);

        _oscilloscopeCouplingCombo.SelectedIndexChanged += (_, _) => RefreshOscilloscopeRender(force: true);
        _oscilloscopeTriggerModeCombo.SelectedIndexChanged += (_, _) => RefreshOscilloscopeRender(force: true);
        _oscilloscopeTriggerEdgeCombo.SelectedIndexChanged += (_, _) => RefreshOscilloscopeRender(force: true);
        _oscilloscopeTriggerLevelTrackBar.Scroll += (_, _) => RefreshOscilloscopeRender(force: true);
        _oscilloscopeTimebaseUpDown.ValueChanged += (_, _) => RefreshOscilloscopeRender(force: true);
        _oscilloscopeVoltsPerDivUpDown.ValueChanged += (_, _) => RefreshOscilloscopeRender(force: true);
        _oscilloscopeRunStopButton.Click += (_, _) => ToggleOscilloscopeRunStop();
        _oscilloscopeResetButton.Click += (_, _) => ResetOscilloscope();

        FlowLayoutPanel buttons = new()
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            AutoSize = true,
            BackColor = WindowBackground,
            Margin = new Padding(0, 12, 0, 0)
        };

        _closeButton = new Button { Text = "Fechar" };
        ConfigureActionButton(_closeButton);
        _closeButton.Click += (_, _) => Close();

        _refreshButton = new Button { Text = "Atualizar agora" };
        ConfigureActionButton(_refreshButton);
        _refreshButton.Click += async (_, _) => await RefreshSensorAsync();

        _pauseButton = new Button { Text = "Pausar" };
        ConfigureActionButton(_pauseButton);
        _pauseButton.Click += (_, _) => TogglePause();

        buttons.Controls.Add(_closeButton);
        buttons.Controls.Add(_refreshButton);
        buttons.Controls.Add(_pauseButton);

        _statusLabel = new Label
        {
            Dock = DockStyle.Fill,
            ForeColor = MutedText,
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = true,
            Margin = new Padding(0, 12, 0, 0),
            Height = 26
        };

        root.Controls.Add(title, 0, 0);
        root.Controls.Add(BuildTabBar(), 0, 1);
        root.Controls.Add(_contentHost, 0, 2);
        root.Controls.Add(BuildBottomPanel(buttons), 0, 3);

        Controls.Add(root);
        ShowView(_summaryView, _summaryTabButton);
        if (openOscilloscopeTab)
        {
            ShowView(_oscilloscopeView, _oscilloscopeTabButton);
        }

        Shown += async (_, _) =>
        {
            _timer.Start();
            await RefreshSensorAsync();
        };
        _timer.Tick += async (_, _) => await RefreshSensorAsync();
        FormClosing += (_, _) => _timer.Stop();
    }

    private void ApplyNotebookWindowMode()
    {
        Rectangle workingArea = Screen.PrimaryScreen?.WorkingArea ?? new Rectangle(0, 0, 1280, 720);
        bool compactScreen =
            workingArea.Width <= 1366 ||
            workingArea.Height <= 768 ||
            DeviceDpi > 120;

        if (compactScreen)
        {
            WindowState = FormWindowState.Maximized;
        }
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        WindowThemeService.ApplyNativeTitleBarTheme(Handle);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _timer.Dispose();
            _ownedIcon?.Dispose();
        }

        base.Dispose(disposing);
    }

    private Panel BuildTabBar()
    {
        FlowLayoutPanel bar = new()
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            WrapContents = true,
            FlowDirection = FlowDirection.LeftToRight,
            BackColor = WindowBackground,
            Margin = new Padding(0, 0, 0, 12),
            Padding = new Padding(0)
        };

        bar.Controls.Add(_summaryTabButton);
        bar.Controls.Add(_bitsTabButton);
        bar.Controls.Add(_oscilloscopeTabButton);
        bar.Controls.Add(_historyTabButton);

        Panel host = new()
        {
            Dock = DockStyle.Fill,
            BackColor = WindowBackground,
            Margin = new Padding(0)
        };
        host.Controls.Add(bar);
        return host;
    }

    private Panel BuildContentHost(params Control[] views)
    {
        Panel host = new()
        {
            Dock = DockStyle.Fill,
            BackColor = WindowBackground,
            Margin = new Padding(0)
        };

        foreach (Control view in views)
        {
            view.Dock = DockStyle.Fill;
            view.Visible = false;
            host.Controls.Add(view);
        }

        return host;
    }

    private static Panel BuildContentScrollHost(Control content)
    {
        Panel host = new()
        {
            Dock = DockStyle.Fill,
            BackColor = WindowBackground,
            Padding = new Padding(0),
            Margin = new Padding(0),
            AutoScroll = true
        };

        content.Dock = DockStyle.Top;
        host.Controls.Add(content);
        return host;
    }

    private Button BuildTabButton(string text)
    {
        Button button = new()
        {
            Text = text,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Height = 40,
            MinimumSize = new Size(152, 40),
            Margin = new Padding(0, 0, 6, 0),
            Padding = new Padding(14, 0, 14, 0),
            FlatStyle = FlatStyle.Flat,
            BackColor = ButtonBackground,
            ForeColor = Color.FromArgb(194, 199, 206),
            UseVisualStyleBackColor = false,
            AutoEllipsis = true
        };
        button.FlatAppearance.BorderColor = ButtonBorder;
        button.FlatAppearance.MouseOverBackColor = Color.FromArgb(44, 50, 57);
        button.FlatAppearance.MouseDownBackColor = Color.FromArgb(30, 34, 39);
        button.FlatAppearance.BorderSize = 1;
        return button;
    }

    private void ShowView(Control viewToShow, Button activeButton)
    {
        foreach (Control child in _contentHost.Controls)
        {
            child.Visible = ReferenceEquals(child, viewToShow);
        }

        UpdateTabButtonState(_summaryTabButton, ReferenceEquals(activeButton, _summaryTabButton));
        UpdateTabButtonState(_bitsTabButton, ReferenceEquals(activeButton, _bitsTabButton));
        UpdateTabButtonState(_oscilloscopeTabButton, ReferenceEquals(activeButton, _oscilloscopeTabButton));
        UpdateTabButtonState(_historyTabButton, ReferenceEquals(activeButton, _historyTabButton));
    }

    private Panel BuildOscilloscopePanel()
    {
        _oscilloscopePanel.Dock = DockStyle.Fill;
        _oscilloscopePanel.BackColor = Color.FromArgb(8, 10, 12);
        _oscilloscopePanel.BorderStyle = BorderStyle.None;
        _oscilloscopePanel.Paint += OscilloscopePanel_Paint;
        _oscilloscopePanel.Resize += (_, _) => _oscilloscopePanel.Invalidate();

        _oscilloscopeTitleLabel = BuildSectionLabel("Scope virtual");
        _oscilloscopeSensorLabel = BuildOscilloscopeInfoLabel("Coletando amostras...");
        _oscilloscopeStateLabel = BuildOscilloscopeInfoLabel("Coletando amostras...");

        TableLayoutPanel host = new()
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = PanelBackground,
            Margin = new Padding(0)
        };

        host.ColumnCount = 1;
        host.RowCount = 4;
        host.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        host.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        host.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        host.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        host.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        host.Controls.Add(BuildOscilloscopeHeaderPanel(), 0, 0);
        host.Controls.Add(BuildOscilloscopeControlsBar(), 0, 1);
        host.Controls.Add(_oscilloscopePanel, 0, 2);
        host.Controls.Add(BuildOscilloscopeMetricsPanel(), 0, 3);
        _oscilloscopePanel.Height = 340;
        return host;
    }

    private TableLayoutPanel BuildOscilloscopeHeaderPanel()
    {
        TableLayoutPanel panel = new()
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 1,
            BackColor = PanelBackground,
            Margin = new Padding(0, 0, 0, 10),
            Padding = new Padding(14, 12, 14, 8)
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        panel.Controls.Add(_oscilloscopeTitleLabel, 0, 0);
        panel.Controls.Add(_oscilloscopeSensorLabel, 0, 1);
        panel.Controls.Add(_oscilloscopeStateLabel, 0, 2);
        return panel;
    }

    private Panel BuildOscilloscopeControlsBar()
    {
        FlowLayoutPanel bar = new()
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            BackColor = PanelBackground,
            Margin = new Padding(0),
            Padding = new Padding(10, 10, 10, 6)
        };

        bar.Controls.Add(BuildOscilloscopeCouplingPanel());
        bar.Controls.Add(BuildOscilloscopeTriggerPanel());
        bar.Controls.Add(BuildOscilloscopeTimeScalePanel());
        bar.Controls.Add(BuildOscilloscopeActionsPanel());
        return bar;
    }

    private Panel BuildOscilloscopeCouplingPanel()
    {
        TableLayoutPanel panel = BuildOscilloscopeGroupBase("Acoplamento");
        panel.Controls.Add(_oscilloscopeCouplingCombo, 0, 1);
        return WrapOscilloscopeGroup(panel);
    }

    private Panel BuildOscilloscopeTriggerPanel()
    {
        TableLayoutPanel panel = BuildOscilloscopeGroupBase("Trigger");
        panel.Controls.Add(BuildOscilloscopeLabeledRow("Modo", _oscilloscopeTriggerModeCombo), 0, 1);
        panel.Controls.Add(BuildOscilloscopeLabeledRow("Borda", _oscilloscopeTriggerEdgeCombo), 0, 2);
        panel.Controls.Add(BuildOscilloscopeLabeledRow("Nível", BuildOscilloscopeTrackBarHost(_oscilloscopeTriggerLevelTrackBar, _oscilloscopeTriggerLevelValueLabel)), 0, 3);
        return WrapOscilloscopeGroup(panel);
    }

    private Panel BuildOscilloscopeTimeScalePanel()
    {
        TableLayoutPanel panel = BuildOscilloscopeGroupBase("Escalas");
        panel.Controls.Add(BuildOscilloscopeLabeledRow("Tempo/div", BuildOscilloscopeNumericHost(_oscilloscopeTimebaseUpDown, _oscilloscopeTimebaseValueLabel, "s/div")), 0, 1);
        panel.Controls.Add(BuildOscilloscopeLabeledRow("Escala vertical", BuildOscilloscopeNumericHost(_oscilloscopeVoltsPerDivUpDown, _oscilloscopeVoltsPerDivValueLabel, "unid./div")), 0, 2);
        return WrapOscilloscopeGroup(panel);
    }

    private Panel BuildOscilloscopeActionsPanel()
    {
        FlowLayoutPanel panel = new()
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            BackColor = PanelBackground,
            Margin = new Padding(0),
            Padding = new Padding(0, 4, 0, 0)
        };
        panel.Controls.Add(_oscilloscopeRunStopButton);
        panel.Controls.Add(_oscilloscopeResetButton);
        return WrapOscilloscopeGroup(panel, "Ações");
    }

    private static Panel WrapOscilloscopeGroup(Control inner, string? title = null)
    {
        Panel host = new()
        {
            Dock = DockStyle.Top,
            BackColor = PanelBackground,
            Margin = new Padding(4),
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            MinimumSize = new Size(220, 0)
        };
        TableLayoutPanel wrapper = new()
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            ColumnCount = 1,
            RowCount = title is null ? 1 : 2,
            BackColor = PanelBackground,
            Margin = new Padding(0)
        };
        if (title is not null)
        {
            wrapper.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            wrapper.Controls.Add(BuildSectionLabel(title), 0, 0);
        }
        wrapper.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        inner.Dock = DockStyle.Fill;
        wrapper.Controls.Add(inner, 0, title is null ? 0 : 1);
        host.Controls.Add(wrapper);
        return host;
    }

    private static TableLayoutPanel BuildOscilloscopeGroupBase(string title)
    {
        TableLayoutPanel panel = new()
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = PanelBackground,
            Margin = new Padding(0)
        };
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.Controls.Add(BuildSectionLabel(title), 0, 0);
        return panel;
    }

    private static Panel BuildOscilloscopeLabeledRow(string label, Control input)
    {
        TableLayoutPanel row = new()
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 2,
            BackColor = PanelBackground,
            Margin = new Padding(0)
        };
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 112));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        row.Controls.Add(BuildLabel(label, true), 0, 0);
        input.Dock = DockStyle.Fill;
        row.Controls.Add(input, 1, 0);
        Panel host = new() { Dock = DockStyle.Top, AutoSize = true, BackColor = PanelBackground, Margin = new Padding(0) };
        host.Controls.Add(row);
        return host;
    }

    private static Control BuildOscilloscopeTrackBarHost(TrackBar trackBar, Label valueLabel)
    {
        TableLayoutPanel host = new()
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            ColumnCount = 1,
            BackColor = PanelBackground,
            Margin = new Padding(0)
        };
        host.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        host.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        trackBar.Dock = DockStyle.Fill;
        host.Controls.Add(trackBar, 0, 0);
        host.Controls.Add(valueLabel, 0, 1);
        return host;
    }

    private static Control BuildOscilloscopeNumericHost(NumericUpDown numericUpDown, Label valueLabel, string unit)
    {
        TableLayoutPanel host = new()
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            ColumnCount = 2,
            BackColor = PanelBackground,
            Margin = new Padding(0)
        };
        host.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        host.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 74));
        numericUpDown.Dock = DockStyle.Fill;
        valueLabel.Text = unit;
        host.Controls.Add(numericUpDown, 0, 0);
        host.Controls.Add(valueLabel, 1, 0);
        return host;
    }

    private static Label BuildOscilloscopeInfoLabel(string text)
    {
        return new Label
        {
            Dock = DockStyle.Fill,
            AutoEllipsis = true,
            ForeColor = MutedText,
            BackColor = PanelBackground,
            Margin = new Padding(0, 2, 0, 0),
            Text = text,
            Height = 24,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI", 9.25f, FontStyle.Regular, GraphicsUnit.Point)
        };
    }

    private static ComboBox BuildOscilloscopeComboBox(params string[] items)
    {
        ComboBox comboBox = new()
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            BackColor = InputBackground,
            ForeColor = MainText,
            FlatStyle = FlatStyle.Flat,
            Margin = new Padding(0),
            Dock = DockStyle.Top
        };
        comboBox.Items.AddRange(items);
        comboBox.SelectedIndex = 0;
        return comboBox;
    }

    private static TrackBar BuildOscilloscopeTrackBar()
    {
        TrackBar trackBar = new()
        {
            Minimum = 0,
            Maximum = 100,
            Value = 50,
            TickFrequency = 10,
            SmallChange = 1,
            LargeChange = 5,
            BackColor = PanelBackground,
            Margin = new Padding(0),
            Dock = DockStyle.Top
        };
        return trackBar;
    }

    private static NumericUpDown BuildOscilloscopeNumericUpDown(decimal minimum, decimal maximum, decimal value, decimal increment)
    {
        return new NumericUpDown
        {
            Minimum = minimum,
            Maximum = maximum,
            Value = value,
            Increment = increment,
            DecimalPlaces = 1,
            BackColor = InputBackground,
            ForeColor = MainText,
            BorderStyle = BorderStyle.FixedSingle,
            TextAlign = HorizontalAlignment.Right,
            Margin = new Padding(0),
            Width = 90
        };
    }

    private static Button BuildDarkButton(string text)
    {
        Button button = new()
        {
            Text = text,
            BackColor = ButtonBackground,
            ForeColor = MainText,
            FlatStyle = FlatStyle.Flat,
            Margin = new Padding(0, 0, 0, 6),
            Width = 110,
            Height = 32
        };
        button.FlatAppearance.BorderColor = ButtonBorder;
        return button;
    }

    private static Label BuildOscilloscopeValueLabel()
    {
        return new Label
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ForeColor = MutedText,
            BackColor = PanelBackground,
            Margin = new Padding(0, 2, 0, 0),
            Text = "--"
        };
    }

    private Control BuildOscilloscopeMetricsPanel()
    {
        TableLayoutPanel panel = new()
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 4,
            RowCount = 2,
            BackColor = PanelBackground,
            Margin = new Padding(0, 10, 0, 0),
            Padding = new Padding(10, 8, 10, 10)
        };

        for (int i = 0; i < 4; i++)
        {
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        }
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        panel.Controls.Add(BuildOscilloscopeMetricTile("Atual", "current", 0, 0), 0, 0);
        panel.Controls.Add(BuildOscilloscopeMetricTile("Mínimo", "min", 1, 0), 1, 0);
        panel.Controls.Add(BuildOscilloscopeMetricTile("Máximo", "max", 2, 0), 2, 0);
        panel.Controls.Add(BuildOscilloscopeMetricTile("Médio", "average", 3, 0), 3, 0);
        panel.Controls.Add(BuildOscilloscopeMetricTile("Pico a pico", "peak_to_peak", 0, 1), 0, 1);
        panel.Controls.Add(BuildOscilloscopeMetricTile("Delta", "delta", 1, 1), 1, 1);
        panel.Controls.Add(BuildOscilloscopeMetricTile("Taxa/s", "rate", 2, 1), 2, 1);
        panel.Controls.Add(BuildOscilloscopeMetricTile("Janela", "window", 3, 1), 3, 1);

        return panel;
    }

    private Control BuildOscilloscopeMetricTile(string title, string key, int column, int row)
    {
        TableLayoutPanel tile = new()
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            BackColor = Color.FromArgb(20, 23, 27),
            Margin = new Padding(4),
            Padding = new Padding(10, 8, 10, 8)
        };
        tile.ColumnCount = 1;
        tile.RowCount = 2;
        tile.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        tile.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        tile.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        Label titleLabel = new()
        {
            Dock = DockStyle.Fill,
            AutoEllipsis = true,
            ForeColor = Color.FromArgb(170, 176, 184),
            BackColor = tile.BackColor,
            Text = title,
            Height = 20,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI", 8.75f, FontStyle.Bold, GraphicsUnit.Point)
        };

        Label valueLabel = new()
        {
            Dock = DockStyle.Fill,
            AutoEllipsis = true,
            ForeColor = MainText,
            BackColor = tile.BackColor,
            Text = "--",
            Height = 22,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI", 10.25f, FontStyle.Bold, GraphicsUnit.Point)
        };

        _oscilloscopeMetricLabels[key] = valueLabel;
        tile.Controls.Add(titleLabel, 0, 0);
        tile.Controls.Add(valueLabel, 0, 1);
        return tile;
    }

    private static void UpdateTabButtonState(Button button, bool isActive)
    {
        button.BackColor = isActive ? Color.FromArgb(39, 44, 50) : ButtonBackground;
        button.ForeColor = isActive ? MainText : Color.FromArgb(194, 199, 206);
        button.FlatAppearance.BorderColor = isActive ? Color.FromArgb(0, 120, 212) : ButtonBorder;
    }

    private Panel BuildHistoryGridHost()
    {
        Panel host = new()
        {
            Dock = DockStyle.Fill,
            BackColor = WindowBackground,
            Padding = new Padding(0),
            Margin = new Padding(0)
        };

        _historyGrid.Dock = DockStyle.Fill;
        host.Controls.Add(_historyGrid);
        return host;
    }

    private TableLayoutPanel BuildSummaryPanel()
    {
        TableLayoutPanel panel = new()
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 2,
            RowCount = 12,
            BackColor = PanelBackground,
            Margin = new Padding(0)
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        AddSummaryRow(panel, 0, "Hardware", _hardwareName);
        AddSummaryRow(panel, 1, "Hardware tipo", _hardwareType);
        AddSummaryRow(panel, 2, "Sensor", _sensorName);
        AddSummaryRow(panel, 3, "Sensor tipo", _sensorType);
        AddSummaryRow(panel, 4, "Sensor ID", _sensorIdentifier);
        AddSummaryRow(panel, 5, "Última atualização", "--");

        _valueLabels["value"] = AddDynamicSummaryRow(panel, 6, "Valor bruto");
        _valueLabels["formatted"] = AddDynamicSummaryRow(panel, 7, "Valor formatado");
        _valueLabels["min"] = AddDynamicSummaryRow(panel, 8, "Mínimo");
        _valueLabels["max"] = AddDynamicSummaryRow(panel, 9, "Máximo");
        _valueLabels["delta"] = AddDynamicSummaryRow(panel, 10, "Delta");
        _valueLabels["samples"] = AddDynamicSummaryRow(panel, 11, "Amostras");

        return panel;
    }

    private TableLayoutPanel BuildBitInspectorPanel()
    {
        // Futuro: canais digitais 0/1 para instrumentos próprios/autorizados, sem captura indevida.
        TableLayoutPanel panel = new()
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 2,
            RowCount = 11,
            BackColor = PanelBackground,
            Margin = new Padding(0)
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        Label sectionTitle = BuildSectionLabel("Bits destacados");
        panel.Controls.Add(sectionTitle, 0, 0);
        panel.SetColumnSpan(sectionTitle, 2);
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        AddBitRow(panel, 1, "Float32 HEX");
        AddBitRow(panel, 2, "Float32 binário");
        AddBitRow(panel, 3, "Sinal");
        AddBitRow(panel, 4, "Expoente");
        AddBitRow(panel, 5, "Mantissa");
        AddBitRow(panel, 6, "Máscara XOR");
        AddBitHighlightedRow(panel, 7, "Bits destacados");
        AddBitRow(panel, 8, "Bits alterados");
        AddBitRow(panel, 9, "Inteiro arredondado");
        AddBitRow(panel, 10, "Inteiro binário");

        return panel;
    }

    private void AddBitRow(TableLayoutPanel panel, int row, string label)
    {
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.Controls.Add(BuildLabel(label, true), 0, row);
        Label valueLabel = BuildLabel("--", false);
        panel.Controls.Add(valueLabel, 1, row);
        _bitLabels[label] = valueLabel;
    }

    private void AddBitHighlightedRow(TableLayoutPanel panel, int row, string label)
    {
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
        panel.Controls.Add(BuildLabel(label, true), 0, row);
        RichTextBox valueBox = BuildBitRichTextBox();
        panel.Controls.Add(valueBox, 1, row);
        _bitRichTextBoxes[label] = valueBox;
    }

    private static RichTextBox BuildBitRichTextBox()
    {
        return new RichTextBox
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            BorderStyle = BorderStyle.None,
            BackColor = PanelBackground,
            ForeColor = MainText,
            Font = new Font("Consolas", 9.25f, FontStyle.Regular, GraphicsUnit.Point),
            WordWrap = false,
            ScrollBars = RichTextBoxScrollBars.None,
            ShortcutsEnabled = false,
            DetectUrls = false,
            HideSelection = true,
            TabStop = false,
            Text = "--"
        };
    }

    private static Panel BuildBottomPanel(Control inner)
    {
        Panel panel = new()
        {
            Dock = DockStyle.Fill,
            BackColor = WindowBackground
        };
        inner.Dock = DockStyle.Fill;
        panel.Controls.Add(inner);
        return panel;
    }

    private void AddSummaryRow(TableLayoutPanel panel, int row, string label, string value)
    {
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.Controls.Add(BuildLabel(label, true), 0, row);
        panel.Controls.Add(BuildLabel(value, false), 1, row);
    }

    private Label AddDynamicSummaryRow(TableLayoutPanel panel, int row, string label)
    {
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.Controls.Add(BuildLabel(label, true), 0, row);
        Label valueLabel = BuildLabel("--", false);
        panel.Controls.Add(valueLabel, 1, row);
        return valueLabel;
    }

    private static Label BuildLabel(string text, bool isTitle)
    {
        return new Label
        {
            Text = text,
            Dock = DockStyle.Fill,
            AutoEllipsis = true,
            ForeColor = isTitle ? MainText : Color.FromArgb(210, 214, 220),
            BackColor = isTitle ? WindowBackground : PanelBackground,
            Padding = new Padding(10, 8, 10, 8),
            Margin = new Padding(0),
            Height = 34,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = isTitle
                ? new Font("Segoe UI", 9.25f, FontStyle.Bold, GraphicsUnit.Point)
                : new Font("Segoe UI", 9.25f, FontStyle.Regular, GraphicsUnit.Point)
        };
    }

    private static Label BuildSectionLabel(string text)
    {
        return new Label
        {
            Text = text,
            Dock = DockStyle.Fill,
            AutoEllipsis = true,
            ForeColor = MainText,
            BackColor = PanelBackground,
            Padding = new Padding(10, 8, 10, 8),
            Margin = new Padding(0),
            Height = 34,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold, GraphicsUnit.Point)
        };
    }

    private static void ConfigureActionButton(Button button)
    {
        button.AutoSize = true;
        button.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        button.Height = 40;
        button.MinimumSize = new Size(142, 40);
        button.Margin = new Padding(0, 0, 0, 0);
        button.Padding = new Padding(10, 0, 10, 0);
        button.FlatStyle = FlatStyle.Flat;
        button.ForeColor = MainText;
        button.BackColor = ButtonBackground;
        button.UseVisualStyleBackColor = false;
        button.FlatAppearance.BorderColor = ButtonBorder;
        button.FlatAppearance.MouseOverBackColor = Color.FromArgb(44, 50, 57);
        button.FlatAppearance.MouseDownBackColor = Color.FromArgb(30, 34, 39);
    }

    private static DataGridViewCellStyle BuildHeaderStyle() => new()
    {
        BackColor = Color.FromArgb(27, 31, 36),
        ForeColor = MainText,
        SelectionBackColor = Color.FromArgb(27, 31, 36),
        SelectionForeColor = MainText,
        Font = new Font("Segoe UI", 9.5f, FontStyle.Bold, GraphicsUnit.Point)
    };

    private static DataGridViewCellStyle BuildCellStyle() => new()
    {
        BackColor = PanelBackground,
        ForeColor = MainText,
        SelectionBackColor = Color.FromArgb(0, 120, 212),
        SelectionForeColor = Color.White,
        WrapMode = DataGridViewTriState.False
    };

    private static DataGridViewCellStyle BuildAlternatingStyle() => new()
    {
        BackColor = InputBackground,
        ForeColor = MainText,
        SelectionBackColor = Color.FromArgb(0, 120, 212),
        SelectionForeColor = Color.White
    };

    private static DataGridViewTextBoxColumn CreateTextColumn(string headerText, string propertyName) => new()
    {
        HeaderText = headerText,
        DataPropertyName = propertyName,
        AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
        MinimumWidth = 110,
        SortMode = DataGridViewColumnSortMode.NotSortable
    };

    private void TogglePause()
    {
        _isPaused = !_isPaused;
        _pauseButton.Text = _isPaused ? "Continuar" : "Pausar";
        _statusLabel.Text = _isPaused
            ? "Atualização pausada."
            : "Atualização em tempo real ativa.";
    }

    private async Task RefreshSensorAsync()
    {
        if (_isPaused || Interlocked.CompareExchange(ref _isRefreshing, 1, 0) != 0)
        {
            return;
        }

        try
        {
            List<SensorReading> readings = await Task.Run(() => _hardwareMonitor.ReadAllSensors());
            if (_isPaused || IsDisposed)
            {
                return;
            }

            SensorReading? reading = readings.FirstOrDefault(sensor =>
                sensor.HardwareType.ToString().Equals(_hardwareType, StringComparison.OrdinalIgnoreCase) &&
                sensor.HardwareName.Equals(_hardwareName, StringComparison.OrdinalIgnoreCase) &&
                sensor.HardwareIdentifier.Equals(_hardwareIdentifier, StringComparison.OrdinalIgnoreCase) &&
                sensor.SensorType.ToString().Equals(_sensorType, StringComparison.OrdinalIgnoreCase) &&
                sensor.SensorName.Equals(_sensorName, StringComparison.OrdinalIgnoreCase) &&
                sensor.SensorIdentifier.Equals(_sensorIdentifier, StringComparison.OrdinalIgnoreCase));

            if (IsHandleCreated && !IsDisposed)
            {
                BeginInvoke(new Action(() => ApplyReading(reading)));
            }
        }
        finally
        {
            Interlocked.Exchange(ref _isRefreshing, 0);
        }
    }

    private void ApplyReading(SensorReading? reading)
    {
        if (IsDisposed)
        {
            return;
        }

        if (reading == null)
        {
            _statusLabel.Text = "Sensor não encontrado nesta atualização.";
            _valueLabels["value"].Text = "--";
            _valueLabels["formatted"].Text = "--";
            _valueLabels["min"].Text = "--";
            _valueLabels["max"].Text = "--";
            _valueLabels["delta"].Text = "--";
            _valueLabels["samples"].Text = _sampleCount.ToString(CultureInfo.CurrentCulture);
            _oscilloscopeStateLabel.Text = "Coletando amostras...";
            SetBitFieldsToMissing();
            return;
        }

        float? delta = _lastValue.HasValue && reading.Value.HasValue
            ? reading.Value.Value - _lastValue.Value
            : null;

        _currentValue = reading.Value;
        _currentMin = reading.Min;
        _currentMax = reading.Max;
        _currentDelta = delta;
        _lastValue = reading.Value;
        _lastUpdatedAt = DateTime.Now;
        _sampleCount++;

        string timestamp = _lastUpdatedAt.Value.ToString("dd/MM/yyyy HH:mm:ss", CultureInfo.CurrentCulture);
        _valueLabels["value"].Text = FormatRawValue(reading.Value);
        _valueLabels["formatted"].Text = FormatFormattedValue(reading.SensorType, reading.Value);
        _valueLabels["min"].Text = FormatRawValue(reading.Min);
        _valueLabels["max"].Text = FormatRawValue(reading.Max);
        _valueLabels["delta"].Text = FormatRawValue(delta);
        _valueLabels["samples"].Text = _sampleCount.ToString(CultureInfo.CurrentCulture);
        _statusLabel.Text = $"Última atualização: {timestamp}";
        _oscilloscopeTitleLabel.Text = $"{reading.SensorName}";
        _oscilloscopeSensorLabel.Text = $"{reading.HardwareName} · {reading.HardwareType} · {reading.SensorType} · {GetSensorUnit(reading.SensorType.ToString())}";
        _oscilloscopeStateLabel.Text = $"Última leitura: {timestamp}";
        UpdateBitInspector(reading.Value);
        UpdateOscilloscope(reading.Value);

        _history.Insert(0, new SensorSampleRow
        {
            Timestamp = timestamp,
            RawValue = FormatRawValue(reading.Value),
            FormattedValue = FormatFormattedValue(reading.SensorType, reading.Value),
            Min = FormatRawValue(reading.Min),
            Max = FormatRawValue(reading.Max),
            Delta = FormatRawValue(delta)
        });

        while (_history.Count > 100)
        {
            _history.RemoveAt(_history.Count - 1);
        }

        _history.ResetBindings();
        _historyGrid.Refresh();
    }

    private void UpdateOscilloscope(float? value)
    {
        _hasOscilloscopeSignal = value.HasValue;
        if (!value.HasValue)
        {
            _oscilloscopeSamples.Clear();
            _oscilloscopeHasRenderedFrame = false;
            _oscilloscopeAxisUnit = GetSensorUnit(_sensorType);
            UpdateOscilloscopeMetrics(Array.Empty<OscilloscopeSample>(), null, null, null, null, null, null, null, null, "Coletando amostras...");
            _oscilloscopePanel.Invalidate();
            return;
        }

        AppendOscilloscopeSample(value.Value);
        RefreshOscilloscopeRender(force: _oscilloscopeRunning || !_oscilloscopeHasRenderedFrame);
    }

    private void AppendOscilloscopeSample(float value)
    {
        DateTime timestamp = DateTime.Now;
        _oscilloscopeSamples.Add(new OscilloscopeSample(timestamp, value));
        TrimOscilloscopeBuffer(timestamp);

        while (_oscilloscopeSamples.Count > OscilloscopeBufferSize)
        {
            _oscilloscopeSamples.RemoveAt(0);
        }
    }

    private void TrimOscilloscopeBuffer(DateTime latestTimestamp)
    {
        DateTime cutoff = latestTimestamp - TimeSpan.FromMinutes(12);
        _oscilloscopeSamples.RemoveAll(sample => sample.Timestamp < cutoff);

        while (_oscilloscopeSamples.Count > OscilloscopeBufferSize)
        {
            _oscilloscopeSamples.RemoveAt(0);
        }
    }

    private void RefreshOscilloscopeRender(bool force)
    {
        if (!_hasOscilloscopeSignal)
        {
            _oscilloscopeHasRenderedFrame = false;
            UpdateOscilloscopeMetrics(Array.Empty<OscilloscopeSample>(), null, null, null, null, null, null, null, null, "Coletando amostras...");
            _oscilloscopePanel.Invalidate();
            return;
        }

        if (!force && !_oscilloscopeRunning)
        {
            return;
        }

        List<OscilloscopeSample> visibleSamples = GetVisibleOscilloscopeSamples();
        if (visibleSamples.Count == 0)
        {
            _oscilloscopeHasRenderedFrame = false;
            UpdateOscilloscopeMetrics(Array.Empty<OscilloscopeSample>(), null, null, null, null, null, null, null, null, "Coletando amostras...");
            _oscilloscopePanel.Invalidate();
            return;
        }

        float timebase = (float)_oscilloscopeTimebaseUpDown.Value;
        bool acCoupling = string.Equals(_oscilloscopeCouplingCombo.SelectedItem as string, "AC", StringComparison.OrdinalIgnoreCase);
        float triggerLevel = (_oscilloscopeTriggerLevelTrackBar.Value - 50f) / 50f;
        bool risingEdge = string.Equals(_oscilloscopeTriggerEdgeCombo.SelectedItem as string, "Subida", StringComparison.OrdinalIgnoreCase);
        string triggerMode = _oscilloscopeTriggerModeCombo.SelectedItem as string ?? "Auto";
        OscilloscopeWindow window = CalculateOscilloscopeWindow(visibleSamples, timebase, triggerLevel, risingEdge, triggerMode);
        List<float> displaySamples = BuildDisplaySamples(visibleSamples, acCoupling);
        OscilloscopeStatistics statistics = CalculateStatistics(visibleSamples, displaySamples);
        UpdateOscilloscopeMetrics(
            visibleSamples,
            statistics.Current,
            statistics.Minimum,
            statistics.Maximum,
            statistics.Average,
            statistics.PeakToPeak,
            statistics.Delta,
            statistics.RatePerSecond,
            window,
            BuildOscilloscopeStatusText(visibleSamples, statistics, acCoupling));

        _oscilloscopeHasRenderedFrame = true;
        _oscilloscopeTriggerLevelValueLabel.Text = $"{((_oscilloscopeTriggerLevelTrackBar.Value - 50f) / 50f):0.00}";
        _oscilloscopeTimebaseValueLabel.Text = $"{timebase:0.0} s/div";
        _oscilloscopeVoltsPerDivValueLabel.Text = $"{(float)_oscilloscopeVoltsPerDivUpDown.Value:0.00} {_oscilloscopeAxisUnit}/div";
        _oscilloscopeRunStopButton.Text = _oscilloscopeRunning ? "Stop" : "Run";
        _oscilloscopePanel.Invalidate();
    }

    private List<OscilloscopeSample> GetVisibleOscilloscopeSamples()
    {
        if (_oscilloscopeSamples.Count == 0)
        {
            return new List<OscilloscopeSample>();
        }

        OscilloscopeWindow window = CalculateOscilloscopeWindow(
            _oscilloscopeSamples,
            (float)_oscilloscopeTimebaseUpDown.Value,
            (_oscilloscopeTriggerLevelTrackBar.Value - 50f) / 50f,
            string.Equals(_oscilloscopeTriggerEdgeCombo.SelectedItem as string, "Subida", StringComparison.OrdinalIgnoreCase),
            _oscilloscopeTriggerModeCombo.SelectedItem as string ?? "Auto");

        List<OscilloscopeSample> visibleSamples = _oscilloscopeSamples
            .Where(sample => sample.Timestamp >= window.Start && sample.Timestamp <= window.End)
            .ToList();

        if (visibleSamples.Count == 0)
        {
            visibleSamples.Add(_oscilloscopeSamples[^1]);
        }

        return visibleSamples;
    }

    private OscilloscopeWindow CalculateOscilloscopeWindow(
        IReadOnlyList<OscilloscopeSample> samples,
        float timebase,
        float triggerLevel,
        bool risingEdge,
        string triggerMode)
    {
        if (samples.Count == 0)
        {
            DateTime now = DateTime.Now;
            return new OscilloscopeWindow(now.AddSeconds(-10), now);
        }

        DateTime end = samples[^1].Timestamp;
        TimeSpan visibleSpan = TimeSpan.FromSeconds(Math.Max(10.0, timebase * 10.0));
        DateTime start = end - visibleSpan;

        if (!string.Equals(triggerMode, "Auto", StringComparison.OrdinalIgnoreCase) && samples.Count > 1)
        {
            int triggerIndex = FindTriggerStartIndex(samples, triggerLevel, risingEdge, triggerMode);
            if (triggerIndex >= 0)
            {
                DateTime triggerTime = samples[triggerIndex].Timestamp;
                DateTime triggeredStart = triggerTime - TimeSpan.FromSeconds(Math.Max(6.0, visibleSpan.TotalSeconds * 0.2));
                start = triggeredStart;
                end = start + visibleSpan;
            }
        }

        if (string.Equals(triggerMode, "Auto", StringComparison.OrdinalIgnoreCase) && end < samples[^1].Timestamp)
        {
            end = samples[^1].Timestamp;
            start = end - visibleSpan;
        }

        return new OscilloscopeWindow(start, end);
    }

    private static int FindTriggerStartIndex(
        IReadOnlyList<OscilloscopeSample> samples,
        float triggerLevel,
        bool risingEdge,
        string triggerMode)
    {
        if (samples.Count < 2)
        {
            return -1;
        }

        float min = samples.Min(sample => sample.Value);
        float max = samples.Max(sample => sample.Value);
        float threshold = min + ((max - min) * ((triggerLevel + 1f) / 2f));
        bool preferFirstCrossing = !string.Equals(triggerMode, "Auto", StringComparison.OrdinalIgnoreCase);

        for (int i = 1; i < samples.Count; i++)
        {
            float previous = samples[i - 1].Value;
            float current = samples[i].Value;
            bool crossed = risingEdge
                ? previous < threshold && current >= threshold
                : previous > threshold && current <= threshold;
            if (crossed)
            {
                return i;
            }
        }

        if (preferFirstCrossing)
        {
            return Math.Min(samples.Count - 1, samples.Count / 4);
        }

        return -1;
    }

    private void ToggleOscilloscopeRunStop()
    {
        _oscilloscopeRunning = !_oscilloscopeRunning;
        _oscilloscopeRunStopButton.Text = _oscilloscopeRunning ? "Stop" : "Run";
        if (_oscilloscopeRunning)
        {
            RefreshOscilloscopeRender(force: true);
        }
    }

    private void ResetOscilloscope()
    {
        _oscilloscopeSamples.Clear();
        _hasOscilloscopeSignal = false;
        _oscilloscopeHasRenderedFrame = false;
        _oscilloscopeAxisUnit = GetSensorUnit(_sensorType);
        UpdateOscilloscopeMetrics(Array.Empty<OscilloscopeSample>(), null, null, null, null, null, null, null, null, "Coletando amostras...");
        _oscilloscopePanel.Invalidate();
    }

    private void OscilloscopePanel_Paint(object? sender, PaintEventArgs e)
    {
        Rectangle bounds = _oscilloscopePanel.ClientRectangle;
        e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        e.Graphics.Clear(Color.FromArgb(8, 10, 12));

        if (bounds.Width <= 2 || bounds.Height <= 2)
        {
            return;
        }

        Rectangle plotBounds = new Rectangle(bounds.Left + 60, bounds.Top + 16, bounds.Width - 76, bounds.Height - 34);
        if (plotBounds.Width <= 20 || plotBounds.Height <= 20)
        {
            return;
        }

        List<OscilloscopeSample> visibleSamples = GetVisibleOscilloscopeSamples();
        bool acCoupling = string.Equals(_oscilloscopeCouplingCombo.SelectedItem as string, "AC", StringComparison.OrdinalIgnoreCase);
        List<float> displaySamples = BuildDisplaySamples(visibleSamples, acCoupling);
        OscilloscopeWindow window = CalculateOscilloscopeWindow(
            visibleSamples,
            (float)_oscilloscopeTimebaseUpDown.Value,
            (_oscilloscopeTriggerLevelTrackBar.Value - 50f) / 50f,
            string.Equals(_oscilloscopeTriggerEdgeCombo.SelectedItem as string, "Subida", StringComparison.OrdinalIgnoreCase),
            _oscilloscopeTriggerModeCombo.SelectedItem as string ?? "Auto");

        OscilloscopeStatistics statistics = CalculateStatistics(visibleSamples, displaySamples);
        if (displaySamples.Count > 0)
        {
            UpdateOscilloscopeMetrics(
                visibleSamples,
                statistics.Current,
                statistics.Minimum,
                statistics.Maximum,
                statistics.Average,
                statistics.PeakToPeak,
                statistics.Delta,
                statistics.RatePerSecond,
                window,
                BuildOscilloscopeStatusText(visibleSamples, statistics, acCoupling));
        }

        float minValue = statistics.Minimum;
        float maxValue = statistics.Maximum;
        float range = Math.Abs(maxValue - minValue);
        float minimumRange = GetMinimumOscilloscopeRange(statistics.Average);
        if (range < minimumRange)
        {
            float center = statistics.Average;
            float halfRange = minimumRange / 2f;
            minValue = center - halfRange;
            maxValue = center + halfRange;
        }

        if (Math.Abs(maxValue - minValue) < 0.001f)
        {
            maxValue = minValue + 1f;
        }

        using (Brush backgroundBrush = new SolidBrush(Color.FromArgb(8, 10, 12)))
        using (Pen gridMajor = new(Color.FromArgb(36, 43, 49)))
        using (Pen gridMinor = new(Color.FromArgb(23, 27, 32)))
        using (Pen axisPen = new(Color.FromArgb(58, 78, 89)))
        using (Pen traceGlow = new(Color.FromArgb(75, 0, 220, 140), 5f))
        using (Pen tracePen = new(Color.FromArgb(0, 220, 140), 2.25f))
        {
            e.Graphics.FillRectangle(backgroundBrush, bounds);

            int columns = 10;
            int rows = 8;
            for (int i = 0; i <= columns; i++)
            {
                int x = plotBounds.Left + (plotBounds.Width * i / columns);
                e.Graphics.DrawLine(i % 5 == 0 ? gridMajor : gridMinor, x, plotBounds.Top, x, plotBounds.Bottom);
            }

            for (int i = 0; i <= rows; i++)
            {
                int y = plotBounds.Top + (plotBounds.Height * i / rows);
                e.Graphics.DrawLine(i % 4 == 0 ? gridMajor : gridMinor, plotBounds.Left, y, plotBounds.Right, y);
            }

            if (statistics.HasData)
            {
                int rowsForLabels = 4;
                using Font axisFont = new("Segoe UI", 8.75f, FontStyle.Regular, GraphicsUnit.Point);
                using Brush axisBrush = new SolidBrush(Color.FromArgb(185, 191, 196));

                for (int i = 0; i <= rowsForLabels; i++)
                {
                    float ratio = i / (float)rowsForLabels;
                    float value = maxValue - ((maxValue - minValue) * ratio);
                    int y = plotBounds.Top + (int)Math.Round(plotBounds.Height * ratio);
                    string label = FormatAxisValue(value, _oscilloscopeAxisUnit);
                    SizeF textSize = e.Graphics.MeasureString(label, axisFont);
                    e.Graphics.DrawString(label, axisFont, axisBrush, plotBounds.Left - textSize.Width - 6, y - textSize.Height / 2f);
                    e.Graphics.DrawLine(i == 2 ? axisPen : gridMinor, plotBounds.Left, y, plotBounds.Right, y);
                }

                PointF[] points = BuildOscilloscopePoints(displaySamples, visibleSamples, window, plotBounds, minValue, maxValue);
                if (points.Length == 1)
                {
                    using Pen singlePen = new(Color.FromArgb(0, 220, 140), 2.25f);
                    e.Graphics.DrawLine(singlePen, plotBounds.Left, points[0].Y, plotBounds.Right, points[0].Y);
                }
                else if (points.Length >= 2)
                {
                    e.Graphics.DrawLines(traceGlow, points);
                    e.Graphics.DrawLines(tracePen, points);
                    using Brush fillBrush = new SolidBrush(Color.FromArgb(28, 0, 220, 140));
                    e.Graphics.FillPolygon(fillBrush, BuildFillPolygon(points, plotBounds));
                }
                else
                {
                    using Font font = new("Segoe UI", 10, FontStyle.Regular, GraphicsUnit.Point);
                    using Brush brush = new SolidBrush(MutedText);
                    e.Graphics.DrawString("Coletando amostras...", font, brush, plotBounds.Left + 10, plotBounds.Top + 10);
                }
            }

            using Font captionFont = new("Segoe UI", 8.75f, FontStyle.Regular, GraphicsUnit.Point);
            using Brush captionBrush = new SolidBrush(Color.FromArgb(180, 210, 216));
            e.Graphics.DrawString("Scope virtual baseado em leitura real do sensor", captionFont, captionBrush, plotBounds.Left, bounds.Top + 4);
            e.Graphics.DrawString(window.Start.ToString("HH:mm:ss", CultureInfo.CurrentCulture), captionFont, captionBrush, plotBounds.Left, plotBounds.Bottom + 2);
            e.Graphics.DrawString(window.End.ToString("HH:mm:ss", CultureInfo.CurrentCulture), captionFont, captionBrush, plotBounds.Right - 60, plotBounds.Bottom + 2);

            if (!_hasOscilloscopeSignal || visibleSamples.Count < 2)
            {
                using Font font = new("Segoe UI", 10, FontStyle.Bold, GraphicsUnit.Point);
                using Brush brush = new SolidBrush(Color.FromArgb(220, 224, 229));
                e.Graphics.DrawString("Coletando amostras...", font, brush, plotBounds.Left + 12, plotBounds.Top + 12);
            }
        }
    }

    private static PointF[] BuildOscilloscopePoints(
        IReadOnlyList<float> displaySamples,
        IReadOnlyList<OscilloscopeSample> samples,
        OscilloscopeWindow window,
        Rectangle plotBounds,
        float minValue,
        float maxValue)
    {
        if (displaySamples.Count == 0 || samples.Count == 0)
        {
            return Array.Empty<PointF>();
        }

        float valueRange = Math.Max(0.001f, maxValue - minValue);
        float timeRange = Math.Max(0.001f, (float)window.End.Subtract(window.Start).TotalSeconds);
        PointF[] points = new PointF[displaySamples.Count];

        for (int i = 0; i < displaySamples.Count; i++)
        {
            double seconds = samples[i].Timestamp.Subtract(window.Start).TotalSeconds;
            float x = plotBounds.Left + (float)Math.Clamp(seconds / timeRange, 0.0, 1.0) * plotBounds.Width;
            float y = plotBounds.Bottom - 1 - ((displaySamples[i] - minValue) / valueRange * Math.Max(1, plotBounds.Height - 2));
            points[i] = new PointF(x, y);
        }

        return points;
    }

    private static PointF[] BuildFillPolygon(PointF[] points, Rectangle plotBounds)
    {
        List<PointF> polygon = new(points.Length + 2);
        polygon.Add(new PointF(points[0].X, plotBounds.Bottom - 1));
        polygon.AddRange(points);
        polygon.Add(new PointF(points[^1].X, plotBounds.Bottom - 1));
        return polygon.ToArray();
    }

    private static string FormatAxisValue(float value, string unit)
    {
        string formatted = Math.Abs(value) >= 1000f || Math.Abs(value) < 0.01f
            ? value.ToString("0.###E0", CultureInfo.CurrentCulture)
            : value.ToString("0.###", CultureInfo.CurrentCulture);

        return string.IsNullOrWhiteSpace(unit) ? formatted : $"{formatted} {unit}";
    }

    private List<float> BuildDisplaySamples(IReadOnlyList<OscilloscopeSample> samples, bool acCoupling)
    {
        List<float> values = samples.Select(sample => sample.Value).ToList();

        if (values.Count == 0)
        {
            return values;
        }

        if (acCoupling)
        {
            float average = values.Average();
            for (int i = 0; i < values.Count; i++)
            {
                values[i] -= average;
            }
        }

        return values;
    }

    private static OscilloscopeStatistics CalculateStatistics(
        IReadOnlyList<OscilloscopeSample> samples,
        IReadOnlyList<float> values)
    {
        if (values.Count == 0)
        {
            return new OscilloscopeStatistics(false, 0f, 0f, 0f, 0f, 0f, 0f, 0f);
        }

        float current = values[^1];
        float min = values.Min();
        float max = values.Max();
        float average = values.Average();
        float peakToPeak = max - min;
        float delta = values.Count >= 2 ? values[^1] - values[^2] : 0f;
        float rate = 0f;
        if (samples.Count >= 2)
        {
            double seconds = Math.Max(0.001, samples[^1].Timestamp.Subtract(samples[^2].Timestamp).TotalSeconds);
            rate = delta / (float)seconds;
        }

        return new OscilloscopeStatistics(true, current, min, max, average, peakToPeak, delta, rate);
    }

    private void UpdateOscilloscopeMetrics(
        IReadOnlyList<OscilloscopeSample> samples,
        float? current,
        float? minimum,
        float? maximum,
        float? average,
        float? peakToPeak,
        float? delta,
        float? rate,
        OscilloscopeWindow? window = null,
        string? stateText = null)
    {
        _oscilloscopeMetricLabels["current"].Text = FormatMetricValue(current);
        _oscilloscopeMetricLabels["min"].Text = FormatMetricValue(minimum);
        _oscilloscopeMetricLabels["max"].Text = FormatMetricValue(maximum);
        _oscilloscopeMetricLabels["average"].Text = FormatMetricValue(average);
        _oscilloscopeMetricLabels["peak_to_peak"].Text = FormatMetricValue(peakToPeak);
        _oscilloscopeMetricLabels["delta"].Text = FormatMetricValue(delta);
        _oscilloscopeMetricLabels["rate"].Text = FormatMetricRate(rate);
        _oscilloscopeMetricLabels["window"].Text = window.HasValue
            ? $"{window.Value.Start:HH:mm:ss} - {window.Value.End:HH:mm:ss}"
            : samples.Count > 0
                ? $"{samples[0].Timestamp:HH:mm:ss} - {samples[^1].Timestamp:HH:mm:ss}"
                : "--";

        _oscilloscopeStateLabel.Text = string.IsNullOrWhiteSpace(stateText)
            ? samples.Count < 2
                ? "Coletando amostras..."
                : $"Janela ativa: {samples.Count} amostras"
            : stateText;
    }

    private string BuildOscilloscopeStatusText(
        IReadOnlyList<OscilloscopeSample> samples,
        OscilloscopeStatistics statistics,
        bool acCoupling)
    {
        if (samples.Count < 2)
        {
            return "Coletando amostras...";
        }

        string coupling = acCoupling ? "AC" : "DC";
        string rawText = FormatMetricValue(samples[^1].Value);
        string displayText = FormatMetricValue(statistics.Current);
        return $"Bruto: {rawText} | Exibido: {displayText} | Modo: {coupling} | Janela ativa: {samples.Count} amostras";
    }

    private string FormatMetricValue(float? value)
    {
        if (!value.HasValue)
        {
            return "--";
        }

        string formatted = value.Value.ToString("0.###", CultureInfo.CurrentCulture);
        return string.IsNullOrWhiteSpace(_oscilloscopeAxisUnit)
            ? formatted
            : $"{formatted} {_oscilloscopeAxisUnit}";
    }

    private static string FormatMetricRate(float? value)
    {
        return value.HasValue
            ? $"{value.Value:0.###} /s"
            : "--";
    }

    private float GetMinimumOscilloscopeRange(float center)
    {
        float baseRange = _sensorType switch
        {
            "Temperature" => 1.5f,
            "Load" => 8f,
            "Voltage" => 0.2f,
            "Clock" => 15f,
            "Fan" => 80f,
            "Power" => 2f,
            "Frequency" => 5f,
            "Current" => 0.2f,
            _ => 1f
        };

        float magnitudeRange = Math.Abs(center) * 0.08f;

        return Math.Max(baseRange, magnitudeRange);
    }

    private static string GetSensorUnit(string sensorType)
    {
        return sensorType switch
        {
            "Voltage" => "V",
            "Current" => "A",
            "Power" => "W",
            "Clock" => "MHz",
            "Temperature" => "°C",
            "Load" => "%",
            "Frequency" => "Hz",
            "Fan" => "RPM",
            "Flow" => "L/h",
            "Control" => "%",
            "Level" => "%",
            "Data" => "GB",
            "SmallData" => "MB",
            "Throughput" => "MB/s",
            "TimeSpan" => "s",
            "Timing" => "ns",
            "Energy" => "mWh",
            "Noise" => "dBA",
            "Conductivity" => "uS/cm",
            "Humidity" => "%",
            _ => ""
        };
    }

    private static string FormatRawValue(float? value)
    {
        return value.HasValue
            ? value.Value.ToString("0.###", CultureInfo.CurrentCulture)
            : "--";
    }

    private static string FormatFormattedValue(SensorType sensorType, float? value)
    {
        if (!value.HasValue)
        {
            return "--";
        }

        string formattedValue = value.Value.ToString("0.###", CultureInfo.CurrentCulture);
        string unit = sensorType switch
        {
            SensorType.Voltage => "V",
            SensorType.Current => "A",
            SensorType.Power => "W",
            SensorType.Clock => "MHz",
            SensorType.Temperature => "°C",
            SensorType.Load => "%",
            SensorType.Frequency => "Hz",
            SensorType.Fan => "RPM",
            SensorType.Flow => "L/h",
            SensorType.Control => "%",
            SensorType.Level => "%",
            SensorType.Data => "GB",
            SensorType.SmallData => "MB",
            SensorType.Throughput => "MB/s",
            SensorType.TimeSpan => "s",
            SensorType.Timing => "ns",
            SensorType.Energy => "mWh",
            SensorType.Noise => "dBA",
            SensorType.Conductivity => "uS/cm",
            SensorType.Humidity => "%",
            _ => ""
        };

        return string.IsNullOrWhiteSpace(unit) ? formattedValue : $"{formattedValue} {unit}";
    }

    private void UpdateBitInspector(float? value)
    {
        _bitLabels["Float32 HEX"].Text = FormatFloatHex(value);
        _bitLabels["Float32 binário"].Text = FormatFloatBinary(value);
        _bitLabels["Sinal"].Text = FormatFloatSign(value);
        _bitLabels["Expoente"].Text = FormatFloatExponent(value);
        _bitLabels["Mantissa"].Text = FormatFloatMantissa(value);
        _bitLabels["Máscara XOR"].Text = FormatXorMask(value, _previousFloatBits);
        _bitLabels["Bits alterados"].Text = CountChangedBits(value, _previousFloatBits).ToString(CultureInfo.CurrentCulture);
        _bitLabels["Inteiro arredondado"].Text = FormatRoundedInteger(value);
        _bitLabels["Inteiro binário"].Text = FormatRoundedIntegerBinary(value);
        UpdateHighlightedBits(value, _previousFloatBits);

        if (value.HasValue)
        {
            _previousFloatBits = GetFloatBits(value.Value);
        }
    }

    private void UpdateHighlightedBits(float? current, uint? previousBits)
    {
        if (!_bitRichTextBoxes.TryGetValue("Bits destacados", out RichTextBox? box))
        {
            return;
        }

        if (!current.HasValue)
        {
            box.Text = "--";
            return;
        }

        uint currentBits = GetFloatBits(current.Value);
        uint changedBits = previousBits.HasValue ? currentBits ^ previousBits.Value : 0u;
        RenderHighlightedBits(box, currentBits, changedBits);
    }

    private static void RenderHighlightedBits(RichTextBox box, uint currentBits, uint changedBits)
    {
        box.Clear();
        AppendBit(box, ((currentBits >> 31) & 1u) != 0, ((changedBits >> 31) & 1u) != 0);
        AppendSeparator(box);
        for (int i = 30; i >= 23; i--)
        {
            AppendBit(box, ((currentBits >> i) & 1u) != 0, ((changedBits >> i) & 1u) != 0);
        }
        AppendSeparator(box);
        for (int i = 22; i >= 0; i--)
        {
            AppendBit(box, ((currentBits >> i) & 1u) != 0, ((changedBits >> i) & 1u) != 0);
        }
    }

    private static void AppendSeparator(RichTextBox box)
    {
        box.SelectionColor = MainText;
        box.SelectionBackColor = PanelBackground;
        box.AppendText(" | ");
    }

    private static void AppendBit(RichTextBox box, bool bit, bool highlighted)
    {
        box.SelectionColor = highlighted ? Color.Black : MainText;
        box.SelectionBackColor = highlighted ? Color.FromArgb(255, 215, 0) : PanelBackground;
        box.AppendText(bit ? "1" : "0");
    }

    private void SetBitFieldsToMissing()
    {
        foreach (Label label in _bitLabels.Values)
        {
            label.Text = "--";
        }

        foreach (RichTextBox box in _bitRichTextBoxes.Values)
        {
            box.Text = "--";
        }
    }

    private static uint GetFloatBits(float value) => BitConverter.SingleToUInt32Bits(value);

    private static string FormatFloatHex(float? value)
    {
        return value.HasValue ? $"0x{GetFloatBits(value.Value):X8}" : "--";
    }

    private static string FormatFloatBinary(float? value)
    {
        if (!value.HasValue)
        {
            return "--";
        }

        uint bits = GetFloatBits(value.Value);
        string sign = ((bits >> 31) & 1u).ToString(CultureInfo.CurrentCulture);
        string exponent = Convert.ToString((int)((bits >> 23) & 0xFFu), 2).PadLeft(8, '0');
        string mantissa = Convert.ToString((int)(bits & 0x7FFFFFu), 2).PadLeft(23, '0');
        return $"{sign} | {exponent} | {mantissa}";
    }

    private static string FormatFloatSign(float? value)
    {
        return value.HasValue ? ((GetFloatBits(value.Value) >> 31) & 1u).ToString(CultureInfo.CurrentCulture) : "--";
    }

    private static string FormatFloatExponent(float? value)
    {
        return value.HasValue ? Convert.ToString((int)((GetFloatBits(value.Value) >> 23) & 0xFFu), 2).PadLeft(8, '0') : "--";
    }

    private static string FormatFloatMantissa(float? value)
    {
        return value.HasValue ? Convert.ToString((int)(GetFloatBits(value.Value) & 0x7FFFFFu), 2).PadLeft(23, '0') : "--";
    }

    private static string FormatXorMask(float? current, uint? previousBits)
    {
        if (!current.HasValue || !previousBits.HasValue)
        {
            return "--";
        }

        uint xor = GetFloatBits(current.Value) ^ previousBits.Value;
        return $"0x{xor:X8}";
    }

    private static int CountChangedBits(float? current, uint? previousBits)
    {
        if (!current.HasValue || !previousBits.HasValue)
        {
            return 0;
        }

        uint xor = GetFloatBits(current.Value) ^ previousBits.Value;
        return BitOperations.PopCount(xor);
    }

    private static string FormatRoundedInteger(float? value)
    {
        return value.HasValue ? Math.Round(value.Value).ToString("0", CultureInfo.CurrentCulture) : "--";
    }

    private static string FormatRoundedIntegerBinary(float? value)
    {
        if (!value.HasValue)
        {
            return "--";
        }

        long rounded = (long)Math.Round(value.Value);
        return Convert.ToString(rounded, 2);
    }

    private readonly record struct OscilloscopeWindow(DateTime Start, DateTime End);

    private sealed record OscilloscopeStatistics(
        bool HasData,
        float Current,
        float Minimum,
        float Maximum,
        float Average,
        float PeakToPeak,
        float Delta,
        float RatePerSecond);

    private sealed record OscilloscopeSample(DateTime Timestamp, float Value);

    private sealed class OscilloscopePanel : Panel
    {
        public OscilloscopePanel()
        {
            DoubleBuffered = true;
        }
    }

    private sealed class SensorSampleRow
    {
        public string Timestamp { get; set; } = "--";
        public string RawValue { get; set; } = "--";
        public string FormattedValue { get; set; } = "--";
        public string Min { get; set; } = "--";
        public string Max { get; set; } = "--";
        public string Delta { get; set; } = "--";
    }
}



