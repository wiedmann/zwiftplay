using ZwiftPlayConsoleApp.Logging;
using ZwiftPlayConsoleApp.Utils;
using ZwiftPlayConsoleApp.Zap;
using ZwiftPlayConsoleApp.Zap.Crypto;
using ZwiftPlayConsoleApp.Zap.Proto;
using ZwiftPlayConsoleApp.Configuration;

namespace ZwiftPlayConsoleApp.BLE;

public class ZwiftPlayDevice : AbstractZapDevice
{
    
    //private readonly IZwiftLogger _logger;
    private int _batteryLevel;
    private ControllerNotification? _lastButtonState;

    private readonly Config _config;

    public ZwiftPlayDevice(IZwiftLogger logger, Config config) : base(logger)
    {
        _config = config;
    }

    protected override void ProcessEncryptedData(byte[] bytes)
    {
        _logger.LogDebug($"Processing encrypted data length: {bytes.Length}");
        try
        {
            if (Debug)
                _logger.LogDebug($"Processing encrypted data: {Utils.Utils.ByteArrayToStringHex(bytes)}");
            var counterBytes = new byte[4];
            Array.Copy(bytes, 0, counterBytes, 0, counterBytes.Length);
            var counter = new ByteBuffer(counterBytes).ReadInt32();
            if (Debug)
                _logger.LogDebug($"Counter bytes: {Utils.Utils.ByteArrayToStringHex(counterBytes)}");
            var payloadBytes = new byte[bytes.Length - 4 - EncryptionUtils.MAC_LENGTH];
            Array.Copy(bytes, 4, payloadBytes, 0, payloadBytes.Length);
            if (Debug)
                _logger.LogDebug($"Attempting payload extraction, length: {payloadBytes.Length}");

            var tagBytes = new byte[EncryptionUtils.MAC_LENGTH];
            Array.Copy(bytes, EncryptionUtils.MAC_LENGTH + payloadBytes.Length, tagBytes, 0, tagBytes.Length);
            if (Debug)
                _logger.LogDebug($"Attempting tag extraction, starting at index: {EncryptionUtils.MAC_LENGTH + payloadBytes.Length}");

            var data = new byte[payloadBytes.Length];
            try
            {
                data = _zapEncryption.Decrypt(counter, payloadBytes, tagBytes);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Decrypt failed - Counter: {counter}, Payload: {BitConverter.ToString(payloadBytes)}, Tag: {BitConverter.ToString(tagBytes)}", ex);
            }
            if (Debug)
            _logger.LogDebug($"Decrypted data: {BitConverter.ToString(data)}");

            var type = data[0];
            var messageBytes = new byte[data.Length - 1];
            Array.Copy(data, 1, messageBytes, 0, messageBytes.Length);

            if (Debug)
                _logger.LogDebug($"Controller notification message type: {type}");
            switch (type)
            {
                case ZapConstants.CONTROLLER_NOTIFICATION_MESSAGE_TYPE:
                    _logger.LogInfo("Button state change detected");
                    ProcessButtonNotification(new ControllerNotification(messageBytes, new ConfigurableLogger(((ConfigurableLogger)_logger)._config, nameof(ControllerNotification))));
                    break;
                case ZapConstants.EMPTY_MESSAGE_TYPE:
                    if (Debug)
                        _logger.LogDebug("Empty Message");
                    break;
                case ZapConstants.BATTERY_LEVEL_TYPE:
                    var notification = new BatteryStatus(messageBytes);
                    if (_batteryLevel != notification.Level)
                    {
                        _batteryLevel = notification.Level;
                        _logger.LogInfo($"Battery level update: {_batteryLevel}");
                    }
                    break;
                default:
                    _logger.LogWarning($"Unprocessed - Type: {type} Data: {Utils.Utils.ByteArrayToStringHex(data)}");
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("Decrypt failed", ex);
        }
    }
    private bool SendKeys { get; set; } = false;

    private void ProcessButtonNotification(ControllerNotification notification)
    {
        if (_config.SendKeys)
        {
            var changes = notification.DiffChange(_lastButtonState);
            foreach (var change in changes)
            {
                KeyboardKeys.ProcessZwiftPlay(change);
            }
        }
        else
        {
            if (_lastButtonState == null)
            {
                _logger.LogInfo($"Controller: {notification}");
            }
            else
            {
                var diff = notification.Diff(_lastButtonState);
                if (!string.IsNullOrEmpty(diff))
                {
                    _logger.LogInfo($"Button: {diff}");
                    Console.WriteLine($"Button: {diff}");
                }
            }
        }

        _lastButtonState = notification;
    }
}