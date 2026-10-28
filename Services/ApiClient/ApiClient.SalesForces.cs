using ERP.Web.Models;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace ERP.Web.Services;

public partial class ApiClient
{
    //sales-force  
    public async Task<PagedResult<SalesForceViewModel>> GetSalesForcesAsync(
        string? search = null,
        bool? isActive = null,
        int page = 1,
        int pageSize = 10)
    {
        var query =
            $"api/salesforces?page={page}&pageSize={pageSize}";

        if (!string.IsNullOrWhiteSpace(search))
        {
            query +=
                $"&search={Uri.EscapeDataString(search)}";
        }

        if (isActive.HasValue)
        {
            query +=
                $"&isActive={isActive.Value.ToString().ToLower()}";
        }

        var result =
            await _http.GetFromJsonAsync<
                PagedResult<SalesForceViewModel>
            >(query);

        return result ??
               new PagedResult<SalesForceViewModel>();
    }

    public async Task<SalesForceViewModel?> GetSalesForceAsync(
        Guid uid)
    {
        var response =
            await _http.GetAsync(

             $"api/salesforces/{uid}");

        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();

        return await response.Content
            .ReadFromJsonAsync<SalesForceViewModel>();
    }

    public async Task<(bool Success, string? Error)>
        CreateSalesForceAsync(
            SalesForceViewModel model)
    {
        var payload = new
        {
            model.Code,
            model.Name,
            model.Phone,
            model.Email,
            model.Area,
            model.IsActive
        };

        var response =
            await _http.PostAsJsonAsync(
                "api/salesforces/create",
                payload);

        return response.IsSuccessStatusCode
            ? (true, null)
            : (
                false,
                await response.Content.ReadAsStringAsync()
            );
    }

    public async Task<(bool Success, string? Error)>
        UpdateSalesForceAsync(
            SalesForceViewModel model)
    {
        var payload = new
        {
            model.Uid,
            model.Code,
            model.Name,
            model.Phone,
            model.Email,
            model.Area,
            model.IsActive
        };

        var response =
            await _http.PostAsJsonAsync(
                "api/salesforces/update",
                payload);

        return response.IsSuccessStatusCode
            ? (true, null)
            : (
                false,
                await response.Content.ReadAsStringAsync()
            );
    }

    public async Task<(bool Success, string? Error)> DeleteSalesForceAsync(Guid uid)
    {
        var payload = new
        {
            Uid = uid
        };

        var response =
            await _http.PostAsJsonAsync(
                "api/salesforces/delete",
                payload);

        return response.IsSuccessStatusCode
            ? (true, null)
            : (
                false,
                await response.Content.ReadAsStringAsync()
            );
    }

    public async Task<List<SalesForceViewModel>> GetSalesForceForExportAsync()
    {
        var response = await _http.GetAsync("api/SalesForces/export");

        if (!response.IsSuccessStatusCode)
            return new List<SalesForceViewModel>();

        var data = await response.Content.ReadFromJsonAsync<List<SalesForceViewModel>>();
        return data ?? new List<SalesForceViewModel>();
    }
}
