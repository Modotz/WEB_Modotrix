using ERP.Web.Models;
using ERP.Web.Services;
using Microsoft.AspNetCore.Mvc;
using QuestPDF.Fluent;

namespace ERP.Web.Controllers;

public class ShippingAreasController : Controller
{
    private readonly ApiClient _api;

    public ShippingAreasController(ApiClient api)
    {
        _api = api;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
    string? search = null,
    bool? isActive = null,
    int page = 1,
    int pageSize = 10)
    {
        var result =
            await _api.GetShippingAreasAsync(
                search,
                isActive,
                page,
                pageSize
            );


        return View(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetById(long id)
    {
        var shippingArea = await _api.GetShippingAreaAsync(id);

        if (shippingArea is null)
            return NotFound();

        return Json(shippingArea);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ShippingAreaViewModel model)
    {
        if (string.IsNullOrWhiteSpace(model.Code))
        {
            ModelState.AddModelError(
                nameof(model.Code),
                "Kode wajib diisi."
            );
        }

        if (!ModelState.IsValid)
        {
            var errors = ModelState
                .Where(x => x.Value != null && x.Value.Errors.Count > 0)
                .Select(x => new
                {
                    Field = x.Key,
                    Errors = x.Value!.Errors
                        .Select(e => e.ErrorMessage)
                        .ToList()
                })
                .ToList();

            foreach (var error in errors)
            {
                Console.WriteLine(
                    $"FIELD ERROR: {error.Field} -> " +
                    string.Join(", ", error.Errors)
                );
            }

            TempData["Error"] = string.Join(
                " | ",
                errors.SelectMany(x =>
                    x.Errors.Select(e => $"{x.Field}: {e}")
                )
            );

            return RedirectToAction(nameof(Index));
        }

        var result = await _api.CreateShippingAreaAsync(model);

        if (!result.Success)
        {
            TempData["Error"] =
                result.Error ?? "Gagal menambahkan area pengiriman.";

            return RedirectToAction(nameof(Index));
        }

        TempData["Success"] =
            "Area pengiriman berhasil ditambahkan.";

        return RedirectToAction(nameof(Index));
    }


    // EDIT di MVC juga POST.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ShippingAreaViewModel model)
    {
        ModelState.Remove(nameof(model.Code));

        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Data area pengiriman belum lengkap.";
            return RedirectToAction(nameof(Index));
        }

        var result = await _api.UpdateShippingAreaAsync(model);

        TempData[result.Success ? "Success" : "Error"] =
            result.Success
                ? "Area pengiriman berhasil diubah."
                : result.Error ?? "Gagal mengubah area pengiriman.";

        return RedirectToAction(nameof(Index));
    }

    // DELETE di MVC juga POST.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(long id)
    {
        var result = await _api.DeleteShippingAreaAsync(id);

        TempData[result.Success ? "Success" : "Error"] =
            result.Success
                ? "Area pengiriman berhasil dihapus."
                : result.Error ?? "Gagal menghapus area pengiriman.";

        return RedirectToAction(nameof(Index));
    }

    //export-excel
    [HttpGet]
    public async Task<IActionResult> ExportExcel()
    {
        var shippingAreas = await _api.GetShippingAreasForExportAsync();

        using var workbook = new ClosedXML.Excel.XLWorkbook();

        var worksheet = workbook.Worksheets.Add("Shipping Area");

        worksheet.Cell(1, 1).Value = "No";
        worksheet.Cell(1, 2).Value = "Kode";
        worksheet.Cell(1, 3).Value = "Nama";
        worksheet.Cell(1, 4).Value = "Harga";
        worksheet.Cell(1, 5).Value = "Deskripsi";
        worksheet.Cell(1, 6).Value = "Aktif";

        var header = worksheet.Range("A1:F1");

        header.Style.Font.Bold = true;
        header.Style.Alignment.Horizontal =
            ClosedXML.Excel.XLAlignmentHorizontalValues.Center;

        int row = 2;
        int no = 1;

        foreach (var item in shippingAreas)
        {
            worksheet.Cell(row, 1).Value = no;
            worksheet.Cell(row, 2).Value = item.Code;
            worksheet.Cell(row, 3).Value = item.Name;
            worksheet.Cell(row, 4).Value = item.Price;
            worksheet.Cell(row, 5).Value = item.Description;
            worksheet.Cell(row, 6).Value = item.IsActive;

            row++;
            no++;
        }

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();

        workbook.SaveAs(stream);

        return File(
            stream.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"ShippingArea_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
        );
    }

    //export-pdf
    [HttpGet]
    public async Task<IActionResult> ExportPdf()
    {
        var shippingAreas = await _api.GetShippingAreasForExportAsync();
        var listShippingAreas = shippingAreas.ToList();

        var document = QuestPDF.Fluent.Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(QuestPDF.Helpers.PageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(9.5f).FontColor("#333333"));

                page.Header().Column(col =>
                {
                    col.Item().Text("ERP-LITE")
                        .Bold()
                        .FontSize(18)
                        .FontColor("#1a1a1a");

                    col.Item().PaddingTop(2).Text(text =>
                    {
                        text.Span("Shipping Area").FontColor("#6c757d");
                        text.Span(" · ").FontColor("#6c757d");
                        text.Span($"Dicetak: {DateTime.Now:dd/MM/yyyy HH:mm}").FontColor("#6c757d");
                        text.Span(" · ").FontColor("#6c757d");
                        text.Span($"Total: {listShippingAreas.Count} data").FontColor("#6c757d");
                    });
                });

                page.Content().PaddingTop(15).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(35); // No
                        columns.ConstantColumn(65); // Code
                        columns.RelativeColumn(2);  // Name
                        columns.ConstantColumn(75); // Price
                        columns.RelativeColumn(3);  // Description
                        columns.ConstantColumn(55); // IsActive
                    });

