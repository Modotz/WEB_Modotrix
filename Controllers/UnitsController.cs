using ERP.Web.Models;
using ERP.Web.Services;
using Microsoft.AspNetCore.Mvc;
using QuestPDF.Fluent;
using QuestPDF.Helpers;

namespace ERP.Web.Controllers;

public class UnitsController : Controller
{
    private readonly ApiClient _api;
    private readonly Translator _t;

    public UnitsController(ApiClient api, Translator t)
    {
        _api = api;
        _t = t;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
    string? search = null,
    int page = 1,
    int pageSize = 10)
    {
        var result =
            await _api.GetUnitsAsync(
                search,
                page,
                pageSize
            );


        return View(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetById(Guid uid)
    {
        var unit = await _api.GetUnitAsync(uid);

        if (unit is null)
            return NotFound();

        return Json(unit);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(UnitViewModel model)
    {
        if (string.IsNullOrWhiteSpace(model.Code))
        {
            ModelState.AddModelError(
                nameof(model.Code),
                _t["Kode satuan wajib diisi."]
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

        var result = await _api.CreateUnitAsync(model);

        if (!result.Success)
        {
            TempData["Error"] =
                result.Error ?? "Gagal menambahkan satuan.";

            return RedirectToAction(nameof(Index));
        }

        TempData["Success"] =
            "Satuan berhasil ditambahkan.";

        return RedirectToAction(nameof(Index));
    }


    // EDIT di MVC juga POST.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(UnitViewModel model)
    {
        ModelState.Remove(nameof(model.Code));

        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Data satuan belum lengkap.";
            return RedirectToAction(nameof(Index));
        }

        // UID wajib untuk update (record tidak diidentifikasi dengan id angka)
        if (string.IsNullOrWhiteSpace(model.Uid))
        {
            TempData["Error"] = "Satuan tidak ditemukan.";
            return RedirectToAction(nameof(Index));
        }

        var result = await _api.UpdateUnitAsync(model);

        TempData[result.Success ? "Success" : "Error"] =
            result.Success
                ? "Satuan berhasil diubah."
                : result.Error ?? "Gagal mengubah satuan.";

        return RedirectToAction(nameof(Index));
    }

    // DELETE di MVC juga POST.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid uid)
    {
        var result = await _api.DeleteUnitAsync(uid);

        TempData[result.Success ? "Success" : "Error"] =
            result.Success
                ? "Satuan berhasil dihapus."
                : result.Error ?? "Gagal menghapus satuan.";

        return RedirectToAction(nameof(Index));
    }

    //export-excel
    [HttpGet]
    public async Task<IActionResult> ExportExcel()
    {
        var units = await _api.GetUnitsForExportAsync();

        using var workbook = new ClosedXML.Excel.XLWorkbook();

        var worksheet = workbook.Worksheets.Add("Unit");

        worksheet.Cell(1, 1).Value = "No";
        worksheet.Cell(1, 2).Value = _t["Kode"];
        worksheet.Cell(1, 3).Value = _t["Nama"];

        var header = worksheet.Range("A1:I1");

        header.Style.Font.Bold = true;
        header.Style.Alignment.Horizontal =
            ClosedXML.Excel.XLAlignmentHorizontalValues.Center;

        int row = 2;
        int no = 1;

        foreach (var item in units)
        {
            worksheet.Cell(row, 1).Value = no;
            worksheet.Cell(row, 2).Value = item.Code;
            worksheet.Cell(row, 3).Value = item.Name;

            row++;
            no++;
        }

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();

        workbook.SaveAs(stream);

        return File(
            stream.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"Unit_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
        );
    }

    //export-pdf
    [HttpGet]
    public async Task<IActionResult> ExportPdf()
    {
        var units = await _api.GetUnitsForExportAsync();
        var listUnits = units.ToList(); 

        var document = QuestPDF.Fluent.Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(QuestPDF.Helpers.PageSizes.A4.Landscape());
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
                        text.Span("Unit").FontColor("#6c757d");
                        text.Span(" · ").FontColor("#6c757d");
                        text.Span(_t["Dicetak: {0}", DateTime.Now.ToString("dd/MM/yyyy HH:mm")]).FontColor("#6c757d");
                        text.Span(" · ").FontColor("#6c757d");
                        text.Span(_t["Total: {0} data", listUnits.Count]).FontColor("#6c757d");
                    });
                });

                page.Content().PaddingTop(15).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(35);   // No
                        columns.ConstantColumn(65);   // Code
                        columns.RelativeColumn(2);    // Name
                    });

                    // Header Tabel
                    table.Header(header =>
                    {
                        header.Cell().Element(HeaderStyle).Text("No");
                        header.Cell().Element(HeaderStyle).Text("Code");
                        header.Cell().Element(HeaderStyle).Text("Name");

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
                    foreach (var item in listUnits)
                    {
                        string bgBgColor = (no % 2 == 0) ? "#f8f9fa" : "#ffffff";

                        table.Cell().Element(c => Cell(c, bgBgColor)).Text(no.ToString());
                        table.Cell().Element(c => Cell(c, bgBgColor)).Text(item.Code ?? "");
                        table.Cell().Element(c => Cell(c, bgBgColor)).Text(item.Name ?? "");

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
                    .Text(_t["Dokumen dihasilkan otomatis oleh sistem."])
                    .FontSize(8.5f)
                    .FontColor("#888888");
            });
        });

        var pdf = document.GeneratePdf();

        return File(
            pdf,
            "application/pdf",
            $"Unit_{DateTime.Now:yyyyMMdd_HHmmss}.pdf"
        );
    }

    //print
    [HttpGet]
    public async Task<IActionResult> Print()
    {
        var units = await _api.GetUnitsForExportAsync();

        var model = new PagedResult<UnitViewModel>
        {
            Data = units,
            Page = 1,
            PageSize = units.Count,
            TotalCount = units.Count,
            TotalData = units.Count,
             
        };

        return View(model);
    }


}
