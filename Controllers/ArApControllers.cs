using ERP.Web.Models;
using ERP.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Web.Controllers;

// Hutang & piutang: satu base controller per jenis halaman, view bersama di Views/ArAp.

public abstract class InvoicesWebControllerBase : Controller
{
    private static readonly int[] AllowedPageSizes = { 10, 25, 50, 100 };

    protected readonly AccountingApiClient Api;
    protected readonly Translator _t;

    protected InvoicesWebControllerBase(AccountingApiClient api, Translator t)
    {
        Api = api;
        _t = t;
    }

    protected abstract ArApPageConfig Config { get; }

    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] ArApFilterViewModel filter)
    {
        filter.Page = Math.Max(filter.Page, 1);
        filter.PageSize = AllowedPageSizes.Contains(filter.PageSize) ? filter.PageSize : 25;

        return View("~/Views/ArAp/InvoiceIndex.cshtml", new InvoicesIndexViewModel
        {
            Config = Config,
            Filter = filter,
            Invoices = await Api.GetInvoicesAsync(Config.ApiBase, filter),
            Parties = await Api.GetPartiesAsync(Config.ApiBase)
        });
    }

    [HttpGet]
    public async Task<IActionResult> Details(Guid uid)
    {
        var inv = await Api.GetInvoiceAsync(Config.ApiBase, uid);
        if (inv == null)
            return NotFoundRedirect();

        ViewData["Config"] = Config;
        // Panel persetujuan (Faktur Pembelian): progres, riwayat, tombol setujui/tolak/tarik.
        if (Config.ApprovalDocType != null)
            ViewData["Approval"] = await Api.GetApprovalDocumentAsync(Config.ApprovalDocType, uid);
        if (Config.BudgetApprovalDocType != null)
            ViewData["BudgetApproval"] = await Api.GetApprovalDocumentAsync(Config.BudgetApprovalDocType, uid);
        return View("~/Views/ArAp/InvoiceDetails.cshtml", inv);
    }

    [HttpGet]
    public async Task<IActionResult> Print(Guid uid)
    {
        var inv = await Api.GetInvoiceAsync(Config.ApiBase, uid);
        if (inv == null)
            return NotFoundRedirect();

        ViewData["Config"] = Config;
        return View("~/Views/ArAp/InvoicePrint.cshtml", inv);
    }

    [HttpGet]
    public async Task<IActionResult> Create(Guid? receiptUid, Guid? apoUid)
    {
        var form = new InvoiceFormViewModel
        {
            ReceiptUid = Config.HasReceiptSource ? receiptUid : null,
            ApoUid = Config.HasReceiptSource && receiptUid == null ? apoUid : null
        };

        // Faktur dari PO Aset: PPN default = tarif PPN header PO.
        if (form.ApoUid != null)
            form.TaxPercent = (await Api.GetApoInvoiceOptionsAsync()).FirstOrDefault(o => o.Uid == form.ApoUid)?.TaxPercent ?? 0;

        return await FormView(form);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(InvoiceFormViewModel form, string? submitAction)
    {
        form.Uid = null;
        return await Submit(form, submitAction == "post");
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid uid)
    {
        var inv = await Api.GetInvoiceAsync(Config.ApiBase, uid);
        if (inv == null)
            return NotFoundRedirect();

        if (inv.Status != "draft")
        {
            TempData["Error"] = "Hanya faktur draft yang dapat diubah.";
            return RedirectToAction(nameof(Details), new { uid });
        }

        return await FormView(new InvoiceFormViewModel
        {
            Uid = inv.Uid,
            InvoiceNo = inv.InvoiceNo,
            PartyId = inv.PartyId,
            PartyRef = inv.PartyRef,
            InvoiceDate = inv.InvoiceDate,
            DueDate = inv.DueDate,
            AccountId = inv.AccountId,
            Description = inv.Description,
            Amount = inv.Amount,
            TaxRateId = inv.TaxRateId,
            TaxPercent = inv.TaxPercent,
            TaxInvoiceNo = inv.TaxInvoiceNo,
            Pph23RateId = inv.Pph23RateId,
            Pph23HasNpwp = inv.Pph23HasNpwp,
            Pph23Base = inv.Pph23RateId != null && inv.Pph23Base != inv.Amount ? inv.Pph23Base : null,
            CostCenterId = inv.CostCenterId,
            ProjectId = inv.ProjectId,
            CurrencyCode = inv.CurrencyCode,
            FxRate = inv.IsForeign ? inv.FxRate : null,
            ReceiptUid = inv.ReceiptUid,
            ApoUid = inv.ApoUid,
            SourceLabel = inv.SourceLabel,
            IsDeferred = inv.IsDeferred,
            DeferralStartMonth = inv.DeferralStart?.ToString("yyyy-MM"),
            DeferralMonths = inv.DeferralMonths
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(InvoiceFormViewModel form, string? submitAction)
    {
        if (form.Uid == null)
            return NotFoundRedirect();

        return await Submit(form, submitAction == "post");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid uid)
    {
        var result = await Api.DeleteInvoiceAsync(Config.ApiBase, uid);
        TempData[result.Success ? "Success" : "Error"] = result.Message;
        return result.Success ? RedirectToAction(nameof(Index)) : RedirectToAction(nameof(Details), new { uid });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Post(Guid uid) => BackToDetails(await Api.PostInvoiceAsync(Config.ApiBase, uid), uid);

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Void(Guid uid, string? reason, DateOnly? date) =>
        BackToDetails(await Api.VoidInvoiceAsync(Config.ApiBase, uid, reason, date), uid);

    /// <summary>Isi/ubah Nomor Faktur Pajak (NSFP) setelah disetujui Coretax — juga untuk faktur posted.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateTaxInvoiceNo(Guid uid, string? taxInvoiceNo) =>
        BackToDetails(await Api.SetTaxInvoiceNoAsync(Config.ApiBase, uid, taxInvoiceNo), uid);

    [HttpGet]
    public async Task<IActionResult> ExportExcel([FromQuery] ArApFilterViewModel filter) =>
        this.ExcelFile(await BuildExportAsync(filter, detailed: true));

    [HttpGet]
    public async Task<IActionResult> ExportPdf([FromQuery] ArApFilterViewModel filter) =>
        await this.PdfFile(await BuildExportAsync(filter, detailed: false));

    [HttpGet]
    public async Task<IActionResult> PrintList([FromQuery] ArApFilterViewModel filter) =>
        this.PrintView(await BuildExportAsync(filter, detailed: false));

    /// <summary>Semua faktur sesuai filter halaman. Excel memuat kolom lengkap (akun, DPP, PPN, dibayar); PDF/cetak ringkas.
    /// Total mengabaikan faktur yang dibatalkan.</summary>
    private async Task<ListExport> BuildExportAsync(ArApFilterViewModel filter, bool detailed)
    {
        var rows = await Api.GetInvoicesForExportAsync(Config.ApiBase, filter);
        var active = rows.Where(r => r.Status != "void").ToList();
        var subtitle = ArApExport.Period(_t, filter.DateFrom, filter.DateTo);
        string Status(InvoiceViewModel r) => _t[ArApPages.StatusBadge(r.Status, r.PayStatus).Label];

        if (detailed)
        {
            return new ListExport
            {
                Title = _t[Config.Title],
                Subtitle = subtitle,
                FileName = Config.Controller.ToLowerInvariant(),
                Headers = new[] { _t["No. Faktur"], _t["Tanggal"], _t["Jatuh Tempo"], _t[Config.PartyLabel], _t[Config.NumberLabel], _t["Akun"], _t["Tarif PPN"], _t["No. Faktur Pajak"], _t["Mata Uang"], _t["DPP"], _t["PPN"], _t["Total"], _t["Dibayar"], _t["Sisa"], _t["Kurs"], _t["Total (IDR)"], _t["Status"] },
                Rows = rows.Select(r => new object?[]
                {
                    r.InvoiceNo, r.InvoiceDate, r.DueDate, r.PartyName, r.PartyRef, $"{r.AccountCode} {r.AccountName}".Trim(), r.TaxRateName, r.TaxInvoiceNo, r.CurrencyCode,
                    r.Amount, r.TaxAmount, r.Total, r.PaidAmount, r.Outstanding, r.IsForeign ? r.FxRate : null, r.FunctionalTotal, Status(r)
                }).ToList(),
                // Nominal campuran mata uang tidak dijumlahkan; total IDR selalu bisa.
                Footer = new object?[] { _t["Total"], null, null, null, null, null, null, null, null,
                    SameCurrency(active) ? active.Sum(r => r.Amount) : null, SameCurrency(active) ? active.Sum(r => r.TaxAmount) : null,
                    SameCurrency(active) ? active.Sum(r => r.Total) : null, SameCurrency(active) ? active.Sum(r => r.PaidAmount) : null,
                    SameCurrency(active) ? active.Sum(r => r.Outstanding) : null, null, active.Sum(r => r.FunctionalTotal), null }
            };
        }

        return new ListExport
        {
            Title = _t[Config.Title],
            Subtitle = subtitle,
            FileName = Config.Controller.ToLowerInvariant(),
            Headers = new[] { _t["No. Faktur"], _t["Tanggal"], _t["Jatuh Tempo"], _t[Config.PartyLabel], _t[Config.NumberLabel], _t["Mata Uang"], _t["Total"], _t["Sisa"], _t["Status"] },
            Rows = rows.Select(r => new object?[] { r.InvoiceNo, r.InvoiceDate, r.DueDate, r.PartyName, r.PartyRef, r.CurrencyCode, r.Total, r.Outstanding, Status(r) }).ToList(),
            Footer = SameCurrency(active)
                ? new object?[] { _t["Total"], null, null, null, null, null, active.Sum(r => r.Total), active.Sum(r => r.Outstanding), null }
                : null
        };
    }

    /// <summary>Total nominal hanya bermakna bila semua baris bermata uang sama.</summary>
    private static bool SameCurrency(List<InvoiceViewModel> rows) => rows.Select(r => r.CurrencyCode).Distinct().Count() <= 1;

    private async Task<IActionResult> Submit(InvoiceFormViewModel form, bool post)
    {
        var result = await Api.SaveInvoiceAsync(Config.ApiBase, form, post);

        if (!result.Success)
        {
            TempData["Error"] = result.Message;
            return await FormView(form);
        }

        TempData["Success"] = result.Message;
        return result.Uid != null ? RedirectToAction(nameof(Details), new { uid = result.Uid }) : RedirectToAction(nameof(Index));
    }

    private async Task<IActionResult> FormView(InvoiceFormViewModel form)
    {
        form.Config = Config;
        form.Parties = await Api.GetPartiesAsync(Config.ApiBase);
        form.Accounts = await Api.GetAccountOptionsAsync(postable: true, manual: true, type: Config.AccountTypes);
        form.Currencies = await Api.GetCurrencyOptionsAsync();
        form.TaxRates = await Api.GetTaxRateOptionsAsync(form.InvoiceDate, form.TaxRateId);
        if (Config.IsPayable)
            form.Pph23Rates = await Api.GetPph23RateOptionsAsync(form.InvoiceDate, form.Pph23RateId);
        form.Dimensions = await Api.GetDimensionOptionsAsync(form.CostCenterId, form.ProjectId);

        // Default dari dokumen sumber (PO Aset: tarif % header) → tarif berlaku yang efektifnya sama.
        if (form.TaxRateId == null && form.TaxPercent > 0)
            form.TaxRateId = form.TaxRates.FirstOrDefault(r => r.IsInputCreditable && r.EffectiveRate == form.TaxPercent)?.Id;

        if (Config.HasReceiptSource && form.Uid == null)
        {
            form.Receipts = await Api.GetReceiptOptionsAsync();
            form.AssetOrders = await Api.GetApoInvoiceOptionsAsync();
        }

        return View("~/Views/ArAp/InvoiceForm.cshtml", form);
    }

    private IActionResult BackToDetails(ApiResult result, Guid uid)
    {
        TempData[result.Success ? "Success" : "Error"] = result.Message;
        return RedirectToAction(nameof(Details), new { uid });
    }

    private IActionResult NotFoundRedirect()
    {
        TempData["Error"] = "Faktur tidak ditemukan.";
        return RedirectToAction(nameof(Index));
    }
}

public class PurchaseInvoicesController : InvoicesWebControllerBase
{
    public PurchaseInvoicesController(AccountingApiClient api, Translator t) : base(api, t) { }

    protected override ArApPageConfig Config => ArApPages.PurchaseInvoices;
}

public class SalesInvoicesController : InvoicesWebControllerBase
{
    public SalesInvoicesController(AccountingApiClient api, Translator t) : base(api, t) { }

    protected override ArApPageConfig Config => ArApPages.SalesInvoices;
}

public abstract class PaymentsWebControllerBase : Controller
{
    private static readonly int[] AllowedPageSizes = { 10, 25, 50, 100 };

    protected readonly AccountingApiClient Api;
    protected readonly Translator _t;

    protected PaymentsWebControllerBase(AccountingApiClient api, Translator t)
    {
        Api = api;
        _t = t;
    }

    protected abstract ArApPageConfig Config { get; }

    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] ArApFilterViewModel filter)
    {
        filter.Page = Math.Max(filter.Page, 1);
        filter.PageSize = AllowedPageSizes.Contains(filter.PageSize) ? filter.PageSize : 25;

        return View("~/Views/ArAp/PaymentIndex.cshtml", new PaymentsIndexViewModel
        {
            Config = Config,
            Filter = filter,
            Payments = await Api.GetPaymentsAsync(Config.ApiBase, filter),
            Parties = await Api.GetPartiesAsync(Config.ApiBase)
        });
    }

    [HttpGet]
    public async Task<IActionResult> Details(Guid uid)
    {
        var p = await Api.GetPaymentAsync(Config.ApiBase, uid);
        if (p == null)
            return NotFoundRedirect();

        ViewData["Config"] = Config;
        if (Config.ApprovalDocType != null)
            ViewData["Approval"] = await Api.GetApprovalDocumentAsync(Config.ApprovalDocType, uid);
        return View("~/Views/ArAp/PaymentDetails.cshtml", p);
    }

    [HttpGet]
    public async Task<IActionResult> Print(Guid uid)
    {
        var p = await Api.GetPaymentAsync(Config.ApiBase, uid);
        if (p == null)
            return NotFoundRedirect();

        ViewData["Config"] = Config;
        return View("~/Views/ArAp/PaymentPrint.cshtml", p);
    }

    /// <summary>Faktur terbuka party (AJAX form pembayaran).</summary>
    [HttpGet]
    public async Task<IActionResult> OpenInvoices(Guid partyUid, Guid? paymentUid) =>
        Json(await Api.GetOpenInvoicesAsync(Config.ApiBase, partyUid, paymentUid));

    [HttpGet]
    public async Task<IActionResult> Create(Guid? partyUid)
    {
        var parties = await Api.GetPartiesAsync(Config.ApiBase);
        return await FormView(new PaymentFormViewModel { PartyId = parties.FirstOrDefault(p => p.Uid != null && p.Uid == partyUid)?.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PaymentFormViewModel form, string? submitAction)
    {
        form.Uid = null;
        return await Submit(form, submitAction == "post");
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid uid)
    {
        var p = await Api.GetPaymentAsync(Config.ApiBase, uid);
        if (p == null)
            return NotFoundRedirect();

        if (p.Status != "draft")
        {
            TempData["Error"] = "Hanya pembayaran draft yang dapat diubah.";
            return RedirectToAction(nameof(Details), new { uid });
        }

        return await FormView(new PaymentFormViewModel
        {
            Uid = p.Uid,
            PaymentNo = p.PaymentNo,
            PartyId = p.PartyId,
            PaymentDate = p.PaymentDate,
            CashAccountId = p.CashAccountId,
            FxRate = p.IsForeign ? p.FxRate : null,
            Reference = p.Reference,
            Description = p.Description,
            Allocations = p.Allocations.Select(a => new PaymentAllocationInput { InvoiceId = a.InvoiceId, Amount = a.Amount }).ToList()
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(PaymentFormViewModel form, string? submitAction)
    {
        if (form.Uid == null)
            return NotFoundRedirect();

        return await Submit(form, submitAction == "post");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid uid)
    {
        var result = await Api.DeletePaymentAsync(Config.ApiBase, uid);
        TempData[result.Success ? "Success" : "Error"] = result.Message;
        return result.Success ? RedirectToAction(nameof(Index)) : RedirectToAction(nameof(Details), new { uid });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Post(Guid uid) => BackToDetails(await Api.PostPaymentAsync(Config.ApiBase, uid), uid);

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Void(Guid uid, string? reason, DateOnly? date) =>
        BackToDetails(await Api.VoidPaymentAsync(Config.ApiBase, uid, reason, date), uid);

    [HttpGet]
    public async Task<IActionResult> ExportExcel([FromQuery] ArApFilterViewModel filter) => this.ExcelFile(await BuildExportAsync(filter));

    [HttpGet]
    public async Task<IActionResult> ExportPdf([FromQuery] ArApFilterViewModel filter) => await this.PdfFile(await BuildExportAsync(filter));

    [HttpGet]
    public async Task<IActionResult> PrintList([FromQuery] ArApFilterViewModel filter) => this.PrintView(await BuildExportAsync(filter));

    /// <summary>API pembayaran belum punya endpoint export → ambil semua halaman (maks. 100 per halaman). Total tanpa yang dibatalkan.</summary>
    private async Task<ListExport> BuildExportAsync(ArApFilterViewModel filter)
    {
        var rows = new List<PaymentViewModel>();
        var query = new ArApFilterViewModel
        {
            Q = filter.Q, PartyUid = filter.PartyUid, Status = filter.Status, DateFrom = filter.DateFrom, DateTo = filter.DateTo,
            Page = 1, PageSize = 100
        };

        while (true)
        {
            var page = await Api.GetPaymentsAsync(Config.ApiBase, query);
            rows.AddRange(page.Data);
            if (page.Data.Count == 0 || rows.Count >= page.TotalCount || query.Page >= 500)
                break;
            query.Page++;
        }

        var active = rows.Where(p => p.Status != "void").ToList();

        return new ListExport
        {
            Title = _t[Config.Title],
            Subtitle = ArApExport.Period(_t, filter.DateFrom, filter.DateTo),
            FileName = Config.Controller.ToLowerInvariant(),
            Headers = new[] { _t["No."], _t["Tanggal"], _t[Config.PartyLabel], _t["Rekening"], _t["Referensi"], _t["Keterangan"], _t["Mata Uang"], _t["Jumlah"], _t["Nilai IDR"], _t["Selisih Kurs"], _t["Status"] },
            Rows = rows.Select(p => new object?[]
            {
                p.PaymentNo, p.PaymentDate, p.PartyName, p.CashAccountName, p.Reference, p.Description, p.CurrencyCode, p.Amount, p.FunctionalAmount,
                p.FxGainLoss == 0 ? null : p.FxGainLoss, _t[ArApPages.StatusBadge(p.Status).Label]
            }).ToList(),
            Footer = new object?[]
            {
                _t["Total"], null, null, null, null, null, null,
                active.Select(p => p.CurrencyCode).Distinct().Count() <= 1 ? active.Sum(p => p.Amount) : null,
                active.Sum(p => p.FunctionalAmount), active.Sum(p => p.FxGainLoss), null
            }
        };
    }

    private async Task<IActionResult> Submit(PaymentFormViewModel form, bool post)
    {
        form.Allocations ??= new();
        var result = await Api.SavePaymentAsync(Config.ApiBase, form, post);

        if (!result.Success)
        {
            TempData["Error"] = result.Message;
            return await FormView(form);
        }

        TempData["Success"] = result.Message;
        return result.Uid != null ? RedirectToAction(nameof(Details), new { uid = result.Uid }) : RedirectToAction(nameof(Index));
    }

    private async Task<IActionResult> FormView(PaymentFormViewModel form)
    {
        form.Config = Config;
        form.Parties = await Api.GetPartiesAsync(Config.ApiBase);
        form.CashAccounts = await Api.GetCashAccountOptionsAsync();

        if (form.Parties.FirstOrDefault(p => p.Id == form.PartyId)?.Uid is { } partyUid)
            form.OpenInvoices = await Api.GetOpenInvoicesAsync(Config.ApiBase, partyUid, form.Uid);

        return View("~/Views/ArAp/PaymentForm.cshtml", form);
    }

    private IActionResult BackToDetails(ApiResult result, Guid uid)
    {
        TempData[result.Success ? "Success" : "Error"] = result.Message;
        return RedirectToAction(nameof(Details), new { uid });
    }

    private IActionResult NotFoundRedirect()
    {
        TempData["Error"] = "Pembayaran tidak ditemukan.";
        return RedirectToAction(nameof(Index));
    }
}

public class SupplierPaymentsController : PaymentsWebControllerBase
{
    public SupplierPaymentsController(AccountingApiClient api, Translator t) : base(api, t) { }

    protected override ArApPageConfig Config => ArApPages.SupplierPayments;
}

public class CustomerPaymentsController : PaymentsWebControllerBase
{
    public CustomerPaymentsController(AccountingApiClient api, Translator t) : base(api, t) { }

    protected override ArApPageConfig Config => ArApPages.CustomerPayments;
}

// ---------- Umur hutang/piutang ----------

public abstract class AgingWebControllerBase : Controller
{
    protected readonly AccountingApiClient Api;
    protected readonly Translator _t;

    protected AgingWebControllerBase(AccountingApiClient api, Translator t)
    {
        Api = api;
        _t = t;
    }

    protected abstract (string ApiPath, string Title, string PartyLabel, string InvoiceController, string CardController) Info { get; }

    [HttpGet]
    public async Task<IActionResult> Index(DateOnly? asOf) => View("~/Views/ArAp/Aging.cshtml", await LoadAsync(asOf));

    /// <summary>Cetak format lama (tetap ada untuk tautan lama); toolbar memakai PrintList.</summary>
    [HttpGet]
    public async Task<IActionResult> Print(DateOnly? asOf) => View("~/Views/ArAp/AgingPrint.cshtml", await LoadAsync(asOf));

    [HttpGet]
    public async Task<IActionResult> ExportExcel(DateOnly? asOf) => this.ExcelFile(BuildExport(await LoadAsync(asOf), withInvoices: true));

    [HttpGet]
    public async Task<IActionResult> ExportPdf(DateOnly? asOf) => await this.PdfFile(BuildExport(await LoadAsync(asOf), withInvoices: false));

    [HttpGet]
    public async Task<IActionResult> PrintList(DateOnly? asOf) => this.PrintView(BuildExport(await LoadAsync(asOf), withInvoices: false));

    private async Task<AgingViewModel> LoadAsync(DateOnly? asOf)
    {
        var model = await Api.GetAgingAsync(Info.ApiPath, asOf ?? DateOnly.FromDateTime(DateTime.Today));
        model.Title = Info.Title;
        model.PartyLabel = Info.PartyLabel;
        model.InvoiceController = Info.InvoiceController;
        model.CardController = Info.CardController;
        return model;
    }

    /// <summary>Baris per party + total (Footer). Excel juga memuat rincian faktur di bawah tiap party (sisa di kolom umurnya).</summary>
    private ListExport BuildExport(AgingViewModel m, bool withInvoices)
    {
        var rows = new List<object?[]>();
        foreach (var r in m.Rows)
        {
            rows.Add(new object?[] { r.PartyName, r.Current, r.Days1To30, r.Days31To60, r.Days61To90, r.Over90, r.Total });
            if (!withInvoices)
                continue;

            foreach (var i in r.Invoices.OrderBy(x => x.DueDate))
            {
                var bucket = i.DaysOverdue <= 0 ? 1 : i.DaysOverdue <= 30 ? 2 : i.DaysOverdue <= 60 ? 3 : i.DaysOverdue <= 90 ? 4 : 5;
                var row = new object?[7];
                row[0] = $"    {i.InvoiceNo} · {Acc.Date(i.InvoiceDate)} · {_t["JT"]} {Acc.Date(i.DueDate)}";
                row[bucket] = i.Outstanding;
                rows.Add(row);
            }
        }

        var c = m.Control;
        var subtitle = _t["Per tanggal"] + " " + Acc.Date(m.AsOf) + " · " +
            _t["Cek akun kontrol {0} {1}: total rincian {2} vs saldo buku besar {3}", c.AccountCode, c.AccountName, Acc.Money(c.SubledgerTotal), Acc.Money(c.LedgerBalance)] +
            (c.IsReconciled ? "" : " — " + _t["selisih"] + " " + Acc.Money(c.Difference));

        return new ListExport
        {
            Title = _t[m.Title],
            Subtitle = subtitle,
            FileName = Info.Title.ToLowerInvariant().Replace(' ', '_'),
            Headers = new[] { _t[m.PartyLabel], _t["Belum JT"], "1–30", "31–60", "61–90", _t["> 90 Hari"], _t["Total"] },
            Rows = rows,
            Footer = new object?[] { _t["Total"], m.Totals.Current, m.Totals.Days1To30, m.Totals.Days31To60, m.Totals.Days61To90, m.Totals.Over90, m.Totals.Total }
        };
    }
}

public class ApAgingController : AgingWebControllerBase
{
    public ApAgingController(AccountingApiClient api, Translator t) : base(api, t) { }

    protected override (string, string, string, string, string) Info =>
        ("api/ap-aging", "Umur Hutang", "Supplier", "PurchaseInvoices", "ApCard");
}

public class ArAgingController : AgingWebControllerBase
{
    public ArAgingController(AccountingApiClient api, Translator t) : base(api, t) { }

    protected override (string, string, string, string, string) Info =>
        ("api/ar-aging", "Umur Piutang", "Customer", "SalesInvoices", "ArCard");
}

// ---------- Kartu hutang/piutang ----------

public abstract class PartyCardWebControllerBase : Controller
{
    protected readonly AccountingApiClient Api;
    protected readonly Translator _t;

    protected PartyCardWebControllerBase(AccountingApiClient api, Translator t)
    {
        Api = api;
        _t = t;
    }

    protected abstract (string ApiPath, string Title, string PartyLabel, string InvoiceController, string PaymentController, bool IsPayable) Info { get; }

    [HttpGet]
    public async Task<IActionResult> Index(Guid? partyUid, DateOnly? from, DateOnly? to) =>
        View("~/Views/ArAp/PartyCard.cshtml", await LoadAsync(partyUid, from, to));

    /// <summary>Cetak format lama (tetap ada untuk tautan lama); toolbar memakai PrintList.</summary>
    [HttpGet]
    public async Task<IActionResult> Print(Guid? partyUid, DateOnly? from, DateOnly? to) =>
        View("~/Views/ArAp/PartyCardPrint.cshtml", await LoadAsync(partyUid, from, to));

    [HttpGet]
    public async Task<IActionResult> ExportExcel(Guid? partyUid, DateOnly? from, DateOnly? to) =>
        this.ExcelFile(BuildExport(await LoadAsync(partyUid, from, to)));

    [HttpGet]
    public async Task<IActionResult> ExportPdf(Guid? partyUid, DateOnly? from, DateOnly? to) =>
        await this.PdfFile(BuildExport(await LoadAsync(partyUid, from, to)));

    [HttpGet]
    public async Task<IActionResult> PrintList(Guid? partyUid, DateOnly? from, DateOnly? to) =>
        this.PrintView(BuildExport(await LoadAsync(partyUid, from, to)));

    private async Task<PartyCardViewModel> LoadAsync(Guid? partyUid, DateOnly? from, DateOnly? to)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var model = await Api.GetPartyCardAsync(Info.ApiPath, partyUid, from ?? new DateOnly(today.Year, 1, 1), to ?? today);

        model.Title = Info.Title;
        model.PartyLabel = Info.PartyLabel;
        model.InvoiceController = Info.InvoiceController;
        model.PaymentController = Info.PaymentController;
        model.IsPayable = Info.IsPayable;
        return model;
    }

    /// <summary>Saldo awal + mutasi seperti di layar; Footer = total mutasi / saldo akhir.</summary>
    private ListExport BuildExport(PartyCardViewModel m)
    {
        var rows = new List<object?[]>();
        if (m.Party != null)
        {
            rows.Add(new object?[] { null, null, null, _t["Saldo awal"], null, null, m.Opening });
            rows.AddRange(m.Lines.Select(l => new object?[] { l.Date, _t[l.DocType], l.DocNo, l.Description, l.Increase, l.Decrease, l.Balance }));
        }

        return new ListExport
        {
            Title = _t[m.Title] + (m.Party != null ? " — " + m.Party.Name : ""),
            Subtitle = _t["Periode"] + " " + Acc.Date(m.From) + " " + _t["s/d"] + " " + Acc.Date(m.To),
            FileName = Info.Title.ToLowerInvariant().Replace(' ', '_'),
            Headers = new[]
            {
                _t["Tanggal"], _t["Dokumen"], _t["No."], _t["Keterangan"],
                m.IsPayable ? _t["Hutang (+)"] : _t["Piutang (+)"], m.IsPayable ? _t["Bayar (−)"] : _t["Terima (−)"], _t["Saldo"]
            },
            Rows = rows,
            Footer = m.Party == null ? null : new object?[] { _t["Total / Saldo akhir"], null, null, null, m.TotalIncrease, m.TotalDecrease, m.Closing }
        };
    }
}

public class ApCardController : PartyCardWebControllerBase
{
    public ApCardController(AccountingApiClient api, Translator t) : base(api, t) { }

    protected override (string, string, string, string, string, bool) Info =>
        ("api/ap-card", "Kartu Hutang", "Supplier", "PurchaseInvoices", "SupplierPayments", true);
}

public class ArCardController : PartyCardWebControllerBase
{
    public ArCardController(AccountingApiClient api, Translator t) : base(api, t) { }

    protected override (string, string, string, string, string, bool) Info =>
        ("api/ar-card", "Kartu Piutang", "Customer", "SalesInvoices", "CustomerPayments", false);
}

// ---------- PPN ----------

/// <summary>Akuntansi &gt; Rekap PPN + Tutup PPN bulanan.</summary>
public class VatController : Controller
{
    private readonly AccountingApiClient _api;
    private readonly Translator _t;

    public VatController(AccountingApiClient api, Translator t)
    {
        _api = api;
        _t = t;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int? year) => View(await _api.GetVatRecapAsync(year ?? DateTime.Today.Year));

    /// <summary>Rincian faktur pajak keluaran &amp; masukan satu masa (bahan SPT Masa PPN / Coretax).</summary>
    [HttpGet]
    public async Task<IActionResult> Invoices(string period) => View(await _api.GetVatInvoicesAsync(period));

    [HttpGet]
    public async Task<IActionResult> ExportInvoicesExcel(string period)
    {
        var m = await _api.GetVatInvoicesAsync(period);
        var rows = new List<object?[]>();

        void Section(string title, List<VatInvoiceRowViewModel> list)
        {
            rows.Add(new object?[] { _t[title] });
            rows.AddRange(list.Select(r => new object?[]
            {
                r.InvoiceDate, r.InvoiceNo, r.TaxInvoiceNo, r.PartyName, r.Npwp, r.TaxRateName, r.CurrencyCode, r.Dpp, r.TaxBase, r.Tax,
                r.Creditable ? null : _t["Tidak kreditabel"]
            }));
        }

        Section("PPN Keluaran", m.Output);
        Section("PPN Masukan", m.Input);

        return this.ExcelFile(new ListExport
        {
            Title = _t["Rincian Faktur Pajak"],
            Subtitle = _t["Masa"] + " " + m.Period,
            FileName = "faktur_pajak_" + m.Period,
            Headers = new[] { "Tanggal", "No. Faktur", "No. Faktur Pajak", "Pihak", "NPWP", "Tarif PPN", "Mata Uang", "DPP (IDR)", "DPP Nilai Lain (IDR)", "PPN (IDR)", "Catatan" }
                .Select(h => _t[h]).ToArray(),
            Rows = rows
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Close(string period, string? note)
    {
        var result = await _api.CloseVatAsync(period, note);
        TempData[result.Success ? "Success" : "Error"] = result.Message;

        return RedirectToAction(nameof(Index), new { year = period.Length >= 4 ? period[..4] : null });
    }

    [HttpGet]
    public async Task<IActionResult> ExportExcel(int? year) => this.ExcelFile(await BuildExportAsync(year));

    [HttpGet]
    public async Task<IActionResult> ExportPdf(int? year) => await this.PdfFile(await BuildExportAsync(year));

    [HttpGet]
    public async Task<IActionResult> PrintList(int? year) => this.PrintView(await BuildExportAsync(year));

    /// <summary>Satu baris per masa seperti di layar (saldo kosong untuk masa yang sudah ditutup); Footer = total mutasi.</summary>
    private async Task<ListExport> BuildExportAsync(int? year)
    {
        var m = await _api.GetVatRecapAsync(year ?? DateTime.Today.Year);
        var culture = _t.IsEnglish ? System.Globalization.CultureInfo.GetCultureInfo("en-US") : Acc.Culture;

        string Status(VatMonthViewModel v) => v.IsClosed
            ? _t["Ditutup"] + (v.JournalNo != null ? " (" + v.JournalNo + ")" : "")
            : v.CanClose ? _t["Belum ditutup"] : _t[v.BlockReason];

        return new ListExport
        {
            Title = _t["Rekap & Tutup PPN"],
            Subtitle = _t["Tahun"] + " " + m.Year,
            FileName = "rekap_ppn_" + m.Year,
            Headers = new[] { "Masa", "PPN Masukan (bulan ini)", "PPN Keluaran (bulan ini)", "Saldo Masukan", "Saldo Keluaran", "Selisih", "Hasil", "Status" }
                .Select(h => _t[h]).ToArray(),
            Rows = m.Months.Select(v => new object?[]
            {
                v.Start.ToString("MMMM yyyy", culture), v.InputMovement, v.OutputMovement,
                v.IsClosed ? null : v.InputBalance, v.IsClosed ? null : v.OutputBalance, v.Net, _t[v.ResultLabel], Status(v)
            }).ToList(),
            Footer = new object?[] { _t["Total"], m.Months.Sum(v => v.InputMovement), m.Months.Sum(v => v.OutputMovement), null, null, null, null, null }
        };
    }
}

internal static class ArApExport
{
    /// <summary>Subjudul periode filter, null bila tanpa tanggal.</summary>
    public static string? Period(Translator t, DateOnly? from, DateOnly? to) =>
        from != null || to != null ? t["Periode"] + " " + Acc.Date(from) + " " + t["s/d"] + " " + Acc.Date(to) : null;
}
