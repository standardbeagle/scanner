using Scanner.Core.Events;
using Scanner.Core.Models;
using Scanner.Core.Services.Interfaces;

namespace Scanner.Infrastructure.WIA;

/// <summary>
/// Service for scanner operations using WIA.
/// </summary>
public class ScannerService : IScannerService, IDisposable
{
    private readonly WiaDeviceManager _deviceManager;
    private bool _disposed;

    public event EventHandler<ScannerEventArgs>? ScannerConnected;
    public event EventHandler<ScannerEventArgs>? ScannerDisconnected;

    public ScannerService()
    {
        _deviceManager = new WiaDeviceManager();
    }

    public Task<IReadOnlyList<ScannerDevice>> GetAvailableScannersAsync(CancellationToken ct = default)
    {
        return Task.Run(() =>
        {
            var scanners = _deviceManager.GetScannerDevices().ToList();
            return (IReadOnlyList<ScannerDevice>)scanners;
        }, ct);
    }

    public async Task<ScannerDevice?> GetDefaultScannerAsync(CancellationToken ct = default)
    {
        var scanners = await GetAvailableScannersAsync(ct);
        return scanners.FirstOrDefault();
    }

    public Task<ScannedPage> ScanSinglePageAsync(
        ScannerDevice device,
        ScanSettings settings,
        IProgress<ScanProgress>? progress = null,
        CancellationToken ct = default)
    {
        return Task.Run(() =>
        {
            using var scanner = new WiaScanner();
            scanner.Connect(device.DeviceId);

            var imageData = scanner.ScanPage(settings, progress);

            return new ScannedPage
            {
                ImageData = imageData,
                PageNumber = 1,
                Dpi = settings.Dpi,
                ScannedAt = DateTime.UtcNow
            };
        }, ct);
    }

    public async Task<IReadOnlyList<ScannedPage>> ScanMultiplePagesAsync(
        ScannerDevice device,
        ScanSettings settings,
        IProgress<ScanProgress>? progress = null,
        CancellationToken ct = default)
    {
        var pages = new List<ScannedPage>();

        await Task.Run(async () =>
        {
            using var scanner = new WiaScanner();
            scanner.Connect(device.DeviceId);

            var pageNumber = 0;
            await foreach (var imageData in scanner.ScanAdfPagesAsync(settings, progress, ct))
            {
                pageNumber++;
                var page = new ScannedPage
                {
                    ImageData = imageData,
                    PageNumber = pageNumber,
                    Dpi = settings.Dpi,
                    ScannedAt = DateTime.UtcNow
                };
                pages.Add(page);
            }
        }, ct);

        return pages;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _deviceManager.Dispose();
            _disposed = true;
            GC.SuppressFinalize(this);
        }
    }
}
