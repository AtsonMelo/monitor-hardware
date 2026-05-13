using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Reflection;
using System.Security.Principal;
using System.Windows.Forms;
using LibreHardwareMonitor.Hardware;

internal static class DashboardLayoutMetrics
{
    public const int CardNormalHeight = 128;
    public const int CardCompactHeight = 112;
    public const int BottomPanelHeight = 144;
    public const int BottomPanelCompactHeight = 108;
    public const int StatusBarHeight = 32;
}

class HardwareDashboardForm : Form
{
    private const int MinimumWindowWidth = 980;
    private const int MinimumWindowHeight = 720;
    private const int DefaultWindowWidth = 1200;
    private const int HeaderStackBreakpoint = 940;
    private const int CardsSingleColumnBreakpoint = 640;
    private const int CardsThreeColumnBreakpoint = 980;
    private const int CardsFiveColumnBreakpoint = 1320;
    private const int MainStackBreakpoint = 1220;
    private const int BottomStackBreakpoint = 820;
    private const int BottomHideBreakpointHeight = 780;

    private readonly AppConfig _config;
    private readonly SnapshotService _snapshotService;
    private readonly CsvLoggerService _csvLogger;
    private readonly UpdateService _updateService;
    private readonly StartupTaskService _startupTaskService;
    private readonly HardwareSelectionService _hardwareSelectionService;
    private readonly System.Windows.Forms.Timer _timer;
    private readonly Icon _windowIcon;
    private readonly ToolTip _headerToolTip = new ToolTip();

    private Label _statusLabel = null!;
    private Label _titleLabel = null!;
    private Label _scopeTitleLabel = null!;
    private Label _scopeSubtitleLabel = null!;
    private Label _sidebarTitleLabel = null!;
    private Label _rawTitleLabel = null!;
    private Label _bitsTitleLabel = null!;
    private Button _updateButton = null!;
    private Button _sensorsButton = null!;
    private Button _scopeButton = null!;
    private Button _hardwareSelectionButton = null!;
    private Button _sensorOriginsButton = null!;
    private Button _diagnosticAiButton = null!;
    private Button _stressTestButton = null!;
    private Button _errorReportButton = null!;
    private Button _helpButton = null!;
    private CheckBox _startupCheckBox = null!;
    private ContextMenuStrip _helpMenu = null!;
    private TableLayoutPanel _headerLayout = null!;
    private TableLayoutPanel _titleLayout = null!;
    private FlowLayoutPanel _actionsLayout = null!;
    private FlowLayoutPanel _headerButtonsPanel = null!;
    private TableLayoutPanel _cardsGrid = null!;
    private TableLayoutPanel _rootLayout = null!;
    private TableLayoutPanel _mainLayout = null!;
    private TableLayoutPanel _bottomLayout = null!;
    private Panel _scopeHost = null!;
    private DashboardScopePanel _scopePanel = null!;
    private FlowLayoutPanel _sensorSidebarPanel = null!;
    private Panel _rawDataPanel = null!;
    private Panel _bitsInspectorPanel = null!;
    private TextBox _rawDataTextBox = null!;
    private TextBox _bitsInspectorTextBox = null!;
    private MetricCard _cpuCard = null!;
    private MetricCard _temperatureCard = null!;
    private MetricCard _fanCard = null!;
    private MetricCard _voltageCard = null!;
    private MetricCard _diagnosticCard = null!;
    private readonly ShortTrendHistory _cpuTrendHistory = new ShortTrendHistory();
    private readonly ShortTrendHistory _temperatureTrendHistory = new ShortTrendHistory();
    private readonly ShortTrendHistory _fanTrendHistory = new ShortTrendHistory();
    private readonly ShortTrendHistory _voltageTrendHistory = new ShortTrendHistory();
    private readonly ShortTrendHistory _diagnosticTrendHistory = new ShortTrendHistory();
    private readonly RollingHistory _scopeHistory = new RollingHistory(180);
    private readonly HardwareMonitorService _hardwareMonitor;
    private List<SensorReading> _latestSensors = new();
    private MonitorSnapshot? _latestSnapshot;
    private SensorReading? _selectedSensor;
    private string? _selectedSensorKey;
    private string? _selectedSidebarKey;
    private readonly Dictionary<string, SensorSummaryRow> _sidebarRows = new(StringComparer.OrdinalIgnoreCase);
    private SensorsDetailsForm? _sensorsDetailsForm;
    private RawHardwareDataForm? _rawHardwareDataForm;
    private HardwareSelectionForm? _hardwareSelectionForm;
    private SensorOriginsForm? _sensorOriginsForm;
    private DiagnosticAiForm? _diagnosticAiForm;
    private HardwareStressTestForm? _stressTestForm;
    private DateTime? _lastUpdatedAt;
    private bool _headerIsStacked;
    private bool _cardsAreStacked;
    private bool _bottomDensityInitialized;
    private bool _bottomIsCompact;

    public HardwareDashboardForm(AppConfig config)
    {
        _config = config;
        _snapshotService = new SnapshotService(config);
        _hardwareMonitor = new HardwareMonitorService();
        _hardwareSelectionService = new HardwareSelectionService(_hardwareMonitor);
        _csvLogger = new CsvLoggerService();
        _updateService = new UpdateService();
        _startupTaskService = new StartupTaskService();
        _timer = new System.Windows.Forms.Timer
        {
            Interval = Math.Max(500, config.IntervaloMs)
        };

        Text = $"Monitor Hardware Versão {GetAppVersion()}";
        AutoScaleMode = AutoScaleMode.Dpi;
        _windowIcon = AppIconService.Load();
        Icon = _windowIcon;
        ShowInTaskbar = true;
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(MinimumWindowWidth, MinimumWindowHeight);
        Size = GetInitialWindowSize();
        ApplyNotebookWindowMode();
        AutoScroll = false;
        BackColor = Color.FromArgb(17, 19, 22);
        ForeColor = Color.White;
        Font = new Font("Segoe UI", 10, FontStyle.Regular, GraphicsUnit.Point);
        DoubleBuffered = true;

        BuildLayout();
        SizeChanged += (_, _) => ApplyResponsiveLayout();
        SetLoadingState();

        _timer.Tick += (_, _) => RefreshSnapshot();
        Shown += (_, _) =>
        {
            SetLoadingState();
            RefreshStartupState();
            _timer.Start();

            BeginInvoke(new Action(async () =>
            {
                await Task.Delay(300);

                RefreshSnapshot();

                if (_config.EnableAutoUpdateCheck)
                {
                    await CheckForUpdatesAsync(showUpToDate: false);
                }
            }));
        };
        FormClosing += HardwareDashboardFormClosing;

        FormClosed += (_, _) =>
        {
            _timer.Stop();
            _timer.Dispose();
            _helpMenu.Dispose();
            _headerToolTip.Dispose();
            _rawHardwareDataForm?.Dispose();
            _diagnosticAiForm?.Dispose();
            _stressTestForm?.Dispose();
            _hardwareMonitor.Dispose();
            _windowIcon.Dispose();
        };
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        WindowThemeService.ApplyNativeTitleBarTheme(Handle);
    }

    private void HardwareDashboardFormClosing(object? sender, FormClosingEventArgs e)
    {
        if (e.CloseReason != CloseReason.UserClosing)
        {
            return;
        }

        e.Cancel = true;
        Hide();
        ShowInTaskbar = false;
    }

    private void SetLoadingState()
    {
        _cpuCard.SetValues("...", "Carregando CPU...", Color.FromArgb(170, 176, 184), Array.Empty<float?>());
        _temperatureCard.SetValues("...", "Carregando temperaturas...", Color.FromArgb(170, 176, 184), Array.Empty<float?>());
        _fanCard.SetValues("...", "Carregando ventoinhas...", Color.FromArgb(170, 176, 184), Array.Empty<float?>());
        _voltageCard.SetValues("...", "Carregando voltagens...", Color.FromArgb(170, 176, 184), Array.Empty<float?>());
        _diagnosticCard.SetValues("...", "Aguardando diagnóstico...", Color.FromArgb(170, 176, 184), Array.Empty<float?>());

        _latestSensors = new List<SensorReading>();
        _latestSnapshot = null;
        _selectedSensor = null;
        _selectedSensorKey = null;
        _selectedSidebarKey = null;
        _scopeHistory.Clear();
        _scopePanel?.SetData("Coletando amostras...", "Selecione um sensor no painel lateral.", Color.FromArgb(78, 140, 255), Array.Empty<float?>(), null);
        if (_rawDataTextBox != null)
        {
            _rawDataTextBox.Text = "Selecione um sensor no painel lateral para ver os dados brutos.";
        }
        if (_bitsInspectorTextBox != null)
        {
            _bitsInspectorTextBox.Text = "Selecione um sensor no painel lateral para inspecionar os bits.";
        }
        foreach (SensorSummaryRow row in _sidebarRows.Values)
        {
            row.SetValues("--", "Aguardando leitura", Color.FromArgb(170, 176, 184), MetricIconKind.Sensor, null, false);
        }

        _lastUpdatedAt = null;
        _headerToolTip.SetToolTip(_titleLabel, "Inicializando monitoramento...");
        _statusLabel.Text = "Carregando sensores...";
        _statusLabel.ForeColor = Color.FromArgb(170, 176, 184);
    }

