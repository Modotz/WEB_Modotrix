using ERP.Web.Models;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace ERP.Web.Services;

public partial class ApiClient
{
    //ROLES 
    public async Task<List<RoleViewModel>> GetAllRolesAsync()
    {
        return await _http.GetFromJsonAsync<List<RoleViewModel>>(
            "api/roles/all"
        ) ?? new List<RoleViewModel>();
    }

    public async Task<PagedResult<RoleViewModel>> GetRolesAsync(
        string? search = null,
        int page = 1,
        int pageSize = 10)
    {
        var url =
            $"api/roles?page={page}&pageSize={pageSize}";

        if (!string.IsNullOrWhiteSpace(search))
            url +=
                $"&search={Uri.EscapeDataString(search)}";

        return await _http
            .GetFromJsonAsync<PagedResult<RoleViewModel>>(url)
            ?? new PagedResult<RoleViewModel>();
    }

    public async Task<RoleViewModel?> GetRoleAsync(Guid uid)
    {
        var response =
            await _http.GetAsync($"api/roles/{uid}");

        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();

        return await response.Content
            .ReadFromJsonAsync<RoleViewModel>();
    }

    public async Task<(bool Success, string? Error)>
        CreateRoleAsync(RoleViewModel model)
    {
        var payload = new
        {
            model.Name,
            model.Description,
            model.IsActive
        };

        var response =
            await _http.PostAsJsonAsync(
                "api/roles/create",
                payload);

        return response.IsSuccessStatusCode
            ? (true, null)
            : (
                false,
                await response.Content.ReadAsStringAsync()
            );
    }

    public async Task<(bool Success, string? Error)>
        UpdateRoleAsync(RoleViewModel model)
    {
        if (string.IsNullOrWhiteSpace(model.Uid))
            return (false, "Role tidak ditemukan.");

        var payload = new
        {
            model.Uid,
            model.Name,
            model.Description,
            model.IsActive
        };

        var response =
            await _http.PostAsJsonAsync(
                "api/roles/update",
                payload);

        return response.IsSuccessStatusCode
            ? (true, null)
            : (
                false,
                await response.Content.ReadAsStringAsync()
            );
    }

    public async Task<(bool Success, string? Error)>
        DeleteRoleAsync(Guid uid)
    {
        var response =
            await _http.PostAsJsonAsync(
                "api/roles/delete",
                new { Uid = uid });

        return response.IsSuccessStatusCode
            ? (true, null)
            : (
                false,
                await response.Content.ReadAsStringAsync()
            );
    }
}
