using CommunityToolkit.Mvvm.ComponentModel;

namespace Scanner.Core.Models;

/// <summary>
/// Scan configuration settings.
/// </summary>
public partial class ScanSettings : ObservableObject
{
    [ObservableProperty]
    private int _dpi = 300;

    [ObservableProperty]
    private ColorMode _colorMode = ColorMode.Color;

    [ObservableProperty]
    private PaperSize _paperSize = PaperSize.Letter;

    [ObservableProperty]
    private ScanSource _scanSource = ScanSource.Auto;

    [ObservableProperty]
    private bool _duplexScanning;

    [ObservableProperty]
    private bool _autoCrop = true;

    [ObservableProperty]
    private bool _autoEnhance = true;

    [ObservableProperty]
    private bool _autoOcr = true;

    [ObservableProperty]
    private string _ocrLanguage = "eng";

    [ObservableProperty]
    private int _brightness;

    [ObservableProperty]
    private int _contrast;

    /// <summary>
    /// Creates a copy of the current settings.
    /// </summary>
    public ScanSettings Clone() => new()
    {
        Dpi = Dpi,
        ColorMode = ColorMode,
        PaperSize = PaperSize,
        ScanSource = ScanSource,
        DuplexScanning = DuplexScanning,
        AutoCrop = AutoCrop,
        AutoEnhance = AutoEnhance,
        AutoOcr = AutoOcr,
        OcrLanguage = OcrLanguage,
        Brightness = Brightness,
        Contrast = Contrast
    };
}

/// <summary>
/// Document input source for scanning.
/// </summary>
public enum ScanSource
{
    /// <summary>
    /// Automatically select the best available source.
    /// </summary>
    Auto,

    /// <summary>
    /// Flatbed glass scanner.
    /// </summary>
    Flatbed,

    /// <summary>
    /// Automatic Document Feeder (single-sided).
    /// </summary>
    Feeder,

    /// <summary>
    /// Duplex ADF (double-sided scanning).
    /// </summary>
    Duplex,

    /// <summary>
    /// Film/transparency scanning (for scanners with film adapters).
    /// </summary>
    Film,

    /// <summary>
    /// Front side only when using duplex feeder.
    /// </summary>
    FeederFrontOnly,

    /// <summary>
    /// Back side only when using duplex feeder.
    /// </summary>
    FeederBackOnly
}

/// <summary>
/// Color mode for scanning.
/// </summary>
public enum ColorMode
{
    Color,
    Grayscale,
    BlackAndWhite
}

/// <summary>
/// Paper size presets.
/// </summary>
public enum PaperSize
{
    Letter,  // 8.5 x 11 inches
    Legal,   // 8.5 x 14 inches
    A4,      // 210 x 297 mm
    A5,      // 148 x 210 mm
    Custom
}

/// <summary>
/// Paper size dimensions in inches.
/// </summary>
public static class PaperSizeExtensions
{
    public static (double Width, double Height) GetDimensions(this PaperSize size) => size switch
    {
        PaperSize.Letter => (8.5, 11),
        PaperSize.Legal => (8.5, 14),
        PaperSize.A4 => (8.27, 11.69),
        PaperSize.A5 => (5.83, 8.27),
        _ => (8.5, 11)
    };
}
