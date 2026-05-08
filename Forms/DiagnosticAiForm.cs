using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

class DiagnosticAiForm : Form
{
    private readonly Label _introLabel;
    private readonly Label _summaryLabel;
    private readonly TextBox _detailsTextBox;

    public DiagnosticAiForm(List<SensorReading> sensors, Icon windowIcon)
    {
        Text = "Diagnóstico por IA";
        Icon = windowIcon;
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Color.FromArgb(17, 19, 22);
        ForeColor = Color.White;
        Font = new Font("Segoe UI", 10, FontStyle.Regular, GraphicsUnit.Point);
        MinimumSize = new Size(720, 480);
        Size = new Size(860, 620);

        _introLabel = new Label
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ForeColor = Color.FromArgb(230, 233, 236),
            Font = new Font("Segoe UI", 12f, FontStyle.Bold, GraphicsUnit.Point),
            Text = "O diagnóstico por IA será baseado nos sensores coletados."
        };

        _summaryLabel = new Label
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            Margin = new Padding(0, 10, 0, 0),
            ForeColor = Color.FromArgb(210, 214, 220),
            Text = BuildSummaryText(sensors)
        };

        _detailsTextBox = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = Color.FromArgb(27, 31, 36),
            ForeColor = Color.FromArgb(230, 233, 236),
            Font = new Font("Consolas", 9.25f, FontStyle.Regular, GraphicsUnit.Point),
            Text = BuildDetailsText(sensors)
        };

        Panel content = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(18),
            BackColor = BackColor
        };

        content.Controls.Add(_detailsTextBox);
        content.Controls.Add(_summaryLabel);
        content.Controls.Add(_introLabel);
        Controls.Add(content);
    }

    public void RefreshSensors(List<SensorReading> sensors)
    {
        _summaryLabel.Text = BuildSummaryText(sensors);
        _detailsTextBox.Text = BuildDetailsText(sensors);
    }

    private static string BuildSummaryText(List<SensorReading> sensors)
    {
        if (sensors.Count == 0)
        {
            return "Nenhum sensor disponível no momento.";
        }

        int withValue = sensors.Count(sensor => sensor.Value.HasValue);
        int withoutValue = sensors.Count - withValue;
        int hardwareTypes = sensors.Select(sensor => sensor.HardwareType).Distinct().Count();

        return $"Sensores detectados: {sensors.Count} | Com valor: {withValue} | Sem valor: {withoutValue} | Tipos de hardware: {hardwareTypes}";
    }

    private static string BuildDetailsText(List<SensorReading> sensors)
    {
        if (sensors.Count == 0)
        {
            return "Nenhum sensor foi lido ainda.";
        }

        StringBuilder builder = new StringBuilder();

        foreach (IGrouping<string, SensorReading> group in sensors
                     .GroupBy(sensor => $"{sensor.HardwareType} - {sensor.HardwareName}")
                     .OrderBy(group => group.Key))
        {
            builder.AppendLine(group.Key);
            foreach (SensorReading sensor in group.OrderBy(sensor => sensor.SensorType).ThenBy(sensor => sensor.SensorName))
            {
                string value = sensor.Value.HasValue
                    ? sensor.Value.Value.ToString("0.0")
                    : "sem valor";

                builder.AppendLine($"  {sensor.SensorType}: {sensor.SensorName} = {value}");
            }

            builder.AppendLine();
        }

        return builder.ToString();
    }
}
