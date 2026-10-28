using ERP.Web.Models;
using ERP.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Web.Controllers;

/// <summary>Akuntansi &gt; Kas &amp; Bank (master rekening).</summary>
public class CashAccountsController : Controller
{
    private readonly AccountingApiClient _api;
    private readonly Translator _t;

    public CashAccountsController(AccountingApiClient api, Translator t)
    {
        _api = api;
        _t = t;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? q, string? type, bool? active)
    {
        ViewData["Q"] = q;
        ViewData["Type"] = type;
        ViewData["Active"] = active;

        return View(new CashAccountsIndexViewModel
        {
            Accounts = Filter(await _api.GetCashAccountsAsync(), q, type, active),
            CoaOptions = await _api.GetAccountOptionsAsync(postable: true, manual: true, type: "asset"),
            Currencies = await _api.GetCurrencyOptionsAsync()
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CashAccountViewModel model)
    {
        model.Uid = Guid.Empty;
        return Done(await _api.SaveCashAccountAsync(model));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(CashAccountViewModel model) =>
        Done(model.Uid == Guid.Empty ? new ApiResult(false, "Rekening tidak ditemukan.") : await _api.SaveCashAccountAsync(model));

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid uid) => Done(await _api.DeleteCashAccountAsync(uid));

    [HttpGet]
    public async Task<IActionResult> ExportExcel(string? q, string? type, bool? active) =>
        this.ExcelFile(await BuildExportAsync(q, type, active));

    [HttpGet]
    public async Task<IActionResult> ExportPdf(string? q, string? type, bool? active) =>
        await this.PdfFile(await BuildExportAsync(q, type, active));

    [HttpGet]
    public async Task<IActionResult> PrintList(string? q, string? type, bool? active) =>
        this.PrintView(await BuildExportAsync(q, type, active));

    /// <summary>Filter daftar rekening (API mengembalikan semua rekening; jumlahnya kecil).</summary>
    private static List<CashAccountViewModel> Filter(List<CashAccountViewModel> accounts, string? q, string? type, bool? active)
    {
        IEnumerable<CashAccountViewModel> query = accounts;

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(c => new[] { c.Code, c.Name, c.BankName, c.AccountNo, c.CoaCode, c.CoaName }
                .Any(v => v != null && v.Contains(term, StringComparison.OrdinalIgnoreCase)));
        }

        if (type is "cash" or "bank")
            query = query.Where(c => c.Type == type);

        if (active != null)
            query = query.Where(c => c.IsActive == active);

        return query.ToList();
    }

    private async Task<ListExport> BuildExportAsync(string? q, string? type, bool? active)
    {
        var accounts = Filter(await _api.GetCashAccountsAsync(), q, type, active);

        var filters = new List<string>();
        if (!string.IsNullOrWhiteSpace(q))
            filters.Add(_t["Cari"] + ": " + q);
        if (type is "cash" or "bank")
            filters.Add(_t["Jenis"] + ": " + (type == "bank" ? "Bank" : _t["Kas"]));
        if (active != null)
            filters.Add(_t["Status"] + ": " + (active.Value ? _t["Aktif"] : _t["Nonaktif"]));

        return new ListExport
        {
            Title = _t["Kas & Bank"],
            Subtitle = filters.Count > 0 ? string.Join(" · ", filters) : null,
            FileName = "kas_bank_rekening",
            Headers = new[] { "Kode", "Nama", "Jenis", "Nama Bank", "No. Rekening", "Akun COA", "Mata Uang", "Saldo Valas", "Status", "Saldo (IDR)" }.Select(h => _t[h]).ToArray(),
            Rows = accounts.Select(c => new object?[]
            {
                c.Code, c.Name, c.Type == "bank" ? "Bank" : _t["Kas"], c.BankName, c.AccountNo, $"{c.CoaCode} {c.CoaName}".Trim(),
                c.CurrencyCode, c.IsForeign ? c.ForeignBalance : null, c.IsActive ? _t["Aktif"] : _t["Nonaktif"], c.Balance
            }).ToList(),
            Footer = new object?[] { _t["Total"], null, null, null, null, null, null, null, null, accounts.Sum(c => c.Balance) }
        };
    }

    private IActionResult Done(ApiResult result)
    {
        TempData[result.Success ? "Success" : "Error"] = result.Message;
        return RedirectToAction(nameof(Index));
    }
}

/// <summary>Akuntansi &gt; Transaksi Kas &amp; Bank (masuk / keluar / transfer).</summary>
public class CashTransactionsController : Controller
{
    private static readonly int[] AllowedPageSizes = { 10, 25, 50, 100 };

    private readonly AccountingApiClient _api;
    private readonly BrandingService _branding;
    private readonly Translator _t;

