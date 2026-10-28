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
    private readonly Translator _t;

    public GoodsReceiptsController(ApiClient api, Translator t)
    {
        _api = api;
        _t = t;
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
                message = _t["Nomor PO gagal dibuat."]
            });
        }

        return Json(new { poNo = poNumber });
    }

    [HttpGet]
    public async Task<IActionResult> Create(Guid? poUid = null)
    {
        var model = new GoodsReceiptViewModel
        {
            ReceiptDate = DateOnly.FromDateTime(DateTime.Today),
            Status = "draft"
        };

        await LoadCreateDropdowns();

        // Penerimaan dari PO: PO diidentifikasi lewat uid di URL (?poUid=...).
        // Id angka PO hanya dipakai sebagai nilai FK tersembunyi di form.
        if (poUid.HasValue && poUid.Value != Guid.Empty)
        {
            var po = await _api.GetGoodsPurchaseOrderAsync(poUid.Value);

            if (po == null)
            {
                TempData["Error"] = "Purchase Order tidak ditemukan.";
            }
            else
            {
                model.POId = po.Id;

                // Pastikan PO terpilih (beserta detail barangnya) tersedia di daftar pilihan PO.
                var purchaseOrders =
                    ViewBag.PurchaseOrders as List<GoodsPurchaseOrderViewModel>
                    ?? new List<GoodsPurchaseOrderViewModel>();

                purchaseOrders.RemoveAll(x => x.Id == po.Id);
                purchaseOrders.Insert(0, po);

                ViewBag.PurchaseOrders = purchaseOrders;
            }
        }

        // false = PO manual dengan prefix GPO
        model.ReceiptNo = await _api.GetNextGoodsReceiptNumberAsync(
            model.ReceiptDate,
            false
        ) ?? string.Empty;

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(GoodsReceiptViewModel model, string? submitAction)
    {
        if (!ModelState.IsValid)
        {
            await LoadCreateDropdowns(); 
            return View(model);
        }
         
        model.Status = "draft"; 
        var result = await _api.CreateGoodsReceiptAsync(model);

        if (!result.Success || string.IsNullOrWhiteSpace(result.Uid))
        {
            TempData["Error"] = result.Error ?? "Penerimaan Barang gagal disimpan."; 
            await LoadCreateDropdowns(); 
            return View(model);
        }

        // "Simpan & Posting": langsung posting setelah draft tersimpan.
        TempData["Success"] = await PostIfRequestedAsync(submitAction, result.Uid)
            ?? "Penerimaan Barang disimpan sebagai draft.";

        return RedirectToAction(
            "Details",
            new { uid = result.Uid });
    }

    //detail purchase order
    [HttpGet]
    public async Task<IActionResult> Details(Guid uid)
    {
        var model = uid == Guid.Empty
            ? null
            : await _api.GetGoodsReceiptAsync(uid);

        if (model == null)
        {
            TempData["Error"] = "Penerimaan Barang tidak ditemukan.";
            return RedirectToAction(nameof(Index));
        }

        // Pastikan collection tidak null ketika digunakan di View.
        model.Details ??= new List<GoodsReceiptItemViewModel>();

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid uid)
    {
        var model = uid == Guid.Empty
            ? null
            : await _api.GetGoodsReceiptAsync(uid);

        if (model == null)
        {
            TempData["Error"] = "Penerimaan Barang tidak ditemukan.";
            return RedirectToAction(nameof(Index));
        }

        if (!string.Equals(model.Status, "draft", StringComparison.OrdinalIgnoreCase))
        {
            TempData["Error"] = "Hanya Penerimaan Barang berstatus draft yang dapat diubah.";
            return RedirectToAction(nameof(Details), new { uid });
        }

        model.Details ??= new List<GoodsReceiptItemViewModel>();

        await LoadCreateDropdowns();

        // Form tambah & ubah memakai view yang sama.
        return View("Create", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(
        GoodsReceiptViewModel model,
        string? submitAction)
    {
        // UID wajib untuk update (record tidak diidentifikasi dengan id angka)
        if (string.IsNullOrWhiteSpace(model.Uid))
        {
            TempData["Error"] = "Penerimaan Barang tidak ditemukan.";
            return RedirectToAction(nameof(Index));
        }

        if (!ModelState.IsValid)
        {
            TempData["Error"] =
                "Data Penerimaan Barang belum lengkap atau tidak valid.";

            await LoadCreateDropdowns();

            return View("Create", model);
        }

        model.Status = "draft";
        var result = await _api.UpdateGoodsReceiptAsync(model);

        if (!result.Success)
        {
            TempData["Error"] =
                result.Error ?? "Gagal mengubah Penerimaan Barang.";

            await LoadCreateDropdowns();

            return View("Create", model);
        }

        TempData["Success"] = await PostIfRequestedAsync(submitAction, model.Uid)
            ?? "Penerimaan Barang berhasil diubah.";

        return RedirectToAction(nameof(Details), new { uid = model.Uid });
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid uid)
    {
        if (uid == Guid.Empty)
        {
            TempData["Error"] = "Penerimaan Barang tidak ditemukan.";
            return RedirectToAction(nameof(Index));
        }

        var result =
            await _api.DeleteGoodsReceiptAsync(uid);

        TempData[result.Success ? "Success" : "Error"] =
            result.Message ?? (result.Success
                ? "Penerimaan Barang berhasil dihapus."
                : "Gagal menghapus Penerimaan Barang.");

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(Guid uid)
    {
        if (uid == Guid.Empty)
        {
            TempData["Error"] = "Penerimaan Barang tidak ditemukan.";
            return RedirectToAction(nameof(Index));
        }

        // Posting draft: stok masuk, PO diperbarui, jurnal & faktur otomatis dibuat oleh API.
        var result = await _api.ApproveGoodsReceiptAsync(uid);

        TempData[result.Success ? "Success" : "Error"] =
            result.Message ?? (result.Success
                ? "Penerimaan Barang berhasil diposting."
                : "Penerimaan Barang gagal diposting.");

        return RedirectToAction(nameof(Details), new { uid });
    }

    /// <summary>
    /// Posting draft bila tombol "Simpan &amp; Posting" yang ditekan. Mengembalikan pesan hasil, atau null bila hanya simpan draft.
    /// </summary>
    private async Task<string?> PostIfRequestedAsync(string? submitAction, string? uid)
    {
        if (!string.Equals(submitAction, "post", StringComparison.OrdinalIgnoreCase) ||
            !Guid.TryParse(uid, out var receiptUid))
            return null;

        var result = await _api.ApproveGoodsReceiptAsync(receiptUid);

        return result.Success
            ? result.Message ?? "Penerimaan Barang disimpan dan diposting."
            : $"Penerimaan Barang tersimpan sebagai draft, tetapi gagal diposting. {result.Message}".TrimEnd();
    }

    // Export PDF satu PO Barang
    [HttpGet]
    public async Task<IActionResult> ExportPdf(Guid uid)
    {
         if (uid == Guid.Empty)  return NotFound("Penerimaan Barang tidak ditemukan.");


        var po = await _api.GetGoodsReceiptAsync(uid);

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
                                .Text(_t["PURCHASE ORDER BARANG"])
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

                            Info(_t["Nomor"], po.ReceiptNo ?? "-");
                            Info(_t["Tanggal"], Tanggal(po.ReceiptDate));
                            Info(_t["Supplier"], po.SupplierName ?? "-");
                            Info(_t["Gudang"], po.WarehouseName ?? "-");
                            Info(
                                _t["Status"],
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
                                        .Text(_t[title])
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
                                    .Text(_t["Detail barang tidak tersedia untuk penerimaan ini."]);
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
                                            .Text(_t["CATATAN"])
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
                                                .Text(_t[title])
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
                                                        : _t["(Nama & Tanda Tangan)"]
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
                        $"{_t["Dicetak"]} {DateTime.Now:dd/MM/yyyy HH:mm}"
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
    public async Task<IActionResult> Print(Guid uid)
    {
        var po = uid == Guid.Empty
            ? null
            : await _api.GetGoodsReceiptAsync(uid);

        if (po == null)
        {
            return NotFound("Penerimaan Barang tidak ditemukan.");
        }

        return View("Print", po);
    }
}