using ERP.Web.Models;
using ERP.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Web.Controllers;

/// <summary>Akuntansi &gt; Rekonsiliasi Bank: rekening koran (staging) dan layar kerja pencocokan.</summary>
public class BankReconciliationsController : Controller
{
    private readonly AccountingApiClient _api;

    public BankReconciliationsController(AccountingApiClient api) => _api = api;

    [HttpGet]
    public async Task<IActionResult> Index(long? cashAccountId, string? status) => View(new BankReconciliationsIndexViewModel
    {
        CashAccountId = cashAccountId,
        Status = status,
        Accounts = await _api.GetReconcilableAccountsAsync(),
        Statements = await _api.GetBankStatementsAsync(cashAccountId, status)
    });

    /// <summary>Usulan periode &amp; saldo awal rekening koran baru (JSON untuk modal).</summary>
    [HttpGet]
    public async Task<IActionResult> Defaults(long cashAccountId)
    {
        var json = await _api.GetBankStatementDefaultsJsonAsync(cashAccountId);
        return json == null ? NoContent() : Content(json, "application/json");
    }

    [HttpGet]
    public async Task<IActionResult> Details(Guid uid)
    {
        var model = await _api.GetBankStatementAsync(uid);
        if (model != null)
            return View(model);

        TempData["Error"] = "Rekening koran tidak ditemukan.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(BankStatementFormViewModel model)
    {
        model.Uid = null;
        var result = await _api.SaveBankStatementAsync(model);
        TempData[result.Success ? "Success" : "Error"] = result.Message;
        return result.Success && result.Uid != null
            ? RedirectToAction(nameof(Details), new { uid = result.Uid })
            : RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(BankStatementFormViewModel model) =>
        Back(await _api.SaveBankStatementAsync(model), model.Uid ?? Guid.Empty);

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid uid)
    {
        var result = await _api.DeleteBankStatementAsync(uid);
        TempData[result.Success ? "Success" : "Error"] = result.Message;
        return result.Success ? RedirectToAction(nameof(Index)) : RedirectToAction(nameof(Details), new { uid });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveLine(Guid uid, BankStatementLineFormViewModel line) =>
        Back(await _api.SaveBankStatementLineAsync(uid, line), uid);

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteLine(Guid uid, Guid lineUid) =>
        Back(await _api.DeleteBankStatementLineAsync(uid, lineUid), uid);

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Match(Guid uid, Guid lineUid, long journalLineId) =>
        Back(await _api.MatchBankLineAsync(uid, lineUid, journalLineId), uid);

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Unmatch(Guid uid, Guid lineUid) =>
        Back(await _api.UnmatchBankLineAsync(uid, lineUid), uid);

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Complete(Guid uid) => Back(await _api.CompleteBankStatementAsync(uid), uid);

    private IActionResult Back(ApiResult result, Guid uid)
    {
        TempData[result.Success ? "Success" : "Error"] = result.Message;
        return RedirectToAction(nameof(Details), new { uid });
    }
}

/// <summary>Laporan item bank belum terkonfirmasi rekening koran.</summary>
public class BankOutstandingController : Controller
{
    private readonly AccountingApiClient _api;

    public BankOutstandingController(AccountingApiClient api) => _api = api;

    [HttpGet]
    public async Task<IActionResult> Index(DateOnly? asOf, long? cashAccountId)
    {
        var model = await _api.GetBankOutstandingAsync(asOf ?? DateOnly.FromDateTime(DateTime.Today), cashAccountId);
        model.Accounts = await _api.GetReconcilableAccountsAsync();
        return View(model);
    }
}
