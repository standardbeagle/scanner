using Scanner.Core.Models;

namespace Scanner.Core.Services.Interfaces;

/// <summary>
/// Service for exporting documents to various formats.
/// </summary>
public interface IExportService
{
    /// <summary>
    /// Exports the document to PDF format with optional OCR text layer.
    /// </summary>
    Task<byte[]> ExportToPdfAsync(
        ScannedDocument document,
        PdfExportOptions options,
        IProgress<double>? progress = null,
        CancellationToken ct = default);

    /// <summary>
    /// Exports a single page to JPEG format.
    /// </summary>
    Task<byte[]> ExportToJpegAsync(ScannedPage page, int quality = 90, CancellationToken ct = default);

    /// <summary>
    /// Exports a single page to PNG format.
    /// </summary>
    Task<byte[]> ExportToPngAsync(ScannedPage page, CancellationToken ct = default);

    /// <summary>
    /// Exports the document to multi-page TIFF format.
    /// </summary>
    Task<byte[]> ExportToTiffAsync(
        ScannedDocument document,
        TiffCompression compression,
        CancellationToken ct = default);

    /// <summary>
    /// Saves binary data to a file.
    /// </summary>
    Task SaveToFileAsync(byte[] data, string filePath, CancellationToken ct = default);

    /// <summary>
    /// Shows a file save dialog and saves the data.
    /// </summary>
    Task<string?> SaveWithDialogAsync(
        byte[] data,
        string suggestedFileName,
        ExportFormat format,
        CancellationToken ct = default);
}
