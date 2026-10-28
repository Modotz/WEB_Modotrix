using System.Security.Claims;
using ERP.Web.Models;
using Microsoft.Extensions.Caching.Memory;

namespace ERP.Web.Services;

/// <summary>
/// Hak akses user yang sedang login (izin per menu + sidebar), diambil dari API dan
/// di-cache singkat per user. Dipakai oleh filter izin, sidebar dan view (mis. menyembunyikan tombol).
/// </summary>
public class AccessService
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);
    private static int _version;

    private readonly AuthApiClient _api;
    private readonly IMemoryCache _cache;
    private readonly IHttpContextAccessor _http;

    private UserAccessViewModel? _current;

    public AccessService(AuthApiClient api, IMemoryCache cache, IHttpContextAccessor http)
    {
        _api = api;
        _cache = cache;
        _http = http;
    }

    /// <summary>Buang semua cache hak akses (dipanggil setelah matriks hak akses disimpan).</summary>
    public static void InvalidateAll() => Interlocked.Increment(ref _version);

    public async Task<UserAccessViewModel?> GetAsync()
    {
        if (_current != null)
            return _current;

        var user = _http.HttpContext?.User;
        var userId = user?.FindFirstValue(ClaimTypes.NameIdentifier);

        if (user?.Identity?.IsAuthenticated != true || userId == null)
            return null;

        var key = $"access:{userId}:{Volatile.Read(ref _version)}";

        _current = await _cache.GetOrCreateAsync(key, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheDuration;
            return await _api.GetAccessAsync();
        });

        return _current;
    }

    public async Task<bool> IsAdminAsync() => (await GetAsync())?.IsAdmin == true;

    /// <summary>
    /// Apakah user boleh melakukan <paramref name="action"/> (view/create/update/delete)
    /// pada menu dengan URL <paramref name="menuUrl"/> (kolom menus.url). Administrator selalu boleh.
    /// </summary>
    public async Task<bool> CanAsync(string menuUrl, string action = "view")
    {
        var access = await GetAsync();
        if (access == null)
            return false;

        if (access.IsAdmin)
            return true;

        if (!access.Permissions.TryGetValue(menuUrl, out var perm))
            return false;

        return action switch
        {
            "create" => perm.Create,
            "update" => perm.Update,
            "delete" => perm.Delete,
            _ => perm.View
        };
    }

    private static readonly string[] WriteActions = { "create", "update", "delete" };

    /// <summary>
    /// Aturan CSS yang menyembunyikan tombol aksi yang tidak diizinkan. Tombol di view ditandai:
    /// <list type="bullet">
    /// <item><c>data-perm="update"</c> — aksi pada menu halaman ini (create/update/delete).</item>
    /// <item><c>data-perm="goods-receipts:create"</c> — aksi pada menu lain.</item>
    /// <item><c>data-perm-any="create update"</c> — tampil bila salah satu diizinkan
    /// (urutan nilai: create, update, delete).</item>
    /// </list>
    /// Hanya tampilan; server tetap menolak aksi tanpa izin.
    /// </summary>
    public async Task<string> BuildHiddenActionsCssAsync(string? currentMenuUrl)
    {
        var access = await GetAsync();
        if (access == null || access.IsAdmin)
            return string.Empty;

        var selectors = new List<string>();

        if (currentMenuUrl != null)
        {
            var denied = new List<string>();
            foreach (var action in WriteActions)
            {
                if (await CanAsync(currentMenuUrl, action))
                    continue;

                denied.Add(action);
                selectors.Add($"[data-perm~=\"{action}\"]");
            }

            // data-perm-any: sembunyikan bila SEMUA aksi yang disebut ditolak.
            for (var mask = 3; mask < 8; mask++)
            {
                var combo = WriteActions.Where((_, i) => (mask & (1 << i)) != 0).ToList();
                if (combo.Count >= 2 && combo.All(denied.Contains))
                    selectors.Add($"[data-perm-any=\"{string.Join(' ', combo)}\"]");
            }
            foreach (var action in denied)
                selectors.Add($"[data-perm-any=\"{action}\"]");
        }

        foreach (var menuUrl in Security.MenuRegistry.Pages.Select(p => p.MenuUrl).Distinct())
        {
            foreach (var action in WriteActions)
            {
                if (!await CanAsync(menuUrl, action))
                    selectors.Add($"[data-perm~=\"{menuUrl}:{action}\"]");
            }
        }

        return selectors.Count == 0
            ? string.Empty
            : string.Join(",\n", selectors) + " { display: none !important; }";
    }
}
