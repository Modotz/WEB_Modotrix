using System.Net.Http.Json;
using System.Text.Json;
using ERP.Web.Models;

namespace ERP.Web.Services;

public class AuditLogApiClient
{
    private readonly HttpClient _http;
    private readonly ILogger<AuditLogApiClient> _logger;

    public AuditLogApiClient(HttpClient http, ILogger<AuditLogApiClient> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<PagedResult<AuditLogViewModel>> GetLogsAsync(AuditLogFilterViewModel filter)
    {
        return await _http.GetFromJsonAsync<PagedResult<AuditLogViewModel>>(
                   $"api/audit-logs?{filter.ToQueryString()}&page={filter.Page}&pageSize={filter.PageSize}")
               ?? new PagedResult<AuditLogViewModel>();
    }

    public async Task<List<AuditLogViewModel>> GetLogsForExportAsync(AuditLogFilterViewModel filter)
    {
        return await _http.GetFromJsonAsync<List<AuditLogViewModel>>($"api/audit-logs/export?{filter.ToQueryString()}")
               ?? new List<AuditLogViewModel>();
    }

    public async Task<AuditLogFilterOptionsViewModel> GetFilterOptionsAsync()
    {
        return await _http.GetFromJsonAsync<AuditLogFilterOptionsViewModel>("api/audit-logs/filters")
               ?? new AuditLogFilterOptionsViewModel();
    }

    public async Task<(bool Success, string Message)> ClearAsync(DateOnly? before)
    {
        var response = await _http.PostAsJsonAsync("api/audit-logs/clear", new { before });
        var message = await ReadMessageAsync(response);

        return response.IsSuccessStatusCode
            ? (true, message ?? "Log audit berhasil dibersihkan.")
            : (false, message ?? $"Gagal membersihkan log ({(int)response.StatusCode}).");
    }

    /// <summary>Catat aktivitas non-perubahan data (ekspor/cetak). Gagal mencatat tidak menggagalkan halaman.</summary>
    public async Task RecordAsync(string action, string module, string description, string? recordId = null)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("api/audit-logs/record", new { action, module, description, recordId });
            if (!response.IsSuccessStatusCode)
                _logger.LogWarning("Gagal mencatat aktivitas {Action} {Module}: {Status}", action, module, (int)response.StatusCode);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Gagal mencatat aktivitas {Action} {Module}", action, module);
        }
    }

    private static async Task<string?> ReadMessageAsync(HttpResponseMessage response)
    {
        try
        {
            var json = await response.Content.ReadFromJsonAsync<JsonElement>();
            return json.ValueKind == JsonValueKind.Object && json.TryGetProperty("message", out var m) ? m.GetString() : null;
        }
        catch (Exception)
        {
            return null;
        }
    }
}
