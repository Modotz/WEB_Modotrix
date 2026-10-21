using ERP.Web.Models;
using ERP.Web.Services;
using Microsoft.AspNetCore.Mvc;
using QuestPDF.Fluent;
using QuestPDF.Helpers;

namespace ERP.Web.Controllers;

public class WorkCentersController : Controller
{
    private readonly ApiClient _api;

    public WorkCentersController(ApiClient api)
    {
        _api = api;
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
            await _api.GetWorkCentersAsync(
                search,
                isActive,
                page,
                pageSize);

        ViewBag.Search = search;
        ViewBag.IsActive = isActive;

        return View(result);
    }

    // =========================
    // GET BY ID
    // =========================

    [HttpGet]
    public async Task<IActionResult> GetById(long id)
    {
        var data =
            await _api.GetWorkCenterAsync(id);

        if (data is null)
            return NotFound();

        return Json(data);
    }

    // =========================
    // CREATE
    // =========================

    [HttpGet]
    public IActionResult Create()
    {
        return View(
            new WorkCenterViewModel
            {
                IsActive = true,
                CostPerHour = 0
            });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        WorkCenterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result =
            await _api.CreateWorkCenterAsync(model);

        if (!result.Success)
        {
            ModelState.AddModelError(
                "",
                result.Error ??
                "Gagal menambahkan Work Center.");

            return View(model);
        }

        TempData["Success"] =
            "Work Center berhasil ditambahkan.";

        return RedirectToAction(nameof(Index));
    }

    // =========================
    // EDIT
    // =========================

    [HttpGet]
    public async Task<IActionResult> Edit(long id)
    {
        var data =
            await _api.GetWorkCenterAsync(id);

        if (data is null)
            return NotFound();

        return View(data);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        WorkCenterViewModel model)
    {
        if (!model.Id.HasValue)
        {
            ModelState.AddModelError(
                "",
                "ID Work Center tidak valid.");

            return View(model);
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result =
            await _api.UpdateWorkCenterAsync(model);

        if (!result.Success)
        {
            ModelState.AddModelError(
                "",
                result.Error ??
                "Gagal mengubah Work Center.");

            return View(model);
        }

        TempData["Success"] =
            "Work Center berhasil diubah.";

        return RedirectToAction(nameof(Index));
    }

    // =========================
    // DELETE
    // =========================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(long id)
    {
        var result =
            await _api.DeleteWorkCenterAsync(id);

        TempData[result.Success
            ? "Success"
            : "Error"] =
            result.Success
                ? "Work Center berhasil dihapus."
                : result.Error ??
                  "Gagal menghapus Work Center.";

        return RedirectToAction(nameof(Index));
    }

    // export-excel
    [HttpGet]
    public async Task<IActionResult> ExportExcel()
    {
        var items = await _api.GetWorkCenterForExportAsync();

        using var workbook = new ClosedXML.Excel.XLWorkbook();
        var worksheet = workbook.Worksheets.Add("WorkCenters");

        worksheet.Cell(1, 1).Value = "No";
        worksheet.Cell(1, 2).Value = "Kode";
        worksheet.Cell(1, 3).Value = "Nama";
        worksheet.Cell(1, 4).Value = "Tarif / Jam";
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
            worksheet.Cell(row, 4).Value = item.CostPerHour;
            worksheet.Cell(row, 4).Style.NumberFormat.Format = "#,##0";
            worksheet.Cell(row, 5).Value = item.IsActive ? "Aktif" : "Nonaktif";

            row++;
            no++;
        }

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        return File(
            stream.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"WorkCenter_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
    }

    // export-pdf
    [HttpGet]
    public async Task<IActionResult> ExportPdf()
    {
        var list = (await _api.GetWorkCenterForExportAsync()).ToList();

        var document = QuestPDF.Fluent.Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(QuestPDF.Helpers.PageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(9.5f).FontColor("#333333"));

                page.Header().Column(col =>
                {
                    col.Item().Text("ERP-LITE").Bold().FontSize(18).FontColor("#1a1a1a");

                    col.Item().PaddingTop(2).Text(text =>
                    {
                        text.Span("Work Center").FontColor("#6c757d");
                        text.Span(" · ").FontColor("#6c757d");
                        text.Span($"Dicetak: {DateTime.Now:dd/MM/yyyy HH:mm}").FontColor("#6c757d");
                        text.Span(" · ").FontColor("#6c757d");
                        text.Span($"Total: {list.Count} data").FontColor("#6c757d");
                    });
                });

                page.Content().PaddingTop(15).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(35);   // No
                        columns.ConstantColumn(80);   // Kode
                        columns.RelativeColumn(2);    // Nama
                        columns.ConstantColumn(90);   // Tarif / Jam
                        columns.ConstantColumn(65);   // Status
                    });

                    table.Header(header =>
                    {
                        header.Cell().Element(HeaderStyle).Text("No");
                        header.Cell().Element(HeaderStyle).Text("Kode");
                        header.Cell().Element(HeaderStyle).Text("Nama");
                        header.Cell().Element(HeaderStyle).AlignRight().Text("Tarif / Jam");
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
                        table.Cell().Element(c => Cell(c, bg)).AlignRight().Text(item.CostPerHour.ToString("N0"));
                        table.Cell().Element(c => Cell(c, bg)).Text(item.IsActive ? "Aktif" : "Nonaktif");

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
                    .Text("Dokumen dihasilkan otomatis oleh sistem.")
                    .FontSize(8.5f).FontColor("#888888");
            });
        });

        var pdf = document.GeneratePdf();

        return File(pdf, "application/pdf",
            $"WorkCenter_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
    }

    // print
    [HttpGet]
    public async Task<IActionResult> Print()
    {
        var items = await _api.GetWorkCenterForExportAsync()
                    ?? new List<WorkCenterViewModel>();

        var model = new PagedResult<WorkCenterViewModel>
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