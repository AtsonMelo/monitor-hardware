public class HardwareSelectionService
{
    private readonly object _gate = new();
    private readonly HardwareMonitorService _hardwareMonitor;
    private List<HardwareSelectionItem> _selectedHardware = new();

    public HardwareSelectionService(HardwareMonitorService hardwareMonitor)
    {
        _hardwareMonitor = hardwareMonitor;
    }

    public List<HardwareSelectionItem> GetDetectedHardware()
    {
        List<SensorReading> sensors = _hardwareMonitor.ReadAllSensors();

        List<HardwareSelectionItem> items = sensors
            .GroupBy(GetHardwareGroupKey, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .Select(reading => new HardwareSelectionItem
            {
                HardwareType = reading.HardwareType.ToString(),
                HardwareName = reading.HardwareName,
                HardwareIdentifier = reading.HardwareIdentifier
            })
            .OrderBy(item => item.HardwareType)
            .ThenBy(item => item.HardwareName)
            .ThenBy(item => item.HardwareIdentifier)
            .ToList();

        ApplyCurrentSelection(items);
        return items;
    }

    public void SetSelectedHardware(IEnumerable<HardwareSelectionItem> selectedHardware)
    {
        lock (_gate)
        {
            _selectedHardware = selectedHardware
                .Where(item => item.IsSelected)
                .Select(item => new HardwareSelectionItem
                {
                    HardwareType = item.HardwareType,
                    HardwareName = item.HardwareName,
                    HardwareIdentifier = item.HardwareIdentifier,
                    IsSelected = true
                })
                .ToList();
        }
    }

    public bool IsHardwareSelected(string? hardwareType, string? hardwareName, string? hardwareIdentifier)
    {
        string selectionKey = GetSelectionKey(hardwareType, hardwareName, hardwareIdentifier);

        lock (_gate)
        {
            if (_selectedHardware.Count == 0)
            {
                return true;
            }

            return _selectedHardware.Any(item => string.Equals(GetSelectionKey(item), selectionKey, StringComparison.OrdinalIgnoreCase));
        }
    }

    public bool HasActiveSelection()
    {
        lock (_gate)
        {
            return _selectedHardware.Count > 0;
        }
    }

    public int GetSelectedCount()
    {
        lock (_gate)
        {
            return _selectedHardware.Count;
        }
    }

    public static string GetHardwareSelectionKey(string? hardwareType, string? hardwareName, string? hardwareIdentifier)
    {
        return GetSelectionKey(hardwareType, hardwareName, hardwareIdentifier);
    }

    private static string GetHardwareGroupKey(SensorReading reading)
    {
        return $"{reading.HardwareType}|{reading.HardwareName}|{reading.HardwareIdentifier}";
    }

    private void ApplyCurrentSelection(List<HardwareSelectionItem> items)
    {
        HashSet<string> selectedKeys;

        lock (_gate)
        {
            selectedKeys = _selectedHardware
                .Select(GetSelectionKey)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        foreach (HardwareSelectionItem item in items)
        {
            item.IsSelected = selectedKeys.Contains(GetSelectionKey(item));
        }
    }

    private static string GetSelectionKey(HardwareSelectionItem item)
    {
        return GetSelectionKey(item.HardwareType, item.HardwareName, item.HardwareIdentifier);
    }

    private static string GetSelectionKey(string? hardwareType, string? hardwareName, string? hardwareIdentifier)
    {
        if (!string.IsNullOrWhiteSpace(hardwareIdentifier))
        {
            return hardwareIdentifier.Trim();
        }

        return $"{hardwareType?.Trim() ?? string.Empty}|{hardwareName?.Trim() ?? string.Empty}";
    }
}
