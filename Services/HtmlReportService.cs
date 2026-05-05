using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

class HtmlReportService
{
    private static readonly CultureInfo InvariantCulture = CultureInfo.InvariantCulture;
    private static readonly CultureInfo BrazilianCulture = CultureInfo.GetCultureInfo("pt-BR");

    public string GenerateLatestReport(string logsDirectory = "logs", string reportsDirectory = "reports")
    {
        FileInfo latestCsv = GetLatestCsv(logsDirectory);
        List<MonitorLogEntry> entries = ReadEntries(latestCsv.FullName);

        if (entries.Count == 0)
        {
            throw new InvalidOperationException($"O arquivo '{latestCsv.Name}' não possui leituras válidas.");
        }

        Directory.CreateDirectory(reportsDirectory);

        string reportFileName = $"{Path.GetFileNameWithoutExtension(latestCsv.Name)}.html";
        string reportPath = Path.Combine(reportsDirectory, reportFileName);

        File.WriteAllText(reportPath, BuildHtml(latestCsv.Name, entries), Encoding.UTF8);

        return reportPath;
    }

    private static FileInfo GetLatestCsv(string logsDirectory)
    {
        if (!Directory.Exists(logsDirectory))
        {
            throw new DirectoryNotFoundException($"A pasta de logs '{logsDirectory}' não foi encontrada.");
        }

        FileInfo? latestCsv = new DirectoryInfo(logsDirectory)
            .GetFiles("monitor-hardware-*.csv")
            .OrderByDescending(file => file.LastWriteTime)
            .FirstOrDefault();

        return latestCsv ?? throw new FileNotFoundException($"Nenhum CSV foi encontrado em '{logsDirectory}'.");
    }

    private static List<MonitorLogEntry> ReadEntries(string csvPath)
    {
        return File.ReadLines(csvPath)
            .Skip(1)
            .Select(ParseEntry)
            .Where(entry => entry != null)
            .Select(entry => entry!)
            .ToList();
    }

    private static MonitorLogEntry? ParseEntry(string line)
    {
        string[] columns = line.Split(',');

        if (columns.Length < 10)
        {
            return null;
        }

        if (!DateTime.TryParseExact(columns[0], "yyyy-MM-dd HH:mm:ss", InvariantCulture, DateTimeStyles.None, out DateTime timestamp))
        {
            return null;
        }

        return new MonitorLogEntry
        {
            Timestamp = timestamp,
            CpuTemp = ParseOptionalFloat(columns[1]),
            CpuUso = ParseOptionalFloat(columns[2]),
            CpuPower = ParseOptionalFloat(columns[3]),
            CpuFan = ParseOptionalFloat(columns[4]),
            GpuTemp = ParseOptionalFloat(columns[5]),
            GpuUso = ParseOptionalFloat(columns[6]),
            GpuPower = ParseOptionalFloat(columns[7]),
            SsdTemp = ParseOptionalFloat(columns[8]),
            RamUso = ParseOptionalFloat(columns[9])
        };
    }

    private static float? ParseOptionalFloat(string value)
    {
        return float.TryParse(value, NumberStyles.Float, InvariantCulture, out float parsedValue)
            ? parsedValue
            : null;
    }

