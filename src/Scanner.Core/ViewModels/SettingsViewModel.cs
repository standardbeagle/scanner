using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Scanner.Core.Models;

namespace Scanner.Core.Services;

/// <summary>
/// ViewModel for application settings.
/// </summary>
public partial class SettingsViewModel : ObservableObject
{
    private static readonly string SettingsDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ScannerApp");

    private static readonly string SettingsFilePath = Path.Combine(SettingsDirectory, "settings.json");

    [ObservableProperty]
    private int _defaultDpi = 300;

    [ObservableProperty]
    private ColorMode _defaultColorMode = ColorMode.Color;

    [ObservableProperty]
    private PaperSize _defaultPaperSize = PaperSize.Letter;

    [ObservableProperty]
    private bool _autoCropEnabled = true;

    [ObservableProperty]
    private bool _autoEnhanceEnabled = true;

    [ObservableProperty]
    private bool _autoOcrEnabled = true;

    [ObservableProperty]
    private string _ocrLanguage = "eng";

    [ObservableProperty]
    private string _defaultSavePath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

    [ObservableProperty]
    private ExportFormat _defaultExportFormat = ExportFormat.Pdf;

    [ObservableProperty]
    private bool _darkModeEnabled;

    [ObservableProperty]
    private bool _soundEnabled = true;

    [ObservableProperty]
    private string _appVersion = "1.0.0";

    [ObservableProperty]
    private string _openRouterApiKey = "";

    public bool IsApiKeyMissing => string.IsNullOrWhiteSpace(OpenRouterApiKey);

    /// <summary>
    /// Event raised when the API key changes and services need reconfiguration.
    /// </summary>
    public event EventHandler? ApiKeyChanged;

    partial void OnOpenRouterApiKeyChanged(string value)
    {
        OnPropertyChanged(nameof(IsApiKeyMissing));
        SaveSettings();
        ApiKeyChanged?.Invoke(this, EventArgs.Empty);
    }

    public SettingsViewModel()
    {
        LoadSettings();
    }

    /// <summary>
    /// Loads settings from local JSON file.
    /// </summary>
    private void LoadSettings()
    {
        if (!File.Exists(SettingsFilePath))
            return;

        try
        {
            var json = File.ReadAllText(SettingsFilePath);
            var data = JsonSerializer.Deserialize<SettingsData>(json);
            if (data == null) return;

            _defaultDpi = data.DefaultDpi ?? _defaultDpi;
            _autoCropEnabled = data.AutoCropEnabled ?? _autoCropEnabled;
            _autoEnhanceEnabled = data.AutoEnhanceEnabled ?? _autoEnhanceEnabled;
            _autoOcrEnabled = data.AutoOcrEnabled ?? _autoOcrEnabled;
            _ocrLanguage = data.OcrLanguage ?? _ocrLanguage;
            _defaultSavePath = data.DefaultSavePath ?? _defaultSavePath;
            _darkModeEnabled = data.DarkModeEnabled ?? _darkModeEnabled;
            _soundEnabled = data.SoundEnabled ?? _soundEnabled;
            _openRouterApiKey = data.OpenRouterApiKey ?? _openRouterApiKey;
        }
        catch (Exception)
        {
            // If settings file is corrupt, use defaults
        }
    }

    /// <summary>
    /// Saves settings to local JSON file.
    /// </summary>
    [RelayCommand]
    private void SaveSettings()
    {
        try
        {
            Directory.CreateDirectory(SettingsDirectory);

            var data = new SettingsData
            {
                DefaultDpi = DefaultDpi,
                AutoCropEnabled = AutoCropEnabled,
                AutoEnhanceEnabled = AutoEnhanceEnabled,
                AutoOcrEnabled = AutoOcrEnabled,
                OcrLanguage = OcrLanguage,
                DefaultSavePath = DefaultSavePath,
                DarkModeEnabled = DarkModeEnabled,
                SoundEnabled = SoundEnabled,
                OpenRouterApiKey = OpenRouterApiKey,
            };

            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsFilePath, json);
        }
        catch (Exception)
        {
            // Silently fail if we can't write settings
        }
    }

    [RelayCommand]
    private void ResetToDefaults()
    {
        DefaultDpi = 300;
        DefaultColorMode = ColorMode.Color;
        DefaultPaperSize = PaperSize.Letter;
        AutoCropEnabled = true;
        AutoEnhanceEnabled = true;
        AutoOcrEnabled = true;
        OcrLanguage = "eng";
        DefaultExportFormat = ExportFormat.Pdf;
        DarkModeEnabled = false;
        SoundEnabled = true;
        // Intentionally do NOT reset the API key
    }

    [RelayCommand]
    private async Task SelectDefaultSavePathAsync()
    {
        // TODO: Open folder picker dialog
        await Task.CompletedTask;
    }

    /// <summary>
    /// Creates scan settings from the current defaults.
    /// </summary>
    public ScanSettings CreateDefaultScanSettings() => new()
    {
        Dpi = DefaultDpi,
        ColorMode = DefaultColorMode,
        PaperSize = DefaultPaperSize,
        AutoCrop = AutoCropEnabled,
        AutoEnhance = AutoEnhanceEnabled,
        AutoOcr = AutoOcrEnabled,
        OcrLanguage = OcrLanguage
    };

    /// <summary>
    /// Available OCR languages.
    /// </summary>
    public IReadOnlyList<OcrLanguageOption> AvailableLanguages { get; } =
    [
        new("eng", "English"),
        new("fra", "French"),
        new("deu", "German"),
        new("spa", "Spanish"),
        new("ita", "Italian"),
        new("por", "Portuguese"),
        new("nld", "Dutch"),
        new("pol", "Polish"),
        new("rus", "Russian"),
        new("chi_sim", "Chinese (Simplified)"),
        new("chi_tra", "Chinese (Traditional)"),
        new("jpn", "Japanese"),
        new("kor", "Korean"),
        new("ara", "Arabic"),
    ];

    /// <summary>
    /// Available DPI options.
    /// </summary>
    public IReadOnlyList<int> AvailableDpiOptions { get; } = [75, 100, 150, 200, 300, 400, 600, 1200];
}

/// <summary>
/// OCR language option for UI display.
/// </summary>
public record OcrLanguageOption(string Code, string Name)
{
    public override string ToString() => Name;
}

/// <summary>
/// Serializable settings data for JSON persistence.
/// </summary>
internal class SettingsData
{
    public int? DefaultDpi { get; set; }
    public bool? AutoCropEnabled { get; set; }
    public bool? AutoEnhanceEnabled { get; set; }
    public bool? AutoOcrEnabled { get; set; }
    public string? OcrLanguage { get; set; }
    public string? DefaultSavePath { get; set; }
    public bool? DarkModeEnabled { get; set; }
    public bool? SoundEnabled { get; set; }
    public string? OpenRouterApiKey { get; set; }
}
