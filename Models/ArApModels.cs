namespace ERP.Web.Models;

/// <summary>Konfigurasi halaman hutang (supplier) / piutang (customer) yang memakai view bersama.</summary>
public record ArApPageConfig(
    string Controller,
    string Title,
    string ApiBase,
    string PartyLabel,
    string DocLabel,
    string NumberLabel,
    string AccountTypes,
    bool IsPayable,
    bool HasReceiptSource = false,
    string? InvoiceController = null,
    string? ApprovalDocType = null, // jenis dokumen di mesin persetujuan (null = tanpa persetujuan)
    string? BudgetApprovalDocType = null); // Level 11: faktur melebihi anggaran

public static class ArApPages
{
    public static readonly ArApPageConfig PurchaseInvoices = new("PurchaseInvoices", "Faktur Pembelian", "api/purchase-invoices",
        "Supplier", "Faktur", "No. Faktur Supplier", "expense,asset", IsPayable: true, HasReceiptSource: true,
        ApprovalDocType: "purchase_invoices", BudgetApprovalDocType: "purchase_invoice_budget");

    public static readonly ArApPageConfig SalesInvoices = new("SalesInvoices", "Faktur Penjualan", "api/sales-invoices",
        "Customer", "Faktur", "No. PO Customer", "revenue", IsPayable: false);

    public static readonly ArApPageConfig SupplierPayments = new("SupplierPayments", "Pembayaran Hutang", "api/supplier-payments",
        "Supplier", "Pembayaran", "", "", IsPayable: true, InvoiceController: "PurchaseInvoices",
        ApprovalDocType: "supplier_payments");

    public static readonly ArApPageConfig CustomerPayments = new("CustomerPayments", "Penerimaan Piutang", "api/customer-payments",
        "Customer", "Penerimaan", "", "", IsPayable: false, InvoiceController: "SalesInvoices");

    public static (string Label, string Css) StatusBadge(string status, string? payStatus = null) => status switch
    {
        "draft" => ("Draft", "bg-secondary"),
        "void" => ("Dibatalkan", "bg-danger"),
        _ => payStatus switch
        {
            "paid" => ("Lunas", "bg-success"),
            "written_off" => ("Lunas (write-off)", "bg-secondary"),
            "partial" => ("Sebagian", "bg-warning text-dark"),
            "unpaid" => ("Belum dibayar", "bg-info text-dark"),
            _ => ("Posted", "bg-success")
        }
    };
}

public class PartyOptionViewModel
{
    public long Id { get; set; }
    public Guid? Uid { get; set; }
    public string Name { get; set; } = "";
    public string? Code { get; set; }
    public string? Npwp { get; set; }
}

public class InvoicePaymentViewModel
{
    public Guid PaymentUid { get; set; }
    public string PaymentNo { get; set; } = "";
    public DateOnly PaymentDate { get; set; }
    public string Status { get; set; } = "";
    public decimal Amount { get; set; }

    /// <summary>Pengurang hutang/piutang (IDR, kurs faktur), selisih kurs terealisasi, kurs bayar.</summary>
    public decimal FunctionalAmount { get; set; }
    public decimal FxGainLoss { get; set; }
    public decimal PaymentFxRate { get; set; } = 1;
}

