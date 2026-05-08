using Microsoft.Win32;
using System.Runtime.InteropServices;

static class WindowThemeService
{
    private const int DwmwaUseImmersiveDarkModeBefore20H1 = 19;
    private const int DwmwaUseImmersiveDarkMode = 20;

    public static void ApplyNativeTitleBarTheme(IntPtr hwnd)
    {
        if (hwnd == IntPtr.Zero)
        {
            return;
        }

        bool useDarkMode = IsAppThemeDark();

        if (!TrySetImmersiveDarkMode(hwnd, useDarkMode))
        {
            return;
        }
    }

    public static bool IsAppThemeDark()
    {
        try
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            object? value = key?.GetValue("AppsUseLightTheme");

            if (value is int themeValue)
            {
                return themeValue == 0;
            }
        }
        catch
        {
        }

        return false;
    }

    private static bool TrySetImmersiveDarkMode(IntPtr hwnd, bool enabled)
    {
        int useDarkMode = enabled ? 1 : 0;

        return TrySetWindowAttribute(hwnd, DwmwaUseImmersiveDarkMode, ref useDarkMode)
            || TrySetWindowAttribute(hwnd, DwmwaUseImmersiveDarkModeBefore20H1, ref useDarkMode);
    }

    private static bool TrySetWindowAttribute(IntPtr hwnd, int attribute, ref int value)
    {
        try
        {
            int hr = DwmSetWindowAttribute(
                hwnd,
                attribute,
                ref value,
                Marshal.SizeOf<int>());

            return hr >= 0;
        }
        catch (DllNotFoundException)
        {
            return false;
        }
        catch (EntryPointNotFoundException)
        {
            return false;
        }
        catch
        {
            return false;
        }
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(
        IntPtr hwnd,
        int attribute,
        ref int pvAttribute,
        int cbAttribute);
}
