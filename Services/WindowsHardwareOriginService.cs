using System.Globalization;
using System.Management;
using System.Runtime.InteropServices;

public class WindowsHardwareOriginService
{
    private const string DiskClassSource = "Win32_DiskDrive";
    private const string VideoClassSource = "Win32_VideoController";
    private const string BiosClassSource = "Win32_BIOS";
    private const string BaseBoardClassSource = "Win32_BaseBoard";
    private const string PnpSignedDriverClassSource = "Win32_PnPSignedDriver";
    private const string PnpEntityClassSource = "Win32_PnPEntity";

    public List<SensorOriginInfo> GetSensorOrigins()
    {
        List<PnpSignedDriverInfo> drivers = GetPnpSignedDrivers();
        List<PnpEntityInfo> entities = GetPnpEntities();

        List<SensorOriginInfo> origins = new();

        origins.AddRange(GetDiskOrigins(drivers, entities));
        origins.AddRange(GetGpuOrigins(drivers, entities));
        origins.AddRange(GetBiosOrigins());
        origins.AddRange(GetBaseBoardOrigins());

        return origins;
    }

    private static List<SensorOriginInfo> GetDiskOrigins(
        List<PnpSignedDriverInfo> drivers,
        List<PnpEntityInfo> entities)
    {
        List<SensorOriginInfo> origins = new();

        foreach (Dictionary<string, object?> row in QueryRows(DiskClassSource))
        {
            string pnpDeviceId = GetString(row, "PNPDeviceID");
            PnpSignedDriverInfo? driver = FindDriver(drivers, pnpDeviceId);
            PnpEntityInfo? entity = FindEntity(entities, pnpDeviceId);

            origins.Add(new SensorOriginInfo
            {
                HardwareType = "Storage",
                Name = FirstNonEmpty(
                    GetString(row, "Caption"),
                    GetString(row, "Name"),
                    GetString(row, "Model"),
                    entity?.Name,
                    driver?.DeviceName),
                Model = GetString(row, "Model"),
                Manufacturer = FirstNonEmpty(
                    GetString(row, "Manufacturer"),
                    entity?.Manufacturer,
                    driver?.Manufacturer),
                DriverProvider = driver?.DriverProvider ?? "",
                DriverVersion = driver?.DriverVersion ?? "",
                DriverDate = driver?.DriverDate ?? "",
                FirmwareVersion = GetString(row, "FirmwareRevision"),
                PnpDeviceId = pnpDeviceId,
                ProbableSensorSource = "SMART / driver de armazenamento / Windows storage stack",
                WindowsClassSource = DiskClassSource
            });
        }

        return origins;
    }

    private static List<SensorOriginInfo> GetGpuOrigins(
        List<PnpSignedDriverInfo> drivers,
        List<PnpEntityInfo> entities)
    {
        List<SensorOriginInfo> origins = new();

        foreach (Dictionary<string, object?> row in QueryRows(VideoClassSource))
        {
            string pnpDeviceId = GetString(row, "PNPDeviceID");
            PnpSignedDriverInfo? driver = FindDriver(drivers, pnpDeviceId);
            PnpEntityInfo? entity = FindEntity(entities, pnpDeviceId);

            origins.Add(new SensorOriginInfo
            {
                HardwareType = "GPU",
                Name = FirstNonEmpty(
                    GetString(row, "Name"),
                    GetString(row, "Caption"),
                    entity?.Name,
                    driver?.DeviceName),
                Model = GetString(row, "VideoProcessor"),
                Manufacturer = FirstNonEmpty(
                    GetString(row, "AdapterCompatibility"),
                    entity?.Manufacturer,
                    driver?.Manufacturer),
                DriverProvider = driver?.DriverProvider ?? "",
                DriverVersion = FirstNonEmpty(GetString(row, "DriverVersion"), driver?.DriverVersion),
                DriverDate = FirstNonEmpty(GetWmiDate(row, "DriverDate"), driver?.DriverDate),
                PnpDeviceId = pnpDeviceId,
                ProbableSensorSource = "driver de vídeo / API do fabricante",
                WindowsClassSource = VideoClassSource
            });
        }

        return origins;
    }

    private static List<SensorOriginInfo> GetBiosOrigins()
    {
        List<SensorOriginInfo> origins = new();

        foreach (Dictionary<string, object?> row in QueryRows(BiosClassSource))
        {
            origins.Add(new SensorOriginInfo
            {
                HardwareType = "BIOS",
                Name = GetString(row, "Name"),
                Manufacturer = GetString(row, "Manufacturer"),
                FirmwareVersion = FirstNonEmpty(
                    GetString(row, "SMBIOSBIOSVersion"),
                    GetString(row, "Version")),
                ProbableSensorSource = "SMBIOS / firmware da placa-mãe",
                WindowsClassSource = BiosClassSource
            });
        }

        return origins;
    }

