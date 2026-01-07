namespace Scanner.Core.Models;

/// <summary>
/// Represents a scanner device.
/// </summary>
public record ScannerDevice(
    string DeviceId,
    string Name,
    string Manufacturer,
    ScannerType Type,
    bool SupportsColor,
    bool SupportsAdf,
    bool SupportsDuplex,
    int MaxDpi,
    IReadOnlyList<ScanSource> SupportedSources,
    IReadOnlyList<int> SupportedDpiValues
)
{
    /// <summary>
    /// Gets the default scan source for this device.
    /// </summary>
    public ScanSource DefaultSource => SupportedSources.Count > 0
        ? SupportedSources[0]
        : ScanSource.Flatbed;

    public override string ToString() => Name;
}

/// <summary>
/// Scanner device types.
/// </summary>
public enum ScannerType
{
    Unknown,
    Flatbed,
    Adf,
    DuplexAdf,
    Multifunction
}

/// <summary>
/// Provides display names for scan sources.
/// </summary>
public static class ScanSourceExtensions
{
    public static string GetDisplayName(this ScanSource source) => source switch
    {
        ScanSource.Auto => "Auto",
        ScanSource.Flatbed => "Flatbed",
        ScanSource.Feeder => "Document Feeder",
        ScanSource.Duplex => "Duplex (2-sided)",
        ScanSource.Film => "Film/Transparency",
        ScanSource.FeederFrontOnly => "Feeder (Front only)",
        ScanSource.FeederBackOnly => "Feeder (Back only)",
        _ => source.ToString()
    };

    public static string GetDescription(this ScanSource source) => source switch
    {
        ScanSource.Auto => "Automatically select the best available source",
        ScanSource.Flatbed => "Scan from the flatbed glass",
        ScanSource.Feeder => "Scan from the automatic document feeder",
        ScanSource.Duplex => "Scan both sides of pages from the document feeder",
        ScanSource.Film => "Scan film negatives or transparencies",
        ScanSource.FeederFrontOnly => "Scan only the front side of pages",
        ScanSource.FeederBackOnly => "Scan only the back side of pages",
        _ => ""
    };
}
