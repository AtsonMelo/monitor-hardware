using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using LibreHardwareMonitor.Hardware;

class SensorsDetailsForm : Form
{
    private static readonly Color WindowBackground = Color.FromArgb(17, 19, 22);
    private static readonly Color PanelBackground = Color.FromArgb(24, 27, 31);
    private static readonly Color MutedText = Color.FromArgb(170, 176, 184);
    private static readonly Color MainText = Color.FromArgb(230, 234, 238);
    private static readonly Color AccentColor = Color.FromArgb(0, 120, 212);
    private static readonly Color WarningColor = Color.FromArgb(255, 185, 0);

    private readonly HardwareMonitorService _hardwareMonitor;
    private readonly Icon? _ownedIcon;
    private readonly TreeView _sensorsTree;
    private readonly Label _statusLabel;
    private readonly Button _refreshButton;

    public SensorsDetailsForm(HardwareMonitorService hardwareMonitor, Icon? windowIcon = null)
    {
        _hardwareMonitor = hardwareMonitor ?? throw new ArgumentNullException(nameof(hardwareMonitor));

        Text = "Conferir todos os sensores";
        AutoScaleMode = AutoScaleMode.Dpi;
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(760, 520);
        Size = new Size(980, 680);
        BackColor = WindowBackground;
        ForeColor = Color.White;
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
            RowCount = 3,
            Padding = new Padding(18),
            BackColor = WindowBackground
        };

        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));

        TableLayoutPanel header = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = WindowBackground,
            Margin = new Padding(0, 0, 0, 12)
        };

        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
        header.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        Label titleLabel = new Label
        {
            Text = "Conferir todos os sensores",
            Dock = DockStyle.Fill,
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 15, FontStyle.Bold, GraphicsUnit.Point),
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = true
        };

        _refreshButton = new Button
        {
            Text = "Atualizar"
        };

        ConfigureActionButton(_refreshButton);
        _refreshButton.Click += (_, _) => RefreshSensors();

        header.Controls.Add(titleLabel, 0, 0);
        header.Controls.Add(_refreshButton, 1, 0);

        _sensorsTree = new TreeView
        {
            Dock = DockStyle.Fill,
            BackColor = PanelBackground,
            ForeColor = MainText,
            BorderStyle = BorderStyle.FixedSingle,
            HideSelection = false,
            ShowNodeToolTips = true,
            FullRowSelect = true,
            ItemHeight = 24,
            PathSeparator = " > ",
            DrawMode = TreeViewDrawMode.OwnerDrawText
        };

        _sensorsTree.DrawNode += DrawSensorNode;

        _statusLabel = new Label
        {
            Text = "Aguardando leitura dos sensores...",
            Dock = DockStyle.Fill,
            ForeColor = MutedText,
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = true,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Regular, GraphicsUnit.Point)
        };

        root.Controls.Add(header, 0, 0);
        root.Controls.Add(_sensorsTree, 0, 1);
        root.Controls.Add(_statusLabel, 0, 2);

        Controls.Add(root);

        Shown += (_, _) => RefreshSensors();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            _ownedIcon?.Dispose();
        }
    }

    private static void ConfigureActionButton(Button button)
    {
        button.Dock = DockStyle.Fill;
        button.Height = 38;
        button.MinimumSize = new Size(130, 38);
        button.FlatStyle = FlatStyle.Flat;
        button.ForeColor = Color.White;
        button.BackColor = Color.FromArgb(36, 41, 47);
        button.UseVisualStyleBackColor = false;
        button.TextAlign = ContentAlignment.MiddleCenter;
        button.FlatAppearance.BorderColor = Color.FromArgb(58, 66, 74);
        button.FlatAppearance.MouseOverBackColor = Color.FromArgb(44, 50, 57);
        button.FlatAppearance.MouseDownBackColor = Color.FromArgb(30, 34, 39);
    }

    private void RefreshSensors()
    {
        try
        {
            _refreshButton.Enabled = false;
            _refreshButton.Text = "Atualizando...";
            _statusLabel.ForeColor = MutedText;
            _statusLabel.Text = "Atualizando sensores...";

            List<SensorReading> sensors = _hardwareMonitor.ReadAllSensors();

            PopulateSensorTree(sensors);
            UpdateSuccessStatus(sensors.Count);
        }
        catch (Exception ex)
        {
            AppLogService.Error(ex, "Não foi possível atualizar a lista completa de sensores.");
            ShowReadError(ex);
        }
        finally
        {
            _refreshButton.Text = "Atualizar";
            _refreshButton.Enabled = true;
        }
    }

    private void PopulateSensorTree(List<SensorReading> sensors)
    {
        _sensorsTree.BeginUpdate();

        try
        {
            _sensorsTree.Nodes.Clear();

            if (sensors.Count == 0)
            {
                TreeNode emptyNode = new TreeNode("Nenhum sensor foi encontrado.")
                {
                    ForeColor = MutedText
                };

                _sensorsTree.Nodes.Add(emptyNode);
                return;
            }

            var hardwareGroups = sensors
                .GroupBy(sensor => new
                {
                    HardwareType = GetHardwareTypeText(sensor),
                    HardwareName = GetSafeText(sensor.HardwareName, "Hardware sem nome")
                })
                .OrderBy(group => group.Key.HardwareType)
                .ThenBy(group => group.Key.HardwareName);

            foreach (var hardwareGroup in hardwareGroups)
            {
                string hardwareText =
                    $"{hardwareGroup.Key.HardwareType} - {hardwareGroup.Key.HardwareName} ({hardwareGroup.Count()} sensores)";

                TreeNode hardwareNode = new TreeNode(hardwareText)
                {
                    ForeColor = Color.White,
                    ToolTipText = $"Hardware: {hardwareGroup.Key.HardwareName}"
                };

                var sensorTypeGroups = hardwareGroup
                    .GroupBy(GetSensorTypeText)
                    .OrderBy(group => group.Key);

                foreach (var sensorTypeGroup in sensorTypeGroups)
                {
                    TreeNode sensorTypeNode = new TreeNode($"{sensorTypeGroup.Key} ({sensorTypeGroup.Count()})")
                    {
                        ForeColor = Color.FromArgb(210, 214, 220)
                    };

                    foreach (SensorReading sensor in sensorTypeGroup.OrderBy(GetSensorNameText))
                    {
                        sensorTypeNode.Nodes.Add(CreateSensorNode(sensor));
                    }

                    hardwareNode.Nodes.Add(sensorTypeNode);
                }

                _sensorsTree.Nodes.Add(hardwareNode);
            }

            _sensorsTree.ExpandAll();
        }
        finally
        {
            _sensorsTree.EndUpdate();
        }
    }

    private TreeNode CreateSensorNode(SensorReading sensor)
    {
        string hardwareName = GetSafeText(sensor.HardwareName, "Hardware sem nome");
        string sensorName = GetSensorNameText(sensor);
        string sensorType = GetSensorTypeText(sensor);
        string value = FormatSensorValue(sensor.SensorType, sensor.Value);

        TreeNode node = new TreeNode($"{sensorName}: {value} | Hardware: {hardwareName}")
        {
            ForeColor = sensor.Value.HasValue ? MainText : MutedText,
            ToolTipText =
                $"Hardware: {hardwareName}{Environment.NewLine}" +
                $"Tipo de hardware: {GetHardwareTypeText(sensor)}{Environment.NewLine}" +
                $"Tipo de sensor: {sensorType}{Environment.NewLine}" +
                $"Valor: {value}{Environment.NewLine}" +
                $"Mín: {FormatSensorValue(sensor.SensorType, sensor.Min)}{Environment.NewLine}" +
                $"Máx: {FormatSensorValue(sensor.SensorType, sensor.Max)}{Environment.NewLine}" +
                $"ID do sensor: {GetSafeText(sensor.SensorIdentifier, "não informado")}"
        };

        return node;
    }

    private void UpdateSuccessStatus(int sensorCount)
    {
        string updatedAt = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss", CultureInfo.CurrentCulture);

        _statusLabel.ForeColor = MutedText;
        _statusLabel.Text = sensorCount == 0
            ? $"Nenhum sensor encontrado. Última atualização: {updatedAt}"
            : $"Total de sensores: {sensorCount} | Última atualização: {updatedAt}";
    }

    private void ShowReadError(Exception exception)
    {
        _sensorsTree.BeginUpdate();

        try
        {
            _sensorsTree.Nodes.Clear();

            TreeNode errorNode = new TreeNode("Não foi possível ler os sensores.")
            {
                ForeColor = WarningColor
            };

            errorNode.Nodes.Add(new TreeNode("Tente atualizar novamente ou executar o app como administrador.")
            {
                ForeColor = MutedText
            });

            _sensorsTree.Nodes.Add(errorNode);
            errorNode.Expand();
        }
        finally
        {
            _sensorsTree.EndUpdate();
        }

        string message = string.IsNullOrWhiteSpace(exception.Message)
            ? "Erro desconhecido."
            : exception.Message;

        _statusLabel.ForeColor = WarningColor;
        _statusLabel.Text = $"Não foi possível ler os sensores: {message}";
    }

    private void DrawSensorNode(object? sender, DrawTreeNodeEventArgs e)
    {
        if (e.Node == null)
        {
            return;
        }

        bool selected = (e.State & TreeNodeStates.Selected) == TreeNodeStates.Selected;
        Color backColor = selected ? AccentColor : _sensorsTree.BackColor;
        Color textColor = selected
            ? Color.White
            : e.Node.ForeColor.IsEmpty
                ? _sensorsTree.ForeColor
                : e.Node.ForeColor;

        Rectangle textBounds = new Rectangle(
            e.Bounds.X,
            e.Bounds.Y,
            Math.Max(0, _sensorsTree.ClientSize.Width - e.Bounds.X),
            e.Bounds.Height);

        using SolidBrush backBrush = new SolidBrush(backColor);
        e.Graphics.FillRectangle(backBrush, textBounds);

        TextRenderer.DrawText(
            e.Graphics,
            e.Node.Text,
            e.Node.NodeFont ?? _sensorsTree.Font,
            textBounds,
            textColor,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
    }

    private static string GetHardwareTypeText(SensorReading sensor)
    {
        string hardwareType = sensor.HardwareType.ToString();
        return GetSafeText(hardwareType, "Tipo de hardware desconhecido");
    }

    private static string GetSensorTypeText(SensorReading sensor)
    {
        string sensorType = sensor.SensorType.ToString();
        return GetSafeText(sensorType, "Tipo de sensor desconhecido");
    }

    private static string GetSensorNameText(SensorReading sensor)
    {
        return GetSafeText(sensor.SensorName, "Sensor sem nome");
    }

    private static string FormatSensorValue(SensorType sensorType, float? value)
    {
        if (!value.HasValue)
        {
            return "--";
        }

        string formattedValue = value.Value.ToString("0.###", CultureInfo.CurrentCulture);
        string unit = GetSensorUnit(sensorType);

        return string.IsNullOrWhiteSpace(unit)
            ? formattedValue
            : $"{formattedValue} {unit}";
    }

    private static string GetSensorUnit(SensorType sensorType)
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

    private static string GetSafeText(string? text, string fallback)
    {
        return string.IsNullOrWhiteSpace(text)
            ? fallback
            : text.Trim();
    }
}
