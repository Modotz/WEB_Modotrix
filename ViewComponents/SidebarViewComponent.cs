using ERP.Web.Models;
using ERP.Web.Security;
using ERP.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace ERP.Web.ViewComponents;

/// <summary>
/// Sidebar dinamis dari tabel menus, sudah difilter hak akses (can_view) oleh API.
/// </summary>
public class SidebarViewComponent : ViewComponent
{
    private readonly AccessService _access;
    private readonly AccountingApiClient _api;
    private readonly IMemoryCache _cache;

    public SidebarViewComponent(AccessService access, AccountingApiClient api, IMemoryCache cache)
    {
        _access = access;
        _api = api;
        _cache = cache;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var access = await _access.GetAsync();

        var controller = ViewContext.RouteData.Values["controller"]?.ToString() ?? string.Empty;
        var action = ViewContext.RouteData.Values["action"]?.ToString() ?? string.Empty;

        var model = new SidebarModel
        {
            Items = access?.Sidebar ?? new List<SidebarMenuViewModel>(),
            ActiveMenuUrl = MenuRegistry.ResolvePageMenuUrl(controller, action)
        };

        // Badge Kotak Persetujuan: jumlah yang menunggu keputusan user (satu panggilan API, cache 30 detik per user).
        if (access != null && await _access.CanAsync("approvals"))
            model.Badges["approvals"] = await Controllers.ApprovalsController.WaitingCountAsync(_api, _cache, UserClaimsPrincipal);

        return View(model);
    }
}

public class SidebarModel
{
    public List<SidebarMenuViewModel> Items { get; set; } = new();

    public string? ActiveMenuUrl { get; set; }

    /// <summary>Angka badge per URL menu (mis. "approvals" = pengajuan yang menunggu user).</summary>
    public Dictionary<string, int> Badges { get; } = new(StringComparer.OrdinalIgnoreCase);

    public int BadgeOf(SidebarMenuViewModel item) =>
        item.Url != null && Badges.TryGetValue(item.Url, out var n) ? n : 0;

    public int GroupBadgeOf(SidebarMenuViewModel item) => item.Children.Sum(BadgeOf);

    public bool IsActive(SidebarMenuViewModel item) =>
        ActiveMenuUrl != null && string.Equals(item.Url, ActiveMenuUrl, StringComparison.OrdinalIgnoreCase);
}
