using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Security.Principal;
using System.Windows.Forms;

class HardwareDashboardForm : Form
{
    private readonly AppConfig _config;
    private readonly SnapshotService _snapshotService;
    private readonly CsvLoggerService _csvLogger;
    private readonly UpdateService _updateService;
    private readonly StartupTaskService _startupTaskService;
    private readonly System.Windows.Forms.Timer _timer;
    private readonly Icon _windowIcon;

    private Label _updatedAtLabel = null!;
    private Label _statusLabel = null!;
    private Button _updateButton = null!;
    private Button _errorReportButton = null!;
    private Button _helpButton = null!;
    private CheckBox _startupCheckBox = null!;
    private ContextMenuStrip _helpMenu = null!;
    private MetricCard _cpuCard = null!;
    private MetricCard _gpuCard = null!;
    private MetricCard _ramCard = null!;
    private MetricCard _ssdCard = null!;
    private HardwareMonitorService? _hardwareMonitor;

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

        Text = "Monitor Hardware";
        AutoScaleMode = AutoScaleMode.Dpi;
        _windowIcon = AppIconService.Load();
        Icon = _windowIcon;
        ShowInTaskbar = true;
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(1080, 720);
        Size = GetInitialWindowSize();
        AutoScroll = true;
        BackColor = Color.FromArgb(17, 19, 22);
        ForeColor = Color.White;
        Font = new Font("Segoe UI", 10, FontStyle.Regular, GraphicsUnit.Point);

        BuildLayout();

        _timer.Tick += (_, _) => RefreshSnapshot();
        Shown += async (_, _) =>
        {
            RefreshSnapshot();
            RefreshStartupState();
            _timer.Start();

            if (_config.EnableAutoUpdateCheck)
            {
                await CheckForUpdatesAsync(showUpToDate: false);
            }
        };
        FormClosed += (_, _) =>
        {
            _timer.Dispose();
            _helpMenu.Dispose();
            _windowIcon.Dispose();
        };
    }
    private static Size GetInitialWindowSize()
    {
        Rectangle workingArea = Screen.PrimaryScreen?.WorkingArea ?? new Rectangle(0, 0, 1280, 720);

        int width = Math.Min(1180, Math.Max(1080, workingArea.Width - 80));
        int height = Math.Min(760, Math.Max(720, workingArea.Height - 80));

        return new Size(width, height);
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

        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 166));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));

        TableLayoutPanel header = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = BackColor,
            ColumnCount = 2,
            RowCount = 1
        };

        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 300));

        Panel titlePanel = new Panel
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

        titlePanel.Controls.Add(_updatedAtLabel);
        titlePanel.Controls.Add(titleLabel);

        FlowLayoutPanel actionsPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Padding = new Padding(0, 4, 0, 0),
            BackColor = BackColor
        };

        _updateButton = new Button
        {
            Text = "Verificar atualizações",
            Width = 260,
            Height = 32,
            FlatStyle = FlatStyle.Flat,
            ForeColor = Color.White,
            BackColor = Color.FromArgb(36, 41, 47)
        };
        _updateButton.FlatAppearance.BorderColor = Color.FromArgb(58, 66, 74);
        _updateButton.Click += async (_, _) => await CheckForUpdatesAsync(showUpToDate: true);

        _errorReportButton = new Button
        {
            Text = "Relatório de erros",
            Width = 260,
            Height = 32,
            FlatStyle = FlatStyle.Flat,
            ForeColor = Color.White,
            BackColor = Color.FromArgb(36, 41, 47)
        };
        _errorReportButton.FlatAppearance.BorderColor = Color.FromArgb(58, 66, 74);
        _errorReportButton.Click += (_, _) => GenerateAndOpenErrorReport();

        _helpButton = new Button
        {
            Text = "Ajuda",
            Width = 260,
            Height = 32,
            FlatStyle = FlatStyle.Flat,
            ForeColor = Color.White,
            BackColor = Color.FromArgb(36, 41, 47)
        };
        _helpButton.FlatAppearance.BorderColor = Color.FromArgb(58, 66, 74);
        _helpButton.Click += (_, _) => ShowHelpMenu();

        _startupCheckBox = new CheckBox
        {
            Text = "Iniciar com o Windows",
            AutoSize = true,
            MinimumSize = new Size(260, 32),
            ForeColor = Color.FromArgb(210, 214, 220),
            BackColor = BackColor
        };
        _startupCheckBox.CheckedChanged += StartupCheckBoxCheckedChanged;

        _helpMenu = BuildHelpMenu();

        actionsPanel.Controls.Add(_updateButton);
        actionsPanel.Controls.Add(_errorReportButton);
        actionsPanel.Controls.Add(_helpButton);
        actionsPanel.Controls.Add(_startupCheckBox);

        header.Controls.Add(titlePanel, 0, 0);
        header.Controls.Add(actionsPanel, 1, 0);

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
            _hardwareMonitor ??= new HardwareMonitorService();

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
            AppLogService.Error(ex, "Não foi possível atualizar o painel gráfico.");
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
            $"Uso {FormatPercent(snapshot.GpuUso)} | Potência {FormatPower(snapshot.GpuPower)} | Fan {FormatFan(snapshot.GpuFan)}",
            GetTemperatureColor(snapshot.GpuTemp, _config.GpuTempMax));

        _ramCard.SetValues(
            GetRamPrimaryText(snapshot),
            GetRamSecondaryText(snapshot),
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
                    $"Existe uma nova versão disponível: {result.LatestVersion}. Versão atual: {result.CurrentVersion}.\n\nDeseja baixar e instalar agora?\n\nO Monitor Hardware será fechado e reaberto automaticamente.",
                    "Atualização disponível",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Information);

                if (dialogResult == DialogResult.Yes)
                {
                    Progress<string> progress = new Progress<string>(message => _updateButton.Text = message);

                    await _updateService.StartUpdateAsync(result, progress);
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

        try
        {
            _errorReportButton.Enabled = false;
            _errorReportButton.Text = "Coletando...";

            errorReport = ErrorReportService.Create(_config);
            technicalReport = TechnicalReportService.Create(_config);

            string reportsFolder = Path.GetDirectoryName(errorReport.ReportPath)
                ?? AppLogService.LogDirectory;

            DialogResult dialogResult = MessageBox.Show(
                $"Relatórios gerados com sucesso.\n\n" +
                $"Relatório de erros:\n{errorReport.ReportPath}\n\n" +
                $"Relatório técnico:\n{technicalReport.ReportPath}\n\n" +
                $"Para enviar ao suporte, anexe esses arquivos na Issue do GitHub.\n\n" +
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

            MessageBox.Show(
                $"Não foi possível concluir a geração dos relatórios: {ex.Message}" +
                errorReportPathText +
                technicalReportPathText,
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
        Padding = new Padding(16);
        MinimumSize = new Size(0, 190);
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
            Height = 58,
            ForeColor = SystemColors.Highlight,
            Font = new Font("Segoe UI", 26, FontStyle.Bold, GraphicsUnit.Point)
        };

        _secondaryLabel = new Label
        {
            Text = "Aguardando leitura",
            Dock = DockStyle.Fill,
            AutoEllipsis = false,
            TextAlign = ContentAlignment.TopLeft,
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
