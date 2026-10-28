using ERP.Web.Models;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace ERP.Web.Services;

public partial class ApiClient
{
    // price-list 
    public async Task<PagedResult<PriceListViewModel>> GetPriceListsAsync(
        string? search = null,
        bool? isActive = null,
        int page = 1,
        int pageSize = 10)
    {
        var url =
            $"api/pricelists?page={page}&pageSize={pageSize}";

        if (!string.IsNullOrWhiteSpace(search))
        {
            url +=
                $"&search={Uri.EscapeDataString(search)}";
        }

        if (isActive.HasValue)
        {
            url +=
                $"&isActive={isActive.Value.ToString().ToLower()}";
        }

        return await _http
            .GetFromJsonAsync<PagedResult<PriceListViewModel>>(url)
            ?? new PagedResult<PriceListViewModel>();
    }

    public async Task<PriceListViewModel?> GetPriceListAsync(
        Guid uid)
    {
        var response =
            await _http.GetAsync(
                $"api/pricelists/{uid}");

        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();

        return await response.Content
            .ReadFromJsonAsync<PriceListViewModel>();
    }

    public async Task<(bool Success, string? Error)> CreatePriceListAsync(
        PriceListViewModel model)
    {
        var payload = new
        {
            model.Code,
            model.Name,
            model.Note,
            model.IsActive
        };

        var response =
            await _http.PostAsJsonAsync(
                "api/pricelists/create",
                payload);

        return response.IsSuccessStatusCode
            ? (true, null)
            : (
                false,
                await response.Content.ReadAsStringAsync()
            );
    }

    public async Task<(bool Success, string? Error)> UpdatePriceListAsync(
        PriceListViewModel model)
    {
        var payload = new
        {
            model.Uid,
            model.Code,
            model.Name,
            model.Note,
            model.IsActive
        };

        var response =
            await _http.PostAsJsonAsync(
                "api/pricelists/update",
                payload);

        return response.IsSuccessStatusCode
            ? (true, null)
            : (
                false,
                await response.Content.ReadAsStringAsync()
            );
    }

    public async Task<(bool Success, string? Error)> DeletePriceListAsync(
        Guid uid)
    {
        var response =
            await _http.PostAsJsonAsync(
                "api/pricelists/delete",
                new { Uid = uid });

        return response.IsSuccessStatusCode
            ? (true, null)
            : (
                false,
                await response.Content.ReadAsStringAsync()
            );
    }

    public async Task<List<PriceListViewModel>> GetPriceListForExportAsync()
    {
        var response = await _http.GetAsync("api/PriceLists/export");

        if (!response.IsSuccessStatusCode)
            return new List<PriceListViewModel>();

        var data = await response.Content
           .ReadFromJsonAsync<List<PriceListViewModel>>();

        return data ?? new List<PriceListViewModel>();
    }

    //Price List Item 
    public async Task<List<PriceListItemViewModel>> GetPriceListItemsAsync(Guid priceListUid)
    {
        var response = await _http.GetAsync(
            $"api/pricelists/{priceListUid}/items");

        if (response.StatusCode == HttpStatusCode.NotFound)
            return new List<PriceListItemViewModel>();

        response.EnsureSuccessStatusCode();

        // DEBUG sementara: lihat JSON mentah di jendela Output Visual Studio
        var raw = await response.Content.ReadAsStringAsync();
        System.Diagnostics.Debug.WriteLine(raw);

        return System.Text.Json.JsonSerializer.Deserialize<List<PriceListItemViewModel>>(
                   raw,
                   new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web))
               ?? new List<PriceListItemViewModel>();
    }

    public async Task<(bool Success, string? Error)> CreatePriceListItemAsync(
        PriceListItemViewModel model)
    {
        var payload = new
        {
            model.PriceListUid,
            model.ProductId,
            model.Price,
            model.TaxInclusive
        };

        var response = await _http.PostAsJsonAsync(
            "api/pricelists/items/create",
            payload);

        if (response.IsSuccessStatusCode)
            return (true, null);

        return (false, await response.Content.ReadAsStringAsync());
    }

    public async Task<(bool Success, string? Error)> UpdatePriceListItemAsync(
        PriceListItemViewModel model)
    {
        var payload = new
        {
            model.Uid,
            model.Price,
            model.TaxInclusive
        };

        var response = await _http.PostAsJsonAsync(
            "api/pricelists/items/update",
            payload);

        if (response.IsSuccessStatusCode)
            return (true, null);

        return (false, await response.Content.ReadAsStringAsync());
    }

    public async Task<(bool Success, string? Error)> DeletePriceListItemAsync(Guid uid)
    {
        var response = await _http.PostAsJsonAsync(
            "api/pricelists/items/delete",
            new
            {
                Uid = uid
            });

        if (response.IsSuccessStatusCode)
            return (true, null);

        return (false, await response.Content.ReadAsStringAsync());
    }
}
