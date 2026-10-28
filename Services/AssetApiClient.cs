using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ERP.Web.Models;

namespace ERP.Web.Services;

/// <summary>Modul Manajemen Aset: kategori, daftar aset, serah terima, maintenance, penyusutan, laporan nilai buku.</summary>
public class AssetApiClient
{
    private readonly HttpClient _http;

    public AssetApiClient(HttpClient http)
    {
        _http = http;
    }

    // ---------- Kategori ----------

    public async Task<PagedResult<AssetCategoryViewModel>> GetCategoriesAsync(string? q, int page, int pageSize = 25) =>
        await GetAsync<PagedResult<AssetCategoryViewModel>>($"api/asset-categories?q={Esc(q)}&page={page}&pageSize={pageSize}") ?? new();

    public async Task<AssetCategoryFormOptionsViewModel> GetCategoryFormOptionsAsync() =>
        await GetAsync<AssetCategoryFormOptionsViewModel>("api/asset-categories/form-options") ?? new();

    public Task<ApiResult> SaveCategoryAsync(AssetCategoryViewModel m) =>
        PostAsync(m.Uid == Guid.Empty ? "api/asset-categories/create" : "api/asset-categories/update", new
        {
            uid = m.Uid == Guid.Empty ? (Guid?)null : m.Uid,
            m.Code,
            m.Name,
            m.UsefulLifeMonths,
            m.DepreciationMethod,
            m.DecliningFactor,
            m.SalvagePercent,
            m.AssetAccountId,
            m.AccumAccountId,
            m.ExpenseAccountId,
            m.IsActive
        });

    public Task<ApiResult> DeleteCategoryAsync(Guid uid) => PostAsync("api/asset-categories/delete", new { uid });

    // ---------- Aset ----------

    public async Task<PagedResult<AssetListItemViewModel>> GetAssetsAsync(AssetFilterViewModel f) =>
        await GetAsync<PagedResult<AssetListItemViewModel>>($"api/assets?{f.ToQueryString()}&page={f.Page}&pageSize={f.PageSize}") ?? new();

    public Task<AssetViewModel?> GetAssetAsync(Guid uid) => GetAsync<AssetViewModel>($"api/assets/{uid}");

    public async Task<AssetFormOptionsViewModel> GetAssetFormOptionsAsync() =>
        await GetAsync<AssetFormOptionsViewModel>("api/assets/form-options") ?? new();

    public async Task<List<AssetSerialOptionViewModel>> GetSerialsAsync(Guid productUid, Guid warehouseUid, string? q) =>
        await GetAsync<List<AssetSerialOptionViewModel>>($"api/assets/serials?productUid={productUid}&warehouseUid={warehouseUid}&q={Esc(q)}") ?? new();

    public Task<ApiResult> SaveAssetAsync(AssetFormViewModel m, bool activate) =>
        PostAsync(m.Uid == null ? "api/assets/create" : "api/assets/update", new
        {
            m.Uid,
            m.Name,
            m.AssetCategoryId,
            m.AcquisitionSource,
            m.AcquisitionDate,
            m.AcquisitionCost,
            m.SupplierId,
            m.CashAccountId,
            m.ProductId,
            m.SourceWarehouseId,
            m.SerialNo,
            m.CounterAccountId,
            m.OpeningDate,
            m.OpeningAccumulated,
            m.DepreciationMethod,
            m.DecliningFactor,
            m.UsefulLifeMonths,
            m.SalvageValue,
            m.DepreciationStart,
            m.TotalEstimatedUnits,
            m.Location,
            m.WarrantyUntil,
            m.Note,
            activate
        });

    // ---------- T3 lanjutan (Level 13): unit produksi & penyesuaian nilai ----------

    public Task<ApiResult> RecordUsageAsync(Guid uid, string? period, decimal units, string? note) =>
        PostAsync($"api/assets/{uid}/usage", new { period, units, note });

    /// <param name="kind">capex | revaluation | impairment | impairment-reversal (segmen URL API).</param>
    public Task<ApiResult> AdjustAssetAsync(Guid uid, string kind, DateOnly? date, decimal amount, string? note, string? direction,
        long? cashAccountId, Guid? reversesUid) =>
        PostAsync($"api/assets/{uid}/{kind}", new { date, amount, note, direction, cashAccountId, reversesUid });

    public Task<ApiResult> VoidAdjustmentAsync(Guid uid, Guid adjustmentUid, string? reason) =>
        PostAsync($"api/assets/{uid}/adjustments/void", new { uid = adjustmentUid, reason });

    public Task<ApiResult> ActivateAssetAsync(Guid uid) => PostAsync($"api/assets/{uid}/activate", new { });

    public Task<ApiResult> ChangeScheduleAsync(Guid uid, int life, decimal salvage, string? method, decimal? factor, string? reason, decimal? units) =>
        PostAsync($"api/assets/{uid}/change-schedule", new
        {
            usefulLifeMonths = life,
            salvageValue = salvage,
            depreciationMethod = method,
            decliningFactor = factor,
            totalEstimatedUnits = units,
            reason
        });

    public Task<ApiResult> DisposeAssetAsync(Guid uid, DateOnly? date, string? type, decimal proceeds, long? cashAccountId, string? note) =>
        PostAsync($"api/assets/{uid}/dispose", new { date, type, proceeds, cashAccountId, note });

    public Task<ApiResult> CancelDisposalAsync(Guid uid, DateOnly? date, string? reason) =>
        PostAsync($"api/assets/{uid}/cancel-disposal", new { uid, date, reason });

