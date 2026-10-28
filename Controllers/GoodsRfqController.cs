using Microsoft.AspNetCore.Mvc;
using ERP.Web.Services;
using ERP.Web.ViewModels;
using ERP.Web.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using System.Globalization;

namespace ERP.Web.Controllers;

public class GoodsRfqController : Controller
{
    private readonly ApiClient _api;
    private readonly Translator _t;

    public GoodsRfqController(ApiClient api, Translator t)
    {
        _api = api;
        _t = t;
    }

    // =========================
    // INDEX
    // =========================
    public async Task<IActionResult> Index(
        string? search = null,
        string? status = null,
        int page = 1,
        int pageSize = 10)
    {
        var result = await _api.GetGoodsRfqsAsync(search, status, page, pageSize);

        ViewBag.Search = search;
        ViewBag.Status = status;

        return View(result);
    }

    // =========================
    // CREATE
    // =========================
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await LoadLookupsAsync();

        var model = new GoodsRfqViewModel
        {
            RfqDate = DateTime.Today,
            Items = new List<GoodsRfqItemViewModel> { new() }
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(GoodsRfqViewModel model)
    {
        model.Items = (model.Items ?? new()).Where(i => i.ProductId > 0).ToList();

        if (!model.Items.Any())
            ModelState.AddModelError(string.Empty, "Minimal satu produk harus dipilih.");

        if (!ModelState.IsValid)
        {
            await LoadLookupsAsync();
            if (!model.Items.Any()) model.Items.Add(new());
            return View(model);
        }

        var (success, error, uid) = await _api.CreateGoodsRfqAsync(BuildPayload(model));

        if (!success)
        {
            ModelState.AddModelError(string.Empty, error ?? "Gagal menyimpan RFQ.");
            await LoadLookupsAsync();
            return View(model);
        }

        TempData["Success"] = "RFQ disimpan sebagai draft.";

        return !string.IsNullOrWhiteSpace(uid)
            ? RedirectToAction(nameof(Details), new { uid })
            : RedirectToAction(nameof(Index));
    }

    // =========================
    // EDIT (hanya saat draft, memakai view Create)
    // =========================
    [HttpGet]
    public async Task<IActionResult> Edit(Guid uid)
    {
        var rfq = uid == Guid.Empty ? null : await _api.GetGoodsRfqAsync(uid);
        if (rfq == null)
        {
            TempData["Error"] = "RFQ tidak ditemukan.";
            return RedirectToAction(nameof(Index));
        }

        if (!IsStatus(rfq, "draft"))
        {
            TempData["Error"] = "Hanya RFQ berstatus Draft yang bisa diedit.";
            return RedirectToAction(nameof(Details), new { uid });
        }

        await LoadLookupsAsync();
        ViewBag.IsEdit = true;

        

        if (rfq.Items == null || !rfq.Items.Any())
            rfq.Items = new List<GoodsRfqItemViewModel> { new() };

        return View("Create", rfq);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid uid, GoodsRfqViewModel model)
    {
        // UID wajib untuk update (record tidak diidentifikasi dengan id angka)
        if (uid == Guid.Empty)
        {
            TempData["Error"] = "RFQ tidak ditemukan.";
            return RedirectToAction(nameof(Index));
        }

        model.Uid = uid.ToString();

        model.Items = (model.Items ?? new()).Where(i => i.ProductId > 0).ToList();

        if (!model.Items.Any())
            ModelState.AddModelError(string.Empty, "Minimal satu produk harus dipilih.");

        if (!ModelState.IsValid)
        {
            await LoadLookupsAsync();
            ViewBag.IsEdit = true;
            if (!model.Items.Any()) model.Items.Add(new());
            return View("Create", model);
        }

        var (success, error) = await _api.UpdateGoodsRfqAsync(uid, BuildPayload(model));

        if (!success)
        {
            ModelState.AddModelError(string.Empty, error ?? "Gagal memperbarui RFQ.");
            await LoadLookupsAsync();
            ViewBag.IsEdit = true;
            return View("Create", model);
        }

        TempData["Success"] = "RFQ berhasil diperbarui.";
        return RedirectToAction(nameof(Details), new { uid });
    }

    // =========================
    // DETAILS
    // =========================
    [HttpGet]
    public async Task<IActionResult> Details(Guid uid)
    {
        var rfq = uid == Guid.Empty ? null : await _api.GetGoodsRfqAsync(uid);
        if (rfq == null)
        {
            TempData["Error"] = "RFQ tidak ditemukan.";
            return RedirectToAction(nameof(Index));
        }

        await EnrichItemsAsync(rfq);

        return View(rfq);
    }

    // =========================
    // KIRIM KE SUPPLIER (draft -> terkirim)
    // =========================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Send(Guid uid)
    {
        var (success, error) = await _api.SendGoodsRfqAsync(uid);

        if (success) TempData["Success"] = "RFQ berhasil dikirim ke supplier.";
        else TempData["Error"] = error ?? "Gagal mengirim RFQ.";

        return RedirectToAction(nameof(Details), new { uid });
    }

    // =========================
    // INPUT PENAWARAN (terkirim -> ditawar)
    // =========================
    [HttpGet]
    public async Task<IActionResult> Quote(Guid uid)
    {
        var rfq = uid == Guid.Empty ? null : await _api.GetGoodsRfqAsync(uid);
        if (rfq == null)
        {
            TempData["Error"] = "RFQ tidak ditemukan.";
            return RedirectToAction(nameof(Index));
        }

        if (!IsStatus(rfq, "sent") && !IsStatus(rfq, "quoted"))
        {
            TempData["Error"] = "Penawaran hanya bisa diinput setelah RFQ dikirim ke supplier.";
            return RedirectToAction(nameof(Details), new { uid });
        }

        await EnrichItemsAsync(rfq);

        return View(rfq);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Quote(Guid uid, List<GoodsRfqQuoteItem> items)
    {
        // items[].Id = id baris item yang di-submit bersama dokumen RFQ (boleh id angka).
        var payload = new
        {
            items = (items ?? new()).Select(i => new { id = i.Id, quotedPrice = i.QuotedPrice })
        };

        var (success, error) = await _api.SaveGoodsRfqQuoteAsync(uid, payload);

        if (success) TempData["Success"] = "Penawaran supplier disimpan.";
        else TempData["Error"] = error ?? "Gagal menyimpan penawaran.";

        return RedirectToAction(nameof(Details), new { uid });
    }

    // =========================
    // BATAL
    // =========================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(Guid uid)
    {
        var (success, error) = await _api.CancelGoodsRfqAsync(uid);

        if (success) TempData["Success"] = "RFQ dibatalkan.";
        else TempData["Error"] = error ?? "Gagal membatalkan RFQ.";

        return RedirectToAction(nameof(Details), new { uid });
    }

    // =========================
    // HELPER
    // =========================
    private async Task LoadLookupsAsync()
    {
        ViewBag.Products = await _api.GetGoodsRfqProductsAsync();
        ViewBag.Suppliers = await _api.GetSuppliersForDropdownAsync();
        ViewBag.Units = await _api.GetUnitsForDropdownAsync();
        ViewBag.NextRfqNo = await _api.GetNextGoodsRfqNoAsync();
    }

    private static bool IsStatus(GoodsRfqViewModel rfq, string status) =>
        string.Equals(
            (rfq.Status ?? string.Empty).Trim().Replace(" ", "_"),
            status,
            StringComparison.OrdinalIgnoreCase);

    private static object BuildPayload(GoodsRfqViewModel m) => new
    {
        supplierId = m.SupplierId,
        rfqDate = m.RfqDate,
        validUntil = m.ValidUntil,
        prId = m.PrId,
        note = m.Note,
        items = m.Items.Select(i => new
        {
            productId = i.ProductId,
            unitId = i.UnitId,
            unitQty = i.UnitQty,
            quantity = i.Quantity,
            estPrice = i.EstPrice
        }).ToList()
    };

private async Task EnrichItemsAsync(GoodsRfqViewModel rfq)
    {
        if (rfq.Items == null || !rfq.Items.Any()) return;

        var products = await _api.GetGoodsRfqProductsAsync();
        var units = await _api.GetUnitsForDropdownAsync();

        foreach (var item in rfq.Items)
        {
            if (string.IsNullOrWhiteSpace(item.ProductName))
            {
                var p = products.FirstOrDefault(x => x.ProductId == item.ProductId);
                if (p != null)
                {
                    item.ProductName = p.ProductName;
                    item.Sku = p.Sku;
                }
            }

            if (string.IsNullOrWhiteSpace(item.UnitSymbol))
            {
                var u = units.FirstOrDefault(x => x.Id == item.UnitId);
                if (u != null) item.UnitSymbol = u.Code;
            }
        }
    }

    [HttpGet]
    public async Task<IActionResult> ConvertToPo(Guid uid)
    {
        var rfq = uid == Guid.Empty ? null : await _api.GetGoodsRfqAsync(uid);
        if (rfq == null)
        {
            TempData["Error"] = "RFQ tidak ditemukan.";
            return RedirectToAction(nameof(Index));
        }

        if (!IsStatus(rfq, "quoted") && !IsStatus(rfq, "ditawar"))
        {
            TempData["Error"] = "RFQ harus berstatus Ditawar sebelum dijadikan PO.";
            return RedirectToAction(nameof(Details), new { uid });
        }

        await EnrichItemsAsync(rfq);

        ViewBag.Warehouses = await _api.GetWarehousesForDropdownAsync();

        return View(rfq);
    }

    /// <summary>Buat PO draft dari RFQ (harga = harga penawaran), lalu buka detail PO.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConvertToPo(Guid uid, long warehouseId)
    {
        if (uid == Guid.Empty)
        {
            TempData["Error"] = "RFQ tidak ditemukan.";
            return RedirectToAction(nameof(Index));
        }

        if (warehouseId <= 0)
        {
            TempData["Error"] = "Gudang tujuan wajib dipilih.";
            return RedirectToAction(nameof(ConvertToPo), new { uid });
        }

        var (success, error, poUid) = await _api.ConvertGoodsRfqToPoAsync(uid, warehouseId);

        if (!success || string.IsNullOrWhiteSpace(poUid))
        {
            TempData["Error"] = error ?? "RFQ gagal dijadikan PO.";
            return RedirectToAction(nameof(Details), new { uid });
        }

        TempData["Success"] = "PO draft dibuat dari RFQ. Lengkapi & setujui PO.";
        return RedirectToAction("Details", "GoodsPurchaseOrders", new { uid = poUid });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid uid)
    {
        var (success, error) = await _api.DeleteGoodsRfqAsync(uid);

        if (success)
        {
            TempData["Success"] = "RFQ berhasil dihapus.";
            return RedirectToAction(nameof(Index));
        }

        TempData["Error"] = error ?? "Gagal menghapus RFQ.";
        return RedirectToAction(nameof(Details), new { uid });
    }

    [HttpGet]
    public async Task<IActionResult> Print(Guid uid, bool pdf = false)
    {
        var rfq = uid == Guid.Empty ? null : await _api.GetGoodsRfqAsync(uid);
        if (rfq == null)
        {
            TempData["Error"] = "RFQ tidak ditemukan.";
            return RedirectToAction(nameof(Index));
        }

        await EnrichItemsAsync(rfq);
        ViewBag.Pdf = pdf;

        return View(rfq);
    }

    // =========================
    // EXPORT PDF 
    // =========================
    [HttpGet]
    public async Task<IActionResult> ExportPdf(Guid uid)
    {
        if (uid == Guid.Empty) return NotFound("RFQ tidak ditemukan.");

        var rfq = await _api.GetGoodsRfqAsync(uid);
        if (rfq == null) return NotFound("RFQ tidak ditemukan.");

        await EnrichItemsAsync(rfq);

        var culture = new CultureInfo("id-ID");
        string Rupiah(decimal? value) => (value ?? 0).ToString("#,##0.##", culture);
        string Tanggal(DateTime? value) => value?.ToString("dd-MM-yyyy") ?? "-";

        var items = rfq.Items ?? new List<GoodsRfqItemViewModel>();

        var status = (rfq.Status ?? "draft").Trim().ToLowerInvariant().Replace(" ", "_");

        var statusText = status switch
        {
            "draft" => "DRAFT",
            "sent" or "terkirim" => _t["TERKIRIM"],
            "quoted" or "ditawar" => _t["DITAWAR"],
            "converted" or "jadi_po" => _t["JADI PO"],
            "cancelled" or "batal" => _t["BATAL"],
            _ => (rfq.Status ?? "-").ToUpperInvariant()
        };

        var hasQuote = items.Any(i => i.QuotedPrice > 0);
        var estimateTotal = items.Sum(i => i.Quantity * i.EstPrice);
        var quoteTotal = items.Where(i => i.QuotedPrice > 0)
                              .Sum(i => i.Quantity * i.QuotedPrice);

        var grandLabel = hasQuote ? _t["TOTAL PENAWARAN"] : _t["ESTIMASI TOTAL"];
        var grandValue = hasQuote ? quoteTotal : estimateTotal;

        static QuestPDF.Infrastructure.IContainer CellStyle(
            QuestPDF.Infrastructure.IContainer cell,
            string background)
        {
            return cell
                .Background(background)
                .Border(1)
                .BorderColor("#D8E1ED")
                .Padding(7);
        }

        var document = QuestPDF.Fluent.Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(QuestPDF.Helpers.PageSizes.A4.Landscape());
                page.Margin(28);
                page.DefaultTextStyle(x => x.FontSize(9));

                // ----- Header -----
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
                                .Text("REQUEST FOR QUOTATION")
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

                            Info(_t["Nomor"], rfq.RfqNo ?? "-");
                            Info(_t["Tanggal"], Tanggal(rfq.RfqDate));
                            Info(_t["Berlaku s/d"], Tanggal(rfq.ValidUntil));
                            Info(_t["Supplier"], rfq.SupplierName ?? "-");
                            Info(_t["Status"], statusText);
                        });
                    });

                    header.Item()
                        .PaddingTop(12)
                        .LineHorizontal(3)
                        .LineColor("#2864FF");
                });

                // ----- Isi -----
                page.Content()
                    .PaddingTop(15)
                    .Column(content =>
                    {
                        content.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(28);   // No
                                columns.RelativeColumn(3.4f); // Produk
                                columns.RelativeColumn(1.1f); // Qty
                                columns.RelativeColumn(1.3f); // Est. Harga
                                columns.RelativeColumn(1.5f); // Harga Penawaran
                                columns.RelativeColumn(1.5f); // Subtotal
                            });

                            table.Header(header =>
                            {
                                void HeaderCell(string title, bool alignRight)
                                {
                                    var cell = header.Cell()
                                        .Background("#203D63")
                                        .Border(1)
                                        .BorderColor("#203D63")
                                        .Padding(7);

                                    if (alignRight)
                                    {
                                        cell.AlignRight()
                                            .Text(title)
                                            .Bold()
                                            .FontSize(8)
                                            .FontColor("#FFFFFF");
                                    }
                                    else
                                    {
                                        cell.Text(title)
                                            .Bold()
                                            .FontSize(8)
                                            .FontColor("#FFFFFF");
                                    }
                                }

                                HeaderCell("NO", false);
                                HeaderCell(_t["PRODUK"], false);
                                HeaderCell(_t["QTY"], true);
                                HeaderCell(_t["EST. HARGA"], true);
                                HeaderCell(_t["HARGA PENAWARAN"], true);
                                HeaderCell(_t["SUBTOTAL"], true);
                            });

                            if (items.Count > 0)
                            {
                                for (var i = 0; i < items.Count; i++)
                                {
                                    var item = items[i];
                                    var quoted = item.QuotedPrice > 0;
                                    var background = (i + 1) % 2 == 0
                                        ? "#F7F9FC"
                                        : "#FFFFFF";

                                    table.Cell()
                                        .Element(c => CellStyle(c, background))
                                        .AlignCenter()
                                        .Text((i + 1).ToString());

                                    table.Cell()
                                        .Element(c => CellStyle(c, background))
                                        .Column(cell =>
                                        {
                                            cell.Item().Text(
                                                string.IsNullOrWhiteSpace(item.ProductName)
                                                    ? "-"
                                                    : item.ProductName);

                                            if (!string.IsNullOrWhiteSpace(item.Sku))
                                            {
                                                cell.Item()
                                                    .Text(item.Sku)
                                                    .FontSize(8)
                                                    .FontColor("#64748B");
                                            }
                                        });

                                    table.Cell()
                                        .Element(c => CellStyle(c, background))
                                        .AlignRight()
                                        .Text($"{item.Quantity.ToString("#,##0.###", culture)} {item.UnitSymbol}".Trim());

                                    table.Cell()
                                        .Element(c => CellStyle(c, background))
                                        .AlignRight()
                                        .Text(Rupiah(item.EstPrice));

                                    table.Cell()
                                        .Element(c => CellStyle(c, background))
                                        .AlignRight()
                                        .Text(quoted ? Rupiah(item.QuotedPrice) : "—");

                                    table.Cell()
                                        .Element(c => CellStyle(c, background))
                                        .AlignRight()
                                        .Text(quoted
                                            ? Rupiah(item.Quantity * item.QuotedPrice)
                                            : "—");
                                }
                            }
                            else
                            {
                                table.Cell()
                                    .ColumnSpan(6)
                                    .Border(1)
                                    .BorderColor("#D8E1ED")
                                    .Padding(15)
                                    .AlignCenter()
                                    .Text(_t["Detail barang tidak tersedia untuk RFQ ini."]);
                            }
                        });

                        // ----- Catatan + Total -----
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
                                                string.IsNullOrWhiteSpace(rfq.Note)
                                                    ? "-"
                                                    : rfq.Note);
                                    });

                                row.ConstantItem(270)
                                    .Background("#203D63")
                                    .Padding(9)
                                    .Row(r =>
                                    {
                                        r.RelativeItem()
                                            .Text(grandLabel)
                                            .Bold()
                                            .FontSize(11)
                                            .FontColor("#FFFFFF");

                                        r.ConstantItem(115)
                                            .AlignRight()
                                            .Text($"Rp {Rupiah(grandValue)}")
                                            .Bold()
                                            .FontSize(11)
                                            .FontColor("#FFFFFF");
                                    });
                            });

                        // ----- Tanda tangan -----
                        content.Item()
                            .PaddingTop(35)
                            .Row(row =>
                            {
                                foreach (var title in new[] { "Dibuat oleh", "Supplier" })
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
                                                        : _t["(Nama & Tanda Tangan)"])
                                                .FontSize(8)
                                                .FontColor("#64748B");
                                        });
                                }
                            });
                    });

                // ----- Footer -----
                page.Footer()
                    .PaddingTop(8)
                    .BorderTop(1)
                    .BorderColor("#DCE4EF")
                    .AlignCenter()
                    .Text(
                        $"© {DateTime.Now.Year} Modotz App | " +
                        $"{_t["Dicetak"]} {DateTime.Now:dd/MM/yyyy HH:mm}")
                    .FontSize(8)
                    .FontColor("#94A3B8");
            });
        });

        var pdf = document.GeneratePdf();

        return File(
            pdf,
            "application/pdf",
            $"RFQ_{rfq.RfqNo}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
    }

}

