namespace Scanner.Core.Models;

/// <summary>
/// Results from OCR text recognition.
/// </summary>
public record OcrResult(
    string Text,
    double Confidence,
    IReadOnlyList<OcrWord> Words,
    IReadOnlyList<OcrBlock> Blocks
);

/// <summary>
/// A recognized word with its bounding box.
/// </summary>
public record OcrWord(
    string Text,
    OcrBounds Bounds,
    double Confidence
);

/// <summary>
/// A text block (paragraph) with its words.
/// </summary>
public record OcrBlock(
    string Text,
    OcrBounds Bounds,
    IReadOnlyList<OcrWord> Words
);

/// <summary>
/// Bounding box for OCR elements.
/// </summary>
public record OcrBounds(int X, int Y, int Width, int Height);
