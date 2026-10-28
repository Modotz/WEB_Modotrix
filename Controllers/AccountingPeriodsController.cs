using ERP.Web.Models;
using ERP.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Web.Controllers;

/// <summary>Akuntansi &gt; Periode Akuntansi: generate per tahun, tutup/buka berurutan dengan catatan.</summary>
public class AccountingPeriodsController : Controller
{
    private readonly AccountingApiClient _api;
    private readonly Translator _t;

    public AccountingPeriodsController(AccountingApiClient api, Translator t)
    {
        _api = api;
        _t = t;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int? year)
    {
        var all = await _api.GetPeriodsAsync();
        var years = all.SelectMany(p => new[] { p.StartDate.Year, p.EndDate.Year }).Distinct().OrderBy(y => y).ToList();

        return View(new AccountingPeriodsIndexViewModel
        {
            Year = year,
            Years = years,
            Periods = FilterYear(all, year)
        });
    }

    [HttpGet]
    public async Task<IActionResult> ExportExcel(int? year) => this.ExcelFile(await BuildExportAsync(year));

    [HttpGet]
    public async Task<IActionResult> ExportPdf(int? year) => await this.PdfFile(await BuildExportAsync(year));

    [HttpGet]
    public async Task<IActionResult> PrintList(int? year) => this.PrintView(await BuildExportAsync(year));

    private static List<AccountingPeriodViewModel> FilterYear(List<AccountingPeriodViewModel> all, int? year) =>
        year == null ? all : all.Where(p => p.StartDate.Year == year || p.EndDate.Year == year).ToList();

    /// <summary>Periode sesuai filter tahun halaman (tidak dipaginasi).</summary>
    private async Task<ListExport> BuildExportAsync(int? year)
    {
        var periods = FilterYear(await _api.GetPeriodsAsync(), year);

        return new ListExport
        {
            Title = _t["Periode Akuntansi"],
            Subtitle = year == null ? null : _t["Tahun"] + " " + year,
            FileName = "periode_akuntansi",
            Headers = new[] { "Periode", "Mulai", "Selesai", "Status", "Jurnal Posted", "Jurnal Draft", "Catatan Terakhir" }
                .Select(h => _t[h]).ToArray(),
            Rows = periods.Select(p => new object?[]
            {
                p.Name, p.StartDate, p.EndDate, p.IsClosed ? _t["Closed"] : _t["Open"], p.PostedJournals, p.DraftJournals,
                p.LastEvent == null
                    ? null
                    : _t[p.LastEvent.Action == "close" ? "Ditutup {0} oleh {1}:" : "Dibuka {0} oleh {1}:",
                        p.LastEvent.CreatedAt.ToString("dd/MM/yyyy HH:mm"), p.LastEvent.Username ?? "-"] + " " + p.LastEvent.Note
            }).ToList()
        };
    }

    [HttpGet]
    public async Task<IActionResult> Events(Guid uid)
    {
        return Json(await _api.GetPeriodEventsAsync(uid));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Generate(int year)
    {
        return Done(await _api.GeneratePeriodsAsync(year), year);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string name, DateOnly startDate, DateOnly endDate, string? note)
    {
        return Done(await _api.SavePeriodAsync(null, name, startDate, endDate, note), startDate.Year);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(Guid uid, string name, DateOnly startDate, DateOnly endDate, string? note)
    {
        return Done(await _api.SavePeriodAsync(uid, name, startDate, endDate, note), startDate.Year);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid uid, int? year)
    {
        return Done(await _api.DeletePeriodAsync(uid), year);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Close(Guid uid, string? note, int? year)
    {
        return Done(await _api.ClosePeriodAsync(uid, note), year);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reopen(Guid uid, string? note, int? year)
    {
        return Done(await _api.ReopenPeriodAsync(uid, note), year);
    }

    private IActionResult Done(ApiResult result, int? year)
    {
        TempData[result.Success ? "Success" : "Error"] = result.Message;
        return RedirectToAction(nameof(Index), new { year });
    }
}
