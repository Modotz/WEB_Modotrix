namespace ERP.Web.Models;

// Pembelian aset: PR Aset, RFQ Aset, PO Aset (+ penerimaan), Retur Aset.

public static class AssetPurchaseLabels
{
    public static readonly (string Value, string Label)[] RequestStatuses =
    {
        ("draft", "Draft"), ("submitted", "Menunggu persetujuan"), ("approved", "Disetujui"), ("closed", "Selesai (jadi PO)")
    };

    public static readonly (string Value, string Label)[] RfqStatuses =
    {
        ("draft", "Draft"), ("sent", "Dikirim"), ("quoted", "Ada penawaran"), ("converted", "Jadi PO"), ("cancelled", "Dibatalkan")
    };

    public static readonly (string Value, string Label)[] OrderStatuses =
    {
        ("draft", "Draft"), ("pending_approval", "Menunggu persetujuan"), ("ordered", "Disetujui (ordered)"),
        ("partial", "Diterima sebagian"), ("received", "Diterima"), ("cancelled", "Dibatalkan")
    };

    public static readonly (string Value, string Label)[] ReturnStatuses =
    {
        ("posted", "Diposting"), ("cancelled", "Dibatalkan")
    };

    public static string Label((string Value, string Label)[] list, string? value) =>
        list.FirstOrDefault(x => x.Value == value).Label ?? value ?? "-";

    public static string Css(string? status) => status switch
    {
        "draft" => "bg-secondary",
        "submitted" or "pending_approval" or "sent" => "bg-warning text-dark",
        "approved" or "ordered" or "quoted" => "bg-primary",
        "partial" => "bg-info text-dark",
        "received" or "closed" or "converted" or "posted" => "bg-success",
        "cancelled" or "void" => "bg-danger",
        _ => "bg-secondary"
    };

    /// <summary>Label status dokumen tertaut (PR/RFQ/PO/retur/faktur) apa pun jenisnya.</summary>
    public static string AnyStatus(string? status) => status switch
    {
        null => "-",
        "void" => "Dibatalkan",
        _ => Label(OrderStatuses.Concat(RequestStatuses).Concat(RfqStatuses).Concat(ReturnStatuses).DistinctBy(x => x.Value).ToArray(), status)
    };
}

public class AssetPurchaseFilterViewModel
{
    public string? Q { get; set; }
    public string? Status { get; set; }
    public Guid? SupplierUid { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;

    public string ToQueryString()
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(Q)) parts.Add("q=" + Uri.EscapeDataString(Q));
        if (!string.IsNullOrWhiteSpace(Status)) parts.Add("status=" + Uri.EscapeDataString(Status));
        if (SupplierUid != null) parts.Add("supplierUid=" + SupplierUid);
        return string.Join("&", parts);
    }
}

public class AssetPurchaseOptionViewModel
{
    public long Id { get; set; }
    public Guid? Uid { get; set; }
    public string Name { get; set; } = "";
    public string? Code { get; set; }
}

public class AssetPurchaseFormOptionsViewModel
{
    public List<AssetPurchaseOptionViewModel> Categories { get; set; } = new();
    public List<AssetPurchaseOptionViewModel> Suppliers { get; set; } = new();
}

public class AssetDocLinkViewModel
{
    public Guid Uid { get; set; }
    public string No { get; set; } = "";
    public DateOnly? Date { get; set; }
    public string? Status { get; set; }
    public decimal Amount { get; set; }
}

