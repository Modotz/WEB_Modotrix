using ERP.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Web.Controllers;

/// <summary>
/// Pengaturan &gt; Peta Data: daftar tabel database, relasi, dan peta Menu → Tabel.
/// </summary>
public class SchemaMapController : Controller
{
    private readonly SettingsApiClient _api;

    public SchemaMapController(SettingsApiClient api)
    {
        _api = api;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        return View(await _api.GetSchemaMapAsync());
    }

    [HttpGet]
    public async Task<IActionResult> Table(string name)
    {
        var detail = string.IsNullOrWhiteSpace(name) ? null : await _api.GetSchemaTableAsync(name);

        if (detail == null)
        {
            TempData["Error"] = "Tabel tidak ditemukan.";
            return RedirectToAction(nameof(Index));
        }

        return View(detail);
    }
}
