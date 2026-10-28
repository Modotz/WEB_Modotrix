using ERP.Web.Models;
using ERP.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Web.Controllers;

/// <summary>Akuntansi &gt; Tarif Pajak: tarif PPN historis (berlaku dari/sampai), DPP Nilai Lain, kreditabel atau tidak.</summary>
public class TaxRatesController : Controller
{
    private readonly AccountingApiClient _api;
    private readonly Translator _t;

    public TaxRatesController(AccountingApiClient api, Translator t)
    {
        _api = api;
        _t = t;
    }

    [HttpGet]
    public async Task<IActionResult> Index() => View(await _api.GetTaxRatesAsync());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TaxRateViewModel model)
    {
        model.Uid = Guid.Empty;
        return Done(await _api.SaveTaxRateAsync(model));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(TaxRateViewModel model) =>
        Done(model.Uid == Guid.Empty ? new ApiResult(false, "Tarif tidak ditemukan.") : await _api.SaveTaxRateAsync(model));

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid uid) => Done(await _api.DeleteTaxRateAsync(uid));

    [HttpGet]
    public async Task<IActionResult> ExportExcel() => this.ExcelFile(await BuildExportAsync());

    [HttpGet]
    public async Task<IActionResult> ExportPdf() => await this.PdfFile(await BuildExportAsync());

    [HttpGet]
    public async Task<IActionResult> PrintList() => this.PrintView(await BuildExportAsync());

    private async Task<ListExport> BuildExportAsync()
    {
        var rows = await _api.GetTaxRatesAsync();

        return new ListExport
        {
            Title = _t["Tarif Pajak"],
            FileName = "tarif_pajak",
            Headers = new[] { "Nama", "Tarif", "Tarif Efektif (%)", "PPN Masukan", "Berlaku Dari", "Berlaku Sampai", "Status", "Dipakai", "Catatan" }
                .Select(h => _t[h]).ToArray(),
            Rows = rows.Select(r => new object?[]
            {
                r.Name, r.Formula, r.EffectiveRate, r.IsInputCreditable ? _t["Kreditabel"] : _t["Tidak kreditabel"], r.ValidFrom, r.ValidTo,
                r.IsActive ? _t["Aktif"] : _t["Nonaktif"], r.UsageCount, r.Note
            }).ToList()
        };
    }

    private IActionResult Done(ApiResult result)
    {
        TempData[result.Success ? "Success" : "Error"] = result.Message;
        return RedirectToAction(nameof(Index));
    }
}
