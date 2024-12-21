namespace ZwiftPlayConsoleApp.Configuration;

public class BleScanConfig
{
    public int ScanTimeoutMs { get; set; } = 30000;
    public int RequiredDeviceCount { get; set; } = 2;
    public int ConnectionTimeoutMs { get; set; } = 10000;
}
