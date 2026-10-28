using ClosedXML.Excel;
using ERP.Web.Models;
using ERP.Web.Services;
using Microsoft.AspNetCore.Mvc;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ERP.Web.Controllers;

/// <summary>
/// Pengaturan &gt; Log Audit: daftar aktivitas user, detail perubahan, ekspor dan pembersihan log.
/// </summary>
public class AuditLogController : Controller
{
    private static readonly int[] AllowedPageSizes = { 15, 25, 50, 100 };

    private readonly AuditLogApiClient _api;
    private readonly Translator _t;

    public AuditLogController(AuditLogApiClient api, Translator t)
    {
        _api = api;
        _t = t;
    }

    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] AuditLogFilterViewModel filter)
    {
        filter.Page = Math.Max(filter.Page, 1);
        filter.PageSize = AllowedPageSizes.Contains(filter.PageSize) ? filter.PageSize : 25;

        return View(new AuditLogIndexViewModel
        {
            Filter = filter,
            Logs = await _api.GetLogsAsync(filter),
            Options = await _api.GetFilterOptionsAsync()
        });
    }

    [HttpGet]
    public async Task<IActionResult> ExportExcel([FromQuery] AuditLogFilterViewModel filter)
    {
        var logs = await _api.GetLogsForExportAsync(filter);

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add(_t["Log Audit"]);

        var headers = new[] { "No", _t["Waktu"], "User", _t["Aksi"], _t["Modul"], "Record", _t["Deskripsi"], "IP" };
        for (var i = 0; i < headers.Length; i++)
            sheet.Cell(1, i + 1).Value = headers[i];

        var header = sheet.Range(1, 1, 1, headers.Length);
        header.Style.Font.Bold = true;
        header.Style.Font.FontColor = XLColor.White;
        header.Style.Fill.BackgroundColor = XLColor.FromHtml("#0D6EFD");

        var row = 2;
        foreach (var log in logs)
        {
            sheet.Cell(row, 1).Value = row - 1;
            sheet.Cell(row, 2).Value = log.CreatedAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? "-";
            sheet.Cell(row, 3).Value = log.Username ?? "-";
            sheet.Cell(row, 4).Value = _t[AuditLogViewModel.ActionLabel(log.Action).Label];
            sheet.Cell(row, 5).Value = log.Module ?? "-";
            sheet.Cell(row, 6).Value = log.RecordId ?? "-";
            sheet.Cell(row, 7).Value = log.Description ?? "-";
            sheet.Cell(row, 8).Value = log.IpAddress ?? "-";
            row++;
        }

        sheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        return File(
            stream.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"audit_log_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
    }

    [HttpGet]
    public async Task<IActionResult> ExportPdf([FromQuery] AuditLogFilterViewModel filter)
    {
        var logs = await _api.GetLogsForExportAsync(filter);

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(8.5f).FontColor("#333333"));

                page.Header().Column(col =>
                {
                    col.Item().Text("ERP-LITE").Bold().FontSize(18).FontColor("#1a1a1a");
                    col.Item().PaddingTop(2).Text(
                            _t["Log Audit · Dicetak: {0} · Total: {1} data", DateTime.Now.ToString("dd/MM/yyyy HH:mm"), logs.Count])
                        .FontColor("#6c757d");
                });

                page.Content().PaddingTop(15).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(30);
                        columns.ConstantColumn(95);
                        columns.RelativeColumn(1.2f);
                        columns.ConstantColumn(65);
                        columns.RelativeColumn(1.4f);
                        columns.RelativeColumn(4);
                        columns.ConstantColumn(75);
                    });

                    table.Header(h =>
                    {
                        foreach (var title in new[] { "No", _t["Waktu"], "User", _t["Aksi"], _t["Modul"], _t["Deskripsi"], "IP" })
                            h.Cell().Element(HeaderCell).Text(title);
                    });

                    var no = 1;
                    foreach (var log in logs)
                    {
                        table.Cell().Element(BodyCell).Text((no++).ToString());
                        table.Cell().Element(BodyCell).Text(log.CreatedAt?.ToString("dd/MM/yyyy HH:mm:ss") ?? "-");
                        table.Cell().Element(BodyCell).Text(log.Username ?? "-");
                        table.Cell().Element(BodyCell).Text(_t[AuditLogViewModel.ActionLabel(log.Action).Label]);
                        table.Cell().Element(BodyCell).Text(log.Module ?? "-");
                        table.Cell().Element(BodyCell).Text(log.Description ?? "-");
                        table.Cell().Element(BodyCell).Text(log.IpAddress ?? "-");
                    }
                });

                page.Footer().AlignRight().Text(t =>
                {
                    t.Span(_t["Halaman"] + " ");
                    t.CurrentPageNumber();
                    t.Span(" / ");
                    t.TotalPages();
                });
            });
        });

        return File(document.GeneratePdf(), "application/pdf", $"audit_log_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");

        static IContainer HeaderCell(IContainer c) =>
            c.Background("#0D6EFD").Padding(4).DefaultTextStyle(x => x.FontColor(Colors.White).Bold());

        static IContainer BodyCell(IContainer c) =>
            c.BorderBottom(0.5f).BorderColor("#dee2e6").Padding(4);
    }

    [HttpGet]
    public async Task<IActionResult> Print([FromQuery] AuditLogFilterViewModel filter)
    {
        return View(await _api.GetLogsForExportAsync(filter));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Clear(DateOnly? before)
    {
        var (success, message) = await _api.ClearAsync(before);

        TempData[success ? "Success" : "Error"] = message;

        return RedirectToAction(nameof(Index));
    }
}
