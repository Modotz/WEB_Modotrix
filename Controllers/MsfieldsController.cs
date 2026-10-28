using ERP.Web.Models;
using ERP.Web.Services;
using Microsoft.AspNetCore.Mvc;

using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ERP.Web.Controllers;

public class MsfieldsController : Controller
{
    private readonly ApiClient _api;
    private readonly Translator _t;

    public MsfieldsController(ApiClient api, Translator t)
    {
        _api = api;
        _t = t;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
    string? search = null,
    bool? isActive = null,
    int page = 1,
    int pageSize = 10)
    {
        page = page < 1 ? 1 : page;

        var allowedPageSizes = new[] { 10, 25, 50, 100 };

        if (!allowedPageSizes.Contains(pageSize))
        {
            pageSize = 10;
        }

        var result = await _api.GetMsfieldsAsync(
            search,
            isActive,
            page,
            pageSize
        );

        return View(result);
    }

    // ==========================================
    // GET: /Msfields/GetById/{uid}
    // ==========================================
    [HttpGet]
    public async Task<IActionResult> GetById(Guid uid)
    {
        var field = await _api.GetMsfieldsAsync(uid);

        if (field == null)
            return NotFound();

        return Json(field);
    }

    // ==========================================
    // POST: /Msfields/Create
    // ==========================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(MsfieldViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] =
                "Data tidak valid. Silakan periksa kembali input.";

            return RedirectToAction(nameof(Index));
        }

        var result = await _api.CreateFieldAsync(model);

        if (!result.Success)
        {
            TempData["Error"] =
                result.Error ?? "Gagal menambahkan data field.";

            return RedirectToAction(nameof(Index));
        }

        TempData["Success"] =
            "Data field berhasil ditambahkan.";

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(MsfieldViewModel model)
    {
        // Validasi input
        if (!ModelState.IsValid)
        {
            TempData["Error"] =
                "Data tidak valid. Silakan periksa kembali input.";

            return RedirectToAction(nameof(Index));
        }

        // Validasi UID (record tidak diidentifikasi dengan id angka)
        if (string.IsNullOrWhiteSpace(model.Uid))
        {
            TempData["Error"] =
                "Data field tidak ditemukan.";

            return RedirectToAction(nameof(Index));
        }

        // Proses update ke API
        var result = await _api.UpdateFieldAsync(model);

        // Jika update gagal
        if (!result.Success)
        {
            TempData["Error"] =
                result.Error ?? "Gagal mengubah data field.";

            return RedirectToAction(nameof(Index));
        }

        // Jika update berhasil
        TempData["Success"] =
            "Data field berhasil diperbarui.";

        return RedirectToAction(nameof(Index));
    }


    // ==========================================
    // POST: /Msfield/Delete
    // ==========================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid uid)
    {
        var result = await _api.DeleteFieldAsync(uid);

        TempData[result.Success ? "Success" : "Error"] =
            result.Success
                ? "Data field berhasil dihapus."
                : result.Error ?? "Gagal menghapus data field.";

        return RedirectToAction(nameof(Index));
    }


    //export-excel
    [HttpGet]
    public async Task<IActionResult> ExportExcel()
    {
        var msfields = await _api.GetFieldsForExportAsync();

        using var workbook = new ClosedXML.Excel.XLWorkbook();

        var worksheet = workbook.Worksheets.Add("Msfield");

        worksheet.Cell(1, 1).Value = "No";
        worksheet.Cell(1, 2).Value = "Field Key";
        worksheet.Cell(1, 3).Value = "Field Text";
        worksheet.Cell(1, 4).Value = "Field Value";
        worksheet.Cell(1, 5).Value = "Status";

        var header = worksheet.Range("A1:I1");

        header.Style.Font.Bold = true;
        header.Style.Alignment.Horizontal =
            ClosedXML.Excel.XLAlignmentHorizontalValues.Center;

        int row = 2;
        int no = 1;

        foreach (var item in msfields)
        {
            worksheet.Cell(row, 1).Value = no;
            worksheet.Cell(row, 2).Value = item.FieldKey;
            worksheet.Cell(row, 3).Value = item.FieldText;
            worksheet.Cell(row, 4).Value = item.FiledValue;
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
            $"Msfield_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
        );
    }

    //export-pdf
    [HttpGet]
    public async Task<IActionResult> ExportPdf()
    {
        var msfields = await _api.GetFieldsForExportAsync();
        var listMsfields = msfields.ToList(); // Mengubah ke List untuk mendapatkan Total Count

        var document = QuestPDF.Fluent.Document.Create(container =>
        {
            container.Page(page =>
            {
                // Set orientasi ke Landscape agar kolom pas
                page.Size(QuestPDF.Helpers.PageSizes.A4.Landscape());
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
                        text.Span("Msfield").FontColor("#6c757d");
                        text.Span(" · ").FontColor("#6c757d");
                        text.Span(_t["Dicetak: {0}", DateTime.Now.ToString("dd/MM/yyyy HH:mm")]).FontColor("#6c757d");
                        text.Span(" · ").FontColor("#6c757d");
                        text.Span(_t["Total: {0} data", listMsfields.Count]).FontColor("#6c757d"); // Menggunakan listMsfields
                    });
                });

                // Isi Tabel
                page.Content().PaddingTop(15).Table(table =>
                {
                    // Definisi Lebar Kolom
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(35);   // No
                        columns.ConstantColumn(65);   // Field Key
                        columns.RelativeColumn(2);    // Field Text
                        columns.RelativeColumn(2);    // Field Value
                        columns.ConstantColumn(65);   // Is Active
                    });

                    // Header Tabel (Biru dengan teks Putih)
                    table.Header(header =>
                    {
                        header.Cell().Element(HeaderStyle).Text("No");
                        header.Cell().Element(HeaderStyle).Text("Field Key");
                        header.Cell().Element(HeaderStyle).Text("Field Text");
                        header.Cell().Element(HeaderStyle).Text("Field Value");
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
                    foreach (var item in listMsfields) // Menggunakan listMsfields
                    {
                        // Latar belakang selang-seling (Zebra striping)
                        string bgBgColor = (no % 2 == 0) ? "#f8f9fa" : "#ffffff";

                        table.Cell().Element(c => Cell(c, bgBgColor)).Text(no.ToString());
                        table.Cell().Element(c => Cell(c, bgBgColor)).Text(item.FieldKey ?? "");
                        table.Cell().Element(c => Cell(c, bgBgColor)).Text(item.FieldText ?? "");
                        table.Cell().Element(c => Cell(c, bgBgColor)).Text(item.FiledValue ?? "");
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
            $"Msfield_{DateTime.Now:yyyyMMdd_HHmmss}.pdf"
        );
    }

    //print
    [HttpGet]
    public async Task<IActionResult> Print()
    {
        var msfields = await _api.GetFieldsForExportAsync();

        var model = new PagedResult<MsfieldViewModel>
        {
            Data = msfields,
            Page = 1,
            PageSize = msfields.Count,
            TotalCount = msfields.Count,
            TotalData = msfields.Count
        };

        return View(model);
    }
}