using ERP.Web.Models;
using ERP.Web.Services;
using Microsoft.AspNetCore.Mvc;
using QuestPDF.Fluent;
using QuestPDF.Helpers;

namespace ERP.Web.Controllers;

public class SalesForcesController : Controller
{
    private readonly ApiClient _api;
    private readonly Translator _t;

    public SalesForcesController(ApiClient api, Translator t)
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
        var result =
            await _api.GetSalesForcesAsync(
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
        var salesForce =
            await _api.GetSalesForceAsync(uid);

        if (salesForce is null)
            return NotFound();

        return Json(salesForce);
    }


    // =========================
    // CREATE
    // =========================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        SalesForceViewModel model)
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
            await _api.CreateSalesForceAsync(model);

        if (!result.Success)
        {
            TempData["Error"] =
                result.Error ??
                "Gagal menambahkan sales force.";

            return RedirectToAction(nameof(Index));
        }

        TempData["Success"] =
            "Sales force berhasil ditambahkan.";

        return RedirectToAction(nameof(Index));
    }


    // =========================
    // EDIT
    // =========================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        SalesForceViewModel model)
    {
        // UID WAJIB UNTUK UPDATE (record tidak diidentifikasi dengan id angka)
        if (string.IsNullOrWhiteSpace(model.Uid))
        {
            TempData["Error"] =
                "Sales force tidak ditemukan.";

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
            await _api.UpdateSalesForceAsync(model);

        if (!result.Success)
        {
            TempData["Error"] =
                result.Error ??
                "Gagal mengubah sales force.";

            return RedirectToAction(nameof(Index));
        }

        TempData["Success"] =
            "Sales force berhasil diubah.";

        return RedirectToAction(nameof(Index));
    }


    // =========================
    // DELETE
    // =========================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid uid)
    {
        if (uid == Guid.Empty)
        {
            TempData["Error"] =
                "Sales force tidak ditemukan.";

            return RedirectToAction(nameof(Index));
        }

        var result =
            await _api.DeleteSalesForceAsync(uid);

        TempData[result.Success
            ? "Success"
            : "Error"] =
            result.Success
                ? "Sales force berhasil dihapus."
                : result.Error ??
                  "Gagal menghapus sales force.";

        return RedirectToAction(nameof(Index));
    }

    // export-excel
    [HttpGet]
    public async Task<IActionResult> ExportExcel()
    {
        var items = await _api.GetSalesForceForExportAsync();

        using var workbook = new ClosedXML.Excel.XLWorkbook();
        var worksheet = workbook.Worksheets.Add("SalesForces");

        worksheet.Cell(1, 1).Value = "No";
        worksheet.Cell(1, 2).Value = _t["Kode"];
        worksheet.Cell(1, 3).Value = _t["Nama"];
        worksheet.Cell(1, 4).Value = _t["Telepon"];
        worksheet.Cell(1, 5).Value = "Email";
        worksheet.Cell(1, 6).Value = "Area";
        worksheet.Cell(1, 7).Value = "Status";

        var header = worksheet.Range("A1:G1");
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
            worksheet.Cell(row, 4).Value = item.Phone;
            worksheet.Cell(row, 5).Value = item.Email;
            worksheet.Cell(row, 6).Value = item.Area;
            worksheet.Cell(row, 7).Value = item.IsActive ? _t["Aktif"] : _t["Nonaktif"];

            row++;
            no++;
        }

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        return File(
            stream.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"SalesForce_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
    }

    // export-pdf
    [HttpGet]
    public async Task<IActionResult> ExportPdf()
    {
        var list = (await _api.GetSalesForceForExportAsync()).ToList();

        var document = QuestPDF.Fluent.Document.Create(container =>
        {
            container.Page(page =>
            {
                // Landscape karena kolom lebih banyak
                page.Size(QuestPDF.Helpers.PageSizes.A4.Landscape());
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(9.5f).FontColor("#333333"));

                page.Header().Column(col =>
                {
                    col.Item().Text("ERP-LITE").Bold().FontSize(18).FontColor("#1a1a1a");

                    col.Item().PaddingTop(2).Text(text =>
                    {
                        text.Span("Sales Force").FontColor("#6c757d");
                        text.Span(" · ").FontColor("#6c757d");
                        text.Span(_t["Dicetak"] + $": {DateTime.Now:dd/MM/yyyy HH:mm}").FontColor("#6c757d");
                        text.Span(" · ").FontColor("#6c757d");
                        text.Span(_t["Total: {0} data", list.Count]).FontColor("#6c757d");
                    });
                });

                page.Content().PaddingTop(15).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(35);   // No
                        columns.ConstantColumn(70);   // Kode
                        columns.RelativeColumn(2);    // Nama
                        columns.ConstantColumn(100);  // Telepon
                        columns.RelativeColumn(2);    // Email
                        columns.RelativeColumn(1.5f); // Area
                        columns.ConstantColumn(65);   // Status
                    });

                    table.Header(header =>
                    {
                        header.Cell().Element(HeaderStyle).Text("No");
                        header.Cell().Element(HeaderStyle).Text(_t["Kode"]);
                        header.Cell().Element(HeaderStyle).Text(_t["Nama"]);
                        header.Cell().Element(HeaderStyle).Text(_t["Telepon"]);
                        header.Cell().Element(HeaderStyle).Text("Email");
                        header.Cell().Element(HeaderStyle).Text("Area");
                        header.Cell().Element(HeaderStyle).Text("Status");

                        static QuestPDF.Infrastructure.IContainer HeaderStyle(
                            QuestPDF.Infrastructure.IContainer c)
                        {
                            return c.Border(1).BorderColor("#0d6efd").Background("#0d6efd")
                                .PaddingVertical(6).PaddingHorizontal(8)
                                .DefaultTextStyle(x => x.Bold().FontColor("#ffffff"));
                        }
                    });

                    int no = 1;
                    foreach (var item in list)
                    {
                        string bg = (no % 2 == 0) ? "#f8f9fa" : "#ffffff";

                        table.Cell().Element(c => Cell(c, bg)).Text(no.ToString());
                        table.Cell().Element(c => Cell(c, bg)).Text(item.Code ?? "");
                        table.Cell().Element(c => Cell(c, bg)).Text(item.Name ?? "");
                        table.Cell().Element(c => Cell(c, bg)).Text(item.Phone ?? "");
                        table.Cell().Element(c => Cell(c, bg)).Text(item.Email ?? "");
                        table.Cell().Element(c => Cell(c, bg)).Text(item.Area ?? "");
                        table.Cell().Element(c => Cell(c, bg)).Text(item.IsActive ? _t["Aktif"] : _t["Nonaktif"]);

                        no++;
                    }

                    static QuestPDF.Infrastructure.IContainer Cell(
                        QuestPDF.Infrastructure.IContainer c, string backgroundColor)
                    {
                        return c.Border(1).BorderColor("#dee2e6").Background(backgroundColor)
                            .PaddingVertical(6).PaddingHorizontal(8);
                    }
                });

                page.Footer().AlignLeft().PaddingTop(10)
                    .Text(_t["Dokumen dihasilkan otomatis oleh sistem."])
                    .FontSize(8.5f).FontColor("#888888");
            });
        });

        var pdf = document.GeneratePdf();

        return File(pdf, "application/pdf",
            $"SalesForce_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
    }

    // print
    [HttpGet]
    public async Task<IActionResult> Print()
    {
        var items = await _api.GetSalesForceForExportAsync()
                    ?? new List<SalesForceViewModel>();

        var model = new PagedResult<SalesForceViewModel>
        {
            Data = items,
            Page = 1,
            PageSize = items.Count,
            TotalCount = items.Count,
            TotalData = items.Count
        };

        return View(model);
    }
}