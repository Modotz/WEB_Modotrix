using ERP.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Web.Controllers;

/// <summary>Pilihan bahasa UI (Indonesia/Inggris), disimpan di cookie selama 1 tahun.</summary>
[AllowAnonymous]
public class LanguageController : Controller
{
    private readonly Translator _t;

    public LanguageController(Translator t) => _t = t;

    [HttpGet]
    public IActionResult Set(string lang, string? returnUrl)
    {
        var uiCulture = lang == "en" ? "en-US" : "id-ID";

        // Format angka/tanggal untuk parsing form tetap en-US (konsisten); hanya bahasa UI yang berganti.
        Response.Cookies.Append(
            CookieRequestCultureProvider.DefaultCookieName,
            CookieRequestCultureProvider.MakeCookieValue(new RequestCulture("en-US", uiCulture)),
            new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1), IsEssential = true, SameSite = SameSiteMode.Lax });

        return LocalRedirect(Url.IsLocalUrl(returnUrl) ? returnUrl! : "/");
    }

    /// <summary>Daftar teks yang belum diterjemahkan (sejak aplikasi start) — alat bantu melengkapi kamus.</summary>
    [HttpGet]
    [Authorize]
    public IActionResult Missing() => Content(string.Join("\n", _t.Missing), "text/plain; charset=utf-8");
}
