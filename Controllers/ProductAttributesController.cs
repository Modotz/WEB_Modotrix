using ERP.Web.Models;
using ERP.Web.Services;
using Microsoft.AspNetCore.Mvc;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ERP.Web.Controllers;

public class ProductAttributesController : Controller
{
    private readonly ApiClient _api;
    private readonly Translator _t;

    public ProductAttributesController(ApiClient api, Translator t)
    {
        _api = api;
        _t = t;
    }

    // =========================
    // INDEX
    // =========================

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

        var result =
            await _api.GetProductAttributesAsync(
                search,
                isActive,
                page,
                pageSize
            );

        ViewBag.Search = search;
        ViewBag.IsActive = isActive;

        return View(result);
    }

    // =========================
    // GET BY ID
    // =========================

    [HttpGet]
    public async Task<IActionResult> GetById(Guid uid)
    {
        var attribute =
            await _api.GetProductAttributeAsync(uid);

        if (attribute is null)
            return NotFound();

        return Json(attribute);
    }

    // =========================
    // CREATE
    // =========================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        ProductAttributeViewModel model)
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
            await _api.CreateProductAttributeAsync(model);

        if (!result.Success)
        {
            TempData["Error"] =
                result.Error ??
                "Gagal menambahkan atribut.";

            return RedirectToAction(nameof(Index));
        }

        TempData["Success"] =
            "Atribut berhasil ditambahkan.";

        return RedirectToAction(nameof(Index));
    }

    // =========================
    // EDIT
    // =========================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        ProductAttributeViewModel model)
    {
        // UID WAJIB UNTUK UPDATE (record tidak diidentifikasi dengan id angka)
        if (string.IsNullOrWhiteSpace(model.Uid))
        {
            TempData["Error"] =
                "Atribut tidak ditemukan.";

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
            await _api.UpdateProductAttributeAsync(model);

        if (!result.Success)
        {
            TempData["Error"] =
                result.Error ??
                "Gagal mengubah atribut.";

            return RedirectToAction(nameof(Index));
        }

        TempData["Success"] =
            "Atribut berhasil diubah.";

        return RedirectToAction(nameof(Index));
    }

    // =========================
    // DELETE
    // =========================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid uid)
    {
        var result =
            await _api.DeleteProductAttributeAsync(uid);

        TempData[result.Success
            ? "Success"
            : "Error"] =
            result.Success
                ? "Atribut berhasil dihapus."
                : result.Error ??
                  "Gagal menghapus atribut.";

        return RedirectToAction(nameof(Index));
    }

    //export-excel
    [HttpGet]
    public async Task<IActionResult> ExportExcel()
    {
        var attributes = await _api.GetProductAttributeForExportAsync();

        using var workbook = new ClosedXML.Excel.XLWorkbook();

        var worksheet = workbook.Worksheets.Add("ProductAttributes");

        worksheet.Cell(1, 1).Value = "No";
        worksheet.Cell(1, 2).Value = _t["Kode"];
        worksheet.Cell(1, 3).Value = _t["Nama Attribut"];
        worksheet.Cell(1, 4).Value = _t["Jml Varian"];
        worksheet.Cell(1, 5).Value = _t["Status"];

        var header = worksheet.Range("A1:E1");

        header.Style.Font.Bold = true;
        header.Style.Alignment.Horizontal =
            ClosedXML.Excel.XLAlignmentHorizontalValues.Center;

        int row = 2;
        int no = 1;

        foreach (var item in attributes)
        {
            worksheet.Cell(row, 1).Value = no;
            worksheet.Cell(row, 2).Value = item.Code ?? "";
            worksheet.Cell(row, 3).Value = item.Name ?? "";
            worksheet.Cell(row, 4).Value = item.VariantCount;
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
            $"ProductAttribute_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
        );
    }

    //export-pdf
    [HttpGet]
    public async Task<IActionResult> ExportPdf()
    {
        var attributes = await _api.GetProductAttributeForExportAsync();
        var listAttributes = attributes.ToList(); // Mengubah ke List untuk mendapatkan Total Count

        var document = QuestPDF.Fluent.Document.Create(container =>
        {
            container.Page(page =>
            {
                // Set orientasi ke Landscape agar kolom pas
                page.Size(QuestPDF.Helpers.PageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(9.5f).FontColor("#333333"));

                // Header Dokumen
                page.Header().Column(col =>
                {
                    col.Item().Text("ERP-LITE")
                        .Bold()
                        .FontSize(18)
                        .FontColor("#1a1a1a");

                    col.Item().PaddingTop(2).Text(text =>
                    {
                        text.Span("Product Attribute").FontColor("#6c757d");
                        text.Span(" · ").FontColor("#6c757d");
                        text.Span(_t["Dicetak: {0}", DateTime.Now.ToString("dd/MM/yyyy HH:mm")]).FontColor("#6c757d");
                        text.Span(" · ").FontColor("#6c757d");
                        text.Span(_t["Total: {0} data", listAttributes.Count]).FontColor("#6c757d");
                    });
                });

                // Isi Tabel
                page.Content().PaddingTop(15).Table(table =>
                {
                    // Definisi Lebar Kolom (Sesuai dengan gambar tanpa kolom Address)
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(35);   // No
                        columns.ConstantColumn(75);   // Kode
                        columns.RelativeColumn(2);    // Nama
                        columns.ConstantColumn(75);   // Jml varian
                        columns.ConstantColumn(70);   // Status
                    });

                    // Header Tabel (Biru dengan teks Putih)
                    table.Header(header =>
                    {
                        header.Cell().Element(HeaderStyle).Text("No");
                        header.Cell().Element(HeaderStyle).Text(_t["kode"]);
                        header.Cell().Element(HeaderStyle).Text(_t["Nama Attribut"]);
                        header.Cell().Element(HeaderStyle).Text(_t["Jml Varian"]);
                        header.Cell().Element(HeaderStyle).Text(_t["status"]);

                        static QuestPDF.Infrastructure.IContainer HeaderStyle(
                            QuestPDF.Infrastructure.IContainer c)
                        {
                            return c
                                .Border(1)
                                .BorderColor("#0d6efd")
                                .Background("#0d6efd") // Warna biru utama
                                .PaddingVertical(6)
                                .PaddingHorizontal(8)
                                .DefaultTextStyle(x => x.Bold().FontColor("#ffffff"));
                        }
                    });

                    // Data Baris Tabel
                    int no = 1;
                    foreach (var item in listAttributes)
                    {
                        // Latar belakang selang-seling (Zebra striping)
                        string bgBgColor = (no % 2 == 0) ? "#f8f9fa" : "#ffffff";

                        table.Cell().Element(c => Cell(c, bgBgColor)).Text(no.ToString());
                        table.Cell().Element(c => Cell(c, bgBgColor)).Text(item.Code ?? "");
                        table.Cell().Element(c => Cell(c, bgBgColor)).Text(item.Name ?? "");
                        table.Cell().Element(c => Cell(c, bgBgColor)).Text(item.VariantCount.ToString());
                        table.Cell().Element(c => Cell(c, bgBgColor)).Text(item.IsActive ? _t["Aktif"] : _t["Nonaktif"]);

                        no++;
                    }

                    static QuestPDF.Infrastructure.IContainer Cell(
                        QuestPDF.Infrastructure.IContainer c, string backgroundColor)
                    {
                        return c
                            .Border(1)
                            .BorderColor("#dee2e6") // Warna border abu-abu halus
                            .Background(backgroundColor)
                            .PaddingVertical(6)
                            .PaddingHorizontal(8);
                    }
                });

                // Footer Dokumen
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
            $"ProductAttribute_{DateTime.Now:yyyyMMdd_HHmmss}.pdf"
        );
    }

    //print
    [HttpGet]
    public async Task<IActionResult> Print()
    {
        var attributes = await _api.GetProductAttributeForExportAsync();

        // Pastikan data tidak null
        attributes ??= new List<ProductAttributeViewModel>();

        var model = new PagedResult<ProductAttributeViewModel>
        {
            Data = attributes,
            Page = 1,
            PageSize = attributes.Count,
            TotalCount = attributes.Count,
            TotalData = attributes.Count
        };

        return View(model);
    }
}