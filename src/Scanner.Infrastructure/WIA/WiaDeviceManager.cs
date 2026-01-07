using Scanner.Core.Models;

namespace Scanner.Infrastructure.WIA;

/// <summary>
/// Manages WIA scanner device enumeration and connections.
/// </summary>
public class WiaDeviceManager : IDisposable
{
    private readonly dynamic _deviceManager;
    private bool _disposed;

    public WiaDeviceManager()
    {
        _deviceManager = WiaInterop.CreateDeviceManager();
    }

    /// <summary>
    /// Gets all available scanner devices.
    /// </summary>
    public IEnumerable<ScannerDevice> GetScannerDevices()
    {
        dynamic deviceInfos = _deviceManager.DeviceInfos;

        foreach (dynamic deviceInfo in deviceInfos)
        {
            // Type 1 = Scanner
            if ((int)deviceInfo.Type == WiaConstants.ScannerDeviceType)
            {
                yield return CreateScannerDevice(deviceInfo);
            }
        }
    }

    /// <summary>
    /// Connects to a scanner device by its ID.
    /// </summary>
    public dynamic ConnectToDevice(string deviceId)
    {
        dynamic deviceInfos = _deviceManager.DeviceInfos;

        foreach (dynamic deviceInfo in deviceInfos)
        {
            if (deviceInfo.DeviceID == deviceId)
            {
                return deviceInfo.Connect();
            }
        }

        throw new InvalidOperationException($"Scanner device not found: {deviceId}");
    }

    /// <summary>
    /// Checks if a device is still connected.
    /// </summary>
    public bool IsDeviceConnected(string deviceId)
    {
        try
        {
            dynamic deviceInfos = _deviceManager.DeviceInfos;

            foreach (dynamic deviceInfo in deviceInfos)
            {
                if (deviceInfo.DeviceID == deviceId)
                {
                    return true;
                }
            }
        }
        catch
        {
            // Ignore errors
        }
        return false;
    }

    private static ScannerDevice CreateScannerDevice(dynamic deviceInfo)
    {
        var deviceId = (string)deviceInfo.DeviceID;
        var props = deviceInfo.Properties;

        var name = WiaInterop.GetPropertyValue(props, WiaConstants.DeviceName)?.ToString() ?? "Unknown Scanner";
        var manufacturer = WiaInterop.GetPropertyValue(props, WiaConstants.DeviceManufacturer)?.ToString() ?? "Unknown";

        // Default values
        var type = ScannerType.Flatbed;
        var supportsColor = true;
        var supportsAdf = false;
        var supportsDuplex = false;
        var maxDpi = 600;
        var supportedSources = new List<ScanSource> { ScanSource.Auto };
        var supportedDpiValues = new List<int> { 75, 100, 150, 200, 300, 600 };

        // Try to get more details by connecting
        try
        {
            dynamic device = deviceInfo.Connect();
            dynamic item = device.Items[1];

            // Check document handling capabilities
            var capabilities = WiaInterop.GetPropertyValue(device.Properties, WiaConstants.DocumentHandlingCapabilities);
            if (capabilities != null)
            {
                int caps = Convert.ToInt32(capabilities);

                // Check for flatbed
                if ((caps & WiaConstants.FlatbedAvailable) != 0)
                {
                    supportedSources.Add(ScanSource.Flatbed);
                }

                // Check for feeder (ADF)
                supportsAdf = (caps & WiaConstants.FeederAvailable) != 0;
                if (supportsAdf)
                {
                    supportedSources.Add(ScanSource.Feeder);
                }

                // Check for duplex
                supportsDuplex = (caps & WiaConstants.DuplexAvailable) != 0;
                if (supportsDuplex)
                {
                    supportedSources.Add(ScanSource.Duplex);
                    supportedSources.Add(ScanSource.FeederFrontOnly);
                    supportedSources.Add(ScanSource.FeederBackOnly);
                    type = ScannerType.DuplexAdf;
                }
                else if (supportsAdf)
                {
                    type = ScannerType.Adf;
                }

                // Check for film scanner (transparency adapter)
                if ((caps & WiaConstants.FilmAvailable) != 0)
                {
                    supportedSources.Add(ScanSource.Film);
                }

                // Determine scanner type based on capabilities
                if (supportsAdf && (caps & WiaConstants.FlatbedAvailable) != 0)
                {
                    type = supportsDuplex ? ScannerType.Multifunction : ScannerType.Multifunction;
                }
            }
            else
            {
                // No document handling info, assume flatbed only
                supportedSources.Add(ScanSource.Flatbed);
            }

            // Try to get supported DPI values
            var dpiValues = GetSupportedDpiValues(item.Properties);
            if (dpiValues.Count > 0)
            {
                supportedDpiValues = dpiValues;
                maxDpi = supportedDpiValues.Max();
            }
            else
            {
                // Try to get max DPI from current value
                var resolution = WiaInterop.GetPropertyValue(item.Properties, WiaConstants.HorizontalResolution);
                if (resolution != null)
                {
                    maxDpi = Math.Max(Convert.ToInt32(resolution), maxDpi);
                }
            }
        }
        catch
        {
            // Ignore errors when getting additional info
            // Fall back to basic flatbed support
            if (!supportedSources.Contains(ScanSource.Flatbed))
            {
                supportedSources.Add(ScanSource.Flatbed);
            }
        }

        return new ScannerDevice(
            deviceId,
            name,
            manufacturer,
            type,
            supportsColor,
            supportsAdf,
            supportsDuplex,
            maxDpi,
            supportedSources.AsReadOnly(),
            supportedDpiValues.AsReadOnly());
    }

    private static List<int> GetSupportedDpiValues(dynamic properties)
    {
        var dpiValues = new List<int>();

        try
        {
            // Try to get the range of supported DPI values
            // WIA properties can return min/max/step or a list of valid values
            foreach (dynamic prop in properties)
            {
                if ((int)prop.PropertyID == WiaConstants.HorizontalResolution)
                {
                    // Check if it's a range or list
                    if (prop.SubType == 1) // WIA_PROP_RANGE
                    {
                        int min = prop.SubTypeMin;
                        int max = prop.SubTypeMax;
                        int step = prop.SubTypeStep;

                        // Generate common DPI values within range
                        var commonDpi = new[] { 75, 100, 150, 200, 300, 400, 600, 1200, 2400, 4800 };
                        foreach (var dpi in commonDpi)
                        {
                            if (dpi >= min && dpi <= max)
                            {
                                dpiValues.Add(dpi);
                            }
                        }
                    }
                    else if (prop.SubType == 2) // WIA_PROP_LIST
                    {
                        dynamic values = prop.SubTypeValues;
                        foreach (dynamic val in values)
                        {
                            dpiValues.Add(Convert.ToInt32(val));
                        }
                    }
                    break;
                }
            }
        }
        catch
        {
            // Ignore errors
        }

        return dpiValues;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            GC.SuppressFinalize(this);
        }
    }
}
