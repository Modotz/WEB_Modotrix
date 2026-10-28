using ERP.Web.Models;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace ERP.Web.Services;

public partial class ApiClient
{
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
    // GET CUSTOMER BY UID
    // =========================================================
    public async Task<CustomerViewModel?> GetCustomerAsync(Guid uid)
    {
        var response =
            await _http.GetAsync(
                $"api/customers/{uid}");

        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();

        return await response.Content
            .ReadFromJsonAsync<CustomerViewModel>();
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
    public async Task<bool> ActivateCustomerAsync(Guid uid)
    {
        var customer =
            await GetCustomerAsync(uid);

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
        Guid uid)
    {
        var response =
            await _http.PostAsJsonAsync(
                "api/customers/delete",
                new
                {
                    uid
                });

        return response.IsSuccessStatusCode;
    }

    // =========================================================
    // UPLOAD IMAGE CUSTOMERS
    // =========================================================

    public async Task<string?> UploadCustomerImageAsync(
        Guid customerUid,
        IFormFile file)
    {
        using var content =
            new MultipartFormDataContent();

        content.Add(
            new StringContent(
                customerUid.ToString()),
            "customerUid");

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

    // SHIPPING ADDRESS

    // GET SHIPPING ADDRESSES BY CUSTOMER
    public async Task<List<ShippingAddressViewModel>>
        GetShippingAddressesByCustomerAsync(Guid customerUid)
    {
        var response = await _http.GetAsync(
            $"api/shipping-addresses/customer/{customerUid}");

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
    Guid customerUid,
    string name,
    string address,
    long? shippingAreaId)
    {
        var response = await _http.PostAsJsonAsync(
            "api/shipping-addresses",
            new
            {
                customerUid,
                name,
                address,
                shippingAreaId
            });

        return response.IsSuccessStatusCode;
    }

    public async Task<bool> UpdateShippingAddressAsync(
        Guid uid,
        Guid customerUid,
        string name,
        string address,
        long? shippingAreaId)
    {
        var response = await _http.PostAsJsonAsync(
            "api/shipping-addresses/update",
            new
            {
                uid,
                customerUid,
                name,
                address,
                shippingAreaId
            });

        return response.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteShippingAddressAsync(Guid uid)
    {
        var response = await _http.PostAsJsonAsync(
            "api/shipping-addresses/delete",
            new
            {
                uid
            });

        return response.IsSuccessStatusCode;
    }
}
