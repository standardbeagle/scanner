using Scanner.Core.Models;
using Scanner.Core.Services.Interfaces;

namespace Scanner.Core.Services;

/// <summary>
/// OneDrive cloud storage service using Microsoft Graph.
/// Note: Requires Azure AD app registration for full functionality.
/// </summary>
public class OneDriveService : ICloudStorageService
{
    private bool _isAuthenticated;

    public string ProviderName => "OneDrive";
    public CloudStorageProvider Provider => CloudStorageProvider.OneDrive;

    public Task<bool> IsAuthenticatedAsync(CancellationToken ct = default)
    {
        return Task.FromResult(_isAuthenticated);
    }

    public Task<bool> AuthenticateAsync(CancellationToken ct = default)
    {
        // TODO: Implement with Microsoft Graph SDK and Azure AD app registration
        // For production, you would:
        // 1. Register an app in Azure Portal
        // 2. Configure redirect URIs
        // 3. Use MSAL for authentication
        // 4. Use GraphServiceClient for file operations

        // Placeholder - always returns false until properly configured
        _isAuthenticated = false;
        return Task.FromResult(false);
    }

    public Task SignOutAsync(CancellationToken ct = default)
    {
        _isAuthenticated = false;
        return Task.CompletedTask;
    }

    public Task<CloudFolder> GetRootFolderAsync(CancellationToken ct = default)
    {
        EnsureAuthenticated();
        return Task.FromResult(new CloudFolder("root", "OneDrive", "/"));
    }

    public Task<IReadOnlyList<CloudFolder>> GetFoldersAsync(string folderId, CancellationToken ct = default)
    {
        EnsureAuthenticated();
        return Task.FromResult<IReadOnlyList<CloudFolder>>([]);
    }

    public Task<CloudFile> UploadFileAsync(
        string folderId,
        string fileName,
        Stream content,
        IProgress<double>? progress = null,
        CancellationToken ct = default)
    {
        EnsureAuthenticated();
        throw new NotImplementedException("OneDrive integration requires Azure AD app registration.");
    }

    public Task<string> GetUploadUrlAsync(string folderId, string fileName, CancellationToken ct = default)
    {
        EnsureAuthenticated();
        throw new NotImplementedException("OneDrive integration requires Azure AD app registration.");
    }

    private void EnsureAuthenticated()
    {
        if (!_isAuthenticated)
            throw new InvalidOperationException("Not authenticated. Configure Azure AD app registration first.");
    }
}
