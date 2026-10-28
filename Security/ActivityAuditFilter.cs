using ERP.Web.Models;
using ERP.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ERP.Web.Security;

/// <summary>
/// Mencatat aktivitas yang tidak mengubah data tetapi tetap perlu jejak (seperti record_audit('export', ...)
/// di aplikasi PHP): ekspor Excel/PDF dan cetak di semua modul. Perubahan data dicatat otomatis oleh API.
/// </summary>
public class ActivityAuditFilter : IAsyncActionFilter
{
    private readonly AuditLogApiClient _audit;
    private readonly AccessService _access;

    public ActivityAuditFilter(AuditLogApiClient audit, AccessService access)
    {
        _audit = audit;
        _access = access;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var executed = await next();

        if (context.ActionDescriptor is not ControllerActionDescriptor descriptor ||
            context.HttpContext.User.Identity?.IsAuthenticated != true ||
            executed.Exception != null && !executed.ExceptionHandled ||
            executed.Result is not (FileResult or ViewResult))
            return;

        var activity = Describe(descriptor.ActionName);
        if (activity == null)
            return;

        var menuUrl = MenuRegistry.ResolveMenuUrl(descriptor.ControllerName, descriptor.ActionName);
        var title = await MenuTitleAsync(menuUrl) ?? descriptor.ControllerName;
        var module = (menuUrl ?? descriptor.ControllerName).Replace('-', '_');
        var recordId = context.ActionArguments.TryGetValue("uid", out var uid) ? uid?.ToString() : null;

        await _audit.RecordAsync(
            activity.Value.Action,
            module,
            $"{activity.Value.Verb} {title}{(recordId != null ? $" ({recordId})" : "")}{activity.Value.Suffix}",
            recordId);
    }

    private static (string Action, string Verb, string Suffix)? Describe(string actionName)
    {
        if (actionName.StartsWith("ExportExcel", StringComparison.OrdinalIgnoreCase))
            return ("export", "Mengekspor", " ke Excel");
        if (actionName.StartsWith("ExportPdf", StringComparison.OrdinalIgnoreCase))
            return ("export", "Mengekspor", " ke PDF");
        if (actionName.StartsWith("Export", StringComparison.OrdinalIgnoreCase))
            return ("export", "Mengekspor", "");
        if (actionName.StartsWith("Print", StringComparison.OrdinalIgnoreCase))
            return ("print", "Mencetak", "");
        if (actionName.Equals("Label", StringComparison.OrdinalIgnoreCase))
            return ("print", "Mencetak label", "");
        return null;
    }

    private async Task<string?> MenuTitleAsync(string? menuUrl)
    {
        if (menuUrl == null)
            return null;

        var sidebar = (await _access.GetAsync())?.Sidebar ?? new List<SidebarMenuViewModel>();

        return sidebar
            .SelectMany(m => m.Children.Prepend(m))
            .FirstOrDefault(m => string.Equals(m.Url, menuUrl, StringComparison.OrdinalIgnoreCase))
            ?.Title;
    }
}
