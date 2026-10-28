using ERP.Web.Models;
using ERP.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Web.Controllers;

// Pembelian aset: PR Aset → RFQ Aset → PO Aset (persetujuan, penerimaan) → Faktur (Faktur Pembelian) → Retur Aset.

public abstract class AssetPurchaseWebControllerBase : Controller
{
    protected static readonly int[] AllowedPageSizes = { 10, 25, 50, 100 };

    /// <summary>Batas pageSize API pembelian aset (Math.Clamp 10..100) — export mengambil semua halaman.</summary>
    private const int ExportPageSize = 100;

    protected readonly AssetPurchaseApiClient Api;

    protected AssetPurchaseWebControllerBase(AssetPurchaseApiClient api) => Api = api;

    protected static void Normalize(AssetPurchaseFilterViewModel f)
    {
        f.Page = Math.Max(f.Page, 1);
        f.PageSize = AllowedPageSizes.Contains(f.PageSize) ? f.PageSize : 25;
    }

    protected IActionResult Back(ApiResult result, string action, Guid uid)
    {
        TempData[result.Success ? "Success" : "Error"] = result.Message;
        return RedirectToAction(action, new { uid });
    }

    protected IActionResult NotFoundRedirect(string message)
    {
        TempData["Error"] = message;
        return RedirectToAction("Index");
    }

    protected Translator Tr => HttpContext.RequestServices.GetRequiredService<Translator>();

    /// <summary>Semua baris sesuai filter halaman (tanpa paging) — loop halaman karena API membatasi pageSize 100.</summary>
    protected static async Task<List<T>> AllRowsAsync<T>(AssetPurchaseFilterViewModel filter, Func<AssetPurchaseFilterViewModel, Task<PagedResult<T>>> fetch)
    {
        var f = new AssetPurchaseFilterViewModel { Q = filter.Q, Status = filter.Status, SupplierUid = filter.SupplierUid, Page = 1, PageSize = ExportPageSize };
        var all = new List<T>();

        while (true)
        {
            var result = await fetch(f);
            all.AddRange(result.Data);
            if (result.Data.Count < ExportPageSize || all.Count >= result.TotalCount || f.Page >= 500)
                break;
            f.Page++;
        }

        return all;
    }

    /// <summary>Keterangan filter aktif untuk subjudul export.</summary>
    protected string? FilterSubtitle(AssetPurchaseFilterViewModel f, (string Value, string Label)[] statuses, string? supplierName = null)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(f.Q)) parts.Add(Tr["Cari"] + ": " + f.Q);
        if (!string.IsNullOrWhiteSpace(supplierName)) parts.Add(Tr["Supplier"] + ": " + supplierName);
        if (!string.IsNullOrWhiteSpace(f.Status)) parts.Add(Tr["Status"] + ": " + Tr[AssetPurchaseLabels.Label(statuses, f.Status)]);
        return parts.Count == 0 ? null : string.Join(" · ", parts);
    }

    protected string[] Headers(params string[] headers) => headers.Select(h => Tr[h]).ToArray();

    protected static List<AssetPurchaseItemViewModel> CleanItems(List<AssetPurchaseItemViewModel>? items) =>
        (items ?? new()).Where(i => !string.IsNullOrWhiteSpace(i.Name)).ToList();
}

/// <summary>Purchase Request &gt; PR Aset.</summary>
public class AssetPurchaseRequestsController : AssetPurchaseWebControllerBase
{
    private readonly AccountingApiClient _acc;
    private readonly AccessService _access;

