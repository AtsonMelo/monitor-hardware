using System.Text;
using System.Text.RegularExpressions;

public class SanitizedReportResult
{
    public string OriginalPath { get; set; } = "";
    public string SanitizedPath { get; set; } = "";
    public string Content { get; set; } = "";
}

public static class ReportSanitizerService
{
    public static SanitizedReportResult CreateSanitizedCopy(string reportPath)
    {
        if (string.IsNullOrWhiteSpace(reportPath))
        {
            throw new ArgumentException("Caminho do relatório não informado.", nameof(reportPath));
        }

        if (!File.Exists(reportPath))
        {
            throw new FileNotFoundException("Relatório original não encontrado.", reportPath);
        }

        string originalContent = File.ReadAllText(reportPath, Encoding.UTF8);
        string sanitizedContent = Sanitize(originalContent);

        string originalDirectory = Path.GetDirectoryName(reportPath) ?? AppLogService.LogDirectory;
        string sanitizedDirectory = Path.Combine(originalDirectory, "github");

        Directory.CreateDirectory(sanitizedDirectory);

        string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(reportPath);
        string extension = Path.GetExtension(reportPath);

        string sanitizedPath = Path.Combine(
            sanitizedDirectory,
            $"{fileNameWithoutExtension}.sanitized{extension}");

        File.WriteAllText(sanitizedPath, sanitizedContent, Encoding.UTF8);

        return new SanitizedReportResult
        {
            OriginalPath = reportPath,
            SanitizedPath = sanitizedPath,
            Content = sanitizedContent
        };
    }

    public static string Sanitize(string content)
    {
        if (string.IsNullOrEmpty(content))
        {
            return content;
        }

        string sanitized = content;

        string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        if (!string.IsNullOrWhiteSpace(userProfile))
        {
            sanitized = sanitized.Replace(
                userProfile,
                @"C:\Users\<usuario>",
                StringComparison.OrdinalIgnoreCase);
        }

        sanitized = Regex.Replace(
            sanitized,
            @"C:\\Users\\[^\\\r\n]+",
            @"C:\Users\<usuario>",
            RegexOptions.IgnoreCase);

        sanitized = Regex.Replace(
            sanitized,
            @"/nic/%7B[^%\r\n]+%7D",
            "/nic/<interface-guid>",
            RegexOptions.IgnoreCase);

        sanitized = Regex.Replace(
            sanitized,
            @"\{?[A-Fa-f0-9]{8}-[A-Fa-f0-9]{4}-[A-Fa-f0-9]{4}-[A-Fa-f0-9]{4}-[A-Fa-f0-9]{12}\}?",
            "<guid>",
            RegexOptions.IgnoreCase);

        sanitized = Regex.Replace(
            sanitized,
            @"(?i)(token|password|senha|api[_-]?key|secret)\s*[:=]\s*[^\s\r\n]+",
            "$1=<redacted>");

        return sanitized;
    }
}