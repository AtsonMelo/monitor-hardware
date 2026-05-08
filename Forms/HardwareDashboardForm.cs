using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Security.Principal;
using System.Windows.Forms;

class HardwareDashboardForm : Form
{
    private const int MinimumWindowWidth = 760;
    private const int MinimumWindowHeight = 700;
    private const int DefaultWindowWidth = 900;
    private const int HeaderStackBreakpoint = 940;
    private const int CardsSingleColumnBreakpoint = 860;
    private const int HeaderActionsWidth = 300;

    private readonly AppConfig _config;
    private readonly SnapshotService _snapshotService;
    private readonly CsvLoggerService _csvLogger;
    private readonly UpdateService _updateService;
    private readonly StartupTaskService _startupTaskService;
    private readonly System.Windows.Forms.Timer _timer;
    private readonly Icon _windowIcon;
    private readonly ToolTip _headerToolTip = new ToolTip();

    private Label _statusLabel = null!;
    private Label _titleLabel = null!;
    private Button _updateButton = null!;
    private Button _sensorsButton = null!;
    private Button _hardwareSelectionButton = null!;
    private Button _sensorOriginsButton = null!;
    private Button _errorReportButton = null!;
    private Button _helpButton = null!;
    private CheckBox _startupCheckBox = null!;
    private ContextMenuStrip _helpMenu = null!;
    private TableLayoutPanel _headerLayout = null!;
    private TableLayoutPanel _titleLayout = null!;
    private TableLayoutPanel _actionsLayout = null!;
    private FlowLayoutPanel _headerButtonsPanel = null!;
    private TableLayoutPanel _cardsGrid = null!;
    private MetricCard _cpuCard = null!;
    private MetricCard _gpuCard = null!;
    private MetricCard _ramCard = null!;
    private MetricCard _ssdCard = null!;
    private readonly ShortTrendHistory _cpuTrendHistory = new ShortTrendHistory();
    private readonly ShortTrendHistory _gpuTrendHistory = new ShortTrendHistory();
    private readonly ShortTrendHistory _ramTrendHistory = new ShortTrendHistory();
    private readonly ShortTrendHistory _ssdTrendHistory = new ShortTrendHistory();
    private HardwareMonitorService? _hardwareMonitor;
    private SensorsDetailsForm? _sensorsDetailsForm;
    private HardwareSelectionForm? _hardwareSelectionForm;
    private SensorOriginsForm? _sensorOriginsForm;
    private DateTime? _lastUpdatedAt;
    private bool _headerIsStacked;
    private bool _cardsAreStacked;

