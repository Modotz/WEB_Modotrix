
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
    [HttpGet]
    public async Task<IActionResult> Index(long id)
    {
        if (id <= 0)
            return RedirectToAction("Index", "Products");

        var json = await _api.GetProductUnitsAsync(id);

        if (json == null ||
            json.Value.ValueKind != JsonValueKind.Object)
        {
            TempData["Error"] = "Gagal mengambil data satuan produk.";
            return RedirectToAction("Index", "Products");
        }

        var data = json.Value;

        var model = new ProductUnitPageViewModel
        {
            ProductId = data.GetProperty("productId").GetInt64(),
            ProductName = data.GetProperty("productName").GetString() ?? "",
            ProductSku = data.TryGetProperty("sku", out var sku)
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

        return View("~/Views/Products/UnitConversion.cshtml", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        CreateProductUnitViewModel model)
    {
        if (model.ProductId <= 0 ||
            model.UnitId <= 0 ||
            model.Conversion <= 0)
        {
            TempData["Error"] = "Data satuan dan konversi harus valid.";

            return RedirectToAction(
                nameof(Index),
                new { id = model.ProductId });
        }

        var success = await _api.CreateProductUnitAsync(model);

        TempData[success ? "Success" : "Error"] = success
            ? "Satuan konversi berhasil ditambahkan."
            : "Gagal menambahkan satuan konversi. Periksa apakah satuan sudah digunakan.";

        return RedirectToAction(
            nameof(Index),
            new { id = model.ProductId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(long id, long productId)
    {
        var result = await _api.DeleteProductUnitAsync(id);

        TempData[result.Success ? "Success" : "Error"] =
            result.Success
                ? "Satuan konversi berhasil dihapus."
                : result.Error ?? "Gagal menghapus satuan konversi.";

        return RedirectToAction(
            nameof(Index),
            new { id = productId });
    }

    // VARIAN - SATUAN
    [HttpGet]
    public async Task<IActionResult> Variant(long id)
    {
        if (id <= 0)
            return RedirectToAction("Index", "Products");

        var json = await _api.GetProductUnitsAsync(id);

        if (json == null || json.Value.ValueKind != JsonValueKind.Object)
        {
            TempData["Error"] = "Gagal mengambil data satuan varian.";
            return RedirectToAction("Index", "ProductVariants");
        }

        var data = json.Value;

        var model = new ProductUnitPageViewModel
        {
            ProductId = data.GetProperty("productId").GetInt64(),
            ProductName = data.GetProperty("productName").GetString() ?? "",
            ProductSku = data.TryGetProperty("sku", out var sku) ? sku.GetString() ?? "" : "",
            BaseUnitId = data.GetProperty("baseUnitId").GetInt64(),
            BaseUnitName = data.GetProperty("baseUnitName").GetString() ?? ""
        };

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

        await LoadAvailableUnitsAsync(model); 
        return View("~/Views/ProductVariants/UnitConversion.cshtml", model);
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
    }

    // TAMBAH SATUAN VARIANT
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateVariant(
        CreateProductUnitViewModel model)
    {
        if (model.ProductId <= 0 ||
            model.UnitId <= 0 ||
            model.Conversion <= 0)
        {
            TempData["Error"] = "Data satuan dan konversi harus valid.";

            return RedirectToAction(
                nameof(Index),
                new { id = model.ProductId });
        }

        var success = await _api.CreateProductUnitAsync(model);

        TempData[success ? "Success" : "Error"] = success
            ? "Satuan konversi berhasil ditambahkan."
            : "Gagal menambahkan satuan konversi. Periksa apakah satuan sudah digunakan.";

        return RedirectToAction(
            nameof(Index),
            new { id = model.ProductId });
    }

    // HAPUS SATUAN Variant
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteVariant(
        long id,
        long productId)
    {
        var result = await _api.DeleteProductUnitAsync(id);

        TempData[result.Success ? "Success" : "Error"] =
            result.Success
                ? "Satuan konversi berhasil dihapus."
                : result.Error ?? "Gagal menghapus satuan konversi.";

        return RedirectToAction(
            nameof(Index),
            new { id = productId });
    }
}
