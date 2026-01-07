using Scanner.Core.Models;

namespace Scanner.Core.Services.Interfaces;

/// <summary>
/// Service for cloud storage integration.
/// </summary>
public interface ICloudStorageService
{
    /// <summary>
    /// Gets the cloud provider name (e.g., "OneDrive", "Google Drive").
    /// </summary>
    string ProviderName { get; }

    /// <summary>
    /// Gets the cloud provider type.
    /// </summary>
    CloudStorageProvider Provider { get; }

    /// <summary>
    /// Checks if the user is authenticated.
    /// </summary>
    Task<bool> IsAuthenticatedAsync(CancellationToken ct = default);

    /// <summary>
    /// Authenticates the user (shows OAuth dialog if needed).
    /// </summary>
    Task<bool> AuthenticateAsync(CancellationToken ct = default);

    /// <summary>
    /// Signs out the current user.
    /// </summary>
    Task SignOutAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets the root folder.
    /// </summary>
    Task<CloudFolder> GetRootFolderAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets the subfolders of a folder.
    /// </summary>
    Task<IReadOnlyList<CloudFolder>> GetFoldersAsync(string folderId, CancellationToken ct = default);

    /// <summary>
    /// Uploads a file to the specified folder.
    /// </summary>
    Task<CloudFile> UploadFileAsync(
        string folderId,
        string fileName,
        Stream content,
        IProgress<double>? progress = null,
        CancellationToken ct = default);

    /// <summary>
    /// Gets a direct upload URL for the file.
    /// </summary>
    Task<string> GetUploadUrlAsync(string folderId, string fileName, CancellationToken ct = default);
}
