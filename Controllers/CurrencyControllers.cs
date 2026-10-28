using ERP.Web.Models;
using ERP.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Web.Controllers;

/// <summary>Akuntansi &gt; Mata Uang &amp; Kurs: master mata uang, input kurs harian, riwayat kurs.</summary>
public class ExchangeRatesController : Controller
{
    private static readonly int[] AllowedPageSizes = { 10, 25, 50, 100 };

    private readonly AccountingApiClient _api;
    private readonly Translator _t;

    public ExchangeRatesController(AccountingApiClient api, Translator t)
    {
        _api = api;
        _t = t;
    }

    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] ExchangeRateFilterViewModel filter)
    {
        filter.Page = Math.Max(filter.Page, 1);
        filter.PageSize = AllowedPageSizes.Contains(filter.PageSize) ? filter.PageSize : 25;

        return View(new ExchangeRatesIndexViewModel
        {
            Currencies = await _api.GetCurrenciesAsync(),
            Filter = filter,
            Rates = await _api.GetExchangeRatesAsync(filter)
        });
    }

    /// <summary>Input kurs harian: satu tanggal untuk semua mata uang aktif (kolom kosong diabaikan).</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddDailyRates(DateOnly rateDate, Dictionary<string, decimal?> rates) =>
        Done(await _api.SaveDailyRatesAsync(rateDate, (rates ?? new()).Select(r => (r.Key, r.Value))));

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateRate(Guid uid, string currencyCode, DateOnly rateDate, decimal rate, string? note) =>
        Done(await _api.SaveExchangeRateAsync(uid, currencyCode, rateDate, rate, note));

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteRate(Guid uid) => Done(await _api.DeleteExchangeRateAsync(uid));

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateCurrency(CurrencyViewModel model)
    {
        model.Uid = Guid.Empty;
        return Done(await _api.SaveCurrencyAsync(model));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateCurrency(CurrencyViewModel model) =>
        Done(model.Uid == Guid.Empty ? new ApiResult(false, "Mata uang tidak ditemukan.") : await _api.SaveCurrencyAsync(model));

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteCurrency(Guid uid) => Done(await _api.DeleteCurrencyAsync(uid));

    [HttpGet]
    public async Task<IActionResult> ExportExcel([FromQuery] ExchangeRateFilterViewModel filter) => this.ExcelFile(await BuildExportAsync(filter));

    [HttpGet]
    public async Task<IActionResult> ExportPdf([FromQuery] ExchangeRateFilterViewModel filter) => await this.PdfFile(await BuildExportAsync(filter));

    [HttpGet]
    public async Task<IActionResult> PrintList([FromQuery] ExchangeRateFilterViewModel filter) => this.PrintView(await BuildExportAsync(filter));

    private async Task<ListExport> BuildExportAsync(ExchangeRateFilterViewModel filter)
    {
        var rows = await _api.GetExchangeRatesForExportAsync(filter);

        var filters = new List<string>();
        if (!string.IsNullOrWhiteSpace(filter.Currency))
            filters.Add(_t["Mata uang"] + ": " + filter.Currency);
        if (filter.DateFrom != null || filter.DateTo != null)
            filters.Add(_t["Periode"] + " " + Acc.Date(filter.DateFrom) + " " + _t["s/d"] + " " + Acc.Date(filter.DateTo));

        return new ListExport
        {
            Title = _t["Kurs Mata Uang"],
            Subtitle = filters.Count > 0 ? string.Join(" · ", filters) : _t["1 unit valas = n IDR"],
            FileName = "kurs_mata_uang",
            Headers = new[] { "Mata Uang", "Tanggal", "Kurs (IDR)", "Perubahan (%)", "Catatan", "Diinput oleh" }.Select(h => _t[h]).ToArray(),
            Rows = rows.Select(r => new object?[] { r.CurrencyCode, r.RateDate, r.Rate, r.ChangePercent, r.Note, r.CreatedByName }).ToList()
        };
    }

    private IActionResult Done(ApiResult result)
    {
        TempData[result.Success ? "Success" : "Error"] = result.Message;
        return Redirect(Request.Headers.Referer.FirstOrDefault() is { Length: > 0 } back && back.Contains("/ExchangeRates", StringComparison.OrdinalIgnoreCase)
            ? back
            : Url.Action(nameof(Index))!);
    }
}

