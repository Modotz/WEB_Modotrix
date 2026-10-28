using ERP.Web.Models;
using ERP.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Web.Controllers;

// Level 13 — adjusting entries: jurnal akrual & pembalik, prepaid/deferred + Proses Rekognisi, cadangan kerugian piutang.

/// <summary>Akuntansi &gt; Jurnal Akrual &amp; Pembalik (laporan).</summary>
public class AccrualsController : Controller
{
    private readonly AccountingApiClient _api;

    public AccrualsController(AccountingApiClient api) => _api = api;

    [HttpGet]
    public async Task<IActionResult> Index(DateOnly? from, DateOnly? to, string? status) => View(new AccrualsIndexViewModel
    {
        From = from,
        To = to,
        Status = status,
        Rows = await _api.GetAccrualsAsync(from, to, status)
    });
}

/// <summary>Akuntansi &gt; Jadwal Prepaid/Deferred: daftar + cross-check, detail per jadwal, ubah jadwal (catch-up).</summary>
public class DeferralSchedulesController : Controller
{
    private readonly AccountingApiClient _api;

    public DeferralSchedulesController(AccountingApiClient api) => _api = api;

    [HttpGet]
    public async Task<IActionResult> Index(DateOnly? asOf, string? side, string? status)
    {
        var model = await _api.GetDeferralSchedulesAsync(asOf ?? DateOnly.FromDateTime(DateTime.Today), side, status);
        model.Side = side;
        model.Status = status;
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Details(Guid uid)
    {
        var model = await _api.GetDeferralScheduleAsync(uid);
        if (model != null)
            return View(model);

        TempData["Error"] = "Jadwal tidak ditemukan.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reschedule(Guid uid, int months)
    {
        var result = await _api.RescheduleDeferralAsync(uid, months);
        TempData[result.Success ? "Success" : "Error"] = result.Message;
        return RedirectToAction(nameof(Details), new { uid });
    }
}

/// <summary>Akuntansi &gt; Proses Rekognisi: pratinjau "s/d periode", proses, batal proses terakhir.</summary>
public class DeferralRecognitionController : Controller
{
    private readonly AccountingApiClient _api;

    public DeferralRecognitionController(AccountingApiClient api) => _api = api;

    [HttpGet]
    public async Task<IActionResult> Index(string? period) => View(await _api.GetDeferralRunAsync(period));

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Run(string period, string? note)
    {
        var result = await _api.RunDeferralRecognitionAsync(period, note);
        TempData[result.Success ? "Success" : "Error"] = result.Message;
        return RedirectToAction(nameof(Index), new { period });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReverseLast(string? reason, string? period)
    {
        var result = await _api.ReverseLastDeferralRecognitionAsync(reason);
        TempData[result.Success ? "Success" : "Error"] = result.Message;
        return RedirectToAction(nameof(Index), new { period });
    }
}

/// <summary>Akuntansi &gt; Cadangan Kerugian Piutang: saldo, estimasi, dua metode write-off (juga dari detail faktur penjualan).</summary>
public class DoubtfulReceivablesController : Controller
{
    private readonly AccountingApiClient _api;

    public DoubtfulReceivablesController(AccountingApiClient api) => _api = api;

    [HttpGet]
    public async Task<IActionResult> Index(DateOnly? asOf) =>
        View(await _api.GetDoubtfulReceivablesAsync(asOf ?? DateOnly.FromDateTime(DateTime.Today)));

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Estimate(DateOnly date, decimal amount, string? note) =>
        Back(await _api.EstimateAllowanceAsync(date, amount, note), null);

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReverseEstimate(Guid uid, DateOnly? date, string? reason) =>
        Back(await _api.ReverseAllowanceEstimateAsync(uid, date, reason), null);

    /// <param name="returnToInvoice">Dikirim dari detail faktur penjualan: kembali ke faktur itu.</param>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> WriteOff(Guid invoiceUid, DateOnly date, decimal amount, string method, string? reason, bool returnToInvoice = false) =>
        Back(await _api.WriteOffReceivableAsync(invoiceUid, date, amount, method, reason), returnToInvoice ? invoiceUid : null);

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> VoidWriteOff(Guid uid, DateOnly? date, string? reason, Guid? invoiceUid) =>
        Back(await _api.VoidWriteOffAsync(uid, date, reason), invoiceUid);

    private IActionResult Back(ApiResult result, Guid? invoiceUid)
    {
        TempData[result.Success ? "Success" : "Error"] = result.Message;
        return invoiceUid != null
            ? RedirectToAction("Details", "SalesInvoices", new { uid = invoiceUid })
            : RedirectToAction(nameof(Index));
    }
}
