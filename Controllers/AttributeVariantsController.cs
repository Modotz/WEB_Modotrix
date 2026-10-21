using ERP.Web.Models;
using ERP.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Web.Controllers;

public class AttributeVariantsController : Controller
{
    private readonly ApiClient _api;

    public AttributeVariantsController(ApiClient api)
    {
        _api = api;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
    long attributeId,
    string? search = null,
    bool? isActive = null,
    int page = 1,
    int pageSize = 10)
    {
        var attribute =
            await _api.GetProductAttributeAsync(attributeId);

        if (attribute == null)
        {
            return NotFound();
        }

        var result =
            await _api.GetProductVariantsAsync(
                attributeId,
                search,
                isActive,
                page,
                pageSize
            );

        ViewBag.AttributeId = attributeId;
        ViewBag.Name = attribute.Name;
        ViewBag.Code = attribute.Code;

        ViewBag.Search = search;
        ViewBag.IsActive = isActive;
        return View("Index", result);
        //return View("~/Views/AttributeVariants/Index.cshtml", result);
    }


    // =========================
    // GET BY ID
    // =========================

    [HttpGet]
    public async Task<IActionResult> GetById(
        long attributeId,
        long id)
    {
        var variant =
            await _api.GetProductVariantAsync(
                attributeId,
                id);

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
                    attributeId = model.AttributeId
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
                    attributeId = model.AttributeId
                });
        }

        TempData["Success"] =
            "Variant berhasil ditambahkan.";

        return RedirectToAction(
            nameof(Index),
            new
            {
                attributeId = model.AttributeId
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
        if (!model.Id.HasValue)
        {
            TempData["Error"] =
                "ID variant tidak valid.";

            return RedirectToAction(
                nameof(Index),
                new
                {
                    attributeId = model.AttributeId
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
                    attributeId = model.AttributeId
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
                    attributeId = model.AttributeId
                });
        }

        TempData["Success"] =
            "Variant berhasil diubah.";

        return RedirectToAction(
            nameof(Index),
            new
            {
                attributeId = model.AttributeId
            });
    }

    // =========================
    // REORDER
    // =========================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reorder(
    long attributeId,
    [FromBody] List<object> items)
    {
        var result =
            await _api.ReorderProductVariantsAsync(
                attributeId,
                items);

        return Json(new
        {
            success = result.Success,
            message = result.Success
                ? "Urutan disimpan."
                : result.Error
        });
    }


    // =========================
    // DELETE
    // =========================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(
        long attributeId,
        long id)
    {
        var result =
            await _api.DeleteProductVariantAsync(
                attributeId,
                id);

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
                attributeId
            });
    }
}