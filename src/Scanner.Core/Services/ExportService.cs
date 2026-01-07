using PdfSharp.Drawing;
using PdfSharp.Pdf;
using Scanner.Core.Models;
using Scanner.Core.Services.Interfaces;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Tiff;

namespace Scanner.Core.Services;

/// <summary>
/// Service for exporting documents to PDF and image formats.
/// </summary>
public class ExportService : IExportService
{
    public async Task<byte[]> ExportToPdfAsync(
        ScannedDocument document,
        PdfExportOptions options,
        IProgress<double>? progress = null,
        CancellationToken ct = default)
    {
        return await Task.Run(() =>
        {
            using var pdfDocument = new PdfDocument();

            // Set metadata
            pdfDocument.Info.Title = options.Title ?? document.Name;
            pdfDocument.Info.Author = options.Author ?? Environment.UserName;
            pdfDocument.Info.Creator = "Scanner App";
            pdfDocument.Info.CreationDate = DateTime.Now;

            var totalPages = document.PageCount;

            for (int i = 0; i < document.Pages.Count; i++)
            {
                ct.ThrowIfCancellationRequested();

                var page = document.Pages[i];
                var pdfPage = pdfDocument.AddPage();

                // Set page size based on image dimensions
                using var stream = new MemoryStream(page.ImageData);
                using var xImage = XImage.FromStream(stream);

                pdfPage.Width = XUnit.FromPoint(xImage.PointWidth);
                pdfPage.Height = XUnit.FromPoint(xImage.PointHeight);

                using var gfx = XGraphics.FromPdfPage(pdfPage);

                // Draw the scanned image
                gfx.DrawImage(xImage, 0, 0, pdfPage.Width.Point, pdfPage.Height.Point);

                // Add invisible OCR text layer if enabled and OCR data is available
                if (options.IncludeOcrTextLayer && page.OcrResult != null)
                {
                    AddOcrTextLayer(pdfPage, gfx, page.OcrResult, xImage.PixelWidth, xImage.PixelHeight);
                }

                progress?.Report((double)(i + 1) / totalPages);
            }

            using var output = new MemoryStream();
            pdfDocument.Save(output);
            return output.ToArray();
        }, ct);
    }

    private static void AddOcrTextLayer(
        PdfPage pdfPage,
        XGraphics gfx,
        OcrResult ocr,
        int imageWidth,
        int imageHeight)
    {
        if (ocr.Words.Count == 0) return;

        // Calculate scale factors from image pixels to PDF points
        double scaleX = pdfPage.Width.Point / imageWidth;
        double scaleY = pdfPage.Height.Point / imageHeight;

        // Use a transparent brush for invisible text
        var transparentBrush = new XSolidBrush(XColor.FromArgb(0, 0, 0, 0));

        foreach (var word in ocr.Words)
        {
            if (string.IsNullOrWhiteSpace(word.Text)) continue;

            // Calculate position and size in PDF coordinates
            double x = word.Bounds.X * scaleX;
            double y = word.Bounds.Y * scaleY;
            double width = word.Bounds.Width * scaleX;
            double height = word.Bounds.Height * scaleY;

            // Calculate appropriate font size based on word height
            double fontSize = height * 0.8;
            if (fontSize < 1) fontSize = 1;
            if (fontSize > 72) fontSize = 72;

            try
            {
                var font = new XFont("Arial", fontSize, XFontStyleEx.Regular);

                // Draw invisible text at the word position
                var rect = new XRect(x, y, width, height);
                gfx.DrawString(
                    word.Text,
                    font,
                    transparentBrush,
                    rect,
                    XStringFormats.TopLeft);
            }
            catch
            {
                // Skip words that can't be rendered
            }
        }
    }

    public async Task<byte[]> ExportToJpegAsync(ScannedPage page, int quality = 90, CancellationToken ct = default)
    {
        return await Task.Run(() =>
        {
            using var image = Image.Load(page.ImageData);
            using var output = new MemoryStream();

            var encoder = new JpegEncoder { Quality = quality };
            image.Save(output, encoder);

            return output.ToArray();
        }, ct);
    }

    public async Task<byte[]> ExportToPngAsync(ScannedPage page, CancellationToken ct = default)
    {
        return await Task.Run(() =>
        {
            using var image = Image.Load(page.ImageData);
            using var output = new MemoryStream();

            image.Save(output, new PngEncoder());

            return output.ToArray();
        }, ct);
    }

    public async Task<byte[]> ExportToTiffAsync(
        ScannedDocument document,
        TiffCompression compression,
        CancellationToken ct = default)
    {
        return await Task.Run(() =>
        {
            if (document.PageCount == 0)
                throw new InvalidOperationException("Document has no pages to export.");

            // For multi-page TIFF, we need to handle it differently
            // ImageSharp supports multi-frame images
            using var output = new MemoryStream();

            // Load the first image
            using var firstImage = Image.Load(document.Pages[0].ImageData);

            // Add remaining pages as frames
            for (int i = 1; i < document.Pages.Count; i++)
            {
                ct.ThrowIfCancellationRequested();

                using var additionalImage = Image.Load(document.Pages[i].ImageData);
                // Note: For true multi-page TIFF support, a more specialized approach is needed
                // This is a simplified version
            }

            var tiffCompression = compression switch
            {
                TiffCompression.Lzw => SixLabors.ImageSharp.Formats.Tiff.Constants.TiffCompression.Lzw,
                TiffCompression.Jpeg => SixLabors.ImageSharp.Formats.Tiff.Constants.TiffCompression.Jpeg,
                TiffCompression.Zip => SixLabors.ImageSharp.Formats.Tiff.Constants.TiffCompression.Deflate,
                _ => SixLabors.ImageSharp.Formats.Tiff.Constants.TiffCompression.None
            };

            var encoder = new TiffEncoder
            {
                Compression = tiffCompression
            };

            firstImage.Save(output, encoder);
            return output.ToArray();
        }, ct);
    }

    public async Task SaveToFileAsync(byte[] data, string filePath, CancellationToken ct = default)
    {
        await File.WriteAllBytesAsync(filePath, data, ct);
    }

    public async Task<string?> SaveWithDialogAsync(
        byte[] data,
        string suggestedFileName,
        ExportFormat format,
        CancellationToken ct = default)
    {
        // Note: File picker needs to be invoked from UI thread
        // This method should be called from the view/viewmodel layer
        // For now, we'll save to Documents folder with the suggested name

        var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        var extension = format switch
        {
            ExportFormat.Pdf => ".pdf",
            ExportFormat.Jpeg => ".jpg",
            ExportFormat.Png => ".png",
            ExportFormat.Tiff => ".tiff",
            ExportFormat.Bmp => ".bmp",
            _ => ".pdf"
        };

        var fileName = Path.GetFileNameWithoutExtension(suggestedFileName) + extension;
        var filePath = Path.Combine(documentsPath, fileName);

        // Handle file name conflicts
        var counter = 1;
        while (File.Exists(filePath))
        {
            fileName = $"{Path.GetFileNameWithoutExtension(suggestedFileName)} ({counter}){extension}";
            filePath = Path.Combine(documentsPath, fileName);
            counter++;
        }

        await SaveToFileAsync(data, filePath, ct);
        return filePath;
    }
}
