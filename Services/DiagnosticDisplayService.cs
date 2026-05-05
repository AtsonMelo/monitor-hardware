using System;
using System.Collections.Generic;
using System.Globalization;

class DiagnosticDisplayService
{
    public void Show(List<SensorReading> sensors)
    {
        Console.WriteLine("=== Diagnóstico de Sensores ===");
        Console.WriteLine($"Total de sensores detectados: {sensors.Count}");
        Console.WriteLine();

        foreach (SensorReading sensor in sensors)
        {
            string value = sensor.Value.HasValue
                ? sensor.Value.Value.ToString("0.0", CultureInfo.InvariantCulture)
                : "sem valor";

            Console.WriteLine($"Hardware: {sensor.HardwareName}");
            Console.WriteLine($"HardwareType: {sensor.HardwareType}");
            Console.WriteLine($"Sensor: {sensor.SensorName}");
            Console.WriteLine($"SensorType: {sensor.SensorType}");
            Console.WriteLine($"Valor: {value}");
            Console.WriteLine();
        }
    }
}
