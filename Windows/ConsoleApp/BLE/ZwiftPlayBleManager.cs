using InTheHand.Bluetooth;
using ZwiftPlayConsoleApp.Zap;

namespace ZwiftPlayConsoleApp.BLE;

public class ZwiftPlayBleManager : IDisposable
{
    private readonly ZwiftPlayDevice _zapDevice = new();

    private readonly BluetoothDevice _device;
    private readonly bool _isLeft;

    private static GattCharacteristic _asyncCharacteristic;
    private static GattCharacteristic _syncRxCharacteristic;
    private static GattCharacteristic _syncTxCharacteristic;

    public ZwiftPlayBleManager(BluetoothDevice device, bool isLeft)
    {
        _device = device;
        _isLeft = isLeft;
    }

    public async Task ConnectAsync()
    {
        try
        {
            var gatt = _device.Gatt;
            await gatt.ConnectAsync();

            if (gatt.IsConnected)
            {
                Console.WriteLine("Connected");
                await RegisterCharacteristics(gatt);
                Console.WriteLine("Send Start");
                if (_syncRxCharacteristic != null)
                {
                    await _syncRxCharacteristic.WriteValueWithResponseAsync(_zapDevice.BuildHandshakeStart());
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Connection failed: {ex.Message}");
        }
    }
    private async Task RegisterCharacteristics(RemoteGattServer gatt)
    {
        try
        {
            var zapService = await gatt.GetPrimaryServiceAsync(ZapBleUuids.ZWIFT_CUSTOM_SERVICE_UUID);
            _asyncCharacteristic = await zapService.GetCharacteristicAsync(ZapBleUuids.ZWIFT_ASYNC_CHARACTERISTIC_UUID);
            _syncRxCharacteristic = await zapService.GetCharacteristicAsync(ZapBleUuids.ZWIFT_SYNC_RX_CHARACTERISTIC_UUID);
            _syncTxCharacteristic = await zapService.GetCharacteristicAsync(ZapBleUuids.ZWIFT_SYNC_TX_CHARACTERISTIC_UUID);

            await _asyncCharacteristic.StartNotificationsAsync();
            _asyncCharacteristic.CharacteristicValueChanged += (sender, eventArgs) =>
            {
                _zapDevice.ProcessCharacteristic("Async", eventArgs.Value);
            };

            await _syncTxCharacteristic.StartNotificationsAsync();
            _syncTxCharacteristic.CharacteristicValueChanged += (sender, eventArgs) =>
            {
                _zapDevice.ProcessCharacteristic("Sync Tx", eventArgs.Value);
            };
        }
        catch (TaskCanceledException)
        {
            Console.WriteLine("BLE connection was canceled during characteristic registration");
        }
    }

    public void Dispose()
    {
        if (_device.Gatt != null)
        {
            _device.Gatt.Disconnect();
        }
    }

}