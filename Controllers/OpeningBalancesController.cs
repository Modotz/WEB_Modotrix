using ERP.Web.Models;
using ERP.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Web.Controllers;

/// <summary>Akuntansi &gt; Saldo Awal (satu jurnal saldo awal; simpan ulang = jurnal lama dibalik).</summary>
public class OpeningBalancesController : Controller
{
    private readonly AccountingApiClient _api;
    private readonly Translator _t;

    public OpeningBalancesController(AccountingApiClient api, Translator t)
    {
        _api = api;
        _t = t;
    }

    [HttpGet]
    public async Task<IActionResult> ExportExcel() => this.ExcelFile(await BuildExportAsync());

    [HttpGet]
    public async Task<IActionResult> ExportPdf() => await this.PdfFile(await BuildExportAsync());

    [HttpGet]
    public async Task<IActionResult> PrintList() => this.PrintView(await BuildExportAsync());

    /// <summary>Saldo awal tersimpan saat ini: hanya akun yang bersaldo, ditambah baris total debit/kredit.</summary>
    private async Task<ListExport> BuildExportAsync()
    {
        var ob = await _api.GetOpeningBalanceAsync();
        var lines = ob.Lines.Where(l => l.Debit != 0 || l.Credit != 0).ToList();

        var subtitle = new List<string>();
        if (ob.Date != null)
            subtitle.Add(_t["Tanggal saldo awal"] + ": " + Acc.Date(ob.Date));
        if (ob.JournalNo != null)
            subtitle.Add(_t["Jurnal {0}", ob.JournalNo]);

        return new ListExport
        {
            Title = _t["Saldo Awal"],
            Subtitle = subtitle.Count > 0 ? string.Join(" · ", subtitle) : null,
            FileName = "saldo_awal",
            Landscape = false,
            Headers = new[] { "Kode", "Nama Akun", "Tipe", "Debit", "Kredit" }.Select(h => _t[h]).ToArray(),
            Rows = lines.Select(l => new object?[]
            {
                l.Code, l.Name, _t[Acc.TypeLabel(l.Type)], l.Debit, l.Credit
            }).ToList(),
            Footer = new object?[] { _t["Total"], null, null, lines.Sum(l => l.Debit), lines.Sum(l => l.Credit) }
        };
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        return View(await _api.GetOpeningBalanceAsync());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(DateOnly date, List<OpeningBalanceLineViewModel> lines)
    {
        var result = await _api.SaveOpeningBalanceAsync(date, lines ?? new());
        TempData[result.Success ? "Success" : "Error"] = result.Message;

        return RedirectToAction(nameof(Index));
    }
}
