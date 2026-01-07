namespace Scanner.Core.Models;

/// <summary>
/// PDF export configuration.
/// </summary>
public record PdfExportOptions(
    bool IncludeOcrTextLayer = true,
    PdfCompression Compression = PdfCompression.Auto,
    string? Title = null,
    string? Author = null
);

/// <summary>
/// PDF compression level.
/// </summary>
public enum PdfCompression
{
    None,
    Low,
    Medium,
    High,
    Auto
}

/// <summary>
/// TIFF compression options.
/// </summary>
public enum TiffCompression
{
    None,
    Lzw,
    Jpeg,
    Zip
}

/// <summary>
/// Export format options.
/// </summary>
public enum ExportFormat
{
    Pdf,
    Jpeg,
    Png,
    Tiff,
    Bmp
}

/// <summary>
/// Cloud storage providers.
/// </summary>
public enum CloudStorageProvider
{
    OneDrive,
    GoogleDrive
}

/// <summary>
/// Cloud folder information.
/// </summary>
public record CloudFolder(string Id, string Name, string Path);

/// <summary>
/// Cloud file information after upload.
/// </summary>
public record CloudFile(string Id, string Name, string WebUrl);
