using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ERP.Web.Models;

namespace ERP.Web.Services;

/// <summary>Pembelian aset: PR Aset, RFQ Aset, PO Aset (+ penerimaan), Retur Aset.</summary>
public class AssetPurchaseApiClient
{
    private const string Pr = "api/asset-purchase-requests";
    private const string Rfq = "api/asset-rfqs";
    private const string Po = "api/asset-purchase-orders";
    private const string Ret = "api/asset-purchase-returns";

    private readonly HttpClient _http;

    public AssetPurchaseApiClient(HttpClient http)
    {
        _http = http;
    }

    private static object Items(IEnumerable<AssetPurchaseItemViewModel>? items) =>
        (items ?? Enumerable.Empty<AssetPurchaseItemViewModel>())
        .Where(i => !string.IsNullOrWhiteSpace(i.Name))
        .Select(i => new { i.AssetCategoryId, i.Name, i.Description, i.Quantity, i.Price, i.DiscPercent })
        .ToList();

    // ---------- PR Aset ----------

    public async Task<PagedResult<AssetPurchaseRequestViewModel>> GetRequestsAsync(AssetPurchaseFilterViewModel f) =>
        await GetAsync<PagedResult<AssetPurchaseRequestViewModel>>($"{Pr}?{f.ToQueryString()}&page={f.Page}&pageSize={f.PageSize}") ?? new();

    public Task<AssetPurchaseRequestViewModel?> GetRequestAsync(Guid uid) => GetAsync<AssetPurchaseRequestViewModel>($"{Pr}/{uid}");

    public async Task<AssetPurchaseFormOptionsViewModel> GetRequestFormOptionsAsync() =>
        await GetAsync<AssetPurchaseFormOptionsViewModel>($"{Pr}/form-options") ?? new();

    public async Task<List<AssetDocLinkViewModel>> GetApprovedRequestsAsync() =>
        await GetAsync<List<AssetDocLinkViewModel>>($"{Pr}/approved") ?? new();

    public Task<ApiResult> SaveRequestAsync(AssetPurchaseRequestFormViewModel m) =>
        PostAsync(m.Uid == null ? $"{Pr}/create" : $"{Pr}/update", new
        {
            m.Uid, m.AprDate, m.NeededDate, m.Department, m.Requester, m.Note, Items = Items(m.Items)
        });

    public Task<ApiResult> DeleteRequestAsync(Guid uid) => PostAsync($"{Pr}/delete", new { uid });

    public Task<ApiResult> SubmitRequestAsync(Guid uid, string? reason) => PostAsync($"{Pr}/{uid}/submit", new { reason });

    public Task<ApiResult> ConvertRequestAsync(Guid uid, long? supplierId, DateOnly? apoDate) =>
        PostAsync($"{Pr}/{uid}/convert", new { supplierId, apoDate });

    // ---------- RFQ Aset ----------

    public async Task<PagedResult<AssetRfqViewModel>> GetRfqsAsync(AssetPurchaseFilterViewModel f) =>
        await GetAsync<PagedResult<AssetRfqViewModel>>($"{Rfq}?{f.ToQueryString()}&page={f.Page}&pageSize={f.PageSize}") ?? new();

    public Task<AssetRfqViewModel?> GetRfqAsync(Guid uid) => GetAsync<AssetRfqViewModel>($"{Rfq}/{uid}");

    public async Task<AssetPurchaseFormOptionsViewModel> GetRfqFormOptionsAsync() =>
        await GetAsync<AssetPurchaseFormOptionsViewModel>($"{Rfq}/form-options") ?? new();

    public Task<ApiResult> SaveRfqAsync(AssetRfqFormViewModel m) =>
        PostAsync(m.Uid == null ? $"{Rfq}/create" : $"{Rfq}/update", new
        {
            m.Uid, m.RfqDate, m.ValidUntil, m.SupplierId, m.PrUid, m.Note, Items = Items(m.Items)
        });

    public Task<ApiResult> DeleteRfqAsync(Guid uid) => PostAsync($"{Rfq}/delete", new { uid });

    public Task<ApiResult> SendRfqAsync(Guid uid) => PostAsync($"{Rfq}/{uid}/send", new { });

