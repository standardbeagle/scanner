using Scanner.Core.Events;
using Scanner.Core.Models;

namespace Scanner.Infrastructure.WIA;

/// <summary>
/// Handles scanning operations via WIA using dynamic COM interop.
/// </summary>
public class WiaScanner : IDisposable
{
    private dynamic? _device;
    private bool _disposed;

    // PNG format GUID
    private const string FormatPng = "{B96B3CAF-0728-11D3-9D7B-0000F81EF32E}";

    /// <summary>
    /// Connects to the specified scanner device.
    /// </summary>
    public void Connect(string deviceId)
    {
        var deviceManager = WiaInterop.CreateDeviceManager();
        dynamic deviceInfos = deviceManager.DeviceInfos;

        foreach (dynamic deviceInfo in deviceInfos)
        {
            if (deviceInfo.DeviceID == deviceId)
            {
                _device = deviceInfo.Connect();
                return;
            }
        }

        throw new InvalidOperationException($"Scanner device not found: {deviceId}");
    }

    /// <summary>
    /// Scans a single page and returns the image data.
    /// </summary>
    public byte[] ScanPage(ScanSettings settings, IProgress<ScanProgress>? progress = null)
    {
        if (_device == null)
            throw new InvalidOperationException("Not connected to a scanner device.");

        progress?.Report(new ScanProgress(1, 1, 0, "Preparing scan..."));

        // Set the document source based on settings
        SetDocumentSource(_device, settings.ScanSource, settings.DuplexScanning);

        // Get the scanner item
        dynamic item = _device.Items[1];

        // Configure scan settings
        ConfigureSettings(item, settings);

        progress?.Report(new ScanProgress(1, 1, 20, "Scanning..."));

        // Perform the scan - transfer as PNG
        dynamic imageFile = item.Transfer(FormatPng);

        progress?.Report(new ScanProgress(1, 1, 80, "Processing image..."));

        // Get the image data
        var imageData = WiaInterop.GetImageData(imageFile);

        progress?.Report(new ScanProgress(1, 1, 100, "Complete"));

        return imageData;
    }

    /// <summary>
    /// Scans multiple pages from the ADF.
    /// </summary>
    public async IAsyncEnumerable<byte[]> ScanAdfPagesAsync(
        ScanSettings settings,
        IProgress<ScanProgress>? progress = null,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        if (_device == null)
            throw new InvalidOperationException("Not connected to a scanner device.");

        // Select the feeder as the document source with appropriate mode
        var source = settings.ScanSource;
        if (source == ScanSource.Auto || source == ScanSource.Flatbed)
        {
            // For ADF scanning, default to Feeder if Auto or Flatbed was selected
            source = settings.DuplexScanning ? ScanSource.Duplex : ScanSource.Feeder;
        }
        SetDocumentSource(_device, source, settings.DuplexScanning);

        var pageNumber = 0;

        while (!ct.IsCancellationRequested && HasMorePages())
        {
            pageNumber++;
            progress?.Report(new ScanProgress(pageNumber, null, 0, $"Scanning page {pageNumber}..."));

            dynamic item = _device.Items[1];
            ConfigureSettings(item, settings);

            byte[] imageData;
            try
            {
                dynamic imageFile = item.Transfer(FormatPng);
                imageData = WiaInterop.GetImageData(imageFile);
            }
            catch (System.Runtime.InteropServices.COMException ex) when (IsNoMorePagesError(ex))
            {
                // No more pages in the feeder
                break;
            }

            progress?.Report(new ScanProgress(pageNumber, null, 100, $"Page {pageNumber} scanned"));

            yield return imageData;

            // Small delay to allow feeder to advance
            await Task.Delay(100, ct);
        }
    }

    private void ConfigureSettings(dynamic item, ScanSettings settings)
    {
        var props = item.Properties;

        // Set resolution
        WiaInterop.SetPropertyValue(props, WiaConstants.HorizontalResolution, settings.Dpi);
        WiaInterop.SetPropertyValue(props, WiaConstants.VerticalResolution, settings.Dpi);

        // Set color mode
        var (intent, dataType) = settings.ColorMode switch
        {
            ColorMode.Color => (WiaConstants.IntentImageTypeColor, WiaConstants.DataTypeColor),
            ColorMode.Grayscale => (WiaConstants.IntentImageTypeGrayscale, WiaConstants.DataTypeGrayscale),
            ColorMode.BlackAndWhite => (WiaConstants.IntentImageTypeText, WiaConstants.DataTypeBlackAndWhite),
            _ => (WiaConstants.IntentImageTypeColor, WiaConstants.DataTypeColor)
        };

        WiaInterop.SetPropertyValue(props, WiaConstants.CurrentIntent, intent);
        WiaInterop.SetPropertyValue(props, WiaConstants.DataType, dataType);

        // Set brightness and contrast if non-default
        if (settings.Brightness != 0)
        {
            WiaInterop.SetPropertyValue(props, WiaConstants.Brightness, settings.Brightness);
        }
        if (settings.Contrast != 0)
        {
            WiaInterop.SetPropertyValue(props, WiaConstants.Contrast, settings.Contrast);
        }

        // Set scan area based on paper size
        var (width, height) = settings.PaperSize.GetDimensions();
        var widthPixels = (int)(width * settings.Dpi);
        var heightPixels = (int)(height * settings.Dpi);

        WiaInterop.SetPropertyValue(props, WiaConstants.HorizontalStartPosition, 0);
        WiaInterop.SetPropertyValue(props, WiaConstants.VerticalStartPosition, 0);
        WiaInterop.SetPropertyValue(props, WiaConstants.HorizontalExtent, widthPixels);
        WiaInterop.SetPropertyValue(props, WiaConstants.VerticalExtent, heightPixels);
    }

    private static void SetDocumentSource(dynamic device, ScanSource source, bool duplexFallback)
    {
        int selectValue = source switch
        {
            ScanSource.Flatbed => WiaConstants.Flatbed,
            ScanSource.Feeder => WiaConstants.Feeder,
            ScanSource.Duplex => WiaConstants.Feeder | WiaConstants.Duplex,
            ScanSource.FeederFrontOnly => WiaConstants.Feeder | WiaConstants.FrontOnly,
            ScanSource.FeederBackOnly => WiaConstants.Feeder | WiaConstants.BackOnly,
            ScanSource.Film => WiaConstants.Flatbed, // Film uses flatbed with transparency adapter
            ScanSource.Auto => WiaConstants.AutoSource,
            _ => WiaConstants.Flatbed
        };

        // Apply duplex fallback if needed
        if (duplexFallback && source == ScanSource.Feeder)
        {
            selectValue |= WiaConstants.Duplex;
        }

        try
        {
            WiaInterop.SetPropertyValue(device.Properties, WiaConstants.DocumentHandlingSelect, selectValue);
        }
        catch
        {
            // Some scanners don't support document handling selection
            // Fall back to default
        }
    }

    private bool HasMorePages()
    {
        if (_device == null) return false;

        try
        {
            var status = WiaInterop.GetPropertyValue(_device.Properties, WiaConstants.DocumentHandlingStatus);
            if (status != null)
            {
                return (Convert.ToInt32(status) & WiaConstants.FeederReady) != 0;
            }
        }
        catch
        {
            // Ignore errors
        }
        return false;
    }

    private static bool IsNoMorePagesError(System.Runtime.InteropServices.COMException ex)
    {
        // WIA error code for "no more pages"
        return ex.ErrorCode == unchecked((int)0x80210003);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _device = null;
            _disposed = true;
            GC.SuppressFinalize(this);
        }
    }
}
