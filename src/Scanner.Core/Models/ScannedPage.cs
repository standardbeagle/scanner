using CommunityToolkit.Mvvm.ComponentModel;

namespace Scanner.Core.Models;

/// <summary>
/// Represents a single scanned page with its image data and metadata.
/// </summary>
public partial class ScannedPage : ObservableObject
{
    [ObservableProperty]
    private string _id = Guid.NewGuid().ToString();

    [ObservableProperty]
    private byte[] _imageData = [];

    [ObservableProperty]
    private byte[]? _thumbnailData;

    [ObservableProperty]
    private int _pageNumber;

    [ObservableProperty]
    private int _rotation; // 0, 90, 180, 270

    [ObservableProperty]
    private CropBounds? _cropBounds;

    [ObservableProperty]
    private bool _isEnhanced;

    [ObservableProperty]
    private string? _ocrText;

    [ObservableProperty]
    private OcrResult? _ocrResult;

    [ObservableProperty]
    private DateTime _scannedAt = DateTime.UtcNow;

    [ObservableProperty]
    private int _width;

    [ObservableProperty]
    private int _height;

    [ObservableProperty]
    private int _dpi;
}

/// <summary>
/// Defines a rectangular crop region.
/// </summary>
public record CropBounds(int X, int Y, int Width, int Height);
