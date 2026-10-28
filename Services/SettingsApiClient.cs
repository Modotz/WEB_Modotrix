using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ERP.Web.Models;

namespace ERP.Web.Services;

/// <summary>App Settings, Reset Data dan Peta Data.</summary>
public class SettingsApiClient
{
    private readonly HttpClient _http;

    public SettingsApiClient(HttpClient http)
    {
        _http = http;
        // Backup database bisa memakan waktu lebih lama dari default 100 detik.
        _http.Timeout = TimeSpan.FromMinutes(30);
    }

    public async Task<SettingsPageViewModel> GetSettingsAsync() =>
        await _http.GetFromJsonAsync<SettingsPageViewModel>("api/settings") ?? new SettingsPageViewModel();

    public async Task<BrandingViewModel> GetBrandingAsync() =>
        await _http.GetFromJsonAsync<BrandingViewModel>("api/settings/branding") ?? new BrandingViewModel();

    public Task<(bool Success, string Message)> SaveAsync(Dictionary<string, string?> values) =>
        PostAsync("api/settings/save", new { values });

    public async Task<(bool Success, string Message)> UploadLogoAsync(IFormFile logo)
    {
        using var content = new MultipartFormDataContent();
        using var stream = logo.OpenReadStream();
        var file = new StreamContent(stream);
        file.Headers.ContentType = new MediaTypeHeaderValue(string.IsNullOrEmpty(logo.ContentType) ? "application/octet-stream" : logo.ContentType);
        content.Add(file, "logo", logo.FileName);

        var response = await _http.PostAsync("api/settings/logo", content);
        return await ResultAsync(response, "Logo berhasil diunggah.");
    }

    public Task<(bool Success, string Message)> PruneLogsAsync(int months) =>
        PostAsync("api/settings/prune-logs", new { months });

    public Task<(bool Success, string Message)> PruneSerialMovementsAsync(int months) =>
        PostAsync("api/settings/prune-serial-movements", new { months });

    public Task<(bool Success, string Message)> BackupAsync(string folder) =>
        PostAsync("api/settings/backup", new { folder });

    /// <summary>Backup lalu unduh. Mengembalikan response mentah agar file bisa di-stream ke browser.</summary>
    public async Task<HttpResponseMessage> BackupDownloadAsync() =>
        await _http.GetAsync("api/settings/backup-download", HttpCompletionOption.ResponseHeadersRead);

    public async Task<List<WipeGroupViewModel>> GetWipeGroupsAsync() =>
        await _http.GetFromJsonAsync<List<WipeGroupViewModel>>("api/settings/wipe") ?? new();

    public Task<(bool Success, string Message)> WipeAsync(string confirm) =>
        PostAsync("api/settings/wipe", new { confirm });

    public async Task<SchemaMapViewModel> GetSchemaMapAsync() =>
        await _http.GetFromJsonAsync<SchemaMapViewModel>("api/schema-map") ?? new SchemaMapViewModel();

    public async Task<SchemaTableDetailViewModel?> GetSchemaTableAsync(string name)
    {
        var response = await _http.GetAsync($"api/schema-map/tables/{Uri.EscapeDataString(name)}");
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<SchemaTableDetailViewModel>();
    }

    private async Task<(bool Success, string Message)> PostAsync(string url, object body) =>
        await ResultAsync(await _http.PostAsJsonAsync(url, body), "Berhasil.");

    public static async Task<(bool Success, string Message)> ResultAsync(HttpResponseMessage response, string fallback)
    {
        string? message = null;
        try
        {
            var json = await response.Content.ReadFromJsonAsync<JsonElement>();
            if (json.ValueKind == JsonValueKind.Object && json.TryGetProperty("message", out var m))
                message = m.GetString();
        }
        catch (Exception)
        {
            // body bukan JSON
        }

        return response.IsSuccessStatusCode
            ? (true, message ?? fallback)
            : (false, message ?? $"Permintaan gagal ({(int)response.StatusCode}).");
    }
}
