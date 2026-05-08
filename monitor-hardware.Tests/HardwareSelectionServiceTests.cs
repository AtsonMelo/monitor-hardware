namespace monitor_hardware.Tests;

public class HardwareSelectionServiceTests
{
    [Fact]
    public void GetHardwareSelectionKey_WhenIdentifierExists_UsesIdentifier()
    {
        string key = HardwareSelectionService.GetHardwareSelectionKey("Cpu", "AMD Ryzen", "cpu-123");

        Assert.Equal("cpu-123", key);
    }

    [Fact]
    public void GetHardwareSelectionKey_WhenIdentifierIsMissing_UsesTypeAndName()
    {
        string key = HardwareSelectionService.GetHardwareSelectionKey("Gpu", "NVIDIA", null);

        Assert.Equal("Gpu|NVIDIA", key);
    }
}
