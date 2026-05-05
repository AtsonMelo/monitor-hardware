using LibreHardwareMonitor.Hardware;

class SensorReading
{
    public string HardwareName { get; set; } = "";
    public HardwareType HardwareType { get; set; }
    public string SensorName { get; set; } = "";
    public SensorType SensorType { get; set; }
    public float? Value { get; set; }
}

class Program
{
    static void Main()
    {
        Console.WriteLine("Monitor de Hardware - versão resumida");
        Console.WriteLine("Pressione Ctrl + C para sair.");

        Computer computer = new Computer
        {
            IsCpuEnabled = true,
            IsGpuEnabled = true,
            IsMotherboardEnabled = true,
            IsStorageEnabled = true,
            IsMemoryEnabled = true,
            IsControllerEnabled = true,
            IsNetworkEnabled = true
        };

        computer.Open();

        while (true)
        {
            List<SensorReading> sensors = ReadAllSensors(computer);

            Console.Clear();
            Console.WriteLine("=== Monitor de Hardware - Resumo ===");
            Console.WriteLine($"Atualizado em: {DateTime.Now:dd/MM/yyyy HH:mm:ss}");
            Console.WriteLine();

            MostrarCpu(sensors);
            MostrarGpu(sensors);
            MostrarSsd(sensors);
            MostrarMemoria(sensors);
            MostrarRede(sensors);
            MostrarAlertas(sensors);

            Thread.Sleep(2000);
        }
    }

    static List<SensorReading> ReadAllSensors(Computer computer)
    {
        List<SensorReading> sensors = new();

        foreach (IHardware hardware in computer.Hardware)
        {
            ReadHardwareRecursive(hardware, sensors);
        }

        return sensors;
    }

    static void ReadHardwareRecursive(IHardware hardware, List<SensorReading> sensors)
    {
        hardware.Update();

        foreach (ISensor sensor in hardware.Sensors)
        {
            sensors.Add(new SensorReading
            {
                HardwareName = hardware.Name,
                HardwareType = hardware.HardwareType,
                SensorName = sensor.Name,
                SensorType = sensor.SensorType,
                Value = sensor.Value
            });
        }

        foreach (IHardware subHardware in hardware.SubHardware)
        {
            ReadHardwareRecursive(subHardware, sensors);
        }
    }

    static float? GetSensor(List<SensorReading> sensors, HardwareType hardwareType, SensorType sensorType, string sensorName)
    {
        return sensors
            .FirstOrDefault(s =>
                s.HardwareType == hardwareType &&
                s.SensorType == sensorType &&
                s.SensorName.Equals(sensorName, StringComparison.OrdinalIgnoreCase))
            ?.Value;
    }

    static float? GetFirstSensor(List<SensorReading> sensors, SensorType sensorType, string sensorNameContains)
    {
        return sensors
            .FirstOrDefault(s =>
                s.SensorType == sensorType &&
                s.SensorName.Contains(sensorNameContains, StringComparison.OrdinalIgnoreCase) &&
                s.Value != null)
            ?.Value;
    }

    static string F(float? value, string unit = "")
    {
        return value == null ? "--" : $"{value:F1}{unit}";
    }

    static void MostrarCpu(List<SensorReading> sensors)
    {
        float? cpuTemp = GetSensor(sensors, HardwareType.Cpu, SensorType.Temperature, "CPU Package");
        float? cpuCoreMax = GetSensor(sensors, HardwareType.Cpu, SensorType.Temperature, "Core Max");
        float? cpuUso = GetSensor(sensors, HardwareType.Cpu, SensorType.Load, "CPU Total");
        float? cpuPower = GetSensor(sensors, HardwareType.Cpu, SensorType.Power, "CPU Package");
        float? cpuClock = GetSensor(sensors, HardwareType.Cpu, SensorType.Clock, "CPU Core #1");

        // Fan geralmente vem do chip sensor da placa-mãe, não da CPU diretamente.
        float? cpuFan = sensors
            .Where(s => s.SensorType == SensorType.Fan && s.Value != null && s.Value > 0)
            .OrderByDescending(s => s.Value)
            .FirstOrDefault()
            ?.Value;

        Console.WriteLine("CPU");
        Console.WriteLine($"  Temperatura Package : {F(cpuTemp, " °C")}");
        Console.WriteLine($"  Temperatura Core Max: {F(cpuCoreMax, " °C")}");
        Console.WriteLine($"  Uso total           : {F(cpuUso, " %")}");
        Console.WriteLine($"  Potência Package    : {F(cpuPower, " W")}");
        Console.WriteLine($"  Clock Core #1       : {F(cpuClock, " MHz")}");
        Console.WriteLine($"  Fan provável CPU    : {F(cpuFan, " RPM")}");
        Console.WriteLine();
    }

