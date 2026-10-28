
using ERP.Web.Models;
using ERP.Web.Services;
using Microsoft.AspNetCore.Mvc;
using QuestPDF.Fluent;
using QuestPDF.Helpers;

namespace ERP.Web.Controllers;

public class PriceListsController : Controller
{
    private readonly ApiClient _api;
    private readonly Translator _t;

    public PriceListsController(ApiClient api, Translator t)
    {
        _api = api;
        _t = t;
    }

    // ==========================================
    // INDEX
    // ==========================================
    [HttpGet]
    public async Task<IActionResult> Index(
        string? search = null,
        bool? isActive = null,
        int page = 1,
        int pageSize = 10)
    {
        // Validasi halaman
        page = page < 1 ? 1 : page;

        // Batasi pilihan jumlah data per halaman
        var allowedPageSizes = new[] { 10, 25, 50, 100 };

        if (!allowedPageSizes.Contains(pageSize))
        {
            pageSize = 10;
        }

        // Ambil data dari API
        var result = await _api.GetPriceListsAsync(
            search,
            isActive,
            page,
            pageSize
        );

        if (result.Data != null)
        {
            foreach (var pl in result.Data)
            {
                if (!Guid.TryParse(pl.Uid, out var plUid)) continue;

                var items = await _api.GetPriceListItemsAsync(plUid);
                pl.ItemCount = items.Count;
            }
        }

        return View(result);
    }

    // ==========================================
    // GET BY ID
    // ==========================================
    [HttpGet]
    public async Task<IActionResult> GetById(Guid uid)
    {
        var priceList = await _api.GetPriceListAsync(uid);

        if (priceList == null)
        {
            return NotFound();
        }

        return Json(priceList);
    }

    // ==========================================
    // CREATE
    // ==========================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PriceListViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] =
                "Data tidak valid. Silakan periksa kembali input.";

