using Scanner.Core.Models;

namespace Scanner.Core.Services.Interfaces;

/// <summary>
/// Service for OCR text recognition.
/// </summary>
public interface IOcrService
{
    /// <summary>
    /// Recognizes text in the image.
    /// </summary>
    Task<OcrResult> RecognizeAsync(
        byte[] imageData,
        string language = "eng",
        CancellationToken ct = default);

    /// <summary>
    /// Recognizes text with layout/position information for PDF text layer.
    /// </summary>
    Task<OcrResult> RecognizeWithLayoutAsync(
        byte[] imageData,
        string language = "eng",
        CancellationToken ct = default);

    /// <summary>
    /// Gets the list of available OCR languages.
    /// </summary>
    IReadOnlyList<string> GetAvailableLanguages();

    /// <summary>
    /// Initializes the OCR engine.
    /// </summary>
    Task InitializeAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets whether the OCR engine is initialized.
    /// </summary>
    bool IsInitialized { get; }
}