    private static List<SensorOriginInfo> GetBaseBoardOrigins()
    {
        List<SensorOriginInfo> origins = new();

        foreach (Dictionary<string, object?> row in QueryRows(BaseBoardClassSource))
        {
            origins.Add(new SensorOriginInfo
            {
                HardwareType = "BaseBoard",
                Name = GetString(row, "Name"),
                Model = FirstNonEmpty(GetString(row, "Product"), GetString(row, "Model")),
                Manufacturer = GetString(row, "Manufacturer"),
                ProbableSensorSource = "SMBIOS / placa-mãe / ACPI",
                WindowsClassSource = BaseBoardClassSource
            });
        }

        return origins;
    }

    private static List<PnpSignedDriverInfo> GetPnpSignedDrivers()
    {
        return QueryRows(PnpSignedDriverClassSource)
            .Select(row => new PnpSignedDriverInfo
            {
                DeviceId = GetString(row, "DeviceID"),
                DeviceName = GetString(row, "DeviceName"),
                Manufacturer = GetString(row, "Manufacturer"),
                DriverProvider = GetString(row, "DriverProviderName"),
                DriverVersion = GetString(row, "DriverVersion"),
                DriverDate = GetWmiDate(row, "DriverDate")
            })
            .Where(driver => !string.IsNullOrWhiteSpace(driver.DeviceId))
            .ToList();
    }

    private static List<PnpEntityInfo> GetPnpEntities()
    {
        return QueryRows(PnpEntityClassSource)
            .Select(row => new PnpEntityInfo
            {
                PnpDeviceId = GetString(row, "PNPDeviceID"),
                Name = FirstNonEmpty(
                    GetString(row, "Name"),
                    GetString(row, "Caption"),
                    GetString(row, "Description")),
                Manufacturer = GetString(row, "Manufacturer")
            })
            .Where(entity => !string.IsNullOrWhiteSpace(entity.PnpDeviceId))
            .ToList();
    }

    private static List<Dictionary<string, object?>> QueryRows(string windowsClass)
    {
        List<Dictionary<string, object?>> rows = new();

        try
        {
            using ManagementObjectSearcher searcher = new($"SELECT * FROM {windowsClass}");
            using ManagementObjectCollection collection = searcher.Get();

            foreach (ManagementObject item in collection)
            {
                using (item)
                {
                    Dictionary<string, object?> row = new(StringComparer.OrdinalIgnoreCase);

                    foreach (PropertyData property in item.Properties)
                    {
                        row[property.Name] = property.Value;
                    }

                    rows.Add(row);
                }
            }
        }
        catch (ManagementException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
        catch (COMException)
        {
        }

        return rows;
    }

    private static PnpSignedDriverInfo? FindDriver(List<PnpSignedDriverInfo> drivers, string pnpDeviceId)
    {
        if (string.IsNullOrWhiteSpace(pnpDeviceId))
        {
            return null;
        }

        return drivers.FirstOrDefault(driver =>
            driver.DeviceId.Equals(pnpDeviceId, StringComparison.OrdinalIgnoreCase));
    }

    private static PnpEntityInfo? FindEntity(List<PnpEntityInfo> entities, string pnpDeviceId)
    {
        if (string.IsNullOrWhiteSpace(pnpDeviceId))
        {
            return null;
        }

        return entities.FirstOrDefault(entity =>
            entity.PnpDeviceId.Equals(pnpDeviceId, StringComparison.OrdinalIgnoreCase));
    }

    private static string GetString(Dictionary<string, object?> row, string propertyName)
    {
        return row.TryGetValue(propertyName, out object? value)
            ? ToSafeString(value)
            : "";
    }

    private static string GetWmiDate(Dictionary<string, object?> row, string propertyName)
    {
        string value = GetString(row, propertyName);

        if (string.IsNullOrWhiteSpace(value))
        {
            return "";
        }

        try
        {
            return ManagementDateTimeConverter
                .ToDateTime(value)
                .ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }
        catch (ArgumentOutOfRangeException)
        {
            return value;
        }
        catch (FormatException)
        {
            return value;
        }
    }

    private static string FirstNonEmpty(params string?[] values)
    {
        foreach (string? value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return "";
    }

    private static string ToSafeString(object? value)
    {
        if (value == null)
        {
            return "";
        }

        if (value is string text)
        {
            return text.Trim();
        }

        if (value is Array values)
        {
            return string.Join(
                ", ",
                values
                    .Cast<object?>()
                    .Select(ToSafeString)
                    .Where(item => !string.IsNullOrWhiteSpace(item)));
        }

        return value.ToString()?.Trim() ?? "";
    }

    private sealed class PnpSignedDriverInfo
    {
        public string DeviceId { get; init; } = "";
        public string DeviceName { get; init; } = "";
        public string Manufacturer { get; init; } = "";
        public string DriverProvider { get; init; } = "";
        public string DriverVersion { get; init; } = "";
        public string DriverDate { get; init; } = "";
    }

    private sealed class PnpEntityInfo
    {
        public string PnpDeviceId { get; init; } = "";
        public string Name { get; init; } = "";
        public string Manufacturer { get; init; } = "";
    }
}
