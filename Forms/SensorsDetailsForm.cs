using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using LibreHardwareMonitor.Hardware;

class SensorsDetailsForm : Form
{
    private const string IconHardwareGeneric = "hardware-generic";
    private const string IconCpu = "hardware-cpu";
    private const string IconGpu = "hardware-gpu";
    private const string IconMemory = "hardware-memory";
    private const string IconStorage = "hardware-storage";
    private const string IconBoard = "hardware-board";
    private const string IconFan = "hardware-fan";
    private const string IconBattery = "hardware-battery";
    private const string IconNetwork = "hardware-network";
    private const string IconSensorTemperature = "sensor-temperature";
    private const string IconSensorPower = "sensor-power";
    private const string IconSensorLoad = "sensor-load";
    private const string IconSensorGeneric = "sensor-generic";

    private static readonly Color WindowBackground = Color.FromArgb(17, 19, 22);
    private static readonly Color PanelBackground = Color.FromArgb(24, 27, 31);
    private static readonly Color InputBackground = Color.FromArgb(30, 34, 39);
    private static readonly Color ButtonBackground = Color.FromArgb(36, 41, 47);
    private static readonly Color ButtonBorder = Color.FromArgb(58, 66, 74);
    private static readonly Color MutedText = Color.FromArgb(170, 176, 184);
    private static readonly Color MainText = Color.FromArgb(230, 234, 238);
    private static readonly Color AccentColor = Color.FromArgb(0, 120, 212);
    private static readonly Color WarningColor = Color.FromArgb(255, 185, 0);

    private readonly HardwareMonitorService _hardwareMonitor;
    private readonly Icon? _ownedIcon;
    private readonly TreeView _sensorsTree;
    private readonly Label _statusLabel;
    private readonly Button _refreshButton;
    private readonly Button _expandAllButton;
    private readonly Button _collapseAllButton;
    private readonly TextBox _filterTextBox;
    private readonly ImageList _sensorIcons;
    private List<SensorReading> _allSensors = new();
    private DateTime? _lastUpdatedAt;

