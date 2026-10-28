using ERP.Web.Models;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace ERP.Web.Services;

public partial class ApiClient
{
    //PRODUCT ATTRIBUTE
    public async Task<PagedResult<ProductAttributeViewModel>> GetProductAttributesAsync(
    string? search = null,
    bool? isActive = null,
    int page = 1,
    int pageSize = 10)
    {
        var url =
            $"api/productattributes?page={page}&pageSize={pageSize}";

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
            .GetFromJsonAsync<PagedResult<ProductAttributeViewModel>>(url)
            ?? new PagedResult<ProductAttributeViewModel>();
    }

    public async Task<ProductAttributeViewModel?> GetProductAttributeAsync(
        Guid uid)
    {
        if (uid == Guid.Empty)
            return null;

        var response =
            await _http.GetAsync(
                $"api/productattributes/{uid}");

        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();

        return await response.Content
            .ReadFromJsonAsync<ProductAttributeViewModel>();
    }

    public async Task<(bool Success, string? Error)> CreateProductAttributeAsync(
        ProductAttributeViewModel model)
    {
        var payload = new
        {
            model.Name,
            model.IsActive
        };

        var response =
            await _http.PostAsJsonAsync(
                "api/productattributes/create",
                payload);

        return response.IsSuccessStatusCode
            ? (true, null)
            : (
                false,
                await response.Content.ReadAsStringAsync()
            );
    }

    public async Task<(bool Success, string? Error)> UpdateProductAttributeAsync(
        ProductAttributeViewModel model)
    {
        if (!Guid.TryParse(model.Uid, out var uid))
        {
            return (false, "Atribut tidak ditemukan.");
        }

        var payload = new
        {
            // UID (identitas record)
            Uid = uid,
            model.Code,
            model.Name,
            model.IsActive
        };

        var response =
            await _http.PostAsJsonAsync(
                "api/productattributes/update",
                payload);

        return response.IsSuccessStatusCode
            ? (true, null)
            : (
                false,
                await response.Content.ReadAsStringAsync()
            );
    }

    public async Task<(bool Success, string? Error)> DeleteProductAttributeAsync(
        Guid uid)
    {
        var response =
            await _http.PostAsJsonAsync(
                "api/productattributes/delete",
                new { Uid = uid });

        return response.IsSuccessStatusCode
            ? (true, null)
            : (
                false,
                await response.Content.ReadAsStringAsync()
            );
    }

    public async Task<List<ProductAttributeViewModel>> GetProductAttributeForExportAsync()
    {
        var response = await _http.GetAsync("api/ProductAttributes/export");

        if (!response.IsSuccessStatusCode)
            return new List<ProductAttributeViewModel>();

        var data = await response.Content.ReadFromJsonAsync<List<ProductAttributeViewModel>>();

        return data ?? new List<ProductAttributeViewModel>();
    }

    // =========================
    // PRODUCT VARIANTS
    // =========================

    public async Task<PagedResult<ProductVariantViewModel>> GetProductVariantsAsync(
     Guid attributeUid,
     string? search = null,
     bool? isActive = null,
     int page = 1,
     int pageSize = 10)
    {
        var url =
            $"api/product-attributes/{attributeUid}/variants?page={page}&pageSize={pageSize}";

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
            .GetFromJsonAsync<PagedResult<ProductVariantViewModel>>(url)
            ?? new PagedResult<ProductVariantViewModel>();
    }

    public async Task<ProductVariantViewModel?> GetProductVariantAsync(
        Guid attributeUid,
        Guid uid)
    {
        if (attributeUid == Guid.Empty || uid == Guid.Empty)
            return null;

        var response =
            await _http.GetAsync(
                $"api/product-attributes/{attributeUid}/variants/{uid}");

        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();

        return await response.Content
            .ReadFromJsonAsync<ProductVariantViewModel>();
    }

    public async Task<(bool Success, string? Error)> CreateProductVariantAsync(
        ProductVariantViewModel model)
    {
        if (!Guid.TryParse(model.AttributeUid, out var attributeUid))
        {
            return (false, "Atribut tidak ditemukan.");
        }

        var payload = new
        {
            model.Name,
            model.DefHeight,
            model.DefWidth,
            model.DefLength,
            model.Description,
            model.SortOrder,
            model.IsActive
        };

        var response =
            await _http.PostAsJsonAsync(
                $"api/product-attributes/{attributeUid}/variants/create",
                payload);

        return response.IsSuccessStatusCode
            ? (true, null)
            : (
                false,
                await response.Content.ReadAsStringAsync()
            );
    }

    public async Task<(bool Success, string? Error)> UpdateProductVariantAsync(
        ProductVariantViewModel model)
    {
        if (!Guid.TryParse(model.AttributeUid, out var attributeUid))
        {
            return (false, "Atribut tidak ditemukan.");
        }

        if (!Guid.TryParse(model.Uid, out var uid))
        {
            return (false, "Varian tidak ditemukan.");
        }

        var payload = new
        {
            // UID (identitas record)
            Uid = uid,
            model.Name,
            model.DefHeight,
            model.DefWidth,
            model.DefLength,
            model.Description,
            model.SortOrder,
            model.IsActive
        };

        var response =
            await _http.PostAsJsonAsync(
                $"api/product-attributes/{attributeUid}/variants/update",
                payload);

        return response.IsSuccessStatusCode
            ? (true, null)
            : (
                false,
                await response.Content.ReadAsStringAsync()
            );
    }

    public async Task<(bool Success, string? Error)> ReorderProductVariantsAsync(
    Guid attributeUid,
    List<object> items)
    {
        var response =
            await _http.PostAsJsonAsync(
                $"api/product-attributes/{attributeUid}/variants/reorder",
                items);

        return response.IsSuccessStatusCode
            ? (true, null)
            : (
                false,
                await response.Content.ReadAsStringAsync()
            );
    }

    public async Task<(bool Success, string? Error)> DeleteProductVariantAsync(
        Guid attributeUid,
        Guid uid)
    {
        var response =
            await _http.PostAsJsonAsync(
                $"api/product-attributes/{attributeUid}/variants/delete",
                new { Uid = uid });

        return response.IsSuccessStatusCode
            ? (true, null)
            : (
                false,
                await response.Content.ReadAsStringAsync()
            );
    }
}
