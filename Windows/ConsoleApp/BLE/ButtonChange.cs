namespace ZwiftPlayConsoleApp.BLE;

public class ButtonChange
{
    public bool IsPressed
    {
        get; set;
    }

    public ZwiftPlayButton Button
    {
        get; set;
    }
}

public enum ZwiftPlayButton
{
    Up,
    Down,
    Left,
    Right,
    // Left Steer/Brake synthetic buttons
    LeftSteer,
    LeftBrake,
    LeftShoulder,
    LeftPower,

    A,
    B,
    Y,
    Z,
    // Right Steer/Brake synthetic buttons
    RightSteer,
    RightBrake,
    RightShoulder,
    RightPower,
}