    public Task<ApiResult> DeleteAssetAsync(Guid uid, DateOnly? date, string? reason) =>
        PostAsync("api/assets/delete", new { uid, date, reason });

    // ---------- Serah terima ----------

    public async Task<PagedResult<AssetAssignmentViewModel>> GetAssignmentsAsync(string? q, string? status, int page, int pageSize = 25) =>
        await GetAsync<PagedResult<AssetAssignmentViewModel>>($"api/asset-assignments?q={Esc(q)}&status={Esc(status)}&page={page}&pageSize={pageSize}") ?? new();

    public async Task<List<AssetOptionViewModel>> GetAssignableAssetsAsync() =>
        await GetAsync<List<AssetOptionViewModel>>("api/asset-assignments/assets") ?? new();

    public Task<ApiResult> CreateAssignmentAsync(Guid assetUid, string? holderName, string? holderDepartment, string? location, DateOnly? assignedDate, string? note) =>
        PostAsync("api/asset-assignments/create", new { assetUid, holderName, holderDepartment, location, assignedDate, note });

    public Task<ApiResult> ReturnAssignmentAsync(Guid uid, DateOnly? returnedDate, string? returnNote) =>
        PostAsync($"api/asset-assignments/{uid}/return", new { returnedDate, returnNote });

    public Task<ApiResult> DeleteAssignmentAsync(Guid uid) => PostAsync("api/asset-assignments/delete", new { uid });

    // ---------- Maintenance ----------

    public async Task<PagedResult<AssetMaintenanceViewModel>> GetMaintenancesAsync(string? q, string? type, int page, int pageSize = 25) =>
        await GetAsync<PagedResult<AssetMaintenanceViewModel>>($"api/asset-maintenance?q={Esc(q)}&type={Esc(type)}&page={page}&pageSize={pageSize}") ?? new();

    public async Task<MaintenanceRemindersViewModel> GetMaintenanceRemindersAsync() =>
        await GetAsync<MaintenanceRemindersViewModel>("api/asset-maintenance/reminders") ?? new();

    public async Task<AssetFormOptionsViewModel> GetMaintenanceFormOptionsAsync() =>
        await GetAsync<AssetFormOptionsViewModel>("api/asset-maintenance/form-options") ?? new();

    public Task<ApiResult> CreateMaintenanceAsync(object body) => PostAsync("api/asset-maintenance/create", body);

    public Task<ApiResult> DeleteMaintenanceAsync(Guid uid, DateOnly? date, string? reason) =>
        PostAsync("api/asset-maintenance/delete", new { uid, date, reason });

    // ---------- Penyusutan ----------

    public async Task<DepreciationPageViewModel> GetDepreciationPageAsync(string? period) =>
        await GetAsync<DepreciationPageViewModel>($"api/asset-depreciation?period={Esc(period)}") ?? new();

    public Task<ApiResult> RunDepreciationAsync(string? period, string? note) =>
        PostAsync("api/asset-depreciation/run", new { period, note });

    public Task<ApiResult> ReverseLastRunAsync(string? reason) =>
        PostAsync("api/asset-depreciation/reverse-last", new { reason });

    // ---------- Laporan nilai buku ----------

    public async Task<BookValueReportViewModel> GetBookValueAsync(DateOnly asOf, Guid? categoryUid) =>
        await GetAsync<BookValueReportViewModel>($"api/asset-book-value?asOf={asOf:yyyy-MM-dd}&categoryUid={categoryUid}") ?? new();

    public async Task<List<AssetCategoryOptionViewModel>> GetBookValueCategoriesAsync() =>
        await GetAsync<List<AssetCategoryOptionViewModel>>("api/asset-book-value/categories") ?? new();

    // ---------- helper ----------

    private async Task<T?> GetAsync<T>(string url)
    {
        var response = await _http.GetAsync(url);

        if (response.StatusCode == HttpStatusCode.NotFound)
            return default;

        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            response.EnsureSuccessStatusCode(); // ditangani ApiUnauthorizedExceptionFilter

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException(await ReadMessageAsync(response) ?? $"API mengembalikan status {(int)response.StatusCode}.");

        return await response.Content.ReadFromJsonAsync<T>();
    }

    private async Task<ApiResult> PostAsync(string url, object body)
    {
        var response = await _http.PostAsJsonAsync(url, body);

        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            response.EnsureSuccessStatusCode();

        var json = await ReadJsonAsync(response);
        var message = json is { ValueKind: JsonValueKind.Object } && json.Value.TryGetProperty("message", out var m) ? m.GetString() : null;
        Guid? uid = json is { ValueKind: JsonValueKind.Object } && json.Value.TryGetProperty("uid", out var u)
                    && u.ValueKind == JsonValueKind.String && Guid.TryParse(u.GetString(), out var g)
            ? g
            : null;

        return new ApiResult(
            response.IsSuccessStatusCode,
            message ?? (response.IsSuccessStatusCode ? "Berhasil." : $"Gagal ({(int)response.StatusCode})."),
            uid);
    }

    private static async Task<JsonElement?> ReadJsonAsync(HttpResponseMessage response)
    {
        try
        {
            return await response.Content.ReadFromJsonAsync<JsonElement>();
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static async Task<string?> ReadMessageAsync(HttpResponseMessage response)
    {
        var json = await ReadJsonAsync(response);
        return json is { ValueKind: JsonValueKind.Object } && json.Value.TryGetProperty("message", out var m) ? m.GetString() : null;
    }

    private static string Esc(string? value) => string.IsNullOrWhiteSpace(value) ? "" : Uri.EscapeDataString(value);
}
