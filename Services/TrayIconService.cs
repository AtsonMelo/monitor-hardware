using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Runtime.InteropServices;
using System.Windows.Forms;

class TrayIconService : IDisposable
{
    private readonly AppConfig _config;
    private readonly NotifyIcon _notifyIcon;
    private Icon? _currentIcon;

    public TrayIconService(AppConfig config)
    {
        _config = config;

        ContextMenuStrip menu = new ContextMenuStrip();

        ToolStripMenuItem openReportItem = new ToolStripMenuItem("Abrir relatório HTML");
        openReportItem.Click += (_, _) => OpenReport();

        ToolStripMenuItem openLogsItem = new ToolStripMenuItem("Abrir pasta de logs");
        openLogsItem.Click += (_, _) => OpenFolder("logs");

        ToolStripMenuItem exitItem = new ToolStripMenuItem("Sair");
        exitItem.Click += (_, _) =>
        {
            Hide();
            Application.Exit();
        };

        menu.Items.Add(openReportItem);
        menu.Items.Add(openLogsItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(exitItem);

        _notifyIcon = new NotifyIcon
        {
            Icon = SystemIcons.Application,
            Text = "Monitor Hardware",
            ContextMenuStrip = menu,
            Visible = true
        };
    }

    public void UpdateTooltip(MonitorSnapshot snapshot)
    {
        string text = string.Join(Environment.NewLine,
            $"CPU {FormatTemperature(snapshot.CpuTemp)} | GPU {FormatTemperature(snapshot.GpuTemp)}",
            $"CPU {FormatPercent(snapshot.CpuUso)} | GPU {FormatPercent(snapshot.GpuUso)}",
            $"RAM {FormatRam(snapshot.RamUso)}");

        _notifyIcon.Text = text.Length <= 63
            ? text
            : text[..63];

        UpdateIcon(snapshot.CpuTemp);
    }

    private void UpdateIcon(float? cpuTemp)
    {
        Icon? previousIcon = _currentIcon;
        string text = GetIconTemperatureText(cpuTemp);

        using Bitmap bitmap = new Bitmap(32, 32);
        using Graphics graphics = Graphics.FromImage(bitmap);
        using GraphicsPath textPath = CreateFittingTextPath(text);
        using SolidBrush textBrush = new SolidBrush(GetTemperatureColor(cpuTemp));

        graphics.Clear(Color.Transparent);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

        FitPathInsideIcon(textPath);
        graphics.FillPath(textBrush, textPath);

        IntPtr iconHandle = bitmap.GetHicon();

        try
        {
            using Icon icon = Icon.FromHandle(iconHandle);
            _currentIcon = (Icon)icon.Clone();
            _notifyIcon.Icon = _currentIcon;
        }
        finally
        {
            DestroyIcon(iconHandle);
        }

        previousIcon?.Dispose();
    }

    private static GraphicsPath CreateFittingTextPath(string text)
    {
        using FontFamily fontFamily = new FontFamily("Segoe UI");
        using StringFormat format = StringFormat.GenericTypographic;

        GraphicsPath path = new GraphicsPath();
        path.AddString(
            text,
            fontFamily,
            (int)FontStyle.Bold,
            28,
            Point.Empty,
            format);

        return path;
    }

    private static void FitPathInsideIcon(GraphicsPath path)
    {
        RectangleF bounds = path.GetBounds();

        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        const float padding = 1;
        float availableWidth = 32 - padding * 2;
        float availableHeight = 32 - padding * 2;
        float scale = Math.Min(availableWidth / bounds.Width, availableHeight / bounds.Height);

        float scaledWidth = bounds.Width * scale;
        float scaledHeight = bounds.Height * scale;
        using Matrix matrix = new Matrix();
        matrix.Translate(-bounds.Left, -bounds.Top, MatrixOrder.Append);
        matrix.Scale(scale, scale, MatrixOrder.Append);
        matrix.Translate((32 - scaledWidth) / 2, (32 - scaledHeight) / 2, MatrixOrder.Append);
        path.Transform(matrix);
    }

    private string GetIconTemperatureText(float? cpuTemp)
    {
        if (!cpuTemp.HasValue)
        {
            return "--";
        }

        string unit = _config.ShowTemperatureUnitInTrayIcon
            ? GetTemperatureUnit()
            : string.Empty;

        return $"{Math.Round(GetDisplayTemperature(cpuTemp.Value)):0}°{unit}";
    }

    private Color GetTemperatureColor(float? cpuTemp)
    {
        if (!cpuTemp.HasValue)
        {
            return SystemColors.GrayText;
        }

        if (cpuTemp.Value >= _config.CpuTempMax)
        {
            return Color.FromArgb(255, 80, 80);
        }

        if (cpuTemp.Value >= _config.CpuTempMax - 10)
        {
            return Color.FromArgb(255, 185, 0);
        }

        return SystemColors.Highlight;
    }

    private string FormatTemperature(float? value)
    {
        return value.HasValue
            ? $"{GetDisplayTemperature(value.Value):0} °{GetTemperatureUnit()}"
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

    private static string FormatRam(float? value)
    {
        return value.HasValue
            ? $"{value.Value:0}%"
            : "--%";
    }

    private void Hide()
    {
        _notifyIcon.Visible = false;
    }

    public void Dispose()
    {
        Hide();
        _notifyIcon.Dispose();
        _currentIcon?.Dispose();
    }

    private static void OpenReport()
    {
        try
        {
            HtmlReportService htmlReportService = new HtmlReportService();
            string reportPath = htmlReportService.GenerateHistoricalReport();

            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = Path.GetFullPath(reportPath),
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Não foi possível abrir o relatório: {ex.Message}",
                "Monitor Hardware",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
    }

    private static void OpenFolder(string folderPath)
    {
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = Path.GetFullPath(folderPath),
            UseShellExecute = true
        });
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(IntPtr hIcon);
}