    private static string BuildHtml(string csvFileName, List<MonitorLogEntry> entries)
    {
        MonitorLogEntry first = entries.First();
        MonitorLogEntry last = entries.Last();

        StringBuilder html = new StringBuilder();

        html.AppendLine("<!doctype html>");
        html.AppendLine("<html lang=\"pt-BR\">");
        html.AppendLine("<head>");
        html.AppendLine("  <meta charset=\"utf-8\">");
        html.AppendLine("  <meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">");
        html.AppendLine("  <title>Relatório Monitor Hardware</title>");
        html.AppendLine("  <style>");
        html.AppendLine(BuildStyles());
        html.AppendLine("  </style>");
        html.AppendLine("</head>");
        html.AppendLine("<body>");
        html.AppendLine("  <header class=\"topbar\">");
        html.AppendLine("    <div>");
        html.AppendLine("      <p class=\"eyebrow\">Monitor Hardware</p>");
        html.AppendLine("      <h1>Relatório de desempenho</h1>");
        html.AppendLine($"      <p class=\"meta\">Arquivo: {Escape(csvFileName)} | Leituras: {entries.Count} | Período: {FormatDate(first.Timestamp)} até {FormatDate(last.Timestamp)}</p>");
        html.AppendLine("    </div>");
        html.AppendLine("  </header>");
        html.AppendLine("  <nav class=\"tabs\" aria-label=\"Visões do relatório\">");
        html.AppendLine("    <button class=\"tab active\" type=\"button\" data-view=\"cards\">Cards</button>");
        html.AppendLine("    <button class=\"tab\" type=\"button\" data-view=\"charts\">Gráficos</button>");
        html.AppendLine("    <button class=\"tab\" type=\"button\" data-view=\"history\">Histórico completo</button>");
        html.AppendLine("  </nav>");
        html.AppendLine("  <main>");
        html.AppendLine(BuildCardsView(entries));
        html.AppendLine(BuildChartsView(entries));
        html.AppendLine(BuildHistoryView(entries));
        html.AppendLine("  </main>");
        html.AppendLine("  <script>");
        html.AppendLine(BuildScript());
        html.AppendLine("  </script>");
        html.AppendLine("</body>");
        html.AppendLine("</html>");

        return html.ToString();
    }

    private static string BuildStyles()
    {
        return """
    :root {
      --bg: #111315;
      --panel: #1a1d20;
      --panel-2: #22262a;
      --text: #f4f7fb;
      --muted: #9aa4b2;
      --line: #343a40;
      --green: #57d163;
      --red: #ff3b62;
      --cyan: #33c7ff;
      --amber: #ffbf47;
      --blue: #73a7ff;
      --violet: #b990ff;
    }

    * { box-sizing: border-box; }

    body {
      margin: 0;
      min-height: 100vh;
      background: #111315;
      color: var(--text);
      font-family: Segoe UI, Arial, sans-serif;
    }

    .topbar {
      padding: 28px 32px 18px;
      background: #171a1d;
      border-bottom: 1px solid #272c31;
    }

    .eyebrow {
      margin: 0 0 6px;
      color: var(--cyan);
      font-size: 13px;
      font-weight: 700;
      text-transform: uppercase;
    }

    h1 {
      margin: 0;
      font-size: 34px;
      font-weight: 700;
    }

    h2 {
      margin: 0 0 14px;
      font-size: 22px;
    }

    .meta {
      margin: 8px 0 0;
      color: var(--muted);
      font-size: 14px;
    }

    .tabs {
      position: sticky;
      top: 0;
      z-index: 10;
      display: flex;
      gap: 8px;
      padding: 14px 32px;
      background: #171a1d;
      border-bottom: 1px solid #272c31;
    }

    .tab {
      border: 1px solid #30363d;
      background: #202429;
      color: var(--text);
      padding: 10px 14px;
      border-radius: 8px;
      cursor: pointer;
      font-weight: 600;
    }

    .tab.active {
      background: #2d333a;
      border-color: var(--cyan);
      color: #ffffff;
    }

    main {
      padding: 26px 32px 40px;
    }

    .view { display: none; }
    .view.active { display: block; }

    .tile-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(220px, 1fr));
      gap: 14px;
    }

    .tile,
    .panel {
      background: var(--panel);
      border: 1px solid #2b3036;
      border-radius: 8px;
      box-shadow: 0 14px 26px rgba(0, 0, 0, 0.22);
    }

    .tile {
      min-height: 172px;
      padding: 16px;
      overflow: hidden;
    }

    .tile-head {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: 10px;
      margin-bottom: 10px;
    }

    .tile-title {
      font-size: 15px;
      font-weight: 700;
    }

    .dot {
      width: 10px;
      height: 10px;
      border-radius: 50%;
      background: var(--accent);
    }

    .tile-value {
      margin: 8px 0;
      font-size: 34px;
      font-weight: 700;
    }

    .tile-subtitle {
      min-height: 34px;
      color: var(--muted);
      font-size: 13px;
      line-height: 1.35;
    }

    .sparkline {
      width: 100%;
      height: 52px;
      margin-top: 12px;
      border: 1px solid #2f363d;
      background: #15181b;
    }

    .charts-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(360px, 1fr));
      gap: 16px;
    }

    .panel {
      padding: 16px;
    }

    .panel-head {
      display: flex;
      justify-content: space-between;
      gap: 12px;
      margin-bottom: 10px;
    }

    .panel-title {
      font-size: 18px;
      font-weight: 700;
    }

    .panel-stat {
      color: var(--muted);
      font-size: 13px;
      text-align: right;
    }

    .chart {
      display: block;
      width: 100%;
      min-height: 260px;
      border: 1px solid #3a4148;
      background: #121416;
    }

    .table-wrap {
      overflow: auto;
      max-height: 72vh;
      border: 1px solid #30363d;
      border-radius: 8px;
      background: var(--panel);
    }

    table {
      width: 100%;
      border-collapse: collapse;
      min-width: 880px;
    }

    th,
    td {
      padding: 10px 12px;
      border-bottom: 1px solid #2d3339;
      text-align: left;
      white-space: nowrap;
    }

    th {
      position: sticky;
      top: 0;
      background: #242a30;
      color: #ffffff;
      z-index: 1;
    }

    td { color: #d9e1ea; }
    tr:hover td { background: #20262b; }

    .section-note {
      color: var(--muted);
      margin: -6px 0 18px;
      line-height: 1.45;
    }

    @media (max-width: 760px) {
      .topbar,
      .tabs,
      main { padding-left: 16px; padding-right: 16px; }

      h1 { font-size: 27px; }

      .tabs { overflow-x: auto; }
      .tab { flex: 0 0 auto; }
      .charts-grid { grid-template-columns: 1fr; }
    }
""";
    }

    private static string BuildCardsView(List<MonitorLogEntry> entries)
    {
        StringBuilder html = new StringBuilder();

        html.AppendLine("    <section id=\"view-cards\" class=\"view active\">");
        html.AppendLine("      <h2>Cards de métricas</h2>");
        html.AppendLine("      <p class=\"section-note\">Visão rápida em blocos, inspirada em widgets: valor atual, pico/média e mini-histórico.</p>");
        html.AppendLine("      <div class=\"tile-grid\">");
        html.AppendLine(BuildMetricTile("CPU temperatura", FormatCelsius(Last(entries, entry => entry.CpuTemp)), BuildSummary(entries, entry => entry.CpuTemp, "Máx", "Média", FormatCelsius), BuildSparkline(entries, entry => entry.CpuTemp, "#57d163", 100), "#57d163"));
        html.AppendLine(BuildMetricTile("CPU uso", FormatPercent(Last(entries, entry => entry.CpuUso)), BuildSummary(entries, entry => entry.CpuUso, "Máx", "Média", FormatPercent), BuildSparkline(entries, entry => entry.CpuUso, "#73a7ff", 100), "#73a7ff"));
        html.AppendLine(BuildMetricTile("GPU temperatura", FormatCelsius(Last(entries, entry => entry.GpuTemp)), BuildSummary(entries, entry => entry.GpuTemp, "Máx", "Média", FormatCelsius), BuildSparkline(entries, entry => entry.GpuTemp, "#ff3b62", 100), "#ff3b62"));
        html.AppendLine(BuildMetricTile("GPU uso", FormatPercent(Last(entries, entry => entry.GpuUso)), BuildSummary(entries, entry => entry.GpuUso, "Máx", "Média", FormatPercent), BuildSparkline(entries, entry => entry.GpuUso, "#ff7a30", 100), "#ff7a30"));
        html.AppendLine(BuildMetricTile("SSD temperatura", FormatCelsius(Last(entries, entry => entry.SsdTemp)), BuildSummary(entries, entry => entry.SsdTemp, "Máx", "Média", FormatCelsius), BuildSparkline(entries, entry => entry.SsdTemp, "#ffbf47", 80), "#ffbf47"));
        html.AppendLine(BuildMetricTile("RAM uso", FormatPercent(Last(entries, entry => entry.RamUso)), BuildSummary(entries, entry => entry.RamUso, "Máx", "Média", FormatPercent), BuildSparkline(entries, entry => entry.RamUso, "#33c7ff", 100), "#33c7ff"));
        html.AppendLine(BuildMetricTile("Fan CPU provável", FormatRpm(Last(entries, entry => entry.CpuFan)), BuildSummary(entries, entry => entry.CpuFan, "Máx", "Média", FormatRpm), BuildSparkline(entries, entry => entry.CpuFan, "#b990ff"), "#b990ff"));
        html.AppendLine(BuildMetricTile("Potência CPU", FormatWatts(Last(entries, entry => entry.CpuPower)), BuildSummary(entries, entry => entry.CpuPower, "Máx", "Média", FormatWatts), BuildSparkline(entries, entry => entry.CpuPower, "#f87171"), "#f87171"));
        html.AppendLine("      </div>");
        html.AppendLine("    </section>");

        return html.ToString();
    }

    private static string BuildChartsView(List<MonitorLogEntry> entries)
    {
        StringBuilder html = new StringBuilder();

        html.AppendLine("    <section id=\"view-charts\" class=\"view\">");
        html.AppendLine("      <h2>Gráficos de desempenho</h2>");
        html.AppendLine("      <p class=\"section-note\">Linhas em grade para observar variação ao longo do histórico do CSV, no espírito do Gerenciador de Tarefas e das métricas da AMD.</p>");
        html.AppendLine("      <div class=\"charts-grid\">");
        html.AppendLine(BuildChartPanel("CPU temperatura", "CPU_Temp_C", entries, entry => entry.CpuTemp, "°C", "#57d163", 100));
        html.AppendLine(BuildChartPanel("CPU uso", "CPU_Uso_Percent", entries, entry => entry.CpuUso, "%", "#73a7ff", 100));
        html.AppendLine(BuildChartPanel("GPU temperatura", "GPU_Temp_C", entries, entry => entry.GpuTemp, "°C", "#ff3b62", 100));
        html.AppendLine(BuildChartPanel("GPU uso", "GPU_Uso_Percent", entries, entry => entry.GpuUso, "%", "#ff7a30", 100));
        html.AppendLine(BuildChartPanel("SSD temperatura", "SSD_Temp_C", entries, entry => entry.SsdTemp, "°C", "#ffbf47", 80));
        html.AppendLine(BuildChartPanel("RAM uso", "RAM_Uso_Percent", entries, entry => entry.RamUso, "%", "#33c7ff", 100));
        html.AppendLine(BuildChartPanel("Fan CPU provável", "CPU_Fan_RPM", entries, entry => entry.CpuFan, "RPM", "#b990ff"));
        html.AppendLine(BuildChartPanel("Potência CPU", "CPU_Power_W", entries, entry => entry.CpuPower, "W", "#f87171"));
        html.AppendLine("      </div>");
        html.AppendLine("    </section>");

        return html.ToString();
    }

    private static string BuildHistoryView(List<MonitorLogEntry> entries)
    {
        StringBuilder html = new StringBuilder();

        html.AppendLine("    <section id=\"view-history\" class=\"view\">");
        html.AppendLine("      <h2>Histórico completo</h2>");
        html.AppendLine($"      <p class=\"section-note\">Tabela com todas as {entries.Count} leituras válidas do CSV analisado.</p>");
        html.AppendLine("      <div class=\"table-wrap\">");
        html.AppendLine("        <table>");
        html.AppendLine("          <thead>");
        html.AppendLine("            <tr>");
        html.AppendLine("              <th>Data/Hora</th>");
        html.AppendLine("              <th>CPU °C</th>");
        html.AppendLine("              <th>CPU %</th>");
        html.AppendLine("              <th>CPU W</th>");
        html.AppendLine("              <th>Fan RPM</th>");
        html.AppendLine("              <th>GPU °C</th>");
        html.AppendLine("              <th>GPU %</th>");
        html.AppendLine("              <th>GPU W</th>");
        html.AppendLine("              <th>SSD °C</th>");
        html.AppendLine("              <th>RAM %</th>");
        html.AppendLine("            </tr>");
        html.AppendLine("          </thead>");
        html.AppendLine("          <tbody>");

        foreach (MonitorLogEntry entry in entries)
        {
            html.AppendLine("            <tr>");
            html.AppendLine($"              <td>{FormatDate(entry.Timestamp)}</td>");
            html.AppendLine($"              <td>{FormatNumber(entry.CpuTemp)}</td>");
            html.AppendLine($"              <td>{FormatNumber(entry.CpuUso)}</td>");
            html.AppendLine($"              <td>{FormatNumber(entry.CpuPower)}</td>");
            html.AppendLine($"              <td>{FormatNumber(entry.CpuFan)}</td>");
            html.AppendLine($"              <td>{FormatNumber(entry.GpuTemp)}</td>");
            html.AppendLine($"              <td>{FormatNumber(entry.GpuUso)}</td>");
            html.AppendLine($"              <td>{FormatNumber(entry.GpuPower)}</td>");
            html.AppendLine($"              <td>{FormatNumber(entry.SsdTemp)}</td>");
            html.AppendLine($"              <td>{FormatNumber(entry.RamUso)}</td>");
            html.AppendLine("            </tr>");
        }

        html.AppendLine("          </tbody>");
        html.AppendLine("        </table>");
        html.AppendLine("      </div>");
        html.AppendLine("    </section>");

        return html.ToString();
    }

    private static string BuildMetricTile(string title, string value, string subtitle, string sparkline, string accent)
    {
        return $"""
        <article class="tile" style="--accent: {accent}">
          <div class="tile-head">
            <div class="tile-title">{Escape(title)}</div>
            <div class="dot"></div>
          </div>
          <div class="tile-value">{Escape(value)}</div>
          <div class="tile-subtitle">{Escape(subtitle)}</div>
          {sparkline}
        </article>
""";
    }

    private static string BuildChartPanel(string title, string source, List<MonitorLogEntry> entries, Func<MonitorLogEntry, float?> selector, string unit, string color, float? fixedScaleMax = null)
    {
        float? latest = Last(entries, selector);
        float? max = Max(entries, selector);
        float? average = Average(entries, selector);
        string stat = $"Atual {FormatValue(latest, unit)} | Máx {FormatValue(max, unit)} | Média {FormatValue(average, unit)}";

        return $"""
        <article class="panel">
          <div class="panel-head">
            <div>
              <div class="panel-title">{Escape(title)}</div>
              <div class="meta">{Escape(source)}</div>
            </div>
            <div class="panel-stat">{Escape(stat)}</div>
          </div>
          {BuildLineChart(entries, selector, color, fixedScaleMax)}
        </article>
""";
    }

    private static string BuildSparkline(List<MonitorLogEntry> entries, Func<MonitorLogEntry, float?> selector, string color, float? fixedScaleMax = null)
    {
        return BuildLineChart(entries, selector, color, fixedScaleMax, width: 260, height: 70, cssClass: "sparkline", showGrid: false);
    }

    private static string BuildLineChart(
        List<MonitorLogEntry> entries,
        Func<MonitorLogEntry, float?> selector,
        string color,
        float? fixedScaleMax = null,
        int width = 720,
        int height = 260,
        string cssClass = "chart",
        bool showGrid = true)
    {
        List<float?> values = entries.Select(selector).ToList();
        float scaleMax = GetScaleMax(values, fixedScaleMax);
        string points = BuildPolylinePoints(values, width, height, scaleMax);
        string areaPoints = string.IsNullOrWhiteSpace(points)
            ? ""
            : $"0,{height} {points} {width},{height}";

        StringBuilder svg = new StringBuilder();

        svg.AppendLine($"<svg class=\"{cssClass}\" viewBox=\"0 0 {width} {height}\" role=\"img\" aria-label=\"Gráfico de linha\">");

        if (showGrid)
        {
            svg.AppendLine(BuildGrid(width, height));
        }

        if (!string.IsNullOrWhiteSpace(points))
        {
            svg.AppendLine($"  <polyline points=\"{areaPoints}\" fill=\"{color}\" opacity=\"0.16\"></polyline>");
            svg.AppendLine($"  <polyline points=\"{points}\" fill=\"none\" stroke=\"{color}\" stroke-width=\"2.2\" stroke-linecap=\"round\" stroke-linejoin=\"round\"></polyline>");
        }

        svg.AppendLine("</svg>");

        return svg.ToString();
    }

    private static string BuildGrid(int width, int height)
    {
        StringBuilder grid = new StringBuilder();

        for (int i = 1; i < 8; i++)
        {
            int x = width * i / 8;
            grid.AppendLine($"  <line x1=\"{x}\" y1=\"0\" x2=\"{x}\" y2=\"{height}\" stroke=\"#2a2f34\" stroke-width=\"1\"></line>");
        }

        for (int i = 1; i < 6; i++)
        {
            int y = height * i / 6;
            grid.AppendLine($"  <line x1=\"0\" y1=\"{y}\" x2=\"{width}\" y2=\"{y}\" stroke=\"#2a2f34\" stroke-width=\"1\"></line>");
        }

        return grid.ToString();
    }

    private static string BuildPolylinePoints(List<float?> values, int width, int height, float scaleMax)
    {
        List<string> points = new List<string>();
        int count = values.Count;

        if (count == 0)
        {
            return "";
        }

        for (int i = 0; i < count; i++)
        {
            if (!values[i].HasValue)
            {
                continue;
            }

            double x = count == 1 ? 0 : (double)i / (count - 1) * width;
            double normalized = Math.Clamp(values[i]!.Value / scaleMax, 0, 1);
            double y = height - normalized * height;

            points.Add($"{x.ToString("0.##", InvariantCulture)},{y.ToString("0.##", InvariantCulture)}");
        }

        return string.Join(" ", points);
    }

    private static float GetScaleMax(List<float?> values, float? fixedScaleMax)
    {
        if (fixedScaleMax.HasValue)
        {
            return fixedScaleMax.Value;
        }

        float max = values
            .Where(value => value.HasValue)
            .Select(value => value!.Value)
            .DefaultIfEmpty(1)
            .Max();

        if (max <= 0)
        {
            return 1;
        }

        return (float)Math.Ceiling(max * 1.15 / 10) * 10;
    }

    private static string BuildSummary(List<MonitorLogEntry> entries, Func<MonitorLogEntry, float?> selector, string maxLabel, string averageLabel, Func<float?, string> formatter)
    {
        return $"{maxLabel}: {formatter(Max(entries, selector))} | {averageLabel}: {formatter(Average(entries, selector))}";
    }

    private static float? Last(List<MonitorLogEntry> entries, Func<MonitorLogEntry, float?> selector)
    {
        return entries.Select(selector).LastOrDefault(value => value.HasValue);
    }

    private static float? Max(List<MonitorLogEntry> entries, Func<MonitorLogEntry, float?> selector)
    {
        List<float> values = entries.Select(selector).Where(value => value.HasValue).Select(value => value!.Value).ToList();

        return values.Count > 0 ? values.Max() : null;
    }

    private static float? Average(List<MonitorLogEntry> entries, Func<MonitorLogEntry, float?> selector)
    {
        List<float> values = entries.Select(selector).Where(value => value.HasValue).Select(value => value!.Value).ToList();

        return values.Count > 0 ? values.Average() : null;
    }

    private static string FormatDate(DateTime value)
    {
        return value.ToString("dd/MM/yyyy HH:mm:ss", BrazilianCulture);
    }

    private static string FormatCelsius(float? value)
    {
        return value.HasValue ? $"{FormatNumber(value)} °C" : "sem dados";
    }

    private static string FormatPercent(float? value)
    {
        return value.HasValue ? $"{FormatNumber(value)} %" : "sem dados";
    }

    private static string FormatWatts(float? value)
    {
        return value.HasValue ? $"{FormatNumber(value)} W" : "sem dados";
    }

    private static string FormatRpm(float? value)
    {
        return value.HasValue ? $"{FormatNumber(value)} RPM" : "sem dados";
    }

    private static string FormatValue(float? value, string unit)
    {
        return value.HasValue ? $"{FormatNumber(value)} {unit}" : "sem dados";
    }

    private static string FormatNumber(float? value)
    {
        return value.HasValue
            ? value.Value.ToString("0.0", BrazilianCulture)
            : "";
    }

    private static string Escape(string value)
    {
        return WebUtility.HtmlEncode(value);
    }

    private static string BuildScript()
    {
        return """
    const tabs = document.querySelectorAll('.tab');
    const views = document.querySelectorAll('.view');

    tabs.forEach((tab) => {
      tab.addEventListener('click', () => {
        const target = tab.dataset.view;

        tabs.forEach((item) => item.classList.remove('active'));
        views.forEach((view) => view.classList.remove('active'));

        tab.classList.add('active');
        document.querySelector(`#view-${target}`).classList.add('active');
      });
    });
""";
    }

    private class MonitorLogEntry
    {
        public DateTime Timestamp { get; set; }
        public float? CpuTemp { get; set; }
        public float? CpuUso { get; set; }
        public float? CpuPower { get; set; }
        public float? CpuFan { get; set; }
        public float? GpuTemp { get; set; }
        public float? GpuUso { get; set; }
        public float? GpuPower { get; set; }
        public float? SsdTemp { get; set; }
        public float? RamUso { get; set; }
    }
}
