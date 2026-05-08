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
    private readonly Panel _historyView;
    private readonly Button _summaryTabButton;
    private readonly Button _bitsTabButton;
    private readonly Button _historyTabButton;
    private readonly Label _statusLabel;
    private readonly Button _pauseButton;
    private readonly Button _refreshButton;
    private readonly Button _closeButton;
    private readonly Dictionary<string, Label> _valueLabels = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Label> _bitLabels = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, RichTextBox> _bitRichTextBoxes = new(StringComparer.OrdinalIgnoreCase);
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

    public SensorLiveMonitorForm(
        HardwareMonitorService hardwareMonitor,
        string hardwareName,
        string hardwareType,
        string hardwareIdentifier,
        string sensorName,
        string sensorType,
        string sensorIdentifier,
        Icon? windowIcon = null)
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
        MinimumSize = new Size(980, 640);
        Size = new Size(1180, 760);
        BackColor = WindowBackground;
        ForeColor = MainText;
        Font = new Font("Segoe UI", 10, FontStyle.Regular, GraphicsUnit.Point);
        ShowInTaskbar = false;

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
        _historyView = BuildHistoryGridHost();
        _contentHost = BuildContentHost(_summaryView, _bitsView, _historyView);
        _summaryTabButton = BuildTabButton("Resumo");
        _bitsTabButton = BuildTabButton("Bits");
        _historyTabButton = BuildTabButton("Histórico");
        _summaryTabButton.Click += (_, _) => ShowView(_summaryView, _summaryTabButton);
        _bitsTabButton.Click += (_, _) => ShowView(_bitsView, _bitsTabButton);
        _historyTabButton.Click += (_, _) => ShowView(_historyView, _historyTabButton);

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

        Shown += async (_, _) =>
        {
            _timer.Start();
            await RefreshSensorAsync();
        };
        _timer.Tick += async (_, _) => await RefreshSensorAsync();
        FormClosing += (_, _) => _timer.Stop();
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
            WrapContents = false,
            FlowDirection = FlowDirection.LeftToRight,
            BackColor = WindowBackground,
            Margin = new Padding(0, 0, 0, 12),
            Padding = new Padding(0)
        };

        bar.Controls.Add(_summaryTabButton);
        bar.Controls.Add(_bitsTabButton);
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
            Margin = new Padding(0)
        };

        content.Dock = DockStyle.Fill;
        host.Controls.Add(content);
        return host;
    }

    private Button BuildTabButton(string text)
    {
        Button button = new()
        {
            Text = text,
            AutoSize = false,
            Height = 38,
            MinimumSize = new Size(120, 38),
            Margin = new Padding(0, 0, 8, 0),
            Padding = new Padding(16, 0, 16, 0),
            FlatStyle = FlatStyle.Flat,
            BackColor = ButtonBackground,
            ForeColor = Color.FromArgb(194, 199, 206),
            UseVisualStyleBackColor = false
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
        UpdateTabButtonState(_historyTabButton, ReferenceEquals(activeButton, _historyTabButton));
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
        UpdateBitInspector(reading.Value);

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



