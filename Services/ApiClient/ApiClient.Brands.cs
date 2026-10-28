using ERP.Web.Models;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace ERP.Web.Services;

public partial class ApiClient
{
    //brand
    public async Task<PagedResult<BrandViewModel>> GetBrandsAsync(
        string? search = null,
        bool? isActive = null,
        int page = 1,
        int pageSize = 10)
    {
        var url =
            $"api/brands?page={page}&pageSize={pageSize}";

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
            .GetFromJsonAsync<PagedResult<BrandViewModel>>(url)
            ?? new PagedResult<BrandViewModel>();
    }

    // =========================================================
    // GET BRAND BY ID
    // =========================================================

    public async Task<BrandViewModel?> GetBrandAsync(Guid uid)
    {
        var response =
            await _http.GetAsync(
                $"api/brands/{uid}");

        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();

        return await response.Content
            .ReadFromJsonAsync<BrandViewModel>();
    }

    // =========================================================
    // CREATE BRAND
    // =========================================================

    public async Task<(bool Success, string? Error)>
        CreateBrandAsync(BrandViewModel model)
    {
        using var content =
            new MultipartFormDataContent();


        // NAME

        content.Add(
            new StringContent(
                model.Name ?? string.Empty),
            "Name");


        // IMAGE

        content.Add(
            new StringContent(
                model.Image ?? string.Empty),
            "Image");


        // IS ACTIVE
        // WAJIB DIKIRIM, TERMASUK SAAT FALSE

        content.Add(
            new StringContent(
                model.IsActive
                    ? "true"
                    : "false"),
            "IsActive");


        var response =
            await _http.PostAsync(
                "api/brands/create",
                content);


        return response.IsSuccessStatusCode
            ? (true, null)
            : (
                false,
                await response.Content.ReadAsStringAsync()
            );
    }

    // =========================================================
    // UPDATE BRAND
    // =========================================================

    public async Task<(bool Success, string? Error)>
        UpdateBrandAsync(BrandViewModel model)
    {
        if (string.IsNullOrWhiteSpace(model.Uid))
        {
            return (
                false,
                "Brand tidak ditemukan."
            );
        }


        using var content =
            new MultipartFormDataContent();


        // UID (identitas record)

        content.Add(
            new StringContent(
                model.Uid),
            "Uid");


        // NAME

        content.Add(
            new StringContent(
                model.Name ?? string.Empty),
            "Name");


        // IMAGE

        content.Add(
            new StringContent(
                model.Image ?? string.Empty),
            "Image");


        // IS ACTIVE
        // SELALU DIKIRIM, TERMASUK FALSE

        content.Add(
            new StringContent(
                model.IsActive
                    ? "true"
                    : "false"),
            "IsActive");


        var response =
            await _http.PostAsync(
                "api/brands/update",
                content);


        return response.IsSuccessStatusCode
            ? (true, null)
            : (
                false,
                await response.Content.ReadAsStringAsync()
            );
    }

    // =========================================================
    // DELETE BRAND - SOFT DELETE
    // =========================================================

    public async Task<(bool Success, string? Error)>
        DeleteBrandAsync(Guid uid)
    {
        var response =
            await _http.PostAsJsonAsync(
                "api/brands/delete",
                new
                {
                    Uid = uid
                });

        return response.IsSuccessStatusCode
            ? (true, null)
            : (
                false,
                await response.Content.ReadAsStringAsync()
            );
    }

    // =========================================================
    // UPLOAD IMAGE BRAND
    // =========================================================

    public async Task<(bool Success, string? Image, string? Error)>
        UploadBrandImageAsync(IFormFile file)
    {
        using var content =
            new MultipartFormDataContent();

        using var stream =
            file.OpenReadStream();

        using var fileContent =
            new StreamContent(stream);

        fileContent.Headers.ContentType =
            new System.Net.Http.Headers.MediaTypeHeaderValue(
                file.ContentType);

        content.Add(
            fileContent,
            "image",
            file.FileName);

        var response =
            await _http.PostAsync(
                "api/brands/upload-image",
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
                .ReadFromJsonAsync<BrandImageUploadResponse>();

        return (
            true,
            result?.Image,
            null
        );
    }

    // =========================================================
    // GET BRAND FOR EXPORT / PRINT
    // =========================================================

    public async Task<List<BrandViewModel>>
        GetBrandsForExportAsync(
            string? search = null,
            bool? isActive = null)
    {
        var url =
            "api/brands?page=1&pageSize=100000";

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

        var result =
            await _http
                .GetFromJsonAsync<PagedResult<BrandViewModel>>(url);

        return result?.Data?.ToList()
               ?? new List<BrandViewModel>();
    }

    public async Task<List<DropdownItemViewModel>>GetBrandsForDropdownAsync()
    {
        var response =
            await _http.GetAsync(
                "api/brands?page=1&pageSize=100000");

        if (!response.IsSuccessStatusCode)
        {
            return new List<DropdownItemViewModel>();
        }

        var result =
            await response.Content
                .ReadFromJsonAsync<PagedResult<BrandViewModel>>();

        if (result?.Data == null)
        {
            return new List<DropdownItemViewModel>();
        }

        return result.Data
            .Where(x => x.IsActive && x.Id.HasValue)
            .Select(x => new DropdownItemViewModel
            {
                Id = x.Id.Value,
                Name = x.Name
            })
            .ToList();
    }

    //relasi dropdown
    public async Task<List<DropdownItemViewModel>> GetBrandsAsync()
    {
        var response = await _http.GetAsync("api/brands");

        if (!response.IsSuccessStatusCode)
        {
            return new List<DropdownItemViewModel>();
        }

        var result =
            await response.Content.ReadFromJsonAsync<PagedResult<UnitViewModel>>();

        if (result?.Data == null)
        {
            return new List<DropdownItemViewModel>();
        }

        return result.Data
            .Select(x => new DropdownItemViewModel
            {
                Id = x.Id,
                Name = x.Name
            })
            .ToList();
    }
}
