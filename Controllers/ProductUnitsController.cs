
using ERP.Web.Models;
using ERP.Web.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace ERP.Web.Controllers;

public class ProductUnitsController : Controller
{
    private readonly ApiClient _api;

    public ProductUnitsController(ApiClient api)
    {
        _api = api;
    }

    //PRODUK - SATUAN
    // uid = uid PRODUK (bukan uid satuan konversi)
    [HttpGet]
    public async Task<IActionResult> Index(Guid uid)
    {
        if (uid == Guid.Empty)
        {
            TempData["Error"] = "Produk tidak ditemukan.";
            return RedirectToAction("Index", "Products");
        }

        var json = await _api.GetProductUnitsAsync(uid);

        if (json == null ||
            json.Value.ValueKind != JsonValueKind.Object)
        {
            TempData["Error"] = "Gagal mengambil data satuan produk.";
            return RedirectToAction("Index", "Products");
        }

        var model = BuildPageModel(json.Value);

        // AMBIL DATA SATUAN UNTUK DROPDOWN.
        try
        {
            var unitResult = await _api.GetUnitsAsync(
                page: 1,
                pageSize: 100000);

            ViewBag.Units = unitResult.Data
                .Select(x => new DropdownItemViewModel
                {
                    Id = x.Id,
                    Name = x.Name
                })
                .ToList();
        }
        catch (Exception ex)
        {
            ViewBag.Units = new List<DropdownItemViewModel>();

            TempData["Error"] =
                $"Gagal mengambil daftar satuan: {ex.Message}";
        }

        ViewBag.ProductId = model.ProductId;
        ViewBag.ProductUid = model.ProductUid;

        return View("~/Views/Products/UnitConversion.cshtml", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        CreateProductUnitViewModel model)
    {
        if (model.ProductUid == Guid.Empty)
        {
            TempData["Error"] = "Produk tidak ditemukan.";
            return RedirectToAction("Index", "Products");
        }

        if (model.UnitId <= 0 ||
            model.Conversion <= 0)
        {
            TempData["Error"] = "Data satuan dan konversi harus valid.";

            return RedirectToAction(
                nameof(Index),
                new { uid = model.ProductUid });
        }

        var success = await _api.CreateProductUnitAsync(model);

        TempData[success ? "Success" : "Error"] = success
            ? "Satuan konversi berhasil ditambahkan."
            : "Gagal menambahkan satuan konversi. Periksa apakah satuan sudah digunakan.";

        return RedirectToAction(
            nameof(Index),
            new { uid = model.ProductUid });
    }

    // uid = uid satuan konversi, productUid = uid produk (untuk kembali ke halaman)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid uid, Guid productUid)
    {
        var result = await _api.DeleteProductUnitAsync(uid);

        TempData[result.Success ? "Success" : "Error"] =
            result.Success
                ? "Satuan konversi berhasil dihapus."
                : result.Error ?? "Gagal menghapus satuan konversi.";

        if (productUid == Guid.Empty)
            return RedirectToAction("Index", "Products");

        return RedirectToAction(
            nameof(Index),
            new { uid = productUid });
    }

    // VARIAN - SATUAN
    // uid = uid PRODUK VARIAN
    [HttpGet]
    public async Task<IActionResult> Variant(Guid uid)
    {
        if (uid == Guid.Empty)
        {
            TempData["Error"] = "Produk tidak ditemukan.";
            return RedirectToAction("Index", "Products");
        }

        var json = await _api.GetProductUnitsAsync(uid);

        if (json == null || json.Value.ValueKind != JsonValueKind.Object)
        {
            TempData["Error"] = "Gagal mengambil data satuan varian.";
            return RedirectToAction("Index", "Products");
        }

        var model = BuildPageModel(json.Value);

        await LoadAvailableUnitsAsync(model);
        return View("~/Views/ProductVariants/UnitConversion.cshtml", model);
    }

    // SUSUN MODEL HALAMAN DARI RESPONS API
    private static ProductUnitPageViewModel BuildPageModel(JsonElement data)
    {
        var model = new ProductUnitPageViewModel
        {
            ProductId = data.GetProperty("productId").GetInt64(),
            ProductUid = data.TryGetProperty("productUid", out var productUid) &&
                         productUid.ValueKind == JsonValueKind.String
                ? productUid.GetString()
                : null,
            ParentUid = data.TryGetProperty("parentUid", out var parentUid) &&
                        parentUid.ValueKind == JsonValueKind.String
                ? parentUid.GetString()
                : null,
            ProductName = data.GetProperty("productName").GetString() ?? "",
            ProductSku = data.TryGetProperty("sku", out var sku) &&
                         sku.ValueKind == JsonValueKind.String
                ? sku.GetString() ?? ""
                : "",
            BaseUnitId = data.GetProperty("baseUnitId").GetInt64(),
            BaseUnitName = data.GetProperty("baseUnitName").GetString() ?? ""
        };

        // Ambil satuan konversi yang sudah digunakan produk.
        if (data.TryGetProperty("units", out var units))
        {
            model.Units =
                JsonSerializer.Deserialize<List<ProductUnitViewModel>>(
                    units.GetRawText(),
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    })
                ?? new List<ProductUnitViewModel>();
        }

        return model;
    }

    // DATA DROPDOWN SATUAN
    private async Task LoadAvailableUnitsAsync(
        ProductUnitPageViewModel model)
    {
        try
        {
            var unitResult = await _api.GetUnitsAsync(
                page: 1,
                pageSize: 100000);

            ViewBag.Units = unitResult.Data
                .Select(x => new DropdownItemViewModel
                {
                    Id = x.Id,
                    Name = x.Name
                })
                .ToList();
        }
        catch (Exception ex)
        {
            ViewBag.Units = new List<DropdownItemViewModel>();
            TempData["Error"] = $"Gagal mengambil daftar satuan: {ex.Message}";
        }

        ViewBag.ProductId = model.ProductId;
        ViewBag.ProductUid = model.ProductUid;
    }

    // TAMBAH SATUAN VARIANT
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateVariant(
        CreateProductUnitViewModel model)
    {
        if (model.ProductUid == Guid.Empty)
        {
            TempData["Error"] = "Produk tidak ditemukan.";
            return RedirectToAction("Index", "Products");
        }

        if (model.UnitId <= 0 ||
            model.Conversion <= 0)
        {
            TempData["Error"] = "Data satuan dan konversi harus valid.";

            return RedirectToAction(
                nameof(Index),
                new { uid = model.ProductUid });
        }

        var success = await _api.CreateProductUnitAsync(model);

        TempData[success ? "Success" : "Error"] = success
            ? "Satuan konversi berhasil ditambahkan."
            : "Gagal menambahkan satuan konversi. Periksa apakah satuan sudah digunakan.";

        return RedirectToAction(
            nameof(Index),
            new { uid = model.ProductUid });
    }

    // HAPUS SATUAN Variant
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteVariant(
        Guid uid,
        Guid productUid)
    {
        var result = await _api.DeleteProductUnitAsync(uid);

        TempData[result.Success ? "Success" : "Error"] =
            result.Success
                ? "Satuan konversi berhasil dihapus."
                : result.Error ?? "Gagal menghapus satuan konversi.";

        if (productUid == Guid.Empty)
            return RedirectToAction("Index", "Products");

        return RedirectToAction(
            nameof(Index),
            new { uid = productUid });
    }
}
