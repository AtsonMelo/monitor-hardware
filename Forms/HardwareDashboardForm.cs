using System;
using System.Collections.Generic;
using System.Drawing;
using System.Security.Principal;
using System.Windows.Forms;

class HardwareDashboardForm : Form
{
    private readonly AppConfig _config;
    private readonly HardwareMonitorService _hardwareMonitor;
    private readonly SnapshotService _snapshotService;
    private readonly CsvLoggerService _csvLogger;
    private readonly System.Windows.Forms.Timer _timer;

    private Label _updatedAtLabel = null!;
    private Label _statusLabel = null!;
    private MetricCard _cpuCard = null!;
    private MetricCard _gpuCard = null!;
    private MetricCard _ramCard = null!;
    private MetricCard _ssdCard = null!;

    public HardwareDashboardForm(AppConfig config)
    {
        _config = config;
        _hardwareMonitor = new HardwareMonitorService();
        _snapshotService = new SnapshotService(config);
        _csvLogger = new CsvLoggerService();
        _timer = new System.Windows.Forms.Timer
        {
            Interval = Math.Max(500, config.IntervaloMs)
        };

        Text = "Monitor Hardware";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(760, 480);
        Size = new Size(900, 560);
        BackColor = Color.FromArgb(17, 19, 22);
        ForeColor = Color.White;
        Font = new Font("Segoe UI", 10, FontStyle.Regular, GraphicsUnit.Point);

        BuildLayout();

        _timer.Tick += (_, _) => RefreshSnapshot();
        Shown += (_, _) =>
        {
            RefreshSnapshot();
            _timer.Start();
        };
        FormClosed += (_, _) => _timer.Dispose();
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

        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 72));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));

        Panel header = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = BackColor
        };

        Label titleLabel = new Label
        {
            Text = "Monitor Hardware",
            Dock = DockStyle.Top,
            Height = 34,
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 18, FontStyle.Bold, GraphicsUnit.Point)
        };

        _updatedAtLabel = new Label
        {
            Text = "Atualizando...",
            Dock = DockStyle.Top,
            Height = 26,
            ForeColor = Color.FromArgb(170, 176, 184),
            Font = new Font("Segoe UI", 10, FontStyle.Regular, GraphicsUnit.Point)
        };

        header.Controls.Add(_updatedAtLabel);
        header.Controls.Add(titleLabel);

        TableLayoutPanel cardsGrid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 2,
            BackColor = BackColor
        };

        cardsGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        cardsGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        cardsGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        cardsGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 50));

        _cpuCard = new MetricCard("CPU");
        _gpuCard = new MetricCard("GPU");
        _ramCard = new MetricCard("Memória RAM");
        _ssdCard = new MetricCard("SSD");

        cardsGrid.Controls.Add(_cpuCard, 0, 0);
        cardsGrid.Controls.Add(_gpuCard, 1, 0);
        cardsGrid.Controls.Add(_ramCard, 0, 1);
        cardsGrid.Controls.Add(_ssdCard, 1, 1);

        _statusLabel = new Label
        {
            Text = "Nenhum alerta crítico.",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = Color.FromArgb(170, 176, 184),
            Font = new Font("Segoe UI", 10, FontStyle.Bold, GraphicsUnit.Point)
        };

        root.Controls.Add(header, 0, 0);
        root.Controls.Add(cardsGrid, 0, 1);
        root.Controls.Add(_statusLabel, 0, 2);

        Controls.Add(root);
    }

    private void RefreshSnapshot()
    {
        try
        {
            List<SensorReading> sensors = _hardwareMonitor.ReadAllSensors();
            MonitorSnapshot snapshot = _snapshotService.Create(sensors);

            if (_config.EnableCsv)
            {
                _csvLogger.Save(snapshot);
            }

            UpdateCards(snapshot);
            _updatedAtLabel.Text = $"Atualizado em {snapshot.Timestamp:dd/MM/yyyy HH:mm:ss}";
            _statusLabel.Text = GetStatusText(snapshot);
            _statusLabel.ForeColor = GetStatusColor(snapshot);
        }
        catch (Exception ex)
        {
            _statusLabel.Text = $"Não foi possível ler os sensores: {ex.Message}";
            _statusLabel.ForeColor = Color.FromArgb(255, 185, 0);
        }
    }

    private void UpdateCards(MonitorSnapshot snapshot)
    {
        _cpuCard.SetValues(
            GetCpuPrimaryText(snapshot),
            GetCpuSecondaryText(snapshot),
            GetCpuAccentColor(snapshot));

        _gpuCard.SetValues(
            FormatTemperature(snapshot.GpuTemp),
            $"Uso {FormatPercent(snapshot.GpuUso)} | Potência {FormatPower(snapshot.GpuPower)}",
            GetTemperatureColor(snapshot.GpuTemp, _config.GpuTempMax));

        _ramCard.SetValues(
            FormatPercent(snapshot.RamUso),
            "Uso da memória física",
            GetLoadColor(snapshot.RamUso));

        _ssdCard.SetValues(
            FormatTemperature(snapshot.SsdTemp),
            $"Limite configurado {FormatTemperature(_config.SsdTempMax)}",
            GetTemperatureColor(snapshot.SsdTemp, _config.SsdTempMax));
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
            : "-- RPM";
    }

    private static string GetCpuTemperatureUnavailableText()
    {
        return IsRunningAsAdministrator()
            ? "Temperatura indisponível pelo sensor atual"
            : "Temperatura indisponível; execute como administrador";
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
    private readonly Label _titleLabel;
    private readonly Label _primaryLabel;
    private readonly Label _secondaryLabel;

    public MetricCard(string title)
    {
        Dock = DockStyle.Fill;
        Margin = new Padding(8);
        Padding = new Padding(18);
        BackColor = Color.FromArgb(28, 31, 35);

        _titleLabel = new Label
        {
            Text = title,
            Dock = DockStyle.Top,
            Height = 28,
            ForeColor = Color.FromArgb(210, 214, 220),
            Font = new Font("Segoe UI", 11, FontStyle.Bold, GraphicsUnit.Point)
        };

        _primaryLabel = new Label
        {
            Text = "--",
            Dock = DockStyle.Top,
            Height = 64,
            ForeColor = SystemColors.Highlight,
            Font = new Font("Segoe UI", 28, FontStyle.Bold, GraphicsUnit.Point)
        };

        _secondaryLabel = new Label
        {
            Text = "Aguardando leitura",
            Dock = DockStyle.Fill,
            ForeColor = Color.FromArgb(170, 176, 184),
            Font = new Font("Segoe UI", 10, FontStyle.Regular, GraphicsUnit.Point)
        };

        Controls.Add(_secondaryLabel);
        Controls.Add(_primaryLabel);
        Controls.Add(_titleLabel);
    }

    public void SetValues(string primary, string secondary, Color accent)
    {
        _primaryLabel.Text = primary;
        _primaryLabel.ForeColor = accent;
        _secondaryLabel.Text = secondary;
    }
}
