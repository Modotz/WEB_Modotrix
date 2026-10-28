using ERP.Web.Models;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace ERP.Web.Services;

public partial class ApiClient
{
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

    // GET MSFIELD BY UID
    public async Task<MsfieldViewModel?> GetMsfieldsAsync(Guid uid)
    {
        var response =
            await _http.GetAsync(
                $"api/msfields/{uid}");

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
        if (string.IsNullOrWhiteSpace(model.Uid))
            return (false, "Data field tidak ditemukan.");

        var payload = new
        {
            model.Uid,
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
        DeleteFieldAsync(Guid uid)
    {
        var response =
            await _http.PostAsJsonAsync(
                "api/msfields/Delete",
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
}
