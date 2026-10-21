using ERP.Web.Models;
using ERP.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Web.Controllers;

public class RolesController : Controller
{
    private readonly ApiClient _api;

    public RolesController(ApiClient api)
    {
        _api = api;
    }
     
    [HttpGet]
    public async Task<IActionResult> Index(
        string? search = null,
        int page = 1,
        int pageSize = 100)
    {
        var result =
            await _api.GetRolesAsync(
                search,
                page,
                pageSize
            );

        ViewBag.Search = search;

        return View(result);
    }
     
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(RoleViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState
                .Where(x =>
                    x.Value != null &&
                    x.Value.Errors.Count > 0)
                .SelectMany(x =>
                    x.Value!
                        .Errors
                        .Select(e =>
                            $"{x.Key}: {e.ErrorMessage}"
                        )
                )
                .ToList();

            TempData["Error"] =
                string.Join(" | ", errors);

            return RedirectToAction(nameof(Index));
        }

        var result =
            await _api.CreateRoleAsync(model);

        if (!result.Success)
        {
            TempData["Error"] =
                result.Error ??
                "Gagal menambahkan role.";

            return RedirectToAction(nameof(Index));
        }

        TempData["Success"] =
            "Role berhasil ditambahkan.";

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> GetById(long id)
    {
        var role = await _api.GetRoleAsync(id);

        if (role == null)
            return NotFound();

        return Json(role);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(RoleViewModel model)
    {
        if (!model.Id.HasValue)
        {
            TempData["Error"] =
                "ID role tidak valid.";

            return RedirectToAction(nameof(Index));
        }

        if (!ModelState.IsValid)
        {
            var errors = ModelState
                .Where(x =>
                    x.Value != null &&
                    x.Value.Errors.Count > 0)
                .SelectMany(x =>
                    x.Value!
                        .Errors
                        .Select(e =>
                            $"{x.Key}: {e.ErrorMessage}"
                        )
                )
                .ToList();

            TempData["Error"] =
                string.Join(" | ", errors);

            return RedirectToAction(nameof(Index));
        }

        var result =
            await _api.UpdateRoleAsync(model);

        if (!result.Success)
        {
            TempData["Error"] =
                result.Error ??
                "Gagal mengubah role.";

            return RedirectToAction(nameof(Index));
        }

        TempData["Success"] =
            "Role berhasil diubah.";

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(long id)
    {
        var result =
            await _api.DeleteRoleAsync(id);

        TempData[result.Success
            ? "Success"
            : "Error"] =
            result.Success
                ? "Role berhasil dihapus."
                : result.Error ??
                  "Gagal menghapus role.";

        return RedirectToAction(nameof(Index));
    }
}
