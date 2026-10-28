using ERP.Web.Models;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace ERP.Web.Services;

public partial class ApiClient
{
    //RFQ Barang

    public async Task<PagedResult<ERP.Web.ViewModels.GoodsRfqViewModel>>
    GetGoodsRfqsAsync(
        string? search = null,
        string? status = null,
        int page = 1,
        int pageSize = 10)
    {
        var url = $"api/GoodsRfqs?page={page}&pageSize={pageSize}";

        if (!string.IsNullOrWhiteSpace(search))
            url += $"&search={Uri.EscapeDataString(search)}";

        if (!string.IsNullOrWhiteSpace(status))
            url += $"&status={Uri.EscapeDataString(status)}";

        return await _http
            .GetFromJsonAsync<
                PagedResult<ERP.Web.ViewModels.GoodsRfqViewModel>>(url)
            ?? new PagedResult<
                ERP.Web.ViewModels.GoodsRfqViewModel>();
    }

    public async Task<ERP.Web.ViewModels.GoodsRfqViewModel?> GetGoodsRfqAsync(Guid uid)
    {
        var response = await _http.GetAsync($"api/GoodsRfqs/{uid}");

        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content
            .ReadFromJsonAsync<ERP.Web.ViewModels.GoodsRfqViewModel>();
    }

    // Mengembalikan Uid RFQ baru supaya bisa redirect ke halaman Details
    public async Task<(bool Success, string? Error, string? Uid)> CreateGoodsRfqAsync(
        object payload)
    {
        var response = await _http.PostAsJsonAsync("api/GoodsRfqs/create", payload);
        var body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            return (false, body, null);

        return (true, null, ReadGoodsRfqUidFromJson(body));
    }

    // Ambil "uid" dari response JSON API (identitas record, bukan id angka).
    private static string? ReadGoodsRfqUidFromJson(string body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);

            return doc.RootElement.ValueKind == JsonValueKind.Object &&
                   doc.RootElement.TryGetProperty("uid", out var uidElement) &&
                   uidElement.ValueKind == JsonValueKind.String
                ? uidElement.GetString()
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public async Task<(bool Success, string? Error)> UpdateGoodsRfqAsync(
        Guid uid, object payload)
    {
        var response = await _http.PutAsJsonAsync($"api/GoodsRfqs/{uid}", payload);
        return await ToActionResultAsync(response);
    }

    // draft -> terkirim
    public async Task<(bool Success, string? Error)> SendGoodsRfqAsync(Guid uid)
    {
        var response = await _http.PostAsync($"api/GoodsRfqs/{uid}/send", null);
        return await ToActionResultAsync(response);
    }

    // terkirim -> ditawar
    public async Task<(bool Success, string? Error)> SaveGoodsRfqQuoteAsync(
        Guid uid, object payload)
    {
        var response = await _http.PostAsJsonAsync($"api/GoodsRfqs/{uid}/quote", payload);
        return await ToActionResultAsync(response);
    }

    // -> batal
    public async Task<(bool Success, string? Error)> CancelGoodsRfqAsync(Guid uid)
    {
        var response = await _http.PostAsync($"api/GoodsRfqs/{uid}/cancel", null);
        return await ToActionResultAsync(response);
    }

    /// <summary>Jadikan RFQ (quoted) sebagai PO draft. Mengembalikan uid PO baru.</summary>
    public async Task<(bool Success, string? Error, string? PoUid)> ConvertGoodsRfqToPoAsync(Guid uid, long warehouseId)
    {
        var response = await _http.PostAsJsonAsync($"api/GoodsRfqs/{uid}/convert", new { warehouseId });
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();

        string? Read(string name) =>
            json.ValueKind == JsonValueKind.Object && json.TryGetProperty(name, out var v) ? v.GetString() : null;

        return response.IsSuccessStatusCode
            ? (true, null, Read("poUid"))
            : (false, Read("message") ?? "RFQ gagal dijadikan PO.", null);
    }

    public async Task<List<GoodsRfqItemViewModel>>
        GetGoodsRfqProductsAsync()
    {
        var response = await _http.GetAsync(
            "api/GoodsRfqItems/products");

        if (!response.IsSuccessStatusCode)
            return new List<GoodsRfqItemViewModel>();

        var products = await response.Content
            .ReadFromJsonAsync<List<GoodsRfqItemProduct>>();

        return products?.Select(p => new GoodsRfqItemViewModel
        {
            ProductId = p.Id,
            ProductName = p.Name,
            Sku = p.Sku,
            UnitId = p.UnitId,
            UnitSymbol = p.UnitSymbol,
            CostPrice = p.CostPrice
        }).ToList() ?? new List<GoodsRfqItemViewModel>();
    }

    public async Task<(bool Success, string? Error)> DeleteGoodsRfqAsync(Guid uid)
    {
        var response = await _http.DeleteAsync($"api/GoodsRfqs/{uid}");
        return await ToActionResultAsync(response);
    }

    public async Task<string?> GetNextGoodsRfqNoAsync()
    {
        try
        {
            var response = await _http.GetAsync("api/GoodsRfqs/next-number");
            if (!response.IsSuccessStatusCode) return null;

            using var doc = await System.Text.Json.JsonDocument.ParseAsync(
                await response.Content.ReadAsStreamAsync());

            return doc.RootElement.TryGetProperty("rfqNo", out var v)
                ? v.GetString()
                : null;
        }
        catch
        {
            return null;
        }
    }
}