                    // Header Tabel
                    table.Header(header =>
                    {
                        header.Cell().Element(HeaderStyle).Text("No");
                        header.Cell().Element(HeaderStyle).Text("Code");
                        header.Cell().Element(HeaderStyle).Text("Name");
                        header.Cell().Element(HeaderStyle).Text("Price");
                        header.Cell().Element(HeaderStyle).Text("Description");
                        header.Cell().Element(HeaderStyle).Text("IsActive");

                        static QuestPDF.Infrastructure.IContainer HeaderStyle(
                            QuestPDF.Infrastructure.IContainer c)
                        {
                            return c
                                .Border(1)
                                .BorderColor("#0d6efd")
                                .Background("#0d6efd")
                                .PaddingVertical(6)
                                .PaddingHorizontal(8)
                                .DefaultTextStyle(x => x.Bold().FontColor("#ffffff"));
                        }
                    });

                    int no = 1;
                    foreach (var item in listShippingAreas)
                    {
                        string bgBgColor = (no % 2 == 0) ? "#f8f9fa" : "#ffffff";

                        table.Cell().Element(c => Cell(c, bgBgColor)).Text(no.ToString());
                        table.Cell().Element(c => Cell(c, bgBgColor)).Text(item.Code ?? "");
                        table.Cell().Element(c => Cell(c, bgBgColor)).Text(item.Name ?? "");
                        table.Cell().Element(c => Cell(c, bgBgColor)).Text(item.Price.ToString("N0"));
                        table.Cell().Element(c => Cell(c, bgBgColor)).Text(item.Description ?? "");
                        table.Cell().Element(c => Cell(c, bgBgColor)).Text(item.IsActive.ToString());

                        no++;
                    }

                    static QuestPDF.Infrastructure.IContainer Cell(
                        QuestPDF.Infrastructure.IContainer c, string backgroundColor)
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
            $"ShippingArea_{DateTime.Now:yyyyMMdd_HHmmss}.pdf"
        );
    }

    //print
    [HttpGet]
    public async Task<IActionResult> Print()
    {
        var shippingAreas = await _api.GetShippingAreasForExportAsync();

        var model = new PagedResult<ShippingAreaViewModel>
        {
            Data = shippingAreas,
            Page = 1,
            PageSize = shippingAreas.Count,
            TotalCount = shippingAreas.Count,
            TotalData = shippingAreas.Count
        };

        return View(model);
    }

}
