using System.Net.Http.Json;
using System.Text.Json;
using ERP.Web.Models;

namespace ERP.Web.Services;

/// <summary>
/// Endpoint API untuk login, hak akses dan matriks access role.
/// </summary>
public class AuthApiClient
{
    private readonly HttpClient _http;

    public AuthApiClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<(LoginResultViewModel? Result, string? Error)> LoginAsync(string username, string password)
    {
        var response = await _http.PostAsJsonAsync("api/auth/login", new { username, password });

        if (response.IsSuccessStatusCode)
            return (await response.Content.ReadFromJsonAsync<LoginResultViewModel>(), null);

        return (null, await ReadMessageAsync(response) ?? "Login gagal.");
    }

    public async Task<(bool Success, string Message)> ForgotPasswordAsync(string email)
    {
        var response = await _http.PostAsJsonAsync("api/auth/forgot-password", new { email });
        var message = await ReadMessageAsync(response);

        return (response.IsSuccessStatusCode, message ?? "Permintaan reset password gagal diproses.");
    }

    public async Task<bool> IsResetTokenValidAsync(string token)
    {
        var response = await _http.PostAsJsonAsync("api/auth/reset-password/validate", new { token });
        return response.IsSuccessStatusCode;
    }

    public async Task<(bool Success, string Message)> ResetPasswordAsync(string token, string password, string passwordConfirm)
    {
        var response = await _http.PostAsJsonAsync("api/auth/reset-password", new { token, password, passwordConfirm });
        var message = await ReadMessageAsync(response);

        return (response.IsSuccessStatusCode, message ?? "Reset password gagal.");
    }

    public async Task<List<DevUserViewModel>> GetDevUsersAsync()
    {
        try
        {
            var response = await _http.GetAsync("api/auth/dev-users");
            if (!response.IsSuccessStatusCode)
                return new();

            return await response.Content.ReadFromJsonAsync<List<DevUserViewModel>>() ?? new();
        }
        catch (HttpRequestException)
        {
            return new();
        }
    }

    public async Task LogoutAsync()
    {
        try
        {
            await _http.PostAsync("api/auth/logout", null);
        }
        catch (HttpRequestException)
        {
            // Logout di sisi web tetap dijalankan walau API tidak bisa dihubungi.
        }
    }

    /// <summary>Melempar HttpRequestException (StatusCode 401) bila token sudah tidak berlaku.</summary>
    public async Task<UserAccessViewModel> GetAccessAsync()
    {
        return await _http.GetFromJsonAsync<UserAccessViewModel>("api/auth/access")
            ?? new UserAccessViewModel();
    }

    public async Task<AccessRoleMatrixViewModel> GetAccessRoleMatrixAsync(Guid? roleUid)
    {
        var url = roleUid != null ? $"api/access-role?roleUid={roleUid}" : "api/access-role";
        return await _http.GetFromJsonAsync<AccessRoleMatrixViewModel>(url)
            ?? new AccessRoleMatrixViewModel();
    }

    public async Task<(bool Success, string Message)> SaveAccessRoleAsync(SaveAccessRoleViewModel request)
    {
        var response = await _http.PostAsJsonAsync("api/access-role/save", request);
        var message = await ReadMessageAsync(response);

        return response.IsSuccessStatusCode
            ? (true, message ?? "Hak akses berhasil disimpan.")
            : (false, message ?? $"Gagal menyimpan hak akses ({(int)response.StatusCode}).");
    }

    private static async Task<string?> ReadMessageAsync(HttpResponseMessage response)
    {
        try
        {
            var json = await response.Content.ReadFromJsonAsync<JsonElement>();
            return json.ValueKind == JsonValueKind.Object && json.TryGetProperty("message", out var m)
                ? m.GetString()
                : null;
        }
        catch (Exception)
        {
            return null;
        }
    }
}
