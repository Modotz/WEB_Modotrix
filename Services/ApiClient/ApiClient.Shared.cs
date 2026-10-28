using ERP.Web.Models;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace ERP.Web.Services;

public partial class ApiClient
{
    private class PhotoUploadResponse
    {
        public string? Photo { get; set; }
    }

    // =========================================================
    // RESPONSE UPLOAD IMAGE BRAND
    // =========================================================

    private class BrandImageUploadResponse
    {
        public string? Image { get; set; }
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

    private class CustomerGroupResponse
    {
        public bool Success { get; set; }

        public string? Message { get; set; }

        public CustomerGroupsViewModel? Data { get; set; }
    }

    private class CustomerCreateResponse
    {
        public string? Message { get; set; }

        public CustomerViewModel? Data { get; set; }
    }

    // =========================================================
    // RESPONSE UPLOAD IMAGE CUSTOMER
    // =========================================================

    private class CustomerImageUploadResponse
    {
        public string? Image { get; set; }
    }

    private class ProductImageUploadResponse
    {
        public string? Image { get; set; }
    }

    private sealed class NextPONumberResponse
    {
        public string? PoNo { get; set; }
    }

    private sealed class NextReceiptNumberResponse
    {
        public string? ReceiptNo { get; set; }
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
}
