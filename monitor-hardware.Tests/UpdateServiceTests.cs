using System;
using System.IO;

public class UpdateServiceTests : IDisposable
{
    private readonly string tempDirectory;

    public UpdateServiceTests()
    {
        tempDirectory = Path.Combine(Path.GetTempPath(), $"monitor-hardware-update-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDirectory);
    }

    [Fact]
    public void FindPayloadDirectory_ReturnsRoot_WhenExecutableIsInRoot()
    {
        string executablePath = Path.Combine(tempDirectory, "monitor-hardware.exe");
        File.WriteAllText(executablePath, "");

        string payloadDirectory = UpdateService.FindPayloadDirectory(tempDirectory);

        Assert.Equal(tempDirectory, payloadDirectory);
    }

    [Fact]
    public void FindPayloadDirectory_ReturnsSubdirectory_WhenExecutableIsNested()
    {
        string nestedDirectory = Path.Combine(tempDirectory, "publish");
        Directory.CreateDirectory(nestedDirectory);
        File.WriteAllText(Path.Combine(nestedDirectory, "monitor-hardware.exe"), "");

        string payloadDirectory = UpdateService.FindPayloadDirectory(tempDirectory);

        Assert.Equal(nestedDirectory, payloadDirectory);
    }

    [Fact]
    public void FindPayloadDirectory_Throws_WhenExecutableDoesNotExist()
    {
        FileNotFoundException exception = Assert.Throws<FileNotFoundException>(
            () => UpdateService.FindPayloadDirectory(tempDirectory));

        Assert.Contains("monitor-hardware.exe", exception.Message);
    }

    public void Dispose()
    {
        if (Directory.Exists(tempDirectory))
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }
}