    public Task<ApiResult> QuoteRfqAsync(Guid uid, IEnumerable<(Guid Uid, decimal Price)> lines) =>
        PostAsync($"{Rfq}/{uid}/quote", new { lines = lines.Select(l => new { uid = l.Uid, quotedPrice = l.Price }) });

    public Task<ApiResult> CancelRfqAsync(Guid uid) => PostAsync($"{Rfq}/{uid}/cancel", new { });

    public Task<ApiResult> ConvertRfqAsync(Guid uid) => PostAsync($"{Rfq}/{uid}/convert", new { });

    // ---------- PO Aset ----------

    public async Task<PagedResult<AssetPurchaseOrderViewModel>> GetOrdersAsync(AssetPurchaseFilterViewModel f) =>
        await GetAsync<PagedResult<AssetPurchaseOrderViewModel>>($"{Po}?{f.ToQueryString()}&page={f.Page}&pageSize={f.PageSize}") ?? new();

    public Task<AssetPurchaseOrderViewModel?> GetOrderAsync(Guid uid) => GetAsync<AssetPurchaseOrderViewModel>($"{Po}/{uid}");

    public async Task<AssetPurchaseFormOptionsViewModel> GetOrderFormOptionsAsync() =>
        await GetAsync<AssetPurchaseFormOptionsViewModel>($"{Po}/form-options") ?? new();

    public Task<ApiResult> SaveOrderAsync(AssetPurchaseOrderFormViewModel m) =>
        PostAsync(m.Uid == null ? $"{Po}/create" : $"{Po}/update", new
        {
            m.Uid, m.ApoDate, m.SupplierId, m.Note, m.DiscountAmount, m.TaxPercent, Items = Items(m.Items)
        });

    public Task<ApiResult> DeleteOrderAsync(Guid uid) => PostAsync($"{Po}/delete", new { uid });

    public Task<ApiResult> SubmitOrderAsync(Guid uid, string? reason) => PostAsync($"{Po}/{uid}/submit", new { reason });

    public Task<ApiResult> CancelOrderAsync(Guid uid, string? reason) => PostAsync($"{Po}/{uid}/cancel", new { reason });

    public Task<ApiResult> RequestCancelOrderAsync(Guid uid, string? reason) => PostAsync($"{Po}/{uid}/request-cancel", new { reason });

    public Task<ApiResult> ReceiveOrderAsync(Guid uid, DateOnly? receiptDate, string? location, string? note, IEnumerable<(Guid ItemUid, int Qty)> lines) =>
        PostAsync($"{Po}/{uid}/receive", new
        {
            receiptDate, location, note,
            lines = lines.Select(l => new { itemUid = l.ItemUid, quantity = l.Qty })
        });

    public Task<ApiResult> CancelReceiptAsync(Guid receiptUid, DateOnly? date, string? reason) =>
        PostAsync($"{Po}/receipts/{receiptUid}/cancel", new { date, reason });

    // ---------- Retur Aset ----------

    public async Task<PagedResult<AssetPurchaseReturnViewModel>> GetReturnsAsync(AssetPurchaseFilterViewModel f) =>
        await GetAsync<PagedResult<AssetPurchaseReturnViewModel>>($"{Ret}?{f.ToQueryString()}&page={f.Page}&pageSize={f.PageSize}") ?? new();

    public Task<AssetPurchaseReturnViewModel?> GetReturnAsync(Guid uid) => GetAsync<AssetPurchaseReturnViewModel>($"{Ret}/{uid}");

    public async Task<List<ReturnableAssetViewModel>> GetReturnableAssetsAsync() =>
        await GetAsync<List<ReturnableAssetViewModel>>($"{Ret}/assets") ?? new();

    public Task<ApiResult> CreateReturnAsync(AssetReturnFormViewModel m) =>
        PostAsync($"{Ret}/create", new { m.ReturnDate, m.AssetUids, m.Reason, m.Note });

    public Task<ApiResult> CancelReturnAsync(Guid uid, DateOnly? date, string? reason) =>
        PostAsync($"{Ret}/{uid}/cancel", new { date, reason });

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
}