    public HardwareDashboardForm(AppConfig config)
    {
        _config = config;
        _snapshotService = new SnapshotService(config);
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
        _cpuCard.SetValues("...", "Carregando sensores da CPU...", Color.FromArgb(170, 176, 184), Array.Empty<float?>());
        _gpuCard.SetValues("...", "Carregando sensores da GPU...", Color.FromArgb(170, 176, 184), Array.Empty<float?>());
        _ramCard.SetValues("...", "Carregando memória RAM...", Color.FromArgb(170, 176, 184), Array.Empty<float?>());
        _ssdCard.SetValues("...", "Carregando sensores do SSD...", Color.FromArgb(170, 176, 184), Array.Empty<float?>());

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

    private bool ShouldStackHeader()
    {
        return GetLayoutWidth(this) < HeaderStackBreakpoint ||
               ClientSize.Height < 820;
    }

    private bool ShouldStackCards()
    {
        int layoutWidth = GetLayoutWidth(_cardsGrid);

        int layoutHeight = _cardsGrid.ClientSize.Height > 0
            ? _cardsGrid.ClientSize.Height
            : _cardsGrid.Height;

        bool isNarrow = layoutWidth < CardsSingleColumnBreakpoint;
        bool hasEnoughHeightForSingleColumn = layoutHeight >= 560;

        return isNarrow && hasEnoughHeightForSingleColumn;
    }

    private bool ShouldUseCompactCards(bool stackCards)
    {
        if (stackCards)
        {
            return true;
        }

        int layoutHeight = _cardsGrid.ClientSize.Height > 0
            ? _cardsGrid.ClientSize.Height
            : _cardsGrid.Height;

        int estimatedCardHeight = layoutHeight / 2;

        return estimatedCardHeight < 240;
    }

    private void BuildLayout()
    {
        TableLayoutPanel root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(18),
            BackColor = BackColor
        };

        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));

        _headerLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = BackColor,
            Margin = new Padding(0, 0, 0, 12)
        };

        _titleLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = BackColor,
            ColumnCount = 1,
            RowCount = 1,
            MinimumSize = new Size(0, 70)
        };

        _titleLabel = new Label
        {
            Text = "Monitor Hardware",
            Dock = DockStyle.Fill,
            Height = 42,
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 17, FontStyle.Bold, GraphicsUnit.Point),
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = true
        };
        _titleLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        _titleLayout.Controls.Add(_titleLabel, 0, 0);

        _headerButtonsPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = BackColor,
            Margin = new Padding(0),
            Padding = new Padding(0),
            RightToLeft = RightToLeft.Yes
        };

        _actionsLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(0),
            BackColor = BackColor,
            Margin = new Padding(0)
        };

        _updateButton = new Button
        {
            Text = "Verificar atualizações"
        };
        ConfigureActionButton(_updateButton);
        _updateButton.Click += async (_, _) => await CheckForUpdatesAsync(showUpToDate: true);

        _sensorsButton = new Button
        {
            Text = "Conferir todos os sensores"
        };
        ConfigureActionButton(_sensorsButton);
        _sensorsButton.Click += (_, _) => OpenSensorsDetails();

        _hardwareSelectionButton = new Button
        {
            Text = "Selecionar hardwares"
        };
        ConfigureActionButton(_hardwareSelectionButton);
        _hardwareSelectionButton.Click += (_, _) => OpenHardwareSelection();

        _sensorOriginsButton = new Button
        {
            Text = "Origem dos sensores"
        };
        ConfigureActionButton(_sensorOriginsButton);
        _sensorOriginsButton.Click += (_, _) => OpenSensorOrigins();

        _errorReportButton = new Button
        {
            Text = "Relatório de erros"
        };
        ConfigureActionButton(_errorReportButton);
        _errorReportButton.Click += (_, _) => GenerateAndOpenErrorReport();

        _helpButton = new Button
        {
            Text = "?"
        };
        ConfigureHelpButton(_helpButton);
        _helpButton.Click += (_, _) => ShowHelpMenu();

        _startupCheckBox = new CheckBox
        {
            Text = "Iniciar com o Windows",
            AutoSize = false,
            Dock = DockStyle.Fill,
            Height = 38,
            MinimumSize = new Size(220, 38),
            ForeColor = Color.FromArgb(210, 214, 220),
            BackColor = BackColor,
            TextAlign = ContentAlignment.MiddleLeft,
            UseVisualStyleBackColor = false
        };

        _startupCheckBox.CheckedChanged += StartupCheckBoxCheckedChanged;

        _helpMenu = BuildHelpMenu();

        _cardsGrid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(0),
            BackColor = BackColor
        };

        _cpuCard = new MetricCard("CPU");
        _gpuCard = new MetricCard("GPU");
        _ramCard = new MetricCard("Memória RAM");
        _ssdCard = new MetricCard("SSD");

        _cardsGrid.SizeChanged += (_, _) => ApplyCardsGridLayout();

        _statusLabel = new Label
        {
            Text = "Nenhum alerta crítico.",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = true,
            ForeColor = Color.FromArgb(170, 176, 184),
            Font = new Font("Segoe UI", 10, FontStyle.Bold, GraphicsUnit.Point)
        };

        root.Controls.Add(_headerLayout, 0, 0);
        root.Controls.Add(_cardsGrid, 0, 1);
        root.Controls.Add(_statusLabel, 0, 2);

        Controls.Add(root);
        ApplyResponsiveLayout();
    }

    private static void ConfigureActionButton(Button button)
    {
        button.Dock = DockStyle.Fill;
        button.Height = 38;
        button.MinimumSize = new Size(220, 38);
        button.FlatStyle = FlatStyle.Flat;
        button.ForeColor = Color.White;
        button.BackColor = Color.FromArgb(36, 41, 47);
        button.UseVisualStyleBackColor = false;
        button.TextAlign = ContentAlignment.MiddleCenter;
        button.FlatAppearance.BorderColor = Color.FromArgb(58, 66, 74);
        button.FlatAppearance.MouseOverBackColor = Color.FromArgb(44, 50, 57);
        button.FlatAppearance.MouseDownBackColor = Color.FromArgb(30, 34, 39);
    }

    private static void ConfigureHelpButton(Button button)
    {
        button.AutoSize = false;
        button.Size = new Size(30, 30);
        button.MinimumSize = new Size(30, 30);
        button.FlatStyle = FlatStyle.Flat;
        button.ForeColor = Color.FromArgb(230, 233, 236);
        button.BackColor = Color.FromArgb(32, 37, 42);
        button.UseVisualStyleBackColor = false;
        button.TextAlign = ContentAlignment.MiddleCenter;
        button.Font = new Font("Segoe UI", 10, FontStyle.Bold, GraphicsUnit.Point);
        button.FlatAppearance.BorderColor = Color.FromArgb(58, 66, 74);
        button.FlatAppearance.MouseOverBackColor = Color.FromArgb(44, 50, 57);
        button.FlatAppearance.MouseDownBackColor = Color.FromArgb(30, 34, 39);
        button.Margin = new Padding(0);
        button.Padding = new Padding(0);
    }

    private void ApplyResponsiveLayout()
    {
        if (_headerLayout == null || _cardsGrid == null)
        {
            return;
        }

        ApplyHeaderLayout();
        ApplyCardsGridLayout();
    }

    private void ApplyHeaderLayout()
    {
        bool stackHeader = ShouldStackHeader();

        if (_headerLayout.Controls.Count > 0 && _headerIsStacked == stackHeader)
        {
            return;
        }

        _headerIsStacked = stackHeader;

        _headerLayout.SuspendLayout();
        _headerLayout.Controls.Clear();
        _headerLayout.ColumnStyles.Clear();
        _headerLayout.RowStyles.Clear();

        if (stackHeader)
        {
            _headerLayout.ColumnCount = 1;
            _headerLayout.RowCount = 2;
            _headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            _headerLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            _headerLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            _titleLayout.Margin = new Padding(0, 0, 0, 0);
            _actionsLayout.Margin = new Padding(0);
            _headerButtonsPanel.Margin = new Padding(0, 0, 0, 10);

            ConfigureActionsLayout(useTwoColumns: true);
            ConfigureHeaderButtonsPanel();
            _headerLayout.Controls.Add(BuildHeaderTopRow(), 0, 0);
            _headerLayout.Controls.Add(_actionsLayout, 0, 1);
        }
        else
        {
            _headerLayout.ColumnCount = 1;
            _headerLayout.RowCount = 2;
            _headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            _headerLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            _headerLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            _titleLayout.Margin = new Padding(0);
            _actionsLayout.Margin = new Padding(0);
            _headerButtonsPanel.Margin = new Padding(0, 0, 0, 10);

            ConfigureActionsLayout(useTwoColumns: true);
            ConfigureHeaderButtonsPanel();
            _headerLayout.Controls.Add(BuildHeaderTopRow(), 0, 0);
            _headerLayout.Controls.Add(_actionsLayout, 0, 1);
        }

        _headerLayout.ResumeLayout(true);
    }

    private Control BuildHeaderTopRow()
    {
        TableLayoutPanel topRow = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = BackColor,
            ColumnCount = 2,
            RowCount = 1,
            Margin = new Padding(0)
        };

        topRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        topRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        topRow.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        topRow.Controls.Add(_titleLayout, 0, 0);
        topRow.Controls.Add(_headerButtonsPanel, 1, 0);
        return topRow;
    }

    private void ConfigureHeaderButtonsPanel()
    {
        _headerButtonsPanel.Controls.Clear();
        _headerButtonsPanel.Controls.Add(_helpButton);
        _headerToolTip.SetToolTip(_helpButton, "Ajuda");
        if (_lastUpdatedAt.HasValue)
        {
            _headerToolTip.SetToolTip(_titleLabel, $"Atualizado em {_lastUpdatedAt.Value:dd/MM/yyyy HH:mm:ss}");
        }
    }

    private void ConfigureActionsLayout(bool useTwoColumns)
    {
        _actionsLayout.SuspendLayout();
        _actionsLayout.Controls.Clear();
        _actionsLayout.ColumnStyles.Clear();
        _actionsLayout.RowStyles.Clear();

        if (useTwoColumns)
        {
            _actionsLayout.ColumnCount = 2;
            _actionsLayout.RowCount = 4;
            _actionsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            _actionsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            _actionsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
            _actionsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
            _actionsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
            _actionsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));

            AddActionControl(_updateButton, 0, 0, new Padding(0, 0, 8, 8));
            AddActionControl(_sensorsButton, 1, 0, new Padding(8, 0, 0, 8));
            AddActionControl(_hardwareSelectionButton, 0, 1, new Padding(0, 0, 8, 8));
            AddActionControl(_sensorOriginsButton, 1, 1, new Padding(8, 0, 0, 8));
            AddActionControl(_errorReportButton, 0, 2, new Padding(0, 0, 8, 8));
            AddActionControl(_startupCheckBox, 0, 3, new Padding(0, 2, 0, 0), columnSpan: 2);
        }
        else
        {
            _actionsLayout.ColumnCount = 1;
            _actionsLayout.RowCount = 6;
            _actionsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            for (int index = 0; index < 6; index++)
            {
                _actionsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
            }

            AddActionControl(_updateButton, 0, 0, new Padding(0, 0, 0, 8));
            AddActionControl(_sensorsButton, 0, 1, new Padding(0, 0, 0, 8));
            AddActionControl(_hardwareSelectionButton, 0, 2, new Padding(0, 0, 0, 8));
            AddActionControl(_sensorOriginsButton, 0, 3, new Padding(0, 0, 0, 8));
            AddActionControl(_errorReportButton, 0, 4, new Padding(0, 0, 0, 8));
            AddActionControl(_startupCheckBox, 0, 5, new Padding(0));
        }

        _actionsLayout.ResumeLayout(true);
    }

    private void AddActionControl(Control control, int column, int row, Padding margin, int columnSpan = 1)
    {
        control.Dock = DockStyle.Fill;
        control.Margin = margin;
        _actionsLayout.Controls.Add(control, column, row);
        _actionsLayout.SetColumnSpan(control, columnSpan);
    }

    private void OpenSensorsDetails()
    {
        try
        {
            _hardwareMonitor ??= new HardwareMonitorService();

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

            _sensorsDetailsForm = new SensorsDetailsForm(_hardwareMonitor, _windowIcon);
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

    private void OpenHardwareSelection()
    {
        try
        {
            _hardwareMonitor ??= new HardwareMonitorService();

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

            _hardwareSelectionForm = new HardwareSelectionForm(_hardwareMonitor, _windowIcon);
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

    private void RefreshSnapshot()
    {
        try
        {
            _hardwareMonitor ??= new HardwareMonitorService();

            List<SensorReading> sensors = _hardwareMonitor.ReadAllSensors();
            MonitorSnapshot snapshot = _snapshotService.Create(sensors);

            if (_config.EnableCsv)
            {
                _csvLogger.Save(snapshot);
            }

            UpdateCards(snapshot);
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

    private void ApplyCardsGridLayout()
    {
        if (GetLayoutWidth(_cardsGrid) <= 0)
        {
            return;
        }

        bool stackCards = ShouldStackCards();
        bool compactCards = ShouldUseCompactCards(stackCards);

        if (_cardsGrid.Controls.Count == 4 && _cardsAreStacked == stackCards)
        {
            ApplyMetricCardDensity(compactCards);
            return;
        }

        _cardsAreStacked = stackCards;

        _cardsGrid.SuspendLayout();
        _cardsGrid.Controls.Clear();
        _cardsGrid.ColumnStyles.Clear();
        _cardsGrid.RowStyles.Clear();

        if (stackCards)
        {
            _cardsGrid.ColumnCount = 1;
            _cardsGrid.RowCount = 4;
            _cardsGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            for (int index = 0; index < 4; index++)
            {
                _cardsGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 25));
            }

            AddMetricCard(_cpuCard, 0, 0, new Padding(0, 0, 0, 8), isCompact: true);
            AddMetricCard(_gpuCard, 0, 1, new Padding(0, 8, 0, 8), isCompact: true);
            AddMetricCard(_ramCard, 0, 2, new Padding(0, 8, 0, 8), isCompact: true);
            AddMetricCard(_ssdCard, 0, 3, new Padding(0, 8, 0, 0), isCompact: true);
        }
        else
        {
            _cardsGrid.ColumnCount = 2;
            _cardsGrid.RowCount = 2;
            _cardsGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            _cardsGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            _cardsGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
            _cardsGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 50));

            AddMetricCard(_cpuCard, 0, 0, new Padding(0, 0, 8, 8), isCompact: compactCards);
            AddMetricCard(_gpuCard, 1, 0, new Padding(8, 0, 0, 8), isCompact: compactCards);
            AddMetricCard(_ramCard, 0, 1, new Padding(0, 8, 8, 0), isCompact: compactCards);
            AddMetricCard(_ssdCard, 1, 1, new Padding(8, 8, 0, 0), isCompact: compactCards);
        }

        _cardsGrid.ResumeLayout(true);
    }

    private void AddMetricCard(MetricCard card, int column, int row, Padding margin, bool isCompact)
    {
        card.Dock = DockStyle.Fill;
        card.Margin = margin;
        card.SetCompactLayout(isCompact);
        _cardsGrid.Controls.Add(card, column, row);
    }

    private void ApplyMetricCardDensity(bool isCompact)
    {
        _cpuCard.SetCompactLayout(isCompact);
        _gpuCard.SetCompactLayout(isCompact);
        _ramCard.SetCompactLayout(isCompact);
        _ssdCard.SetCompactLayout(isCompact);
    }

    private void UpdateCards(MonitorSnapshot snapshot)
    {
        _cpuTrendHistory.Add(GetCpuTrendValue(snapshot));
        _gpuTrendHistory.Add(snapshot.GpuTemp);
        _ramTrendHistory.Add(snapshot.RamUso);
        _ssdTrendHistory.Add(snapshot.SsdTemp);

        _cpuCard.SetValues(
            GetCpuPrimaryText(snapshot),
            GetCpuSecondaryText(snapshot),
            GetCpuAccentColor(snapshot),
            _cpuTrendHistory.Values);

        _gpuCard.SetValues(
            FormatTemperature(snapshot.GpuTemp),
            $"Uso {FormatPercent(snapshot.GpuUso)} | Potência {FormatPower(snapshot.GpuPower)} | Fan {FormatFan(snapshot.GpuFan)}",
            GetTemperatureColor(snapshot.GpuTemp, _config.GpuTempMax),
            _gpuTrendHistory.Values);

        _ramCard.SetValues(
            GetRamPrimaryText(snapshot),
            GetRamSecondaryText(snapshot),
            GetLoadColor(snapshot.RamUso),
            _ramTrendHistory.Values);

        _ssdCard.SetValues(
            FormatTemperature(snapshot.SsdTemp),
            $"Limite configurado {FormatTemperature(_config.SsdTempMax)}",
            GetTemperatureColor(snapshot.SsdTemp, _config.SsdTempMax),
            _ssdTrendHistory.Values);
    }

    private static float? GetCpuTrendValue(MonitorSnapshot snapshot)
    {
        return snapshot.CpuTemp ?? snapshot.CpuUso;
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
            ? GetTemperatureColor(snapshot.CpuTemp, _config.CpuTempMax)
            : GetLoadColor(snapshot.CpuUso);
    }

    private static string GetRamPrimaryText(MonitorSnapshot snapshot)
    {
        return snapshot.RamUsadaGb.HasValue && snapshot.RamTotalGb.HasValue
            ? $"{FormatMemoryNumber(snapshot.RamUsadaGb)}/{FormatMemory(snapshot.RamTotalGb)}"
            : FormatPercent(snapshot.RamUso);
    }

    private static string GetRamSecondaryText(MonitorSnapshot snapshot)
    {
        string usageText = $"Uso {FormatPercent(snapshot.RamUso)}";

        if (snapshot.RamDisponivelGb.HasValue)
        {
            usageText += $" | Disponível {FormatMemory(snapshot.RamDisponivelGb)}";
        }

        return usageText;
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

    private Color GetStatusColor(MonitorSnapshot snapshot)
    {
        return snapshot.CpuTemp >= _config.CpuTempMax ||
               snapshot.GpuTemp >= _config.GpuTempMax ||
               snapshot.SsdTemp >= _config.SsdTempMax
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

                    await _updateService.StartUpdateAsync(result, progress);
                    AppLogService.Info($"Atualização iniciada na pasta: {AppContext.BaseDirectory}");
                    Application.Exit();
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
            if (showUpToDate)
            {
                MessageBox.Show(
                    $"Não foi possível verificar atualizações: {ex.Message}",
                    "Monitor Hardware",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }
        finally
        {
            _updateButton.Text = "Verificar atualizações";
            _updateButton.Enabled = true;
        }
    }

    private ContextMenuStrip BuildHelpMenu()
    {
        ContextMenuStrip menu = new ContextMenuStrip();

        ToolStripMenuItem openSupportItem = new ToolStripMenuItem("Abrir suporte no GitHub");
        openSupportItem.Click += (_, _) => OpenUrl("https://github.com/AtsonMelo/monitor-hardware/issues/new/choose");

        ToolStripMenuItem openLogItem = new ToolStripMenuItem("Abrir log de erros");
        openLogItem.Click += (_, _) => OpenLogFile();

        ToolStripMenuItem openLogFolderItem = new ToolStripMenuItem("Abrir pasta de logs");
        openLogFolderItem.Click += (_, _) => OpenFolder(Path.GetDirectoryName(AppLogService.LogPath) ?? ".");

        ToolStripMenuItem createReportItem = new ToolStripMenuItem("Gerar relatório de erros");
        createReportItem.Click += (_, _) => GenerateAndOpenErrorReport();

        menu.Items.Add(openSupportItem);
        menu.Items.Add(createReportItem);
        menu.Items.Add(openLogItem);
        menu.Items.Add(openLogFolderItem);

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

class MetricCard : Panel
{
    private static readonly Color CardBackground = Color.FromArgb(27, 31, 36);
    private static readonly Color CardBorder = Color.FromArgb(52, 60, 69);
    private static readonly Color CardAccent = Color.FromArgb(78, 140, 255);
    private static readonly Color TitleColor = Color.FromArgb(220, 224, 229);
    private static readonly Color SecondaryColor = Color.FromArgb(177, 185, 194);

    private readonly Label _titleLabel;
    private readonly Label _primaryLabel;
    private readonly Label _secondaryLabel;
    private readonly MiniTrendGraph _trendGraph;
    private readonly Panel _contentPanel;
    private string _secondaryText = "Aguardando leitura";
    private bool _isCompactLayout;

    public MetricCard(string title)
    {
        Dock = DockStyle.Fill;
        Margin = new Padding(8);
        Padding = new Padding(20, 18, 20, 18);
        MinimumSize = new Size(0, 190);
        BackColor = CardBackground;
        DoubleBuffered = true;

        _contentPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = CardBackground,
            Padding = new Padding(0),
            Margin = new Padding(0)
        };

        _titleLabel = new Label
        {
            Text = title,
            Dock = DockStyle.Top,
            Height = 26,
            ForeColor = TitleColor,
            Font = new Font("Segoe UI", 10.5f, FontStyle.Bold, GraphicsUnit.Point),
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = true,
            Margin = new Padding(0)
        };

        _primaryLabel = new Label
        {
            Text = "--",
            Dock = DockStyle.Top,
            Height = 72,
            ForeColor = CardAccent,
            Font = new Font("Segoe UI", 27, FontStyle.Bold, GraphicsUnit.Point),
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = true,
            Margin = new Padding(0)
        };

        _secondaryLabel = new Label
        {
            Text = "Aguardando leitura",
            Dock = DockStyle.Top,
            AutoEllipsis = false,
            TextAlign = ContentAlignment.TopLeft,
            ForeColor = SecondaryColor,
            Font = new Font("Segoe UI", 10f, FontStyle.Regular, GraphicsUnit.Point),
            UseMnemonic = false,
            Margin = new Padding(0),
            AutoSize = true
        };

        _trendGraph = new MiniTrendGraph
        {
            Dock = DockStyle.Bottom,
            Height = 40,
            Margin = new Padding(0),
            Visible = true
        };

        _contentPanel.Controls.Add(_secondaryLabel);
        _contentPanel.Controls.Add(_primaryLabel);
        _contentPanel.Controls.Add(_titleLabel);
        _contentPanel.Controls.Add(_trendGraph);
        Controls.Add(_contentPanel);
        SizeChanged += (_, _) => UpdateSecondaryLabel();
        Paint += MetricCardPaint;
    }

    public void SetValues(string primary, string secondary, Color accent, IReadOnlyList<float?> trendValues)
    {
        _primaryLabel.Text = string.IsNullOrWhiteSpace(primary) ? "--" : primary;
        _primaryLabel.ForeColor = accent;
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
            Padding = new Padding(14, 12, 14, 12);
            MinimumSize = new Size(0, 126);
            _titleLabel.Height = 20;
            _primaryLabel.Height = 38;
            _titleLabel.Font = new Font("Segoe UI", 9.25f, FontStyle.Bold, GraphicsUnit.Point);
            _primaryLabel.Font = new Font("Segoe UI", 19, FontStyle.Bold, GraphicsUnit.Point);
            _secondaryLabel.Font = new Font("Segoe UI", 8.75f, FontStyle.Regular, GraphicsUnit.Point);
            _trendGraph.Visible = false;
        }
        else
        {
            Padding = new Padding(20, 18, 20, 18);
            MinimumSize = new Size(0, 190);
            _titleLabel.Height = 26;
            _primaryLabel.Height = 72;
            _titleLabel.Font = new Font("Segoe UI", 10.5f, FontStyle.Bold, GraphicsUnit.Point);
            _primaryLabel.Font = new Font("Segoe UI", 27, FontStyle.Bold, GraphicsUnit.Point);
            _secondaryLabel.Font = new Font("Segoe UI", 10f, FontStyle.Regular, GraphicsUnit.Point);
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
        using Pen accentPen = new Pen(CardAccent, 2f);

        e.Graphics.DrawRectangle(borderPen, bounds);
        e.Graphics.DrawLine(accentPen, bounds.Left + 10, bounds.Top + 1, bounds.Right - 10, bounds.Top + 1);
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

        e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        Rectangle bounds = ClientRectangle;

        using Pen gridPen = new Pen(GridColor);
        using Pen linePen = new Pen(_accent, 2f);
        using Brush fillBrush = new SolidBrush(Color.FromArgb(35, _accent));
        using Brush emptyBrush = new SolidBrush(EmptyColor);

        int midY = bounds.Top + bounds.Height / 2;
        e.Graphics.DrawLine(gridPen, bounds.Left, midY, bounds.Right, midY);

        if (_values.Count < 2 || !_values.Any(value => value.HasValue))
        {
            e.Graphics.FillEllipse(emptyBrush, bounds.Left + 2, bounds.Top + bounds.Height / 2 - 2, 4, 4);
            return;
        }

        List<PointF> points = new List<PointF>(_values.Count);
        float min = float.MaxValue;
        float max = float.MinValue;

        foreach (float? value in _values)
        {
            if (!value.HasValue)
            {
                continue;
            }

            min = Math.Min(min, value.Value);
            max = Math.Max(max, value.Value);
        }

        if (min == float.MaxValue || max == float.MinValue)
        {
            return;
        }

        float range = Math.Max(1f, max - min);
        float stepX = _values.Count > 1 ? (float)bounds.Width / (float)(_values.Count - 1) : bounds.Width;

        for (int i = 0; i < _values.Count; i++)
        {
            float? value = _values[i];
            if (!value.HasValue)
            {
                continue;
            }

            float x = bounds.Left + i * stepX;
            float y = bounds.Bottom - 1 - ((value.Value - min) / range * Math.Max(1, bounds.Height - 2));
            points.Add(new PointF(x, y));
        }

        if (points.Count < 2)
        {
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