            return RedirectToAction(nameof(Index));
        }

        var result = await _api.CreatePriceListAsync(model);

        if (!result.Success)
        {
            TempData["Error"] =
                result.Error ?? "Gagal menambahkan price list.";

            return RedirectToAction(nameof(Index));
        }

        TempData["Success"] =
            "Price list berhasil ditambahkan.";

        return RedirectToAction(nameof(Index));
    }

    // ==========================================
    // POST EDIT HEADER
    // ==========================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(PriceListViewModel model)
    {
        // UID WAJIB UNTUK UPDATE (record tidak diidentifikasi dengan id angka)
        if (string.IsNullOrWhiteSpace(model.Uid))
        {
            TempData["Error"] =
                "Price list tidak ditemukan.";

            return RedirectToAction(nameof(Index));
        }

        // Validasi input
        if (!ModelState.IsValid)
        {
            TempData["Error"] =
                "Data tidak valid. Silakan periksa kembali input.";

            return RedirectToAction(nameof(Index));
        }

        var result = await _api.UpdatePriceListAsync(model);

        if (!result.Success)
        {
            TempData["Error"] =
                result.Error ?? "Gagal mengubah price list.";

            return RedirectToAction(nameof(Index));
        }

        TempData["Success"] =
            "Price list berhasil diubah.";

        return RedirectToAction(nameof(Index));
    }

    // ==========================================
    // DELETE
    // ==========================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid uid)
    {
        if (uid == Guid.Empty)
        {
            TempData["Error"] = "Price list tidak ditemukan.";
            return RedirectToAction(nameof(Index));
        }

        var result = await _api.DeletePriceListAsync(uid);

        TempData[result.Success ? "Success" : "Error"] =
            result.Success
                ? "Price list berhasil dihapus."
                : result.Error ?? "Gagal menghapus price list.";

        return RedirectToAction(nameof(Index));
    }

   
    // ==========================================
    // GET EDIT
    // ==========================================

    [HttpGet]
    public async Task<IActionResult> Edit(Guid uid)
    {
        var priceList = uid == Guid.Empty
            ? null
            : await _api.GetPriceListAsync(uid);

        if (priceList == null)
        {
            TempData["Error"] = "Price list tidak ditemukan.";
            return RedirectToAction(nameof(Index));
        }

        var items = await _api.GetPriceListItemsAsync(uid);
        var products = await _api.GetProductsForExportAsync();

        // Isi data produk ke setiap item (cocokkan lewat ProductId)
        var productMap = products
            .GroupBy(p => p.Id)
            .ToDictionary(g => g.Key, g => g.First());

        foreach (var item in items)
        {
            if (!productMap.TryGetValue(item.ProductId, out var p))
                continue;

            item.ProductNumber = p.ProductNumber;
            item.ProductName = p.Name;
            item.Sku = p.Sku;
            item.CategoryName = p.CategoryName;
            item.BrandName = p.BrandName;
            item.ProductType = p.ProductType;
            item.UnitName = p.UnitName;
            item.CostPrice = p.CostPrice;
            item.SellPrice = p.SellPrice;
        }

        // Produk yang sudah ada di list tidak muncul di dropdown
        var usedIds = items.Select(i => i.ProductId).ToHashSet();

        ViewBag.Items = items;
        ViewBag.Products = products.Where(p => !usedIds.Contains(p.Id)).ToList();

        return View(priceList);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddItem(
    [FromBody] PriceListItemViewModel model)
    {
        if (!Guid.TryParse(model.PriceListUid, out var priceListUid) ||
            priceListUid == Guid.Empty)
            return BadRequest(_t["Price List tidak ditemukan."]);

        if (model.ProductId <= 0)
            return BadRequest(_t["Produk belum dipilih."]);

        if (model.Price < 0)
            return BadRequest(_t["Harga tidak valid."]);

        var result = await _api.CreatePriceListItemAsync(model);

        if (!result.Success)
            return BadRequest(_t[result.Error ?? "Gagal menambahkan produk."]);

        return Ok(new
        {
            success = true
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateItem(
    [FromBody] PriceListItemViewModel model)
    {
        if (!Guid.TryParse(model.Uid, out var itemUid) ||
            itemUid == Guid.Empty)
            return BadRequest(_t["Item Price List tidak ditemukan."]);

        if (model.Price < 0)
            return BadRequest(_t["Harga tidak valid."]);

        var result = await _api.UpdatePriceListItemAsync(model);

        if (!result.Success)
            return BadRequest(_t[result.Error ?? "Gagal mengubah harga."]);

        return Ok(new
        {
            success = true
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteItem(
    [FromBody] PriceListItemViewModel model)
    {
        if (!Guid.TryParse(model.Uid, out var itemUid) ||
            itemUid == Guid.Empty)
            return BadRequest(_t["Item Price List tidak ditemukan."]);

        var result = await _api.DeletePriceListItemAsync(itemUid);

        if (!result.Success)
            return BadRequest(_t[result.Error ?? "Gagal menghapus produk."]);

        return Ok(new
        {
            success = true
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteItems([FromBody] DeleteItemsRequest request)
    {
        if (request.Uids == null || request.Uids.Count == 0)
            return BadRequest(_t["Tidak ada item yang dipilih."]);

        var failed = 0;

        foreach (var itemUid in request.Uids)
        {
            var result = await _api.DeletePriceListItemAsync(itemUid);
            if (!result.Success) failed++;
        }

        if (failed > 0)
            return BadRequest(_t["{0} item gagal dihapus.", failed]);

        return Ok(new { success = true });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddAllItems([FromBody] AddAllItemsRequest request)
    {
        if (request.PriceListUid == Guid.Empty)
            return BadRequest(_t["Price List tidak ditemukan."]);

        var products = await _api.GetProductsForExportAsync();
        var existing = await _api.GetPriceListItemsAsync(request.PriceListUid);

        var usedIds = existing.Select(i => i.ProductId).ToHashSet();

        var added = 0;
        var failed = 0;

        foreach (var p in products.Where(p => !usedIds.Contains(p.Id)))
        {
            var result = await _api.CreatePriceListItemAsync(new PriceListItemViewModel
            {
                PriceListUid = request.PriceListUid.ToString(),
                ProductId = p.Id,
                Price = p.SellPrice ?? 0,   // harga default = harga jual produk
                TaxInclusive = false
            });

            if (result.Success) added++; else failed++;
        }

        if (failed > 0)
            return BadRequest(_t["{0} produk ditambahkan, {1} gagal.", added, failed]);

        return Ok(new { success = true, added });
    }

    // export-excel
    [HttpGet]
    public async Task<IActionResult> ExportExcel()
    {
        var items = await _api.GetPriceListForExportAsync();

        using var workbook = new ClosedXML.Excel.XLWorkbook();
        var worksheet = workbook.Worksheets.Add("PriceLists");

        worksheet.Cell(1, 1).Value = _t["No"];
        worksheet.Cell(1, 2).Value = _t["Kode"];
        worksheet.Cell(1, 3).Value = _t["Nama"];
        worksheet.Cell(1, 4).Value = _t["Keterangan"];
        worksheet.Cell(1, 5).Value = _t["Status"];

        var header = worksheet.Range("A1:E1");
        header.Style.Font.Bold = true;
        header.Style.Alignment.Horizontal =
            ClosedXML.Excel.XLAlignmentHorizontalValues.Center;

        int row = 2;
        int no = 1;

        foreach (var item in items)
        {
            worksheet.Cell(row, 1).Value = no;
            worksheet.Cell(row, 2).Value = item.Code;
            worksheet.Cell(row, 3).Value = item.Name;
            worksheet.Cell(row, 4).Value = item.Note;
            worksheet.Cell(row, 5).Value =
                item.IsActive ? _t["Aktif"] : _t["Nonaktif"];

            row++;
            no++;
        }

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        return File(
            stream.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"PriceList_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
    }

    // export-pdf
    [HttpGet]
    public async Task<IActionResult> ExportPdf()
    {
        var list = (await _api.GetPriceListForExportAsync()).ToList();

        var document = QuestPDF.Fluent.Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(QuestPDF.Helpers.PageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(x =>
                    x.FontSize(9.5f).FontColor("#333333"));

                page.Header().Column(col =>
                {
                    col.Item()
                        .Text("ERP-LITE")
                        .Bold()
                        .FontSize(18)
                        .FontColor("#1a1a1a");

                    col.Item()
                        .PaddingTop(2)
                        .Text(text =>
                        {
                            text.Span("Price List")
                                .FontColor("#6c757d");

                            text.Span(" · ")
                                .FontColor("#6c757d");

                            text.Span(
                                _t["Dicetak"] + $": {DateTime.Now:dd/MM/yyyy HH:mm}")
                                .FontColor("#6c757d");

                            text.Span(" · ")
                                .FontColor("#6c757d");

                            text.Span(_t["Total: {0} data", list.Count])
                                .FontColor("#6c757d");
                        });
                });

                page.Content()
                    .PaddingTop(15)
                    .Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(35);  // No
                            columns.ConstantColumn(80);  // Kode
                            columns.RelativeColumn(2);   // Nama
                            columns.RelativeColumn(2);   // Keterangan
                            columns.ConstantColumn(65);  // Status
                        });

                        table.Header(header =>
                        {
                            header.Cell()
                                .Element(HeaderStyle)
                                .Text(_t["No"]);

                            header.Cell()
                                .Element(HeaderStyle)
                                .Text(_t["Kode"]);

                            header.Cell()
                                .Element(HeaderStyle)
                                .Text(_t["Nama"]);

                            header.Cell()
                                .Element(HeaderStyle)
                                .Text(_t["Keterangan"]);

                            header.Cell()
                                .Element(HeaderStyle)
                                .Text(_t["Status"]);

                            static QuestPDF.Infrastructure.IContainer HeaderStyle(
                                QuestPDF.Infrastructure.IContainer c)
                            {
                                return c
                                    .Border(1)
                                    .BorderColor("#0d6efd")
                                    .Background("#0d6efd")
                                    .PaddingVertical(6)
                                    .PaddingHorizontal(8)
                                    .DefaultTextStyle(x =>
                                        x.Bold().FontColor("#ffffff"));
                            }
                        });

                        int no = 1;

                        foreach (var item in list)
                        {
                            string bg =
                                (no % 2 == 0)
                                    ? "#f8f9fa"
                                    : "#ffffff";

                            table.Cell()
                                .Element(c => Cell(c, bg))
                                .Text(no.ToString());

                            table.Cell()
                                .Element(c => Cell(c, bg))
                                .Text(item.Code ?? "");

                            table.Cell()
                                .Element(c => Cell(c, bg))
                                .Text(item.Name ?? "");

                            table.Cell()
                                .Element(c => Cell(c, bg))
                                .Text(item.Note ?? "");

                            table.Cell()
                                .Element(c => Cell(c, bg))
                                .Text(
                                    item.IsActive
                                        ? _t["Aktif"]
                                        : _t["Nonaktif"]);

                            no++;
                        }

                        static QuestPDF.Infrastructure.IContainer Cell(
                            QuestPDF.Infrastructure.IContainer c,
                            string backgroundColor)
                        {
                            return c
                                .Border(1)
                                .BorderColor("#dee2e6")
                                .Background(backgroundColor)
                                .PaddingVertical(6)
                                .PaddingHorizontal(8);
                        }
                    });

                page.Footer()
                    .AlignLeft()
                    .PaddingTop(10)
                    .Text(_t["Dokumen dihasilkan otomatis oleh sistem."])
                    .FontSize(8.5f)
                    .FontColor("#888888");
            });
        });

        var pdf = document.GeneratePdf();

        return File(
            pdf,
            "application/pdf",
            $"PriceList_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
    }

    // print
    [HttpGet]
    public async Task<IActionResult> Print()
    {
        var items = await _api.GetPriceListForExportAsync()
                    ?? new List<PriceListViewModel>();

        var model = new PagedResult<PriceListViewModel>
        {
            Data = items,
            Page = 1,
            PageSize = items.Count,
            TotalCount = items.Count,
            TotalData = items.Count
        };

        return View(model);
    }

    public class DeleteItemsRequest
    {
        // Identitas item yang dihapus (uid, bukan id angka)
        public List<Guid> Uids { get; set; } = new();
    }
    public class AddAllItemsRequest
    {
        // Identitas price list induk (uid, bukan id angka)
        public Guid PriceListUid { get; set; }
    }

}