
using ERP.Web.Models;
using ERP.Web.Services;
using Microsoft.AspNetCore.Mvc;
using QuestPDF.Fluent;
using QuestPDF.Helpers;

namespace ERP.Web.Controllers;

public class PriceListsController : Controller
{
    private readonly ApiClient _api;

    public PriceListsController(ApiClient api)
    {
        _api = api;
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
                if (pl.Id is not long plId) continue;

                var items = await _api.GetPriceListItemsAsync(plId);
                pl.ItemCount = items.Count;
            }
        }

        return View(result);
    }

    // ==========================================
    // GET BY ID
    // ==========================================
    [HttpGet]
    public async Task<IActionResult> GetById(long id)
    {
        var priceList = await _api.GetPriceListAsync(id);

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
        // Validasi ID
        if (!model.Id.HasValue || model.Id.Value <= 0)
        {
            TempData["Error"] =
                "ID price list tidak valid.";

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
    public async Task<IActionResult> Delete(long id)
    {
        var result = await _api.DeletePriceListAsync(id);

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
    public async Task<IActionResult> Edit(long id)
    {
        var priceList = await _api.GetPriceListAsync(id);

        if (priceList == null)
            return NotFound();

        var items = await _api.GetPriceListItemsAsync(id);
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
        if (model.PriceListId <= 0)
            return BadRequest("Price List tidak valid.");

        if (model.ProductId <= 0)
            return BadRequest("Produk belum dipilih.");

        if (model.Price < 0)
            return BadRequest("Harga tidak valid.");

        var result = await _api.CreatePriceListItemAsync(model);

        if (!result.Success)
            return BadRequest(result.Error ?? "Gagal menambahkan produk.");

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
        if (model.Id <= 0)
            return BadRequest("ID item tidak valid.");

        if (model.Price < 0)
            return BadRequest("Harga tidak valid.");

        var result = await _api.UpdatePriceListItemAsync(model);

        if (!result.Success)
            return BadRequest(result.Error ?? "Gagal mengubah harga.");

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
        if (model.Id <= 0)
            return BadRequest("ID item tidak valid.");

        var result = await _api.DeletePriceListItemAsync(model.Id);

        if (!result.Success)
            return BadRequest(result.Error ?? "Gagal menghapus produk.");

        return Ok(new
        {
            success = true
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteItems([FromBody] DeleteItemsRequest request)
    {
        if (request.Ids == null || request.Ids.Count == 0)
            return BadRequest("Tidak ada item yang dipilih.");

        var failed = 0;

        foreach (var id in request.Ids)
        {
            var result = await _api.DeletePriceListItemAsync(id);
            if (!result.Success) failed++;
        }

        if (failed > 0)
            return BadRequest($"{failed} item gagal dihapus.");

        return Ok(new { success = true });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddAllItems([FromBody] AddAllItemsRequest request)
    {
        if (request.PriceListId <= 0)
            return BadRequest("Price List tidak valid.");

        var products = await _api.GetProductsForExportAsync();
        var existing = await _api.GetPriceListItemsAsync(request.PriceListId);

        var usedIds = existing.Select(i => i.ProductId).ToHashSet();

        var added = 0;
        var failed = 0;

        foreach (var p in products.Where(p => !usedIds.Contains(p.Id)))
        {
            var result = await _api.CreatePriceListItemAsync(new PriceListItemViewModel
            {
                PriceListId = request.PriceListId,
                ProductId = p.Id,
                Price = p.SellPrice ?? 0,   // harga default = harga jual produk
                TaxInclusive = false
            });

            if (result.Success) added++; else failed++;
        }

        if (failed > 0)
            return BadRequest($"{added} produk ditambahkan, {failed} gagal.");

        return Ok(new { success = true, added });
    }

    // export-excel
    [HttpGet]
    public async Task<IActionResult> ExportExcel()
    {
        var items = await _api.GetPriceListForExportAsync();

        using var workbook = new ClosedXML.Excel.XLWorkbook();
        var worksheet = workbook.Worksheets.Add("PriceLists");

        worksheet.Cell(1, 1).Value = "No";
        worksheet.Cell(1, 2).Value = "Kode";
        worksheet.Cell(1, 3).Value = "Nama";
        worksheet.Cell(1, 4).Value = "Keterangan";
        worksheet.Cell(1, 5).Value = "Status";

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
                item.IsActive ? "Aktif" : "Nonaktif";

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
                                $"Dicetak: {DateTime.Now:dd/MM/yyyy HH:mm}")
                                .FontColor("#6c757d");

                            text.Span(" · ")
                                .FontColor("#6c757d");

                            text.Span($"Total: {list.Count} data")
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
                                .Text("No");

                            header.Cell()
                                .Element(HeaderStyle)
                                .Text("Kode");

                            header.Cell()
                                .Element(HeaderStyle)
                                .Text("Nama");

                            header.Cell()
                                .Element(HeaderStyle)
                                .Text("Keterangan");

                            header.Cell()
                                .Element(HeaderStyle)
                                .Text("Status");

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
                                        ? "Aktif"
                                        : "Nonaktif");

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
                    .Text("Dokumen dihasilkan otomatis oleh sistem.")
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
        public List<long> Ids { get; set; } = new();
    }
    public class AddAllItemsRequest
    {
        public long PriceListId { get; set; }
    }

}