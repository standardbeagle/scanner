using OpenCvSharp;
using Scanner.Core.Models;
using Scanner.Core.Services.Interfaces;

namespace Scanner.Core.Services;

/// <summary>
/// Service for image processing using OpenCvSharp.
/// </summary>
public class ImageProcessingService : IImageProcessingService
{
    public async Task<byte[]> AutoCropAsync(byte[] imageData, CancellationToken ct = default)
    {
        return await Task.Run(() =>
        {
            var corners = DetectDocumentBoundsInternal(imageData);
            if (corners == null)
                return imageData; // No document found, return original

            return ApplyPerspectiveCorrectionInternal(imageData, corners);
        }, ct);
    }

    public Task<DocumentCorners?> DetectDocumentBoundsAsync(byte[] imageData, CancellationToken ct = default)
    {
        return Task.Run(() => DetectDocumentBoundsInternal(imageData), ct);
    }

    private static DocumentCorners? DetectDocumentBoundsInternal(byte[] imageData)
    {
        using var mat = Mat.FromImageData(imageData);
        using var gray = new Mat();
        using var blur = new Mat();
        using var edges = new Mat();

        // Convert to grayscale
        Cv2.CvtColor(mat, gray, ColorConversionCodes.BGR2GRAY);

        // Apply Gaussian blur to reduce noise
        Cv2.GaussianBlur(gray, blur, new OpenCvSharp.Size(5, 5), 0);

        // Canny edge detection
        Cv2.Canny(blur, edges, 75, 200);

        // Find contours
        Cv2.FindContours(edges, out var contours, out _, RetrievalModes.External, ContourApproximationModes.ApproxSimple);

        // Find the largest quadrilateral contour
        OpenCvSharp.Point[]? documentContour = null;
        double maxArea = 0;

        foreach (var contour in contours)
        {
            var area = Cv2.ContourArea(contour);
            if (area < 1000) continue; // Skip small contours

            var peri = Cv2.ArcLength(contour, true);
            var approx = Cv2.ApproxPolyDP(contour, 0.02 * peri, true);

            // Look for quadrilateral
            if (approx.Length == 4 && area > maxArea)
            {
                maxArea = area;
                documentContour = approx;
            }
        }

        if (documentContour == null)
            return null;

        // Order the corners: top-left, top-right, bottom-right, bottom-left
        var orderedCorners = OrderCorners(documentContour);

        return new DocumentCorners(
            new Interfaces.Point(orderedCorners[0].X, orderedCorners[0].Y),
            new Interfaces.Point(orderedCorners[1].X, orderedCorners[1].Y),
            new Interfaces.Point(orderedCorners[2].X, orderedCorners[2].Y),
            new Interfaces.Point(orderedCorners[3].X, orderedCorners[3].Y)
        );
    }

    private static OpenCvSharp.Point[] OrderCorners(OpenCvSharp.Point[] corners)
    {
        // Sort by Y to get top and bottom pairs
        var sorted = corners.OrderBy(p => p.Y).ToArray();
        var top = sorted.Take(2).OrderBy(p => p.X).ToArray();
        var bottom = sorted.Skip(2).OrderBy(p => p.X).ToArray();

        return [top[0], top[1], bottom[1], bottom[0]];
    }

    public Task<byte[]> ApplyPerspectiveCorrectionAsync(byte[] imageData, DocumentCorners corners, CancellationToken ct = default)
    {
        return Task.Run(() => ApplyPerspectiveCorrectionInternal(imageData, corners), ct);
    }

    private static byte[] ApplyPerspectiveCorrectionInternal(byte[] imageData, DocumentCorners corners)
    {
        using var mat = Mat.FromImageData(imageData);

        var srcPoints = new Point2f[]
        {
            new(corners.TopLeft.X, corners.TopLeft.Y),
            new(corners.TopRight.X, corners.TopRight.Y),
            new(corners.BottomRight.X, corners.BottomRight.Y),
            new(corners.BottomLeft.X, corners.BottomLeft.Y)
        };

        // Calculate the dimensions of the output image
        var widthTop = Math.Sqrt(Math.Pow(corners.TopRight.X - corners.TopLeft.X, 2) + Math.Pow(corners.TopRight.Y - corners.TopLeft.Y, 2));
        var widthBottom = Math.Sqrt(Math.Pow(corners.BottomRight.X - corners.BottomLeft.X, 2) + Math.Pow(corners.BottomRight.Y - corners.BottomLeft.Y, 2));
        var maxWidth = (int)Math.Max(widthTop, widthBottom);

        var heightLeft = Math.Sqrt(Math.Pow(corners.BottomLeft.X - corners.TopLeft.X, 2) + Math.Pow(corners.BottomLeft.Y - corners.TopLeft.Y, 2));
        var heightRight = Math.Sqrt(Math.Pow(corners.BottomRight.X - corners.TopRight.X, 2) + Math.Pow(corners.BottomRight.Y - corners.TopRight.Y, 2));
        var maxHeight = (int)Math.Max(heightLeft, heightRight);

        var dstPoints = new Point2f[]
        {
            new(0, 0),
            new(maxWidth - 1, 0),
            new(maxWidth - 1, maxHeight - 1),
            new(0, maxHeight - 1)
        };

        using var transform = Cv2.GetPerspectiveTransform(srcPoints, dstPoints);
        using var result = new Mat();
        Cv2.WarpPerspective(mat, result, transform, new OpenCvSharp.Size(maxWidth, maxHeight));

        return result.ToBytes(".png");
    }

