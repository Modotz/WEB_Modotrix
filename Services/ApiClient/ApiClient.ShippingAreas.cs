using ERP.Web.Models;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace ERP.Web.Services;

public partial class ApiClient
{
    //Shipping Areas
    public async Task<List<ShippingAreaViewModel>> GetShippingAreasAsync(string? search = null)
    {
        var url = "api/shipping-areas";

        if (!string.IsNullOrWhiteSpace(search))
            url += $"?search={Uri.EscapeDataString(search)}";

        return await _http.GetFromJsonAsync<List<ShippingAreaViewModel>>(url)
               ?? [];
    }

    public async Task<PagedResult<ShippingAreaViewModel>> GetShippingAreasAsync(
    string? search = null,
    bool? isActive = null,
    int page = 1,
    int pageSize = 10)
    {
        var url = $"api/shipping-areas?page={page}&pageSize={pageSize}";

        if (!string.IsNullOrWhiteSpace(search))
        {
            url += $"&search={Uri.EscapeDataString(search)}";
        }

        if (isActive.HasValue)
        {
            url += $"&isActive={isActive.Value.ToString().ToLower()}";
        }

        var response = await _http.GetAsync(url);

        var body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception(
                $"Shipping Area API Error: {response.StatusCode}\n{body}"
            );
        }

        return System.Text.Json.JsonSerializer.Deserialize<
            PagedResult<ShippingAreaViewModel>
        >(
            body,
            new System.Text.Json.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }
        ) ?? new PagedResult<ShippingAreaViewModel>();
    }

    public async Task<ShippingAreaViewModel?> GetShippingAreaAsync(Guid uid)
    {
        var response = await _http.GetAsync($"api/shipping-areas/{uid}");

        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ShippingAreaViewModel>();
    }

    public async Task<(bool Success, string? Error)> CreateShippingAreaAsync(ShippingAreaViewModel model)
    {
        var payload = new
        {
            model.Id,
            model.Uid,
            model.Code,
            model.Name,
            model.Price,
            model.Description,
            model.IsActive
        };

        var response = await _http.PostAsJsonAsync("api/shipping-areas/create", payload);

        return response.IsSuccessStatusCode
            ? (true, null)
            : (false, await response.Content.ReadAsStringAsync());
    }

    // UPDATE pakai POST.
    public async Task<(bool Success, string? Error)> UpdateShippingAreaAsync(ShippingAreaViewModel model)
    {
        var payload = new
        {
            model.Uid,
            model.Code,
            model.Name,
            model.Price,
            model.Description,
            model.IsActive
        };

        var response =
            await _http.PostAsJsonAsync(
                "api/shipping-areas/update",
                payload
            );

        return response.IsSuccessStatusCode
            ? (true, null)
            : (
                false,
                await response.Content.ReadAsStringAsync()
            );
    }

    // DELETE pakai POST.
    public async Task<(bool Success, string? Error)> DeleteShippingAreaAsync(Guid uid)
    {
        var response = await _http.PostAsJsonAsync(
            "api/shipping-areas/delete",
            new { Uid = uid });

        return response.IsSuccessStatusCode
            ? (true, null)
            : (false, await response.Content.ReadAsStringAsync());
    }

    public async Task<List<ShippingAreaViewModel>> GetShippingAreasForExportAsync()
    {
        var response = await _http.GetAsync("api/shipping-areas/export");

        var responseText = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Gagal mengambil data Area Pengiriman untuk export. " +
                $"Status: {response.StatusCode}. " +
                $"URL: {response.RequestMessage?.RequestUri}. " +
                $"Response: {responseText}");
        }

        return await response.Content
            .ReadFromJsonAsync<List<ShippingAreaViewModel>>()
            ?? new List<ShippingAreaViewModel>();
    }
}
