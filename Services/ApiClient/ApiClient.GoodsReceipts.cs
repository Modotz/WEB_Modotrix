using ERP.Web.Models;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace ERP.Web.Services;

public partial class ApiClient
{
    //penerimaan-barang
    public async Task<string?> GetNextGoodsReceiptNumberAsync(
    DateOnly receiptDate,
    bool fromPRorRFQ = false)
    {
        var date = receiptDate.ToString("yyyy-MM-dd");

        var url =
            $"api/goods-receipts/next-number" +
            $"?poDate={date}&fromPRorRFQ={fromPRorRFQ.ToString().ToLowerInvariant()}";

        var result = await _http.GetFromJsonAsync<NextReceiptNumberResponse>(url);

        return result?.ReceiptNo;
    }

    public async Task<PagedResult<GoodsReceiptViewModel>> GetGoodsReceiptsAsync(
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

        var url = "api/goods-receipts?" +
                  string.Join("&", query);

        var result = await _http.GetFromJsonAsync<
            PagedResult<GoodsReceiptViewModel>>(url);

        return result ?? new PagedResult<GoodsReceiptViewModel>();
    }

    public async Task<GoodsReceiptViewModel?> GetGoodsReceiptAsync(Guid uid)
    {
        var response = await _http.GetAsync($"api/goods-receipts/{uid}");

        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();

        var model = await response.Content.ReadFromJsonAsync<GoodsReceiptViewModel>();

        if (model == null)
            return null;

        model.Details ??= new List<GoodsReceiptItemViewModel>();

        return model;
    }

    public async Task<(bool Success, string? Uid, string? Error)> CreateGoodsReceiptAsync(GoodsReceiptViewModel model)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("api/goods-receipts/create", model);
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
                    "UID tidak ditemukan dalam respons"
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

    public async Task<(bool Success, string? Error)> UpdateGoodsReceiptAsync(GoodsReceiptViewModel model)
    {
        try
        {
            var response = await _http.PostAsJsonAsync(
                "api/goods-receipts/update",
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

    /// <summary>Batalkan penerimaan: draft dihapus; posted → stok &amp; PO dikembalikan. Message = pesan API.</summary>
    public async Task<(bool Success, string? Message)> DeleteGoodsReceiptAsync(Guid uid)
    {
        var response = await _http.PostAsJsonAsync(
            "api/goods-receipts/delete",
            new { Uid = uid });

        return (response.IsSuccessStatusCode, await ReadGoodsReceiptMessageAsync(response));
    }

    /// <summary>Posting draft penerimaan (stok, PO, jurnal, faktur otomatis). Message = pesan API.</summary>
    public async Task<(bool Success, string? Message)> ApproveGoodsReceiptAsync(Guid uid)
    {
        var response = await _http.PostAsync(
            $"api/goods-receipts/{uid}/approve",
            content: null);

        return (response.IsSuccessStatusCode, await ReadGoodsReceiptMessageAsync(response));
    }

    private static async Task<string?> ReadGoodsReceiptMessageAsync(HttpResponseMessage response)
    {
        try
        {
            var json = await response.Content.ReadFromJsonAsync<JsonElement>();

            return json.ValueKind == JsonValueKind.Object && json.TryGetProperty("message", out var m)
                ? m.GetString()
                : null;
        }
        catch (Exception)
        {
            return null;
        }
    }
}
