using ERP.Web.Models;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace ERP.Web.Services;

public partial class ApiClient
{
    //product
    public async Task<PagedResult<ProductViewModel>> GetProductsAsync(
    string? search = null,
    long? brandId = null,
    long? categoryId = null,
    long? subCategoryId = null,
    string? productType = null,
    long? unitId = null,
    bool? trackStock = null,
    bool? isActive = null,
    bool showVariant = false,
    int page = 1,
    int pageSize = 10)
    {
        var url =
            $"api/products?page={page}" +
            $"&pageSize={pageSize}" +
            $"&showVariant={showVariant.ToString().ToLower()}";
         
        if (!string.IsNullOrWhiteSpace(search))
        {
            url += $"&search={Uri.EscapeDataString(search)}";
        }
         
        if (brandId.HasValue)
        {
            url += $"&brandId={brandId.Value}";
        }
         
        if (categoryId.HasValue)
        {
            url += $"&categoryId={categoryId.Value}";
        }
         
        if (subCategoryId.HasValue)
        {
            url += $"&subCategoryId={subCategoryId.Value}";
        }
         
        if (!string.IsNullOrWhiteSpace(productType))
        {
            url +=  $"&productType={Uri.EscapeDataString(productType)}";
        }
         
        if (unitId.HasValue)
        {
            url += $"&unitId={unitId.Value}";
        }
         
        if (trackStock.HasValue)
        {
            url += $"&trackStock={trackStock.Value.ToString().ToLower()}";
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
                $"Product API Error: {response.StatusCode}\n{body}"
            );
        }

