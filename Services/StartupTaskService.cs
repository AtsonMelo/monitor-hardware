using System.Diagnostics;
using System.Security.Principal;
using System.Windows.Forms;

class StartupTaskService
{
    private const string TaskName = "Monitor Hardware";

    public bool IsEnabled()
    {
        using Process process = StartSchtasks($"/Query /TN \"{TaskName}\"");
        process.WaitForExit();

        return process.ExitCode == 0;
    }

    public void SetEnabled(bool enabled)
    {
        if (!IsRunningAsAdministrator())
        {
            throw new InvalidOperationException("Execute o app como administrador para alterar a inicialização com o Windows.");
        }

        if (enabled)
        {
            Enable();
        }
        else
        {
            Disable();
        }
    }

    private static void Enable()
    {
        string executablePath = Environment.ProcessPath ?? Application.ExecutablePath;
        string workingDirectory = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar);
        string command = $"\\\"{executablePath}\\\" --gui";
        string arguments = $"/Create /TN \"{TaskName}\" /TR \"{command}\" /SC ONLOGON /RL HIGHEST /F";

        RunSchtasksOrThrow(arguments, workingDirectory);
    }

    private static void Disable()
    {
        using Process queryProcess = StartSchtasks($"/Query /TN \"{TaskName}\"");
        queryProcess.WaitForExit();

        if (queryProcess.ExitCode != 0)
        {
            return;
        }

        RunSchtasksOrThrow($"/Delete /TN \"{TaskName}\" /F", AppContext.BaseDirectory);
    }

    private static void RunSchtasksOrThrow(string arguments, string workingDirectory)
    {
        using Process process = StartSchtasks(arguments, workingDirectory);
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            string error = process.StandardError.ReadToEnd();
            string output = process.StandardOutput.ReadToEnd();
            string message = string.IsNullOrWhiteSpace(error) ? output : error;

            throw new InvalidOperationException($"Não foi possível atualizar a inicialização automática. {message.Trim()}");
        }
    }

    private static Process StartSchtasks(string arguments, string? workingDirectory = null)
    {
        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = "schtasks.exe",
            Arguments = arguments,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        if (!string.IsNullOrWhiteSpace(workingDirectory))
        {
            startInfo.WorkingDirectory = workingDirectory;
        }

        return Process.Start(startInfo) ?? throw new InvalidOperationException("Não foi possível iniciar schtasks.exe.");
    }

    private static bool IsRunningAsAdministrator()
    {
        using WindowsIdentity identity = WindowsIdentity.GetCurrent();
        WindowsPrincipal principal = new WindowsPrincipal(identity);

        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }
}