/// <summary>Baris aset (form &amp; tampilan). Price = estimasi (PR/RFQ) atau harga satuan (PO).</summary>
public class AssetPurchaseItemViewModel
{
    public Guid Uid { get; set; }
    public long? AssetCategoryId { get; set; }
    public string? CategoryName { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public int Quantity { get; set; } = 1;
    public int ReceivedQty { get; set; }
    public decimal Price { get; set; }
    public decimal QuotedPrice { get; set; }
    public decimal DiscPercent { get; set; }
    public decimal Subtotal { get; set; }
    public decimal UnitCost { get; set; }
    public int Remaining => Math.Max(Quantity - ReceivedQty, 0);
}

/// <summary>Editor baris bersama (form PR/RFQ/PO).</summary>
public class AssetItemsEditorModel
{
    public List<AssetPurchaseItemViewModel> Items { get; set; } = new();
    public List<AssetPurchaseOptionViewModel> Categories { get; set; } = new();
    public bool ShowDiscount { get; set; }
    public bool CategoryRequired { get; set; }
    public string PriceLabel { get; set; } = "Est. Harga";
}

public class AssetPurchaseRequestViewModel
{
    public Guid Uid { get; set; }
    public string AprNo { get; set; } = "";
    public DateOnly? AprDate { get; set; }
    public DateOnly? NeededDate { get; set; }
    public string? Department { get; set; }
    public string? Requester { get; set; }
    public string? Note { get; set; }
    public string Status { get; set; } = "";
    public decimal TotalAmount { get; set; }
    public string? CreatedByName { get; set; }
    public DateTime? CreatedAt { get; set; }
    public int ItemCount { get; set; }
    public List<AssetPurchaseItemViewModel> Items { get; set; } = new();
    public List<AssetDocLinkViewModel> Rfqs { get; set; } = new();
    public List<AssetDocLinkViewModel> Orders { get; set; } = new();
}

public class AssetPurchaseRequestFormViewModel
{
    public Guid? Uid { get; set; }
    public string? AprNo { get; set; }
    public DateOnly AprDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public DateOnly? NeededDate { get; set; }
    public string? Department { get; set; }
    public string? Requester { get; set; }
    public string? Note { get; set; }
    public List<AssetPurchaseItemViewModel> Items { get; set; } = new();
    public AssetPurchaseFormOptionsViewModel Options { get; set; } = new();
}

public class AssetRfqViewModel
{
    public Guid Uid { get; set; }
    public string RfqNo { get; set; } = "";
    public DateOnly RfqDate { get; set; }
    public DateOnly? ValidUntil { get; set; }
    public long? SupplierId { get; set; }
    public string? SupplierName { get; set; }
    public AssetDocLinkViewModel? Request { get; set; }
    public string? Note { get; set; }
    public string Status { get; set; } = "";
    public decimal SubtotalAmount { get; set; }
    public string? CreatedByName { get; set; }
    public DateTime? CreatedAt { get; set; }
    public List<AssetPurchaseItemViewModel> Items { get; set; } = new();
    public List<AssetDocLinkViewModel> Orders { get; set; } = new();
}

public class AssetRfqFormViewModel
{
    public Guid? Uid { get; set; }
    public string? RfqNo { get; set; }
    public DateOnly RfqDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public DateOnly? ValidUntil { get; set; }
    public long? SupplierId { get; set; }
    public Guid? PrUid { get; set; }
    public string? PrNo { get; set; }
    public string? Note { get; set; }
    public List<AssetPurchaseItemViewModel> Items { get; set; } = new();
    public AssetPurchaseFormOptionsViewModel Options { get; set; } = new();
}

public class AssetLinkViewModel
{
    public Guid Uid { get; set; }
    public string AssetNo { get; set; } = "";
    public string Name { get; set; } = "";
    public decimal AcquisitionCost { get; set; }
    public string Status { get; set; } = "";
    public string? DisposalType { get; set; }
}

public class AssetReceiptItemViewModel
{
    public string Name { get; set; } = "";
    public string? CategoryName { get; set; }
    public int Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal Subtotal { get; set; }
}

public class AssetReceiptViewModel
{
    public Guid Uid { get; set; }
    public string ReceiptNo { get; set; } = "";
    public DateOnly ReceiptDate { get; set; }
    public string? Location { get; set; }
    public string? Note { get; set; }
    public string Status { get; set; } = "";
    public decimal TotalAmount { get; set; }
    public Guid? JournalUid { get; set; }
    public string? JournalNo { get; set; }
    public Guid? InvoiceUid { get; set; }
    public string? InvoiceNo { get; set; }
    public string? CancelReason { get; set; }
    public DateOnly? CancelDate { get; set; }
    public string? CreatedByName { get; set; }
    public bool CanCancel { get; set; }
    public List<AssetReceiptItemViewModel> Items { get; set; } = new();
    public List<AssetLinkViewModel> Assets { get; set; } = new();
}

public class AssetPurchaseOrderViewModel
{
    public Guid Uid { get; set; }
    public string ApoNo { get; set; } = "";
    public DateOnly? ApoDate { get; set; }
    public long? SupplierId { get; set; }
    public string? SupplierName { get; set; }
    public AssetDocLinkViewModel? Request { get; set; }
    public AssetDocLinkViewModel? Rfq { get; set; }
    public string? Note { get; set; }
    public string Status { get; set; } = "";
    public decimal SubtotalAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxPercent { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public DateOnly? ReceivedDate { get; set; }
    public string? CancelReason { get; set; }
    public string? CreatedByName { get; set; }
    public DateTime? CreatedAt { get; set; }
    public int UnitsOrdered { get; set; }
    public int UnitsReceived { get; set; }
    public decimal UninvoicedAmount { get; set; }
    public bool CancelPending { get; set; }
    public bool HasReceipts { get; set; }
    public List<AssetPurchaseItemViewModel> Items { get; set; } = new();
    public List<AssetReceiptViewModel> Receipts { get; set; } = new();
    public List<AssetDocLinkViewModel> Invoices { get; set; } = new();
    public List<AssetDocLinkViewModel> Returns { get; set; } = new();

    public bool CanReceive => Status is "ordered" or "partial" && !CancelPending;
}

public class AssetPurchaseOrderFormViewModel
{
    public Guid? Uid { get; set; }
    public string? ApoNo { get; set; }
    public DateOnly ApoDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public long? SupplierId { get; set; }
    public string? Note { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxPercent { get; set; }
    public List<AssetPurchaseItemViewModel> Items { get; set; } = new();
    public AssetPurchaseFormOptionsViewModel Options { get; set; } = new();
}

public class AssetPurchaseOrderDetailsPageViewModel
{
    public AssetPurchaseOrderViewModel Order { get; set; } = new();
    public ApprovalDocumentViewModel? Approval { get; set; }
    public ApprovalDocumentViewModel? CancelApproval { get; set; }
    public bool CanCreateInvoice { get; set; }
}

public class AssetPurchaseRequestDetailsPageViewModel
{
    public AssetPurchaseRequestViewModel Request { get; set; } = new();
    public ApprovalDocumentViewModel? Approval { get; set; }
    public List<AssetPurchaseOptionViewModel> Suppliers { get; set; } = new();
    public bool CanCreateRfq { get; set; }
}

public class AssetReturnItemViewModel
{
    public Guid? AssetUid { get; set; }
    public string? AssetNo { get; set; }
    public string? AssetName { get; set; }
    public string? ReceiptNo { get; set; }
    public decimal Value { get; set; }
    public decimal Accumulated { get; set; }
    public Guid? JournalUid { get; set; }
    public string? JournalNo { get; set; }
}

public class AssetPurchaseReturnViewModel
{
    public Guid Uid { get; set; }
    public string ReturnNo { get; set; } = "";
    public DateOnly? ReturnDate { get; set; }
    public string? SupplierName { get; set; }
    public AssetDocLinkViewModel? Order { get; set; }
    public string? Reason { get; set; }
    public string? Note { get; set; }
    public string Status { get; set; } = "";
    public decimal TotalAmount { get; set; }
    public DateOnly? CancelDate { get; set; }
    public string? CancelReason { get; set; }
    public string? CreatedByName { get; set; }
    public DateTime? CreatedAt { get; set; }
    public int ItemCount { get; set; }
    public List<AssetReturnItemViewModel> Items { get; set; } = new();
}

public class ReturnableAssetViewModel
{
    public Guid Uid { get; set; }
    public string AssetNo { get; set; } = "";
    public string Name { get; set; } = "";
    public decimal AcquisitionCost { get; set; }
    public string? SupplierName { get; set; }
    public string? ApoNo { get; set; }
    public string? ReceiptNo { get; set; }
    public DateOnly? ReceiptDate { get; set; }
    public string? InvoiceNo { get; set; }
}

public class AssetReturnFormViewModel
{
    public DateOnly ReturnDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public List<Guid> AssetUids { get; set; } = new();
    public string? Reason { get; set; }
    public string? Note { get; set; }
    public List<ReturnableAssetViewModel> Assets { get; set; } = new();
}

public class AssetPurchaseIndexViewModel<T>
{
    public AssetPurchaseFilterViewModel Filter { get; set; } = new();
    public PagedResult<T> Rows { get; set; } = new();
    public List<AssetPurchaseOptionViewModel> Suppliers { get; set; } = new();
}

/// <summary>PO Aset dengan penerimaan belum difakturkan (pilihan sumber faktur pembelian).</summary>
public class ApoInvoiceOptionViewModel
{
    public Guid Uid { get; set; }
    public string ApoNo { get; set; } = "";
    public long? SupplierId { get; set; }
    public string? SupplierName { get; set; }
    public decimal Amount { get; set; }
    public decimal TaxPercent { get; set; }
    public string ReceiptNos { get; set; } = "";
}