public class InvoiceViewModel
{
    public Guid Uid { get; set; }
    public string InvoiceNo { get; set; } = "";
    public long? PartyId { get; set; }
    public Guid? PartyUid { get; set; }
    public string? PartyName { get; set; }
    public string? PartyRef { get; set; }
    public DateOnly InvoiceDate { get; set; }
    public DateOnly? DueDate { get; set; }
    public long AccountId { get; set; }
    public string? AccountCode { get; set; }
    public string? AccountName { get; set; }
    public string? Description { get; set; }
    public decimal Amount { get; set; }
    public decimal TaxPercent { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal Total { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal Outstanding { get; set; }

    /// <summary>Tarif PPN, DPP Nilai Lain, status kreditabel, NSFP Coretax.</summary>
    public long? TaxRateId { get; set; }
    public string? TaxRateName { get; set; }
    public decimal TaxBase { get; set; }
    public bool TaxCreditable { get; set; } = true;
    public string? TaxInvoiceNo { get; set; }

    /// <summary>PPh 23 dipotong: Total = DPP + PPN − PPh 23 (hutang ke vendor).</summary>
    public long? Pph23RateId { get; set; }
    public string? Pph23RateName { get; set; }
    public bool Pph23HasNpwp { get; set; } = true;
    public decimal Pph23Base { get; set; }
    public decimal Pph23Percent { get; set; }
    public decimal Pph23Amount { get; set; }
    public decimal FunctionalPph23 { get; set; }
    public string? Pph23CertNo { get; set; }
    public DateOnly? Pph23CertDate { get; set; }
    public string? Pph23DepositNo { get; set; }
    public Guid? Pph23DepositUid { get; set; }

    /// <summary>Dimensi (Level 11) baris beban/pendapatan.</summary>
    public long? CostCenterId { get; set; }
    public string? CostCenterCode { get; set; }
    public string? CostCenterName { get; set; }
    public long? ProjectId { get; set; }
    public Guid? ProjectUid { get; set; }
    public string? ProjectCode { get; set; }
    public string? ProjectName { get; set; }

    /// <summary>Status pengajuan persetujuan terakhir (pending | approved | rejected | cancelled); null = tidak pernah diajukan.</summary>
    public string? ApprovalStatus { get; set; }

    /// <summary>Tagihan sebelum potongan PPh 23 (DPP + PPN).</summary>
    public decimal GrossTotal => Amount + TaxAmount;

    /// <summary>Mata uang dokumen (nominal di atas) dan nilai IDR pada kurs faktur.</summary>
    public string CurrencyCode { get; set; } = Acc.FunctionalCurrency;
    public decimal FxRate { get; set; } = 1;
    public decimal FunctionalAmount { get; set; }
    public decimal FunctionalTax { get; set; }
    public decimal FunctionalTotal { get; set; }
    public string Status { get; set; } = "";
    public string PayStatus { get; set; } = "";
    public string? VoidReason { get; set; }
    public Guid? JournalUid { get; set; }
    public string? JournalNo { get; set; }
    public string? SourceLabel { get; set; }
    public Guid? ReceiptUid { get; set; }

    /// <summary>Faktur dari PO Aset.</summary>
    public Guid? ApoUid { get; set; }
    public string? CreatedByName { get; set; }
    public DateTime? CreatedAt { get; set; }
    public List<InvoicePaymentViewModel> Payments { get; set; } = new();

    /// <summary>Level 13: prepaid / diterima di muka (bulan mulai, jumlah bulan) dan jadwalnya.</summary>
    public DateOnly? DeferralStart { get; set; }
    public int? DeferralMonths { get; set; }
    public DeferralScheduleViewModel? DeferralSchedule { get; set; }

    /// <summary>Level 13: piutang yang di-write-off dan rinciannya.</summary>
    public decimal WrittenOffAmount { get; set; }
    public List<WriteOffViewModel> WriteOffs { get; set; } = new();

    public bool IsDeferred => DeferralMonths is > 0;

    public bool IsForeign => Acc.IsForeign(CurrencyCode);

    public bool IsOverdue => Status == "posted" && Outstanding > 0 && DueDate != null && DueDate < DateOnly.FromDateTime(DateTime.Today);
}

public class ArApFilterViewModel
{
    public string? Q { get; set; }
    public Guid? PartyUid { get; set; }
    public string? Status { get; set; }
    public string? PayStatus { get; set; }
    public DateOnly? DateFrom { get; set; }
    public DateOnly? DateTo { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;

    public string ToQueryString()
    {
        var parts = new List<string>();
        void Add(string key, string? value)
        {
            if (!string.IsNullOrWhiteSpace(value))
                parts.Add($"{key}={Uri.EscapeDataString(value)}");
        }

        Add("q", Q);
        Add("partyUid", PartyUid?.ToString());
        Add("status", Status);
        Add("payStatus", PayStatus);
        Add("dateFrom", DateFrom?.ToString("yyyy-MM-dd"));
        Add("dateTo", DateTo?.ToString("yyyy-MM-dd"));

        return string.Join("&", parts);
    }
}

public class InvoicesIndexViewModel
{
    public ArApPageConfig Config { get; set; } = ArApPages.PurchaseInvoices;
    public ArApFilterViewModel Filter { get; set; } = new();
    public PagedResult<InvoiceViewModel> Invoices { get; set; } = new();
    public List<PartyOptionViewModel> Parties { get; set; } = new();
}

public class ReceiptOptionViewModel
{
    public Guid Uid { get; set; }
    public string ReceiptNo { get; set; } = "";
    public DateOnly ReceiptDate { get; set; }
    public long? SupplierId { get; set; }
    public string? SupplierName { get; set; }
    public string? RefNo { get; set; }
    public decimal Amount { get; set; }
}

public class InvoiceFormViewModel
{
    public ArApPageConfig Config { get; set; } = ArApPages.PurchaseInvoices;
    public Guid? Uid { get; set; }
    public string? InvoiceNo { get; set; }
    public long? PartyId { get; set; }
    public string? PartyRef { get; set; }
    public DateOnly InvoiceDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public DateOnly? DueDate { get; set; }
    public long AccountId { get; set; }
    public string? Description { get; set; }
    public decimal Amount { get; set; }

    /// <summary>Tarif PPN (kosong = tanpa PPN). <see cref="TaxPercent"/> hanya default dari dokumen sumber (PO Aset).</summary>
    public long? TaxRateId { get; set; }
    public decimal TaxPercent { get; set; }

    /// <summary>Nomor Faktur Pajak (NSFP) Coretax — opsional.</summary>
    public string? TaxInvoiceNo { get; set; }

    /// <summary>PPh 23 dipotong (faktur pembelian): tarif, status NPWP supplier, DPP (kosong = DPP faktur).</summary>
    public long? Pph23RateId { get; set; }
    public bool Pph23HasNpwp { get; set; } = true;
    public decimal? Pph23Base { get; set; }

    /// <summary>Cost center / project (Level 11) — hanya untuk akun beban/pendapatan.</summary>
    public long? CostCenterId { get; set; }
    public long? ProjectId { get; set; }

    /// <summary>Mata uang dokumen (IDR default) dan kurs (kosong = kurs tabel pada tanggal faktur).</summary>
    public string CurrencyCode { get; set; } = Acc.FunctionalCurrency;
    public decimal? FxRate { get; set; }

    /// <summary>Faktur dari Penerimaan Barang (hanya pembelian).</summary>
    public Guid? ReceiptUid { get; set; }

    /// <summary>Faktur dari PO Aset (semua penerimaan yang belum difakturkan; hanya pembelian).</summary>
    public Guid? ApoUid { get; set; }
    public string? SourceLabel { get; set; }

    /// <summary>Level 13: prepaid (pembelian, akun beban) / diterima di muka (penjualan) — bulan mulai "yyyy-MM" &amp; jumlah bulan.</summary>
    public bool IsDeferred { get; set; }
    public string? DeferralStartMonth { get; set; }
    public int? DeferralMonths { get; set; }

    public List<PartyOptionViewModel> Parties { get; set; } = new();
    public List<AccountOptionViewModel> Accounts { get; set; } = new();
    public List<ReceiptOptionViewModel> Receipts { get; set; } = new();
    public List<ApoInvoiceOptionViewModel> AssetOrders { get; set; } = new();
    public List<CurrencyViewModel> Currencies { get; set; } = new();
    public List<TaxRateViewModel> TaxRates { get; set; } = new();
    public List<Pph23RateViewModel> Pph23Rates { get; set; } = new();
    public DimensionOptions Dimensions { get; set; } = new();
}

public class AllocationViewModel
{
    public long InvoiceId { get; set; }
    public Guid? InvoiceUid { get; set; }
    public string? InvoiceNo { get; set; }
    public DateOnly? InvoiceDate { get; set; }
    public decimal InvoiceTotal { get; set; }
    public decimal InvoiceFxRate { get; set; } = 1;
    public decimal Amount { get; set; }
    public decimal FunctionalAmount { get; set; }
    public decimal FxGainLoss { get; set; }
}

public class PaymentViewModel
{
    public Guid Uid { get; set; }
    public string PaymentNo { get; set; } = "";
    public long? PartyId { get; set; }
    public string? PartyName { get; set; }
    public DateOnly PaymentDate { get; set; }
    public long CashAccountId { get; set; }
    public string? CashAccountName { get; set; }
    public string CashCurrencyCode { get; set; } = Acc.FunctionalCurrency;

    /// <summary>Nominal dalam mata uang faktur; kas IDR yang bergerak; selisih kurs terealisasi (+ laba).</summary>
    public decimal Amount { get; set; }
    public string CurrencyCode { get; set; } = Acc.FunctionalCurrency;
    public decimal FxRate { get; set; } = 1;
    public decimal FunctionalAmount { get; set; }
    public decimal FxGainLoss { get; set; }

    /// <summary>Status pengajuan persetujuan terakhir; null = tidak pernah diajukan.</summary>
    public string? ApprovalStatus { get; set; }

    /// <summary>Rekonsiliasi bank (Level 12): pending | confirmed; null = rekening tanpa rekonsiliasi.</summary>
    public string? BankStatus { get; set; }
    public DateOnly? BankConfirmedDate { get; set; }
    public string? Reference { get; set; }
    public string? Description { get; set; }
    public string Status { get; set; } = "";
    public string? VoidReason { get; set; }
    public Guid? JournalUid { get; set; }
    public string? JournalNo { get; set; }
    public string? CreatedByName { get; set; }
    public DateTime? CreatedAt { get; set; }
    public List<AllocationViewModel> Allocations { get; set; } = new();

    public bool IsForeign => Acc.IsForeign(CurrencyCode);
}

public class PaymentsIndexViewModel
{
    public ArApPageConfig Config { get; set; } = ArApPages.SupplierPayments;
    public ArApFilterViewModel Filter { get; set; } = new();
    public PagedResult<PaymentViewModel> Payments { get; set; } = new();
    public List<PartyOptionViewModel> Parties { get; set; } = new();
}

public class OpenInvoiceViewModel
{
    public long Id { get; set; }
    public Guid Uid { get; set; }
    public string InvoiceNo { get; set; } = "";
    public string? PartyRef { get; set; }
    public DateOnly InvoiceDate { get; set; }
    public DateOnly? DueDate { get; set; }
    public decimal Total { get; set; }
    public decimal Outstanding { get; set; }
    public string CurrencyCode { get; set; } = Acc.FunctionalCurrency;
    public decimal FxRate { get; set; } = 1;
    public decimal FunctionalOutstanding { get; set; }
}

public class PaymentAllocationInput
{
    public long InvoiceId { get; set; }
    public decimal Amount { get; set; }
}

public class PaymentFormViewModel
{
    public ArApPageConfig Config { get; set; } = ArApPages.SupplierPayments;
    public Guid? Uid { get; set; }
    public string? PaymentNo { get; set; }
    public long? PartyId { get; set; }
    public DateOnly PaymentDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public long CashAccountId { get; set; }

    /// <summary>Kurs bayar (mata uang mengikuti faktur); kosong = kurs tabel.</summary>
    public decimal? FxRate { get; set; }
    public string? Reference { get; set; }
    public string? Description { get; set; }
    public List<PaymentAllocationInput> Allocations { get; set; } = new();

    public List<PartyOptionViewModel> Parties { get; set; } = new();
    public List<CashAccountViewModel> CashAccounts { get; set; } = new();
    public List<OpenInvoiceViewModel> OpenInvoices { get; set; } = new();
}

// ===================== Umur, kartu, PPN =====================

public class AgingInvoiceViewModel
{
    public Guid Uid { get; set; }
    public string InvoiceNo { get; set; } = "";
    public DateOnly InvoiceDate { get; set; }
    public DateOnly DueDate { get; set; }
    public int DaysOverdue { get; set; }

    /// <summary>Sisa IDR (nilai buku per tanggal).</summary>
    public decimal Outstanding { get; set; }
    public string CurrencyCode { get; set; } = Acc.FunctionalCurrency;
    public decimal ForeignOutstanding { get; set; }
}

public class AgingRowViewModel
{
    public long PartyId { get; set; }
    public Guid? PartyUid { get; set; }
    public string PartyName { get; set; } = "";
    public decimal Current { get; set; }
    public decimal Days1To30 { get; set; }
    public decimal Days31To60 { get; set; }
    public decimal Days61To90 { get; set; }
    public decimal Over90 { get; set; }
    public decimal Total { get; set; }
    public List<AgingInvoiceViewModel> Invoices { get; set; } = new();
}

public class ControlCheckViewModel
{
    public string AccountCode { get; set; } = "";
    public string AccountName { get; set; } = "";
    public decimal SubledgerTotal { get; set; }
    public decimal LedgerBalance { get; set; }
    public decimal Difference { get; set; }
    public bool IsReconciled { get; set; }
}

public class AgingViewModel
{
    public DateOnly AsOf { get; set; }
    public List<AgingRowViewModel> Rows { get; set; } = new();
    public AgingRowViewModel Totals { get; set; } = new();
    public ControlCheckViewModel Control { get; set; } = new();

    // diisi controller Web
    public string Title { get; set; } = "";
    public string PartyLabel { get; set; } = "";
    public string InvoiceController { get; set; } = "";
    public string CardController { get; set; } = "";
}

public class PartyCardLineViewModel
{
    public DateOnly Date { get; set; }
    public string DocType { get; set; } = "";
    public Guid DocUid { get; set; }
    public string DocNo { get; set; } = "";
    public string? Description { get; set; }
    public decimal Increase { get; set; }
    public decimal Decrease { get; set; }
    public decimal Balance { get; set; }
    public string? CurrencyCode { get; set; }
    public decimal? ForeignAmount { get; set; }
}

public class PartyCardViewModel
{
    public PartyOptionViewModel? Party { get; set; }
    public DateOnly From { get; set; }
    public DateOnly To { get; set; }
    public decimal Opening { get; set; }
    public decimal TotalIncrease { get; set; }
    public decimal TotalDecrease { get; set; }
    public decimal Closing { get; set; }
    public List<PartyCardLineViewModel> Lines { get; set; } = new();
    public List<PartyOptionViewModel> Parties { get; set; } = new();

    // diisi controller Web
    public string Title { get; set; } = "";
    public string PartyLabel { get; set; } = "";
    public string InvoiceController { get; set; } = "";
    public string PaymentController { get; set; } = "";
    public bool IsPayable { get; set; }
}

public class VatMonthViewModel
{
    public string Period { get; set; } = "";
    public DateOnly Start { get; set; }
    public DateOnly End { get; set; }
    public decimal InputMovement { get; set; }
    public decimal OutputMovement { get; set; }
    public decimal InputBalance { get; set; }
    public decimal OutputBalance { get; set; }
    public decimal Net { get; set; }
    public string ResultType { get; set; } = "";
    public bool IsClosed { get; set; }
    public Guid? ClosingUid { get; set; }
    public Guid? JournalUid { get; set; }
    public string? JournalNo { get; set; }
    public bool CanClose { get; set; }
    public string? BlockReason { get; set; }

    public string ResultLabel => ResultType switch
    {
        "kurang_bayar" => "Kurang bayar",
        "lebih_bayar" => "Lebih bayar",
        _ => "Nihil"
    };
}

public class VatRecapViewModel
{
    public int Year { get; set; }
    public string? InputAccount { get; set; }
    public string? OutputAccount { get; set; }
    public string? PayableAccount { get; set; }
    public List<VatMonthViewModel> Months { get; set; } = new();
}

// ===================== Tarif pajak & rincian faktur pajak =====================

public class TaxRateViewModel
{
    public Guid Uid { get; set; }
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public decimal NominalRate { get; set; }
    public int DppNumerator { get; set; } = 1;
    public int DppDenominator { get; set; } = 1;
    public decimal EffectiveRate { get; set; }
    public bool IsInputCreditable { get; set; } = true;
    public DateOnly ValidFrom { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public DateOnly? ValidTo { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Note { get; set; }
    public bool IsUsed { get; set; }
    public int UsageCount { get; set; }

    /// <summary>"12% × 11/12" atau "12%".</summary>
    public string Formula => DppNumerator == DppDenominator
        ? $"{NominalRate:0.##}%"
        : $"{NominalRate:0.##}% × {DppNumerator}/{DppDenominator}";

    public string ValidityLabel => ValidTo == null ? $"{Acc.Date(ValidFrom)} –" : $"{Acc.Date(ValidFrom)} – {Acc.Date(ValidTo)}";
}

public class VatInvoiceRowViewModel
{
    public Guid Uid { get; set; }
    public string InvoiceNo { get; set; } = "";
    public DateOnly InvoiceDate { get; set; }
    public string? PartyName { get; set; }
    public string? Npwp { get; set; }
    public string? PartyRef { get; set; }
    public string? TaxInvoiceNo { get; set; }
    public string? TaxRateName { get; set; }
    public string CurrencyCode { get; set; } = Acc.FunctionalCurrency;
    public decimal Dpp { get; set; }
    public decimal TaxBase { get; set; }
    public decimal Tax { get; set; }
    public bool Creditable { get; set; } = true;
    public string Status { get; set; } = "";
}

public class VatInvoicesViewModel
{
    public string Period { get; set; } = "";
    public bool IsClosed { get; set; }
    public List<VatInvoiceRowViewModel> Output { get; set; } = new();
    public List<VatInvoiceRowViewModel> Input { get; set; } = new();
    public decimal OutputTax { get; set; }
    public decimal InputCreditableTax { get; set; }
    public decimal InputNonCreditableTax { get; set; }
    public int MissingTaxInvoiceNo { get; set; }
}

// ===================== PPh 23 =====================

public class Pph23RateViewModel
{
    public Guid Uid { get; set; }
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public string ObjectType { get; set; } = "jasa";
    public decimal Rate { get; set; }
    public decimal NoNpwpRate { get; set; }
    public DateOnly ValidFrom { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public DateOnly? ValidTo { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Note { get; set; }
    public bool IsUsed { get; set; }
    public int UsageCount { get; set; }

    public static readonly (string Value, string Label)[] ObjectTypes =
    {
        ("jasa", "Jasa"), ("sewa", "Sewa harta"), ("dividen_dll", "Dividen / bunga / royalti / hadiah")
    };

    public string ObjectTypeLabel => ObjectTypes.FirstOrDefault(t => t.Value == ObjectType).Label ?? ObjectType;
    public string ValidityLabel => ValidTo == null ? $"{Acc.Date(ValidFrom)} –" : $"{Acc.Date(ValidFrom)} – {Acc.Date(ValidTo)}";
}

public class Pph23SlipViewModel
{
    public Guid InvoiceUid { get; set; }
    public string InvoiceNo { get; set; } = "";
    public DateOnly InvoiceDate { get; set; }
    public string? SupplierName { get; set; }
    public string? Npwp { get; set; }
    public bool HasNpwp { get; set; }
    public string? RateName { get; set; }
    public string? ObjectType { get; set; }
    public decimal Percent { get; set; }
    public decimal Base { get; set; }
    public decimal Amount { get; set; }
    public string? CertNo { get; set; }
    public DateOnly? CertDate { get; set; }
    public Guid? DepositUid { get; set; }
    public string? DepositNo { get; set; }
    public DateOnly? DepositDate { get; set; }
}

public class Pph23MonthViewModel
{
    public string Period { get; set; } = "";
    public DateOnly DueDate { get; set; }
    public int SlipCount { get; set; }
    public decimal Withheld { get; set; }
    public decimal Deposited { get; set; }
    public decimal Outstanding { get; set; }
    public int MissingCertificates { get; set; }
    public bool IsOverdue { get; set; }
}

public class Pph23YearViewModel
{
    public int Year { get; set; }
    public List<Pph23MonthViewModel> Months { get; set; } = new();
    public decimal LedgerBalance { get; set; }
    public decimal OutstandingTotal { get; set; }
}

public class Pph23DepositViewModel
{
    public Guid Uid { get; set; }
    public string DepositNo { get; set; } = "";
    public string Period { get; set; } = "";
    public DateOnly DepositDate { get; set; }
    public string? CashAccountName { get; set; }
    public decimal Amount { get; set; }
    public int InvoiceCount { get; set; }
    public string? Ntpn { get; set; }
    public string Status { get; set; } = "";
    public string? Note { get; set; }
    public string? VoidReason { get; set; }
    public Guid? JournalUid { get; set; }
    public string? JournalNo { get; set; }
}

public class Pph23PeriodViewModel
{
    public Pph23MonthViewModel Summary { get; set; } = new();
    public List<Pph23SlipViewModel> Slips { get; set; } = new();
    public List<Pph23DepositViewModel> Deposits { get; set; } = new();

    // diisi controller Web
    public List<CashAccountViewModel> CashAccounts { get; set; } = new();
}
