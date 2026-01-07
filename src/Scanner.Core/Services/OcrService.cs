using Scanner.Core.Models;
using Scanner.Core.Services.Interfaces;
using Tesseract;

namespace Scanner.Core.Services;

/// <summary>
/// Service for OCR text recognition using Tesseract.
/// </summary>
public class OcrService : IOcrService, IDisposable
{
    private TesseractEngine? _engine;
    private readonly string _tessDataPath;
    private readonly List<string> _availableLanguages = ["eng"];
    private bool _disposed;

    public bool IsInitialized => _engine != null;

    public OcrService()
    {
        // Look for tessdata in common locations
        _tessDataPath = FindTessDataPath() ?? "./tessdata";
    }

    public OcrService(string tessDataPath)
    {
        _tessDataPath = tessDataPath;
    }

    private static string? FindTessDataPath()
    {
        var searchPaths = new[]
        {
            "./tessdata",
            "../tessdata",
            Path.Combine(AppContext.BaseDirectory, "tessdata"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Tesseract-OCR", "tessdata"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Tesseract-OCR", "tessdata"),
        };

        foreach (var path in searchPaths)
        {
            if (Directory.Exists(path))
            {
                return path;
            }
        }

        return null;
    }

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        if (_engine != null) return;

        await Task.Run(() =>
        {
            try
            {
                _engine = new TesseractEngine(_tessDataPath, "eng", EngineMode.Default);

                // Discover available languages
                DiscoverAvailableLanguages();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to initialize Tesseract OCR. Ensure tessdata is available at: {_tessDataPath}", ex);
            }
        }, ct);
    }

    private void DiscoverAvailableLanguages()
    {
        _availableLanguages.Clear();

        if (Directory.Exists(_tessDataPath))
        {
            var trainedDataFiles = Directory.GetFiles(_tessDataPath, "*.traineddata");
            foreach (var file in trainedDataFiles)
            {
                var lang = Path.GetFileNameWithoutExtension(file);
                _availableLanguages.Add(lang);
            }
        }

        if (_availableLanguages.Count == 0)
        {
            _availableLanguages.Add("eng");
        }
    }

    public IReadOnlyList<string> GetAvailableLanguages() => _availableLanguages.AsReadOnly();

    public async Task<OcrResult> RecognizeAsync(byte[] imageData, string language = "eng", CancellationToken ct = default)
    {
        await EnsureInitializedAsync(language, ct);

        return await Task.Run(() =>
        {
            using var pix = Pix.LoadFromMemory(imageData);
            using var page = _engine!.Process(pix);

            var text = page.GetText();
            var confidence = page.GetMeanConfidence();

            return new OcrResult(
                text,
                confidence,
                [],
                []
            );
        }, ct);
    }

    public async Task<OcrResult> RecognizeWithLayoutAsync(byte[] imageData, string language = "eng", CancellationToken ct = default)
    {
        await EnsureInitializedAsync(language, ct);

        return await Task.Run(() =>
        {
            using var pix = Pix.LoadFromMemory(imageData);
            using var page = _engine!.Process(pix);

            var text = page.GetText();
            var confidence = page.GetMeanConfidence();

            var words = new List<OcrWord>();
            var blocks = new List<OcrBlock>();

            // Extract word-level information using the iterator
            using var iter = page.GetIterator();

            iter.Begin();

            do
            {
                if (iter.TryGetBoundingBox(PageIteratorLevel.Word, out var bounds))
                {
                    var wordText = iter.GetText(PageIteratorLevel.Word);
                    var wordConfidence = iter.GetConfidence(PageIteratorLevel.Word);

                    if (!string.IsNullOrWhiteSpace(wordText))
                    {
                        words.Add(new OcrWord(
                            wordText.Trim(),
                            new OcrBounds(bounds.X1, bounds.Y1, bounds.Width, bounds.Height),
                            wordConfidence / 100.0
                        ));
                    }
                }
            } while (iter.Next(PageIteratorLevel.Word));

            // Extract block-level information
            iter.Begin();

            do
            {
                if (iter.TryGetBoundingBox(PageIteratorLevel.Block, out var bounds))
                {
                    var blockText = iter.GetText(PageIteratorLevel.Block);
                    if (!string.IsNullOrWhiteSpace(blockText))
                    {
                        var blockWords = words.Where(w =>
                            w.Bounds.X >= bounds.X1 &&
                            w.Bounds.Y >= bounds.Y1 &&
                            w.Bounds.X + w.Bounds.Width <= bounds.X2 &&
                            w.Bounds.Y + w.Bounds.Height <= bounds.Y2
                        ).ToList();

                        blocks.Add(new OcrBlock(
                            blockText.Trim(),
                            new OcrBounds(bounds.X1, bounds.Y1, bounds.Width, bounds.Height),
                            blockWords
                        ));
                    }
                }
            } while (iter.Next(PageIteratorLevel.Block));

            return new OcrResult(text, confidence, words, blocks);
        }, ct);
    }

    private async Task EnsureInitializedAsync(string language, CancellationToken ct)
    {
        if (_engine == null)
        {
            await InitializeAsync(ct);
        }

        // Reinitialize with different language if needed
        // Note: Tesseract engine language is set at initialization time
        // For production, you might want to manage multiple engines or reinitialize
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _engine?.Dispose();
            _engine = null;
            _disposed = true;
            GC.SuppressFinalize(this);
        }
    }
}