    public AssetPurchaseRequestsController(AssetPurchaseApiClient api, AccountingApiClient acc, AccessService access) : base(api)
    {
        _acc = acc;
        _access = access;
    }

    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] AssetPurchaseFilterViewModel filter)
    {
        Normalize(filter);
        return View(new AssetPurchaseIndexViewModel<AssetPurchaseRequestViewModel> { Filter = filter, Rows = await Api.GetRequestsAsync(filter) });
    }

    [HttpGet]
    public async Task<IActionResult> ExportExcel([FromQuery] AssetPurchaseFilterViewModel filter) => this.ExcelFile(await BuildExportAsync(filter));

    [HttpGet]
    public async Task<IActionResult> ExportPdf([FromQuery] AssetPurchaseFilterViewModel filter) => await this.PdfFile(await BuildExportAsync(filter));

    [HttpGet]
    public async Task<IActionResult> PrintList([FromQuery] AssetPurchaseFilterViewModel filter) => this.PrintView(await BuildExportAsync(filter));

    private async Task<ListExport> BuildExportAsync(AssetPurchaseFilterViewModel filter)
    {
        var rows = await AllRowsAsync(filter, Api.GetRequestsAsync);

        return new ListExport
        {
            Title = Tr["PR Aset"],
            Subtitle = FilterSubtitle(filter, AssetPurchaseLabels.RequestStatuses),
            FileName = "pr_aset",
            Headers = Headers("No. PR", "Tanggal", "Dibutuhkan", "Pemohon", "Departemen", "Baris", "Total Estimasi", "Status"),
            Rows = rows.Select(r => new object?[]
            {
                r.AprNo, r.AprDate, r.NeededDate, r.Requester, r.Department, r.ItemCount, r.TotalAmount,
                Tr[AssetPurchaseLabels.Label(AssetPurchaseLabels.RequestStatuses, r.Status)]
            }).ToList(),
            Footer = new object?[] { Tr["Total"], null, null, null, null, rows.Sum(r => r.ItemCount), rows.Sum(r => r.TotalAmount), null }
        };
    }

    [HttpGet]
    public async Task<IActionResult> Details(Guid uid)
    {
        var pr = await Api.GetRequestAsync(uid);
        if (pr == null)
            return NotFoundRedirect("PR Aset tidak ditemukan.");

        var approval = await _acc.GetApprovalDocumentAsync("asset_purchase_requests", uid);
        if (approval != null)
            approval.ReturnUrl = Url.Action(nameof(Details), new { uid });

        return View(new AssetPurchaseRequestDetailsPageViewModel
        {
            Request = pr,
            Approval = approval,
            Suppliers = pr.Status == "approved" ? (await Api.GetRequestFormOptionsAsync()).Suppliers : new(),
            CanCreateRfq = pr.Status == "approved" && await _access.CanAsync("asset-rfqs", "create")
        });
    }

    [HttpGet]
    public async Task<IActionResult> Create() =>
        await FormView(new AssetPurchaseRequestFormViewModel { Items = new() { new AssetPurchaseItemViewModel() } });

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AssetPurchaseRequestFormViewModel form)
    {
        form.Uid = null;
        return await Submit(form);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid uid)
    {
        var pr = await Api.GetRequestAsync(uid);
        if (pr == null)
            return NotFoundRedirect("PR Aset tidak ditemukan.");

        if (pr.Status != "draft")
        {
            TempData["Error"] = "PR yang sudah diproses tidak dapat diedit.";
            return RedirectToAction(nameof(Details), new { uid });
        }

        return await FormView(new AssetPurchaseRequestFormViewModel
        {
            Uid = pr.Uid,
            AprNo = pr.AprNo,
            AprDate = pr.AprDate ?? DateOnly.FromDateTime(DateTime.Today),
            NeededDate = pr.NeededDate,
            Department = pr.Department,
            Requester = pr.Requester,
            Note = pr.Note,
            Items = pr.Items
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(AssetPurchaseRequestFormViewModel form) =>
        form.Uid == null ? NotFoundRedirect("PR Aset tidak ditemukan.") : await Submit(form);

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid uid)
    {
        var result = await Api.DeleteRequestAsync(uid);
        TempData[result.Success ? "Success" : "Error"] = result.Message;
        return result.Success ? RedirectToAction(nameof(Index)) : RedirectToAction(nameof(Details), new { uid });
    }

    /// <summary>Ajukan (alur persetujuan) / setujui langsung bila tidak ada alur.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(Guid uid, string? reason) =>
        Back(await Api.SubmitRequestAsync(uid, reason), nameof(Details), uid);

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConvertToPo(Guid uid, long? supplierId, DateOnly? apoDate)
    {
        var result = await Api.ConvertRequestAsync(uid, supplierId, apoDate);
        TempData[result.Success ? "Success" : "Error"] = result.Message;

        return result.Success && result.Uid != null
            ? RedirectToAction("Details", "AssetPurchaseOrders", new { uid = result.Uid })
            : RedirectToAction(nameof(Details), new { uid });
    }

    private async Task<IActionResult> FormView(AssetPurchaseRequestFormViewModel form)
    {
        form.Options = await Api.GetRequestFormOptionsAsync();
        if (form.Items.Count == 0)
            form.Items.Add(new AssetPurchaseItemViewModel());
        return View("Form", form);
    }

    private async Task<IActionResult> Submit(AssetPurchaseRequestFormViewModel form)
    {
        form.Items = CleanItems(form.Items);
        var result = await Api.SaveRequestAsync(form);

        if (!result.Success)
        {
            TempData["Error"] = result.Message;
            return await FormView(form);
        }

        TempData["Success"] = result.Message;
        return RedirectToAction(nameof(Details), new { uid = result.Uid ?? form.Uid });
    }
}

/// <summary>Pembelian &gt; RFQ Aset.</summary>
public class AssetRfqsController : AssetPurchaseWebControllerBase
{
    public AssetRfqsController(AssetPurchaseApiClient api) : base(api)
    {
    }

    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] AssetPurchaseFilterViewModel filter)
    {
        Normalize(filter);
        return View(new AssetPurchaseIndexViewModel<AssetRfqViewModel>
        {
            Filter = filter,
            Rows = await Api.GetRfqsAsync(filter),
            Suppliers = (await Api.GetRfqFormOptionsAsync()).Suppliers
        });
    }

    [HttpGet]
    public async Task<IActionResult> ExportExcel([FromQuery] AssetPurchaseFilterViewModel filter) => this.ExcelFile(await BuildExportAsync(filter));

    [HttpGet]
    public async Task<IActionResult> ExportPdf([FromQuery] AssetPurchaseFilterViewModel filter) => await this.PdfFile(await BuildExportAsync(filter));

    [HttpGet]
    public async Task<IActionResult> PrintList([FromQuery] AssetPurchaseFilterViewModel filter) => this.PrintView(await BuildExportAsync(filter));

    private async Task<ListExport> BuildExportAsync(AssetPurchaseFilterViewModel filter)
    {
        var rows = await AllRowsAsync(filter, Api.GetRfqsAsync);
        var supplier = filter.SupplierUid == null ? null
            : (await Api.GetRfqFormOptionsAsync()).Suppliers.FirstOrDefault(s => s.Uid == filter.SupplierUid)?.Name;

        return new ListExport
        {
            Title = Tr["RFQ Aset"],
            Subtitle = FilterSubtitle(filter, AssetPurchaseLabels.RfqStatuses, supplier),
            FileName = "rfq_aset",
            Headers = Headers("No. RFQ", "Tanggal", "Berlaku s/d", "Supplier", "PR Aset", "Nilai", "Status"),
            Rows = rows.Select(r => new object?[]
            {
                r.RfqNo, r.RfqDate, r.ValidUntil, r.SupplierName, r.Request?.No, r.SubtotalAmount,
                Tr[AssetPurchaseLabels.Label(AssetPurchaseLabels.RfqStatuses, r.Status)]
            }).ToList(),
            Footer = new object?[] { Tr["Total"], null, null, null, null, rows.Sum(r => r.SubtotalAmount), null }
        };
    }

    [HttpGet]
    public async Task<IActionResult> Details(Guid uid)
    {
        var rfq = await Api.GetRfqAsync(uid);
        return rfq == null ? NotFoundRedirect("RFQ Aset tidak ditemukan.") : View(rfq);
    }

    /// <summary>RFQ baru, opsional dari PR Aset disetujui (baris disalin).</summary>
    [HttpGet]
    public async Task<IActionResult> Create(Guid? prUid)
    {
        var form = new AssetRfqFormViewModel();

        if (prUid != null)
        {
            var pr = await Api.GetRequestAsync(prUid.Value);
            if (pr == null || pr.Status != "approved")
            {
                TempData["Error"] = "Hanya PR Aset disetujui yang dapat dibuatkan RFQ.";
                return RedirectToAction(nameof(Index));
            }

            form.PrUid = pr.Uid;
            form.PrNo = pr.AprNo;
            form.Note = $"Dari PR Aset {pr.AprNo}";
            form.Items = pr.Items;
        }

        return await FormView(form);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AssetRfqFormViewModel form)
    {
        form.Uid = null;
        return await Submit(form);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid uid)
    {
        var rfq = await Api.GetRfqAsync(uid);
        if (rfq == null)
            return NotFoundRedirect("RFQ Aset tidak ditemukan.");

        if (rfq.Status != "draft")
        {
            TempData["Error"] = "RFQ yang sudah diproses tidak dapat diedit.";
            return RedirectToAction(nameof(Details), new { uid });
        }

        return await FormView(new AssetRfqFormViewModel
        {
            Uid = rfq.Uid,
            RfqNo = rfq.RfqNo,
            RfqDate = rfq.RfqDate,
            ValidUntil = rfq.ValidUntil,
            SupplierId = rfq.SupplierId,
            PrUid = rfq.Request?.Uid,
            PrNo = rfq.Request?.No,
            Note = rfq.Note,
            Items = rfq.Items
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(AssetRfqFormViewModel form) =>
        form.Uid == null ? NotFoundRedirect("RFQ Aset tidak ditemukan.") : await Submit(form);

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid uid)
    {
        var result = await Api.DeleteRfqAsync(uid);
        TempData[result.Success ? "Success" : "Error"] = result.Message;
        return result.Success ? RedirectToAction(nameof(Index)) : RedirectToAction(nameof(Details), new { uid });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Send(Guid uid) => Back(await Api.SendRfqAsync(uid), nameof(Details), uid);

    /// <summary>Simpan harga penawaran (quoted[uid baris] = harga).</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Quote(Guid uid, Dictionary<Guid, decimal>? quoted) =>
        Back(await Api.QuoteRfqAsync(uid, (quoted ?? new()).Select(x => (x.Key, x.Value))), nameof(Details), uid);

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(Guid uid) => Back(await Api.CancelRfqAsync(uid), nameof(Details), uid);

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConvertToPo(Guid uid)
    {
        var result = await Api.ConvertRfqAsync(uid);
        TempData[result.Success ? "Success" : "Error"] = result.Message;

        return result.Success && result.Uid != null
            ? RedirectToAction("Details", "AssetPurchaseOrders", new { uid = result.Uid })
            : RedirectToAction(nameof(Details), new { uid });
    }

    private async Task<IActionResult> FormView(AssetRfqFormViewModel form)
    {
        form.Options = await Api.GetRfqFormOptionsAsync();
        if (form.Items.Count == 0)
            form.Items.Add(new AssetPurchaseItemViewModel());
        return View("Form", form);
    }

    private async Task<IActionResult> Submit(AssetRfqFormViewModel form)
    {
        form.Items = CleanItems(form.Items);
        var result = await Api.SaveRfqAsync(form);

        if (!result.Success)
        {
            TempData["Error"] = result.Message;
            return await FormView(form);
        }

        TempData["Success"] = result.Message;
        return RedirectToAction(nameof(Details), new { uid = result.Uid ?? form.Uid });
    }
}

/// <summary>Pembelian &gt; PO Aset (persetujuan, penerimaan, pembatalan).</summary>
public class AssetPurchaseOrdersController : AssetPurchaseWebControllerBase
{
    private readonly AccountingApiClient _acc;
    private readonly AccessService _access;

    public AssetPurchaseOrdersController(AssetPurchaseApiClient api, AccountingApiClient acc, AccessService access) : base(api)
    {
        _acc = acc;
        _access = access;
    }

    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] AssetPurchaseFilterViewModel filter)
    {
        Normalize(filter);
        return View(new AssetPurchaseIndexViewModel<AssetPurchaseOrderViewModel>
        {
            Filter = filter,
            Rows = await Api.GetOrdersAsync(filter),
            Suppliers = (await Api.GetOrderFormOptionsAsync()).Suppliers
        });
    }

    [HttpGet]
    public async Task<IActionResult> ExportExcel([FromQuery] AssetPurchaseFilterViewModel filter) => this.ExcelFile(await BuildExportAsync(filter));

    [HttpGet]
    public async Task<IActionResult> ExportPdf([FromQuery] AssetPurchaseFilterViewModel filter) => await this.PdfFile(await BuildExportAsync(filter));

    [HttpGet]
    public async Task<IActionResult> PrintList([FromQuery] AssetPurchaseFilterViewModel filter) => this.PrintView(await BuildExportAsync(filter));

    private async Task<ListExport> BuildExportAsync(AssetPurchaseFilterViewModel filter)
    {
        var rows = await AllRowsAsync(filter, Api.GetOrdersAsync);
        var supplier = filter.SupplierUid == null ? null
            : (await Api.GetOrderFormOptionsAsync()).Suppliers.FirstOrDefault(s => s.Uid == filter.SupplierUid)?.Name;

        return new ListExport
        {
            Title = Tr["PO Aset"],
            Subtitle = FilterSubtitle(filter, AssetPurchaseLabels.OrderStatuses, supplier),
            FileName = "po_aset",
            Headers = Headers("No. PO", "Tanggal", "Supplier", "Asal", "Unit Diterima", "Unit Dipesan", "Total", "Status"),
            Rows = rows.Select(o => new object?[]
            {
                o.ApoNo, o.ApoDate, o.SupplierName, o.Rfq?.No ?? o.Request?.No, o.UnitsReceived, o.UnitsOrdered, o.TotalAmount,
                Tr[AssetPurchaseLabels.Label(AssetPurchaseLabels.OrderStatuses, o.Status)]
                    + (o.CancelPending ? " — " + Tr["Pembatalan menunggu persetujuan"] : "")
            }).ToList(),
            Footer = new object?[] { Tr["Total"], null, null, null, rows.Sum(o => o.UnitsReceived), rows.Sum(o => o.UnitsOrdered), rows.Sum(o => o.TotalAmount), null }
        };
    }

    [HttpGet]
    public async Task<IActionResult> Details(Guid uid)
    {
        var po = await Api.GetOrderAsync(uid);
        if (po == null)
            return NotFoundRedirect("PO Aset tidak ditemukan.");

        var returnUrl = Url.Action(nameof(Details), new { uid });
        var approval = await _acc.GetApprovalDocumentAsync("asset_purchase_orders", uid);
        if (approval != null)
            approval.ReturnUrl = returnUrl;

        var cancel = await _acc.GetApprovalDocumentAsync("asset_purchase_orders_cancel", uid);
        if (cancel != null)
        {
            cancel.ReturnUrl = returnUrl;
            cancel.Title = "Persetujuan Pembatalan";
            // Panel pembatalan hanya tampil bila pernah diajukan (preview alur ada di modal Ajukan Pembatalan).
            cancel.RequiresApproval = cancel.RequiresApproval && cancel.Requests.Count > 0;
        }

        return View(new AssetPurchaseOrderDetailsPageViewModel
        {
            Order = po,
            Approval = approval,
            CancelApproval = cancel,
            CanCreateInvoice = po.UninvoicedAmount > 0 && await _access.CanAsync("purchase-invoices", "create")
        });
    }

    [HttpGet]
    public async Task<IActionResult> Create() =>
        await FormView(new AssetPurchaseOrderFormViewModel { Items = new() { new AssetPurchaseItemViewModel() } });

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AssetPurchaseOrderFormViewModel form)
    {
        form.Uid = null;
        return await Submit(form);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid uid)
    {
        var po = await Api.GetOrderAsync(uid);
        if (po == null)
            return NotFoundRedirect("PO Aset tidak ditemukan.");

        if (po.Status != "draft")
        {
            TempData["Error"] = "PO Aset yang sudah diproses tidak dapat diedit.";
            return RedirectToAction(nameof(Details), new { uid });
        }

        return await FormView(new AssetPurchaseOrderFormViewModel
        {
            Uid = po.Uid,
            ApoNo = po.ApoNo,
            ApoDate = po.ApoDate ?? DateOnly.FromDateTime(DateTime.Today),
            SupplierId = po.SupplierId,
            Note = po.Note,
            DiscountAmount = po.DiscountAmount,
            TaxPercent = po.TaxPercent,
            Items = po.Items
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(AssetPurchaseOrderFormViewModel form) =>
        form.Uid == null ? NotFoundRedirect("PO Aset tidak ditemukan.") : await Submit(form);

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid uid)
    {
        var result = await Api.DeleteOrderAsync(uid);
        TempData[result.Success ? "Success" : "Error"] = result.Message;
        return result.Success ? RedirectToAction(nameof(Index)) : RedirectToAction(nameof(Details), new { uid });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(Guid uid, string? reason) =>
        Back(await Api.SubmitOrderAsync(uid, reason), nameof(Details), uid);

    /// <summary>Batalkan PO draft (belum disetujui).</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(Guid uid, string? reason) =>
        Back(await Api.CancelOrderAsync(uid, reason), nameof(Details), uid);

    /// <summary>Ajukan pembatalan PO ordered (alasan ≥ 2 kalimat).</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RequestCancel(Guid uid, string? cancelReason) =>
        Back(await Api.RequestCancelOrderAsync(uid, cancelReason), nameof(Details), uid);

    /// <summary>Terima aset: qty[uid baris] = unit diterima.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Receive(Guid uid, DateOnly? receiptDate, string? location, string? note, Dictionary<Guid, int>? qty) =>
        Back(await Api.ReceiveOrderAsync(uid, receiptDate, location, note, (qty ?? new()).Where(x => x.Value != 0).Select(x => (x.Key, x.Value))),
            nameof(Details), uid);

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelReceipt(Guid uid, Guid receiptUid, DateOnly? date, string? reason) =>
        Back(await Api.CancelReceiptAsync(receiptUid, date, reason), nameof(Details), uid);

    private async Task<IActionResult> FormView(AssetPurchaseOrderFormViewModel form)
    {
        form.Options = await Api.GetOrderFormOptionsAsync();
        if (form.Items.Count == 0)
            form.Items.Add(new AssetPurchaseItemViewModel());
        return View("Form", form);
    }

    private async Task<IActionResult> Submit(AssetPurchaseOrderFormViewModel form)
    {
        form.Items = CleanItems(form.Items);
        var result = await Api.SaveOrderAsync(form);

        if (!result.Success)
        {
            TempData["Error"] = result.Message;
            return await FormView(form);
        }

        TempData["Success"] = result.Message;
        return RedirectToAction(nameof(Details), new { uid = result.Uid ?? form.Uid });
    }
}

/// <summary>Pembelian &gt; Retur Aset.</summary>
public class AssetPurchaseReturnsController : AssetPurchaseWebControllerBase
{
    public AssetPurchaseReturnsController(AssetPurchaseApiClient api) : base(api)
    {
    }

    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] AssetPurchaseFilterViewModel filter)
    {
        Normalize(filter);
        return View(new AssetPurchaseIndexViewModel<AssetPurchaseReturnViewModel> { Filter = filter, Rows = await Api.GetReturnsAsync(filter) });
    }

    [HttpGet]
    public async Task<IActionResult> ExportExcel([FromQuery] AssetPurchaseFilterViewModel filter) => this.ExcelFile(await BuildExportAsync(filter));

    [HttpGet]
    public async Task<IActionResult> ExportPdf([FromQuery] AssetPurchaseFilterViewModel filter) => await this.PdfFile(await BuildExportAsync(filter));

    [HttpGet]
    public async Task<IActionResult> PrintList([FromQuery] AssetPurchaseFilterViewModel filter) => this.PrintView(await BuildExportAsync(filter));

    private async Task<ListExport> BuildExportAsync(AssetPurchaseFilterViewModel filter)
    {
        var rows = await AllRowsAsync(filter, Api.GetReturnsAsync);

        return new ListExport
        {
            Title = Tr["Retur Aset"],
            Subtitle = FilterSubtitle(filter, AssetPurchaseLabels.ReturnStatuses),
            FileName = "retur_aset",
            Headers = Headers("No. Retur", "Tanggal", "Supplier", "PO Aset", "Alasan", "Aset", "Nilai", "Status"),
            Rows = rows.Select(r => new object?[]
            {
                r.ReturnNo, r.ReturnDate, r.SupplierName, r.Order?.No, r.Reason, r.ItemCount, r.TotalAmount,
                Tr[AssetPurchaseLabels.Label(AssetPurchaseLabels.ReturnStatuses, r.Status)]
            }).ToList(),
            Footer = new object?[] { Tr["Total"], null, null, null, null, rows.Sum(r => r.ItemCount), rows.Sum(r => r.TotalAmount), null }
        };
    }

    [HttpGet]
    public async Task<IActionResult> Details(Guid uid)
    {
        var ret = await Api.GetReturnAsync(uid);
        return ret == null ? NotFoundRedirect("Retur aset tidak ditemukan.") : View(ret);
    }

    [HttpGet]
    public async Task<IActionResult> Create() => await FormView(new AssetReturnFormViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AssetReturnFormViewModel form)
    {
        var result = await Api.CreateReturnAsync(form);

        if (!result.Success)
        {
            TempData["Error"] = result.Message;
            return await FormView(form);
        }

        TempData["Success"] = result.Message;
        return RedirectToAction(nameof(Details), new { uid = result.Uid });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(Guid uid, DateOnly? date, string? reason) =>
        Back(await Api.CancelReturnAsync(uid, date, reason), nameof(Details), uid);

    private async Task<IActionResult> FormView(AssetReturnFormViewModel form)
    {
        form.Assets = await Api.GetReturnableAssetsAsync();
        return View("Create", form);
    }
}
