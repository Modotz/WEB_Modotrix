using ERP.Web.Models;
using ERP.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Globalization;

namespace ERP.Web.Controllers;

public class GoodsReceiptsController : Controller
{
    private readonly ApiClient _api;

    public GoodsReceiptsController(ApiClient api)
    {
        _api = api;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        string? search,
        int page = 1,
        int pageSize = 10,
        string? status = null)
    {
        var result = await _api.GetGoodsReceiptsAsync(
            search,
            page,
            pageSize,
            status);

        ViewBag.Search = search;
        ViewBag.Status = status; 
        ViewBag.Suppliers = await _api.GetSuppliersForDropdownAsync();
        ViewBag.Warehouses = await _api.GetWarehousesForDropdownAsync();

        return View(result);
    }

    private async Task LoadCreateDropdowns()
    { 
        var supplierResult = await _api.GetSuppliersAsync(
            search: null,
            isActive: true,
            page: 1,
            pageSize: 100
        );

        ViewBag.Suppliers = supplierResult.Data ?? new List<SupplierViewModel>();
         
        var warehouseResult = await _api.GetWarehousesAsync(
            search: null,
            isDefault: null,
            isActive: true
        );

        if (warehouseResult.Success)
        {
            ViewBag.Warehouses = warehouseResult.Data;
        }
        else
        {
            ViewBag.Warehouses = new List<WarehouseViewModel>();

            ViewBag.WarehouseError = warehouseResult.Error;
        }

        var productResult = await _api.GetProductsAsync(
            search: null,
            isActive: true,
            showVariant: false,
            page: 1,
            pageSize: 100
        );

        ViewBag.Products = productResult.Data ?? new List<ProductViewModel>();
        ViewBag.Units = await _api.GetUnitsForDropdownAsync();

        var poResult = await _api.GetGoodsPurchaseOrdersAsync(
            search: null,
            page: 1,
            pageSize: 100,
            status: null
        );

        var purchaseOrders = (poResult.Data
            ?? new List<GoodsPurchaseOrderViewModel>())
            .Where(po =>
                string.Equals(
                    po.Status,
                    "ordered",
                    StringComparison.OrdinalIgnoreCase
                )
                ||
                string.Equals(
                    po.Status,
                    "partial",
                    StringComparison.OrdinalIgnoreCase
                )
            )
            .ToList();

        ViewBag.PurchaseOrders = purchaseOrders;

    }

    [HttpGet]
    public async Task<IActionResult> NextPONumber(DateOnly poDate)
    {
        var poNumber = await _api.GetNextGoodsPONumberAsync(poDate);

        if (string.IsNullOrWhiteSpace(poNumber))
        {
            return StatusCode(500, new
            {
                message = "Nomor PO gagal dibuat."
            });
        }

        return Json(new { poNo = poNumber });
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var model = new GoodsReceiptViewModel
        {
            ReceiptDate = DateOnly.FromDateTime(DateTime.Today),
            Status = "draft"
        };

        await LoadCreateDropdowns();

        // false = PO manual dengan prefix GPO
        model.ReceiptNo = await _api.GetNextGoodsReceiptNumberAsync(
            model.ReceiptDate,
            false
        ) ?? string.Empty;

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(GoodsReceiptViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await LoadCreateDropdowns(); 
            return View(model);
        }
         
        model.Status = "draft"; 
        var result = await _api.CreateGoodsReceiptAsync(model);

        if (!result.Success || !result.Id.HasValue)
        {
            TempData["Error"] = result.Error ?? "Penerimaan Barang gagal disimpan."; 
            await LoadCreateDropdowns(); 
            return View(model);
        }

        // Berhasil disimpan: langsung buka halaman Detail PO.
        return RedirectToAction(
            "Details",
            new { id = result.Id.Value });
    }

