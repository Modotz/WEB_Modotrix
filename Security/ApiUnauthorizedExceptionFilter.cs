using System.Net;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ERP.Web.Security;

/// <summary>
/// Menangani penolakan dari API:
/// 401 (token kedaluwarsa / user dinonaktifkan) = logout dari web lalu kembali ke halaman login;
/// 403 (hak akses tidak cukup) = tampilkan halaman "Akses ditolak".
/// </summary>
public class ApiUnauthorizedExceptionFilter : IAsyncExceptionFilter
{
    public async Task OnExceptionAsync(ExceptionContext context)
    {
        var status = ApiStatusCode(context.Exception);
        var isAjax = RequestKind.IsAjax(context.HttpContext.Request);

        if (status == HttpStatusCode.Unauthorized)
        {
            await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            if (isAjax)
            {
                context.Result = new JsonResult(new { success = false, message = "Sesi berakhir. Silakan login kembali." })
                {
                    StatusCode = StatusCodes.Status401Unauthorized
                };
            }
            else
            {
                var returnUrl = context.HttpContext.Request.Path + context.HttpContext.Request.QueryString;
                context.Result = new RedirectToActionResult("Login", "Account", new { returnUrl, expired = true });
            }
        }
        else if (status == HttpStatusCode.Forbidden)
        {
            context.Result = isAjax
                ? new JsonResult(new { success = false, message = "Akses ditolak." }) { StatusCode = StatusCodes.Status403Forbidden }
                : new ViewResult { ViewName = "AccessDenied", StatusCode = StatusCodes.Status403Forbidden };
        }
        else
        {
            return;
        }

        context.ExceptionHandled = true;
    }

    private static HttpStatusCode? ApiStatusCode(Exception? ex)
    {
        for (; ex != null; ex = ex.InnerException)
        {
            if (ex is HttpRequestException { StatusCode: HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden } http)
                return http.StatusCode;
        }

        return null;
    }
}