    public CashTransactionsController(AccountingApiClient api, BrandingService branding, Translator t)
    {
        _api = api;
        _branding = branding;
        _t = t;
    }

    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] CashTransactionFilterViewModel filter)
    {
        filter.Page = Math.Max(filter.Page, 1);
        filter.PageSize = AllowedPageSizes.Contains(filter.PageSize) ? filter.PageSize : 25;

        return View(new CashTransactionsIndexViewModel
        {
            Filter = filter,
            Transactions = await _api.GetCashTransactionsAsync(filter),
            CashAccounts = await _api.GetCashAccountOptionsAsync()
        });
    }

    [HttpGet]
    public async Task<IActionResult> Details(Guid uid)
    {
        var trx = await _api.GetCashTransactionAsync(uid);
        if (trx == null)
            return NotFoundRedirect();

        ViewData["AttachmentUrl"] = _branding.AssetUrl(trx.Attachment);

        // Panel persetujuan: pengeluaran Kas Kecil (draft) dan pembatalan (posted).
        if (trx.Type == "out")
            ViewData["ApprovalExpense"] = await _api.GetApprovalDocumentAsync("petty_cash_expense", uid);
        if (trx.Status != "draft")
            ViewData["ApprovalUnpost"] = await _api.GetApprovalDocumentAsync("cash_transaction_unpost", uid);

        return View(trx);
    }

    [HttpGet]
    /// <remarks>Parameter isian awal dipakai layar Rekonsiliasi Bank untuk mencatat item baru dari rekening koran (biaya adm, bunga).</remarks>
    public async Task<IActionResult> Create(string? type, long? cashAccountId, DateOnly? trxDate, decimal? amount, string? description, string? reference) =>
        await FormView(new CashTransactionFormViewModel
        {
            Type = type is "out" or "transfer" ? type : "in",
            CashAccountId = cashAccountId ?? 0,
            TrxDate = trxDate ?? DateOnly.FromDateTime(DateTime.Today),
            Amount = amount ?? 0,
            Description = description,
            Reference = reference
        });

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CashTransactionFormViewModel form, string? submitAction)
    {
        form.Uid = null;
        return await Submit(form, submitAction == "post");
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid uid)
    {
        var trx = await _api.GetCashTransactionAsync(uid);
        if (trx == null)
            return NotFoundRedirect();

        if (trx.Status != "draft")
        {
            TempData["Error"] = "Hanya transaksi draft yang dapat diubah.";
            return RedirectToAction(nameof(Details), new { uid });
        }

        return await FormView(new CashTransactionFormViewModel
        {
            Uid = trx.Uid,
            TrxNo = trx.TrxNo,
            TrxDate = trx.TrxDate,
            Type = trx.Type,
            CashAccountId = trx.CashAccountId,
            DestCashAccountId = trx.DestCashAccountId,
            CounterAccountId = trx.CounterAccountId,
            CostCenterId = trx.CostCenterId,
            ProjectId = trx.ProjectId,
            Amount = trx.Amount,
            FxRate = trx.HasForeign ? trx.FxRate : null,
            Description = trx.Description,
            Reference = trx.Reference
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(CashTransactionFormViewModel form, string? submitAction)
    {
        if (form.Uid == null)
            return NotFoundRedirect();

        return await Submit(form, submitAction == "post");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid uid)
    {
        var result = await _api.DeleteCashTransactionAsync(uid);
        TempData[result.Success ? "Success" : "Error"] = result.Message;

        return result.Success ? RedirectToAction(nameof(Index)) : RedirectToAction(nameof(Details), new { uid });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Post(Guid uid) => BackToDetails(await _api.PostCashTransactionAsync(uid), uid);

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Void(Guid uid, string? reason, DateOnly? date) =>
        BackToDetails(await _api.VoidCashTransactionAsync(uid, reason, date), uid);

    [HttpGet]
    public async Task<IActionResult> Print(Guid uid)
    {
        var trx = await _api.GetCashTransactionAsync(uid);
        return trx == null ? NotFoundRedirect() : View(trx);
    }

    [HttpGet]
    public async Task<IActionResult> ExportExcel([FromQuery] CashTransactionFilterViewModel filter) =>
        this.ExcelFile(await BuildExportAsync(filter, full: true));

    [HttpGet]
    public async Task<IActionResult> ExportPdf([FromQuery] CashTransactionFilterViewModel filter) =>
        await this.PdfFile(await BuildExportAsync(filter, full: false));

    [HttpGet]
    public async Task<IActionResult> PrintList([FromQuery] CashTransactionFilterViewModel filter) =>
        this.PrintView(await BuildExportAsync(filter, full: false));

    /// <summary>Semua transaksi sesuai filter halaman. Excel memuat kolom lengkap (referensi, no. jurnal); PDF/cetak lebih ringkas.</summary>
    private async Task<ListExport> BuildExportAsync(CashTransactionFilterViewModel filter, bool full)
    {
        var rows = await _api.GetCashTransactionsForExportAsync(filter);

        var filters = new List<string>();
        if (filter.DateFrom != null || filter.DateTo != null)
            filters.Add(_t["Periode"] + " " + Acc.Date(filter.DateFrom) + " " + _t["s/d"] + " " + Acc.Date(filter.DateTo));
        if (filter.CashAccountUid != null)
        {
            var account = (await _api.GetCashAccountOptionsAsync()).FirstOrDefault(c => c.Uid == filter.CashAccountUid);
            if (account != null)
                filters.Add(_t["Rekening"] + ": " + account.Name);
        }

        string Counter(CashTransactionViewModel t) =>
            t.Type == "transfer" ? t.DestCashAccountName ?? "" : $"{t.CounterCode} {t.CounterName}".Trim();

        var headers = full
            ? new[] { "No. Transaksi", "Tanggal", "Jenis", "Rekening", "Lawan / Tujuan", "Keterangan", "Referensi", "Mata Uang", "Nominal", "Kurs", "Nilai IDR", "Selisih Kurs", "Status", "No. Jurnal" }
            : new[] { "No. Transaksi", "Tanggal", "Jenis", "Rekening", "Lawan / Tujuan", "Keterangan", "Nominal", "Status" };

        return new ListExport
        {
            Title = _t["Transaksi Kas & Bank"],
            Subtitle = filters.Count > 0 ? string.Join(" · ", filters) : null,
            FileName = "kas_bank",
            Headers = headers.Select(h => _t[h]).ToArray(),
            Rows = rows.Select(t => full
                ? new object?[]
                {
                    t.TrxNo, t.TrxDate, _t[t.TypeLabel], t.CashAccountName, Counter(t), t.Description, t.Reference, t.CurrencyCode, t.Amount,
                    t.HasForeign ? t.FxRate : null, t.FunctionalAmount, t.FxGainLoss == 0 ? null : t.FxGainLoss, _t[t.StatusBadge.Label], t.JournalNo
                }
                : new object?[]
                {
                    t.TrxNo, t.TrxDate, _t[t.TypeLabel], t.CashAccountName, Counter(t), t.Description, t.Amount, _t[t.StatusBadge.Label]
                }).ToList()
        };
    }

    private async Task<IActionResult> Submit(CashTransactionFormViewModel form, bool post)
    {
        var result = await _api.SaveCashTransactionAsync(form, post);

        if (!result.Success)
        {
            TempData["Error"] = result.Message;
            return await FormView(form);
        }

        TempData["Success"] = result.Message;
        return result.Uid != null ? RedirectToAction(nameof(Details), new { uid = result.Uid }) : RedirectToAction(nameof(Index));
    }

    private async Task<IActionResult> FormView(CashTransactionFormViewModel form)
    {
        form.CashAccounts = await _api.GetCashAccountOptionsAsync();
        form.CounterAccounts = await _api.GetAccountOptionsAsync(postable: true, manual: true);

        // Akun kas/bank tidak boleh jadi akun lawan (pakai jenis Transfer).
        var cashCoa = form.CashAccounts.Select(c => c.CoaAccountId).ToHashSet();
        form.CounterAccounts = form.CounterAccounts.Where(a => !cashCoa.Contains(a.Id)).ToList();
        form.Dimensions = await _api.GetDimensionOptionsAsync(form.CostCenterId, form.ProjectId);

        return View("Form", form);
    }

    private IActionResult BackToDetails(ApiResult result, Guid uid)
    {
        TempData[result.Success ? "Success" : "Error"] = result.Message;
        return RedirectToAction(nameof(Details), new { uid });
    }

    private IActionResult NotFoundRedirect()
    {
        TempData["Error"] = "Transaksi tidak ditemukan.";
        return RedirectToAction(nameof(Index));
    }
}

/// <summary>Akuntansi &gt; Kas Kecil (isi dana &amp; pengeluaran rekening KAS-KECIL).</summary>
public class PettyCashController : Controller
{
    private static readonly int[] AllowedPageSizes = { 10, 25, 50, 100 };

    /// <summary>Batas pageSize API petty-cash/transactions (Math.Clamp 10..100).</summary>
    private const int ExportPageSize = 100;

    private readonly AccountingApiClient _api;
    private readonly BrandingService _branding;
    private readonly Translator _t;

    public PettyCashController(AccountingApiClient api, BrandingService branding, Translator t)
    {
        _api = api;
        _branding = branding;
        _t = t;
    }

    [HttpGet]
    public async Task<IActionResult> Index(DateOnly? dateFrom, DateOnly? dateTo, int page = 1, int pageSize = 25)
    {
        pageSize = AllowedPageSizes.Contains(pageSize) ? pageSize : 25;

        var model = new PettyCashPageViewModel
        {
            Summary = await _api.GetPettyCashSummaryAsync(),
            DateFrom = dateFrom,
            DateTo = dateTo
        };

        if (model.Summary.Account != null)
        {
            model.Transactions = await _api.GetPettyCashTransactionsAsync(dateFrom, dateTo, Math.Max(page, 1), pageSize);
            model.ExpenseAccounts = (await _api.GetAccountOptionsAsync(postable: true, manual: true, type: "expense,asset,liability"))
                .Where(a => a.Id != model.Summary.Account.CoaAccountId && model.Summary.Sources.All(s => s.CoaAccountId != a.Id))
                .ToList();
            model.Dimensions = await _api.GetDimensionOptionsAsync();
        }

        ViewData["AssetBase"] = _branding.AssetUrl("/");
        ViewData["PageSize"] = pageSize;
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Topup(DateOnly trxDate, long sourceId, decimal amount, string? description, string? reference) =>
        Done(await _api.PettyCashTopupAsync(trxDate, sourceId, amount, description, reference));

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> Expense(DateOnly trxDate, long counterAccountId, decimal amount, string? description,
        string? reference, string? saveMode, IFormFile? attachment, long? costCenterId, long? projectId) =>
        Done(await _api.PettyCashExpenseAsync(trxDate, counterAccountId, amount, description, reference, saveMode != "draft", attachment,
            costCenterId, projectId));

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Post(Guid uid) => Done(await _api.PostPettyCashAsync(uid));

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Void(Guid uid, string? reason, DateOnly? date) => Done(await _api.VoidPettyCashAsync(uid, reason, date));

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid uid) => Done(await _api.DeletePettyCashAsync(uid));

    [HttpGet]
    public async Task<IActionResult> ExportExcel(DateOnly? dateFrom, DateOnly? dateTo) =>
        this.ExcelFile(await BuildExportAsync(dateFrom, dateTo));

    [HttpGet]
    public async Task<IActionResult> ExportPdf(DateOnly? dateFrom, DateOnly? dateTo) =>
        await this.PdfFile(await BuildExportAsync(dateFrom, dateTo));

    [HttpGet]
    public async Task<IActionResult> PrintList(DateOnly? dateFrom, DateOnly? dateTo) =>
        this.PrintView(await BuildExportAsync(dateFrom, dateTo));

    /// <summary>Semua transaksi Kas Kecil sesuai filter tanggal (API tanpa endpoint export → ambil per halaman 100).</summary>
    private async Task<ListExport> BuildExportAsync(DateOnly? dateFrom, DateOnly? dateTo)
    {
        var summary = await _api.GetPettyCashSummaryAsync();
        var rows = new List<CashTransactionViewModel>();

        if (summary.Account != null)
        {
            for (var page = 1; ; page++)
            {
                var result = await _api.GetPettyCashTransactionsAsync(dateFrom, dateTo, page, ExportPageSize);
                rows.AddRange(result.Data);
                if (result.Data.Count == 0 || page >= result.TotalPage)
                    break;
            }
        }

        var subtitle = new List<string>();
        if (summary.Account != null)
            subtitle.Add(summary.Account.Name + " · " + _t["Saldo"] + " " + Acc.Money(summary.Account.Balance));
        if (dateFrom != null || dateTo != null)
            subtitle.Add(_t["Periode"] + " " + Acc.Date(dateFrom) + " " + _t["s/d"] + " " + Acc.Date(dateTo));

        return new ListExport
        {
            Title = _t["Kas Kecil"],
            Subtitle = subtitle.Count > 0 ? string.Join(" · ", subtitle) : null,
            FileName = "kas_kecil",
            Headers = new[] { "No. Transaksi", "Tanggal", "Keterangan", "Akun / Sumber", "Referensi", "Masuk / (Keluar)", "Status" }
                .Select(h => _t[h]).ToArray(),
            Rows = rows.Select(t => new object?[]
            {
                t.TrxNo, t.TrxDate, t.Description,
                t.Type == "transfer" ? _t["dari {0}", t.CashAccountName] : $"{t.CounterCode} {t.CounterName}".Trim(),
                t.Reference, t.Signed, _t[t.StatusBadge.Label]
            }).ToList(),
            Footer = new object?[] { _t["Total posted"], null, null, null, null, rows.Where(t => t.Status == "posted").Sum(t => t.Signed), null }
        };
    }

    private IActionResult Done(ApiResult result)
    {
        TempData[result.Success ? "Success" : "Error"] = result.Message;
        return RedirectToAction(nameof(Index));
    }
}
