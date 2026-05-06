using System.Drawing;
using System.Windows.Forms;

static class AppIconService
{
    public static Icon Load()
    {
        using Icon? associatedIcon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);

        if (associatedIcon != null)
        {
            return (Icon)associatedIcon.Clone();
        }

        string iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "monitor-hardware.ico");

        if (File.Exists(iconPath))
        {
            using Icon icon = new Icon(iconPath);
            return (Icon)icon.Clone();
        }

        return (Icon)SystemIcons.Application.Clone();
    }
}
