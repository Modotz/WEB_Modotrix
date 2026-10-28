using ERP.Web.Models;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace ERP.Web.Services;

public partial class ApiClient
{
    //kategori produk
    public async Task<PagedResult<ProductCategoryViewModel>> GetProductCategoriesAsync(
        string? search = null,
        long? parentId = null,
        bool? isActive = null,
        int page = 1,
        int pageSize = 10)
    {
        var url =
            $"api/productcategories?page={page}&pageSize={pageSize}";

        // Search
        if (!string.IsNullOrWhiteSpace(search))
        {
            url +=
                $"&search={Uri.EscapeDataString(search)}";
        }

        // Filter parent
        if (parentId.HasValue)
        {
            url +=
                $"&parentId={parentId.Value}";
        }

        // Filter status
        if (isActive.HasValue)
        {
            url +=
                $"&isActive={isActive.Value.ToString().ToLower()}";
        }

        return await _http
            .GetFromJsonAsync<PagedResult<ProductCategoryViewModel>>(url)
            ?? new PagedResult<ProductCategoryViewModel>();
    }

    public async Task<ProductCategoryViewModel?> GetProductCategoryAsync(
        Guid uid)
    {
        if (uid == Guid.Empty)
            return null;

        var response =
            await _http.GetAsync(
                $"api/productcategories/{uid}");

        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();

        return await response.Content
            .ReadFromJsonAsync<ProductCategoryViewModel>();
    }

    public async Task<(bool Success, string? Error)> CreateProductCategoryAsync(
        ProductCategoryViewModel model)
    {
        var payload = new
        {
            model.ParentId,
            model.Code,
            model.Name,
            model.IsActive
        };

        var response =
            await _http.PostAsJsonAsync(
                "api/productcategories/create",
                payload);

        return response.IsSuccessStatusCode
            ? (true, null)
            : (
                false,
                await response.Content.ReadAsStringAsync()
            );
    }

    public async Task<(bool Success, string? Error)> UpdateProductCategoryAsync(
        ProductCategoryViewModel model)
    {
        if (!Guid.TryParse(model.Uid, out var uid))
        {
            return (false, "Kategori tidak ditemukan.");
        }

        var payload = new
        {
            // UID (identitas record)
            Uid = uid,
            model.ParentId,
            model.Code,
            model.Name,
            model.IsActive
        };

        var response =
            await _http.PostAsJsonAsync(
                "api/productcategories/update",
                payload);

        return response.IsSuccessStatusCode
            ? (true, null)
            : (
                false,
                await response.Content.ReadAsStringAsync()
            );
    }

    public async Task<(bool Success, string? Error)> DeleteProductCategoryAsync(
        Guid uid)
    {
        var response =
            await _http.PostAsJsonAsync(
                "api/productcategories/delete",
                new { Uid = uid });

        return response.IsSuccessStatusCode
            ? (true, null)
            : (
                false,
                await response.Content.ReadAsStringAsync()
            );
    }

    public async Task<List<ProductCategoryViewModel>> GetProductCategoryForExportAsync()
    {
        var response = await _http.GetAsync("api/ProductCategories/export");

        if (!response.IsSuccessStatusCode)
            return new List<ProductCategoryViewModel>();

        var data = await response.Content.ReadFromJsonAsync<List<ProductCategoryViewModel>>();

        return data ?? new List<ProductCategoryViewModel>();
    }
}
