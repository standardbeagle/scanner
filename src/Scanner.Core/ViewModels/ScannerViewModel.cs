using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Scanner.Core.Events;
using Scanner.Core.Models;
using Scanner.Core.Services.Interfaces;

namespace Scanner.Core.Services;

/// <summary>
/// ViewModel for the main scanner interface.
/// </summary>
public partial class ScannerViewModel : ObservableObject
{
    private readonly IScannerService _scannerService;
    private CancellationTokenSource? _scanCts;

    [ObservableProperty]
    private ObservableCollection<ScannerDevice> _availableScanners = [];

    [ObservableProperty]
    private ScannerDevice? _selectedScanner;

    [ObservableProperty]
    private ScanSettings _settings = new();

    [ObservableProperty]
    private ScannedDocument _currentDocument = new();

    [ObservableProperty]
    private ScannedPage? _previewPage;

    [ObservableProperty]
    private bool _isScanning;

    [ObservableProperty]
    private double _scanProgress;

    [ObservableProperty]
    private string _statusMessage = "Ready";

    [ObservableProperty]
    private bool _hasNoScanners;

    [ObservableProperty]
    private ObservableCollection<ScanSourceOption> _availableSources = [];

    [ObservableProperty]
    private ScanSourceOption? _selectedSource;

    [ObservableProperty]
    private ObservableCollection<int> _availableDpiValues = [];

    [ObservableProperty]
    private int _selectedDpi = 300;

    public ScannerViewModel(IScannerService scannerService)
    {
        _scannerService = scannerService;
    }

    /// <summary>
    /// Initializes the ViewModel and loads available scanners.
    /// </summary>
    public async Task InitializeAsync()
    {
        await RefreshScannersAsync();
    }

    [RelayCommand]
    private async Task RefreshScannersAsync()
    {
        try
        {
            StatusMessage = "Searching for scanners...";
            var scanners = await _scannerService.GetAvailableScannersAsync();

            AvailableScanners.Clear();
            foreach (var scanner in scanners)
            {
                AvailableScanners.Add(scanner);
            }

            HasNoScanners = AvailableScanners.Count == 0;
            SelectedScanner ??= AvailableScanners.FirstOrDefault();

            StatusMessage = HasNoScanners
                ? "No scanners found. Connect a scanner and click Refresh."
                : $"Found {AvailableScanners.Count} scanner(s)";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
            HasNoScanners = true;
        }
    }

    [RelayCommand(CanExecute = nameof(CanScan))]
    private async Task ScanAsync()
    {
        if (SelectedScanner == null) return;

        try
        {
            IsScanning = true;
            _scanCts = new CancellationTokenSource();

            var progress = new Progress<ScanProgress>(p =>
            {
                ScanProgress = p.PercentComplete;
                StatusMessage = p.Status;
            });

            var page = await _scannerService.ScanSinglePageAsync(
                SelectedScanner,
                Settings,
                progress,
                _scanCts.Token);

            PreviewPage = page;
            CurrentDocument.AddPage(page);

            StatusMessage = $"Scanned page {CurrentDocument.PageCount}";
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Scan cancelled";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Scan error: {ex.Message}";
        }
        finally
        {
            IsScanning = false;
            _scanCts?.Dispose();
            _scanCts = null;
        }
    }

    private bool CanScan() => SelectedScanner != null && !IsScanning;

    [RelayCommand(CanExecute = nameof(CanScan))]
    private async Task ScanMultipleAsync()
    {
        if (SelectedScanner == null) return;

        try
        {
            IsScanning = true;
            _scanCts = new CancellationTokenSource();

            var progress = new Progress<ScanProgress>(p =>
            {
                ScanProgress = p.PercentComplete;
                StatusMessage = p.Status;
            });

            var pages = await _scannerService.ScanMultiplePagesAsync(
                SelectedScanner,
                Settings,
                progress,
                _scanCts.Token);

            foreach (var page in pages)
            {
                CurrentDocument.AddPage(page);
            }

            PreviewPage = pages.LastOrDefault();
            StatusMessage = $"Scanned {pages.Count} page(s) - Total: {CurrentDocument.PageCount}";
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Scan cancelled";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Scan error: {ex.Message}";
        }
        finally
        {
            IsScanning = false;
            _scanCts?.Dispose();
            _scanCts = null;
        }
    }

    [RelayCommand(CanExecute = nameof(IsScanning))]
    private void CancelScan()
    {
        _scanCts?.Cancel();
        StatusMessage = "Cancelling...";
    }

    [RelayCommand]
    private void ClearDocument()
    {
        CurrentDocument.Clear();
        PreviewPage = null;
        StatusMessage = "Document cleared";
    }

    [RelayCommand]
    private void NewDocument()
    {
        CurrentDocument = new ScannedDocument();
        PreviewPage = null;
        StatusMessage = "New document created";
    }

    partial void OnSelectedScannerChanged(ScannerDevice? value)
    {
        ScanCommand.NotifyCanExecuteChanged();
        ScanMultipleCommand.NotifyCanExecuteChanged();

        if (value != null)
        {
            // Update available sources for this scanner
            AvailableSources.Clear();
            foreach (var source in value.SupportedSources)
            {
                AvailableSources.Add(new ScanSourceOption(source));
            }

            // Select the default source
            SelectedSource = AvailableSources.FirstOrDefault(s => s.Source == value.DefaultSource)
                          ?? AvailableSources.FirstOrDefault();

            // Update available DPI values
            AvailableDpiValues.Clear();
            foreach (var dpi in value.SupportedDpiValues.OrderBy(d => d))
            {
                AvailableDpiValues.Add(dpi);
            }

            // Select default DPI (300 if available, otherwise closest)
            SelectedDpi = AvailableDpiValues.Contains(300)
                ? 300
                : AvailableDpiValues.OrderBy(d => Math.Abs(d - 300)).FirstOrDefault();

            // Update settings based on scanner capabilities
            Settings.DuplexScanning = value.SupportsDuplex;
        }
        else
        {
            AvailableSources.Clear();
            AvailableDpiValues.Clear();
        }
    }

    partial void OnSelectedSourceChanged(ScanSourceOption? value)
    {
        if (value != null)
        {
            Settings.ScanSource = value.Source;

            // Auto-enable duplex if duplex source is selected
            if (value.Source == ScanSource.Duplex)
            {
                Settings.DuplexScanning = true;
            }
        }
    }

    partial void OnSelectedDpiChanged(int value)
    {
        Settings.Dpi = value;
    }

    partial void OnIsScanningChanged(bool value)
    {
        ScanCommand.NotifyCanExecuteChanged();
        ScanMultipleCommand.NotifyCanExecuteChanged();
        CancelScanCommand.NotifyCanExecuteChanged();
    }
}

/// <summary>
/// Represents a scan source option for UI binding.
/// </summary>
public class ScanSourceOption
{
    public ScanSource Source { get; }
    public string DisplayName { get; }
    public string Description { get; }

    public ScanSourceOption(ScanSource source)
    {
        Source = source;
        DisplayName = source.GetDisplayName();
        Description = source.GetDescription();
    }

    public override string ToString() => DisplayName;
}
