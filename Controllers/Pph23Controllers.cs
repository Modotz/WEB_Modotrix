using ERP.Web.Models;
using ERP.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Web.Controllers;

/// <summary>Akuntansi &gt; Tarif PPh 23: tarif historis (normal / tanpa NPWP) per jenis objek.</summary>
public class Pph23RatesController : Controller
{
    private readonly AccountingApiClient _api;

    public Pph23RatesController(AccountingApiClient api) => _api = api;

    [HttpGet]
    public async Task<IActionResult> Index() => View(await _api.GetPph23RatesAsync());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Pph23RateViewModel model)
    {
        model.Uid = Guid.Empty;
        return Done(await _api.SavePph23RateAsync(model));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(Pph23RateViewModel model) =>
        Done(model.Uid == Guid.Empty ? new ApiResult(false, "Tarif tidak ditemukan.") : await _api.SavePph23RateAsync(model));

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid uid) => Done(await _api.DeletePph23RateAsync(uid));

    private IActionResult Done(ApiResult result)
    {
        TempData[result.Success ? "Success" : "Error"] = result.Message;
        return RedirectToAction(nameof(Index));
    }
}

/// <summary>Akuntansi &gt; PPh 23: ringkasan per masa, daftar Bukti Potong, setoran per masa.</summary>
public class Pph23Controller : Controller
{
    private readonly AccountingApiClient _api;
    private readonly Translator _t;

    public Pph23Controller(AccountingApiClient api, Translator t)
    {
        _api = api;
        _t = t;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int? year) => View(await _api.GetPph23YearAsync(year ?? DateTime.Today.Year));

    [HttpGet]
    public async Task<IActionResult> Period(string period)
    {
        var model = await _api.GetPph23PeriodAsync(period);
        model.CashAccounts = (await _api.GetCashAccountOptionsAsync()).Where(c => !c.IsForeign).ToList();
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateDeposit(string period, DateOnly depositDate, long cashAccountId, string? ntpn, string? note) =>
        Back(await _api.CreatePph23DepositAsync(period, depositDate, cashAccountId, ntpn, note), period);

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> VoidDeposit(Guid uid, string period, string? reason, DateOnly? date) =>
        Back(await _api.VoidPph23DepositAsync(uid, reason, date), period);

    /// <summary>Isi/ubah nomor Bukti Potong e-Bupot.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateCertificate(Guid invoiceUid, string period, string? certNo, DateOnly? certDate) =>
        Back(await _api.SetPph23CertificateAsync(invoiceUid, certNo, certDate), period);

    [HttpGet]
    public async Task<IActionResult> ExportPeriodExcel(string period)
    {
        var m = await _api.GetPph23PeriodAsync(period);

        return this.ExcelFile(new ListExport
        {
            Title = _t["Daftar Bukti Potong PPh 23"],
            Subtitle = _t["Masa"] + " " + period,
            FileName = "bukti_potong_pph23_" + period,
            Headers = new[] { "Tanggal", "No. Faktur", "Supplier", "NPWP", "Tarif PPh 23", "Tarif (%)", "DPP (IDR)", "PPh 23 (IDR)", "No. Bukti Potong", "Tgl. Bukti Potong", "Setoran" }
                .Select(h => _t[h]).ToArray(),
            Rows = m.Slips.Select(s => new object?[]
            {
                s.InvoiceDate, s.InvoiceNo, s.SupplierName, s.HasNpwp ? s.Npwp : _t["Tanpa NPWP"], s.RateName, s.Percent, s.Base, s.Amount,
                s.CertNo, s.CertDate, s.DepositNo
            }).ToList(),
            Footer = new object?[] { _t["Total"], null, null, null, null, null, m.Slips.Sum(s => s.Base), m.Slips.Sum(s => s.Amount), null, null, null }
        });
    }

    private IActionResult Back(ApiResult result, string period)
    {
        TempData[result.Success ? "Success" : "Error"] = result.Message;
        return RedirectToAction(nameof(Period), new { period });
    }
}
