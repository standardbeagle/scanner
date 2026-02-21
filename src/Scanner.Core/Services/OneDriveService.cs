using Microsoft.Identity.Client;
using Scanner.Core.Models;
using Scanner.Core.Services.Interfaces;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Scanner.Core.Services;

/// <summary>
/// OneDrive cloud storage service using MSAL + Microsoft Graph REST API.
/// </summary>
public class OneDriveService : ICloudStorageService
{
    private static readonly string[] Scopes = ["Files.ReadWrite", "offline_access"];
    private static readonly string GraphBase = "https://graph.microsoft.com/v1.0";
    private static readonly string TokenCachePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ScannerApp", "onedrive-token-cache.bin");

    private readonly SettingsViewModel _settings;
    private readonly HttpClient _http = new();
    private IPublicClientApplication? _msalApp;
    private string? _accessToken;
    private DateTimeOffset _tokenExpiry;

    public string ProviderName => "OneDrive";
    public CloudStorageProvider Provider => CloudStorageProvider.OneDrive;

    public OneDriveService(SettingsViewModel settings)
    {
        _settings = settings;
    }

    private IPublicClientApplication GetMsalApp()
    {
        if (string.IsNullOrWhiteSpace(_settings.OneDriveClientId))
            throw new InvalidOperationException(
                "OneDrive Client ID is not configured. Enter it in Settings.");

        if (_msalApp == null || _msalApp.AppConfig.ClientId != _settings.OneDriveClientId)
        {
            _msalApp = PublicClientApplicationBuilder
                .Create(_settings.OneDriveClientId)
                .WithAuthority(AzureCloudInstance.AzurePublic, AadAuthorityAudience.PersonalMicrosoftAccount)
                .WithDefaultRedirectUri()
                .Build();

            _msalApp.UserTokenCache.SetBeforeAccess(args =>
            {
                if (File.Exists(TokenCachePath))
                    args.TokenCache.DeserializeMsalV3(File.ReadAllBytes(TokenCachePath));
            });
            _msalApp.UserTokenCache.SetAfterAccess(args =>
            {
                if (args.HasStateChanged)
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(TokenCachePath)!);
                    File.WriteAllBytes(TokenCachePath, args.TokenCache.SerializeMsalV3());
                }
            });
        }

        return _msalApp;
    }

    public async Task<bool> IsAuthenticatedAsync(CancellationToken ct = default)
    {
        if (_tokenExpiry > DateTimeOffset.UtcNow.AddMinutes(5))
            return true;

        if (string.IsNullOrWhiteSpace(_settings.OneDriveClientId))
            return false;

        try
        {
            var app = GetMsalApp();
            var accounts = await app.GetAccountsAsync();
            if (!accounts.Any()) return false;

            var result = await app.AcquireTokenSilent(Scopes, accounts.First())
                .ExecuteAsync(ct);
            SetToken(result);
            return true;
        }
        catch (MsalUiRequiredException)
        {
            return false;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> AuthenticateAsync(CancellationToken ct = default)
    {
        var app = GetMsalApp();
        try
        {
            AuthenticationResult result;
            try
            {
                var accounts = await app.GetAccountsAsync();
                result = await app.AcquireTokenSilent(Scopes, accounts.FirstOrDefault())
                    .ExecuteAsync(ct);
            }
            catch (MsalUiRequiredException)
            {
                result = await app.AcquireTokenInteractive(Scopes).ExecuteAsync(ct);
            }

            SetToken(result);
            return true;
        }
        catch (MsalClientException ex) when (ex.ErrorCode == "authentication_canceled")
        {
            return false;
        }
    }

    public async Task SignOutAsync(CancellationToken ct = default)
    {
        if (_msalApp != null)
        {
            foreach (var account in await _msalApp.GetAccountsAsync())
                await _msalApp.RemoveAsync(account);
        }

        _accessToken = null;
        _tokenExpiry = default;

        if (File.Exists(TokenCachePath))
            File.Delete(TokenCachePath);
    }

    public async Task<CloudFolder> GetRootFolderAsync(CancellationToken ct = default)
    {
        var json = await GetAsync("me/drive/root?$select=id,name", ct);
        var id = json.GetProperty("id").GetString()!;
        return new CloudFolder(id, "OneDrive", "/");
    }

    public async Task<IReadOnlyList<CloudFolder>> GetFoldersAsync(string folderId, CancellationToken ct = default)
    {
        var json = await GetAsync(
            $"me/drive/items/{folderId}/children?$filter=folder ne null&$select=id,name", ct);
        var folders = new List<CloudFolder>();
        foreach (var item in json.GetProperty("value").EnumerateArray())
        {
            var id = item.GetProperty("id").GetString()!;
            var name = item.GetProperty("name").GetString()!;
            folders.Add(new CloudFolder(id, name, name));
        }
        return folders;
    }

    public async Task<CloudFile> UploadFileAsync(
        string folderId,
        string fileName,
        Stream content,
        IProgress<double>? progress = null,
        CancellationToken ct = default)
    {
        await EnsureTokenFreshAsync(ct);

        var totalSize = content.CanSeek ? content.Length : throw new InvalidOperationException("Stream must be seekable.");

        // Create upload session
        var sessionUrl = $"{GraphBase}/me/drive/items/{folderId}:/{Uri.EscapeDataString(fileName)}:/createUploadSession";
        using var sessionReq = AuthorizedRequest(HttpMethod.Post, sessionUrl);
        sessionReq.Content = new StringContent(
            "{\"item\":{\"@microsoft.graph.conflictBehavior\":\"rename\"}}",
            Encoding.UTF8, "application/json");

        using var sessionResp = await _http.SendAsync(sessionReq, ct);
        sessionResp.EnsureSuccessStatusCode();
        var sessionJson = JsonDocument.Parse(await sessionResp.Content.ReadAsStringAsync(ct));
        var uploadUrl = sessionJson.RootElement.GetProperty("uploadUrl").GetString()!;

        // Upload in 5 MB chunks (must be a multiple of 320 KB)
        const int chunkSize = 5 * 1024 * 1024;
        var buffer = new byte[chunkSize];
        long offset = 0;
        JsonElement fileItem = default;

        while (offset < totalSize)
        {
            var bytesRead = await content.ReadAsync(buffer.AsMemory(0, chunkSize), ct);
            if (bytesRead == 0) break;

            using var chunkReq = new HttpRequestMessage(HttpMethod.Put, uploadUrl);
            chunkReq.Content = new ByteArrayContent(buffer, 0, bytesRead);
            chunkReq.Content.Headers.ContentRange =
                new ContentRangeHeaderValue(offset, offset + bytesRead - 1, totalSize);
            chunkReq.Content.Headers.ContentLength = bytesRead;

            using var chunkResp = await _http.SendAsync(chunkReq, ct);
            if (chunkResp.StatusCode != HttpStatusCode.Accepted)
                chunkResp.EnsureSuccessStatusCode();

            offset += bytesRead;
            progress?.Report((double)offset / totalSize);

            if (chunkResp.StatusCode is HttpStatusCode.OK or HttpStatusCode.Created)
                fileItem = JsonDocument.Parse(await chunkResp.Content.ReadAsStringAsync(ct)).RootElement.Clone();
        }

        var id = fileItem.TryGetProperty("id", out var idProp) ? idProp.GetString() ?? "" : "";
        var name = fileItem.TryGetProperty("name", out var nameProp) ? nameProp.GetString() ?? fileName : fileName;
        var webUrl = fileItem.TryGetProperty("webUrl", out var urlProp) ? urlProp.GetString() ?? "" : "";
        return new CloudFile(id, name, webUrl);
    }

    public Task<string> GetUploadUrlAsync(string folderId, string fileName, CancellationToken ct = default)
        => Task.FromResult(string.Empty);

    private async Task<JsonElement> GetAsync(string path, CancellationToken ct)
    {
        await EnsureTokenFreshAsync(ct);
        using var req = AuthorizedRequest(HttpMethod.Get, $"{GraphBase}/{path}");
        using var resp = await _http.SendAsync(req, ct);
        resp.EnsureSuccessStatusCode();
        var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync(ct));
        return doc.RootElement.Clone();
    }

    private HttpRequestMessage AuthorizedRequest(HttpMethod method, string url)
    {
        var req = new HttpRequestMessage(method, url);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
        return req;
    }

    private async Task EnsureTokenFreshAsync(CancellationToken ct)
    {
        if (_tokenExpiry <= DateTimeOffset.UtcNow.AddMinutes(5))
            await IsAuthenticatedAsync(ct);
        if (string.IsNullOrEmpty(_accessToken))
            throw new InvalidOperationException("Not authenticated with OneDrive. Sign in first.");
    }

    private void SetToken(AuthenticationResult result)
    {
        _accessToken = result.AccessToken;
        _tokenExpiry = result.ExpiresOn;
    }
}
