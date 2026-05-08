using System;
using System.Drawing;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

class HardwareStressTestForm : Form
{
    private static readonly Color WindowBackground = Color.FromArgb(17, 19, 22);
    private static readonly Color PanelBackground = Color.FromArgb(24, 27, 31);
    private static readonly Color ButtonBackground = Color.FromArgb(36, 41, 47);
    private static readonly Color ButtonBorder = Color.FromArgb(58, 66, 74);
    private static readonly Color MainText = Color.FromArgb(230, 234, 238);
    private static readonly Color MutedText = Color.FromArgb(170, 176, 184);

    private readonly HardwareMonitorService _hardwareMonitor;
    private readonly AppConfig _config;
    private readonly System.Windows.Forms.Timer _timer;
    private readonly Icon? _ownedIcon;
    private readonly Label _statusLabel;
    private readonly Label _cpuLabel;
    private readonly Label _ramLabel;
    private readonly Label _limitLabel;
    private readonly Button _startStopButton;
    private readonly Button _closeButton;
    private CancellationTokenSource? _cts;
    private Task? _cpuTask;
    private Task? _ramTask;
    private byte[]? _ramBuffer;
    private DateTime _startedAt;
    private bool _isRunning;

    public HardwareStressTestForm(HardwareMonitorService hardwareMonitor, AppConfig config, Icon? windowIcon = null)
    {
        _hardwareMonitor = hardwareMonitor ?? throw new ArgumentNullException(nameof(hardwareMonitor));
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _timer = new System.Windows.Forms.Timer { Interval = 1000 };

        Text = "Teste de estresse por hardware";
        AutoScaleMode = AutoScaleMode.Dpi;
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(780, 520);
        Size = new Size(920, 640);
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
            RowCount = 6,
            Padding = new Padding(18),
            BackColor = WindowBackground
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 1));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        Label title = new()
        {
            Text = "Teste de estresse por hardware",
            Dock = DockStyle.Fill,
            AutoEllipsis = true,
            ForeColor = MainText,
            Font = new Font("Segoe UI", 16, FontStyle.Bold, GraphicsUnit.Point),
            MinimumSize = new Size(0, 42)
        };

        Label warning = new()
        {
            Text = "Modo inicial: CPU e RAM somente. GPU e SSD permanecem em desenvolvimento. O teste para automaticamente se a temperatura ultrapassar o limite configurado.",
            Dock = DockStyle.Fill,
            AutoEllipsis = true,
            ForeColor = MutedText,
            Margin = new Padding(0, 0, 0, 12)
        };

        TableLayoutPanel body = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 2,
            BackColor = WindowBackground,
            Margin = new Padding(0, 0, 0, 12)
        };
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        body.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        body.RowStyles.Add(new RowStyle(SizeType.Percent, 50));

        body.Controls.Add(BuildInfoCard("CPU", out _cpuLabel), 0, 0);
        body.Controls.Add(BuildInfoCard("RAM", out _ramLabel), 1, 0);
        Control limitCard = BuildInfoCard("Limite e tempo", out _limitLabel);
        body.Controls.Add(limitCard, 0, 1);
        body.SetColumnSpan(limitCard, 2);

        _statusLabel = new Label
        {
            Dock = DockStyle.Fill,
            AutoEllipsis = true,
            ForeColor = MutedText,
            Margin = new Padding(0, 0, 0, 12),
            MinimumSize = new Size(0, 28),
            Text = "Pronto para iniciar."
        };

        FlowLayoutPanel buttons = new()
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = true,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = WindowBackground,
            Margin = new Padding(0)
        };

        _closeButton = new Button { Text = "Fechar" };
        ConfigureActionButton(_closeButton);
        _closeButton.Click += (_, _) =>
        {
            if (_isRunning)
            {
                StopTest("Teste interrompido pelo usuário.");
            }

            Close();
        };

        _startStopButton = new Button { Text = "Iniciar" };
        ConfigureActionButton(_startStopButton);
        _startStopButton.Click += async (_, _) =>
        {
            if (_isRunning)
            {
                StopTest("Teste interrompido pelo usuário.");
                return;
            }

            await StartTestAsync();
        };

        buttons.Controls.Add(_closeButton);
        buttons.Controls.Add(_startStopButton);

        root.Controls.Add(title, 0, 0);
        root.Controls.Add(warning, 0, 1);
        root.Controls.Add(body, 0, 2);
        root.Controls.Add(new Panel { Dock = DockStyle.Fill, BackColor = PanelBackground, Margin = new Padding(0), Height = 1 }, 0, 3);
        root.Controls.Add(_statusLabel, 0, 4);
        root.Controls.Add(buttons, 0, 5);

        Controls.Add(root);

        _timer.Tick += (_, _) => RefreshStatus();
        FormClosing += (_, _) =>
        {
            if (_isRunning)
            {
                StopTest("Teste encerrado.");
            }
        };
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
            _cts?.Cancel();
            _cts?.Dispose();
            _ownedIcon?.Dispose();
        }

        base.Dispose(disposing);
    }

    private static Panel BuildInfoCard(string title, out Label valueLabel)
    {
        TableLayoutPanel card = new()
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            BackColor = PanelBackground,
            Margin = new Padding(6),
            Padding = new Padding(12, 10, 12, 10)
        };
        card.ColumnCount = 1;
        card.RowCount = 2;
        card.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        card.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        card.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        Label titleLabel = new()
        {
            Text = title,
            Dock = DockStyle.Fill,
            AutoEllipsis = true,
            ForeColor = MutedText,
            Margin = new Padding(0, 0, 0, 6),
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold, GraphicsUnit.Point)
        };

        valueLabel = new Label
        {
            Text = "Aguardando",
            Dock = DockStyle.Fill,
            AutoEllipsis = true,
            ForeColor = MainText,
            Font = new Font("Segoe UI", 10.5f, FontStyle.Regular, GraphicsUnit.Point),
            MinimumSize = new Size(0, 56)
        };

        card.Controls.Add(titleLabel, 0, 0);
        card.Controls.Add(valueLabel, 0, 1);

        return card;
    }

    private static void ConfigureActionButton(Button button)
    {
        button.AutoSize = true;
        button.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        button.Height = 40;
        button.MinimumSize = new Size(138, 40);
        button.Padding = new Padding(10, 0, 10, 0);
        button.FlatStyle = FlatStyle.Flat;
        button.ForeColor = MainText;
        button.BackColor = ButtonBackground;
        button.UseVisualStyleBackColor = false;
        button.FlatAppearance.BorderColor = ButtonBorder;
        button.FlatAppearance.MouseOverBackColor = Color.FromArgb(44, 50, 57);
        button.FlatAppearance.MouseDownBackColor = Color.FromArgb(30, 34, 39);
    }

    private async Task StartTestAsync()
    {
        DialogResult result = MessageBox.Show(
            "Este teste vai carregar CPU e RAM de forma controlada.\n\n" +
            "Ele não altera clock, tensão ou qualquer configuração do sistema.\n" +
            "O teste para automaticamente se a temperatura ultrapassar o limite configurado ou quando o tempo limite for atingido.\n\n" +
            "Continuar?",
            Text,
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);

        if (result != DialogResult.Yes)
        {
            return;
        }

        _cts = new CancellationTokenSource();
        _ramBuffer = new byte[32 * 1024 * 1024];
        _startedAt = DateTime.Now;
        _isRunning = true;
        _startStopButton.Text = "Parar";
        _statusLabel.Text = "Teste iniciado.";
        _timer.Start();
        RefreshStatus();

        CancellationToken token = _cts.Token;
        int cpuWorkers = Math.Max(1, Math.Min(2, Environment.ProcessorCount / 2));
        Task[] workers = new Task[cpuWorkers + 1];

        for (int i = 0; i < cpuWorkers; i++)
        {
            workers[i] = Task.Factory.StartNew(
                () => RunCpuLoad(token),
                token,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default);
        }

        workers[^1] = Task.Factory.StartNew(
            () => RunRamLoad(token),
            token,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Default);

        _cpuTask = Task.WhenAll(workers);
        _ramTask = _cpuTask;
        await Task.Yield();
    }

    private void RunCpuLoad(CancellationToken token)
    {
        using SHA256 sha = SHA256.Create();
        byte[] buffer = new byte[4096];
        Random random = new Random(Environment.TickCount ^ Thread.CurrentThread.ManagedThreadId);
        random.NextBytes(buffer);

        while (!token.IsCancellationRequested)
        {
            sha.ComputeHash(buffer);
            Thread.SpinWait(20000);
        }
    }

    private void RunRamLoad(CancellationToken token)
    {
        byte[]? buffer = _ramBuffer;

        if (buffer == null)
        {
            return;
        }

        while (!token.IsCancellationRequested)
        {
            for (int i = 0; i < buffer.Length; i += 4096)
            {
                buffer[i]++;
            }

            Thread.Sleep(500);
        }
    }

    private void RefreshStatus()
    {
        if (!_isRunning)
        {
            return;
        }

        try
        {
            float cpuTempLimit = _config.CpuTempMax > 0 ? _config.CpuTempMax : 85f;
            var sensors = _hardwareMonitor.ReadAllSensors();
            float? cpuTemp = sensors.FirstOrDefault(sensor => sensor.SensorType.ToString().Equals("Temperature", StringComparison.OrdinalIgnoreCase) && sensor.HardwareType.ToString().Contains("Cpu", StringComparison.OrdinalIgnoreCase))?.Value;
            float? cpuLoad = sensors.FirstOrDefault(sensor => sensor.SensorType.ToString().Equals("Load", StringComparison.OrdinalIgnoreCase) && sensor.HardwareType.ToString().Contains("Cpu", StringComparison.OrdinalIgnoreCase))?.Value;
            float? ramLoad = sensors.FirstOrDefault(sensor => sensor.SensorType.ToString().Equals("Load", StringComparison.OrdinalIgnoreCase) && sensor.HardwareType.ToString().Equals("Memory", StringComparison.OrdinalIgnoreCase))?.Value;

            TimeSpan elapsed = DateTime.Now - _startedAt;
            _cpuLabel.Text = cpuTemp.HasValue
                ? $"Temperatura: {cpuTemp:0.0} °C | Carga: {cpuLoad:0.0}%"
                : "Temperatura CPU indisponível | Carga: " + (cpuLoad.HasValue ? $"{cpuLoad:0.0}%" : "--");
            _ramLabel.Text = $"Bloco reservado: {(_ramBuffer?.Length ?? 0) / (1024 * 1024)} MB | Uso observado: {(ramLoad.HasValue ? $"{ramLoad:0.0}%" : "--")}";
            _limitLabel.Text = $"Limite CPU: {cpuTempLimit:0.0} °C | Tempo máximo: 5 min | GPU/SSD: em desenvolvimento";
            _statusLabel.Text = $"Tempo decorrido: {elapsed:mm\\:ss} | CPU: {(cpuTemp.HasValue ? $"{cpuTemp:0.0} °C" : "--")} | RAM: {(ramLoad.HasValue ? $"{ramLoad:0.0}%" : "--")}";

            if (elapsed >= TimeSpan.FromMinutes(5))
            {
                StopTest("Teste encerrado automaticamente após o tempo limite.");
                return;
            }

            if (cpuTemp.HasValue && cpuTemp.Value >= cpuTempLimit)
            {
                StopTest($"Teste encerrado automaticamente ao atingir {cpuTemp:0.0} °C.");
            }
        }
        catch (Exception ex)
        {
            StopTest($"Falha ao monitorar sensores: {ex.Message}");
            return;
        }

        if (_isRunning && DateTime.Now - _startedAt >= TimeSpan.FromMinutes(5))
        {
            StopTest("Teste encerrado automaticamente após o tempo limite.");
        }
    }

    private void StopTest(string reason)
    {
        _cts?.Cancel();
        _timer.Stop();
        _isRunning = false;
        _startStopButton.Text = "Iniciar";
        _statusLabel.Text = reason;
        _cpuTask = null;
        _ramTask = null;
        _ramBuffer = null;
        _cts?.Dispose();
        _cts = null;
    }
}
