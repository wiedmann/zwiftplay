﻿using InTheHand.Bluetooth;
using ZwiftPlayConsoleApp.BLE;
using ZwiftPlayConsoleApp.Configuration;
using ZwiftPlayConsoleApp.Zap;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ZwiftPlayConsoleApp.Logging;

namespace ZwiftPlayConsoleApp;

public class Program
{
    public static async Task Main(string[] args)
    {
        var services = new ServiceCollection();
        ConfigureServices(services);

        var serviceProvider = services.BuildServiceProvider();
        var config = serviceProvider.GetRequiredService<Config>();
        var app = serviceProvider.GetRequiredService<App>();
        if (args.Contains("--sendkeys"))
        {
            config.SendKeys = true;
        }

        await app.RunAsync();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        var loggingConfig = new LoggingConfig 
        {
            EnableBleManagerLogging = false,
            EnableZwiftDeviceLogging = false,
            EnableControllerNotificationLogging = false,
            EnableAppLogging = false
        };

        var config = new Config();
        services.AddSingleton(config);

        services.AddLogging(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Debug);
            builder.AddConsole(options =>
            {
                options.LogToStandardErrorThreshold = LogLevel.Debug;
            });
        });

        services.AddSingleton(loggingConfig);

        services.AddSingleton<IZwiftLogger>(provider => new ConfigurableLogger(loggingConfig));
        services.AddSingleton<BleScanConfig>();
        services.AddSingleton<App>();
        services.AddSingleton<ZwiftPlayDevice>();


    }
}
public class App
{
    private readonly IZwiftLogger _logger;
    private readonly BleScanConfig _bleScanConfig;
    private readonly Config _config;

    private readonly Dictionary<string, ZwiftPlayBleManager> _bleManagers = new();
    private readonly HashSet<string> _connectedDevices = new();
    private readonly CancellationTokenSource _scanCts = new();

     public App(IZwiftLogger logger, BleScanConfig bleScanConfig, Config config)
    {
        _logger = logger;
        _bleScanConfig = bleScanConfig;
        _config = config;
    }
    public async Task RunAsync()
    {
        var available = await Bluetooth.GetAvailabilityAsync();
        if (!available)
        {
            _logger.LogError("Bluetooth not available");
            throw new ArgumentException("Bluetooth required");
        }

        SetupBluetoothHandler();
        await RunScanningLoop();
        await HandleUserInput();
        CleanupResources();
    }

    private void SetupBluetoothHandler()
    {
        Bluetooth.AdvertisementReceived += async (sender, scanResult) =>
        {
            if (scanResult.ManufacturerData?.ContainsKey(ZapConstants.ZWIFT_MANUFACTURER_ID) == true)
            {
                await HandleDeviceDiscovered(scanResult);
            }
        };
    }

    private async Task HandleDeviceDiscovered(BluetoothAdvertisingEvent scanResult)
    {
        var manufacturerData = scanResult.ManufacturerData[ZapConstants.ZWIFT_MANUFACTURER_ID];
        var isLeft = manufacturerData[0] == ZapConstants.RC1_LEFT_SIDE;
        var deviceKey = $"{(isLeft ? "Left" : "Right")}_{scanResult.Device.Id}";

        if (!_connectedDevices.Contains(deviceKey))
        {
            _logger.LogInfo($"Found {(isLeft ? "Left" : "Right")} controller");
            Console.WriteLine($"Found {(isLeft ? "Left" : "Right")} controller");
            var manager = new ZwiftPlayBleManager(scanResult.Device, isLeft, _logger, _config);
            _bleManagers[deviceKey] = manager;
            _connectedDevices.Add(deviceKey);

            using var cts = new CancellationTokenSource(_bleScanConfig.ConnectionTimeoutMs);
            await manager.ConnectAsync();
        }
    }

    private async Task RunScanningLoop()
    {
        while (_bleManagers.Count < _bleScanConfig.RequiredDeviceCount)
        {
            _logger.LogInfo($"Start BLE Scan - Connected {_bleManagers.Count}/{_bleScanConfig.RequiredDeviceCount}");
            Console.WriteLine($"Start BLE Scan - Connected {_bleManagers.Count}/{_bleScanConfig.RequiredDeviceCount}");
            await Bluetooth.RequestLEScanAsync(new BluetoothLEScanOptions());

            try
            {
                await Task.Delay(_bleScanConfig.ScanTimeoutMs, _scanCts.Token);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInfo("Scanning canceled");
                Console.WriteLine("Scanning canceled");
                break;
            }
        }
    }

    private async Task HandleUserInput()
    {
        var run = true;
        while (run)
        {
            try
            {
                if (Console.In.Peek() != -1)
                {
                    var key = Console.ReadKey(true);
                    if (key.KeyChar == 'q')
                    {
                        _logger.LogInfo("Shutting down...");
                        _scanCts.Cancel();
                        break;
                    }
                }
            }
            catch (InvalidOperationException)
            {
                // Handle case where console input is not available
                await Task.Delay(1000);
                continue;
            }
            await Task.Delay(100);
        }
}
    private void CleanupResources()
    {
        foreach (var manager in _bleManagers.Values)
        {
            manager.Dispose();
        }
        _scanCts.Dispose();
    }
}
