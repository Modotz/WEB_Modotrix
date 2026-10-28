using ERP.Web.Models;
using ERP.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Web.Controllers;

public class AttributeVariantsController : Controller
{
    private readonly ApiClient _api;
    private readonly Translator _t;

    public AttributeVariantsController(ApiClient api, Translator t)
    {
        _api = api;
        _t = t;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
    Guid attributeUid,
    string? search = null,
    bool? isActive = null,
    int page = 1,
    int pageSize = 10)
    {
        var attribute =
            await _api.GetProductAttributeAsync(attributeUid);

        if (attribute == null)
        {
            TempData["Error"] =
                "Atribut tidak ditemukan.";

            return RedirectToAction(
                "Index",
                "ProductAttributes");
        }

        var result =
            await _api.GetProductVariantsAsync(
                attributeUid,
                search,
                isActive,
                page,
                pageSize
            );

        ViewBag.AttributeUid = attribute.Uid ?? attributeUid.ToString();
        ViewBag.Name = attribute.Name;
        ViewBag.Code = attribute.Code;

        ViewBag.Search = search;
        ViewBag.IsActive = isActive;
        return View("Index", result);
        //return View("~/Views/AttributeVariants/Index.cshtml", result);
    }


    // =========================
    // GET BY UID
    // =========================

    [HttpGet]
    public async Task<IActionResult> GetById(
        Guid attributeUid,
        Guid uid)
    {
        var variant =
            await _api.GetProductVariantAsync(
                attributeUid,
                uid);

        if (variant is null)
            return NotFound();

        return Json(variant);
    }


    // =========================
    // CREATE
    // =========================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        ProductVariantViewModel model)
    {
        if (string.IsNullOrWhiteSpace(model.AttributeUid))
        {
            TempData["Error"] =
                "Atribut tidak ditemukan.";

            return RedirectToAction(
                "Index",
                "ProductAttributes");
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

            return RedirectToAction(
                nameof(Index),
                new
                {
                    attributeUid = model.AttributeUid
                });
        }

        var result =
            await _api.CreateProductVariantAsync(model);

        if (!result.Success)
        {
            TempData["Error"] =
                result.Error ??
                "Gagal menambahkan variant.";

            return RedirectToAction(
                nameof(Index),
                new
                {
                    attributeUid = model.AttributeUid
                });
        }

        TempData["Success"] =
            "Variant berhasil ditambahkan.";

        return RedirectToAction(
            nameof(Index),
            new
            {
                attributeUid = model.AttributeUid
            });
    }


    // =========================
    // EDIT
    // =========================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        ProductVariantViewModel model)
    {
        if (string.IsNullOrWhiteSpace(model.AttributeUid))
        {
            TempData["Error"] =
                "Atribut tidak ditemukan.";

            return RedirectToAction(
                "Index",
                "ProductAttributes");
        }

        // UID WAJIB UNTUK UPDATE (record tidak diidentifikasi dengan id angka)
        if (string.IsNullOrWhiteSpace(model.Uid))
        {
            TempData["Error"] =
                "Variant tidak ditemukan.";

            return RedirectToAction(
                nameof(Index),
                new
                {
                    attributeUid = model.AttributeUid
                });
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

            return RedirectToAction(
                nameof(Index),
                new
                {
                    attributeUid = model.AttributeUid
                });
        }

        var result =
            await _api.UpdateProductVariantAsync(model);

        if (!result.Success)
        {
            TempData["Error"] =
                result.Error ??
                "Gagal mengubah variant.";

            return RedirectToAction(
                nameof(Index),
                new
                {
                    attributeUid = model.AttributeUid
                });
        }

        TempData["Success"] =
            "Variant berhasil diubah.";

        return RedirectToAction(
            nameof(Index),
            new
            {
                attributeUid = model.AttributeUid
            });
    }

    // =========================
    // REORDER
    // =========================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reorder(
    Guid attributeUid,
    [FromBody] List<object> items)
    {
        // items: [{ uid, sortOrder }] (uid varian, bukan id angka)
        var result =
            await _api.ReorderProductVariantsAsync(
                attributeUid,
                items);

        return Json(new
        {
            success = result.Success,
            message = result.Success
                ? _t["Urutan disimpan."]
                : (result.Error is null ? null : _t[result.Error])
        });
    }


    // =========================
    // DELETE
    // =========================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(
        Guid attributeUid,
        Guid uid)
    {
        var result =
            await _api.DeleteProductVariantAsync(
                attributeUid,
                uid);

        TempData[result.Success
            ? "Success"
            : "Error"] =
            result.Success
                ? "Variant berhasil dihapus."
                : result.Error ??
                  "Gagal menghapus variant.";

        return RedirectToAction(
            nameof(Index),
            new
            {
                attributeUid
            });
    }
}
