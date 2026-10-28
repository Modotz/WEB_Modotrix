using ERP.Web.Models;
using ERP.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Web.Controllers;

/// <summary>Akuntansi &gt; Neraca Saldo (saldo awal, mutasi, saldo akhir).</summary>
public class TrialBalanceController : Controller
{
    private readonly AccountingApiClient _api;
    private readonly Translator _t;

    public TrialBalanceController(AccountingApiClient api, Translator t)
    {
        _api = api;
        _t = t;
    }

    [HttpGet]
    public async Task<IActionResult> Index(DateOnly? from, DateOnly? to, bool includeZero = false)
    {
        return View(await LoadAsync(from, to, includeZero));
    }

    [HttpGet]
    public async Task<IActionResult> Print(DateOnly? from, DateOnly? to, bool includeZero = false)
    {
        return View(await LoadAsync(from, to, includeZero));
    }

    [HttpGet]
    public async Task<IActionResult> ExportExcel(DateOnly? from, DateOnly? to, bool includeZero = false) =>
        this.ExcelFile(await BuildExportAsync(from, to, includeZero));

    [HttpGet]
    public async Task<IActionResult> ExportPdf(DateOnly? from, DateOnly? to, bool includeZero = false) =>
        await this.PdfFile(await BuildExportAsync(from, to, includeZero));

    [HttpGet]
    public async Task<IActionResult> PrintList(DateOnly? from, DateOnly? to, bool includeZero = false) =>
        this.PrintView(await BuildExportAsync(from, to, includeZero));

    private async Task<ListExport> BuildExportAsync(DateOnly? from, DateOnly? to, bool includeZero)
    {
        var tb = await LoadAsync(from, to, includeZero);
        static object? Blank(decimal v) => v == 0 ? null : v;

        return new ListExport
        {
            Title = _t["Neraca Saldo"],
            Subtitle = _t["Periode"] + " " + Acc.Date(tb.From) + " " + _t["s/d"] + " " + Acc.Date(tb.To),
            FileName = "neraca_saldo",
            Headers = new[] { "Kode", "Nama Akun", "Saldo Awal Debit", "Saldo Awal Kredit", "Mutasi Debit", "Mutasi Kredit", "Saldo Akhir Debit", "Saldo Akhir Kredit" }
                .Select(h => _t[h]).ToArray(),
            Rows = tb.Rows.Select(r => new object?[]
            {
                r.Code, r.Name, Blank(r.OpeningDebit), Blank(r.OpeningCredit), Blank(r.MovementDebit),
                Blank(r.MovementCredit), Blank(r.ClosingDebit), Blank(r.ClosingCredit)
            }).ToList(),
            Footer = new object?[]
            {
                null, _t["TOTAL"], tb.TotalOpeningDebit, tb.TotalOpeningCredit, tb.TotalMovementDebit,
                tb.TotalMovementCredit, tb.TotalClosingDebit, tb.TotalClosingCredit
            }
        };
    }

    private async Task<TrialBalanceViewModel> LoadAsync(DateOnly? from, DateOnly? to, bool includeZero)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var start = from ?? new DateOnly(today.Year, today.Month, 1);
        var end = to ?? today;

        var tb = await _api.GetTrialBalanceAsync(start, end, includeZero);
        tb.IncludeZero = includeZero;

        return tb;
    }
}
