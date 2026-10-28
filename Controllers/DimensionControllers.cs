using ERP.Web.Models;
using ERP.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Web.Controllers;

// Anggaran & Biaya (Level 11): cost center, project, anggaran, alokasi biaya, laporan per dimensi.

/// <summary>Master cost center (unit akuntabilitas biaya).</summary>
public class CostCentersController : Controller
{
    private readonly AccountingApiClient _api;

    public CostCentersController(AccountingApiClient api) => _api = api;

    [HttpGet]
    public async Task<IActionResult> Index() => View(await _api.GetCostCentersAsync());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CostCenterViewModel model)
    {
        model.Uid = Guid.Empty;
        return Done(await _api.SaveCostCenterAsync(model));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(CostCenterViewModel model) =>
        Done(model.Uid == Guid.Empty ? new ApiResult(false, "Cost center tidak ditemukan.") : await _api.SaveCostCenterAsync(model));

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid uid) => Done(await _api.DeleteCostCenterAsync(uid));

    private IActionResult Done(ApiResult result)
    {
        TempData[result.Success ? "Success" : "Error"] = result.Message;
        return RedirectToAction(nameof(Index));
    }
}

/// <summary>Master project (internal / client).</summary>
public class ProjectsController : Controller
{
    private readonly AccountingApiClient _api;

    public ProjectsController(AccountingApiClient api) => _api = api;

    [HttpGet]
    public async Task<IActionResult> Index(string? status) => View(new ProjectsPageViewModel
    {
        Status = status,
        Projects = await _api.GetProjectsAsync(status),
        Customers = await _api.GetProjectCustomersAsync()
    });

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProjectViewModel model)
    {
        model.Uid = Guid.Empty;
        return Done(await _api.SaveProjectAsync(model));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(ProjectViewModel model) =>
        Done(model.Uid == Guid.Empty ? new ApiResult(false, "Project tidak ditemukan.") : await _api.SaveProjectAsync(model));

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid uid) => Done(await _api.DeleteProjectAsync(uid));

    private IActionResult Done(ApiResult result)
    {
        TempData[result.Success ? "Success" : "Error"] = result.Message;
        return RedirectToAction(nameof(Index));
    }
}

/// <summary>Anggaran per cost center ATAU project × akun beban × tahun.</summary>
public class BudgetsController : Controller
{
    private readonly AccountingApiClient _api;

    public BudgetsController(AccountingApiClient api) => _api = api;

