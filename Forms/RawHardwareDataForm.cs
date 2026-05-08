using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using LibreHardwareMonitor.Hardware;

class RawHardwareDataForm : Form
{
    private static readonly Color WindowBackground = Color.FromArgb(17, 19, 22);
    private static readonly Color PanelBackground = Color.FromArgb(24, 27, 31);
    private static readonly Color InputBackground = Color.FromArgb(30, 34, 39);
    private static readonly Color ButtonBackground = Color.FromArgb(36, 41, 47);
    private static readonly Color ButtonBorder = Color.FromArgb(58, 66, 74);
    private static readonly Color MainText = Color.FromArgb(230, 234, 238);
    private static readonly Color MutedText = Color.FromArgb(170, 176, 184);

    private readonly HardwareMonitorService _hardwareMonitor;
    private readonly HardwareSelectionService _hardwareSelectionService;
    private readonly Icon? _ownedIcon;
    private readonly DataGridView _grid;
    private readonly Label _footerLabel;
    private readonly Button _refreshButton;
    private readonly Button _closeButton;
    private DateTime? _lastUpdatedAt;
    private bool _isFilteringByHardware;

    public RawHardwareDataForm(HardwareMonitorService hardwareMonitor, HardwareSelectionService hardwareSelectionService, Icon? windowIcon = null)
    {
        _hardwareMonitor = hardwareMonitor ?? throw new ArgumentNullException(nameof(hardwareMonitor));
        _hardwareSelectionService = hardwareSelectionService ?? throw new ArgumentNullException(nameof(hardwareSelectionService));

        Text = "Dados brutos do hardware";
        AutoScaleMode = AutoScaleMode.Dpi;
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(1020, 620);
        Size = new Size(1280, 760);
        BackColor = WindowBackground;
        ForeColor = MainText;
        Font = new Font("Segoe UI", 10, FontStyle.Regular, GraphicsUnit.Point);
        ShowInTaskbar = false;

        if (windowIcon != null)
        {
            _ownedIcon = (Icon)windowIcon.Clone();
            Icon = _ownedIcon;
        }

        TableLayoutPanel root = new TableLayoutPanel
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
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));

        TableLayoutPanel header = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = WindowBackground,
            Margin = new Padding(0, 0, 0, 12),
            AutoSize = true
        };

        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        header.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        Label titleLabel = new Label
        {
            Text = "Dados brutos do hardware",
            Dock = DockStyle.Fill,
            ForeColor = MainText,
            Font = new Font("Segoe UI", 15, FontStyle.Bold, GraphicsUnit.Point),
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = true
        };

        _refreshButton = new Button { Text = "Atualizar" };
        ConfigureActionButton(_refreshButton);
        _refreshButton.Click += (_, _) => RefreshData();

        header.Controls.Add(titleLabel, 0, 0);
        header.Controls.Add(_refreshButton, 1, 0);

        _grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            BackgroundColor = PanelBackground,
            BorderStyle = BorderStyle.FixedSingle,
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
            ColumnHeadersDefaultCellStyle = BuildHeaderStyle(),
            DefaultCellStyle = BuildCellStyle(),
            AlternatingRowsDefaultCellStyle = BuildAlternatingStyle()
        };

        _grid.Columns.Add(CreateTextColumn("Hardware", nameof(RawHardwareRow.Hardware)));
        _grid.Columns.Add(CreateTextColumn("Tipo hardware", nameof(RawHardwareRow.HardwareType)));
        _grid.Columns.Add(CreateTextColumn("Tipo sensor", nameof(RawHardwareRow.SensorType)));
        _grid.Columns.Add(CreateTextColumn("Nome sensor", nameof(RawHardwareRow.SensorName)));
        _grid.Columns.Add(CreateTextColumn("Valor bruto", nameof(RawHardwareRow.RawValue)));
        _grid.Columns.Add(CreateTextColumn("Valor formatado", nameof(RawHardwareRow.FormattedValue)));
        _grid.Columns.Add(CreateTextColumn("Mínimo", nameof(RawHardwareRow.Min)));
        _grid.Columns.Add(CreateTextColumn("Máximo", nameof(RawHardwareRow.Max)));
        _grid.Columns.Add(CreateTextColumn("Identificador do sensor", nameof(RawHardwareRow.SensorIdentifier)));
        _grid.Columns.Add(CreateTextColumn("Identificador do hardware", nameof(RawHardwareRow.HardwareIdentifier)));

        _footerLabel = new Label
        {
            Dock = DockStyle.Fill,
            ForeColor = MutedText,
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = true
        };

        Panel footerPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = WindowBackground
        };

        footerPanel.Controls.Add(_footerLabel);

        FlowLayoutPanel buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            AutoSize = true,
            BackColor = WindowBackground,
            Margin = new Padding(0, 0, 0, 12)
        };

        _closeButton = new Button { Text = "Fechar" };
        ConfigureActionButton(_closeButton);
        _closeButton.Click += (_, _) => Close();

        buttons.Controls.Add(_closeButton);

        root.Controls.Add(header, 0, 0);
        root.Controls.Add(buttons, 0, 1);
        root.Controls.Add(_grid, 0, 2);
        root.Controls.Add(footerPanel, 0, 3);

        Controls.Add(root);

        Shown += (_, _) => RefreshData();
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
            _ownedIcon?.Dispose();
        }

        base.Dispose(disposing);
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

    private static DataGridViewCellStyle BuildHeaderStyle()
    {
        return new DataGridViewCellStyle
        {
            BackColor = Color.FromArgb(27, 31, 36),
            ForeColor = MainText,
            SelectionBackColor = Color.FromArgb(27, 31, 36),
            SelectionForeColor = MainText,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold, GraphicsUnit.Point)
        };
    }

    private static DataGridViewCellStyle BuildCellStyle()
    {
        return new DataGridViewCellStyle
        {
            BackColor = PanelBackground,
            ForeColor = MainText,
            SelectionBackColor = Color.FromArgb(0, 120, 212),
            SelectionForeColor = Color.White,
            WrapMode = DataGridViewTriState.False
        };
    }

    private static DataGridViewCellStyle BuildAlternatingStyle()
    {
        return new DataGridViewCellStyle
        {
            BackColor = InputBackground,
            ForeColor = MainText,
            SelectionBackColor = Color.FromArgb(0, 120, 212),
            SelectionForeColor = Color.White
        };
    }

    private static DataGridViewTextBoxColumn CreateTextColumn(string headerText, string propertyName)
    {
        return new DataGridViewTextBoxColumn
        {
            HeaderText = headerText,
            DataPropertyName = propertyName,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells,
            MinimumWidth = 110,
            SortMode = DataGridViewColumnSortMode.NotSortable
        };
    }

    private void RefreshData()
    {
        try
        {
            _refreshButton.Enabled = false;
            _refreshButton.Text = "Atualizando...";

            List<SensorReading> sensors = _hardwareMonitor.ReadAllSensors();
            _isFilteringByHardware = _hardwareSelectionService.HasActiveSelection();

            IEnumerable<SensorReading> visibleSensors = _isFilteringByHardware
                ? sensors.Where(sensor => _hardwareSelectionService.IsHardwareSelected(
                    sensor.HardwareType.ToString(),
                    sensor.HardwareName,
                    sensor.HardwareIdentifier))
                : sensors;

            List<RawHardwareRow> rows = visibleSensors
                .OrderBy(sensor => sensor.HardwareType.ToString())
                .ThenBy(sensor => sensor.HardwareName)
                .ThenBy(sensor => sensor.SensorType.ToString())
                .ThenBy(sensor => sensor.SensorName)
                .Select(CreateRow)
                .ToList();

            _grid.DataSource = new BindingList<RawHardwareRow>(rows);
            _lastUpdatedAt = DateTime.Now;
            UpdateFooter(rows.Count);
        }
        catch (Exception ex)
        {
            AppLogService.Error(ex, "Não foi possível carregar os dados brutos do hardware.");
            _grid.DataSource = new BindingList<RawHardwareRow>(new List<RawHardwareRow>());
            _lastUpdatedAt = null;
            UpdateFooter(0);
            MessageBox.Show(
                $"Não foi possível carregar os dados brutos do hardware: {ex.Message}",
                "Dados brutos do hardware",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
        finally
        {
            _refreshButton.Text = "Atualizar";
            _refreshButton.Enabled = true;
        }
    }

    private void UpdateFooter(int totalSensors)
    {
        string filterText = _isFilteringByHardware
            ? $"Filtro por hardware ativo ({_hardwareSelectionService.GetSelectedCount()} selecionados)"
            : "Filtro por hardware inativo";

        string updatedAt = _lastUpdatedAt.HasValue
            ? _lastUpdatedAt.Value.ToString("dd/MM/yyyy HH:mm:ss", CultureInfo.CurrentCulture)
            : "--";

        _footerLabel.Text = $"Sensores exibidos: {totalSensors} | {filterText} | Última atualização: {updatedAt}";
    }

    private static RawHardwareRow CreateRow(SensorReading sensor)
    {
        return new RawHardwareRow
        {
            Hardware = sensor.HardwareName,
            HardwareType = sensor.HardwareType.ToString(),
            SensorType = sensor.SensorType.ToString(),
            SensorName = sensor.SensorName,
            RawValue = FormatRawValue(sensor.Value),
            FormattedValue = FormatFormattedValue(sensor.SensorType, sensor.Value),
            Min = FormatRawValue(sensor.Min),
            Max = FormatRawValue(sensor.Max),
            SensorIdentifier = string.IsNullOrWhiteSpace(sensor.SensorIdentifier) ? "--" : sensor.SensorIdentifier,
            HardwareIdentifier = string.IsNullOrWhiteSpace(sensor.HardwareIdentifier) ? "--" : sensor.HardwareIdentifier
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
        string unit = GetUnit(sensorType);
        return string.IsNullOrWhiteSpace(unit) ? formattedValue : $"{formattedValue} {unit}";
    }

    private static string GetUnit(SensorType sensorType)
    {
        return sensorType switch
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
    }

    private sealed class RawHardwareRow
    {
        public string Hardware { get; set; } = "--";
        public string HardwareType { get; set; } = "--";
        public string SensorType { get; set; } = "--";
        public string SensorName { get; set; } = "--";
        public string RawValue { get; set; } = "--";
        public string FormattedValue { get; set; } = "--";
        public string Min { get; set; } = "--";
        public string Max { get; set; } = "--";
        public string SensorIdentifier { get; set; } = "--";
        public string HardwareIdentifier { get; set; } = "--";
    }
}
