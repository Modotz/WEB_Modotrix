using ERP.Web.Models;
using ERP.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Web.Controllers;

/// <summary>Akuntansi &gt; Buku Besar per akun.</summary>
public class GeneralLedgerController : Controller
{
    private readonly AccountingApiClient _api;
    private readonly Translator _t;

    public GeneralLedgerController(AccountingApiClient api, Translator t)
    {
        _api = api;
        _t = t;
    }

    [HttpGet]
    public async Task<IActionResult> Index(Guid? accountUid, DateOnly? from, DateOnly? to)
    {
        return View(await BuildAsync(accountUid, from, to));
    }

    [HttpGet]
    public async Task<IActionResult> Print(Guid? accountUid, DateOnly? from, DateOnly? to)
    {
        var model = await BuildAsync(accountUid, from, to);
        return model.Ledger == null ? RedirectToAction(nameof(Index)) : View(model);
    }

    [HttpGet]
    public async Task<IActionResult> ExportExcel(Guid? accountUid, DateOnly? from, DateOnly? to)
    {
        var data = await BuildExportAsync(accountUid, from, to);
        return data == null ? RedirectToAction(nameof(Index)) : this.ExcelFile(data);
    }

    [HttpGet]
    public async Task<IActionResult> ExportPdf(Guid? accountUid, DateOnly? from, DateOnly? to)
    {
        var data = await BuildExportAsync(accountUid, from, to);
        return data == null ? RedirectToAction(nameof(Index)) : await this.PdfFile(data);
    }

    [HttpGet]
    public async Task<IActionResult> PrintList(Guid? accountUid, DateOnly? from, DateOnly? to)
    {
        var data = await BuildExportAsync(accountUid, from, to);
        return data == null ? RedirectToAction(nameof(Index)) : this.PrintView(data);
    }

    /// <summary>Baris persis seperti di layar: saldo awal, mutasi, lalu total mutasi / saldo akhir (footer).</summary>
    private async Task<ListExport?> BuildExportAsync(Guid? accountUid, DateOnly? from, DateOnly? to)
    {
        var gl = (await BuildAsync(accountUid, from, to)).Ledger;
        if (gl == null)
            return null;

        var rows = new List<object?[]>
        {
            new object?[] { null, null, null, _t["Saldo awal per {0}", Acc.Date(gl.From)], null, null, gl.Opening }
        };
        rows.AddRange(gl.Lines.Select(l => new object?[]
        {
            l.Date, l.JournalNo, l.Reference, l.Description,
            l.Debit == 0 ? null : l.Debit, l.Credit == 0 ? null : l.Credit, l.Balance
        }));

        return new ListExport
        {
            Title = _t["Buku Besar"] + " — " + gl.Account.Code + " " + gl.Account.Name,
            Subtitle = _t["Periode"] + " " + Acc.Date(gl.From) + " " + _t["s/d"] + " " + Acc.Date(gl.To),
            FileName = "buku_besar_" + gl.Account.Code,
            Headers = new[] { "Tanggal", "No. Jurnal", "Referensi", "Keterangan", "Debit", "Kredit", "Saldo" }.Select(h => _t[h]).ToArray(),
            Rows = rows,
            Footer = new object?[] { null, null, null, _t["Total mutasi / Saldo akhir"], gl.TotalDebit, gl.TotalCredit, gl.Closing }
        };
    }

    private async Task<GeneralLedgerPageViewModel> BuildAsync(Guid? accountUid, DateOnly? from, DateOnly? to)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);

        var model = new GeneralLedgerPageViewModel
        {
            AccountUid = accountUid,
            From = from ?? new DateOnly(today.Year, today.Month, 1),
            To = to ?? today,
            Accounts = await _api.GetAccountOptionsAsync(postable: true)
        };

        if (accountUid is { } uid && uid != Guid.Empty)
            model.Ledger = await _api.GetGeneralLedgerAsync(uid, model.From, model.To);

        return model;
    }
}
