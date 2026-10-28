using ERP.Web.Models;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace ERP.Web.Services;

public partial class ApiClient
{
    //Supplier
    //public async Task<List<DropdownItemViewModel>> GetSuppliersForDropdownAsync()
    //{
    //    var response =
    //        await _http.GetAsync(
    //            "api/suppliers?page=1&pageSize=100000");

    //    if (!response.IsSuccessStatusCode)
    //    {
    //        return new List<DropdownItemViewModel>();
    //    }

    //    var result =
    //        await response.Content
    //            .ReadFromJsonAsync<PagedResult<BrandViewModel>>();

    //    if (result?.Data == null)
    //    {
    //        return new List<DropdownItemViewModel>();
    //    }

    //    return result.Data
    //        .Where(x => x.IsActive && x.Id.HasValue)
    //        .Select(x => new DropdownItemViewModel
    //        {
    //            Id = x.Id.Value,
    //            Name = x.Name
    //        })
    //        .ToList();
    //}

    public async Task<List<DropdownItemViewModel>> GetSuppliersForDropdownAsync()
    {
        var response = await _http.GetAsync(
            "api/suppliers?page=1&pageSize=100000");

        var body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Gagal mengambil Supplier. " +
                $"Status: {response.StatusCode}. " +
                $"Response: {body}");
        }

        var result = System.Text.Json.JsonSerializer.Deserialize<
            PagedResult<SupplierViewModel>
        >(
            body,
            new System.Text.Json.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

        return result?.Data?
            .Where(x => x.IsActive && x.Id.HasValue)
            .Select(x => new DropdownItemViewModel
            {
                Id = x.Id!.Value,
                Name = x.Name
            })
            .ToList()
            ?? new List<DropdownItemViewModel>();
    }

    public async Task<PagedResult<SupplierViewModel>> GetSuppliersAsync(
    string? search = null,
    bool? isActive = null,
    int page = 1,
    int pageSize = 10)
    {
        var url = $"api/suppliers?page={page}&pageSize={pageSize}";

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
                $"Supplier API Error: {response.StatusCode}\n{body}"
            );
        }

        return System.Text.Json.JsonSerializer.Deserialize<
            PagedResult<SupplierViewModel>
        >(
            body,
            new System.Text.Json.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }
        ) ?? new PagedResult<SupplierViewModel>();
    }

    public async Task<SupplierViewModel?> GetSupplierAsync(Guid uid)
    {
        var response = await _http.GetAsync($"api/suppliers/{uid}");

        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<SupplierViewModel>();
    }

    public async Task<(bool Success, string? Error)> CreateSupplierAsync(SupplierViewModel model)
    {
        var payload = new
        {
            model.Id,
            model.Uid,
            model.Code,
            model.Name,
            model.ContactPerson,
            model.Phone,
            model.Email,
            model.Npwp,
            model.Address,
            model.IsActive
        };

        var response = await _http.PostAsJsonAsync("api/suppliers/create", payload);

        return response.IsSuccessStatusCode
            ? (true, null)
            : (false, await response.Content.ReadAsStringAsync());
    }

    // UPDATE pakai POST.
    public async Task<(bool Success, string? Error)> UpdateSupplierAsync(SupplierViewModel model)
    {
        var payload = new
        {
            model.Uid,
            model.Code,
            model.Name,
            model.ContactPerson,
            model.Phone,
            model.Email,
            model.Npwp,
            model.Address,
            model.IsActive
        };

        var response =
            await _http.PostAsJsonAsync(
                "api/suppliers/update",
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
    public async Task<(bool Success, string? Error)> DeleteSupplierAsync(Guid uid)
    {
        var response = await _http.PostAsJsonAsync(
            "api/suppliers/delete",
            new { Uid = uid });

        return response.IsSuccessStatusCode
            ? (true, null)
            : (false, await response.Content.ReadAsStringAsync());
    }

    public async Task<List<SupplierViewModel>> GetSuppliersForExportAsync()
    {
        var response = await _http.GetAsync("api/suppliers/export");

        if (!response.IsSuccessStatusCode)
            return new List<SupplierViewModel>();

        var data = await response.Content.ReadFromJsonAsync<List<SupplierViewModel>>();

        return data ?? new List<SupplierViewModel>();
    }
}
