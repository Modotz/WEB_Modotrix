using ERP.Web.Models;
using ERP.Web.Services;
using Microsoft.AspNetCore.Mvc;
using QuestPDF.Fluent;

namespace ERP.Web.Controllers;

public class ProductCategoriesController : Controller
{
    private readonly ApiClient _api;
    private readonly Translator _t;

    public ProductCategoriesController(ApiClient api, Translator t)
    {
        _api = api;
        _t = t;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        string? search = null,
        long? parentId = null,
        bool? isActive = null,
        int page = 1,
        int pageSize = 10)
    {
        var result =
            await _api.GetProductCategoriesAsync(
                search,
                parentId,
                isActive,
                page,
                pageSize
            );



        ViewBag.Search = search;
        ViewBag.ParentId = parentId;
        ViewBag.IsActive = isActive;

        var allCategories = await _api.GetProductCategoriesAsync(
            null,
            null,
            null,
            1,
            100
            );

        ViewBag.AllCategories = allCategories.Data;

        return View(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetById(Guid uid)
    {
        var category = await _api.GetProductCategoryAsync(uid);

        if (category is null)
            return NotFound();

        return Json(category);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        ProductCategoryViewModel model)
    {
        Console.WriteLine($"CREATE IsActive = {model.IsActive}");

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
            await _api.CreateProductCategoryAsync(model);

        if (!result.Success)
        {
            TempData["Error"] =
                result.Error ??
                "Gagal menambahkan kategori.";

            return RedirectToAction(nameof(Index));
        }

        TempData["Success"] =
            "Kategori berhasil ditambahkan.";

        return RedirectToAction(nameof(Index));
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        ProductCategoryViewModel model)
    {
        Console.WriteLine($"EDIT IsActive = {model.IsActive}");

        // UID WAJIB UNTUK UPDATE (record tidak diidentifikasi dengan id angka)
        if (string.IsNullOrWhiteSpace(model.Uid))
        {
            TempData["Error"] =
                "Kategori tidak ditemukan.";

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
            await _api.UpdateProductCategoryAsync(model);

        if (!result.Success)
        {
            TempData["Error"] =
                result.Error ??
                "Gagal mengubah kategori.";

            return RedirectToAction(nameof(Index));
        }

        TempData["Success"] =
            "Kategori berhasil diubah.";

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid uid)
    {
        var result =
            await _api.DeleteProductCategoryAsync(uid);

        TempData[result.Success
            ? "Success"
            : "Error"] =
            result.Success
                ? "Kategori berhasil dihapus."
                : result.Error ??
                  "Gagal menghapus kategori.";

        return RedirectToAction(nameof(Index));
    }

    private List<ProductCategoryViewModel> OrderCategories(
    IEnumerable<ProductCategoryViewModel> categories)
    {
        var list = categories.ToList();

        var result = new List<ProductCategoryViewModel>();

        var parents = list
            .Where(x => !x.ParentId.HasValue || x.ParentId == 0)
            .ToList();

        foreach (var parent in parents)
        {
            result.Add(parent);

            var children = list
                .Where(x => x.ParentId == parent.Id)
                .ToList();

            result.AddRange(children);
        }

        return result;
    }

    //export-excel
    [HttpGet]
    public async Task<IActionResult> ExportExcel()
    {
        var categories = await _api.GetProductCategoryForExportAsync();

        using var workbook = new ClosedXML.Excel.XLWorkbook();

        var worksheet = workbook.Worksheets.Add("ProductCategories");

        worksheet.Cell(1, 1).Value = "No";
        worksheet.Cell(1, 2).Value = _t["Kode"];
        worksheet.Cell(1, 3).Value = _t["Nama"];
        worksheet.Cell(1, 9).Value = _t["Status"];

        var header = worksheet.Range("A1:I1");

        header.Style.Font.Bold = true;
        header.Style.Alignment.Horizontal =
            ClosedXML.Excel.XLAlignmentHorizontalValues.Center;

        int row = 2;
        int no = 1;

        foreach (var item in categories)
        {
            worksheet.Cell(row, 1).Value = no;
            worksheet.Cell(row, 2).Value = item.Code;
            worksheet.Cell(row, 3).Value = item.Name;
            worksheet.Cell(row, 9).Value =
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
            $"Category_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
        );
    }

    //export-pdf
    [HttpGet]
    public async Task<IActionResult> ExportPdf()
    {
        var category = await _api.GetProductCategoryForExportAsync();
        var listCategories = category.ToList(); // Mengubah ke List untuk mendapatkan Total Count

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
                        text.Span("Category").FontColor("#6c757d");
                        text.Span(" · ").FontColor("#6c757d");
                        text.Span(_t["Dicetak: {0}", DateTime.Now.ToString("dd/MM/yyyy HH:mm")]).FontColor("#6c757d");
                        text.Span(" · ").FontColor("#6c757d");
                        text.Span(_t["Total: {0} data", listCategories.Count]).FontColor("#6c757d");
                    });
                });

                // Isi Tabel
                page.Content().PaddingTop(15).Table(table =>
                {
                    // Definisi Lebar Kolom (Sesuai dengan gambar tanpa kolom Address)
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(35);   // No
                        columns.ConstantColumn(65);   // Code
                        columns.RelativeColumn(2);    // Name
                        columns.ConstantColumn(65);   // Is Active
                    });

                    // Header Tabel (Biru dengan teks Putih)
                    table.Header(header =>
                    {
                        header.Cell().Element(HeaderStyle).Text("No");
                        header.Cell().Element(HeaderStyle).Text("Code");
                        header.Cell().Element(HeaderStyle).Text("Name");
                        header.Cell().Element(HeaderStyle).Text("Is Active");

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
                    foreach (var item in listCategories)
                    {
                        // Latar belakang selang-seling (Zebra striping)
                        string bgBgColor = (no % 2 == 0) ? "#f8f9fa" : "#ffffff";

                        table.Cell().Element(c => Cell(c, bgBgColor)).Text(no.ToString());
                        table.Cell().Element(c => Cell(c, bgBgColor)).Text(item.Code ?? "");
                        table.Cell().Element(c => Cell(c, bgBgColor)).Text(item.Name ?? "");
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
            $"Category_{DateTime.Now:yyyyMMdd_HHmmss}.pdf"
        );
    }

    //print
    [HttpGet]
    public async Task<IActionResult> Print()
    {
        var categories = await _api.GetProductCategoryForExportAsync();

        // Pastikan data tidak null
        categories ??= new List<ProductCategoryViewModel>();

        // Urutkan kategori induk dan subkategori
        var orderedCategories = OrderCategories(categories);

        var model = new PagedResult<ProductCategoryViewModel>
        {
            Data = orderedCategories,
            Page = 1,
            PageSize = orderedCategories.Count,
            TotalCount = orderedCategories.Count,
            TotalData = orderedCategories.Count
        };

        return View(model);
    }
}