using ERP.Web.Models;
using ERP.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Web.Controllers;

/// <summary>
/// Matriks hak akses per role (menu x lihat/tambah/ubah/hapus).
/// </summary>
public class AccessRoleController : Controller
{
    private readonly AuthApiClient _api;

    public AccessRoleController(AuthApiClient api)
    {
        _api = api;
    }

    [HttpGet]
    public async Task<IActionResult> Index(Guid? roleUid)
    {
        return View(await _api.GetAccessRoleMatrixAsync(roleUid));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestFormLimits(ValueCountLimit = 10000)]
    public async Task<IActionResult> Save(SaveAccessRoleViewModel model)
    {
        if (model.RoleUid == Guid.Empty)
        {
            TempData["Error"] = "Role tidak valid.";
            return RedirectToAction(nameof(Index));
        }

        var (success, message) = await _api.SaveAccessRoleAsync(model);

        if (success)
            AccessService.InvalidateAll();

        TempData[success ? "Success" : "Error"] = message;

        return RedirectToAction(nameof(Index), new { roleUid = model.RoleUid });
    }
}
