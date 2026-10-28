using ERP.Web.Models;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace ERP.Web.Services;

public partial class ApiClient
{
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
        GetCustomerGroupAsync(Guid uid)
    {
        var response =
            await _http.GetAsync(
                $"api/customergroups/{uid}");

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
            model.Uid,
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
            Guid uid)
    {
        var response =
            await _http.PostAsJsonAsync(
                "api/customergroups/delete",
                new
                {
                    Uid = uid
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
}
