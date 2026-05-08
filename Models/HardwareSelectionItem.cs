public sealed class HardwareSelectionItem
{
    public string HardwareType { get; set; } = string.Empty;
    public string HardwareName { get; set; } = string.Empty;
    public string? HardwareIdentifier { get; set; }
    public bool IsSelected { get; set; }

    public string DisplayIdentifier => string.IsNullOrWhiteSpace(HardwareIdentifier) ? "-" : HardwareIdentifier;
}
