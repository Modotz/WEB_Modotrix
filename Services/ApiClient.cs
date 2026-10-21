using ERP.Web.Models;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace ERP.Web.Services;

public class ApiClient
{
    private readonly HttpClient _http;

    public ApiClient(HttpClient http)
    {
        _http = http;
    }
     
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

    public async Task<RoleViewModel?> GetRoleAsync(long id)
    {
        var response =
            await _http.GetAsync($"api/roles/{id}");

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
        var payload = new
        {
            Id = model.Id!.Value,
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
        DeleteRoleAsync(long id)
    {
        var response =
            await _http.PostAsJsonAsync(
                "api/roles/delete",
                new { Id = id });

        return response.IsSuccessStatusCode
            ? (true, null)
            : (
                false,
                await response.Content.ReadAsStringAsync()
            );
    }

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

    public async Task<UserViewModel?> GetUserAsync(long id)
    {
        var response =
            await _http.GetAsync($"api/users/{id}");

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
        var payload = new
        {
            Id = model.Id!.Value,
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

    private class PhotoUploadResponse
    {
        public string? Photo { get; set; }
    }

    public async Task<(bool Success, string? Error)>
        DeleteUserAsync(long id)
    {
        var response =
            await _http.PostAsJsonAsync(
                "api/users/delete",
                new { Id = id });

        return response.IsSuccessStatusCode
            ? (true, null)
            : (
                false,
                await response.Content.ReadAsStringAsync()
            );
    }

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

    public async Task<MenuViewModel?> GetMenuAsync(long id)
    {
        var response = await _http.GetAsync($"api/menus/{id}");

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
        var payload = new
        {
            Id = model.Id!.Value,
            model.ParentId,
            model.Title,
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
    public async Task<(bool Success, string? Error)> DeleteMenuAsync(long id)
    {
        var response = await _http.PostAsJsonAsync(
            "api/menus/delete",
            new { Id = id });

        return response.IsSuccessStatusCode
            ? (true, null)
            : (false, await response.Content.ReadAsStringAsync());
    }

   

    //Shipping Areas
    public async Task<List<ShippingAreaViewModel>> GetShippingAreasAsync(string? search = null)
    {
        var url = "api/shipping-areas";

        if (!string.IsNullOrWhiteSpace(search))
            url += $"?search={Uri.EscapeDataString(search)}";

        return await _http.GetFromJsonAsync<List<ShippingAreaViewModel>>(url)
               ?? [];
    }

    public async Task<PagedResult<ShippingAreaViewModel>> GetShippingAreasAsync(
    string? search = null,
    bool? isActive = null,
    int page = 1,
    int pageSize = 10)
    {
        var url = $"api/shipping-areas?page={page}&pageSize={pageSize}";

        if (!string.IsNullOrWhiteSpace(search))
        {
            url += $"&search={Uri.EscapeDataString(search)}";
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
                $"Shipping Area API Error: {response.StatusCode}\n{body}"
            );
        }

        return System.Text.Json.JsonSerializer.Deserialize<
            PagedResult<ShippingAreaViewModel>
        >(
            body,
            new System.Text.Json.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }
        ) ?? new PagedResult<ShippingAreaViewModel>();
    }


    public async Task<ShippingAreaViewModel?> GetShippingAreaAsync(long id)
    {
        var response = await _http.GetAsync($"api/shipping-areas/{id}");

        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ShippingAreaViewModel>();
    }

    public async Task<(bool Success, string? Error)> CreateShippingAreaAsync(ShippingAreaViewModel model)
    {
        var payload = new
        {
            model.Id,
            model.Uid,
            model.Code,
            model.Name,
            model.Price,
            model.Description,
            model.IsActive
        };

        var response = await _http.PostAsJsonAsync("api/shipping-areas/create", payload);

        return response.IsSuccessStatusCode
            ? (true, null)
            : (false, await response.Content.ReadAsStringAsync());
    }

    // UPDATE pakai POST.
    public async Task<(bool Success, string? Error)> UpdateShippingAreaAsync(ShippingAreaViewModel model)
    {
        var payload = new
        {
            Id = model.Id!.Value,
            model.Uid,
            model.Code,
            model.Name,
            model.Price,
            model.Description,
            model.IsActive
        };

        var response =
            await _http.PostAsJsonAsync(
                "api/shipping-areas/update",
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
    public async Task<(bool Success, string? Error)> DeleteShippingAreaAsync(long id)
    {
        var response = await _http.PostAsJsonAsync(
            "api/shipping-areas/delete",
            new { Id = id });

        return response.IsSuccessStatusCode
            ? (true, null)
            : (false, await response.Content.ReadAsStringAsync());
    }

    public async Task<List<ShippingAreaViewModel>> GetShippingAreasForExportAsync()
    {
        var response = await _http.GetAsync("api/shipping-areas/export");

        var responseText = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Gagal mengambil data Area Pengiriman untuk export. " +
                $"Status: {response.StatusCode}. " +
                $"URL: {response.RequestMessage?.RequestUri}. " +
                $"Response: {responseText}");
        }

        return await response.Content
            .ReadFromJsonAsync<List<ShippingAreaViewModel>>()
            ?? new List<ShippingAreaViewModel>();
    }

    //Supplier
    //public async Task<List<DropdownItemViewModel>> GetSuppliersForDropdownAsync()
    //{
    //    var response =
    //        await _http.GetAsync(
    //            "api/suppliers?page=1&pageSize=100000");

    //    if (!response.IsSuccessStatusCode)
    //    {
    //        return new List<DropdownItemViewModel>();
    //    }

    //    var result =
    //        await response.Content
    //            .ReadFromJsonAsync<PagedResult<BrandViewModel>>();

    //    if (result?.Data == null)
    //    {
    //        return new List<DropdownItemViewModel>();
    //    }

    //    return result.Data
    //        .Where(x => x.IsActive && x.Id.HasValue)
    //        .Select(x => new DropdownItemViewModel
    //        {
    //            Id = x.Id.Value,
    //            Name = x.Name
    //        })
    //        .ToList();
    //}

    public async Task<List<DropdownItemViewModel>> GetSuppliersForDropdownAsync()
    {
        var response = await _http.GetAsync(
            "api/suppliers?page=1&pageSize=100000");

        var body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Gagal mengambil Supplier. " +
                $"Status: {response.StatusCode}. " +
                $"Response: {body}");
        }

        var result = System.Text.Json.JsonSerializer.Deserialize<
            PagedResult<SupplierViewModel>
        >(
            body,
            new System.Text.Json.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

        return result?.Data?
            .Where(x => x.IsActive && x.Id.HasValue)
            .Select(x => new DropdownItemViewModel
            {
                Id = x.Id!.Value,
                Name = x.Name
            })
            .ToList()
            ?? new List<DropdownItemViewModel>();
    }

    public async Task<PagedResult<SupplierViewModel>> GetSuppliersAsync(
    string? search = null,
    bool? isActive = null,
    int page = 1,
    int pageSize = 10)
    {
        var url = $"api/suppliers?page={page}&pageSize={pageSize}";

        if (!string.IsNullOrWhiteSpace(search))
        {
            url += $"&search={Uri.EscapeDataString(search)}";
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
                $"Supplier API Error: {response.StatusCode}\n{body}"
            );
        }

        return System.Text.Json.JsonSerializer.Deserialize<
            PagedResult<SupplierViewModel>
        >(
            body,
            new System.Text.Json.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }
        ) ?? new PagedResult<SupplierViewModel>();
    }


    public async Task<SupplierViewModel?> GetSupplierAsync(long id)
    {
        var response = await _http.GetAsync($"api/suppliers/{id}");

        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<SupplierViewModel>();
    }

    public async Task<(bool Success, string? Error)> CreateSupplierAsync(SupplierViewModel model)
    {
        var payload = new
        {
            model.Id,
            model.Uid,
            model.Code,
            model.Name,
            model.ContactPerson,
            model.Phone,
            model.Email,
            model.Npwp,
            model.Address,
            model.IsActive
        };

        var response = await _http.PostAsJsonAsync("api/suppliers/create", payload);

        return response.IsSuccessStatusCode
            ? (true, null)
            : (false, await response.Content.ReadAsStringAsync());
    }

    // UPDATE pakai POST.
    public async Task<(bool Success, string? Error)> UpdateSupplierAsync(SupplierViewModel model)
    {
        var payload = new
        {
            Id = model.Id!.Value,
            model.Uid,
            model.Code,
            model.Name,
            model.ContactPerson,
            model.Phone,
            model.Email,
            model.Npwp,
            model.Address,
            model.IsActive
        };

        var response =
            await _http.PostAsJsonAsync(
                "api/suppliers/update",
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
    public async Task<(bool Success, string? Error)> DeleteSupplierAsync(long id)
    {
        var response = await _http.PostAsJsonAsync(
            "api/suppliers/delete",
            new { Id = id });

        return response.IsSuccessStatusCode
            ? (true, null)
            : (false, await response.Content.ReadAsStringAsync());
    }

    public async Task<List<SupplierViewModel>> GetSuppliersForExportAsync()
    {
        var response = await _http.GetAsync("api/suppliers/export");

        if (!response.IsSuccessStatusCode)
            return new List<SupplierViewModel>();

        var data = await response.Content.ReadFromJsonAsync<List<SupplierViewModel>>();

        return data ?? new List<SupplierViewModel>();
    }

    // msfield
    public async Task<PagedResult<MsfieldViewModel>> GetMsfieldsAsync(
    string? search = null,
    bool? isActive = null,
    int page = 1,
    int pageSize = 10)
    {
        // Validasi halaman dan ukuran halaman
        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? 10 : pageSize;

        // Ambil seluruh data dari API
        var response = await _http.GetAsync("api/msfields");

        if (!response.IsSuccessStatusCode)
        {
            return new PagedResult<MsfieldViewModel>
            {
                Data = new List<MsfieldViewModel>(),
                Page = page,
                PageSize = pageSize,
                TotalCount = 0,
                TotalData = 0
            };
        }

        var allData =
            await response.Content.ReadFromJsonAsync<List<MsfieldViewModel>>()
            ?? new List<MsfieldViewModel>();

        // Filter pencarian
        var filteredData = allData.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();

            filteredData = filteredData.Where(x =>
                (x.FieldKey ?? "").Contains(search, StringComparison.OrdinalIgnoreCase) ||
                (x.FieldText ?? "").Contains(search, StringComparison.OrdinalIgnoreCase) ||
                (x.FiledValue ?? "").Contains(search, StringComparison.OrdinalIgnoreCase) ||
                (x.Description ?? "").Contains(search, StringComparison.OrdinalIgnoreCase)
            );
        }

        // Filter status
        if (isActive.HasValue)
        {
            filteredData = filteredData.Where(x =>
                x.IsActive == isActive.Value
            );
        }

        var totalCount = filteredData.Count();

        // Hitung jumlah halaman
        var totalPage = (int)Math.Ceiling(
            totalCount / (double)pageSize
        );

        if (totalPage > 0 && page > totalPage)
        {
            page = totalPage;
        }

        var pagedData = filteredData
            .OrderBy(x => x.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PagedResult<MsfieldViewModel>
        {
            Data = pagedData,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalData = totalCount
        };
    }


    // GET MSFIELD BY ID
    public async Task<MsfieldViewModel?> GetMsfieldsAsync(long id)
    {
        var response =
            await _http.GetAsync(
                $"api/msfields/{id}");

        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();

        var x =
            await response.Content
                .ReadFromJsonAsync<MsfieldViewModel>();

        if (x == null)
            return null;

        return new MsfieldViewModel 
        {
            Id = x.Id,
            Uid = x.Uid,
            FieldKey = x.FieldKey,
            FieldText = x.FieldText,
            FiledValue = x.FiledValue,
            Description = x.Description,
            IsActive = x.IsActive,
            CreatedAt = x.CreatedAt,
            UpdatedAt = x.UpdatedAt,
            DeletedAt = x.DeletedAt,
            UserId = x.UserId
        };
    }


    // CREATE MSFIELD
    public async Task<(bool Success, string? Error)>
    CreateFieldAsync(MsfieldViewModel model)
    {
        var payload = new
        {
            model.FieldKey,
            model.FieldText,
            model.FiledValue,
            model.Description,
            IsActive = model.IsActive,
            model.UserId
        };

        var response =
            await _http.PostAsJsonAsync(
                "api/msfields/Create",
                payload);

        return response.IsSuccessStatusCode
            ? (true, null)
            : (
                false,
                await response.Content.ReadAsStringAsync()
            );
    }


    // UPDATE MSFIELD
    public async Task<(bool Success, string? Error)>
     UpdateFieldAsync(MsfieldViewModel model)
    {
        var payload = new
        {
            Id = model.Id!.Value,
            model.FieldKey,
            model.FieldText,
            model.FiledValue,
            model.Description,

            // Kirim bool, bukan byte
            IsActive = model.IsActive,

            model.UserId
        };

        var response =
            await _http.PostAsJsonAsync(
                "api/msfields/Update",
                payload);

        return response.IsSuccessStatusCode
            ? (true, null)
            : (
                false,
                await response.Content.ReadAsStringAsync()
            );
    }


    // DELETE MSFIELD
    public async Task<(bool Success, string? Error)>
        DeleteFieldAsync(long id)
    {
        var response =
            await _http.PostAsJsonAsync(
                "api/msfields/Delete",
                new
                {
                    Id = id
                });

        return response.IsSuccessStatusCode
            ? (true, null)
            : (
                false,
                await response.Content.ReadAsStringAsync()
            );
    }

    // =========================================================
    // EXPORT MSFIELD
    // =========================================================

    // GET ALL MSFIELD FOR EXPORT / PRINT
    public async Task<List<MsfieldViewModel>> GetFieldsForExportAsync()
    {
        var response = await _http.GetAsync("api/msfields/export");

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();

            throw new HttpRequestException(
                $"Gagal mengambil data Msfield. " +
                $"Status: {(int)response.StatusCode} " +
                $"{response.StatusCode}. " +
                $"Detail: {error}"
            );
        }

        var data = await response.Content
            .ReadFromJsonAsync<List<MsfieldViewModel>>();

        return data ?? new List<MsfieldViewModel>();
    }

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


    public async Task<UnitViewModel?> GetUnitAsync(long id)
    {
        var response = await _http.GetAsync($"api/units/{id}");

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
        var payload = new
        {
            Id = model.Id,
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
    public async Task<(bool Success, string? Error)> DeleteUnitAsync(long id)
    {
        var response = await _http.PostAsJsonAsync(
            "api/units/delete",
            new { Id = id });

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

    public async Task<BrandViewModel?> GetBrandAsync(long id)
    {
        var response =
            await _http.GetAsync(
                $"api/brands/{id}");

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
        if (!model.Id.HasValue)
        {
            return (
                false,
                "ID brand tidak ditemukan."
            );
        }


        using var content =
            new MultipartFormDataContent();


        // ID

        content.Add(
            new StringContent(
                model.Id.Value.ToString()),
            "Id");


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
        DeleteBrandAsync(long id)
    {
        var response =
            await _http.PostAsJsonAsync(
                "api/brands/delete",
                new
                {
                    Id = id
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
    // RESPONSE UPLOAD IMAGE BRAND
    // =========================================================

    private class BrandImageUploadResponse
    {
        public string? Image { get; set; }
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

    //warehouse
    public async Task<List<DropdownItemViewModel>> GetWarehousesForDropdownAsync()
    {
        var response =
            await _http.GetAsync(
                "api/warehouses?page=1&pageSize=100000");

        if (!response.IsSuccessStatusCode)
        {
            return new List<DropdownItemViewModel>();
        }

        var result =
            await response.Content
                .ReadFromJsonAsync<PagedResult<WarehouseViewModel>>();

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
    private class WarehouseListResponse
    {
        public bool Success { get; set; }

        public string? Message { get; set; }

        public List<WarehouseViewModel>? Data { get; set; }
    }

    private class WarehouseSingleResponse
    {
        public bool Success { get; set; }

        public string? Message { get; set; }

        public WarehouseViewModel? Data { get; set; }
    }

    public async Task<(bool Success, List<WarehouseViewModel> Data, string? Error)>
        GetWarehousesAsync(
            string? search = null,
            bool? isDefault = null,
            bool? isActive = null)
    {
        var query = new List<string>();

        // SEARCH
        if (!string.IsNullOrWhiteSpace(search))
        {
            query.Add(
                $"search={Uri.EscapeDataString(search)}");
        }

        // IS DEFAULT
        if (isDefault.HasValue)
        {
            query.Add(
                $"isDefault={isDefault.Value.ToString().ToLowerInvariant()}");
        }

        // IS ACTIVE
        if (isActive.HasValue)
        {
            query.Add(
                $"isActive={isActive.Value.ToString().ToLowerInvariant()}");
        }

        var url = "api/warehouses";

        if (query.Count > 0)
        {
            url += "?" + string.Join("&", query);
        }

        try
        {
            var response = await _http.GetAsync(url);
             
            if (!response.IsSuccessStatusCode)
            {
                var errorMessage =
                    await GetWarehouseApiErrorAsync(response);

                return (
                    false,
                    new List<WarehouseViewModel>(),
                    errorMessage
                );
            }
             
            var result =
                await response.Content
                    .ReadFromJsonAsync<WarehouseListResponse>(
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });
             
            if (result == null)
            {
                return (
                    false,
                    new List<WarehouseViewModel>(),
                    "Response API gudang kosong."
                );
            }
             
            if (!result.Success)
            {
                return (
                    false,
                    new List<WarehouseViewModel>(),
                    result.Message ?? "Gagal mengambil data gudang."
                );
            }
             
            return (
                true,
                result.Data ?? new List<WarehouseViewModel>(),
                null
            );
        }
        catch (Exception ex)
        {
            return (
                false,
                new List<WarehouseViewModel>(),
                $"Gagal mengambil data gudang: {ex.Message}"
            );
        }
    }


    // ==========================================
    // GET WAREHOUSE BY ID
    // ==========================================
    public async Task<WarehouseViewModel?> GetWarehouseAsync(long id)
    {
        try
        {
            var response =
                await _http.GetAsync(
                    $"api/warehouses/{id}");
             
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }
             
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }
             
            var result =
                await response.Content
                    .ReadFromJsonAsync<WarehouseSingleResponse>(
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

            if (result == null)
            {
                return null;
            }

            if (!result.Success)
            {
                return null;
            }

            return result.Data;
        }
        catch
        {
            return null;
        }
    }


    // ==========================================
    // CREATE WAREHOUSE
    // ==========================================
    public async Task<(bool Success, string? Error)>
        CreateWarehouseAsync(WarehouseViewModel model)
    {
        var payload = new
        {
            model.Code,
            model.Name,
            model.Address,
            model.Phone,
            model.PicName,
            model.IsDefault,
            model.IsActive
        };

        try
        {
            var response =
                await _http.PostAsJsonAsync(
                    "api/warehouses/Create",
                    payload);

            if (response.IsSuccessStatusCode)
            {
                return (true, null);
            }

            var errorMessage =
                await GetWarehouseApiErrorAsync(response);

            return (
                false,
                errorMessage
            );
        }
        catch (Exception ex)
        {
            return (
                false,
                $"Gagal menghubungi server: {ex.Message}"
            );
        }
    }


    // ==========================================
    // UPDATE WAREHOUSE
    // ==========================================
    public async Task<(bool Success, string? Error)>
        UpdateWarehouseAsync(WarehouseViewModel model)
    {
        var payload = new
        {
            Id = model.Id,
            model.Code,
            model.Name,
            model.Address,
            model.Phone,
            model.PicName,
            model.IsDefault,
            model.IsActive
        };

        try
        {
            var response =
                await _http.PostAsJsonAsync(
                    "api/warehouses/Update",
                    payload);

            if (response.IsSuccessStatusCode)
            {
                return (true, null);
            }

            var errorMessage =
                await GetWarehouseApiErrorAsync(response);

            return (
                false,
                errorMessage
            );
        }
        catch (Exception ex)
        {
            return (
                false,
                $"Gagal menghubungi server: {ex.Message}"
            );
        }
    }


    // ==========================================
    // DELETE WAREHOUSE
    // ==========================================
    public async Task<(bool Success, string? Error)>
        DeleteWarehouseAsync(long id)
    {
        var payload = new
        {
            Id = id
        };

        try
        {
            var response =
                await _http.PostAsJsonAsync(
                    "api/warehouses/Delete",
                    payload);

            if (response.IsSuccessStatusCode)
            {
                return (true, null);
            }

            var errorMessage =
                await GetWarehouseApiErrorAsync(response);

            return (
                false,
                errorMessage
            );
        }
        catch (Exception ex)
        {
            return (
                false,
                $"Gagal menghubungi server: {ex.Message}"
            );
        }
    }


    // ==========================================
    // GET WAREHOUSES FOR EXPORT / PRINT
    // ==========================================
    public async Task<List<WarehouseViewModel>>
        GetWarehousesForExportAsync()
    {
        try
        {
            var response =
                await _http.GetAsync(
                    "api/warehouses/export");

            if (!response.IsSuccessStatusCode)
            {
                return new List<WarehouseViewModel>();
            }

            var result =
                await response.Content
                    .ReadFromJsonAsync<WarehouseListResponse>(
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

            if (result == null || !result.Success)
            {
                return new List<WarehouseViewModel>();
            }

            return result.Data
                   ?? new List<WarehouseViewModel>();
        }
        catch
        {
            return new List<WarehouseViewModel>();
        }
    }


    // ==========================================
    // GET ERROR MESSAGE FROM WAREHOUSE API
    // ==========================================
    private async Task<string>
        GetWarehouseApiErrorAsync(
            HttpResponseMessage response)
    {
        try
        {
            var json =
                await response.Content
                    .ReadAsStringAsync();

            if (string.IsNullOrWhiteSpace(json))
            {
                return
                    $"Server mengembalikan status " +
                    $"{(int)response.StatusCode} " +
                    $"({response.StatusCode}).";
            }

            using var document = JsonDocument.Parse(json);
              
            if (document.RootElement.TryGetProperty(
                "message",
                out var message))
            {
                var messageValue =
                    message.GetString();

                if (!string.IsNullOrWhiteSpace(
                    messageValue))
                {
                    return messageValue;
                }
            } 

            if (document.RootElement.TryGetProperty(
                "errors",
                out var errors))
            {
                var messages =
                    new List<string>();

                foreach (
                    var property
                    in errors.EnumerateObject())
                {
                    foreach (
                        var error
                        in property.Value.EnumerateArray())
                    {
                        var errorMessage =
                            error.GetString();

                        if (!string.IsNullOrWhiteSpace(
                            errorMessage))
                        {
                            messages.Add(errorMessage);
                        }
                    }
                }

                if (messages.Count > 0)
                {
                    return string.Join(
                        " ",
                        messages);
                }
            }

            return json;
        }
        catch
        {
            return
                $"Server mengembalikan status " +
                $"{(int)response.StatusCode} " +
                $"({response.StatusCode}).";
        }
    }

    //GROUP CUSTOMER
    public async Task<PagedResult<CustomerGroupsViewModel>>
        GetCustomerGroupsAsync(
            string? search = null,
            bool? isActive = null,
            int page = 1,
            int pageSize = 10)
    {
        var url =
            $"api/customergroups?page={page}&pageSize={pageSize}";

        if (!string.IsNullOrWhiteSpace(search))
        {
            url += $"&search={Uri.EscapeDataString(search)}";
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
                $"Customer Group API Error: {response.StatusCode}\n{body}"
            );
        }

        return System.Text.Json.JsonSerializer.Deserialize<
            PagedResult<CustomerGroupsViewModel>
        >(
            body,
            new System.Text.Json.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }
        ) ?? new PagedResult<CustomerGroupsViewModel>();
    }


    // =========================================================
    // GET CUSTOMER GROUPS FOR EXPORT / PRINT
    // Mengikuti filter search dan isActive, tanpa pagination
    // =========================================================

    public async Task<List<CustomerGroupsViewModel>>
        GetCustomerGroupsForExportAsync(
            string? search = null,
            bool? isActive = null)
    {
        var url = "api/customergroups/export";
        var query = new List<string>();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query.Add($"search={Uri.EscapeDataString(search.Trim())}");
        }

        if (isActive.HasValue)
        {
            query.Add(
                $"isActive={isActive.Value.ToString().ToLowerInvariant()}");
        }

        if (query.Count > 0)
        {
            url += "?" + string.Join("&", query);
        }

        var response = await _http.GetAsync(url);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();

            throw new HttpRequestException(
                $"Gagal mengambil data Customer Group untuk export/print. " +
                $"Status: {(int)response.StatusCode} {response.StatusCode}. " +
                $"Detail: {error}");
        }

        var data = await response.Content
            .ReadFromJsonAsync<List<CustomerGroupsViewModel>>(
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

        return data ?? new List<CustomerGroupsViewModel>();
    }


    // =========================================================
    // GET CUSTOMER GROUP BY ID
    // =========================================================

    public async Task<CustomerGroupsViewModel?>
        GetCustomerGroupAsync(long id)
    {
        var response =
            await _http.GetAsync(
                $"api/customergroups/{id}");

        if (response.StatusCode ==
            HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        var result =
            await response.Content
                .ReadFromJsonAsync<CustomerGroupResponse>();

        return result?.Data;
    }


    // =========================================================
    // CREATE CUSTOMER GROUP
    // =========================================================

    public async Task<(bool Success, string? Error)>
        CreateCustomerGroupAsync(
            CustomerGroupsViewModel model)
    {
        var payload = new
        {
            model.Code,
            model.Name,
            model.DiscountPercent,
            model.Description,
            model.IsActive,
            model.PriceListId
        };

        var response =
            await _http.PostAsJsonAsync(
                "api/customergroups/create",
                payload);

        if (response.IsSuccessStatusCode)
        {
            return (true, null);
        }

        return (
            false,
            await response.Content.ReadAsStringAsync()
        );
    }


    // =========================================================
    // UPDATE CUSTOMER GROUP
    // =========================================================

    public async Task<(bool Success, string? Error)>
        UpdateCustomerGroupAsync(
            CustomerGroupsViewModel model)
    {
        var payload = new
        {
            Id = model.Id,
            model.Code,
            model.Name,
            model.DiscountPercent,
            model.Description,
            model.IsActive,
            model.PriceListId
        };

        var response =
            await _http.PostAsJsonAsync(
                "api/customergroups/update",
                payload);

        if (response.IsSuccessStatusCode)
        {
            return (true, null);
        }

        return (
            false,
            await response.Content.ReadAsStringAsync()
        );
    }


    // =========================================================
    // DELETE CUSTOMER GROUP
    // =========================================================

    public async Task<(bool Success, string? Error)>
        DeleteCustomerGroupAsync(
            long id)
    {
        var response =
            await _http.PostAsJsonAsync(
                "api/customergroups/delete",
                new
                {
                    Id = id
                });

        if (response.IsSuccessStatusCode)
        {
            return (true, null);
        }

        return (
            false,
            await response.Content.ReadAsStringAsync()
        );
    }


    private class CustomerGroupResponse
    {
        public bool Success { get; set; }

        public string? Message { get; set; }

        public CustomerGroupsViewModel? Data { get; set; }
    }

    // =========================================================
    // DROPDOWN CUSTOMER
    // =========================================================
    public async Task<List<DropdownItemViewModel>> GetCustomerGroupsForCustomerAsync()
    {
        var response = await _http.GetAsync(
            "api/customers/customer-groups");

        if (!response.IsSuccessStatusCode)
        {
            return new List<DropdownItemViewModel>();
        }

        var data =
            await response.Content
                .ReadFromJsonAsync<List<DropdownItemViewModel>>();

        return data ?? new List<DropdownItemViewModel>();
    }


    // SALES FORCE - dropdown untuk customer
    public async Task<List<DropdownItemViewModel>> GetSalesForcesForCustomerAsync()
    {
        var response = await _http.GetAsync(
            "api/customers/sales-forces");

        if (!response.IsSuccessStatusCode)
        {
            return new List<DropdownItemViewModel>();
        }

        var data =
            await response.Content
                .ReadFromJsonAsync<List<DropdownItemViewModel>>();

        return data ?? new List<DropdownItemViewModel>();
    }


    // PRICE LIST -dropdown untuk customer
    public async Task<List<DropdownItemViewModel>> GetPriceListsForCustomerAsync()
    {
        var response = await _http.GetAsync(
            "api/customers/price-lists");

        if (!response.IsSuccessStatusCode)
        {
            return new List<DropdownItemViewModel>();
        }

        var data =
            await response.Content
                .ReadFromJsonAsync<List<DropdownItemViewModel>>();

        return data ?? new List<DropdownItemViewModel>();
    }

    //shipping area - dropdown untuk customer
    public async Task<List<DropdownItemViewModel>> GetShippingAreasForCustomerAsync()
    {
        var response = await _http.GetAsync(
            "api/customers/shipping-areas");

        if (!response.IsSuccessStatusCode)
        {
            return new List<DropdownItemViewModel>();
        }

        var data =
            await response.Content
                .ReadFromJsonAsync<List<DropdownItemViewModel>>();

        return data ?? new List<DropdownItemViewModel>();
    }

    //customer
    public async Task<PagedResult<CustomerViewModel>> GetCustomersAsync(
        string? search = null,
        bool? isActive = null,
        int page = 1,
        int pageSize = 10)
    {
        var url =
            $"api/customers?page={page}&pageSize={pageSize}";

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

        var response =
            await _http.GetAsync(url);

        if (!response.IsSuccessStatusCode)
        {
            return new PagedResult<CustomerViewModel>
            {
                Data = new List<CustomerViewModel>(),
                Page = page,
                PageSize = pageSize,
                TotalCount = 0,
                TotalData = 0
            };
        }

        return await response.Content
            .ReadFromJsonAsync<PagedResult<CustomerViewModel>>()
            ?? new PagedResult<CustomerViewModel>
            {
                Data = new List<CustomerViewModel>(),
                Page = page,
                PageSize = pageSize,
                TotalCount = 0,
                TotalData = 0
            };
    }

    // =========================================================
    // CREATE CUSTOMERS
    // =========================================================
    public async Task<CustomerViewModel?> CreateCustomerAsync(
        CustomerViewModel model)
    {
        var response = await _http.PostAsJsonAsync(
            "api/customers",
            model);

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var result =
            await response.Content
                .ReadFromJsonAsync<CustomerCreateResponse>();

        return result?.Data;
    }

    private class CustomerCreateResponse
    {
        public string? Message { get; set; }

        public CustomerViewModel? Data { get; set; }
    }

    // =========================================================
    // UPDATE CUSTOMERS
    // =========================================================

    public async Task<bool> UpdateCustomerAsync(
        CustomerViewModel model)
    {
        var response =
            await _http.PostAsJsonAsync(
                "api/customers/update",
                model);

        return response.IsSuccessStatusCode;
    }

    // =========================================================
    // ACTIVATE CUSTOMER
    // =========================================================
    public async Task<bool> ActivateCustomerAsync(long id)
    {
        var result =
            await GetCustomersAsync(
                page: 1,
                pageSize: 100);

        var customer =
            result.Data.FirstOrDefault(
                x => x.Id == id);

        if (customer == null)
        {
            return false;
        }

        customer.IsActive = true;

        var response =
            await _http.PostAsJsonAsync(
                "api/customers/update",
                customer);

        return response.IsSuccessStatusCode;
    }

    // =========================================================
    // DELETE CUSTOMERS
    // =========================================================

    public async Task<bool> DeleteCustomerAsync(
        long id)
    {
        var response =
            await _http.PostAsJsonAsync(
                "api/customers/delete",
                new
                {
                    id
                });

        return response.IsSuccessStatusCode;
    }

    // =========================================================
    // UPLOAD IMAGE CUSTOMERS
    // =========================================================

    public async Task<string?> UploadCustomerImageAsync(
        long customerId,
        IFormFile file)
    {
        using var content =
            new MultipartFormDataContent();

        content.Add(
            new StringContent(
                customerId.ToString()),
            "customerId");

        using var stream =
            file.OpenReadStream();

        using var fileContent =
            new StreamContent(stream);

        fileContent.Headers.ContentType =
            new System.Net.Http.Headers.MediaTypeHeaderValue(
                string.IsNullOrWhiteSpace(file.ContentType)
                    ? "application/octet-stream"
                    : file.ContentType);

        content.Add(
            fileContent,
            "file",
            file.FileName);

        var response =
            await _http.PostAsync(
                "api/customers/upload-image",
                content);

        if (!response.IsSuccessStatusCode)
        {
            var error =
                await response.Content
                    .ReadAsStringAsync();

            throw new Exception(
                $"Upload foto gagal. Status: {(int)response.StatusCode}. Response: {error}");
        }

        var result =
            await response.Content
                .ReadFromJsonAsync<CustomerImageUploadResponse>();

        return result?.Image;
    }

    // =========================================================
    // RESPONSE UPLOAD IMAGE CUSTOMER
    // =========================================================

    private class CustomerImageUploadResponse
    {
        public string? Image { get; set; }
    }

    // SHIPPING ADDRESS

    // GET SHIPPING ADDRESSES BY CUSTOMER
    public async Task<List<ShippingAddressViewModel>>
        GetShippingAddressesByCustomerAsync(long customerId)
    {
        var response = await _http.GetAsync(
            $"api/shipping-addresses/customer/{customerId}");

        if (!response.IsSuccessStatusCode)
        {
            return new List<ShippingAddressViewModel>();
        }

        var data =
            await response.Content
                .ReadFromJsonAsync<List<ShippingAddressViewModel>>();

        return data
            ?? new List<ShippingAddressViewModel>();
    }

    public async Task<bool> CreateShippingAddressAsync(
    long customerId,
    string name,
    string address,
    long? shippingAreaId)
    {
        var response = await _http.PostAsJsonAsync(
            "api/shipping-addresses",
            new
            {
                customerId,
                name,
                address,
                shippingAreaId
            });

        return response.IsSuccessStatusCode;
    }


    public async Task<bool> UpdateShippingAddressAsync(
        long id,
        long customerId,
        string name,
        string address,
        long? shippingAreaId)
    {
        var response = await _http.PostAsJsonAsync(
            "api/shipping-addresses/update",
            new
            {
                id,
                customerId,
                name,
                address,
                shippingAreaId
            });

        return response.IsSuccessStatusCode;
    }


    public async Task<bool> DeleteShippingAddressAsync(long id)
    {
        var response = await _http.PostAsJsonAsync(
            "api/shipping-addresses/delete",
            new
            {
                id
            });

        return response.IsSuccessStatusCode;
    }


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
        long id)
    {
        var response =
            await _http.GetAsync(
                $"api/productcategories/{id}");

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
        var payload = new
        {
            Id = model.Id!.Value,
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
        long id)
    {
        var response =
            await _http.PostAsJsonAsync(
                "api/productcategories/delete",
                new { Id = id });

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

    public async Task<ProductViewModel?> GetProductAsync(long id)
    {
        var response = await _http.GetAsync($"api/products/{id}");

        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ProductViewModel>();
    }

    public async Task<ProductViewModel?> GetVariantAsync(long id)
    {
        var response = await _http.GetAsync($"api/products/{id}");

        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<ProductViewModel>();
    }

    public async Task<List<ProductViewModel>> GetVariantsAsync(long parentId)
    {
        var response = await _http.GetAsync(
            $"api/products/variants/{parentId}");

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

    private class ProductImageUploadResponse
    {
        public string? Image { get; set; }
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
        var payload = new
        {
            Id = model.Id,
            model.Uid,
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
    public async Task<(bool Success, string? Error)> DeleteProductAsync(long id)
    {
        var response = await _http.PostAsJsonAsync(
            "api/products/delete",
            new { Id = id });

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

    //label-qr-product
    public async Task<ProductViewModel?> GetProductByUidAsync(string uid)
    {
        var response = await _http.GetAsync(
            $"api/products/uid/{Uri.EscapeDataString(uid)}"
        );

        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();

        return await response.Content
            .ReadFromJsonAsync<ProductViewModel>();
    }

    //samakan harga varian
    public async Task<(bool Success, string? Error)> SyncPricesToVariantsAsync(
        long id)
    {
        try
        {
            var response = await _http.PostAsync(
                $"api/products/{id}/sync-prices-to-variants",
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
        long id)
    {
        var response =
            await _http.GetAsync(

             $"api/salesforces/{id}");

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
            Id = model.Id!.Value,
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


    public async Task<(bool Success, string? Error)> DeleteSalesForceAsync(long id)
    {
        var payload = new
        {
            Id = id
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

    // price-list 
    public async Task<PagedResult<PriceListViewModel>> GetPriceListsAsync(
        string? search = null,
        bool? isActive = null,
        int page = 1,
        int pageSize = 10)
    {
        var url =
            $"api/pricelists?page={page}&pageSize={pageSize}";

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
            .GetFromJsonAsync<PagedResult<PriceListViewModel>>(url)
            ?? new PagedResult<PriceListViewModel>();
    }


    public async Task<PriceListViewModel?> GetPriceListAsync(
        long id)
    {
        var response =
            await _http.GetAsync(
                $"api/pricelists/{id}");

        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();

        return await response.Content
            .ReadFromJsonAsync<PriceListViewModel>();
    }


    public async Task<(bool Success, string? Error)> CreatePriceListAsync(
        PriceListViewModel model)
    {
        var payload = new
        {
            model.Code,
            model.Name,
            model.Note,
            model.IsActive
        };

        var response =
            await _http.PostAsJsonAsync(
                "api/pricelists/create",
                payload);

        return response.IsSuccessStatusCode
            ? (true, null)
            : (
                false,
                await response.Content.ReadAsStringAsync()
            );
    }


    public async Task<(bool Success, string? Error)> UpdatePriceListAsync(
        PriceListViewModel model)
    {
        var payload = new
        {
            Id = model.Id!.Value,
            model.Code,
            model.Name,
            model.Note,
            model.IsActive
        };

        var response =
            await _http.PostAsJsonAsync(
                "api/pricelists/update",
                payload);

        return response.IsSuccessStatusCode
            ? (true, null)
            : (
                false,
                await response.Content.ReadAsStringAsync()
            );
    }


    public async Task<(bool Success, string? Error)> DeletePriceListAsync(
        long id)
    {
        var response =
            await _http.PostAsJsonAsync(
                "api/pricelists/delete",
                new { Id = id });

        return response.IsSuccessStatusCode
            ? (true, null)
            : (
                false,
                await response.Content.ReadAsStringAsync()
            );
    }

    public async Task<List<PriceListViewModel>> GetPriceListForExportAsync()
    {
        var response = await _http.GetAsync("api/PriceLists/export");

        if (!response.IsSuccessStatusCode)
            return new List<PriceListViewModel>();

        var data = await response.Content
           .ReadFromJsonAsync<List<PriceListViewModel>>();

        return data ?? new List<PriceListViewModel>();
    }

    //Price List Item 
    public async Task<List<PriceListItemViewModel>> GetPriceListItemsAsync(long priceListId)
    {
        var response = await _http.GetAsync(
            $"api/pricelists/{priceListId}/items");

        if (response.StatusCode == HttpStatusCode.NotFound)
            return new List<PriceListItemViewModel>();

        response.EnsureSuccessStatusCode();

        // DEBUG sementara: lihat JSON mentah di jendela Output Visual Studio
        var raw = await response.Content.ReadAsStringAsync();
        System.Diagnostics.Debug.WriteLine(raw);

        return System.Text.Json.JsonSerializer.Deserialize<List<PriceListItemViewModel>>(
                   raw,
                   new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web))
               ?? new List<PriceListItemViewModel>();
    }


    public async Task<(bool Success, string? Error)> CreatePriceListItemAsync(
        PriceListItemViewModel model)
    {
        var payload = new
        {
            model.PriceListId,
            model.ProductId,
            model.Price,
            model.TaxInclusive
        };

        var response = await _http.PostAsJsonAsync(
            "api/pricelists/items/create",
            payload);

        if (response.IsSuccessStatusCode)
            return (true, null);

        return (false, await response.Content.ReadAsStringAsync());
    }


    public async Task<(bool Success, string? Error)> UpdatePriceListItemAsync(
        PriceListItemViewModel model)
    {
        var payload = new
        {
            Id = model.Id,
            model.Price,
            model.TaxInclusive
        };

        var response = await _http.PostAsJsonAsync(
            "api/pricelists/items/update",
            payload);

        if (response.IsSuccessStatusCode)
            return (true, null);

        return (false, await response.Content.ReadAsStringAsync());
    }


    public async Task<(bool Success, string? Error)> DeletePriceListItemAsync(long id)
    {
        var response = await _http.PostAsJsonAsync(
            "api/pricelists/items/delete",
            new
            {
                Id = id
            });

        if (response.IsSuccessStatusCode)
            return (true, null);

        return (false, await response.Content.ReadAsStringAsync());
    }

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
        long id)
    {
        var response =
            await _http.GetAsync(
                $"api/productattributes/{id}");

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
        var payload = new
        {
            Id = model.Id!.Value,
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
        long id)
    {
        var response =
            await _http.PostAsJsonAsync(
                "api/productattributes/delete",
                new { Id = id });

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
     long attributeId,
     string? search = null,
     bool? isActive = null,
     int page = 1,
     int pageSize = 10)
    {
        var url =
            $"api/product-attributes/{attributeId}/variants?page={page}&pageSize={pageSize}";

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
        long attributeId,
        long id)
    {
        var response =
            await _http.GetAsync(
                $"api/product-attributes/{attributeId}/variants/{id}");

        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();

        return await response.Content
            .ReadFromJsonAsync<ProductVariantViewModel>();
    }

    public async Task<(bool Success, string? Error)> CreateProductVariantAsync(
        ProductVariantViewModel model)
    {
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
                $"api/product-attributes/{model.AttributeId}/variants/create",
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
        var payload = new
        {
            Id = model.Id!.Value,
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
                $"api/product-attributes/{model.AttributeId}/variants/update",
                payload);

        return response.IsSuccessStatusCode
            ? (true, null)
            : (
                false,
                await response.Content.ReadAsStringAsync()
            );
    }

    public async Task<(bool Success, string? Error)> ReorderProductVariantsAsync(
    long attributeId,
    List<object> items)
    {
        var response =
            await _http.PostAsJsonAsync(
                $"api/product-attributes/{attributeId}/variants/reorder",
                items);

        return response.IsSuccessStatusCode
            ? (true, null)
            : (
                false,
                await response.Content.ReadAsStringAsync()
            );
    }

    public async Task<(bool Success, string? Error)> DeleteProductVariantAsync(
        long attributeId,
        long id)
    {
        var response =
            await _http.PostAsJsonAsync(
                $"api/product-attributes/{attributeId}/variants/delete",
                new { Id = id });

        return response.IsSuccessStatusCode
            ? (true, null)
            : (
                false,
                await response.Content.ReadAsStringAsync()
            );
    }

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
        long id)
    {
        var response =
            await _http.GetAsync(
                $"api/workcenters/{id}");

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
        var payload = new
        {
            Id = model.Id!.Value,
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
        long id)
    {
        var response =
            await _http.PostAsJsonAsync(
                "api/workcenters/delete",
                new { Id = id });

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

    //product unit
    public async Task<JsonElement?> GetProductUnitsAsync(long productId)
    {
        var response = await _http.GetAsync(
            $"api/product-units/product/{productId}");

        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    public async Task<bool> CreateProductUnitAsync(
        CreateProductUnitViewModel model)
    {
        var response = await _http.PostAsJsonAsync(
            "api/product-units/create",
            model);

        return response.IsSuccessStatusCode;
    }

    public async Task<(bool Success, string? Error)> DeleteProductUnitAsync(
        long id)
    {
        var response = await _http.PostAsJsonAsync(
            "api/product-units/delete",
            new { Id = id });

        if (response.IsSuccessStatusCode)
            return (true, null);

        var body = await response.Content.ReadAsStringAsync();

        try
        {
            using var json = JsonDocument.Parse(body);

            if (json.RootElement.TryGetProperty("message", out var message))
                return (false, message.GetString());
        }
        catch (JsonException)
        {
            // Respons API bukan JSON.
        }

        return (false, "Gagal menghapus satuan konversi.");
    }

    //PO Barang
    public async Task<string?> GetNextGoodsPONumberAsync(
    DateOnly poDate,
    bool fromPRorRFQ = false)
    {
        var date = poDate.ToString("yyyy-MM-dd");

        var url =
            $"api/goods-purchase-orders/next-number" +
            $"?poDate={date}&fromPRorRFQ={fromPRorRFQ.ToString().ToLowerInvariant()}";

        var result = await _http.GetFromJsonAsync<NextPONumberResponse>(url);

        return result?.PoNo;
    }

    private sealed class NextPONumberResponse
    {
        public string? PoNo { get; set; }
    }

    public async Task<PagedResult<GoodsPurchaseOrderViewModel>> GetGoodsPurchaseOrdersAsync(
        string? search = null,
        int page = 1,
        int pageSize = 10,
        string? status = null)
    {
        var query = new List<string>
        {
            $"page={page}",
            $"pageSize={pageSize}"
        };

        if (!string.IsNullOrWhiteSpace(search))
        {
            query.Add($"search={Uri.EscapeDataString(search)}");
        }

        var url = "api/goods-purchase-orders?" +
                  string.Join("&", query);

        var result = await _http.GetFromJsonAsync<
            PagedResult<GoodsPurchaseOrderViewModel>>(url);

        return result ?? new PagedResult<GoodsPurchaseOrderViewModel>();
    }

    public async Task<GoodsPurchaseOrderViewModel?> GetGoodsPurchaseOrderAsync(long id)
    {
        var response = await _http.GetAsync($"api/goods-purchase-orders/{id}");

        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();

        var model = await response.Content
            .ReadFromJsonAsync<GoodsPurchaseOrderViewModel>();

        if (model == null)
            return null;

        model.Details ??= new List<GoodsPurchaseOrderItemViewModel>();

        return model;
    }

    public async Task<(bool Success, long? Id, string? Error)> CreateGoodsPurchaseOrderAsync(GoodsPurchaseOrderViewModel model)
    {
        try
        {
            var response = await _http.PostAsJsonAsync( "api/goods-purchase-orders/create", model ); 
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return (
                    false,
                    null,
                    $"HTTP {(int)response.StatusCode} ({response.StatusCode}): {responseContent}"
                );
            }

            using var document = JsonDocument.Parse(responseContent);

            var root = document.RootElement;

            if (!root.TryGetProperty("id", out var idElement) ||
                !idElement.TryGetInt64(out var id))
            {
                return (
                    false,
                    null,
                    "PO berhasil dikirim ke API, tetapi ID PO tidak ditemukan pada response."
                );
            }

            return (true, id, null);
        }
        catch (HttpRequestException ex)
        {
            return (false, null, $"Gagal menghubungi API: {ex.Message}");
        }
        catch (JsonException ex)
        {
            return (false, null, $"Response API tidak valid: {ex.Message}");
        }
    }

    public async Task<(bool Success, string? Error)> UpdateGoodsPurchaseOrderAsync(GoodsPurchaseOrderViewModel model)
    {
        try
        {
            var response = await _http.PostAsJsonAsync(
                "api/goods-purchase-orders/update",
                model);

            if (response.IsSuccessStatusCode)
                return (true, null);

            var error = await response.Content.ReadAsStringAsync();

            return (
                false,
                $"HTTP {(int)response.StatusCode} ({response.StatusCode}): {error}"
            );
        }
        catch (HttpRequestException ex)
        {
            return (false, $"Gagal menghubungi API: {ex.Message}");
        }
        catch (TaskCanceledException ex)
        {
            return (false, $"Request API dibatalkan atau timeout: {ex.Message}");
        }
    }

    public async Task<(bool Success, string? Error)> DeleteGoodsPurchaseOrderAsync(long id)
    {
        var response = await _http.PostAsJsonAsync(
            "api/goods-purchase-orders/delete",
            new { Id = id });

        return response.IsSuccessStatusCode
            ? (true, null)
            : (false, await response.Content.ReadAsStringAsync());
    }

    public async Task<bool> ApproveGoodsPurchaseOrderAsync(long id)
    {
        var response = await _http.PostAsync(
            $"api/goods-purchase-orders/{id}/approve",
            content: null);

        return response.IsSuccessStatusCode;
    }


    //penerimaan-barang
    public async Task<string?> GetNextGoodsReceiptNumberAsync(
    DateOnly receiptDate,
    bool fromPRorRFQ = false)
    {
        var date = receiptDate.ToString("yyyy-MM-dd");

        var url =
            $"api/goods-receipts/next-number" +
            $"?poDate={date}&fromPRorRFQ={fromPRorRFQ.ToString().ToLowerInvariant()}";

        var result = await _http.GetFromJsonAsync<NextReceiptNumberResponse>(url);

        return result?.ReceiptNo;
    }

    private sealed class NextReceiptNumberResponse
    {
        public string? ReceiptNo { get; set; }
    }

    public async Task<PagedResult<GoodsReceiptViewModel>> GetGoodsReceiptsAsync(
        string? search = null,
        int page = 1,
        int pageSize = 10,
        string? status = null)
    {
        var query = new List<string>
        {
            $"page={page}",
            $"pageSize={pageSize}"
        };

        if (!string.IsNullOrWhiteSpace(search))
        {
            query.Add($"search={Uri.EscapeDataString(search)}");
        }

        var url = "api/goods-receipts?" +
                  string.Join("&", query);

        var result = await _http.GetFromJsonAsync<
            PagedResult<GoodsReceiptViewModel>>(url);

        return result ?? new PagedResult<GoodsReceiptViewModel>();
    }

    public async Task<GoodsReceiptViewModel?> GetGoodsReceiptAsync(long id)
    {
        var response = await _http.GetAsync($"api/goods-receipts/{id}");

        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();

        var model = await response.Content.ReadFromJsonAsync<GoodsReceiptViewModel>();

        if (model == null)
            return null;

        model.Details ??= new List<GoodsReceiptItemViewModel>();

        return model;
    }

    public async Task<(bool Success, long? Id, string? Error)> CreateGoodsReceiptAsync(GoodsReceiptViewModel model)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("api/goods-receipts/create", model);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return (
                    false,
                    null,
                    $"HTTP {(int)response.StatusCode} ({response.StatusCode}): {responseContent}"
                );
            }

            using var document = JsonDocument.Parse(responseContent);

            var root = document.RootElement;

            if (!root.TryGetProperty("id", out var idElement) ||
                !idElement.TryGetInt64(out var id))
            {
                return (
                    false,
                    null,
                    "ID tidak ditemukan dalam respons"
                );
            }

            return (true, id, null);
        }
        catch (HttpRequestException ex)
        {
            return (false, null, $"Gagal menghubungi API: {ex.Message}");
        }
        catch (JsonException ex)
        {
            return (false, null, $"Response API tidak valid: {ex.Message}");
        }
    }

    public async Task<(bool Success, string? Error)> UpdateGoodsReceiptAsync(GoodsReceiptViewModel model)
    {
        try
        {
            var response = await _http.PostAsJsonAsync(
                "api/goods-receipts/update",
                model);

            if (response.IsSuccessStatusCode)
                return (true, null);

            var error = await response.Content.ReadAsStringAsync();

            return (
                false,
                $"HTTP {(int)response.StatusCode} ({response.StatusCode}): {error}"
            );
        }
        catch (HttpRequestException ex)
        {
            return (false, $"Gagal menghubungi API: {ex.Message}");
        }
        catch (TaskCanceledException ex)
        {
            return (false, $"Request API dibatalkan atau timeout: {ex.Message}");
        }
    }

    public async Task<(bool Success, string? Error)> DeleteGoodsReceiptAsync(long id)
    {
        var response = await _http.PostAsJsonAsync(
            "api/goods-receipts/delete",
            new { Id = id });

        return response.IsSuccessStatusCode
            ? (true, null)
            : (false, await response.Content.ReadAsStringAsync());
    }

    //RFQ Barang

    public async Task<PagedResult<ERP.Web.ViewModels.GoodsRfqViewModel>>
    GetGoodsRfqsAsync(
        string? search = null,
        string? status = null,
        int page = 1,
        int pageSize = 10)
    {
        var url = $"api/GoodsRfqs?page={page}&pageSize={pageSize}";

        if (!string.IsNullOrWhiteSpace(search))
            url += $"&search={Uri.EscapeDataString(search)}";

        if (!string.IsNullOrWhiteSpace(status))
            url += $"&status={Uri.EscapeDataString(status)}";

        return await _http
            .GetFromJsonAsync<
                PagedResult<ERP.Web.ViewModels.GoodsRfqViewModel>>(url)
            ?? new PagedResult<
                ERP.Web.ViewModels.GoodsRfqViewModel>();
    }

    public async Task<ERP.Web.ViewModels.GoodsRfqViewModel?> GetGoodsRfqAsync(long id)
    {
        var response = await _http.GetAsync($"api/GoodsRfqs/{id}");

        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content
            .ReadFromJsonAsync<ERP.Web.ViewModels.GoodsRfqViewModel>();
    }

    // Sekarang mengembalikan Id RFQ baru supaya bisa redirect ke halaman Details
    public async Task<(bool Success, string? Error, long? Id)> CreateGoodsRfqAsync(
        object payload)
    {
        var response = await _http.PostAsJsonAsync("api/GoodsRfqs/create", payload);
        var body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            return (false, body, null);

        return (true, null, ReadIdFromJson(body));
    }

    public async Task<(bool Success, string? Error)> UpdateGoodsRfqAsync(
        long id, object payload)
    {
        var response = await _http.PutAsJsonAsync($"api/GoodsRfqs/{id}", payload);
        return await ToActionResultAsync(response);
    }

    // draft -> terkirim
    public async Task<(bool Success, string? Error)> SendGoodsRfqAsync(long id)
    {
        var response = await _http.PostAsync($"api/GoodsRfqs/{id}/send", null);
        return await ToActionResultAsync(response);
    }

    // terkirim -> ditawar
    public async Task<(bool Success, string? Error)> SaveGoodsRfqQuoteAsync(
        long id, object payload)
    {
        var response = await _http.PostAsJsonAsync($"api/GoodsRfqs/{id}/quote", payload);
        return await ToActionResultAsync(response);
    }

    // -> batal
    public async Task<(bool Success, string? Error)> CancelGoodsRfqAsync(long id)
    {
        var response = await _http.PostAsync($"api/GoodsRfqs/{id}/cancel", null);
        return await ToActionResultAsync(response);
    }

    private static async Task<(bool Success, string? Error)> ToActionResultAsync(
        HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
            return (true, null);

        return (false, await response.Content.ReadAsStringAsync());
    }

    // Membaca "id" dari response, baik { "id": 5 } maupun { "data": { "id": 5 } }
    private static long? ReadIdFromJson(string body)
    {
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(body);
            var root = doc.RootElement;

            if (root.ValueKind != System.Text.Json.JsonValueKind.Object)
                return null;

            foreach (var prop in root.EnumerateObject())
            {
                if (prop.Name.Equals("id", StringComparison.OrdinalIgnoreCase) &&
                    prop.Value.TryGetInt64(out var id))
                    return id;

                if (prop.Name.Equals("data", StringComparison.OrdinalIgnoreCase) &&
                    prop.Value.ValueKind == System.Text.Json.JsonValueKind.Object)
                {
                    foreach (var inner in prop.Value.EnumerateObject())
                    {
                        if (inner.Name.Equals("id", StringComparison.OrdinalIgnoreCase) &&
                            inner.Value.TryGetInt64(out var innerId))
                            return innerId;
                    }
                }
            }
        }
        catch
        {
            // abaikan, id tidak terbaca
        }

        return null;
    }

    public async Task<List<GoodsRfqItemViewModel>>
        GetGoodsRfqProductsAsync()
    {
        var response = await _http.GetAsync(
            "api/GoodsRfqItems/products");

        if (!response.IsSuccessStatusCode)
            return new List<GoodsRfqItemViewModel>();

        var products = await response.Content
            .ReadFromJsonAsync<List<GoodsRfqItemProduct>>();

        return products?.Select(p => new GoodsRfqItemViewModel
        {
            ProductId = p.Id,
            ProductName = p.Name,
            Sku = p.Sku,
            UnitId = p.UnitId,
            UnitSymbol = p.UnitSymbol,
            CostPrice = p.CostPrice
        }).ToList() ?? new List<GoodsRfqItemViewModel>();
    }

    private async Task<List<T>> GetListAsync<T>(string url)
    {
        try
        {
            var response = await _http.GetAsync(url);

            if (!response.IsSuccessStatusCode)
                return new List<T>();

            await using var stream = await response.Content.ReadAsStreamAsync();
            using var doc = await System.Text.Json.JsonDocument.ParseAsync(stream);

            var root = doc.RootElement;
            var options = new System.Text.Json.JsonSerializerOptions(
                System.Text.Json.JsonSerializerDefaults.Web);

            if (root.ValueKind == System.Text.Json.JsonValueKind.Array)
                return root.Deserialize<List<T>>(options) ?? new List<T>();

            if (root.ValueKind == System.Text.Json.JsonValueKind.Object)
            {
                foreach (var prop in root.EnumerateObject())
                {
                    if (prop.Value.ValueKind == System.Text.Json.JsonValueKind.Array &&
                        (prop.NameEquals("data") ||
                         prop.Name.Equals("items", StringComparison.OrdinalIgnoreCase) ||
                         prop.Name.Equals("results", StringComparison.OrdinalIgnoreCase) ||
                         prop.Name.Equals("data", StringComparison.OrdinalIgnoreCase)))
                    {
                        return prop.Value.Deserialize<List<T>>(options) ?? new List<T>();
                    }
                }
            }

            return new List<T>();
        }
        catch
        {
            return new List<T>();
        }
    }


    public async Task<(bool Success, string? Error)> DeleteGoodsRfqAsync(long id)
    {
        var response = await _http.DeleteAsync($"api/GoodsRfqs/{id}");
        return await ToActionResultAsync(response);
    }

    public async Task<string?> GetNextGoodsRfqNoAsync()
    {
        try
        {
            var response = await _http.GetAsync("api/GoodsRfqs/next-number");
            if (!response.IsSuccessStatusCode) return null;

            using var doc = await System.Text.Json.JsonDocument.ParseAsync(
                await response.Content.ReadAsStreamAsync());

            return doc.RootElement.TryGetProperty("rfqNo", out var v)
                ? v.GetString()
                : null;
        }
        catch
        {
            return null;
        }
    }

}
