using Scanner.Core.Events;
using Scanner.Core.Models;

namespace Scanner.Core.Services.Interfaces;

/// <summary>
/// Service for accessing scanner hardware via WIA.
/// </summary>
public interface IScannerService
{
    /// <summary>
    /// Gets all available scanner devices.
    /// </summary>
    Task<IReadOnlyList<ScannerDevice>> GetAvailableScannersAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets the default/preferred scanner device.
    /// </summary>
    Task<ScannerDevice?> GetDefaultScannerAsync(CancellationToken ct = default);

    /// <summary>
    /// Scans a single page from the specified device.
    /// </summary>
    Task<ScannedPage> ScanSinglePageAsync(
        ScannerDevice device,
        ScanSettings settings,
        IProgress<ScanProgress>? progress = null,
        CancellationToken ct = default);

    /// <summary>
    /// Scans multiple pages from the ADF (automatic document feeder).
    /// </summary>
    Task<IReadOnlyList<ScannedPage>> ScanMultiplePagesAsync(
        ScannerDevice device,
        ScanSettings settings,
        IProgress<ScanProgress>? progress = null,
        CancellationToken ct = default);

    /// <summary>
    /// Event fired when a scanner is connected.
    /// </summary>
    event EventHandler<ScannerEventArgs>? ScannerConnected;

    /// <summary>
    /// Event fired when a scanner is disconnected.
    /// </summary>
    event EventHandler<ScannerEventArgs>? ScannerDisconnected;
}
