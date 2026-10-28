using ERP.Web.Models;
using ERP.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Web.Controllers;

/// <summary>Akuntansi &gt; Bagan Akun.</summary>
public class ChartOfAccountsController : Controller
{
    private readonly AccountingApiClient _api;
    private readonly Translator _t;

    public ChartOfAccountsController(AccountingApiClient api, Translator t)
    {
        _api = api;
        _t = t;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? q, string? type, bool? active)
    {
        return View(new ChartOfAccountsIndexViewModel
        {
            Q = q,
            Type = type,
            Active = active,
            Accounts = await _api.GetAccountsAsync(q, type, active)
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AccountViewModel model)
    {
        model.Uid = Guid.Empty;
        return Done(await _api.SaveAccountAsync(model));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(AccountViewModel model)
    {
        if (model.Uid == Guid.Empty)
            return Done(new ApiResult(false, "Akun tidak ditemukan."));

        return Done(await _api.SaveAccountAsync(model));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid uid)
    {
        return Done(await _api.DeleteAccountAsync(uid));
    }

    [HttpGet]
    public async Task<IActionResult> ExportExcel(string? q, string? type, bool? active) =>
        this.ExcelFile(await BuildExportAsync(q, type, active));

    [HttpGet]
    public async Task<IActionResult> ExportPdf(string? q, string? type, bool? active) =>
        await this.PdfFile(await BuildExportAsync(q, type, active));

    [HttpGet]
    public async Task<IActionResult> PrintList(string? q, string? type, bool? active) =>
        this.PrintView(await BuildExportAsync(q, type, active));

    /// <summary>Semua akun sesuai filter halaman (bagan akun tidak dipaginasi).</summary>
    private async Task<ListExport> BuildExportAsync(string? q, string? type, bool? active)
    {
        var accounts = await _api.GetAccountsAsync(q, type, active);

        var filters = new List<string>();
        if (!string.IsNullOrWhiteSpace(q))
            filters.Add(_t["Cari"] + ": " + q);
        if (!string.IsNullOrWhiteSpace(type))
            filters.Add(_t["Tipe"] + ": " + _t[Acc.TypeLabel(type)]);
        if (active != null)
            filters.Add(_t["Status"] + ": " + (active.Value ? _t["Aktif"] : _t["Nonaktif"]));

        return new ListExport
        {
            Title = _t["Bagan Akun"],
            Subtitle = filters.Count > 0 ? string.Join(" · ", filters) : null,
            FileName = "bagan_akun",
            Headers = new[] { "Kode", "Nama Akun", "Induk", "Tipe", "Saldo Normal", "Arus Kas", "Header", "Kontrol", "Status", "Keterangan" }
                .Select(h => _t[h]).ToArray(),
            Rows = accounts.Select(a => new object?[]
            {
                a.Code,
                new string(' ', a.Level * 4) + a.Name,
                a.ParentCode,
                _t[Acc.TypeLabel(a.Type)],
                a.NormalBalance == "debit" ? _t["Debit"] : _t["Kredit"],
                a.IsPostable ? _t[Acc.CashFlowLabel(a.CashFlowCategory)] : null,
                a.IsPostable ? null : _t["Ya"],
                a.IsControl ? _t["Ya"] : null,
                a.IsActive ? _t["Aktif"] : _t["Nonaktif"],
                a.IsUsed ? _t["Sudah dipakai jurnal"] : null
            }).ToList()
        };
    }

    private IActionResult Done(ApiResult result)
    {
        TempData[result.Success ? "Success" : "Error"] = result.Message;
        return RedirectToAction(nameof(Index));
    }
}