    [HttpGet]
    public async Task<IActionResult> Index(int? year, string? against)
    {
        var y = year ?? DateTime.Today.Year;
        var options = await _api.GetDimensionOptionsAsync();

        return View(new BudgetsPageViewModel
        {
            Year = y,
            Against = against,
            Budgets = await _api.GetBudgetsAsync(y, against),
            CostCenters = options.CostCenters,
            Projects = options.Projects,
            ExpenseAccounts = await _api.GetAccountOptionsAsync(type: "expense")
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(BudgetViewModel model)
    {
        model.Uid = Guid.Empty;
        return Done(await _api.SaveBudgetAsync(model), model.FiscalYear);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(BudgetViewModel model) =>
        Done(model.Uid == Guid.Empty ? new ApiResult(false, "Anggaran tidak ditemukan.") : await _api.SaveBudgetAsync(model), model.FiscalYear);

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid uid, int? year) => Done(await _api.DeleteBudgetAsync(uid), year);

    private IActionResult Done(ApiResult result, int? year)
    {
        TempData[result.Success ? "Success" : "Error"] = result.Message;
        return RedirectToAction(nameof(Index), new { year });
    }
}

/// <summary>Alokasi biaya (distribution cycle): draft → posting (jurnal baru) → batal (jurnal pembalik).</summary>
public class CostAllocationsController : Controller
{
    private readonly AccountingApiClient _api;

    public CostAllocationsController(AccountingApiClient api) => _api = api;

    [HttpGet]
    public async Task<IActionResult> Index(string? status)
    {
        ViewData["Status"] = status;
        return View(await _api.GetCostAllocationsAsync(status));
    }

    [HttpGet]
    public async Task<IActionResult> Details(Guid uid)
    {
        var model = await _api.GetCostAllocationAsync(uid);
        return model == null ? NotFoundRedirect() : View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Create() => await FormView(new CostAllocationFormViewModel
    {
        Lines = new() { new(), new() }
    });

    [HttpGet]
    public async Task<IActionResult> Edit(Guid uid)
    {
        var a = await _api.GetCostAllocationAsync(uid);
        if (a == null)
            return NotFoundRedirect();

        if (a.Status != "draft")
        {
            TempData["Error"] = "Hanya alokasi draft yang dapat diubah.";
            return RedirectToAction(nameof(Details), new { uid });
        }

        return await FormView(new CostAllocationFormViewModel
        {
            Uid = a.Uid,
            AllocationNo = a.AllocationNo,
            Name = a.Name,
            FromCostCenterId = a.FromCostCenterId,
            AllocationDate = a.AllocationDate,
            Description = a.Description,
            Lines = a.Lines
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Create(CostAllocationFormViewModel model, string? submitAction) => SaveAsync(model, submitAction);

    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Update(CostAllocationFormViewModel model, string? submitAction) => SaveAsync(model, submitAction);

    /// <summary>Preview basis &amp; bagian per penerima untuk form (JSON).</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Preview(CostAllocationFormViewModel model)
    {
        var (ok, json) = await _api.PreviewCostAllocationJsonAsync(model);
        return new ContentResult { Content = json, ContentType = "application/json", StatusCode = ok ? 200 : 400 };
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Post(Guid uid) => BackToDetails(await _api.PostCostAllocationAsync(uid), uid);

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Void(Guid uid, string? reason, DateOnly? date) =>
        BackToDetails(await _api.VoidCostAllocationAsync(uid, reason, date), uid);

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid uid)
    {
        var result = await _api.DeleteCostAllocationAsync(uid);
        TempData[result.Success ? "Success" : "Error"] = result.Message;
        return result.Success ? RedirectToAction(nameof(Index)) : RedirectToAction(nameof(Details), new { uid });
    }

    private async Task<IActionResult> SaveAsync(CostAllocationFormViewModel model, string? submitAction)
    {
        var result = await _api.SaveCostAllocationAsync(model, submitAction == "post");
        if (!result.Success)
        {
            TempData["Error"] = result.Message;
            return await FormView(model);
        }

        TempData["Success"] = result.Message;
        return result.Uid != null ? RedirectToAction(nameof(Details), new { uid = result.Uid }) : RedirectToAction(nameof(Index));
    }

    private async Task<IActionResult> FormView(CostAllocationFormViewModel model)
    {
        model.CostCenters = (await _api.GetDimensionOptionsAsync(
            model.Lines.Select(l => (long?)l.ToCostCenterId).Append(model.FromCostCenterId), Array.Empty<long?>())).CostCenters;
        return View("Form", model);
    }

    private IActionResult BackToDetails(ApiResult result, Guid uid)
    {
        TempData[result.Success ? "Success" : "Error"] = result.Message;
        return RedirectToAction(nameof(Details), new { uid });
    }

    private IActionResult NotFoundRedirect()
    {
        TempData["Error"] = "Alokasi tidak ditemukan.";
        return RedirectToAction(nameof(Index));
    }
}

/// <summary>Laporan Laba Rugi per Cost Center (cost-only).</summary>
public class CostCenterPnlController : Controller
{
    private readonly AccountingApiClient _api;

    public CostCenterPnlController(AccountingApiClient api) => _api = api;

    [HttpGet]
    public async Task<IActionResult> Index(DateOnly? from, DateOnly? to)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        return View(await _api.GetCostCenterPnlAsync(from ?? new DateOnly(today.Year, today.Month, 1), to ?? today));
    }
}

/// <summary>Laporan Profitabilitas per Project + rincian per project.</summary>
public class ProjectProfitabilityController : Controller
{
    private readonly AccountingApiClient _api;

    public ProjectProfitabilityController(AccountingApiClient api) => _api = api;

    [HttpGet]
    public async Task<IActionResult> Index(DateOnly? from, DateOnly? to)
    {
        var (f, t) = Range(from, to);
        return View(await _api.GetProjectProfitabilityAsync(f, t));
    }

    [HttpGet]
    public async Task<IActionResult> Details(Guid uid, DateOnly? from, DateOnly? to)
    {
        var (f, t) = Range(from, to);
        var model = await _api.GetProjectProfitDetailAsync(uid, f, t);
        if (model != null)
            return View(model);

        TempData["Error"] = "Project tidak ditemukan.";
        return RedirectToAction(nameof(Index));
    }

    private static (DateOnly, DateOnly) Range(DateOnly? from, DateOnly? to)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        return (from ?? new DateOnly(today.Year, 1, 1), to ?? today);
    }
}

/// <summary>Laporan Anggaran vs Realisasi.</summary>
public class BudgetVsActualController : Controller
{
    private readonly AccountingApiClient _api;

    public BudgetVsActualController(AccountingApiClient api) => _api = api;

    [HttpGet]
    public async Task<IActionResult> Index(int? year) => View(await _api.GetBudgetVsActualAsync(year ?? DateTime.Today.Year));
}