    //detail purchase order
    [HttpGet]
    public async Task<IActionResult> Details(long id)
    {
        if (id <= 0)
            return BadRequest("ID Penerimaan Barang tidak valid.");

        var model = await _api.GetGoodsReceiptAsync(id);

        if (model == null)
            return NotFound("Penerimaan Barang tidak ditemukan.");

        // Pastikan collection tidak null ketika digunakan di View.
        model.Details ??= new List<GoodsReceiptItemViewModel>();

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(long id)
    {
        if (id <= 0)
            return BadRequest("ID Penerimaan Barang tidak valid.");

        var model = await _api.GetGoodsReceiptAsync(id);

        if (model == null)
        {
            TempData["Error"] = "Penerimaan Barang tidak ditemukan.";
            return RedirectToAction(nameof(Index));
        }

        model.Details ??= new List<GoodsReceiptItemViewModel>();

        await LoadCreateDropdowns();

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(
        GoodsReceiptViewModel model)
    {
        if (model.Id <= 0)
            return BadRequest("ID Penerimaan Barang tidak valid.");

        if (!ModelState.IsValid)
        {
            TempData["Error"] =
                "Data Penerimaan Barang belum lengkap atau tidak valid.";

            await LoadCreateDropdowns();

            return View("Edit", model);
        }

        var result = await _api.UpdateGoodsReceiptAsync(model);

        if (!result.Success)
        {
            TempData["Error"] =
                result.Error ?? "Gagal mengubah Penerimaan Barang.";

            await LoadCreateDropdowns();

            return View("Edit", model);
        }

        TempData["Success"] = "Penerimaan Barang berhasil diubah.";

        return RedirectToAction(nameof(Details), new { id = model.Id });
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(long id)
    {
        if (id <= 0)
            return BadRequest("ID Penerimaan Barang tidak valid.");

        var result =
            await _api.DeleteGoodsReceiptAsync(id);

        TempData[result.Success ? "Success" : "Error"] =
            result.Success
                ? "Penerimaan Barang berhasil dihapus."
                : result.Error ?? "Gagal menghapus Penerimaan Barang.";

        return RedirectToAction(nameof(Index));
    }

    // Export PDF satu PO Barang
    [HttpGet]
    public async Task<IActionResult> ExportPdf(long id)
    {
         if (id <= 0)  return BadRequest("ID Penerimaan Barang tidak valid.");


        var po = await _api.GetGoodsReceiptAsync(id);

        if (po == null) return NotFound("Penerimaan Barang tidak ditemukan.");

        var culture = new CultureInfo("id-ID"); 
        string Rupiah(decimal? value) => (value ?? 0).ToString("#,##0.##", culture); 
        string Tanggal(DateOnly? value) => value?.ToString("dd-MM-yyyy") ?? "-";
          
        var details = po.Details ?? new List<GoodsReceiptItemViewModel>();
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(28);
                page.DefaultTextStyle(x => x.FontSize(9));

                page.Header().Column(header =>
                {
                    header.Item().Row(row =>
                    {
                        row.RelativeItem().Column(left =>
                        {
                            left.Item()
                                .Text("ERPLITE")
                                .Bold()
                                .FontSize(27)
                                .FontColor("#0878D1");

                            left.Item()
                                .Text("Jl. Kran V No.20, RT.15/RW.3, Gn. Sahari Sel.,")
                                .FontSize(8)
                                .FontColor("#52627A");

                            left.Item()
                                .Text("Kec. Kemayoran, Kota Jakarta Pusat, DKI Jakarta 10810")
                                .FontSize(8)
                                .FontColor("#52627A");

                            left.Item()
                                .Text("Telp: 021-7875-2345 | Email: admin@erplite.com")
                                .FontSize(8)
                                .FontColor("#52627A");
                        });

                        row.ConstantItem(285).Column(right =>
                        {
                            right.Item()
                                .AlignRight()
                                .Text("PURCHASE ORDER BARANG")
                                .Bold()
                                .FontSize(17)
                                .FontColor("#2864FF");

                            void Info(string label, string value)
                            {
                                right.Item()
                                    .PaddingTop(3)
                                    .Row(r =>
                                    {
                                        r.ConstantItem(70)
                                            .Text(label)
                                            .FontColor("#64748B");

                                        r.RelativeItem()
                                            .AlignRight()
                                            .Text(value)
                                            .Bold();
                                    });
                            }

                            Info("Nomor", po.ReceiptNo ?? "-");
                            Info("Tanggal", Tanggal(po.ReceiptDate));
                            Info("Supplier", po.SupplierName ?? "-");
                            Info("Gudang", po.WarehouseName ?? "-");
                            Info(
                                "Status",
                                (po.Status ?? "-")
                                    .Replace("_", " ")
                                    .ToUpperInvariant()
                            );
                        });
                    });

                    header.Item()
                        .PaddingTop(12)
                        .LineHorizontal(3)
                        .LineColor("#2864FF");
                });

                page.Content()
                    .PaddingTop(15)
                    .Column(content =>
                    {
                        content.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(28);
                                columns.RelativeColumn(3.1f);
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(0.8f);
                                columns.RelativeColumn(1.2f);
                                columns.RelativeColumn(0.9f);
                                columns.RelativeColumn(0.9f);
                                columns.RelativeColumn(1.5f);
                            });

                            table.Header(header =>
                            {
                                string[] titles =
                                {
                                        "NO", "PRODUK", "SATUAN", "QTY",
                                        "HARGA", "DISK", "PAJAK", "SUBTOTAL"
                                };

                                foreach (var title in titles)
                                {
                                    header.Cell()
                                        .Background("#203D63")
                                        .Border(1)
                                        .BorderColor("#203D63")
                                        .Padding(7)
                                        .Text(title)
                                        .Bold()
                                        .FontSize(8)
                                        .FontColor("#FFFFFF");
                                }
                            });

                            static IContainer CellStyle(
                                IContainer cell,
                                string background)
                            {
                                return cell
                                    .Background(background)
                                    .Border(1)
                                    .BorderColor("#D8E1ED")
                                    .Padding(7);
                            }

                            if (details.Count > 0)
                            {
                                for (var i = 0; i < details.Count; i++)
                                {
                                    var item = details[i];
                                    var background = (i + 1) % 2 == 0
                                        ? "#F7F9FC"
                                        : "#FFFFFF";

                                    table.Cell()
                                        .Element(c => CellStyle(c, background))
                                        .AlignCenter()
                                        .Text((i + 1).ToString());

                                    table.Cell()
                                        .Element(c => CellStyle(c, background))
                                        .Text(item.ProductName ?? "-");

                                    table.Cell()
                                        .Element(c => CellStyle(c, background))
                                        .Text(item.UnitCode ?? "-");

                                    table.Cell()
                                        .Element(c => CellStyle(c, background))
                                        .AlignRight()
                                        .Text(item.Qty.ToString("#,##0.###", culture));

                                    //table.Cell()
                                    //    .Element(c => CellStyle(c, background))
                                    //    .AlignRight()
                                    //    .Text(Rupiah(item.UnitCost));
 
                                    table.Cell()
                                        .Element(c => CellStyle(c, background))
                                        .AlignRight()
                                        .Text(Rupiah(item.Subtotal));
                                }
                            }
                            else
                            {
                                table.Cell()
                                    .ColumnSpan(8)
                                    .Border(1)
                                    .BorderColor("#D8E1ED")
                                    .Padding(15)
                                    .AlignCenter()
                                    .Text("Detail barang tidak tersedia untuk PO ini.");
                            }
                        });

                        content.Item()
                            .PaddingTop(14)
                            .Row(row =>
                            {
                                row.RelativeItem()
                                    .PaddingRight(18)
                                    .Column(note =>
                                    {
                                        note.Item()
                                            .Text("CATATAN")
                                            .Bold()
                                            .FontSize(9)
                                            .FontColor("#64748B");

                                        note.Item()
                                            .PaddingTop(5)
                                            .Background("#F5F8FC")
                                            .BorderLeft(3)
                                            .BorderColor("#2864FF")
                                            .Padding(10)
                                            .Text(
                                                string.IsNullOrWhiteSpace(po.Note)
                                                    ? "-"
                                                    : po.Note
                                            );
                                    });

                                row.ConstantItem(270).Table(totals =>
                                {
                                    totals.ColumnsDefinition(columns =>
                                    {
                                        columns.RelativeColumn();
                                        columns.ConstantColumn(115);
                                    });

                                    void TotalRow(string label, string value)
                                    {
                                        totals.Cell()
                                            .BorderBottom(1)
                                            .BorderColor("#DCE4EF")
                                            .Padding(7)
                                            .Text(label);

                                        totals.Cell()
                                            .BorderBottom(1)
                                            .BorderColor("#DCE4EF")
                                            .Padding(7)
                                            .AlignRight()
                                            .Text(value);
                                    }

                                   
                                    totals.Cell()
                                        .ColumnSpan(2)
                                        .Background("#203D63")
                                        .Padding(9)
                                        .Row(r =>
                                        {
                                            r.RelativeItem()
                                                .Text("TOTAL")
                                                .Bold()
                                                .FontSize(11)
                                                .FontColor("#FFFFFF");
                                             
                                        });
                                });
                            });

                        content.Item()
                            .PaddingTop(35)
                            .Row(row =>
                            {
                                foreach (var title in new[]
                                {
                                        "Dibuat oleh",
                                        "Disetujui",
                                        "Supplier"
                                })
                                {
                                    row.RelativeItem()
                                        .PaddingHorizontal(10)
                                        .Column(signature =>
                                        {
                                            signature.Item()
                                                .AlignCenter()
                                                .Text(title)
                                                .Bold();

                                            signature.Item()
                                                .PaddingTop(45)
                                                .BorderBottom(1)
                                                .BorderColor("#64748B");

                                            signature.Item()
                                                .AlignCenter()
                                                .PaddingTop(5)
                                                .Text(
                                                    title == "Dibuat oleh"
                                                        ? "Administrator"
                                                        : "(Nama & Tanda Tangan)"
                                                )
                                                .FontSize(8)
                                                .FontColor("#64748B");
                                        });
                                }
                            });
                    });

                page.Footer()
                    .PaddingTop(8)
                    .BorderTop(1)
                    .BorderColor("#DCE4EF")
                    .AlignCenter()
                    .Text(
                        $"© {DateTime.Now.Year} Modotz App | " +
                        $"Dicetak {DateTime.Now:dd/MM/yyyy HH:mm}"
                    )
                    .FontSize(8)
                    .FontColor("#94A3B8");
            });
        });

        var pdf = document.GeneratePdf();

        return File(
            pdf,
            "application/pdf",
            $"Penerimaan_{po.ReceiptNo}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf"
        );
    }


    //print
    [HttpGet]
    public async Task<IActionResult> Print(long id)
    {
        var po = await _api.GetGoodsReceiptAsync(id);

        if (po == null)
        {
            return NotFound("PO Barang tidak ditemukan.");
        }

        return View("Print", po);
    }
}