    private static Size GetInitialWindowSize()
    {
        Rectangle workingArea = Screen.PrimaryScreen?.WorkingArea ?? new Rectangle(0, 0, 1280, 720);

        int width = Math.Min(1180, Math.Max(DefaultWindowWidth, workingArea.Width - 40));
        int height = Math.Min(820, Math.Max(MinimumWindowHeight, workingArea.Height - 40));

        return new Size(width, height);
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

    private static int GetLayoutWidth(Control control)
    {
        return control.ClientSize.Width > 0
            ? control.ClientSize.Width
            : control.Width;
    }

    private void BuildLayout()
    {
        TableLayoutPanel root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            Padding = new Padding(10, 6, 10, 6),
            BackColor = BackColor
        };
        _rootLayout = root;

        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, DashboardLayoutMetrics.BottomPanelHeight));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, DashboardLayoutMetrics.StatusBarHeight));

        _headerLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = BackColor,
            Margin = new Padding(0),
            ColumnCount = 1,
            RowCount = 2
        };

        _headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        _headerLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _headerLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        _titleLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = BackColor,
            ColumnCount = 2,
            RowCount = 1,
            Margin = new Padding(0)
        };
        _titleLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        _titleLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        _titleLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        _titleLabel = new Label
        {
            Text = "Monitor Hardware",
            Dock = DockStyle.Fill,
            Height = 32,
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 18, FontStyle.Bold, GraphicsUnit.Point),
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = true,
            Margin = new Padding(0)
        };

        _scopeButton = new Button
        {
            Text = "Osciloscópio"
        };
        ConfigurePrimaryActionButton(_scopeButton);
        _scopeButton.Click += (_, _) => OpenScopeSelection();

        _headerButtonsPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = BackColor,
            Margin = new Padding(0),
            Padding = new Padding(0)
        };

        _helpButton = new Button { Text = "Menu" };
        ConfigureHelpButton(_helpButton);
        _helpButton.Click += (_, _) => ShowHelpMenu();
        _headerButtonsPanel.Controls.Add(_helpButton);
        _headerButtonsPanel.Controls.Add(_scopeButton);

        _titleLayout.Controls.Add(_titleLabel, 0, 0);
        _titleLayout.Controls.Add(_headerButtonsPanel, 1, 0);

        _actionsLayout = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Margin = new Padding(0),
            Padding = new Padding(0)
        };

        _updateButton = new Button { Text = "Verificar atualizações" };
        ConfigureActionButton(_updateButton);
        _updateButton.Click += async (_, _) => await CheckForUpdatesAsync(showUpToDate: true);

        _sensorsButton = new Button { Text = "Conferir sensores" };
        ConfigureActionButton(_sensorsButton);
        _sensorsButton.Click += (_, _) => OpenSensorsDetails();

        _hardwareSelectionButton = new Button { Text = "Selecionar hardwares" };
        ConfigureActionButton(_hardwareSelectionButton);
        _hardwareSelectionButton.Click += (_, _) => OpenHardwareSelection();

        _sensorOriginsButton = new Button { Text = "Origem dos sensores" };
        ConfigureActionButton(_sensorOriginsButton);
        _sensorOriginsButton.Click += (_, _) => OpenSensorOrigins();

        _diagnosticAiButton = new Button { Text = "Diagnóstico por IA" };
        ConfigureActionButton(_diagnosticAiButton);
        _diagnosticAiButton.Click += (_, _) => OpenDiagnosticAi();

        _stressTestButton = new Button { Text = "Teste de estresse" };
        ConfigureActionButton(_stressTestButton);
        _stressTestButton.Click += (_, _) => OpenStressTest();

        _errorReportButton = new Button { Text = "Relatório de erros" };
        ConfigureActionButton(_errorReportButton);
        _errorReportButton.Click += (_, _) => GenerateAndOpenErrorReport();

        _startupCheckBox = new CheckBox
        {
            Text = "Iniciar com o Windows",
            AutoSize = false,
            Dock = DockStyle.Top,
            Height = 30,
            MinimumSize = new Size(170, 30),
            ForeColor = Color.FromArgb(210, 214, 220),
            BackColor = BackColor,
            TextAlign = ContentAlignment.MiddleLeft,
            UseVisualStyleBackColor = false,
            Margin = new Padding(4, 1, 6, 1)
        };
        _startupCheckBox.CheckedChanged += StartupCheckBoxCheckedChanged;

        ConfigureActionChips();
        _helpMenu = BuildHelpMenu();

        _cardsGrid = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            GrowStyle = TableLayoutPanelGrowStyle.FixedSize,
            Padding = new Padding(0),
            BackColor = BackColor,
            Margin = new Padding(0, 0, 0, 4)
        };
        _cardsGrid.SizeChanged += (_, _) => ApplyCardsGridLayout();

        _cpuCard = new MetricCard("CPU", MetricIconKind.Cpu);
        _temperatureCard = new MetricCard("Temperatura", MetricIconKind.Temperature);
        _fanCard = new MetricCard("Ventoinha", MetricIconKind.Fan);
        _voltageCard = new MetricCard("Voltagem", MetricIconKind.Voltage);
        _diagnosticCard = new MetricCard("Diagnóstico", MetricIconKind.Diagnostic);

        _mainLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = BackColor,
            Margin = new Padding(0, 0, 0, 6)
        };
        _mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 76));
        _mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 24));
        _mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        _scopeHost = BuildScopeHost();
        _sensorSidebarPanel = BuildSensorSidebarPanel();
        _mainLayout.Controls.Add(_scopeHost, 0, 0);
        _mainLayout.Controls.Add(_sensorSidebarPanel, 1, 0);

        _bottomLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = BackColor,
            Margin = new Padding(0)
        };
        _bottomLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58));
        _bottomLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42));
        _bottomLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        _rawDataPanel = BuildRawDataPanel();
        _bitsInspectorPanel = BuildBitsInspectorPanel();
        _bottomLayout.Controls.Add(_rawDataPanel, 0, 0);
        _bottomLayout.Controls.Add(_bitsInspectorPanel, 1, 0);

        _statusLabel = new Label
        {
            Text = "Nenhum alerta crítico.",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = true,
            ForeColor = Color.FromArgb(170, 176, 184),
            Font = new Font("Segoe UI", 10, FontStyle.Bold, GraphicsUnit.Point),
            Margin = new Padding(0, 2, 0, 0)
        };

        root.Controls.Add(_headerLayout, 0, 0);
        root.Controls.Add(_cardsGrid, 0, 1);
        root.Controls.Add(_mainLayout, 0, 2);
        root.Controls.Add(_bottomLayout, 0, 3);
        root.Controls.Add(_statusLabel, 0, 4);

        _headerLayout.Controls.Add(_titleLayout, 0, 0);
        _headerLayout.Controls.Add(_actionsLayout, 0, 1);

        Controls.Add(root);
        ApplyResponsiveLayout();
    }

    private static void ConfigureActionButton(Button button)
    {
        button.AutoSize = true;
        button.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        button.Height = 32;
        button.MinimumSize = new Size(132, 32);
        button.Padding = new Padding(12, 0, 12, 0);
        button.FlatStyle = FlatStyle.Flat;
        button.ForeColor = Color.White;
        button.BackColor = Color.FromArgb(36, 41, 47);
        button.UseVisualStyleBackColor = false;
        button.TextAlign = ContentAlignment.MiddleCenter;
        button.FlatAppearance.BorderColor = Color.FromArgb(58, 66, 74);
        button.FlatAppearance.MouseOverBackColor = Color.FromArgb(44, 50, 57);
        button.FlatAppearance.MouseDownBackColor = Color.FromArgb(30, 34, 39);
        button.Margin = new Padding(0, 0, 6, 4);
    }

    private static void ConfigurePrimaryActionButton(Button button)
    {
        ConfigureActionButton(button);
        button.MinimumSize = new Size(170, 36);
        button.Height = 36;
        button.Font = new Font("Segoe UI", 10f, FontStyle.Bold, GraphicsUnit.Point);
        button.BackColor = Color.FromArgb(0, 120, 212);
        button.FlatAppearance.BorderColor = Color.FromArgb(55, 155, 255);
        button.FlatAppearance.MouseOverBackColor = Color.FromArgb(0, 132, 230);
        button.FlatAppearance.MouseDownBackColor = Color.FromArgb(0, 95, 170);
    }

    private static void ConfigureHelpButton(Button button)
    {
        button.AutoSize = false;
        button.Size = new Size(86, 36);
        button.MinimumSize = new Size(86, 36);
        button.FlatStyle = FlatStyle.Flat;
        button.ForeColor = Color.FromArgb(230, 233, 236);
        button.BackColor = Color.FromArgb(32, 37, 42);
        button.UseVisualStyleBackColor = false;
        button.TextAlign = ContentAlignment.MiddleCenter;
        button.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold, GraphicsUnit.Point);
        button.FlatAppearance.BorderColor = Color.FromArgb(58, 66, 74);
        button.FlatAppearance.MouseOverBackColor = Color.FromArgb(44, 50, 57);
        button.FlatAppearance.MouseDownBackColor = Color.FromArgb(30, 34, 39);
        button.Margin = new Padding(6, 0, 0, 0);
        button.Padding = new Padding(8, 0, 8, 0);
    }

    private void ConfigureActionChips()
    {
        _actionsLayout.SuspendLayout();
        _actionsLayout.Controls.Clear();

        _actionsLayout.Controls.Add(_sensorsButton);
        _actionsLayout.Controls.Add(_stressTestButton);
        _actionsLayout.Controls.Add(_updateButton);
        _actionsLayout.Controls.Add(_startupCheckBox);

        _actionsLayout.ResumeLayout(true);
    }

    private void ApplyResponsiveLayout()
    {
        if (_rootLayout == null || _headerLayout == null || _cardsGrid == null || _mainLayout == null || _bottomLayout == null)
        {
            return;
        }

        ApplyHeaderLayout();
        ApplyCardsGridLayout();
        ApplyMainLayout();
        ApplyBottomLayout();
        ApplyRootLayout();
        ApplySidebarSelectionState();
    }

    private void ApplyRootLayout()
    {
        if (_rootLayout == null)
        {
            return;
        }

        bool hideBottom = ClientSize.Height < BottomHideBreakpointHeight;
        bool stackBottom = GetLayoutWidth(this) < BottomStackBreakpoint;
        bool compactBottom = ClientSize.Height < 930 || stackBottom;
        int panelHeight = compactBottom ? DashboardLayoutMetrics.BottomPanelCompactHeight : DashboardLayoutMetrics.BottomPanelHeight;
        int bottomHeight = hideBottom
            ? 0
            : stackBottom
                ? (panelHeight * 2) + 6
                : panelHeight;

        _bottomLayout.Visible = !hideBottom;

        _rootLayout.SuspendLayout();
        _rootLayout.RowStyles.Clear();
        _rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        _rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, bottomHeight));
        _rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, DashboardLayoutMetrics.StatusBarHeight));
        _rootLayout.ResumeLayout(true);
    }

    private void ApplyHeaderLayout()
    {
        bool stackHeader = ShouldStackHeader();

        if (_headerIsStacked == stackHeader && _headerLayout.Controls.Count > 0)
        {
            return;
        }

        _headerIsStacked = stackHeader;

        _headerLayout.SuspendLayout();
        _headerLayout.Controls.Clear();

        _titleLayout.Margin = new Padding(0);
        _actionsLayout.Margin = new Padding(0);
        _actionsLayout.Width = stackHeader ? Math.Max(0, ClientSize.Width - 28) : _actionsLayout.Width;

        _headerLayout.RowStyles.Clear();
        _headerLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _headerLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _headerLayout.Controls.Add(_titleLayout, 0, 0);
        _headerLayout.Controls.Add(_actionsLayout, 0, 1);

        _headerLayout.ResumeLayout(true);
    }

    private bool ShouldStackHeader()
    {
        return GetLayoutWidth(this) < HeaderStackBreakpoint ||
               ClientSize.Height < 760;
    }

    private void ApplyCardsGridLayout()
    {
        if (GetLayoutWidth(_cardsGrid) <= 0)
        {
            return;
        }

        int layoutWidth = GetLayoutWidth(_cardsGrid);
        bool stackCards = layoutWidth < CardsSingleColumnBreakpoint;
        bool compactCards = stackCards || layoutWidth < 1480 || ClientSize.Height < 820;
        int cardRowHeight = compactCards ? DashboardLayoutMetrics.CardCompactHeight : DashboardLayoutMetrics.CardNormalHeight;
        int columns = stackCards
            ? 1
            : layoutWidth >= CardsFiveColumnBreakpoint
                ? 5
                : layoutWidth >= CardsThreeColumnBreakpoint
                    ? 3
                    : 2;

        int rowCount = (int)Math.Ceiling(5.0 / columns);
        bool layoutMatches =
            _cardsGrid.Controls.Count == 5 &&
            _cardsAreStacked == stackCards &&
            _cardsGrid.ColumnCount == columns &&
            _cardsGrid.RowStyles.Count == rowCount &&
            _cardsGrid.RowStyles.Cast<RowStyle>().All(rowStyle => Math.Abs(rowStyle.Height - cardRowHeight) < 0.5f);

        if (layoutMatches)
        {
            ApplyMetricCardDensity(compactCards);
            return;
        }

        _cardsAreStacked = stackCards;

        _cardsGrid.SuspendLayout();
        _cardsGrid.Controls.Clear();
        _cardsGrid.ColumnStyles.Clear();
        _cardsGrid.RowStyles.Clear();

        _cardsGrid.ColumnCount = columns;
        _cardsGrid.RowCount = rowCount;

        for (int column = 0; column < columns; column++)
        {
            _cardsGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / columns));
        }

        for (int row = 0; row < rowCount; row++)
        {
            _cardsGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, cardRowHeight));
        }

        MetricCard[] cards = { _cpuCard, _temperatureCard, _fanCard, _voltageCard, _diagnosticCard };

        for (int index = 0; index < cards.Length; index++)
        {
            int column = index % columns;
            int row = index / columns;
            AddMetricCard(cards[index], column, row, new Padding(3), isCompact: compactCards);
        }

        _cardsGrid.ResumeLayout(true);
    }

    private void AddMetricCard(MetricCard card, int column, int row, Padding margin, bool isCompact)
    {
        if (column >= _cardsGrid.ColumnCount || row >= _cardsGrid.RowCount)
        {
            return;
        }

        card.Dock = DockStyle.Fill;
        card.Margin = margin;
        card.SetCompactLayout(isCompact);
        _cardsGrid.Controls.Add(card, column, row);
    }

    private void ApplyMetricCardDensity(bool isCompact)
    {
        _cpuCard.SetCompactLayout(isCompact);
        _temperatureCard.SetCompactLayout(isCompact);
        _fanCard.SetCompactLayout(isCompact);
        _voltageCard.SetCompactLayout(isCompact);
        _diagnosticCard.SetCompactLayout(isCompact);
    }

    private void ApplyMainLayout()
    {
        bool stackMain = GetLayoutWidth(_mainLayout) < MainStackBreakpoint;

        _scopeHost.Margin = stackMain
            ? new Padding(0, 0, 0, 6)
            : new Padding(0, 0, 8, 0);
        _sensorSidebarPanel.MinimumSize = stackMain
            ? new Size(0, 0)
            : new Size(260, 0);

        if (_mainLayout.Controls.Count > 0 &&
            _mainLayout.ColumnCount == (stackMain ? 1 : 2))
        {
            return;
        }

        _mainLayout.SuspendLayout();
        _mainLayout.Controls.Clear();
        _mainLayout.ColumnStyles.Clear();
        _mainLayout.RowStyles.Clear();

        if (stackMain)
        {
            _mainLayout.ColumnCount = 1;
            _mainLayout.RowCount = 2;
            _mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            _mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 74));
            _mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 26));
            _mainLayout.Controls.Add(_scopeHost, 0, 0);
            _mainLayout.Controls.Add(_sensorSidebarPanel, 0, 1);
        }
        else
        {
            _mainLayout.ColumnCount = 2;
            _mainLayout.RowCount = 1;
            _mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 76));
            _mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 24));
            _mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            _mainLayout.Controls.Add(_scopeHost, 0, 0);
            _mainLayout.Controls.Add(_sensorSidebarPanel, 1, 0);
        }

        _mainLayout.ResumeLayout(true);
    }

    private void ApplyBottomLayout()
    {
        bool stackBottom = GetLayoutWidth(this) < BottomStackBreakpoint;
        bool compactBottom = ClientSize.Height < 930 || stackBottom;

        _rawDataPanel.Margin = stackBottom
            ? new Padding(0, 0, 0, 6)
            : new Padding(0, 0, 6, 0);
        _bitsInspectorPanel.Margin = new Padding(0);

        if (_bottomLayout.Controls.Count > 0 &&
            _bottomLayout.ColumnCount == (stackBottom ? 1 : 2))
        {
            ApplyBottomPanelDensity(compactBottom);
            return;
        }

        _bottomLayout.SuspendLayout();
        _bottomLayout.Controls.Clear();
        _bottomLayout.ColumnStyles.Clear();
        _bottomLayout.RowStyles.Clear();

        if (stackBottom)
        {
            _bottomLayout.ColumnCount = 1;
            _bottomLayout.RowCount = 2;
            _bottomLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            _bottomLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 58));
            _bottomLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 42));
            _bottomLayout.Controls.Add(_rawDataPanel, 0, 0);
            _bottomLayout.Controls.Add(_bitsInspectorPanel, 0, 1);
        }
        else
        {
            _bottomLayout.ColumnCount = 2;
            _bottomLayout.RowCount = 1;
            _bottomLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60));
            _bottomLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
            _bottomLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            _bottomLayout.Controls.Add(_rawDataPanel, 0, 0);
            _bottomLayout.Controls.Add(_bitsInspectorPanel, 1, 0);
        }

        _bottomLayout.ResumeLayout(true);
        ApplyBottomPanelDensity(compactBottom);
    }

    private void ApplyBottomPanelDensity(bool compact)
    {
        if (_bottomDensityInitialized && _bottomIsCompact == compact)
        {
            return;
        }

        _bottomDensityInitialized = true;
        _bottomIsCompact = compact;

        int panelHeight = compact ? DashboardLayoutMetrics.BottomPanelCompactHeight : DashboardLayoutMetrics.BottomPanelHeight;
        int contentHeight = Math.Max(0, panelHeight - 44);

        _rawDataPanel.MinimumSize = new Size(0, panelHeight);
        _bitsInspectorPanel.MinimumSize = new Size(0, panelHeight);
        _rawTitleLabel.Height = 24;
        _bitsTitleLabel.Height = 24;
        _rawDataTextBox.Font = new Font("Consolas", compact ? 8.5f : 9f, FontStyle.Regular, GraphicsUnit.Point);
        _bitsInspectorTextBox.Font = new Font("Consolas", compact ? 8.5f : 9f, FontStyle.Regular, GraphicsUnit.Point);
        _rawDataTextBox.MinimumSize = new Size(0, contentHeight);
        _bitsInspectorTextBox.MinimumSize = new Size(0, contentHeight);
    }

    private Panel BuildScopeHost()
    {
        TableLayoutPanel host = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = Color.FromArgb(21, 24, 28),
            Padding = new Padding(10),
            MinimumSize = new Size(0, 240),
            Margin = new Padding(0, 0, 8, 0)
        };

        host.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        host.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        host.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        host.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        _scopeTitleLabel = new Label
        {
            Text = "Scope principal",
            Dock = DockStyle.Fill,
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 13.5f, FontStyle.Bold, GraphicsUnit.Point),
            AutoEllipsis = true,
            Margin = new Padding(0)
        };

        _scopeSubtitleLabel = new Label
        {
            Text = "Coletando amostras...",
            Dock = DockStyle.Fill,
            ForeColor = Color.FromArgb(170, 176, 184),
            Font = new Font("Segoe UI", 9.25f, FontStyle.Regular, GraphicsUnit.Point),
            AutoEllipsis = true,
            Margin = new Padding(0, 0, 0, 8)
        };

        _scopePanel = new DashboardScopePanel
        {
            Dock = DockStyle.Fill,
            MinimumSize = new Size(0, 180),
            BackColor = Color.FromArgb(14, 16, 18)
        };

        host.Controls.Add(_scopeTitleLabel, 0, 0);
        host.Controls.Add(_scopeSubtitleLabel, 0, 1);
        host.Controls.Add(_scopePanel, 0, 2);
        return host;
    }

    private FlowLayoutPanel BuildSensorSidebarPanel()
    {
        FlowLayoutPanel panel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true,
            BackColor = Color.FromArgb(21, 24, 28),
            Padding = new Padding(10),
            MinimumSize = new Size(260, 0),
            Margin = new Padding(0, 0, 0, 0)
        };
        _sensorSidebarPanel = panel;

        _sidebarTitleLabel = new Label
        {
            Text = "Sensores principais",
            AutoSize = true,
            Height = 24,
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 12.5f, FontStyle.Bold, GraphicsUnit.Point),
            Margin = new Padding(0, 0, 0, 8)
        };
        panel.Controls.Add(_sidebarTitleLabel);

        AddSidebarRow("cpu-temp", "CPU temperatura", "--", "Aguardando leitura", MetricIconKind.Cpu);
        AddSidebarRow("cpu-load", "CPU uso", "--", "Aguardando leitura", MetricIconKind.Cpu);
        AddSidebarRow("gpu-temp", "GPU temperatura", "--", "Aguardando leitura", MetricIconKind.Gpu);
        AddSidebarRow("gpu-load", "GPU uso", "--", "Aguardando leitura", MetricIconKind.Gpu);
        AddSidebarRow("fan", "Ventoinha", "--", "Aguardando leitura", MetricIconKind.Fan);
        AddSidebarRow("voltage", "Voltagem", "--", "Aguardando leitura", MetricIconKind.Voltage);
        AddSidebarRow("ssd-temp", "SSD temperatura", "--", "Aguardando leitura", MetricIconKind.Storage);
        AddSidebarRow("ram", "RAM uso", "--", "Aguardando leitura", MetricIconKind.Memory);

        _hardwareSelectionButton = new Button
        {
            Text = "Selecionar hardware..."
        };
        ConfigureActionButton(_hardwareSelectionButton);
        _hardwareSelectionButton.Click += (_, _) => OpenHardwareSelection();
        _hardwareSelectionButton.Margin = new Padding(0, 8, 0, 0);
        panel.Controls.Add(_hardwareSelectionButton);

        return panel;
    }

    private Panel BuildRawDataPanel()
    {
        Panel panel = CreateDashboardSectionPanel();
        TableLayoutPanel content = CreateSectionContentPanel();

        _rawTitleLabel = BuildSectionTitle("Dados brutos");
        _rawDataTextBox = BuildMonospaceTextBox();

        content.Controls.Add(_rawTitleLabel, 0, 0);
        content.Controls.Add(_rawDataTextBox, 0, 1);
        panel.Controls.Add(content);
        return panel;
    }

    private Panel BuildBitsInspectorPanel()
    {
        Panel panel = CreateDashboardSectionPanel();
        TableLayoutPanel content = CreateSectionContentPanel();

        _bitsTitleLabel = BuildSectionTitle("Inspetor de bits");
        _bitsInspectorTextBox = BuildMonospaceTextBox();

        content.Controls.Add(_bitsTitleLabel, 0, 0);
        content.Controls.Add(_bitsInspectorTextBox, 0, 1);
        panel.Controls.Add(content);
        return panel;
    }

    private static Panel CreateDashboardSectionPanel()
    {
        return new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(21, 24, 28),
            MinimumSize = new Size(0, DashboardLayoutMetrics.BottomPanelHeight),
            Padding = new Padding(12),
            Margin = new Padding(0, 0, 0, 0)
        };
    }

    private static TableLayoutPanel CreateSectionContentPanel()
    {
        TableLayoutPanel content = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = Color.FromArgb(21, 24, 28),
            Margin = new Padding(0)
        };

        content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        content.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        content.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        return content;
    }

    private static Label BuildSectionTitle(string title)
    {
        return new Label
        {
            Text = title,
            Dock = DockStyle.Top,
            Height = 24,
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 11.5f, FontStyle.Bold, GraphicsUnit.Point),
            Margin = new Padding(0, 0, 0, 8),
            AutoEllipsis = true
        };
    }

    private static TextBox BuildMonospaceTextBox()
    {
        return new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = Color.FromArgb(14, 16, 18),
            ForeColor = Color.FromArgb(230, 234, 238),
            Font = new Font("Consolas", 9.25f, FontStyle.Regular, GraphicsUnit.Point),
            ShortcutsEnabled = false,
            WordWrap = false,
            MinimumSize = new Size(0, 96)
        };
    }

    private void AddSidebarRow(string key, string title, string value, string subtitle, MetricIconKind iconKind)
    {
        SensorSummaryRow row = new SensorSummaryRow(title, value, subtitle, iconKind)
        {
            Dock = DockStyle.Top,
            Margin = new Padding(0, 0, 0, 8)
        };

        row.Clicked += (_, _) =>
        {
            SelectSidebarRow(key, row.SourceSensor);
        };

        _sidebarRows[key] = row;
        _sensorSidebarPanel.Controls.Add(row);
        _sensorSidebarPanel.Controls.SetChildIndex(row, _sensorSidebarPanel.Controls.Count - 1);
    }

    private void SelectSidebarRow(string key, SensorReading? sourceSensor)
    {
        _selectedSidebarKey = key;
        _selectedSensor = sourceSensor;
        _selectedSensorKey = sourceSensor == null ? null : GetSensorKey(sourceSensor);
        _scopeHistory.Clear();
        ApplySidebarSelectionState();
        UpdateScopePanel();
        UpdateRawPanels();
    }

    private void ApplySidebarSelectionState()
    {
        foreach (KeyValuePair<string, SensorSummaryRow> entry in _sidebarRows)
        {
            entry.Value.SetSelected(string.Equals(entry.Key, _selectedSidebarKey, StringComparison.OrdinalIgnoreCase));
        }
    }

    private void OpenSensorsDetails()
    {
        try
        {
            if (_sensorsDetailsForm is { IsDisposed: false })
            {
                if (_sensorsDetailsForm.WindowState == FormWindowState.Minimized)
                {
                    _sensorsDetailsForm.WindowState = FormWindowState.Normal;
                }

                _sensorsDetailsForm.Show();
                _sensorsDetailsForm.Activate();
                return;
            }

            _sensorsDetailsForm = new SensorsDetailsForm(_hardwareMonitor, _hardwareSelectionService, _windowIcon);
            _sensorsDetailsForm.FormClosed += (_, _) => _sensorsDetailsForm = null;
            _sensorsDetailsForm.Show(this);
        }
        catch (Exception ex)
        {
            AppLogService.Error(ex, "Não foi possível abrir a tela de sensores.");

            MessageBox.Show(
                $"Não foi possível abrir a tela de sensores: {ex.Message}",
                "Monitor Hardware",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
    }

    private void OpenRawHardwareData()
    {
        try
        {
            if (_rawHardwareDataForm is { IsDisposed: false })
            {
                if (_rawHardwareDataForm.WindowState == FormWindowState.Minimized)
                {
                    _rawHardwareDataForm.WindowState = FormWindowState.Normal;
                }

                _rawHardwareDataForm.Show();
                _rawHardwareDataForm.Activate();
                return;
            }

            _rawHardwareDataForm = new RawHardwareDataForm(_hardwareMonitor, _hardwareSelectionService, _windowIcon);
            _rawHardwareDataForm.FormClosed += (_, _) => _rawHardwareDataForm = null;
            _rawHardwareDataForm.Show(this);
        }
        catch (Exception ex)
        {
            AppLogService.Error(ex, "Não foi possível abrir a tela de seleção de sensores.");

            MessageBox.Show(
                $"Não foi possível abrir a tela de seleção de sensores: {ex.Message}",
                "Monitor Hardware",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
    }

    private void OpenScopeSelection()
    {
        try
        {
            RawHardwareDataForm scopeSelectionForm = new RawHardwareDataForm(
                _hardwareMonitor,
                _hardwareSelectionService,
                _windowIcon,
                "Selecionar sensor para Scope");
            scopeSelectionForm.Show(this);
        }
        catch (Exception ex)
        {
            AppLogService.Error(ex, "Não foi possível abrir a tela do osciloscópio.");

            MessageBox.Show(
                $"Não foi possível abrir a tela do osciloscópio: {ex.Message}",
                "Monitor Hardware",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
    }

    private void OpenHardwareSelection()
    {
        try
        {
            if (_hardwareSelectionForm is { IsDisposed: false })
            {
                if (_hardwareSelectionForm.WindowState == FormWindowState.Minimized)
                {
                    _hardwareSelectionForm.WindowState = FormWindowState.Normal;
                }

                _hardwareSelectionForm.Show();
                _hardwareSelectionForm.Activate();
                return;
            }

            _hardwareSelectionForm = new HardwareSelectionForm(_hardwareSelectionService, _windowIcon);
            _hardwareSelectionForm.FormClosed += (_, _) => _hardwareSelectionForm = null;
            _hardwareSelectionForm.Show(this);
        }
        catch (Exception ex)
        {
            AppLogService.Error(ex, "Não foi possível abrir a tela de seleção de hardwares.");

            MessageBox.Show(
                $"Não foi possível abrir a tela de seleção de hardwares: {ex.Message}",
                "Monitor Hardware",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
    }

    private void OpenSensorOrigins()
    {
        try
        {
            if (_sensorOriginsForm is { IsDisposed: false })
            {
                if (_sensorOriginsForm.WindowState == FormWindowState.Minimized)
                {
                    _sensorOriginsForm.WindowState = FormWindowState.Normal;
                }

                _sensorOriginsForm.Show();
                _sensorOriginsForm.Activate();
                return;
            }

            _sensorOriginsForm = new SensorOriginsForm(_windowIcon);
            _sensorOriginsForm.FormClosed += (_, _) => _sensorOriginsForm = null;
            _sensorOriginsForm.Show(this);
        }
        catch (Exception ex)
        {
            AppLogService.Error(ex, "Não foi possível abrir a tela de origem dos sensores.");

            MessageBox.Show(
                $"Não foi possível abrir a tela de origem dos sensores: {ex.Message}",
                "Monitor Hardware",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
    }

    private void OpenDiagnosticAi()
    {
        try
        {
            List<SensorReading> sensors = _hardwareMonitor.ReadAllSensors();

            if (_diagnosticAiForm is { IsDisposed: false })
            {
                _diagnosticAiForm.RefreshSensors(sensors);

                if (_diagnosticAiForm.WindowState == FormWindowState.Minimized)
                {
                    _diagnosticAiForm.WindowState = FormWindowState.Normal;
                }

                _diagnosticAiForm.Show();
                _diagnosticAiForm.Activate();
                return;
            }

            _diagnosticAiForm = new DiagnosticAiForm(sensors, _windowIcon);
            _diagnosticAiForm.FormClosed += (_, _) => _diagnosticAiForm = null;
            _diagnosticAiForm.Show(this);
        }
        catch (Exception ex)
        {
            AppLogService.Error(ex, "Não foi possível abrir o diagnóstico por IA.");

            MessageBox.Show(
                $"Não foi possível abrir o diagnóstico por IA: {ex.Message}",
                "Monitor Hardware",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
    }

    private void OpenStressTest()
    {
        try
        {
            if (_stressTestForm is { IsDisposed: false })
            {
                if (_stressTestForm.WindowState == FormWindowState.Minimized)
                {
                    _stressTestForm.WindowState = FormWindowState.Normal;
                }

                _stressTestForm.Show();
                _stressTestForm.Activate();
                return;
            }

            _stressTestForm = new HardwareStressTestForm(_hardwareMonitor, _config, _windowIcon);
            _stressTestForm.FormClosed += (_, _) => _stressTestForm = null;
            _stressTestForm.Show(this);
        }
        catch (Exception ex)
        {
            AppLogService.Error(ex, "Não foi possível abrir o teste de estresse.");

            MessageBox.Show(
                $"Não foi possível abrir o teste de estresse: {ex.Message}",
                "Monitor Hardware",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
    }

    private void RefreshSnapshot()
    {
        try
        {
            List<SensorReading> sensors = _hardwareMonitor.ReadAllSensors();
            MonitorSnapshot snapshot = _snapshotService.Create(sensors);

            _latestSensors = sensors;
            _latestSnapshot = snapshot;

            if (_config.EnableCsv)
            {
                _csvLogger.Save(snapshot);
            }

            UpdateDashboardPanels(sensors, snapshot);
            _lastUpdatedAt = snapshot.Timestamp;
            _headerToolTip.SetToolTip(_titleLabel, $"Atualizado em {snapshot.Timestamp:dd/MM/yyyy HH:mm:ss}");
            _statusLabel.Text = GetStatusText(snapshot);
            _statusLabel.ForeColor = GetStatusColor(snapshot);
        }
        catch (Exception ex)
        {
            AppLogService.Error(ex, "Não foi possível atualizar o painel gráfico.");
            _statusLabel.Text = $"Não foi possível ler os sensores: {ex.Message}";
            _statusLabel.ForeColor = Color.FromArgb(255, 185, 0);
        }
    }

    private void UpdateDashboardPanels(List<SensorReading> sensors, MonitorSnapshot snapshot)
    {
        UpdateMetricCards(sensors, snapshot);
        UpdateSensorSidebar(sensors, snapshot);
        UpdateSelectedSensor(sensors);
        UpdateScopePanel();
        UpdateRawPanels();
    }

    private void UpdateMetricCards(List<SensorReading> sensors, MonitorSnapshot snapshot)
    {
        SensorReading? voltageSensor = FindVoltageSensor(sensors);
        float? cpuTrend = snapshot.CpuTemp ?? snapshot.CpuUso;
        float? temperatureTrend = GetDashboardTemperature(snapshot);
        float? fanTrend = GetDashboardFan(snapshot);
        float? voltageTrend = voltageSensor?.Value;
        int diagnosticSeverity = GetDiagnosticSeverity(snapshot, voltageSensor);

        _cpuTrendHistory.Add(cpuTrend);
        _temperatureTrendHistory.Add(temperatureTrend);
        _fanTrendHistory.Add(fanTrend);
        _voltageTrendHistory.Add(voltageTrend);
        _diagnosticTrendHistory.Add(diagnosticSeverity);

        _cpuCard.SetValues(GetCpuPrimaryText(snapshot), GetCpuSecondaryText(snapshot), GetCpuAccentColor(snapshot), _cpuTrendHistory.Values);
        _temperatureCard.SetValues(FormatTemperature(temperatureTrend), BuildTemperatureSubtitle(snapshot), GetTemperatureAccentColor(temperatureTrend), _temperatureTrendHistory.Values);
        _fanCard.SetValues(FormatFan(fanTrend), BuildFanSubtitle(snapshot), GetFanAccentColor(fanTrend), _fanTrendHistory.Values);
        _voltageCard.SetValues(FormatVoltage(voltageTrend), voltageSensor == null ? "Nenhum sensor de voltagem detectado" : $"{voltageSensor.HardwareName} · {voltageSensor.SensorName}", GetVoltageAccentColor(voltageTrend), _voltageTrendHistory.Values);
        _diagnosticCard.SetValues(diagnosticSeverity == 0 ? "OK" : diagnosticSeverity == 1 ? "Atenção" : "Crítico", BuildDiagnosticSubtitle(snapshot), GetDiagnosticAccentColor(diagnosticSeverity), _diagnosticTrendHistory.Values);
    }

    private void UpdateSensorSidebar(List<SensorReading> sensors, MonitorSnapshot snapshot)
    {
        UpdateSidebarRow("cpu-temp", FormatTemperature(snapshot.CpuTemp), BuildSensorMeta(FindCpuTemperatureSensor(sensors)), GetTemperatureAccentColor(snapshot.CpuTemp), MetricIconKind.Cpu, FindCpuTemperatureSensor(sensors));
        UpdateSidebarRow("cpu-load", FormatPercent(snapshot.CpuUso), BuildSensorMeta(FindCpuLoadSensor(sensors)), GetLoadColor(snapshot.CpuUso), MetricIconKind.Cpu, FindCpuLoadSensor(sensors));
        UpdateSidebarRow("gpu-temp", FormatTemperature(snapshot.GpuTemp), BuildSensorMeta(FindGpuTemperatureSensor(sensors)), GetTemperatureAccentColor(snapshot.GpuTemp), MetricIconKind.Gpu, FindGpuTemperatureSensor(sensors));
        UpdateSidebarRow("gpu-load", FormatPercent(snapshot.GpuUso), BuildSensorMeta(FindGpuLoadSensor(sensors)), GetLoadColor(snapshot.GpuUso), MetricIconKind.Gpu, FindGpuLoadSensor(sensors));
        UpdateSidebarRow("fan", FormatFan(GetDashboardFan(snapshot)), BuildSensorMeta(FindFanSensor(sensors)), GetFanAccentColor(GetDashboardFan(snapshot)), MetricIconKind.Fan, FindFanSensor(sensors));
        UpdateSidebarRow("voltage", FormatVoltage(FindVoltageSensor(sensors)?.Value), BuildSensorMeta(FindVoltageSensor(sensors)), GetVoltageAccentColor(FindVoltageSensor(sensors)?.Value), MetricIconKind.Voltage, FindVoltageSensor(sensors));
        UpdateSidebarRow("ssd-temp", FormatTemperature(snapshot.SsdTemp), BuildSensorMeta(FindStorageTemperatureSensor(sensors)), GetTemperatureAccentColor(snapshot.SsdTemp), MetricIconKind.Storage, FindStorageTemperatureSensor(sensors));
        UpdateSidebarRow("ram", FormatPercent(snapshot.RamUso), BuildSensorMeta(FindRamLoadSensor(sensors)), GetLoadColor(snapshot.RamUso), MetricIconKind.Memory, FindRamLoadSensor(sensors));

        if (string.IsNullOrWhiteSpace(_selectedSidebarKey) || !_sidebarRows.ContainsKey(_selectedSidebarKey))
        {
            _selectedSidebarKey = _sidebarRows.Keys.FirstOrDefault();
        }

        ApplySidebarSelectionState();
    }

    private void UpdateSidebarRow(string key, string value, string subtitle, Color accent, MetricIconKind iconKind, SensorReading? sourceSensor)
    {
        if (!_sidebarRows.TryGetValue(key, out SensorSummaryRow? row))
        {
            return;
        }

        row.SetValues(value, subtitle, accent, iconKind, sourceSensor, string.Equals(_selectedSidebarKey, key, StringComparison.OrdinalIgnoreCase));
    }

    private void UpdateSelectedSensor(List<SensorReading> sensors)
    {
        if (!string.IsNullOrWhiteSpace(_selectedSidebarKey) &&
            _sidebarRows.TryGetValue(_selectedSidebarKey, out SensorSummaryRow? sidebarRow) &&
            sidebarRow.SourceSensor != null)
        {
            _selectedSensor = sidebarRow.SourceSensor;
            _selectedSensorKey = GetSensorKey(sidebarRow.SourceSensor);
            return;
        }

        if (_selectedSensor != null)
        {
            string selectedKey = GetSensorKey(_selectedSensor);
            SensorReading? matched = sensors.FirstOrDefault(sensor => GetSensorKey(sensor) == selectedKey);

            if (matched != null)
            {
                _selectedSensor = matched;
                _selectedSensorKey = selectedKey;
                return;
            }
        }

        _selectedSensor = FindPreferredScopeSensor(sensors);
        _selectedSensorKey = _selectedSensor == null ? null : GetSensorKey(_selectedSensor);
        _scopeHistory.Clear();
    }

    private void UpdateScopePanel()
    {
        if (_selectedSensor == null || !_selectedSensor.Value.HasValue)
        {
            _scopeTitleLabel.Text = "Scope principal";
            _scopeSubtitleLabel.Text = "Selecione um sensor no painel lateral.";
            _scopePanel.SetData("Coletando amostras...", "Selecione um sensor no painel lateral.", Color.FromArgb(78, 140, 255), Array.Empty<float?>(), null);
            _rawDataTextBox.Text = "Selecione um sensor no painel lateral para ver os dados brutos.";
            _bitsInspectorTextBox.Text = "Selecione um sensor no painel lateral para inspecionar os bits.";
            return;
        }

        _scopeTitleLabel.Text = $"{_selectedSensor.SensorName}";
        _scopeSubtitleLabel.Text = $"{_selectedSensor.HardwareName} · {_selectedSensor.HardwareType} · {FormatSensorType(_selectedSensor.SensorType)}";
        _scopeHistory.Add(_selectedSensor.Value.Value);
        _scopePanel.SetData(
            _selectedSensor.SensorName,
            $"{_selectedSensor.HardwareName} · {_selectedSensor.HardwareType} · {FormatSensorType(_selectedSensor.SensorType)}",
            GetScopeAccent(_selectedSensor),
            _scopeHistory.Values,
            GetSensorUnit(_selectedSensor.SensorType));
    }

    private void UpdateRawPanels()
    {
        if (_selectedSensor == null)
        {
            return;
        }

        _rawDataTextBox.Text = BuildRawSensorText(_selectedSensor);
        _bitsInspectorTextBox.Text = BuildBitsInspectorText(_selectedSensor);
    }

    private static string BuildSensorMeta(SensorReading? sensor)
    {
        return sensor == null
            ? "Sem sensor correspondente"
            : $"{sensor.HardwareName} · {sensor.SensorName}";
    }

    private string GetCpuPrimaryText(MonitorSnapshot snapshot)
    {
        return snapshot.CpuTemp.HasValue
            ? FormatTemperature(snapshot.CpuTemp)
            : FormatPercent(snapshot.CpuUso);
    }

    private string GetCpuSecondaryText(MonitorSnapshot snapshot)
    {
        string usageText = $"Uso {FormatPercent(snapshot.CpuUso)} | Potência {FormatPower(snapshot.CpuPower)} | Fan {FormatFan(snapshot.CpuFan)}";

        return snapshot.CpuTemp.HasValue
            ? usageText
            : $"{GetCpuTemperatureUnavailableText()} | {usageText}";
    }

    private Color GetCpuAccentColor(MonitorSnapshot snapshot)
    {
        return snapshot.CpuTemp.HasValue
            ? GetTemperatureAccentColor(snapshot.CpuTemp)
            : GetLoadColor(snapshot.CpuUso);
    }

    private string BuildTemperatureSubtitle(MonitorSnapshot snapshot)
    {
        return $"CPU {FormatTemperature(snapshot.CpuTemp)} | GPU {FormatTemperature(snapshot.GpuTemp)} | SSD {FormatTemperature(snapshot.SsdTemp)}";
    }

    private string BuildFanSubtitle(MonitorSnapshot snapshot)
    {
        return $"CPU {FormatFan(snapshot.CpuFan)} | GPU {FormatFan(snapshot.GpuFan)}";
    }

    private string BuildDiagnosticSubtitle(MonitorSnapshot snapshot)
    {
        return $"CPU {FormatTemperature(snapshot.CpuTemp)} | GPU {FormatTemperature(snapshot.GpuTemp)} | SSD {FormatTemperature(snapshot.SsdTemp)}";
    }

    private static string BuildRawSensorText(SensorReading sensor)
    {
        return
            $"Hardware: {sensor.HardwareName}\r\n" +
            $"Tipo hardware: {sensor.HardwareType}\r\n" +
            $"Sensor: {sensor.SensorName}\r\n" +
            $"Tipo sensor: {sensor.SensorType}\r\n" +
            $"Valor: {FormatRawValue(sensor.Value)}\r\n" +
            $"Mínimo: {FormatRawValue(sensor.Min)}\r\n" +
            $"Máximo: {FormatRawValue(sensor.Max)}\r\n" +
            $"Identificador sensor: {GetDisplayText(sensor.SensorIdentifier)}\r\n" +
            $"Identificador hardware: {GetDisplayText(sensor.HardwareIdentifier)}";
    }

    private static string BuildBitsInspectorText(SensorReading sensor)
    {
        if (!sensor.Value.HasValue)
        {
            return "O sensor selecionado não possui valor numérico no momento.";
        }

        uint bits = BitConverter.ToUInt32(BitConverter.GetBytes(sensor.Value.Value), 0);
        string binary = Convert.ToString((long)bits, 2).PadLeft(32, '0');
        string sign = ((bits >> 31) & 1u).ToString();
        string exponent = Convert.ToString((int)((bits >> 23) & 0xFFu), 2).PadLeft(8, '0');
        string mantissa = Convert.ToString((int)(bits & 0x7FFFFFu), 2).PadLeft(23, '0');

        return
            $"Float32 HEX: 0x{bits:X8}\r\n" +
            $"Float32 binário: {binary}\r\n" +
            $"Sinal: {sign}\r\n" +
            $"Expoente: {exponent}\r\n" +
            $"Mantissa: {mantissa}\r\n" +
            $"Valor arredondado: {sensor.Value.Value:0.000}";
    }

    private static string FormatRawValue(float? value)
    {
        return value.HasValue
            ? value.Value.ToString("0.###")
            : "--";
    }

    private static string GetDisplayText(string? text)
    {
        return string.IsNullOrWhiteSpace(text) ? "--" : text;
    }

    private static string FormatSensorType(LibreHardwareMonitor.Hardware.SensorType sensorType)
    {
        return sensorType switch
        {
            LibreHardwareMonitor.Hardware.SensorType.Temperature => "temperatura",
            LibreHardwareMonitor.Hardware.SensorType.Load => "uso",
            LibreHardwareMonitor.Hardware.SensorType.Fan => "ventoinha",
            LibreHardwareMonitor.Hardware.SensorType.Power => "potência",
            LibreHardwareMonitor.Hardware.SensorType.Voltage => "voltagem",
            LibreHardwareMonitor.Hardware.SensorType.Clock => "clock",
            _ => sensorType.ToString()
        };
    }

    private static string GetSensorUnit(LibreHardwareMonitor.Hardware.SensorType sensorType)
    {
        return sensorType switch
        {
            LibreHardwareMonitor.Hardware.SensorType.Temperature => "°C",
            LibreHardwareMonitor.Hardware.SensorType.Load => "%",
            LibreHardwareMonitor.Hardware.SensorType.Fan => "RPM",
            LibreHardwareMonitor.Hardware.SensorType.Power => "W",
            LibreHardwareMonitor.Hardware.SensorType.Voltage => "V",
            LibreHardwareMonitor.Hardware.SensorType.Clock => "MHz",
            LibreHardwareMonitor.Hardware.SensorType.Data => "GB",
            LibreHardwareMonitor.Hardware.SensorType.SmallData => "MB",
            _ => "--"
        };
    }

    private static Color GetScopeAccent(SensorReading sensor)
    {
        return sensor.SensorType switch
        {
            LibreHardwareMonitor.Hardware.SensorType.Temperature => GetTemperatureAccentColor(sensor.Value),
            LibreHardwareMonitor.Hardware.SensorType.Load => GetLoadColor(sensor.Value),
            LibreHardwareMonitor.Hardware.SensorType.Fan => GetFanAccentColor(sensor.Value),
            LibreHardwareMonitor.Hardware.SensorType.Voltage => GetVoltageAccentColor(sensor.Value),
            LibreHardwareMonitor.Hardware.SensorType.Power => Color.FromArgb(255, 185, 0),
            _ => Color.FromArgb(78, 140, 255)
        };
    }

    private static string GetSensorKey(SensorReading reading)
    {
        return !string.IsNullOrWhiteSpace(reading.SensorIdentifier)
            ? reading.SensorIdentifier
            : $"{reading.HardwareType}|{reading.HardwareName}|{reading.SensorType}|{reading.SensorName}";
    }

    private static SensorReading? FindPreferredScopeSensor(List<SensorReading> sensors)
    {
        return FindCpuTemperatureSensor(sensors)
               ?? FindCpuLoadSensor(sensors)
               ?? FindGpuTemperatureSensor(sensors)
               ?? FindGpuLoadSensor(sensors)
               ?? FindFanSensor(sensors)
               ?? FindVoltageSensor(sensors)
               ?? FindStorageTemperatureSensor(sensors)
               ?? FindRamLoadSensor(sensors);
    }

    private static SensorReading? FindCpuTemperatureSensor(List<SensorReading> sensors)
    {
        return sensors
            .Where(sensor => sensor.HardwareType == LibreHardwareMonitor.Hardware.HardwareType.Cpu &&
                             sensor.SensorType == LibreHardwareMonitor.Hardware.SensorType.Temperature &&
                             sensor.Value.HasValue)
            .OrderByDescending(sensor => sensor.Value)
            .FirstOrDefault();
    }

    private static SensorReading? FindCpuLoadSensor(List<SensorReading> sensors)
    {
        return sensors.FirstOrDefault(sensor =>
            sensor.HardwareType == LibreHardwareMonitor.Hardware.HardwareType.Cpu &&
            sensor.SensorType == LibreHardwareMonitor.Hardware.SensorType.Load &&
            sensor.Value.HasValue);
    }

    private static SensorReading? FindGpuTemperatureSensor(List<SensorReading> sensors)
    {
        return sensors
            .Where(sensor => sensor.HardwareType.ToString().StartsWith("Gpu", StringComparison.OrdinalIgnoreCase) &&
                             sensor.SensorType == LibreHardwareMonitor.Hardware.SensorType.Temperature &&
                             sensor.Value.HasValue)
            .OrderByDescending(sensor => sensor.Value)
            .FirstOrDefault();
    }

    private static SensorReading? FindGpuLoadSensor(List<SensorReading> sensors)
    {
        return sensors.FirstOrDefault(sensor =>
            sensor.HardwareType.ToString().StartsWith("Gpu", StringComparison.OrdinalIgnoreCase) &&
            sensor.SensorType == LibreHardwareMonitor.Hardware.SensorType.Load &&
            sensor.Value.HasValue);
    }

    private static SensorReading? FindFanSensor(List<SensorReading> sensors)
    {
        return sensors
            .Where(sensor => sensor.SensorType == LibreHardwareMonitor.Hardware.SensorType.Fan && sensor.Value.HasValue)
            .OrderByDescending(sensor => sensor.Value)
            .FirstOrDefault();
    }

    private static SensorReading? FindVoltageSensor(List<SensorReading> sensors)
    {
        return sensors
            .Where(sensor => sensor.SensorType == LibreHardwareMonitor.Hardware.SensorType.Voltage && sensor.Value.HasValue)
            .OrderByDescending(sensor => sensor.Value)
            .FirstOrDefault();
    }

    private static SensorReading? FindStorageTemperatureSensor(List<SensorReading> sensors)
    {
        return sensors.FirstOrDefault(sensor =>
            sensor.HardwareType == LibreHardwareMonitor.Hardware.HardwareType.Storage &&
            sensor.SensorType == LibreHardwareMonitor.Hardware.SensorType.Temperature &&
            sensor.Value.HasValue);
    }

    private static SensorReading? FindRamLoadSensor(List<SensorReading> sensors)
    {
        return sensors.FirstOrDefault(sensor =>
            sensor.HardwareName.Equals("Total Memory", StringComparison.OrdinalIgnoreCase) &&
            sensor.SensorType == LibreHardwareMonitor.Hardware.SensorType.Load &&
            sensor.SensorName.Equals("Memory", StringComparison.OrdinalIgnoreCase) &&
            sensor.Value.HasValue);
    }

    private static float? GetDashboardTemperature(MonitorSnapshot snapshot)
    {
        float?[] values = { snapshot.CpuTemp, snapshot.GpuTemp, snapshot.SsdTemp };
        List<float> sampleValues = values.Where(value => value.HasValue).Select(value => value!.Value).ToList();
        return sampleValues.Count == 0 ? null : sampleValues.Max();
    }

    private static float? GetDashboardFan(MonitorSnapshot snapshot)
    {
        float?[] values = { snapshot.CpuFan, snapshot.GpuFan };
        List<float> sampleValues = values.Where(value => value.HasValue).Select(value => value!.Value).ToList();
        return sampleValues.Count == 0 ? null : sampleValues.Max();
    }

    private static string FormatVoltage(float? value)
    {
        return value.HasValue
            ? $"{value.Value:0.00} V"
            : "-- V";
    }

    private static Color GetTemperatureAccentColor(float? value)
    {
        if (!value.HasValue)
        {
            return Color.FromArgb(170, 176, 184);
        }

        if (value.Value >= 85)
        {
            return Color.FromArgb(255, 80, 80);
        }

        if (value.Value >= 70)
        {
            return Color.FromArgb(255, 185, 0);
        }

        return Color.FromArgb(67, 190, 255);
    }

    private static Color GetFanAccentColor(float? value)
    {
        return value.HasValue
            ? Color.FromArgb(67, 220, 230)
            : Color.FromArgb(170, 176, 184);
    }

    private static Color GetVoltageAccentColor(float? value)
    {
        return value.HasValue
            ? Color.FromArgb(255, 185, 0)
            : Color.FromArgb(170, 176, 184);
    }

    private static Color GetDiagnosticAccentColor(int severity)
    {
        return severity switch
        {
            2 => Color.FromArgb(255, 80, 80),
            1 => Color.FromArgb(255, 185, 0),
            _ => Color.FromArgb(126, 211, 33)
        };
    }

    private static int GetDiagnosticSeverity(MonitorSnapshot snapshot, SensorReading? voltageSensor)
    {
        bool critical =
            (snapshot.CpuTemp.HasValue && snapshot.CpuTemp.Value >= 90) ||
            (snapshot.GpuTemp.HasValue && snapshot.GpuTemp.Value >= 90) ||
            (snapshot.SsdTemp.HasValue && snapshot.SsdTemp.Value >= 80);

        if (critical)
        {
            return 2;
        }

        bool warning =
            (snapshot.CpuTemp.HasValue && snapshot.CpuTemp.Value >= 75) ||
            (snapshot.GpuTemp.HasValue && snapshot.GpuTemp.Value >= 75) ||
            (snapshot.SsdTemp.HasValue && snapshot.SsdTemp.Value >= 70) ||
            voltageSensor == null;

        return warning ? 1 : 0;
    }

    private string GetStatusText(MonitorSnapshot snapshot)
    {
        List<string> alerts = new List<string>();

        if (snapshot.CpuTemp >= _config.CpuTempMax)
        {
            alerts.Add("CPU acima do limite");
        }

        if (snapshot.GpuTemp >= _config.GpuTempMax)
        {
            alerts.Add("GPU acima do limite");
        }

        if (snapshot.SsdTemp >= _config.SsdTempMax)
        {
            alerts.Add("SSD acima do limite");
        }

        return alerts.Count == 0
            ? "Nenhum alerta crítico."
            : string.Join(" | ", alerts);
    }

    private static Color GetStatusColor(MonitorSnapshot snapshot)
    {
        return snapshot.CpuTemp >= 90 ||
               snapshot.GpuTemp >= 90 ||
               snapshot.SsdTemp >= 80
            ? Color.FromArgb(255, 80, 80)
            : Color.FromArgb(170, 176, 184);
    }

    private async Task CheckForUpdatesAsync(bool showUpToDate)
    {
        try
        {
            _updateButton.Enabled = false;
            _updateButton.Text = "Verificando...";

            UpdateCheckResult result = await _updateService.CheckForUpdatesAsync();

            if (result.HasUpdate)
            {
                DialogResult dialogResult = MessageBox.Show(
                    $"Existe uma nova versão disponível: {result.LatestVersion}. Versão atual: {result.CurrentVersion}.\n\n" +
                    $"Deseja baixar e aplicar a atualização agora?\n\n" +
                    $"A atualização será aplicada na pasta atual do aplicativo:\n{AppContext.BaseDirectory}\n\n" +
                    $"Se você abriu o app por uma pasta de teste, o atalho da Área de Trabalho pode continuar apontando para outra instalação.\n\n" +
                    $"O Monitor Hardware será fechado e reaberto automaticamente.",
                    "Atualização disponível",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Information);

                if (dialogResult == DialogResult.Yes)
                {
                    Progress<string> progress = new Progress<string>(message => _updateButton.Text = message);

                    try
                    {
                                                await _updateService.StartUpdateAsync(result, progress);

                        string? downloadedExecutable = TryFindDownloadedUpdateExecutable(result.LatestVersion);

                        if (!string.IsNullOrWhiteSpace(downloadedExecutable))
                        {
                            AppLogService.Info($"Atualização baixada. Abrindo executável extraído: {downloadedExecutable}");

                            MessageBox.Show(
                                $"Atualização baixada com sucesso.\n\nO Monitor Hardware será reaberto pela versão baixada em:\n{downloadedExecutable}",
                                "Atualização baixada",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information);

                            Process.Start(new ProcessStartInfo
                            {
                                FileName = downloadedExecutable,
                                Arguments = "--gui",
                                UseShellExecute = true
                            });

                            Application.Exit();
                        }
                        else
                        {
                            AppLogService.Info($"Atualização baixada, mas executável extraído não foi encontrado. Pasta atual: {AppContext.BaseDirectory}");

                            MessageBox.Show(
                                "A atualização foi baixada, mas o executável extraído não foi localizado.\n\nAbra a pasta de atualizações em AppData\\Local\\MonitorHardware\\updates e execute manualmente a versão mais recente.",
                                "Atualização baixada",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Warning);
                        }
                    }
                    catch (Exception updateEx)
                    {
                        AppLogService.Error(updateEx, "Não foi possível aplicar a atualização automática.");
                        MessageBox.Show(
                            $"Não foi possível aplicar a atualização automática.\n\n" +
                            $"Versão atual: {result.CurrentVersion}\n" +
                            $"Versão disponível: {result.LatestVersion}\n\n" +
                            $"Detalhe: {updateEx.Message}\n\n" +
                            $"A página da release será aberta como fallback.",
                            "Monitor Hardware",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning);

                        OpenUrl(string.IsNullOrWhiteSpace(result.ReleaseUrl)
                            ? "https://github.com/AtsonMelo/monitor-hardware/releases/latest"
                            : result.ReleaseUrl);
                    }
                }
            }
            else if (showUpToDate)
            {
                MessageBox.Show(
                    $"Você já está usando a versão mais recente: {result.CurrentVersion}.",
                    "Monitor Hardware",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
        }
        catch (Exception ex)
        {
            string fallbackUrl = "https://github.com/AtsonMelo/monitor-hardware/releases/latest";
            MessageBox.Show(
                $"Não foi possível verificar atualizações.\n\n" +
                $"Versão atual: {_updateService.CurrentVersion}\n\n" +
                $"Detalhe: {ex.Message}\n\n" +
                $"A página de releases será aberta como fallback.",
                "Monitor Hardware",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);

            OpenUrl(fallbackUrl);
        }
        finally
        {
            _updateButton.Text = "Verificar atualizações";
            _updateButton.Enabled = true;
        }
    }

    private static string? TryFindDownloadedUpdateExecutable(object? latestVersion)
    {
        string versionText = Convert.ToString(latestVersion) ?? string.Empty;
        versionText = versionText.Trim();

        if (versionText.StartsWith("v", StringComparison.OrdinalIgnoreCase))
        {
            versionText = versionText[1..];
        }

        if (string.IsNullOrWhiteSpace(versionText))
        {
            return null;
        }

        string updateExecutablePath = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MonitorHardware",
            "updates",
            versionText,
            "extracted",
            "monitor-hardware.exe");

        return System.IO.File.Exists(updateExecutablePath)
            ? updateExecutablePath
            : null;
    }
    private ContextMenuStrip BuildHelpMenu()
    {
        ContextMenuStrip menu = new ContextMenuStrip();

        ToolStripMenuItem openSensorsItem = new ToolStripMenuItem("Conferir todos os sensores");
        openSensorsItem.Click += (_, _) => OpenSensorsDetails();

        ToolStripMenuItem openHardwareSelectionItem = new ToolStripMenuItem("Selecionar hardwares");
        openHardwareSelectionItem.Click += (_, _) => OpenHardwareSelection();

        ToolStripMenuItem openSensorOriginsItem = new ToolStripMenuItem("Origem dos sensores");
        openSensorOriginsItem.Click += (_, _) => OpenSensorOrigins();

        ToolStripMenuItem openDiagnosticAiItem = new ToolStripMenuItem("Diagnóstico por IA");
        openDiagnosticAiItem.Click += (_, _) => OpenDiagnosticAi();

        ToolStripMenuItem openSupportItem = new ToolStripMenuItem("Abrir suporte no GitHub");
        openSupportItem.Click += (_, _) => OpenUrl("https://github.com/AtsonMelo/monitor-hardware/issues/new/choose");

        ToolStripMenuItem openLogItem = new ToolStripMenuItem("Abrir log de erros");
        openLogItem.Click += (_, _) => OpenLogFile();

        ToolStripMenuItem openLogFolderItem = new ToolStripMenuItem("Abrir pasta de logs");
        openLogFolderItem.Click += (_, _) => OpenFolder(Path.GetDirectoryName(AppLogService.LogPath) ?? ".");

        ToolStripMenuItem createReportItem = new ToolStripMenuItem("Gerar relatório de erros");
        createReportItem.Click += (_, _) => GenerateAndOpenErrorReport();

        menu.Items.Add(openSensorsItem);
        menu.Items.Add(openHardwareSelectionItem);
        menu.Items.Add(openSensorOriginsItem);
        menu.Items.Add(openDiagnosticAiItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(createReportItem);
        menu.Items.Add(openLogItem);
        menu.Items.Add(openLogFolderItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(openSupportItem);

        return menu;
    }

    private void ShowHelpMenu()
    {
        _helpMenu.Show(_helpButton, new Point(0, _helpButton.Height));
    }

    private void GenerateAndOpenErrorReport()
    {
        ErrorReportResult? errorReport = null;
        TechnicalReportResult? technicalReport = null;
        SanitizedReportResult? sanitizedErrorReport = null;
        SanitizedReportResult? sanitizedTechnicalReport = null;

        try
        {
            _errorReportButton.Enabled = false;
            _errorReportButton.Text = "Coletando...";

            errorReport = ErrorReportService.Create(_config);
            technicalReport = TechnicalReportService.Create(_config);

            sanitizedErrorReport = ReportSanitizerService.CreateSanitizedCopy(errorReport.ReportPath);
            sanitizedTechnicalReport = ReportSanitizerService.CreateSanitizedCopy(technicalReport.ReportPath);

            string reportsFolder = Path.GetDirectoryName(sanitizedErrorReport.SanitizedPath)
                ?? AppLogService.LogDirectory;

            DialogResult dialogResult = MessageBox.Show(
                $"Relatórios gerados com sucesso.\n\n" +
                $"Arquivos recomendados para anexar no GitHub:\n\n" +
                $"{sanitizedErrorReport.SanitizedPath}\n\n" +
                $"{sanitizedTechnicalReport.SanitizedPath}\n\n" +
                $"Os arquivos originais também foram mantidos localmente, mas para Issue pública use preferencialmente os arquivos .sanitized.txt.\n\n" +
                $"Deseja abrir o GitHub e a pasta dos relatórios agora?",
                "Relatório de erros",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Information);

            if (dialogResult == DialogResult.Yes)
            {
                OpenFolder(reportsFolder);
                OpenUrl(errorReport.GitHubUrl);
            }
        }
        catch (Exception ex)
        {
            AppLogService.Error(ex, "Não foi possível gerar relatório de erros.");

            string errorReportPathText = errorReport != null
                ? $"\n\nRelatório de erros salvo em:\n{errorReport.ReportPath}"
                : "";

            string technicalReportPathText = technicalReport != null
                ? $"\n\nRelatório técnico salvo em:\n{technicalReport.ReportPath}"
                : "";

            string sanitizedErrorPathText = sanitizedErrorReport != null
                ? $"\n\nRelatório de erros sanitizado salvo em:\n{sanitizedErrorReport.SanitizedPath}"
                : "";

            string sanitizedTechnicalPathText = sanitizedTechnicalReport != null
                ? $"\n\nRelatório técnico sanitizado salvo em:\n{sanitizedTechnicalReport.SanitizedPath}"
                : "";

            MessageBox.Show(
                $"Não foi possível concluir a geração dos relatórios: {ex.Message}" +
                errorReportPathText +
                technicalReportPathText +
                sanitizedErrorPathText +
                sanitizedTechnicalPathText,
                "Monitor Hardware",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
        finally
        {
            _errorReportButton.Text = "Relatório de erros";
            _errorReportButton.Enabled = true;
        }
    }

    private static void OpenLogFile()
    {
        string logPath = AppLogService.LogPath;
        string? logDirectory = Path.GetDirectoryName(logPath);

        if (!string.IsNullOrWhiteSpace(logDirectory))
        {
            Directory.CreateDirectory(logDirectory);
        }

        if (!File.Exists(logPath))
        {
            File.WriteAllText(logPath, "Nenhum erro registrado ate agora." + Environment.NewLine);
        }

        OpenFile(logPath);
    }

    private static void OpenFolder(string folderPath)
    {
        Directory.CreateDirectory(folderPath);
        OpenFile(Path.GetFullPath(folderPath));
    }

    private static void OpenUrl(string url)
    {
        OpenFile(url);
    }

    private static void OpenFile(string pathOrUrl)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = pathOrUrl,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Não foi possível abrir o recurso solicitado: {ex.Message}",
                "Monitor Hardware",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
    }

    private void RefreshStartupState()
    {
        _startupCheckBox.CheckedChanged -= StartupCheckBoxCheckedChanged;
        _startupCheckBox.Checked = _startupTaskService.IsEnabled();
        _startupCheckBox.CheckedChanged += StartupCheckBoxCheckedChanged;
    }

    private void StartupCheckBoxCheckedChanged(object? sender, EventArgs eventArgs)
    {
        SetStartupFromCheckbox();
    }

    private void SetStartupFromCheckbox()
    {
        try
        {
            _startupTaskService.SetEnabled(_startupCheckBox.Checked);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Não foi possível alterar a inicialização automática: {ex.Message}",
                "Monitor Hardware",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);

            RefreshStartupState();
        }
    }

    private string FormatTemperature(float? celsius)
    {
        return celsius.HasValue
            ? $"{GetDisplayTemperature(celsius.Value):0} °{GetTemperatureUnit()}"
            : $"-- °{GetTemperatureUnit()}";
    }

    private float GetDisplayTemperature(float celsius)
    {
        return GetTemperatureUnit() == "F"
            ? celsius * 9 / 5 + 32
            : celsius;
    }

    private string GetTemperatureUnit()
    {
        return string.Equals(_config.TemperatureUnit, "F", StringComparison.OrdinalIgnoreCase)
            ? "F"
            : "C";
    }

    private static string FormatPercent(float? value)
    {
        return value.HasValue
            ? $"{value.Value:0}%"
            : "--%";
    }

    private static string FormatPower(float? value)
    {
        return value.HasValue
            ? $"{value.Value:0.0} W"
            : "-- W";
    }

    private static string FormatFan(float? value)
    {
        return value.HasValue
            ? $"{value.Value:0} RPM"
            : "não disponível";
    }

    private static string FormatMemory(float? value)
    {
        return value.HasValue
            ? $"{value.Value:0.0} GB"
            : "-- GB";
    }

    private static string FormatMemoryNumber(float? value)
    {
        return value.HasValue
            ? $"{value.Value:0.0}"
            : "--";
    }

    private static string GetCpuTemperatureUnavailableText()
    {
        return IsRunningAsAdministrator()
            ? "Temperatura indisponível pelo sensor atual"
            : "Temperatura indisponível; execute como administrador";
    }

    private static string GetAppVersion()
    {
        string version = Assembly.GetExecutingAssembly()
                             .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                             ?.InformationalVersion
                         ?? Assembly.GetExecutingAssembly().GetName().Version?.ToString()
                         ?? "desconhecida";

        int metadataIndex = version.IndexOf('+');

        if (metadataIndex > 0)
        {
            version = version[..metadataIndex];
        }

        return version;
    }

    private static bool IsRunningAsAdministrator()
    {
        using WindowsIdentity identity = WindowsIdentity.GetCurrent();
        WindowsPrincipal principal = new WindowsPrincipal(identity);

        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }

    private Color GetTemperatureColor(float? value, float max)
    {
        if (!value.HasValue)
        {
            return Color.FromArgb(170, 176, 184);
        }

        if (value.Value >= max)
        {
            return Color.FromArgb(255, 80, 80);
        }

        if (value.Value >= max - 10)
        {
            return Color.FromArgb(255, 185, 0);
        }

        return SystemColors.Highlight;
    }

    private static Color GetLoadColor(float? value)
    {
        if (!value.HasValue)
        {
            return Color.FromArgb(170, 176, 184);
        }

        if (value.Value >= 90)
        {
            return Color.FromArgb(255, 80, 80);
        }

        if (value.Value >= 75)
        {
            return Color.FromArgb(255, 185, 0);
        }

        return SystemColors.Highlight;
    }
}

enum MetricIconKind
{
    Cpu,
    Temperature,
    Fan,
    Voltage,
    Diagnostic,
    Gpu,
    Storage,
    Memory,
    Sensor
}

class MetricCard : Panel
{
    private static readonly Color CardBackground = Color.FromArgb(27, 31, 36);
    private static readonly Color CardBorder = Color.FromArgb(52, 60, 69);
    private static readonly Color TitleColor = Color.FromArgb(220, 224, 229);
    private static readonly Color SecondaryColor = Color.FromArgb(177, 185, 194);

    private readonly Label _titleLabel;
    private readonly Label _primaryLabel;
    private readonly Label _secondaryLabel;
    private readonly MiniTrendGraph _trendGraph;
    private readonly MetricIconBadge _iconBadge;
    private string _secondaryText = "Aguardando leitura";
    private bool _isCompactLayout;

    public MetricCard(string title, MetricIconKind iconKind)
    {
        Dock = DockStyle.Fill;
        Margin = new Padding(6);
        Padding = new Padding(10, 9, 10, 9);
        MinimumSize = new Size(0, DashboardLayoutMetrics.CardNormalHeight);
        BackColor = CardBackground;
        DoubleBuffered = true;

        TableLayoutPanel content = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = CardBackground,
            ColumnCount = 1,
            RowCount = 4,
            Margin = new Padding(0),
            Padding = new Padding(0)
        };
        content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        content.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        content.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        content.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        content.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        _titleLabel = new Label
        {
            Text = title,
            Dock = DockStyle.Fill,
            Height = 22,
            ForeColor = TitleColor,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold, GraphicsUnit.Point),
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = true,
            Margin = new Padding(0)
        };

        _iconBadge = new MetricIconBadge(iconKind)
        {
            Dock = DockStyle.Left,
            Size = new Size(44, 22),
            Margin = new Padding(0, 0, 8, 0)
        };

        _primaryLabel = new Label
        {
            Text = "--",
            Dock = DockStyle.Fill,
            Height = 46,
            ForeColor = Color.FromArgb(78, 140, 255),
            Font = new Font("Segoe UI", 22, FontStyle.Bold, GraphicsUnit.Point),
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = true,
            Margin = new Padding(0)
        };

        _secondaryLabel = new Label
        {
            Text = "Aguardando leitura",
            Dock = DockStyle.Fill,
            AutoEllipsis = true,
            TextAlign = ContentAlignment.TopLeft,
            ForeColor = SecondaryColor,
            Font = new Font("Segoe UI", 9.25f, FontStyle.Regular, GraphicsUnit.Point),
            UseMnemonic = false,
            Margin = new Padding(0),
            AutoSize = false,
            Height = 24
        };

        _trendGraph = new MiniTrendGraph
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 4, 0, 0),
            Visible = true
        };

        TableLayoutPanel titleRow = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = CardBackground,
            Margin = new Padding(0, 0, 0, 2),
            Height = 24,
            Padding = new Padding(0),
            ColumnCount = 2,
            RowCount = 1
        };
        titleRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 52));
        titleRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        titleRow.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        titleRow.Controls.Add(_iconBadge, 0, 0);
        titleRow.Controls.Add(_titleLabel, 1, 0);

        content.Controls.Add(titleRow, 0, 0);
        content.Controls.Add(_primaryLabel, 0, 1);
        content.Controls.Add(_secondaryLabel, 0, 2);
        content.Controls.Add(_trendGraph, 0, 3);
        Controls.Add(content);

        SizeChanged += (_, _) => UpdateSecondaryLabel();
        Paint += MetricCardPaint;
    }

    public void SetValues(string primary, string secondary, Color accent, IReadOnlyList<float?> trendValues)
    {
        _primaryLabel.Text = string.IsNullOrWhiteSpace(primary) ? "--" : primary;
        _primaryLabel.ForeColor = accent;
        _iconBadge.SetAccent(accent);
        _trendGraph.SetValues(trendValues, accent);

        _secondaryText = string.IsNullOrWhiteSpace(secondary)
            ? "Informação não disponível"
            : secondary;

        UpdateSecondaryLabel();
    }

    public void SetCompactLayout(bool isCompact)
    {
        if (_isCompactLayout == isCompact)
        {
            return;
        }

        _isCompactLayout = isCompact;

        if (isCompact)
        {
            Padding = new Padding(9, 7, 9, 7);
            MinimumSize = new Size(0, DashboardLayoutMetrics.CardCompactHeight);
            _titleLabel.Font = new Font("Segoe UI", 8.75f, FontStyle.Bold, GraphicsUnit.Point);
            _primaryLabel.Font = new Font("Segoe UI", 18, FontStyle.Bold, GraphicsUnit.Point);
            _primaryLabel.Height = 38;
            _secondaryLabel.Font = new Font("Segoe UI", 8.25f, FontStyle.Regular, GraphicsUnit.Point);
            _secondaryLabel.Height = 20;
            _trendGraph.Visible = false;
        }
        else
        {
            Padding = new Padding(10, 9, 10, 9);
            MinimumSize = new Size(0, DashboardLayoutMetrics.CardNormalHeight);
            _titleLabel.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold, GraphicsUnit.Point);
            _primaryLabel.Font = new Font("Segoe UI", 22, FontStyle.Bold, GraphicsUnit.Point);
            _primaryLabel.Height = 46;
            _secondaryLabel.Font = new Font("Segoe UI", 9.25f, FontStyle.Regular, GraphicsUnit.Point);
            _secondaryLabel.Height = 24;
            _trendGraph.Visible = true;
        }

        UpdateSecondaryLabel();
    }

    private void UpdateSecondaryLabel()
    {
        _secondaryLabel.Text = FormatSecondaryText(_secondaryText, stackLines: !_isCompactLayout);
        int availableWidth = Math.Max(0, ClientSize.Width - Padding.Horizontal);
        _secondaryLabel.MaximumSize = availableWidth > 0
            ? new Size(availableWidth, 0)
            : Size.Empty;
    }

    private static string FormatSecondaryText(string text, bool stackLines)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return "Informação não disponível";
        }

        return stackLines
            ? text.Replace(" | ", Environment.NewLine)
            : text;
    }

    private void MetricCardPaint(object? sender, PaintEventArgs e)
    {
        Rectangle bounds = ClientRectangle;
        bounds.Width -= 1;
        bounds.Height -= 1;

        using Pen borderPen = new Pen(CardBorder);
        using Pen accentPen = new Pen(_primaryLabel.ForeColor, 2f);

        e.Graphics.DrawRectangle(borderPen, bounds);
        e.Graphics.DrawLine(accentPen, bounds.Left + 10, bounds.Top + 1, bounds.Right - 10, bounds.Top + 1);
    }
}

class MetricIconBadge : Control
{
    private MetricIconKind _iconKind;
    private Color _accent = Color.FromArgb(78, 140, 255);
    private readonly string _text;

    public MetricIconBadge(MetricIconKind iconKind)
    {
        _iconKind = iconKind;
        DoubleBuffered = true;
        BackColor = Color.FromArgb(33, 37, 42);
        ForeColor = Color.FromArgb(236, 239, 243);
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
        _text = iconKind switch
        {
            MetricIconKind.Cpu => "CPU",
            MetricIconKind.Gpu => "GPU",
            MetricIconKind.Temperature => "TEMP",
            MetricIconKind.Fan => "FAN",
            MetricIconKind.Voltage => "V",
            MetricIconKind.Diagnostic => "OK",
            MetricIconKind.Storage => "SSD",
            MetricIconKind.Memory => "RAM",
            _ => "S"
        };
        Text = _text;
    }

    public void SetAccent(Color accent)
    {
        _accent = accent;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        Rectangle bounds = ClientRectangle;
        bounds.Inflate(-1, -1);

        using Brush bgBrush = new SolidBrush(Color.FromArgb(42, _accent));
        using Pen borderPen = new Pen(Color.FromArgb(190, _accent), 1);
        using Font badgeFont = new Font("Segoe UI Semibold", 8.25f, FontStyle.Bold, GraphicsUnit.Point);

        Rectangle roundRect = bounds;
        int radius = Math.Max(6, Math.Min(roundRect.Height, roundRect.Width) / 2);
        using GraphicsPath path = CreateRoundedRect(roundRect, radius);
        e.Graphics.FillPath(bgBrush, path);
        e.Graphics.DrawPath(borderPen, path);

        TextRenderer.DrawText(
            e.Graphics,
            _text,
            badgeFont,
            bounds,
            ForeColor,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
    }

    private static GraphicsPath CreateRoundedRect(Rectangle bounds, int radius)
    {
        int diameter = Math.Max(1, radius * 2);
        Rectangle arc = new Rectangle(bounds.Location, new Size(diameter, diameter));
        GraphicsPath path = new GraphicsPath();

        path.AddArc(arc, 180, 90);
        arc.X = bounds.Right - diameter;
        path.AddArc(arc, 270, 90);
        arc.Y = bounds.Bottom - diameter;
        path.AddArc(arc, 0, 90);
        arc.X = bounds.Left;
        path.AddArc(arc, 90, 90);
        path.CloseFigure();
        return path;
    }
}

class SensorSummaryRow : Panel
{
    private static readonly Color RowBackground = Color.FromArgb(24, 27, 31);
    private static readonly Color RowSelected = Color.FromArgb(32, 53, 71);
    private static readonly Color BorderColor = Color.FromArgb(48, 56, 64);
    private static readonly Color MainText = Color.FromArgb(230, 234, 238);
    private static readonly Color MutedText = Color.FromArgb(170, 176, 184);

    private readonly Panel _indicator;
    private readonly MetricIconBadge _iconBadge;
    private readonly Label _titleLabel;
    private readonly Label _valueLabel;
    private readonly Label _subtitleLabel;
    private Color _accent = Color.FromArgb(78, 140, 255);
    private bool _selected;

    public SensorReading? SourceSensor { get; private set; }

    public event EventHandler? Clicked;

    public SensorSummaryRow(string title, string value, string subtitle, MetricIconKind iconKind)
    {
        Height = 64;
        BackColor = RowBackground;
        BorderStyle = BorderStyle.FixedSingle;
        Cursor = Cursors.Hand;
        DoubleBuffered = true;

        _indicator = new Panel
        {
            Width = 10,
            Height = 10,
            BackColor = _accent,
            Margin = new Padding(0, 0, 10, 0)
        };

        _iconBadge = new MetricIconBadge(iconKind)
        {
            Size = new Size(40, 24),
            Margin = new Padding(0, 0, 10, 0)
        };

        _titleLabel = new Label
        {
            Text = title,
            Dock = DockStyle.Top,
            Height = 19,
            ForeColor = MainText,
            Font = new Font("Segoe UI", 9.25f, FontStyle.Bold, GraphicsUnit.Point),
            AutoEllipsis = true
        };

        _valueLabel = new Label
        {
            Text = value,
            Dock = DockStyle.Top,
            Height = 20,
            ForeColor = _accent,
            Font = new Font("Segoe UI", 10f, FontStyle.Bold, GraphicsUnit.Point),
            AutoEllipsis = true
        };

        _subtitleLabel = new Label
        {
            Text = subtitle,
            Dock = DockStyle.Top,
            Height = 18,
            ForeColor = MutedText,
            Font = new Font("Segoe UI", 8.25f, FontStyle.Regular, GraphicsUnit.Point),
            AutoEllipsis = true
        };

        FlowLayoutPanel iconColumn = new FlowLayoutPanel
        {
            Dock = DockStyle.Left,
            Width = 48,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            BackColor = RowBackground,
            Margin = new Padding(0),
            Padding = new Padding(4, 14, 0, 0)
        };
        iconColumn.Controls.Add(_indicator);
        iconColumn.Controls.Add(_iconBadge);

        Panel textColumn = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = RowBackground,
            Margin = new Padding(0),
            Padding = new Padding(0, 8, 0, 0)
        };
        textColumn.Controls.Add(_subtitleLabel);
        textColumn.Controls.Add(_valueLabel);
        textColumn.Controls.Add(_titleLabel);

        Controls.Add(textColumn);
        Controls.Add(iconColumn);

        SourceSensor = null;
        SetValues(value, subtitle, _accent, iconKind, null, false);

        Click += (_, _) => Clicked?.Invoke(this, EventArgs.Empty);
        foreach (Control child in Controls)
        {
            child.Click += (_, _) => Clicked?.Invoke(this, EventArgs.Empty);
            foreach (Control grandChild in child.Controls)
            {
                grandChild.Click += (_, _) => Clicked?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    public void SetValues(string value, string subtitle, Color accent, MetricIconKind iconKind, SensorReading? sourceSensor, bool selected)
    {
        SourceSensor = sourceSensor;
        _accent = accent;
        _iconBadge.SetAccent(accent);
        _indicator.BackColor = accent;
        _valueLabel.Text = string.IsNullOrWhiteSpace(value) ? "--" : value;
        _subtitleLabel.Text = string.IsNullOrWhiteSpace(subtitle) ? "Sem dados adicionais" : subtitle;
        _valueLabel.ForeColor = accent;
        _iconBadge.Invalidate();
        _selected = selected;
        BackColor = selected ? RowSelected : RowBackground;
        _titleLabel.ForeColor = MainText;
        _subtitleLabel.ForeColor = selected ? Color.FromArgb(210, 218, 224) : MutedText;
        Invalidate();
    }

    public void SetSelected(bool selected)
    {
        _selected = selected;
        BackColor = selected ? RowSelected : RowBackground;
        _subtitleLabel.ForeColor = selected ? Color.FromArgb(210, 218, 224) : MutedText;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        using Pen borderPen = new Pen(_selected ? _accent : BorderColor);
        Rectangle bounds = ClientRectangle;
        bounds.Width -= 1;
        bounds.Height -= 1;
        e.Graphics.DrawRectangle(borderPen, bounds);
    }
}

class DashboardScopePanel : Control
{
    private static readonly Color GridColor = Color.FromArgb(42, 49, 56);
    private static readonly Color AxisColor = Color.FromArgb(69, 79, 88);
    private static readonly Color EmptyColor = Color.FromArgb(170, 176, 184);
    private const int MaxPoints = 240;

    private readonly List<float?> _values = new();
    private Color _accent = Color.FromArgb(78, 140, 255);
    private string _title = "Coletando amostras...";
    private string _subtitle = "Selecione um sensor";
    private string? _unit;

    public DashboardScopePanel()
    {
        DoubleBuffered = true;
        BackColor = Color.FromArgb(14, 16, 18);
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
    }

    public void SetData(string title, string subtitle, Color accent, IReadOnlyList<float?> values, string? unit)
    {
        _title = title;
        _subtitle = subtitle;
        _accent = accent;
        _unit = unit;

        _values.Clear();
        int start = Math.Max(0, values.Count - MaxPoints);
        for (int index = start; index < values.Count; index++)
        {
            _values.Add(values[index]);
        }

        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

        Rectangle bounds = ClientRectangle;
        using Brush backgroundBrush = new SolidBrush(BackColor);
        using Pen gridPen = new Pen(GridColor);
        using Pen axisPen = new Pen(AxisColor);
        using Pen linePen = new Pen(_accent, 2.2f);
        using Brush glowBrush = new SolidBrush(Color.FromArgb(38, _accent));
        using Brush captionBrush = new SolidBrush(Color.FromArgb(210, 218, 224));
        using Brush mutedBrush = new SolidBrush(EmptyColor);
        using Font titleFont = new Font("Segoe UI", 10.5f, FontStyle.Bold, GraphicsUnit.Point);
        using Font subtitleFont = new Font("Segoe UI", 8.75f, FontStyle.Regular, GraphicsUnit.Point);

        e.Graphics.FillRectangle(backgroundBrush, bounds);

        Rectangle plotBounds = Rectangle.Inflate(bounds, -14, -24);
        plotBounds.Y += 18;
        plotBounds.Height -= 10;

        if (plotBounds.Width <= 10 || plotBounds.Height <= 10)
        {
            return;
        }

        for (int column = 0; column <= 8; column++)
        {
            int x = plotBounds.Left + (plotBounds.Width * column / 8);
            e.Graphics.DrawLine(column == 4 ? axisPen : gridPen, x, plotBounds.Top, x, plotBounds.Bottom);
        }

        for (int row = 0; row <= 4; row++)
        {
            int y = plotBounds.Top + (plotBounds.Height * row / 4);
            e.Graphics.DrawLine(row == 2 ? axisPen : gridPen, plotBounds.Left, y, plotBounds.Right, y);
        }

        e.Graphics.DrawString(_title, titleFont, captionBrush, bounds.Left + 14, bounds.Top + 4);
        e.Graphics.DrawString(_subtitle, subtitleFont, mutedBrush, bounds.Left + 14, bounds.Top + 22);

        if (_values.Count == 0 || !_values.Any(value => value.HasValue))
        {
            e.Graphics.DrawString("Coletando amostras...", titleFont, mutedBrush, plotBounds.Left + 14, plotBounds.Top + 18);
            return;
        }

        List<float> samples = _values.Where(value => value.HasValue).Select(value => value!.Value).ToList();
        if (samples.Count == 0)
        {
            return;
        }

        float min = samples.Min();
        float max = samples.Max();
        if (Math.Abs(max - min) < 0.001f)
        {
            float pad = Math.Max(1f, Math.Abs(max) * 0.1f);
            min -= pad;
            max += pad;
        }

        float range = Math.Max(0.001f, max - min);
        float stepX = _values.Count > 1 ? (float)plotBounds.Width / (_values.Count - 1) : plotBounds.Width;
        List<PointF> points = new List<PointF>();

        for (int i = 0; i < _values.Count; i++)
        {
            float? value = _values[i];
            if (!value.HasValue)
            {
                continue;
            }

            float normalized = (value.Value - min) / range;
            normalized = Math.Max(0f, Math.Min(1f, normalized));

            float x = plotBounds.Left + i * stepX;
            float y = plotBounds.Bottom - 1 - normalized * Math.Max(1, plotBounds.Height - 2);
            points.Add(new PointF(x, y));
        }

        if (points.Count == 0)
        {
            return;
        }

        if (points.Count == 1)
        {
            PointF point = points[0];
            e.Graphics.DrawLine(linePen, plotBounds.Left, point.Y, plotBounds.Right, point.Y);
            e.Graphics.FillEllipse(glowBrush, point.X - 2, point.Y - 2, 4, 4);
            return;
        }

        List<PointF> fillPoints = new List<PointF>(points.Count + 2)
        {
            new PointF(points[0].X, plotBounds.Bottom - 1)
        };
        fillPoints.AddRange(points);
        fillPoints.Add(new PointF(points[^1].X, plotBounds.Bottom - 1));

        e.Graphics.FillPolygon(glowBrush, fillPoints.ToArray());
        e.Graphics.DrawLines(linePen, points.ToArray());
    }
}

class MiniTrendGraph : Control
{
    private static readonly Color GridColor = Color.FromArgb(45, 52, 60);
    private static readonly Color EmptyColor = Color.FromArgb(90, 96, 104);
    private const int MaxPoints = 30;

    private readonly List<float?> _values = new List<float?>(MaxPoints);
    private Color _accent = Color.FromArgb(78, 140, 255);

    public MiniTrendGraph()
    {
        DoubleBuffered = true;
        BackColor = Color.FromArgb(27, 31, 36);
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
    }

    public void SetValues(IReadOnlyList<float?> values, Color accent)
    {
        _accent = accent;
        _values.Clear();

        int start = Math.Max(0, values.Count - MaxPoints);
        for (int i = start; i < values.Count; i++)
        {
            _values.Add(values[i]);
        }

        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        Rectangle bounds = ClientRectangle;
        if (bounds.Width <= 2 || bounds.Height <= 2)
        {
            return;
        }

        using Pen gridPen = new Pen(GridColor);
        using Pen linePen = new Pen(_accent, 2f);
        using Brush fillBrush = new SolidBrush(Color.FromArgb(35, _accent));
        using Brush emptyBrush = new SolidBrush(EmptyColor);

        int midY = bounds.Top + bounds.Height / 2;
        e.Graphics.DrawLine(gridPen, bounds.Left, midY, bounds.Right, midY);

        List<float> samples = _values.Where(value => value.HasValue).Select(value => value!.Value).ToList();
        if (samples.Count == 0)
        {
            e.Graphics.FillEllipse(emptyBrush, bounds.Left + 2, bounds.Top + bounds.Height / 2 - 2, 4, 4);
            return;
        }

        float min = samples.Min();
        float max = samples.Max();
        if (Math.Abs(max - min) < 0.001f)
        {
            float pad = Math.Max(1f, Math.Abs(max) * 0.1f);
            min -= pad;
            max += pad;
        }

        float range = Math.Max(0.001f, max - min);
        float stepX = _values.Count > 1 ? (float)bounds.Width / (_values.Count - 1) : bounds.Width;
        List<PointF> points = new List<PointF>();

        for (int i = 0; i < _values.Count; i++)
        {
            float? value = _values[i];
            if (!value.HasValue)
            {
                continue;
            }

            float normalized = (value.Value - min) / range;
            normalized = Math.Max(0f, Math.Min(1f, normalized));
            float x = bounds.Left + i * stepX;
            float y = bounds.Bottom - 1 - normalized * Math.Max(1, bounds.Height - 2);
            points.Add(new PointF(x, y));
        }

        if (points.Count == 1)
        {
            e.Graphics.DrawLine(linePen, bounds.Left, points[0].Y, bounds.Right, points[0].Y);
            e.Graphics.FillEllipse(fillBrush, points[0].X - 2, points[0].Y - 2, 4, 4);
            return;
        }

        e.Graphics.FillPolygon(fillBrush, BuildFillPolygon(points, bounds));
        e.Graphics.DrawLines(linePen, points.ToArray());
    }

    private static PointF[] BuildFillPolygon(List<PointF> points, Rectangle bounds)
    {
        List<PointF> polygon = new List<PointF>(points.Count + 2);
        polygon.Add(new PointF(points[0].X, bounds.Bottom - 1));
        polygon.AddRange(points);
        polygon.Add(new PointF(points[^1].X, bounds.Bottom - 1));
        return polygon.ToArray();
    }
}

class ShortTrendHistory
{
    private const int MaxPoints = 30;
    private readonly Queue<float?> _values = new Queue<float?>(MaxPoints);

    public IReadOnlyList<float?> Values => _values.ToArray();

    public void Add(float? value)
    {
        _values.Enqueue(value);
        while (_values.Count > MaxPoints)
        {
            _values.Dequeue();
        }
    }
}

class RollingHistory
{
    private readonly int _maxPoints;
    private readonly Queue<float?> _values;

    public RollingHistory(int maxPoints)
    {
        _maxPoints = Math.Max(1, maxPoints);
        _values = new Queue<float?>(_maxPoints);
    }

    public IReadOnlyList<float?> Values => _values.ToArray();

    public void Add(float? value)
    {
        _values.Enqueue(value);
        while (_values.Count > _maxPoints)
        {
            _values.Dequeue();
        }
    }

    public void Clear()
    {
        _values.Clear();
    }
}





