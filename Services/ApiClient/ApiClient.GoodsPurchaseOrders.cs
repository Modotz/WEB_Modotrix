using ERP.Web.Models;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace ERP.Web.Services;

public partial class ApiClient
{
    //PO Barang
    public async Task<string?> GetNextGoodsPONumberAsync(
    DateOnly poDate,
    bool fromPRorRFQ = false)
    {
        var date = poDate.ToString("yyyy-MM-dd");

        var url =
            $"api/goods-purchase-orders/next-number" +
            $"?poDate={date}&fromPRorRFQ={fromPRorRFQ.ToString().ToLowerInvariant()}";

        var result = await _http.GetFromJsonAsync<NextPONumberResponse>(url);

        return result?.PoNo;
    }

    public async Task<PagedResult<GoodsPurchaseOrderViewModel>> GetGoodsPurchaseOrdersAsync(
        string? search = null,
        int page = 1,
        int pageSize = 10,
        string? status = null)
    {
        var query = new List<string>
        {
            $"page={page}",
            $"pageSize={pageSize}"
        };

        if (!string.IsNullOrWhiteSpace(search))
        {
            query.Add($"search={Uri.EscapeDataString(search)}");
        }

        var url = "api/goods-purchase-orders?" +
                  string.Join("&", query);

        var result = await _http.GetFromJsonAsync<
            PagedResult<GoodsPurchaseOrderViewModel>>(url);

        return result ?? new PagedResult<GoodsPurchaseOrderViewModel>();
    }

    public async Task<GoodsPurchaseOrderViewModel?> GetGoodsPurchaseOrderAsync(Guid uid)
    {
        var response = await _http.GetAsync($"api/goods-purchase-orders/{uid}");

        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();

        var model = await response.Content
            .ReadFromJsonAsync<GoodsPurchaseOrderViewModel>();

        if (model == null)
            return null;

        model.Details ??= new List<GoodsPurchaseOrderItemViewModel>();

        return model;
    }

    public async Task<(bool Success, string? Uid, string? Error)> CreateGoodsPurchaseOrderAsync(GoodsPurchaseOrderViewModel model)
    {
        try
        {
            var response = await _http.PostAsJsonAsync( "api/goods-purchase-orders/create", model ); 
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return (
                    false,
                    null,
                    $"HTTP {(int)response.StatusCode} ({response.StatusCode}): {responseContent}"
                );
            }

            using var document = JsonDocument.Parse(responseContent);

            var root = document.RootElement;

            // Identitas record = uid (bukan id angka)
            if (!root.TryGetProperty("uid", out var uidElement) ||
                uidElement.ValueKind != JsonValueKind.String ||
                string.IsNullOrWhiteSpace(uidElement.GetString()))
            {
                return (
                    false,
                    null,
                    "PO berhasil dikirim ke API, tetapi UID PO tidak ditemukan pada response."
                );
            }

            return (true, uidElement.GetString(), null);
        }
        catch (HttpRequestException ex)
        {
            return (false, null, $"Gagal menghubungi API: {ex.Message}");
        }
        catch (JsonException ex)
        {
            return (false, null, $"Response API tidak valid: {ex.Message}");
        }
    }

    public async Task<(bool Success, string? Error)> UpdateGoodsPurchaseOrderAsync(GoodsPurchaseOrderViewModel model)
    {
        try
        {
            var response = await _http.PostAsJsonAsync(
                "api/goods-purchase-orders/update",
                model);

            if (response.IsSuccessStatusCode)
                return (true, null);

            var error = await response.Content.ReadAsStringAsync();

            return (
                false,
                $"HTTP {(int)response.StatusCode} ({response.StatusCode}): {error}"
            );
        }
        catch (HttpRequestException ex)
        {
            return (false, $"Gagal menghubungi API: {ex.Message}");
        }
        catch (TaskCanceledException ex)
        {
            return (false, $"Request API dibatalkan atau timeout: {ex.Message}");
        }
    }

    public async Task<(bool Success, string? Error)> DeleteGoodsPurchaseOrderAsync(Guid uid)
    {
        var response = await _http.PostAsJsonAsync(
            "api/goods-purchase-orders/delete",
            new { Uid = uid });

        return response.IsSuccessStatusCode
            ? (true, null)
            : (false, await response.Content.ReadAsStringAsync());
    }

    public async Task<bool> ApproveGoodsPurchaseOrderAsync(Guid uid)
    {
        var response = await _http.PostAsync(
            $"api/goods-purchase-orders/{uid}/approve",
            content: null);

        return response.IsSuccessStatusCode;
    }
}
