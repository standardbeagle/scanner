using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Scanner.Core.Models;
using Scanner.Core.Services.Interfaces;

namespace Scanner.Core.Services;

/// <summary>
/// ViewModel for export and save operations.
/// </summary>
public partial class ExportViewModel : ObservableObject
{
    private readonly IExportService? _exportService;
    private readonly IEnumerable<ICloudStorageService>? _cloudServices;

    [ObservableProperty]
    private ScannedDocument? _document;

    [ObservableProperty]
    private ExportFormat _selectedFormat = ExportFormat.Pdf;

    [ObservableProperty]
    private PdfExportOptions _pdfOptions = new();

    [ObservableProperty]
    private CloudStorageProvider? _selectedCloudProvider;

    [ObservableProperty]
    private CloudFolder? _selectedFolder;

    [ObservableProperty]
    private string _fileName = "Scanned Document";

    [ObservableProperty]
    private bool _isExporting;

    [ObservableProperty]
    private double _exportProgress;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private bool _includeOcrText = true;

    public ExportViewModel()
    {
        // Default constructor for design-time
    }

    public ExportViewModel(IExportService exportService, IEnumerable<ICloudStorageService> cloudServices)
    {
        _exportService = exportService;
        _cloudServices = cloudServices;
    }

    /// <summary>
    /// Sets the document to export.
    /// </summary>
    public void SetDocument(ScannedDocument document)
    {
        Document = document;
        FileName = document.Name;
    }

    [RelayCommand(CanExecute = nameof(CanExport))]
    private async Task ExportToLocalAsync()
    {
        if (Document == null || _exportService == null) return;

        try
        {
            IsExporting = true;
            StatusMessage = "Exporting...";

            var progress = new Progress<double>(p => ExportProgress = p * 100);

            byte[] data;
            string extension;

            switch (SelectedFormat)
            {
                case ExportFormat.Pdf:
                    var pdfOptions = PdfOptions with { IncludeOcrTextLayer = IncludeOcrText };
                    data = await _exportService.ExportToPdfAsync(Document, pdfOptions, progress);
                    extension = ".pdf";
                    break;

                case ExportFormat.Jpeg:
                    if (Document.Pages.Count == 0) return;
                    data = await _exportService.ExportToJpegAsync(Document.Pages[0]);
                    extension = ".jpg";
                    break;

                case ExportFormat.Png:
                    if (Document.Pages.Count == 0) return;
                    data = await _exportService.ExportToPngAsync(Document.Pages[0]);
                    extension = ".png";
                    break;

                case ExportFormat.Tiff:
                    data = await _exportService.ExportToTiffAsync(Document, TiffCompression.Lzw);
                    extension = ".tiff";
                    break;

                default:
                    return;
            }

            var result = await _exportService.SaveWithDialogAsync(data, $"{FileName}{extension}", SelectedFormat);

            StatusMessage = result != null
                ? $"Saved to {result}"
                : "Export cancelled";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Export error: {ex.Message}";
        }
        finally
        {
            IsExporting = false;
        }
    }

    private bool CanExport() => Document != null && Document.PageCount > 0 && !IsExporting;

    [RelayCommand(CanExecute = nameof(CanExport))]
    private async Task ExportToCloudAsync()
    {
        if (Document == null || _cloudServices == null || SelectedCloudProvider == null) return;

        var cloudService = _cloudServices.FirstOrDefault(s => s.Provider == SelectedCloudProvider);
        if (cloudService == null) return;

        try
        {
            IsExporting = true;
            StatusMessage = "Authenticating...";

            if (!await cloudService.IsAuthenticatedAsync())
            {
                var authenticated = await cloudService.AuthenticateAsync();
                if (!authenticated)
                {
                    StatusMessage = "Authentication failed";
                    return;
                }
            }

            StatusMessage = "Exporting to cloud...";
            var progress = new Progress<double>(p => ExportProgress = p * 100);

            // Export to PDF
            var pdfOptions = PdfOptions with { IncludeOcrTextLayer = IncludeOcrText };
            var data = await _exportService!.ExportToPdfAsync(Document, pdfOptions, progress);

            // Upload to cloud
            using var stream = new MemoryStream(data);
            var folderId = SelectedFolder?.Id ?? "root";
            var uploadProgress = new Progress<double>(p => ExportProgress = 50 + p * 50);

            var file = await cloudService.UploadFileAsync(folderId, $"{FileName}.pdf", stream, uploadProgress);

            StatusMessage = $"Uploaded to {cloudService.ProviderName}: {file.Name}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Cloud upload error: {ex.Message}";
        }
        finally
        {
            IsExporting = false;
        }
    }

    [RelayCommand]
    private async Task AuthenticateCloudAsync(CloudStorageProvider provider)
    {
        if (_cloudServices == null) return;

        var cloudService = _cloudServices.FirstOrDefault(s => s.Provider == provider);
        if (cloudService == null) return;

        try
        {
            StatusMessage = $"Signing in to {cloudService.ProviderName}...";
            var success = await cloudService.AuthenticateAsync();
            StatusMessage = success
                ? $"Signed in to {cloudService.ProviderName}"
                : "Sign in failed";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Sign in error: {ex.Message}";
        }
    }

    partial void OnDocumentChanged(ScannedDocument? value)
    {
        ExportToLocalCommand.NotifyCanExecuteChanged();
        ExportToCloudCommand.NotifyCanExecuteChanged();
    }

    partial void OnIsExportingChanged(bool value)
    {
        ExportToLocalCommand.NotifyCanExecuteChanged();
        ExportToCloudCommand.NotifyCanExecuteChanged();
    }
}