    static void MostrarGpu(List<SensorReading> sensors)
    {
        float? gpuTemp = GetSensor(sensors, HardwareType.GpuAmd, SensorType.Temperature, "GPU Core");
        float? gpuUso = GetSensor(sensors, HardwareType.GpuAmd, SensorType.Load, "GPU Core");
        float? gpuPower = GetSensor(sensors, HardwareType.GpuAmd, SensorType.Power, "GPU Package");
        float? gpuClock = GetSensor(sensors, HardwareType.GpuAmd, SensorType.Clock, "GPU Core");
        float? gpuMemClock = GetSensor(sensors, HardwareType.GpuAmd, SensorType.Clock, "GPU Memory");
        float? gpuFan = GetSensor(sensors, HardwareType.GpuAmd, SensorType.Fan, "GPU Fan");

        Console.WriteLine("GPU - Radeon RX 470");
        Console.WriteLine($"  Temperatura : {F(gpuTemp, " °C")}");
        Console.WriteLine($"  Uso         : {F(gpuUso, " %")}");
        Console.WriteLine($"  Potência    : {F(gpuPower, " W")}");
        Console.WriteLine($"  Clock Core  : {F(gpuClock, " MHz")}");
        Console.WriteLine($"  Clock Mem   : {F(gpuMemClock, " MHz")}");
        Console.WriteLine($"  Fan         : {F(gpuFan, " RPM")}");
        Console.WriteLine();
    }

    static void MostrarSsd(List<SensorReading> sensors)
    {
        float? ssdTemp = GetSensor(sensors, HardwareType.Storage, SensorType.Temperature, "Temperature");
        float? ssdVida = GetSensor(sensors, HardwareType.Storage, SensorType.Level, "Life");
        float? ssdUso = GetSensor(sensors, HardwareType.Storage, SensorType.Load, "Used Space");
        float? ssdAtividade = GetSensor(sensors, HardwareType.Storage, SensorType.Load, "Total Activity");

        Console.WriteLine("SSD");
        Console.WriteLine($"  Temperatura : {F(ssdTemp, " °C")}");
        Console.WriteLine($"  Vida útil   : {F(ssdVida, " %")}");
        Console.WriteLine($"  Espaço usado: {F(ssdUso, " %")}");
        Console.WriteLine($"  Atividade   : {F(ssdAtividade, " %")}");
        Console.WriteLine();
    }

    static void MostrarMemoria(List<SensorReading> sensors)
    {
        float? ramUso = sensors
            .FirstOrDefault(s =>
                s.HardwareName.Equals("Total Memory", StringComparison.OrdinalIgnoreCase) &&
                s.SensorType == SensorType.Load &&
                s.SensorName.Equals("Memory", StringComparison.OrdinalIgnoreCase))
            ?.Value;

        float? ramUsada = sensors
            .FirstOrDefault(s =>
                s.HardwareName.Equals("Total Memory", StringComparison.OrdinalIgnoreCase) &&
                s.SensorType == SensorType.Data &&
                s.SensorName.Equals("Memory Used", StringComparison.OrdinalIgnoreCase))
            ?.Value;

        float? ramLivre = sensors
            .FirstOrDefault(s =>
                s.HardwareName.Equals("Total Memory", StringComparison.OrdinalIgnoreCase) &&
                s.SensorType == SensorType.Data &&
                s.SensorName.Equals("Memory Available", StringComparison.OrdinalIgnoreCase))
            ?.Value;

        Console.WriteLine("Memória RAM");
        Console.WriteLine($"  Uso       : {F(ramUso, " %")}");
        Console.WriteLine($"  Usada     : {F(ramUsada, " GB")}");
        Console.WriteLine($"  Disponível: {F(ramLivre, " GB")}");
        Console.WriteLine();
    }

    static void MostrarRede(List<SensorReading> sensors)
    {
        float? down = GetFirstSensor(sensors, SensorType.Throughput, "Download Speed");
        float? up = GetFirstSensor(sensors, SensorType.Throughput, "Upload Speed");

        Console.WriteLine("Rede");
        Console.WriteLine($"  Download: {F(down)} B/s");
        Console.WriteLine($"  Upload  : {F(up)} B/s");
        Console.WriteLine();
    }

    static void MostrarAlertas(List<SensorReading> sensors)
    {
        float? cpuTemp = GetSensor(sensors, HardwareType.Cpu, SensorType.Temperature, "CPU Package");
        float? gpuTemp = GetSensor(sensors, HardwareType.GpuAmd, SensorType.Temperature, "GPU Core");
        float? ssdTemp = GetSensor(sensors, HardwareType.Storage, SensorType.Temperature, "Temperature");

        Console.WriteLine("Alertas");

        bool alerta = false;

        if (cpuTemp >= 80)
        {
            Console.WriteLine($"  ALERTA: CPU alta: {cpuTemp:F1} °C");
            alerta = true;
        }

        if (gpuTemp >= 80)
        {
            Console.WriteLine($"  ALERTA: GPU alta: {gpuTemp:F1} °C");
            alerta = true;
        }

        if (ssdTemp >= 60)
        {
            Console.WriteLine($"  ALERTA: SSD quente: {ssdTemp:F1} °C");
            alerta = true;
        }

        if (!alerta)
        {
            Console.WriteLine("  Nenhum alerta crítico.");
        }

        Console.WriteLine();
    }
}