using Scanner.Core.Models;

namespace Scanner.Core.Services.Interfaces;

/// <summary>
/// Service for image processing operations (crop, enhance, rotate).
/// </summary>
public interface IImageProcessingService
{
    /// <summary>
    /// Automatically detects and crops the document from the image.
    /// </summary>
    Task<byte[]> AutoCropAsync(byte[] imageData, CancellationToken ct = default);

    /// <summary>
    /// Detects the document boundaries in the image.
    /// </summary>
    Task<DocumentCorners?> DetectDocumentBoundsAsync(byte[] imageData, CancellationToken ct = default);

    /// <summary>
    /// Applies perspective correction using the specified corner points.
    /// </summary>
    Task<byte[]> ApplyPerspectiveCorrectionAsync(
        byte[] imageData,
        DocumentCorners corners,
        CancellationToken ct = default);

    /// <summary>
    /// Enhances the image with auto-contrast, sharpening, etc.
    /// </summary>
    Task<byte[]> EnhanceAsync(byte[] imageData, EnhanceOptions options, CancellationToken ct = default);

    /// <summary>
    /// Rotates the image by the specified degrees (90, 180, 270).
    /// </summary>
    Task<byte[]> RotateAsync(byte[] imageData, int degrees, CancellationToken ct = default);

    /// <summary>
    /// Crops the image to the specified bounds.
    /// </summary>
    Task<byte[]> CropAsync(byte[] imageData, CropBounds bounds, CancellationToken ct = default);

    /// <summary>
    /// Generates a thumbnail of the image.
    /// </summary>
    Task<byte[]> GenerateThumbnailAsync(byte[] imageData, int maxWidth, int maxHeight, CancellationToken ct = default);

    /// <summary>
    /// Converts the image to grayscale.
    /// </summary>
    Task<byte[]> ConvertToGrayscaleAsync(byte[] imageData, CancellationToken ct = default);

    /// <summary>
    /// Adjusts brightness and contrast.
    /// </summary>
    Task<byte[]> AdjustBrightnessContrastAsync(
        byte[] imageData,
        double brightness,
        double contrast,
        CancellationToken ct = default);

    /// <summary>
    /// Gets the dimensions of an image.
    /// </summary>
    Task<(int Width, int Height)> GetImageDimensionsAsync(byte[] imageData, CancellationToken ct = default);
}

/// <summary>
/// Document corner points for perspective correction.
/// </summary>
public record DocumentCorners(
    Point TopLeft,
    Point TopRight,
    Point BottomRight,
    Point BottomLeft
);

/// <summary>
/// 2D point.
/// </summary>
public record Point(int X, int Y);

/// <summary>
/// Image enhancement options.
/// </summary>
public record EnhanceOptions(
    bool AutoContrast = true,
    bool Sharpen = true,
    bool DenoiseLight = true,
    bool Deskew = true
);
