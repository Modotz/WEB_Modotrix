using ERP.Web.Models;
using Microsoft.Extensions.Caching.Memory;

namespace ERP.Web.Services;

/// <summary>
/// Nama &amp; logo aplikasi dari App Settings untuk layout dan halaman login (di-cache singkat).
/// </summary>
public class BrandingService
{
    private const string CacheKey = "branding";

    private readonly SettingsApiClient _api;
    private readonly IMemoryCache _cache;
    private readonly IConfiguration _config;
    private readonly ILogger<BrandingService> _logger;

    public BrandingService(SettingsApiClient api, IMemoryCache cache, IConfiguration config, ILogger<BrandingService> logger)
    {
        _api = api;
        _cache = cache;
        _config = config;
        _logger = logger;
    }

    public async Task<BrandingViewModel> GetAsync()
    {
        if (_cache.TryGetValue(CacheKey, out BrandingViewModel? cached) && cached != null)
            return cached;

        try
        {
            var branding = await _api.GetBrandingAsync();
            _cache.Set(CacheKey, branding, TimeSpan.FromMinutes(5));
            return branding;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            // API mati: tampilkan default, jangan gagalkan halaman (termasuk halaman login).
            _logger.LogWarning(ex, "Gagal memuat branding dari API.");
            return new BrandingViewModel();
        }
    }

    public void Invalidate() => _cache.Remove(CacheKey);

    /// <summary>URL lengkap file yang disimpan di API (mis. logo "uploads/x.png").</summary>
    public string? AssetUrl(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return null;

        if (path.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            return path;

        var baseUrl = (_config["ApiSettings:BaseUrl"] ?? "https://localhost:7101/").TrimEnd('/');
        return $"{baseUrl}/{path.TrimStart('/')}";
    }
}
