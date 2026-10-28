using ERP.Web.Models;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace ERP.Web.Services;

public partial class ApiClient
{
    //===================
    //USER
    //====================
    public async Task<PagedResult<UserViewModel>> GetUsersAsync(
        string? search = null,
        int? roleId = null,
        bool? isActive = null,
        int page = 1,
        int pageSize = 10)
    {
        var url =
            $"api/users?page={page}&pageSize={pageSize}";

        if (!string.IsNullOrWhiteSpace(search))
            url +=
                $"&search={Uri.EscapeDataString(search)}";

        if (roleId.HasValue)
            url +=
                $"&roleId={roleId.Value}";

        if (isActive.HasValue)
            url +=
                $"&isActive={isActive.Value.ToString().ToLower()}";

        return await _http
            .GetFromJsonAsync<PagedResult<UserViewModel>>(url)
            ?? new PagedResult<UserViewModel>();
    }

    public async Task<List<UserViewModel>> GetUsersForExportAsync()
    {
        var url = "api/users?page=1&pageSize=100000";

        var result = await _http
            .GetFromJsonAsync<PagedResult<UserViewModel>>(url);

        return result?.Data?.ToList()
               ?? new List<UserViewModel>();
    }

    public async Task<UserViewModel?> GetUserAsync(Guid uid)
    {
        var response =
            await _http.GetAsync($"api/users/{uid}");

        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();

        return await response.Content
            .ReadFromJsonAsync<UserViewModel>();
    }

    // CREATE USER
    public async Task<(bool Success, string? Error)>
        CreateUserAsync(UserViewModel model)
    {
        var payload = new
        {
            model.RoleId,
            model.Name,
            model.Username,
            model.Email,
            model.Password,
            model.Photo,
            model.Signature,
            model.IsActive
        };

        var response =
            await _http.PostAsJsonAsync(
                "api/users/create",
                payload);

        return response.IsSuccessStatusCode
            ? (true, null)
            : (
                false,
                await response.Content.ReadAsStringAsync()
            );
    }

    // UPDATE USER
    public async Task<(bool Success, string? Error)>
        UpdateUserAsync(UserViewModel model)
    {
        if (string.IsNullOrWhiteSpace(model.Uid))
            return (false, "User tidak ditemukan.");

        var payload = new
        {
            model.Uid,
            model.RoleId,
            model.Name,
            model.Username,
            model.Email,
            model.Password,
            model.Photo,
            model.Signature,
            model.IsActive
        };

        var response =
            await _http.PostAsJsonAsync(
                "api/users/update",
                payload);

        return response.IsSuccessStatusCode
            ? (true, null)
            : (
                false,
                await response.Content.ReadAsStringAsync()
            );
    }

    // UPLOAD FOTO USER
    public async Task<(bool Success, string? Photo, string? Error)>
        UploadUserPhotoAsync(IFormFile file)
    {
        using var content = new MultipartFormDataContent();

        using var stream = file.OpenReadStream();
        using var fileContent = new StreamContent(stream);

        fileContent.Headers.ContentType =
            new System.Net.Http.Headers.MediaTypeHeaderValue(
                file.ContentType);

        content.Add(
            fileContent,
            "photo",
            file.FileName);

        var response =
            await _http.PostAsync(
                "api/users/upload-photo",
                content);

        if (!response.IsSuccessStatusCode)
        {
            return (
                false,
                null,
                await response.Content.ReadAsStringAsync()
            );
        }

        var result =
            await response.Content
                .ReadFromJsonAsync<PhotoUploadResponse>();

        return (
            true,
            result?.Photo,
            null
        );
    }

    public async Task<(bool Success, string? Error)>
        DeleteUserAsync(Guid uid)
    {
        var response =
            await _http.PostAsJsonAsync(
                "api/users/delete",
                new { Uid = uid });

        return response.IsSuccessStatusCode
            ? (true, null)
            : (
                false,
                await response.Content.ReadAsStringAsync()
            );
    }
}
