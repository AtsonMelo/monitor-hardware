using System;
using System.Drawing;
using System.Windows.Forms;

class TrayIconService : IDisposable
{
    private readonly NotifyIcon _notifyIcon;

    public TrayIconService()
    {
        ContextMenuStrip menu = new ContextMenuStrip();

        ToolStripMenuItem openLogsItem = new ToolStripMenuItem("Abrir pasta de logs");
        openLogsItem.Click += (_, _) => OpenFolder("logs");

        ToolStripMenuItem exitItem = new ToolStripMenuItem("Sair");
        exitItem.Click += (_, _) => Application.Exit();

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

    public void Dispose()
    {
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
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
