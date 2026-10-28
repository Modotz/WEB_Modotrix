using ERP.Web.Models;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace ERP.Web.Services;

public partial class ApiClient
{
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
    // GET WAREHOUSE BY UID
    // ==========================================
    public async Task<WarehouseViewModel?> GetWarehouseAsync(Guid uid)
    {
        try
        {
            var response =
                await _http.GetAsync(
                    $"api/warehouses/{uid}");
             
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
        if (string.IsNullOrWhiteSpace(model.Uid))
        {
            return (
                false,
                "Data gudang tidak ditemukan."
            );
        }

        var payload = new
        {
            model.Uid,
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
        DeleteWarehouseAsync(Guid uid)
    {
        var payload = new
        {
            Uid = uid
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
}
