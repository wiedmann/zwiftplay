using InTheHand.Bluetooth;
using ZwiftPlayConsoleApp.BLE;
using ZwiftPlayConsoleApp.Zap;

public class Program
{
    private static readonly Dictionary<string, ZwiftPlayBleManager> _bleManagers = new();
    private static CancellationTokenSource _scanCts = new();

    public static async Task Main(string[] args)
    {
        var available = await Bluetooth.GetAvailabilityAsync();
        if (!available)
        {
            throw new ArgumentException("Need Bluetooth");
        }

        Bluetooth.AdvertisementReceived += (sender, scanResult) =>
        {
            if (scanResult.ManufacturerData == null || !scanResult.ManufacturerData.ContainsKey(ZapConstants.ZWIFT_MANUFACTURER_ID))
            {
                return;
            }

            if (!scanResult.ManufacturerData.ContainsKey(ZapConstants.ZWIFT_MANUFACTURER_ID))
            {
                return;
            }

            if (_bleManagers.ContainsKey(scanResult.Device.Id))
            {
                return;
            }

            var data = scanResult.ManufacturerData[ZapConstants.ZWIFT_MANUFACTURER_ID];
            var typeByte = data[0];

            if (typeByte != ZapConstants.RC1_LEFT_SIDE && typeByte != ZapConstants.RC1_RIGHT_SIDE)
            {
                return;
            }

            var device = scanResult.Device;
            Console.WriteLine("Connecting to " + device.Id);
            var clientManager = new ZwiftPlayBleManager(device, typeByte == ZapConstants.RC1_LEFT_SIDE);

            _bleManagers[device.Id] = clientManager;

            clientManager.ConnectAsync();
        };

        var scanTask = Task.Run(async () =>
        {
            try
            {
                while (_bleManagers.Count < 2)
                {
                    Console.WriteLine("Start BLE Scan - Connected " + _bleManagers.Count + "/2");
                    var options = new BluetoothLEScanOptions();
                    await Bluetooth.RequestLEScanAsync(options);
                    try
                    {
                        await Task.Delay(30000, _scanCts.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        Console.WriteLine("Scanning canceled");
                        break;
                    }
                }
            }
            catch (ObjectDisposedException)
            {
                Console.WriteLine("BLE scanning stopped - resources disposed");
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("BLE scanning stopped");
            }
            Console.WriteLine("BLE Scan - loop done");
        });
        
        var run = true;
        while (run)
        {
            var line = Console.ReadLine();
            if (line != null)
            {
                var split = line.Split(" ");
                switch (split[0])
                {
                    case "q":
                    case "quit":
                        run = false;
                        _scanCts.Cancel();
                        break;
                }
            }
        }

        foreach (var manager in _bleManagers.Values)
        {
            manager.Dispose();
        }
    }
}
