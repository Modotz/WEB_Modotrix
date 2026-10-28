using ERP.Web.Models;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace ERP.Web.Services;

public partial class ApiClient
{
    //================
    //WORK CENTER
    public async Task<PagedResult<WorkCenterViewModel>> GetWorkCentersAsync(
     string? search = null,
     bool? isActive = null,
     int page = 1,
     int pageSize = 10)
    {
        var url =
            $"api/workcenters?page={page}&pageSize={pageSize}";

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
            .GetFromJsonAsync<PagedResult<WorkCenterViewModel>>(url)
            ?? new PagedResult<WorkCenterViewModel>();
    }

    public async Task<WorkCenterViewModel?> GetWorkCenterAsync(
        Guid uid)
    {
        var response =
            await _http.GetAsync(
                $"api/workcenters/{uid}");

        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();

        return await response.Content
            .ReadFromJsonAsync<WorkCenterViewModel>();
    }

    public async Task<(bool Success, string? Error)> CreateWorkCenterAsync(
        WorkCenterViewModel model)
    {
        var payload = new
        {
            model.Code,
            model.Name,
            model.CostPerHour,
            model.IsActive
        };

        var response =
            await _http.PostAsJsonAsync(
                "api/workcenters/create",
                payload);

        return response.IsSuccessStatusCode
            ? (true, null)
            : (
                false,
                await response.Content.ReadAsStringAsync()
            );
    }

    public async Task<(bool Success, string? Error)> UpdateWorkCenterAsync(
        WorkCenterViewModel model)
    {
        if (string.IsNullOrWhiteSpace(model.Uid))
            return (false, "Work Center tidak ditemukan.");

        var payload = new
        {
            model.Uid,
            model.Code,
            model.Name,
            model.CostPerHour,
            model.IsActive
        };

        var response =
            await _http.PostAsJsonAsync(
                "api/workcenters/update",
                payload);

        return response.IsSuccessStatusCode
            ? (true, null)
            : (
                false,
                await response.Content.ReadAsStringAsync()
            );
    }

    public async Task<(bool Success, string? Error)> DeleteWorkCenterAsync(
        Guid uid)
    {
        var response =
            await _http.PostAsJsonAsync(
                "api/workcenters/delete",
                new { Uid = uid });

        return response.IsSuccessStatusCode
            ? (true, null)
            : (
                false,
                await response.Content.ReadAsStringAsync()
            );
    }

    public async Task<List<WorkCenterViewModel>> GetWorkCenterForExportAsync()
    {
        var response = await _http.GetAsync("api/WorkCenters/export");

        if (!response.IsSuccessStatusCode)
            return new List<WorkCenterViewModel>();

        var data = await response.Content.ReadFromJsonAsync<List<WorkCenterViewModel>>();

        return data ?? new List<WorkCenterViewModel>();
    }
}
