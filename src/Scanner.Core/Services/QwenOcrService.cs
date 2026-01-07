using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Scanner.Core.Models;
using Scanner.Core.Services.Interfaces;

namespace Scanner.Core.Services;

/// <summary>
/// OCR service using Qwen2.5-VL via OpenRouter API for advanced text recognition.
/// </summary>
public class QwenOcrService : IOcrService, IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _model;
    private bool _isInitialized;
    private bool _disposed;

    private const string OpenRouterBaseUrl = "https://openrouter.ai/api/v1/chat/completions";

    // Available Qwen vision models on OpenRouter
    private static readonly Dictionary<string, string> AvailableModels = new()
    {
        ["qwen-vl-72b"] = "qwen/qwen2.5-vl-72b-instruct",
        ["qwen-vl-7b"] = "qwen/qwen2.5-vl-7b-instruct",
        ["qwen-vl-3b"] = "qwen/qwen2.5-vl-3b-instruct"
    };

    public bool IsInitialized => _isInitialized;

    /// <summary>
    /// Creates a new QwenOcrService with the specified API key.
    /// </summary>
    /// <param name="apiKey">OpenRouter API key</param>
    /// <param name="model">Model to use: qwen-vl-72b, qwen-vl-7b, or qwen-vl-3b (default: qwen-vl-7b)</param>
    public QwenOcrService(string apiKey, string model = "qwen-vl-7b")
    {
        _apiKey = apiKey ?? "";
        _model = AvailableModels.GetValueOrDefault(model, AvailableModels["qwen-vl-7b"]);
        _httpClient = new HttpClient();
        if (!string.IsNullOrEmpty(_apiKey))
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        }
        _httpClient.DefaultRequestHeaders.Add("HTTP-Referer", "https://scanner-app.local");
        _httpClient.DefaultRequestHeaders.Add("X-Title", "Scanner App OCR");
    }

    public Task InitializeAsync(CancellationToken ct = default)
    {
        _isInitialized = true;
        return Task.CompletedTask;
    }

    public IReadOnlyList<string> GetAvailableLanguages()
    {
        // Qwen2.5-VL supports many languages automatically
        return new List<string>
        {
            "auto",      // Auto-detect
            "eng",       // English
            "chi_sim",   // Simplified Chinese
            "chi_tra",   // Traditional Chinese
            "jpn",       // Japanese
            "kor",       // Korean
            "fra",       // French
            "deu",       // German
            "spa",       // Spanish
            "ita",       // Italian
            "por",       // Portuguese
            "rus",       // Russian
            "ara",       // Arabic
            "hin",       // Hindi
            "vie",       // Vietnamese
            "tha",       // Thai
        }.AsReadOnly();
    }

    public async Task<OcrResult> RecognizeAsync(
        byte[] imageData,
        string language = "eng",
        CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(_apiKey))
            throw new InvalidOperationException("OpenRouter API key is not configured. Please set it in Settings.");

        if (!_isInitialized)
            await InitializeAsync(ct);

        var base64Image = Convert.ToBase64String(imageData);
        var mimeType = DetectImageMimeType(imageData);

        var prompt = BuildOcrPrompt(language, includeLayout: false);

        var request = new OpenRouterRequest
        {
            Model = _model,
            Messages =
            [
                new OpenRouterMessage
                {
                    Role = "user",
                    Content =
                    [
                        new ContentPart { Type = "text", Text = prompt },
                        new ContentPart
                        {
                            Type = "image_url",
                            ImageUrl = new ImageUrlContent
                            {
                                Url = $"data:{mimeType};base64,{base64Image}"
                            }
                        }
                    ]
                }
            ],
            MaxTokens = 4096,
            Temperature = 0.1
        };

        var response = await SendRequestAsync(request, ct);
        var text = ExtractTextContent(response);

        return new OcrResult(
            text,
            0.95, // Qwen2.5-VL typically has high confidence
            [],
            []
        );
    }

    public async Task<OcrResult> RecognizeWithLayoutAsync(
        byte[] imageData,
        string language = "eng",
        CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(_apiKey))
            throw new InvalidOperationException("OpenRouter API key is not configured. Please set it in Settings.");

        if (!_isInitialized)
            await InitializeAsync(ct);

        var base64Image = Convert.ToBase64String(imageData);
        var mimeType = DetectImageMimeType(imageData);

        var prompt = BuildOcrPrompt(language, includeLayout: true);

        var request = new OpenRouterRequest
        {
            Model = _model,
            Messages =
            [
                new OpenRouterMessage
                {
                    Role = "user",
                    Content =
                    [
                        new ContentPart { Type = "text", Text = prompt },
                        new ContentPart
                        {
                            Type = "image_url",
                            ImageUrl = new ImageUrlContent
                            {
                                Url = $"data:{mimeType};base64,{base64Image}"
                            }
                        }
                    ]
                }
            ],
            MaxTokens = 8192,
            Temperature = 0.1
        };

        var response = await SendRequestAsync(request, ct);
        var content = ExtractTextContent(response);

        // Parse the structured response
        return ParseLayoutResponse(content);
    }

    private static string BuildOcrPrompt(string language, bool includeLayout)
    {
        var languageHint = language switch
        {
            "eng" => "English",
            "chi_sim" => "Simplified Chinese",
            "chi_tra" => "Traditional Chinese",
            "jpn" => "Japanese",
            "kor" => "Korean",
            "fra" => "French",
            "deu" => "German",
            "spa" => "Spanish",
            "auto" => "any language present",
            _ => language
        };

        if (includeLayout)
        {
            return $$"""
                You are an OCR engine. Extract ALL text from this scanned document image.
                The document may contain {{languageHint}} text.

                Return the result in this exact JSON format:
                {
                    "text": "full extracted text here with newlines preserved",
                    "words": [
                        {"text": "word", "x": 0, "y": 0, "width": 100, "height": 20, "confidence": 0.99},
                        ...
                    ],
                    "blocks": [
                        {"text": "paragraph text", "x": 0, "y": 0, "width": 500, "height": 100},
                        ...
                    ]
                }

                Coordinates should be approximate pixel positions from top-left.
                Extract EVERY word visible in the image. Maintain reading order.
                If you cannot determine exact coordinates, estimate based on visual position.
                """;
        }
        else
        {
            return $"""
                You are an OCR engine. Extract ALL text from this scanned document image.
                The document may contain {languageHint} text.

                Return ONLY the extracted text, preserving the original layout and line breaks.
                Do not add any commentary or explanation, just the raw extracted text.
                """;
        }
    }

    private async Task<OpenRouterResponse> SendRequestAsync(OpenRouterRequest request, CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(request, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });

        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(OpenRouterBaseUrl, content, ct);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct);
            throw new HttpRequestException($"OpenRouter API error ({response.StatusCode}): {errorBody}");
        }

        var responseJson = await response.Content.ReadAsStringAsync(ct);
        var result = JsonSerializer.Deserialize<OpenRouterResponse>(responseJson, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        });

        return result ?? throw new InvalidOperationException("Empty response from OpenRouter API");
    }

    private static string ExtractTextContent(OpenRouterResponse response)
    {
        if (response.Choices == null || response.Choices.Count == 0)
            return string.Empty;

        return response.Choices[0].Message?.Content ?? string.Empty;
    }

    private static OcrResult ParseLayoutResponse(string content)
    {
        // Try to extract JSON from the response
        var jsonMatch = Regex.Match(content, @"\{[\s\S]*\}", RegexOptions.Singleline);
        if (!jsonMatch.Success)
        {
            // Fallback: treat entire content as text
            return new OcrResult(content, 0.95, [], []);
        }

        try
        {
            var layoutResult = JsonSerializer.Deserialize<LayoutOcrResponse>(
                jsonMatch.Value,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );

            if (layoutResult == null)
                return new OcrResult(content, 0.95, [], []);

            var words = layoutResult.Words?.Select(w => new OcrWord(
                w.Text ?? "",
                new OcrBounds(w.X, w.Y, w.Width, w.Height),
                w.Confidence
            )).ToList() ?? [];

            var blocks = layoutResult.Blocks?.Select(b => new OcrBlock(
                b.Text ?? "",
                new OcrBounds(b.X, b.Y, b.Width, b.Height),
                words.Where(w =>
                    w.Bounds.X >= b.X &&
                    w.Bounds.Y >= b.Y &&
                    w.Bounds.X + w.Bounds.Width <= b.X + b.Width &&
                    w.Bounds.Y + w.Bounds.Height <= b.Y + b.Height
                ).ToList()
            )).ToList() ?? [];

            return new OcrResult(
                layoutResult.Text ?? content,
                0.95,
                words,
                blocks
            );
        }
        catch (JsonException)
        {
            // JSON parsing failed, return text only
            return new OcrResult(content, 0.95, [], []);
        }
    }

    private static string DetectImageMimeType(byte[] imageData)
    {
        if (imageData.Length < 4)
            return "image/jpeg";

        // Check magic bytes
        if (imageData[0] == 0xFF && imageData[1] == 0xD8)
            return "image/jpeg";
        if (imageData[0] == 0x89 && imageData[1] == 0x50 && imageData[2] == 0x4E && imageData[3] == 0x47)
            return "image/png";
        if (imageData[0] == 0x47 && imageData[1] == 0x49 && imageData[2] == 0x46)
            return "image/gif";
        if (imageData[0] == 0x42 && imageData[1] == 0x4D)
            return "image/bmp";
        if (imageData[0] == 0x49 && imageData[1] == 0x49 && imageData[2] == 0x2A && imageData[3] == 0x00)
            return "image/tiff";
        if (imageData[0] == 0x4D && imageData[1] == 0x4D && imageData[2] == 0x00 && imageData[3] == 0x2A)
            return "image/tiff";

        return "image/jpeg"; // Default
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _httpClient.Dispose();
            _disposed = true;
            GC.SuppressFinalize(this);
        }
    }

    #region OpenRouter API Models

    private class OpenRouterRequest
    {
        public string Model { get; set; } = "";
        public List<OpenRouterMessage> Messages { get; set; } = [];
        public int MaxTokens { get; set; }
        public double Temperature { get; set; }
    }

    private class OpenRouterMessage
    {
        public string Role { get; set; } = "";
        public List<ContentPart> Content { get; set; } = [];
    }

    private class ContentPart
    {
        public string Type { get; set; } = "";
        public string? Text { get; set; }
        public ImageUrlContent? ImageUrl { get; set; }
    }

    private class ImageUrlContent
    {
        public string Url { get; set; } = "";
    }

    private class OpenRouterResponse
    {
        public List<Choice>? Choices { get; set; }
        public Usage? Usage { get; set; }
    }

    private class Choice
    {
        public ResponseMessage? Message { get; set; }
        public int Index { get; set; }
    }

    private class ResponseMessage
    {
        public string? Role { get; set; }
        public string? Content { get; set; }
    }

    private class Usage
    {
        public int PromptTokens { get; set; }
        public int CompletionTokens { get; set; }
        public int TotalTokens { get; set; }
    }

    private class LayoutOcrResponse
    {
        public string? Text { get; set; }
        public List<WordLayout>? Words { get; set; }
        public List<BlockLayout>? Blocks { get; set; }
    }

    private class WordLayout
    {
        public string? Text { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public double Confidence { get; set; } = 0.95;
    }

    private class BlockLayout
    {
        public string? Text { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
    }

    #endregion
}