    public SensorsDetailsForm(HardwareMonitorService hardwareMonitor, Icon? windowIcon = null)
    {
        _hardwareMonitor = hardwareMonitor ?? throw new ArgumentNullException(nameof(hardwareMonitor));
        _sensorIcons = CreateSensorImageList();

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
            RowCount = 4,
            Padding = new Padding(18),
            BackColor = WindowBackground
        };

        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));

        TableLayoutPanel header = new TableLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = WindowBackground,
            Margin = new Padding(0, 0, 0, 12)
        };

        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        header.RowStyles.Add(new RowStyle(SizeType.AutoSize));

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

        TableLayoutPanel controlsBar = new TableLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1,
            BackColor = WindowBackground,
            Margin = new Padding(0, 0, 0, 12)
        };

        controlsBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        controlsBar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        controlsBar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        controlsBar.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        _filterTextBox = new TextBox
        {
            Dock = DockStyle.Fill,
            AutoSize = false,
            Height = 40,
            MinimumSize = new Size(220, 40),
            Margin = new Padding(0, 0, 8, 0),
            BackColor = InputBackground,
            ForeColor = MainText,
            BorderStyle = BorderStyle.FixedSingle,
            PlaceholderText = "Filtrar por hardware, tipo ou sensor"
        };

        _filterTextBox.TextChanged += (_, _) => ApplySensorFilter();

        _expandAllButton = new Button
        {
            Text = "Expandir tudo"
        };

        ConfigureActionButton(_expandAllButton);
        _expandAllButton.Margin = new Padding(0, 0, 8, 0);

        _collapseAllButton = new Button
        {
            Text = "Recolher tudo"
        };

        ConfigureActionButton(_collapseAllButton);

        controlsBar.Controls.Add(_filterTextBox, 0, 0);
        controlsBar.Controls.Add(_expandAllButton, 1, 0);
        controlsBar.Controls.Add(_collapseAllButton, 2, 0);

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
            DrawMode = TreeViewDrawMode.OwnerDrawText,
            ImageList = _sensorIcons,
            ImageKey = IconHardwareGeneric,
            SelectedImageKey = IconHardwareGeneric
        };

        _sensorsTree.DrawNode += DrawSensorNode;
        _expandAllButton.Click += (_, _) => _sensorsTree.ExpandAll();
        _collapseAllButton.Click += (_, _) => _sensorsTree.CollapseAll();

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
        root.Controls.Add(controlsBar, 0, 1);
        root.Controls.Add(_sensorsTree, 0, 2);
        root.Controls.Add(_statusLabel, 0, 3);

        Controls.Add(root);

        Shown += (_, _) => RefreshSensors();
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
            _sensorsTree.ImageList = null;
            _sensorIcons.Dispose();
            _ownedIcon?.Dispose();
        }

        base.Dispose(disposing);
    }

    private static void ConfigureActionButton(Button button)
    {
        button.AutoSize = true;
        button.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        button.Dock = DockStyle.Fill;
        button.Height = 40;
        button.MinimumSize = new Size(142, 40);
        button.Margin = new Padding(0);
        button.Padding = new Padding(10, 0, 10, 0);
        button.FlatStyle = FlatStyle.Flat;
        button.ForeColor = Color.White;
        button.BackColor = ButtonBackground;
        button.UseVisualStyleBackColor = false;
        button.TextAlign = ContentAlignment.MiddleCenter;
        button.FlatAppearance.BorderColor = ButtonBorder;
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

            _allSensors = _hardwareMonitor.ReadAllSensors();
            _lastUpdatedAt = DateTime.Now;

            ApplySensorFilter();
        }
        catch (Exception ex)
        {
            _allSensors = new List<SensorReading>();
            _lastUpdatedAt = null;
            AppLogService.Error(ex, "Não foi possível atualizar a lista completa de sensores.");
            ShowReadError(ex);
        }
        finally
        {
            _refreshButton.Text = "Atualizar";
            _refreshButton.Enabled = true;
        }
    }

    private void ApplySensorFilter()
    {
        bool hasActiveFilter = !string.IsNullOrWhiteSpace(_filterTextBox.Text);
        List<SensorReading> visibleSensors = GetFilteredSensors(_allSensors, _filterTextBox.Text).ToList();

        PopulateSensorTree(visibleSensors, hasActiveFilter);
        UpdateSuccessStatus(_allSensors.Count, visibleSensors.Count, hasActiveFilter);
    }

    private void PopulateSensorTree(List<SensorReading> sensors, bool hasActiveFilter)
    {
        _sensorsTree.BeginUpdate();

        try
        {
            _sensorsTree.Nodes.Clear();

            if (sensors.Count == 0)
            {
                TreeNode emptyNode = CreateTreeNode(
                    hasActiveFilter ? "Nenhum sensor corresponde ao filtro." : "Nenhum sensor foi encontrado.",
                    MutedText,
                    IconSensorGeneric);

                _sensorsTree.Nodes.Add(emptyNode);
                return;
            }

            var hardwareGroups = sensors
                .GroupBy(sensor => new
                {
                    HardwareType = GetHardwareTypeText(sensor),
                    HardwareName = GetSafeText(sensor.HardwareName, "Hardware sem nome"),
                    HardwareIdentifier = GetSafeText(sensor.HardwareIdentifier, "")
                })
                .OrderBy(group => group.Key.HardwareType)
                .ThenBy(group => group.Key.HardwareName);

            foreach (var hardwareGroup in hardwareGroups)
            {
                SensorReading firstHardwareSensor = hardwareGroup.First();
                string hardwareIconKey = GetHardwareIconKey(firstHardwareSensor.HardwareType);
                string hardwareText =
                    $"{hardwareGroup.Key.HardwareType} - {hardwareGroup.Key.HardwareName} ({hardwareGroup.Count()} sensores)";

                TreeNode hardwareNode = CreateTreeNode(hardwareText, Color.White, hardwareIconKey);
                hardwareNode.ToolTipText =
                    $"Hardware: {hardwareGroup.Key.HardwareName}{Environment.NewLine}" +
                    $"Tipo de hardware: {hardwareGroup.Key.HardwareType}{Environment.NewLine}" +
                    $"Identificador do hardware: {GetSafeText(hardwareGroup.Key.HardwareIdentifier, "não informado")}{Environment.NewLine}" +
                    $"Sensores: {hardwareGroup.Count()}";

                var sensorTypeGroups = hardwareGroup
                    .GroupBy(GetSensorTypeText)
                    .OrderBy(group => group.Key);

                foreach (var sensorTypeGroup in sensorTypeGroups)
                {
                    SensorReading firstSensorType = sensorTypeGroup.First();
                    string sensorTypeIconKey = GetSensorTypeIconKey(firstSensorType.SensorType);

                    TreeNode sensorTypeNode = CreateTreeNode(
                        $"{sensorTypeGroup.Key} ({sensorTypeGroup.Count()})",
                        Color.FromArgb(210, 214, 220),
                        sensorTypeIconKey);

                    sensorTypeNode.ToolTipText = $"Tipo de sensor: {sensorTypeGroup.Key}";

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
        string sensorIconKey = GetSensorTypeIconKey(sensor.SensorType);

        TreeNode node = CreateTreeNode(
            $"{sensorName}: {value}",
            sensor.Value.HasValue ? MainText : MutedText,
            sensorIconKey);

        node.ToolTipText =
            $"Hardware: {hardwareName}{Environment.NewLine}" +
            $"Tipo de hardware: {GetHardwareTypeText(sensor)}{Environment.NewLine}" +
            $"Tipo de sensor: {sensorType}{Environment.NewLine}" +
            $"Valor: {value}{Environment.NewLine}" +
            $"Mínimo: {FormatSensorValue(sensor.SensorType, sensor.Min)}{Environment.NewLine}" +
            $"Máximo: {FormatSensorValue(sensor.SensorType, sensor.Max)}{Environment.NewLine}" +
            $"Identificador do sensor: {GetSafeText(sensor.SensorIdentifier, "não informado")}";

        return node;
    }

    private void UpdateSuccessStatus(int totalSensorCount, int visibleSensorCount, bool hasActiveFilter)
    {
        string updatedAt = (_lastUpdatedAt ?? DateTime.Now).ToString("dd/MM/yyyy HH:mm:ss", CultureInfo.CurrentCulture);

        _statusLabel.ForeColor = MutedText;

        if (totalSensorCount == 0)
        {
            _statusLabel.Text = $"Nenhum sensor encontrado. Última atualização: {updatedAt}";
            return;
        }

        _statusLabel.Text = hasActiveFilter
            ? $"Exibindo {visibleSensorCount} de {totalSensorCount} sensores | Última atualização: {updatedAt}"
            : $"Total de sensores: {totalSensorCount} | Última atualização: {updatedAt}";
    }

    private void ShowReadError(Exception exception)
    {
        _sensorsTree.BeginUpdate();

        try
        {
            _sensorsTree.Nodes.Clear();

            TreeNode errorNode = CreateTreeNode("Não foi possível ler os sensores.", WarningColor, IconSensorGeneric);
            errorNode.ToolTipText = GetSafeText(exception.Message, "Erro desconhecido.");

            errorNode.Nodes.Add(CreateTreeNode(
                "Tente atualizar novamente ou executar o app como administrador.",
                MutedText,
                IconSensorGeneric));

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
            TextFormatFlags.Left |
            TextFormatFlags.VerticalCenter |
            TextFormatFlags.NoPrefix |
            TextFormatFlags.EndEllipsis);
    }

    private static TreeNode CreateTreeNode(string text, Color foreColor, string imageKey)
    {
        return new TreeNode(text)
        {
            ForeColor = foreColor,
            ImageKey = imageKey,
            SelectedImageKey = imageKey
        };
    }

    private static IEnumerable<SensorReading> GetFilteredSensors(IEnumerable<SensorReading> sensors, string filterText)
    {
        string filter = filterText.Trim();

        if (filter.Length == 0)
        {
            return sensors;
        }

        return sensors.Where(sensor => ContainsFilter(GetSensorSearchText(sensor), filter));
    }

    private static string GetSensorSearchText(SensorReading sensor)
    {
        string[] parts =
        {
            GetSafeText(sensor.HardwareName, ""),
            GetHardwareTypeText(sensor),
            GetSafeText(sensor.HardwareIdentifier, ""),
            GetHardwareSearchAlias(sensor.HardwareType),
            GetSensorTypeText(sensor),
            GetSensorTypeSearchAlias(sensor.SensorType),
            GetSensorNameText(sensor),
            GetSafeText(sensor.SensorIdentifier, "")
        };

        return string.Join(" ", parts);
    }

    private static bool ContainsFilter(string text, string filter)
    {
        return text.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static string GetHardwareIconKey(HardwareType hardwareType)
    {
        if (hardwareType.ToString().StartsWith("Gpu", StringComparison.OrdinalIgnoreCase))
        {
            return IconGpu;
        }

        return hardwareType switch
        {
            HardwareType.Cpu => IconCpu,
            HardwareType.Memory => IconMemory,
            HardwareType.Storage => IconStorage,
            HardwareType.Motherboard or HardwareType.SuperIO or HardwareType.EmbeddedController => IconBoard,
            HardwareType.Cooler => IconFan,
            HardwareType.Battery => IconBattery,
            HardwareType.Network => IconNetwork,
            _ => IconHardwareGeneric
        };
    }

    private static string GetSensorTypeIconKey(SensorType sensorType)
    {
        return sensorType switch
        {
            SensorType.Temperature => IconSensorTemperature,
            SensorType.Fan => IconFan,
            SensorType.Power or SensorType.Voltage or SensorType.Current or SensorType.Energy => IconSensorPower,
            SensorType.Load or SensorType.Control or SensorType.Level => IconSensorLoad,
            SensorType.Data or SensorType.SmallData => IconStorage,
            SensorType.Throughput => IconNetwork,
            SensorType.Clock or SensorType.Frequency or SensorType.Timing => IconCpu,
            _ => IconSensorGeneric
        };
    }

    private static string GetHardwareSearchAlias(HardwareType hardwareType)
    {
        if (hardwareType.ToString().StartsWith("Gpu", StringComparison.OrdinalIgnoreCase))
        {
            return "GPU vídeo video placa de vídeo placa de video";
        }

        return hardwareType switch
        {
            HardwareType.Cpu => "CPU processador chip",
            HardwareType.Memory => "RAM memória memoria memory",
            HardwareType.Storage => "SSD HDD disco storage nvme sata",
            HardwareType.Motherboard or HardwareType.SuperIO or HardwareType.EmbeddedController => "placa motherboard mainboard superio placa-mãe placa mae",
            HardwareType.Cooler => "fan ventoinha cooler",
            HardwareType.Battery => "bateria battery",
            HardwareType.Network => "rede network ethernet wifi wi-fi",
            HardwareType.Psu or HardwareType.PowerMonitor => "fonte energia potência potencia power",
            _ => "hardware"
        };
    }

    private static string GetSensorTypeSearchAlias(SensorType sensorType)
    {
        return sensorType switch
        {
            SensorType.Temperature => "temperatura temperature temp",
            SensorType.Fan => "fan ventoinha cooler rpm",
            SensorType.Power => "potência potencia power watts",
            SensorType.Voltage => "tensão tensao voltage volts",
            SensorType.Current => "corrente current ampere",
            SensorType.Clock or SensorType.Frequency => "clock frequência frequencia",
            SensorType.Load => "uso carga load",
            SensorType.Control => "controle control",
            SensorType.Level => "nível nivel level bateria battery",
            SensorType.Data or SensorType.SmallData => "dados memória memoria disco data",
            SensorType.Throughput => "rede tráfego trafego throughput download upload",
            SensorType.TimeSpan => "tempo time",
            SensorType.Timing => "latência latencia timing",
            SensorType.Energy => "energia energy",
            SensorType.Noise => "ruído ruido noise",
            SensorType.Conductivity => "condutividade conductivity",
            SensorType.Humidity => "umidade humidity",
            _ => ""
        };
    }

    private static ImageList CreateSensorImageList()
    {
        ImageList imageList = new ImageList
        {
            ColorDepth = ColorDepth.Depth32Bit,
            ImageSize = new Size(16, 16),
            TransparentColor = Color.Transparent
        };

        imageList.Images.Add(IconHardwareGeneric, CreateIcon(Color.FromArgb(170, 176, 184), DrawGenericHardwareIcon));
        imageList.Images.Add(IconCpu, CreateIcon(Color.FromArgb(67, 190, 255), DrawCpuIcon));
        imageList.Images.Add(IconGpu, CreateIcon(Color.FromArgb(126, 211, 33), DrawGpuIcon));
        imageList.Images.Add(IconMemory, CreateIcon(Color.FromArgb(198, 147, 255), DrawMemoryIcon));
        imageList.Images.Add(IconStorage, CreateIcon(Color.FromArgb(255, 193, 84), DrawStorageIcon));
        imageList.Images.Add(IconBoard, CreateIcon(Color.FromArgb(84, 214, 187), DrawBoardIcon));
        imageList.Images.Add(IconFan, CreateIcon(Color.FromArgb(144, 202, 249), DrawFanIcon));
        imageList.Images.Add(IconBattery, CreateIcon(Color.FromArgb(139, 224, 128), DrawBatteryIcon));
        imageList.Images.Add(IconNetwork, CreateIcon(Color.FromArgb(86, 156, 214), DrawNetworkIcon));
        imageList.Images.Add(IconSensorTemperature, CreateIcon(Color.FromArgb(255, 112, 112), DrawTemperatureIcon));
        imageList.Images.Add(IconSensorPower, CreateIcon(Color.FromArgb(255, 214, 102), DrawPowerIcon));
        imageList.Images.Add(IconSensorLoad, CreateIcon(Color.FromArgb(0, 188, 212), DrawLoadIcon));
        imageList.Images.Add(IconSensorGeneric, CreateIcon(Color.FromArgb(190, 196, 205), DrawSensorGenericIcon));

        return imageList;
    }

    private static Bitmap CreateIcon(Color accent, Action<Graphics, Rectangle, Color> drawIcon)
    {
        Bitmap bitmap = new Bitmap(16, 16);

        using Graphics graphics = Graphics.FromImage(bitmap);
        graphics.Clear(Color.Transparent);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.PixelOffsetMode = PixelOffsetMode.Half;

        drawIcon(graphics, new Rectangle(1, 1, 14, 14), accent);

        return bitmap;
    }

    private static void DrawGenericHardwareIcon(Graphics graphics, Rectangle bounds, Color accent)
    {
        using SolidBrush fillBrush = new SolidBrush(Color.FromArgb(125, accent));
        using Pen borderPen = new Pen(Color.FromArgb(230, accent), 1);
        using Pen linePen = new Pen(Color.FromArgb(130, MainText), 1);

        graphics.FillRectangle(fillBrush, 3, 3, 10, 10);
        graphics.DrawRectangle(borderPen, 3, 3, 10, 10);
        graphics.DrawLine(linePen, 5, 6, 11, 6);
        graphics.DrawLine(linePen, 5, 9, 11, 9);
    }

    private static void DrawCpuIcon(Graphics graphics, Rectangle bounds, Color accent)
    {
        using Pen pinPen = new Pen(Color.FromArgb(170, accent), 1);
        using SolidBrush fillBrush = new SolidBrush(Color.FromArgb(160, accent));
        using Pen borderPen = new Pen(Color.FromArgb(240, accent), 1);
        using SolidBrush coreBrush = new SolidBrush(Color.FromArgb(230, PanelBackground));

        for (int offset = 4; offset <= 12; offset += 4)
        {
            graphics.DrawLine(pinPen, offset, 1, offset, 4);
            graphics.DrawLine(pinPen, offset, 12, offset, 15);
            graphics.DrawLine(pinPen, 1, offset, 4, offset);
            graphics.DrawLine(pinPen, 12, offset, 15, offset);
        }

        graphics.FillRectangle(fillBrush, 4, 4, 8, 8);
        graphics.DrawRectangle(borderPen, 4, 4, 8, 8);
        graphics.FillRectangle(coreBrush, 6, 6, 4, 4);
    }

    private static void DrawGpuIcon(Graphics graphics, Rectangle bounds, Color accent)
    {
        using SolidBrush fillBrush = new SolidBrush(Color.FromArgb(155, accent));
        using Pen borderPen = new Pen(Color.FromArgb(235, accent), 1);
        using SolidBrush fanBrush = new SolidBrush(Color.FromArgb(220, PanelBackground));

        graphics.FillRectangle(fillBrush, 2, 5, 11, 7);
        graphics.DrawRectangle(borderPen, 2, 5, 11, 7);
        graphics.FillRectangle(fillBrush, 13, 7, 2, 3);
        graphics.FillEllipse(fanBrush, 5, 6, 5, 5);
        graphics.DrawEllipse(borderPen, 5, 6, 5, 5);
    }

    private static void DrawMemoryIcon(Graphics graphics, Rectangle bounds, Color accent)
    {
        using SolidBrush fillBrush = new SolidBrush(Color.FromArgb(150, accent));
        using Pen borderPen = new Pen(Color.FromArgb(235, accent), 1);
        using SolidBrush chipBrush = new SolidBrush(Color.FromArgb(220, PanelBackground));

        graphics.FillRectangle(fillBrush, 1, 5, 14, 6);
        graphics.DrawRectangle(borderPen, 1, 5, 14, 6);

        for (int x = 3; x <= 11; x += 4)
        {
            graphics.FillRectangle(chipBrush, x, 6, 2, 4);
        }
    }

    private static void DrawStorageIcon(Graphics graphics, Rectangle bounds, Color accent)
    {
        using SolidBrush fillBrush = new SolidBrush(Color.FromArgb(150, accent));
        using Pen borderPen = new Pen(Color.FromArgb(235, accent), 1);
        using SolidBrush detailBrush = new SolidBrush(Color.FromArgb(230, PanelBackground));

        graphics.FillRectangle(fillBrush, 3, 2, 10, 12);
        graphics.DrawRectangle(borderPen, 3, 2, 10, 12);
        graphics.FillEllipse(detailBrush, 6, 5, 4, 4);
        graphics.FillRectangle(detailBrush, 5, 11, 6, 1);
    }

    private static void DrawBoardIcon(Graphics graphics, Rectangle bounds, Color accent)
    {
        using SolidBrush fillBrush = new SolidBrush(Color.FromArgb(130, accent));
        using Pen borderPen = new Pen(Color.FromArgb(235, accent), 1);
        using Pen circuitPen = new Pen(Color.FromArgb(180, MainText), 1);

        graphics.FillRectangle(fillBrush, 2, 2, 12, 12);
        graphics.DrawRectangle(borderPen, 2, 2, 12, 12);
        graphics.DrawRectangle(circuitPen, 5, 5, 4, 4);
        graphics.DrawLine(circuitPen, 9, 7, 12, 7);
        graphics.DrawLine(circuitPen, 7, 9, 7, 12);
    }

    private static void DrawFanIcon(Graphics graphics, Rectangle bounds, Color accent)
    {
        using SolidBrush bladeBrush = new SolidBrush(Color.FromArgb(175, accent));
        using Pen borderPen = new Pen(Color.FromArgb(230, accent), 1);
        using SolidBrush centerBrush = new SolidBrush(Color.FromArgb(240, PanelBackground));

        graphics.DrawEllipse(borderPen, 2, 2, 12, 12);
        graphics.FillPie(bladeBrush, 3, 3, 10, 10, -80, 70);
        graphics.FillPie(bladeBrush, 3, 3, 10, 10, 40, 70);
        graphics.FillPie(bladeBrush, 3, 3, 10, 10, 160, 70);
        graphics.FillEllipse(centerBrush, 6, 6, 4, 4);
        graphics.DrawEllipse(borderPen, 6, 6, 4, 4);
    }

    private static void DrawBatteryIcon(Graphics graphics, Rectangle bounds, Color accent)
    {
        using SolidBrush fillBrush = new SolidBrush(Color.FromArgb(150, accent));
        using Pen borderPen = new Pen(Color.FromArgb(235, accent), 1);

        graphics.DrawRectangle(borderPen, 2, 4, 10, 8);
        graphics.FillRectangle(fillBrush, 4, 6, 6, 4);
        graphics.FillRectangle(fillBrush, 12, 7, 2, 2);
    }

    private static void DrawNetworkIcon(Graphics graphics, Rectangle bounds, Color accent)
    {
        using Pen linePen = new Pen(Color.FromArgb(210, accent), 1);
        using SolidBrush dotBrush = new SolidBrush(Color.FromArgb(185, accent));

        graphics.DrawLine(linePen, 8, 5, 4, 10);
        graphics.DrawLine(linePen, 8, 5, 12, 10);
        graphics.FillEllipse(dotBrush, 6, 2, 4, 4);
        graphics.FillEllipse(dotBrush, 2, 9, 4, 4);
        graphics.FillEllipse(dotBrush, 10, 9, 4, 4);
    }

    private static void DrawTemperatureIcon(Graphics graphics, Rectangle bounds, Color accent)
    {
        using Pen borderPen = new Pen(Color.FromArgb(235, accent), 2);
        using SolidBrush bulbBrush = new SolidBrush(Color.FromArgb(170, accent));

        graphics.DrawLine(borderPen, 8, 2, 8, 10);
        graphics.FillEllipse(bulbBrush, 5, 9, 6, 6);
        graphics.DrawEllipse(borderPen, 5, 9, 6, 6);
    }

    private static void DrawPowerIcon(Graphics graphics, Rectangle bounds, Color accent)
    {
        using SolidBrush fillBrush = new SolidBrush(Color.FromArgb(190, accent));
        Point[] points =
        {
            new Point(9, 1),
            new Point(4, 8),
            new Point(8, 8),
            new Point(6, 15),
            new Point(12, 6),
            new Point(8, 6)
        };

        graphics.FillPolygon(fillBrush, points);
    }

    private static void DrawLoadIcon(Graphics graphics, Rectangle bounds, Color accent)
    {
        using Pen borderPen = new Pen(Color.FromArgb(230, accent), 1);
        using Pen needlePen = new Pen(Color.FromArgb(230, MainText), 1);
        using SolidBrush centerBrush = new SolidBrush(Color.FromArgb(180, accent));

        graphics.DrawArc(borderPen, 3, 4, 10, 10, 180, 180);
        graphics.DrawLine(needlePen, 8, 10, 12, 7);
        graphics.FillEllipse(centerBrush, 6, 9, 4, 4);
    }

    private static void DrawSensorGenericIcon(Graphics graphics, Rectangle bounds, Color accent)
    {
        using SolidBrush fillBrush = new SolidBrush(Color.FromArgb(150, accent));
        using Pen linePen = new Pen(Color.FromArgb(210, accent), 1);

        graphics.FillEllipse(fillBrush, 6, 2, 4, 4);
        graphics.FillEllipse(fillBrush, 6, 10, 4, 4);
        graphics.DrawLine(linePen, 8, 6, 8, 10);
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
