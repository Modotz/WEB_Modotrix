using ERP.Web.Models;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace ERP.Web.Services;

public partial class ApiClient
{
    //product unit
    public async Task<JsonElement?> GetProductUnitsAsync(Guid productUid)
    {
        var response = await _http.GetAsync(
            $"api/product-units/product/{productUid}");

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
        Guid uid)
    {
        var response = await _http.PostAsJsonAsync(
            "api/product-units/delete",
            new { Uid = uid });

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
}
