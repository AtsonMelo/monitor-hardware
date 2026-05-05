using System;
using System.Drawing;
using System.Windows.Forms;

class TrayIconService : IDisposable
{
    private readonly NotifyIcon _notifyIcon;

    public TrayIconService()
    {
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
    }

    private static string FormatTemperature(float? value)
    {
        return value.HasValue
            ? $"{value.Value:0} °C"
            : "-- °C";
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
}