        return System.Text.Json.JsonSerializer
            .Deserialize<PagedResult<ProductViewModel>>(
                body,
                new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }
            )
            ?? new PagedResult<ProductViewModel>();
    }

    public async Task<ProductViewModel?> GetProductAsync(Guid uid)
    {
        if (uid == Guid.Empty)
            return null;

        var response = await _http.GetAsync($"api/products/{uid}");

        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ProductViewModel>();
    }

    public async Task<ProductViewModel?> GetVariantAsync(Guid uid)
    {
        return await GetProductAsync(uid);
    }

    public async Task<List<ProductViewModel>> GetVariantsAsync(Guid parentUid)
    {
        var response = await _http.GetAsync(
            $"api/products/variants/{parentUid}");

        if (response.StatusCode == HttpStatusCode.NotFound)
            return new List<ProductViewModel>();

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<List<ProductViewModel>>()
               ?? new List<ProductViewModel>();
    }

    public async Task<(bool Success, string? Image, string? Error)>
       UploadProductImageAsync(IFormFile file)
    {
        using var content =  new MultipartFormDataContent(); 
        using var stream = file.OpenReadStream(); 
        using var fileContent = new StreamContent(stream); 
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue( file.ContentType);

        content.Add(
            fileContent,
            "image",
            file.FileName);

        var response =
            await _http.PostAsync(
                "api/products/upload-image",
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
                .ReadFromJsonAsync<ProductImageUploadResponse>();

        return (
            true,
            result?.Image,
            null
        );
    }

    public async Task<(bool Success, string? Error)>
    CreateProductAsync(ProductViewModel model)
    {
        var payload = new
        {
            model.Sku,
            model.Barcode,
            model.InternalCode,
            model.ExternalCode1,
            model.ExternalCode2,
            model.ExternalCode3,
            model.Name,

            model.CategoryId,
            model.SubCategoryId,
            model.BrandId,

            model.ProductType,
            model.ParentId,
            model.HasVariants,
            model.UnitId,

            model.DimMode,
            model.DefLength,
            model.DefWidth,
            model.DefHeight,

            model.PriceMode,
            model.CostPerM3,
            model.SellPerM3,

            model.Description,

            model.CostPrice,
            model.SellPrice,
            model.MinStock,

            model.TrackStock,
            model.TrackSerial,
            model.WarrantyMonths,

            model.IsActive,

            model.Image
        };

        var response =
            await _http.PostAsJsonAsync(
                "api/products/create",
                payload
            );

        var responseBody =
            await response.Content.ReadAsStringAsync();

        Console.WriteLine(
            $"POST api/products/create => {(int)response.StatusCode} {response.StatusCode}"
        );

        Console.WriteLine(
            $"Response => {responseBody}"
        );

        if (!response.IsSuccessStatusCode)
        {
            return (
                false,
                $"HTTP {(int)response.StatusCode} - " +
                $"{response.ReasonPhrase}\n" +
                $"{responseBody}"
            );
        }

        return (true, null);
    }

    // UPDATE pakai POST.
    public async Task<(bool Success, string? Error)> UpdateProductAsync(ProductViewModel model)
    {
        if (!Guid.TryParse(model.Uid, out var uid))
        {
            return (false, "Produk tidak ditemukan.");
        }

        var payload = new
        {
            // UID (identitas record)
            Uid = uid,
            model.ProductNumber,
            model.Sku,
            model.Barcode,
            model.InternalCode,
            model.ExternalCode1,
            model.ExternalCode2,
            model.ExternalCode3,
            model.Name,
            model.CategoryId,
            model.SubCategoryId,
            model.BrandId,
            model.ProductType,
            model.HasVariants,
            model.UnitId,
            model.DimMode,
            model.DefLength,
            model.DefWidth,
            model.DefHeight,
            model.PriceMode,
            model.CostPerM3,
            model.SellPerM3,
            model.CostPrice,
            model.SellPrice,
            model.MinStock,
            model.TrackStock,
            model.TrackSerial,
            model.WarrantyMonths,
            model.IsActive,
            model.Image,
            model.Description
        };

        var response =
            await _http.PostAsJsonAsync(
                "api/products/update",
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
    public async Task<(bool Success, string? Error)> DeleteProductAsync(Guid uid)
    {
        var response = await _http.PostAsJsonAsync(
            "api/products/delete",
            new { Uid = uid });

        return response.IsSuccessStatusCode
            ? (true, null)
            : (false, await response.Content.ReadAsStringAsync());
    }

    public async Task<List<ProductViewModel>> GetProductsForExportAsync()
    {
        var response = await _http.GetAsync("api/products/export");

        if (!response.IsSuccessStatusCode)
            return new List<ProductViewModel>();

        var data = await response.Content.ReadFromJsonAsync<List<ProductViewModel>>();

        return data ?? new List<ProductViewModel>();
    }

    public async Task<List<DropdownItemViewModel>> GetCategoriesAsync()
    {
        var response = await _http.GetAsync("api/products/categories");

        if (!response.IsSuccessStatusCode)
        {
            return new List<DropdownItemViewModel>();
        }

        var data =
            await response.Content.ReadFromJsonAsync<List<DropdownItemViewModel>>();

        return data ?? new List<DropdownItemViewModel>();
    }

    public async Task<List<DropdownItemViewModel>> GetSubCategoriesAsync(long subCategory)
    {
        var response = await _http.GetAsync(
            $"api/products/subcategories/{subCategory}");

        if (!response.IsSuccessStatusCode)
        {
            return new List<DropdownItemViewModel>();
        }

        var data =
            await response.Content.ReadFromJsonAsync<List<DropdownItemViewModel>>();

        return data ?? new List<DropdownItemViewModel>();
    }

    //label-qr-product
    public async Task<ProductViewModel?> GetProductByUidAsync(string uid)
    {
        if (!Guid.TryParse(uid, out var parsedUid))
            return null;

        return await GetProductAsync(parsedUid);
    }

    //samakan harga varian
    public async Task<(bool Success, string? Error)> SyncPricesToVariantsAsync(
        Guid uid)
    {
        try
        {
            var response = await _http.PostAsync(
                $"api/products/{uid}/sync-prices-to-variants",
                content: null
            );

            if (response.IsSuccessStatusCode)
            {
                return (true, null);
            }

            var error = await response.Content.ReadAsStringAsync();

            return (
                false,
                string.IsNullOrWhiteSpace(error)
                    ? "Gagal menyamakan harga varian."
                    : error
            );
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }
}
