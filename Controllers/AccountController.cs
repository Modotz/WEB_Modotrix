using System.Security.Claims;
using ERP.Web.Models;
using ERP.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Web.Controllers;

public class AccountController : Controller
{
    public const string RoleIdClaim = "role_id";
    public const string RoleNameClaim = "role_name";
    public const string DisplayNameClaim = "display_name";
    public const string PhotoClaim = "photo";

    private readonly AuthApiClient _auth;
    private readonly IWebHostEnvironment _env;

    public AccountController(AuthApiClient auth, IWebHostEnvironment env)
    {
        _auth = auth;
        _env = env;
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> Login(string? returnUrl = null, bool expired = false, bool resetInvalid = false)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToLocal(returnUrl);

        if (expired)
            ViewData["Error"] = "Sesi berakhir. Silakan login kembali.";
        else if (resetInvalid)
            ViewData["Error"] = "Tautan reset tidak valid atau kedaluwarsa.";

        return View(await BuildLoginModelAsync(new LoginViewModel { ReturnUrl = returnUrl }));
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
            return View(await BuildLoginModelAsync(model));

        LoginResultViewModel? result;
        string? error;

        try
        {
            (result, error) = await _auth.LoginAsync(model.Username, model.Password);
        }
        catch (HttpRequestException)
        {
            (result, error) = (null, "Server API tidak dapat dihubungi. Coba beberapa saat lagi.");
        }

        if (result == null)
        {
            ViewData["Error"] = error;
            model.Password = string.Empty;
            return View(await BuildLoginModelAsync(model));
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, result.User.Id.ToString()),
            new(ClaimTypes.Name, result.User.Username),
            new(DisplayNameClaim, result.User.Name),
            new(ClaimTypes.Email, result.User.Email),
            new(RoleIdClaim, result.User.RoleId.ToString()),
            new(RoleNameClaim, result.User.RoleName ?? string.Empty),
            new(PhotoClaim, result.User.Photo ?? string.Empty),
            new(ApiTokenHandler.AccessTokenClaim, result.Token)
        };

        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));

        // Cookie berakhir bersamaan dengan token API.
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = model.RememberMe,
                ExpiresUtc = result.ExpiresAt,
                AllowRefresh = false
            });

        TempData["Success"] = $"Selamat datang, {result.User.Name}!";

        return RedirectToLocal(model.ReturnUrl);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _auth.LogoutAsync();
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        TempData["Success"] = "Anda telah logout.";

        return RedirectToAction(nameof(Login));
    }

    // ---------------------------------------------------------------
    // LUPA / RESET PASSWORD
    // ---------------------------------------------------------------

    [AllowAnonymous]
    [HttpGet]
    public IActionResult ForgotPassword() => View(new ForgotPasswordViewModel());

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        try
        {
            var (success, message) = await _auth.ForgotPasswordAsync(model.Email.Trim());

            if (!success)
            {
                ViewData["Error"] = message;
                return View(model);
            }

            TempData["Success"] = message;
            return RedirectToAction(nameof(Login));
        }
        catch (HttpRequestException)
        {
            ViewData["Error"] = "Server API tidak dapat dihubungi. Coba beberapa saat lagi.";
            return View(model);
        }
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> ResetPassword(string? token)
    {
        if (string.IsNullOrWhiteSpace(token) || !await _auth.IsResetTokenValidAsync(token))
        {
            return RedirectToAction(nameof(Login), new { resetInvalid = true });
        }

        return View(new ResetPasswordViewModel { Token = token });
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var (success, message) = await _auth.ResetPasswordAsync(model.Token, model.Password, model.PasswordConfirm);

        if (!success)
        {
            ViewData["Error"] = message;
            return View(new ResetPasswordViewModel { Token = model.Token });
        }

        TempData["Success"] = message;
        return RedirectToAction(nameof(Login));
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult AccessDenied()
    {
        Response.StatusCode = StatusCodes.Status403Forbidden;
        return View("AccessDenied");
    }

    private async Task<LoginViewModel> BuildLoginModelAsync(LoginViewModel model)
    {
        // Dropdown "Login sebagai" hanya untuk testing di Development.
        if (_env.IsDevelopment())
            model.DevUsers = await _auth.GetDevUsersAsync();

        return model;
    }

    private IActionResult RedirectToLocal(string? returnUrl) =>
        !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? LocalRedirect(returnUrl)
            : RedirectToAction("Index", "Dashboard");
}
