using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Navigation;
using Scanner.App.Views;
using Scanner.Core.Services;
using Scanner.Core.Services.Interfaces;
using Scanner.Infrastructure.WIA;

namespace Scanner.App;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App : Application
{
    private Window? _window;

    /// <summary>
    /// Gets the current application instance.
    /// </summary>
    public new static App Current => (App)Application.Current;

    /// <summary>
    /// Gets the service provider for dependency injection.
    /// </summary>
    public IServiceProvider Services { get; }

    /// <summary>
    /// Gets the main window.
    /// </summary>
    public Window? MainWindow => _window;

    public App()
    {
        Services = ConfigureServices();
        this.InitializeComponent();
        this.UnhandledException += OnUnhandledException;
    }

    private void OnUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        e.Handled = true;
        var dialog = new Microsoft.UI.Xaml.Controls.ContentDialog
        {
            Title = "Unexpected Error",
            Content = $"An unexpected error occurred:\n\n{e.Message}\n\nThe application will continue running, but some features may not work correctly.",
            CloseButtonText = "OK",
            XamlRoot = _window?.Content?.XamlRoot
        };
        _ = dialog.ShowAsync();
    }

    /// <summary>
    /// Configures services for dependency injection.
    /// </summary>
    private static IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        // Register services
        services.AddSingleton<IScannerService, ScannerService>();
        services.AddSingleton<IImageProcessingService, ImageProcessingService>();

        // Settings ViewModel as singleton so all consumers share the same instance
        services.AddSingleton<SettingsViewModel>();

        // OCR Service - Use Qwen2.5-VL via OpenRouter for advanced OCR
        // API key is loaded from user settings (configured in Settings page)
        services.AddSingleton<IOcrService>(sp =>
        {
            var settings = sp.GetRequiredService<SettingsViewModel>();
            var apiKey = settings.OpenRouterApiKey;
            return new QwenOcrService(apiKey, "qwen-vl-7b");
        });

        // Also register Tesseract as fallback for offline use
        services.AddSingleton<OcrService>();

        services.AddSingleton<IExportService, ExportService>();
        services.AddSingleton<OneDriveService>();
        services.AddSingleton<GoogleDriveService>();
        services.AddSingleton<ICloudStorageService>(sp => sp.GetRequiredService<OneDriveService>());
        services.AddSingleton<ICloudStorageService>(sp => sp.GetRequiredService<GoogleDriveService>());

        // Register ViewModels
        services.AddTransient<ScannerViewModel>();
        services.AddTransient<DocumentViewModel>();
        services.AddTransient<ExportViewModel>(sp => new ExportViewModel(
            sp.GetRequiredService<IExportService>(),
            sp.GetServices<ICloudStorageService>()));

        return services.BuildServiceProvider();
    }

    /// <summary>
    /// Gets a service from the DI container.
    /// </summary>
    public static T GetService<T>() where T : class
    {
        var service = Current.Services.GetService<T>();
        return service ?? throw new InvalidOperationException($"Service {typeof(T).Name} not registered.");
    }

    protected override void OnLaunched(LaunchActivatedEventArgs e)
    {
        _window = new Window();
        _window.Title = "Scanner";
        _window.ExtendsContentIntoTitleBar = true;

        if (_window.Content is not Frame rootFrame)
        {
            rootFrame = new Frame();
            rootFrame.NavigationFailed += OnNavigationFailed;
            _window.Content = rootFrame;
        }

        rootFrame.Navigate(typeof(ShellPage), e.Arguments);
        _window.Activate();
    }

    private void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
    {
        var dialog = new Microsoft.UI.Xaml.Controls.ContentDialog
        {
            Title = "Navigation Error",
            Content = $"Failed to load page '{e.SourcePageType.Name}'.\n\n{e.Exception?.Message}",
            CloseButtonText = "OK",
            XamlRoot = _window?.Content?.XamlRoot
        };
        _ = dialog.ShowAsync();
    }
}
