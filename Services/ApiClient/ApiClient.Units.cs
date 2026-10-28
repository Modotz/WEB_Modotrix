using ERP.Web.Models;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace ERP.Web.Services;

public partial class ApiClient
{
    //Unit
    //public async Task<List<UnitViewModel>> GetUnitsAsync(string? search = null)
    //{
    //    var url = "api/units";

    //    if (!string.IsNullOrWhiteSpace(search))
    //        url += $"?search={Uri.EscapeDataString(search)}";

    //    return await _http.GetFromJsonAsync<List<UnitViewModel>>(url)
    //           ?? [];
    //}

    public async Task<PagedResult<UnitViewModel>> GetUnitsAsync(
    string? search = null,
    int page = 1,
    int pageSize = 10)
    {
        var url = $"api/units?page={page}&pageSize={pageSize}";

        if (!string.IsNullOrWhiteSpace(search))
        {
            url += $"&search={Uri.EscapeDataString(search)}";
        }

        var response = await _http.GetAsync(url);

        var body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception(
                $"Unit API Error: {response.StatusCode}\n{body}"
            );
        }

        return System.Text.Json.JsonSerializer.Deserialize<
            PagedResult<UnitViewModel>
        >(
            body,
            new System.Text.Json.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }
        ) ?? new PagedResult<UnitViewModel>();
    }

    public async Task<UnitViewModel?> GetUnitAsync(Guid uid)
    {
        var response = await _http.GetAsync($"api/units/{uid}");

        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<UnitViewModel>();
    }

    public async Task<(bool Success, string? Error)> CreateUnitAsync(UnitViewModel model)
    {
        var payload = new
        {
            model.Id,
            model.Uid,
            model.Code,
            model.Name,
            model.Symbol,
            model.Description,
        };

        var response = await _http.PostAsJsonAsync("api/units/create", payload);

        return response.IsSuccessStatusCode
            ? (true, null)
            : (false, await response.Content.ReadAsStringAsync());
    }

    // UPDATE pakai POST.
    public async Task<(bool Success, string? Error)> UpdateUnitAsync(UnitViewModel model)
    {
        if (string.IsNullOrWhiteSpace(model.Uid))
            return (false, "Satuan tidak ditemukan.");

        var payload = new
        {
            model.Uid,
            model.Code,
            model.Name,
            model.Symbol,
            model.Description,
        };

        var response =
            await _http.PostAsJsonAsync(
                "api/units/update",
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
    public async Task<(bool Success, string? Error)> DeleteUnitAsync(Guid uid)
    {
        var response = await _http.PostAsJsonAsync(
            "api/units/delete",
            new { Uid = uid });

        return response.IsSuccessStatusCode
            ? (true, null)
            : (false, await response.Content.ReadAsStringAsync());
    }

    public async Task<List<UnitViewModel>> GetUnitsForExportAsync()
    {
        var response = await _http.GetAsync("api/units/export");

        if (!response.IsSuccessStatusCode)
            return new List<UnitViewModel>();

        var data = await response.Content.ReadFromJsonAsync<List<UnitViewModel>>();

        return data ?? new List<UnitViewModel>();
    }

    public async Task<List<DropdownItemViewModel>> GetUnitsForDropdownAsync()
    {
        var result = await _http.GetFromJsonAsync<PagedResult<UnitViewModel>>(
            "api/units?page=1&pageSize=1000"
        );

        return result?.Data?
            .Select(unit => new DropdownItemViewModel
            {
                Id = unit.Id,
                Name = unit.Name,
                Code = unit.Code,
            })
            .ToList()
            ?? new List<DropdownItemViewModel>();
    }
}
