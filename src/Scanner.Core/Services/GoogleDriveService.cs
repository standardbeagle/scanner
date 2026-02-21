using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Drive.v3;
using Google.Apis.Drive.v3.Data;
using DriveFile = Google.Apis.Drive.v3.Data.File;
using Google.Apis.Services;
using Google.Apis.Upload;
using Google.Apis.Util.Store;
using Scanner.Core.Models;
using Scanner.Core.Services.Interfaces;

namespace Scanner.Core.Services;

/// <summary>
/// Google Drive cloud storage service using Google APIs + OAuth2.
/// </summary>
public class GoogleDriveService : ICloudStorageService
{
    // Register at console.cloud.google.com → APIs & Services → Credentials → Desktop app
    private const string GoogleClientId = "REPLACE_WITH_GOOGLE_CLIENT_ID";
    private const string GoogleClientSecret = "REPLACE_WITH_GOOGLE_CLIENT_SECRET";

    private static readonly string[] Scopes = [DriveService.Scope.DriveFile];
    private static readonly string TokenStorePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ScannerApp", "google-tokens");

    private static readonly ClientSecrets Secrets = new()
    {
        ClientId = GoogleClientId,
        ClientSecret = GoogleClientSecret
    };

    private DriveService? _driveService;
    private UserCredential? _credential;

    public string ProviderName => "Google Drive";
    public CloudStorageProvider Provider => CloudStorageProvider.GoogleDrive;

    public async Task<bool> IsAuthenticatedAsync(CancellationToken ct = default)
    {
        // If we have an active, non-stale credential, report authenticated
        if (_credential != null)
        {
            if (!_credential.Token.IsStale) return true;
            return await _credential.RefreshTokenAsync(ct);
        }

        // Check whether a stored refresh token exists on disk (no browser prompt)
        var store = new FileDataStore(TokenStorePath, true);
        var stored = await store.GetAsync<TokenResponse>("user");
        if (stored?.RefreshToken == null) return false;

        // Restore session silently using stored token (GoogleWebAuthorizationBroker
        // returns immediately when a valid token exists in the DataStore)
        try
        {
            _credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                Secrets, Scopes, "user", ct, store);
            _driveService = BuildDriveService();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> AuthenticateAsync(CancellationToken ct = default)
    {
        // GoogleWebAuthorizationBroker: silent if token cached, browser otherwise
        var store = new FileDataStore(TokenStorePath, true);
        _credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
            Secrets, Scopes, "user", ct, store);

        _driveService = BuildDriveService();
        return true;
    }

    public async Task SignOutAsync(CancellationToken ct = default)
    {
        if (_credential != null)
        {
            await _credential.RevokeTokenAsync(ct);
            _credential = null;
        }

        _driveService = null;

        var store = new FileDataStore(TokenStorePath, true);
        await store.ClearAsync();
    }

    public async Task<CloudFolder> GetRootFolderAsync(CancellationToken ct = default)
    {
        EnsureAuthenticated();
        var req = _driveService!.Files.Get("root");
        req.Fields = "id,name";
        var root = await req.ExecuteAsync(ct);
        return new CloudFolder(root.Id, "My Drive", "/");
    }

    public async Task<IReadOnlyList<CloudFolder>> GetFoldersAsync(string folderId, CancellationToken ct = default)
    {
        EnsureAuthenticated();
        var req = _driveService!.Files.List();
        req.Q = $"'{folderId}' in parents and mimeType = 'application/vnd.google-apps.folder' and trashed = false";
        req.Fields = "files(id,name)";
        var result = await req.ExecuteAsync(ct);
        return result.Files.Select(f => new CloudFolder(f.Id, f.Name, f.Name)).ToList();
    }

    public async Task<CloudFile> UploadFileAsync(
        string folderId,
        string fileName,
        Stream content,
        IProgress<double>? progress = null,
        CancellationToken ct = default)
    {
        EnsureAuthenticated();

        var metadata = new DriveFile
        {
            Name = fileName,
            Parents = [folderId]
        };

        var req = _driveService!.Files.Create(metadata, content, GetMimeType(fileName));
        req.Fields = "id,name,webViewLink";

        if (progress != null && content.CanSeek)
        {
            var totalSize = content.Length;
            req.ProgressChanged += p =>
            {
                if (totalSize > 0) progress.Report((double)p.BytesSent / totalSize);
            };
        }

        var result = await req.UploadAsync(ct);

        if (result.Status == UploadStatus.Failed)
            throw new IOException($"Google Drive upload failed: {result.Exception?.Message}");

        var file = req.ResponseBody;
        return new CloudFile(file.Id, file.Name, file.WebViewLink ?? string.Empty);
    }

    public Task<string> GetUploadUrlAsync(string folderId, string fileName, CancellationToken ct = default)
        => Task.FromResult(string.Empty);

    private DriveService BuildDriveService() =>
        new(new BaseClientService.Initializer
        {
            HttpClientInitializer = _credential,
            ApplicationName = "Scanner"
        });

    private void EnsureAuthenticated()
    {
        if (_driveService == null)
            throw new InvalidOperationException("Not authenticated with Google Drive. Sign in first.");
    }

    private static string GetMimeType(string fileName) =>
        Path.GetExtension(fileName).ToLowerInvariant() switch
        {
            ".pdf" => "application/pdf",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".tiff" or ".tif" => "image/tiff",
            _ => "application/octet-stream"
        };
}
