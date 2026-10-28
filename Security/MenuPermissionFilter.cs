using ERP.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ERP.Web.Security;

/// <summary>
/// Mengecek hak akses (tabel access_role) sebelum setiap action dijalankan
/// (padanan filter "permission:menu,action" di aplikasi PHP). Administrator selalu lolos.
/// Controller yang belum terdaftar di <see cref="MenuRegistry"/> hanya boleh diakses Administrator.
/// </summary>
public class MenuPermissionFilter : IAsyncActionFilter
{
    private readonly AccessService _access;
    private readonly ILogger<MenuPermissionFilter> _logger;

    public MenuPermissionFilter(AccessService access, ILogger<MenuPermissionFilter> logger)
    {
        _access = access;
        _logger = logger;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (context.ActionDescriptor is not ControllerActionDescriptor descriptor ||
            context.ActionDescriptor.EndpointMetadata.OfType<IAllowAnonymous>().Any() ||
            context.HttpContext.User.Identity?.IsAuthenticated != true)
        {
            await next();
            return;
        }

        // Selalu ambil hak akses lebih dulu: bila token API sudah kedaluwarsa,
        // error 401-nya ditangani ApiUnauthorizedExceptionFilter (logout otomatis).
        var access = await _access.GetAsync();

        if (access == null || access.IsAdmin || MenuRegistry.IsAuthenticatedOnly(descriptor.ControllerName))
        {
            await next();
            return;
        }

        var menuUrl = MenuRegistry.ResolveMenuUrl(descriptor.ControllerName, descriptor.ActionName);

        if (menuUrl == null)
        {
            _logger.LogWarning(
                "Controller {Controller} belum terdaftar di MenuRegistry; akses hanya untuk Administrator.",
                descriptor.ControllerName);
            context.Result = Deny(context);
            return;
        }

        foreach (var action in MenuRegistry.RequiredActions(descriptor.ControllerName, descriptor.ActionName))
        {
            if (await _access.CanAsync(menuUrl, action))
            {
                await next();
                return;
            }
        }

        context.Result = Deny(context);
    }

    private static IActionResult Deny(ActionExecutingContext context)
    {
        if (RequestKind.IsAjax(context.HttpContext.Request))
        {
            return new JsonResult(new { success = false, message = "Akses ditolak." })
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
        }

        var controller = (Controller)context.Controller;

        return new ViewResult
        {
            ViewName = "AccessDenied",
            StatusCode = StatusCodes.Status403Forbidden,
            ViewData = controller.ViewData,
            TempData = controller.TempData
        };
    }
}

public static class RequestKind
{
    /// <summary>Request dari JavaScript (fetch/jQuery) yang mengharapkan JSON, bukan halaman HTML.</summary>
    public static bool IsAjax(HttpRequest request) =>
        request.Headers.XRequestedWith == "XMLHttpRequest" ||
        request.Headers.Accept.ToString().Contains("application/json", StringComparison.OrdinalIgnoreCase) ||
        (request.ContentType?.Contains("application/json", StringComparison.OrdinalIgnoreCase) ?? false);
}
