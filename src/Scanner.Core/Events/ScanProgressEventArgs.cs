namespace Scanner.Core.Events;

/// <summary>
/// Scan progress information.
/// </summary>
public record ScanProgress(
    int CurrentPage,
    int? TotalPages,
    double PercentComplete,
    string Status
);

/// <summary>
/// Scanner connection event args.
/// </summary>
public class ScannerEventArgs : EventArgs
{
    public required string DeviceId { get; init; }
    public required string DeviceName { get; init; }
}