/// <summary>Akuntansi &gt; Revaluasi Kurs: pratinjau &amp; posting revaluasi akhir periode, riwayat, pembatalan.</summary>
public class FxRevaluationsController : Controller
{
    private readonly AccountingApiClient _api;

    public FxRevaluationsController(AccountingApiClient api) => _api = api;

    [HttpGet]
    public async Task<IActionResult> Index(DateOnly? date) => View(new FxRevaluationsIndexViewModel
    {
        Revaluations = await _api.GetFxRevaluationsAsync(),
        Preview = await _api.GetFxRevaluationPreviewAsync(date)
    });

    [HttpGet]
    public async Task<IActionResult> Details(Guid uid)
    {
        var reval = await _api.GetFxRevaluationAsync(uid);
        if (reval == null)
        {
            TempData["Error"] = "Revaluasi tidak ditemukan.";
            return RedirectToAction(nameof(Index));
        }

        return View(reval);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(DateOnly revalDate, string? note)
    {
        var result = await _api.CreateFxRevaluationAsync(revalDate, note);
        TempData[result.Success ? "Success" : "Error"] = result.Message;

        return result.Success && result.Uid != null
            ? RedirectToAction(nameof(Details), new { uid = result.Uid })
            : RedirectToAction(nameof(Index), new { date = revalDate.ToString("yyyy-MM-dd") });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Void(Guid uid, string? reason)
    {
        var result = await _api.VoidFxRevaluationAsync(uid, reason);
        TempData[result.Success ? "Success" : "Error"] = result.Message;
        return RedirectToAction(nameof(Details), new { uid });
    }
}

/// <summary>Lookup untuk form dokumen (faktur, pembayaran, transaksi kas): kurs &amp; tarif PPN — cukup login.</summary>
public class LookupController : Controller
{
    private readonly AccountingApiClient _api;

    public LookupController(AccountingApiClient api) => _api = api;

    /// <summary>Kurs terakhir pada/sebelum tanggal.</summary>
    [HttpGet]
    public async Task<IActionResult> Rate(string currency, DateOnly date)
    {
        var rate = await _api.LookupRateAsync(currency, date);
        return rate == null ? NotFound(new { message = $"Belum ada kurs {currency} pada atau sebelum {Acc.Date(date)}." }) : Json(rate);
    }

    /// <summary>Tarif PPN yang berlaku pada tanggal (+ tarif draft yang sedang diedit).</summary>
    [HttpGet]
    public async Task<IActionResult> TaxRates(DateOnly date, long? include) =>
        Json(await _api.GetTaxRateOptionsAsync(date, include));

    /// <summary>Tarif PPh 23 yang berlaku pada tanggal (+ tarif draft yang sedang diedit).</summary>
    [HttpGet]
    public async Task<IActionResult> Pph23Rates(DateOnly date, long? include) =>
        Json(await _api.GetPph23RateOptionsAsync(date, include));

    /// <summary>Cek anggaran baris beban faktur pembelian (Level 11): anggaran tersentuh, proyeksi, jalur persetujuan bila melebihi.</summary>
    [HttpGet]
    public async Task<IActionResult> BudgetCheck(long accountId, long? costCenterId, long? projectId, DateOnly date, decimal amount, Guid? invoiceUid)
    {
        var json = await _api.CheckBudgetJsonAsync(accountId, costCenterId, projectId, date, amount, invoiceUid);
        return json == null ? NoContent() : Content(json, "application/json");
    }

    /// <summary>Jalur persetujuan yang akan dipakai untuk nominal (IDR) ini — HTML untuk kotak preview di form.</summary>
    [HttpGet]
    public async Task<IActionResult> ApprovalPreview(string docType, decimal amount) =>
        PartialView("~/Views/ArAp/_ApprovalPreview.cshtml", await _api.GetApprovalPreviewAsync(docType, amount));
}
