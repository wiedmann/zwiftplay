using ZwiftPlayConsoleApp.BLE;
using ZwiftPlayConsoleApp.Utils;

namespace ZwiftPlayConsoleApp.Configuration;

public class Config
{
    public bool SendKeys { get; set; } = false;
    public bool UseMapping { get; set; } = false;
    public KeyboardMapping KeyboardMapping { get; set; } = new();

}

public class BleScanConfig
{
    public int ScanTimeoutMs { get; set; } = 30000;
    public int RequiredDeviceCount { get; set; } = 2;
    public int ConnectionTimeoutMs { get; set; } = 10000;
}

public class KeyboardMapping
{
    public Dictionary<ZwiftPlayButton, byte> ButtonToKeyMap { get; set; } = new()
    {
        { ZwiftPlayButton.Up, KeyboardKeys.UP },
        { ZwiftPlayButton.Down, KeyboardKeys.DOWN },
        { ZwiftPlayButton.Left, KeyboardKeys.LEFT },
        { ZwiftPlayButton.Right, KeyboardKeys.RIGHT },
        { ZwiftPlayButton.LeftShoulder, KeyboardKeys.SUBTRACT },
        { ZwiftPlayButton.RightShoulder, KeyboardKeys.ADD },
        { ZwiftPlayButton.Y, KeyboardKeys.Y },
        { ZwiftPlayButton.Z, KeyboardKeys.Z },
        { ZwiftPlayButton.A, KeyboardKeys.A },
        { ZwiftPlayButton.B, KeyboardKeys.B }
    };
}