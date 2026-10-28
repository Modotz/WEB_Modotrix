using ERP.Web.Models;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace ERP.Web.Services;

public partial class ApiClient
{
    //==================
    //MENU
    //==================
    public async Task<List<MenuViewModel>> GetMenuAsyncOld(string? search = null)
    {
        var url = "api/menus";

        if (!string.IsNullOrWhiteSpace(search))
            url += $"?search={Uri.EscapeDataString(search)}";

        return await _http.GetFromJsonAsync<List<MenuViewModel>>(url)
               ?? [];
    }

    public async Task<PagedResult<MenuViewModel>> GetMenuAsync(
        string? search = null,
        int page = 1,
        int pageSize = 10)
    {
        var url =
            $"api/menus?page={page}&pageSize={pageSize}";


        if (!string.IsNullOrWhiteSpace(search))
        {
            url +=
                $"&search={Uri.EscapeDataString(search)}";
        }


        return await _http
            .GetFromJsonAsync<
                PagedResult<MenuViewModel>
            >(url)
            ?? new PagedResult<MenuViewModel>();
    }

    public async Task<MenuViewModel?> GetMenuAsync(Guid uid)
    {
        var response = await _http.GetAsync($"api/menus/{uid}");

        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<MenuViewModel>();
    }

    public async Task<(bool Success, string? Error)> CreateMenuAsync(MenuViewModel model)
    {
        var payload = new
        {
            model.ParentId,
            model.Title,
            model.TitleEn,
            model.UrlSlug,
            model.Icon,
            model.SortOrder,
            model.IsActive
        };

        var response = await _http.PostAsJsonAsync("api/menus/create", payload);

        return response.IsSuccessStatusCode
            ? (true, null)
            : (false, await response.Content.ReadAsStringAsync());
    }

    // UPDATE pakai POST.
    public async Task<(bool Success, string? Error)> UpdateMenuAsync(MenuViewModel model)
    {
        if (string.IsNullOrWhiteSpace(model.Uid))
            return (false, "Menu tidak ditemukan.");

        var payload = new
        {
            model.Uid,
            model.ParentId,
            model.Title,
            model.TitleEn,
            model.UrlSlug,
            model.Icon,
            model.SortOrder,
            model.IsActive
        };

        var response =
            await _http.PostAsJsonAsync(
                "api/menus/update",
                payload
            );

        return response.IsSuccessStatusCode
            ? (true, null)
            : (
                false,
                await response.Content.ReadAsStringAsync()
            );
    }

    public async Task<HttpResponseMessage> SaveMenuOrderAsync(
    List<MenuViewModel> request)
    {
        var response = await _http.PostAsJsonAsync(
            "api/menus/reorder",
            request
        );

        return response;
    }

    // DELETE pakai POST.
    public async Task<(bool Success, string? Error)> DeleteMenuAsync(Guid uid)
    {
        var response = await _http.PostAsJsonAsync(
            "api/menus/delete",
            new { Uid = uid });

        return response.IsSuccessStatusCode
            ? (true, null)
            : (false, await response.Content.ReadAsStringAsync());
    }
}
