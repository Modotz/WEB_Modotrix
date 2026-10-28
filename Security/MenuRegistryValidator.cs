using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace ERP.Web.Security;

/// <summary>
/// Saat aplikasi start, peringatkan controller yang belum terdaftar di <see cref="MenuRegistry"/>
/// (halamannya hanya bisa dibuka Administrator sampai didaftarkan).
/// </summary>
public static class MenuRegistryValidator
{
    public static void WarnUnregisteredControllers(this WebApplication app)
    {
        var provider = app.Services.GetRequiredService<IActionDescriptorCollectionProvider>();
        var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(MenuRegistryValidator));

        var unregistered = provider.ActionDescriptors.Items
            .OfType<ControllerActionDescriptor>()
            .Where(a => !a.EndpointMetadata.OfType<IAllowAnonymous>().Any())
            .Where(a => !MenuRegistry.IsAuthenticatedOnly(a.ControllerName))
            .Where(a => MenuRegistry.ResolveMenuUrl(a.ControllerName, a.ActionName) == null)
            .Select(a => a.ControllerName)
            .Distinct()
            .OrderBy(n => n)
            .ToList();

        if (unregistered.Count > 0)
        {
            logger.LogWarning(
                "Controller belum terdaftar di Security/MenuRegistry.cs (hanya Administrator yang bisa membuka): {Controllers}",
                string.Join(", ", unregistered));
        }
    }
}