    public async Task<byte[]> EnhanceAsync(byte[] imageData, EnhanceOptions options, CancellationToken ct = default)
    {
        return await Task.Run(() =>
        {
            using var mat = Mat.FromImageData(imageData);
            using var result = mat.Clone();

            if (options.AutoContrast)
            {
                // Apply CLAHE (Contrast Limited Adaptive Histogram Equalization)
                ApplyAutoContrast(result);
            }

            if (options.Sharpen)
            {
                ApplySharpen(result);
            }

            if (options.DenoiseLight)
            {
                ApplyLightDenoise(result);
            }

            if (options.Deskew)
            {
                // Deskew is handled separately as it changes image dimensions
            }

            return result.ToBytes(".png");
        }, ct);
    }

    private static void ApplyAutoContrast(Mat image)
    {
        // Simple contrast enhancement
        Cv2.ConvertScaleAbs(image, image, 1.1, 10);
    }

    private static void ApplySharpen(Mat image)
    {
        // Create sharpening kernel
        using var kernel = new Mat(3, 3, MatType.CV_32F);
        kernel.Set(0, 0, 0f);
        kernel.Set(0, 1, -1f);
        kernel.Set(0, 2, 0f);
        kernel.Set(1, 0, -1f);
        kernel.Set(1, 1, 5f);
        kernel.Set(1, 2, -1f);
        kernel.Set(2, 0, 0f);
        kernel.Set(2, 1, -1f);
        kernel.Set(2, 2, 0f);

        Cv2.Filter2D(image, image, -1, kernel);
    }

    private static void ApplyLightDenoise(Mat image)
    {
        Cv2.FastNlMeansDenoisingColored(image, image, 3, 3, 7, 21);
    }

    public async Task<byte[]> RotateAsync(byte[] imageData, int degrees, CancellationToken ct = default)
    {
        return await Task.Run(() =>
        {
            using var mat = Mat.FromImageData(imageData);
            using var result = new Mat();

            var rotateCode = degrees switch
            {
                90 => RotateFlags.Rotate90Clockwise,
                180 => RotateFlags.Rotate180,
                270 => RotateFlags.Rotate90Counterclockwise,
                _ => throw new ArgumentException("Degrees must be 90, 180, or 270", nameof(degrees))
            };

            Cv2.Rotate(mat, result, rotateCode);
            return result.ToBytes(".png");
        }, ct);
    }

    public async Task<byte[]> CropAsync(byte[] imageData, CropBounds bounds, CancellationToken ct = default)
    {
        return await Task.Run(() =>
        {
            using var mat = Mat.FromImageData(imageData);
            var rect = new OpenCvSharp.Rect(bounds.X, bounds.Y, bounds.Width, bounds.Height);
            using var cropped = new Mat(mat, rect);
            return cropped.ToBytes(".png");
        }, ct);
    }

    public async Task<byte[]> GenerateThumbnailAsync(byte[] imageData, int maxWidth, int maxHeight, CancellationToken ct = default)
    {
        return await Task.Run(() =>
        {
            using var mat = Mat.FromImageData(imageData);

            // Calculate new size maintaining aspect ratio
            var ratio = Math.Min((double)maxWidth / mat.Width, (double)maxHeight / mat.Height);
            var newWidth = (int)(mat.Width * ratio);
            var newHeight = (int)(mat.Height * ratio);

            using var thumbnail = new Mat();
            Cv2.Resize(mat, thumbnail, new OpenCvSharp.Size(newWidth, newHeight), interpolation: InterpolationFlags.Area);

            return thumbnail.ToBytes(".png");
        }, ct);
    }

    public async Task<byte[]> ConvertToGrayscaleAsync(byte[] imageData, CancellationToken ct = default)
    {
        return await Task.Run(() =>
        {
            using var mat = Mat.FromImageData(imageData);
            using var gray = new Mat();
            Cv2.CvtColor(mat, gray, ColorConversionCodes.BGR2GRAY);
            return gray.ToBytes(".png");
        }, ct);
    }

    public async Task<byte[]> AdjustBrightnessContrastAsync(byte[] imageData, double brightness, double contrast, CancellationToken ct = default)
    {
        return await Task.Run(() =>
        {
            using var mat = Mat.FromImageData(imageData);
            using var result = new Mat();

            // brightness: add to each pixel (-255 to 255)
            // contrast: multiply each pixel (0.0 to 3.0, 1.0 = no change)
            mat.ConvertTo(result, -1, contrast, brightness);

            return result.ToBytes(".png");
        }, ct);
    }

    public async Task<(int Width, int Height)> GetImageDimensionsAsync(byte[] imageData, CancellationToken ct = default)
    {
        return await Task.Run(() =>
        {
            using var mat = Mat.FromImageData(imageData);
            return (mat.Width, mat.Height);
        }, ct);
    }
}
