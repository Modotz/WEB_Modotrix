using System.Globalization;

namespace ERP.Web.Models;

/// <summary>Label &amp; format bersama untuk halaman akuntansi.</summary>
public static class Acc
{
    public static readonly CultureInfo Culture = new("id-ID");

    public static readonly (string Value, string Label)[] Types =
    {
        ("asset", "Aset"), ("liability", "Liabilitas"), ("equity", "Ekuitas"), ("revenue", "Pendapatan"), ("expense", "Beban")
    };

    public static readonly (string Value, string Label)[] CashFlowCategories =
    {
        ("operating", "Operasi"), ("investing", "Investasi"), ("financing", "Pendanaan")
    };

    public static string TypeLabel(string? type) =>
        Types.FirstOrDefault(t => t.Value == type).Label ?? type ?? "-";

    public static string CashFlowLabel(string? category) =>
        category == null ? "Operasi (default)" : CashFlowCategories.FirstOrDefault(t => t.Value == category).Label ?? category;

    /// <summary>Format angka akuntansi: 1.234.567,89 — negatif dalam kurung.</summary>
    public static string Money(decimal value) =>
        value < 0 ? $"({(-value).ToString("N2", Culture)})" : value.ToString("N2", Culture);

    /// <summary>Kosong bila 0 (kolom debit/kredit).</summary>
    public static string MoneyOrBlank(decimal value) => value == 0 ? "" : Money(value);

    public static string Date(DateOnly? date) => date?.ToString("dd/MM/yyyy") ?? "-";

    // ---------- Multi-currency (mata uang fungsional IDR) ----------

    public const string FunctionalCurrency = "IDR";

    public static bool IsForeign(string? currency) => !string.IsNullOrEmpty(currency) && currency != FunctionalCurrency;

    /// <summary>Nominal bermata uang: "USD 1.234,56" untuk valas, angka biasa untuk IDR.</summary>
    public static string Fx(decimal value, string? currency) =>
        IsForeign(currency) ? $"{currency} {Money(value)}" : Money(value);

    /// <summary>Kurs: 15.850 / 105,25 / 0,0625 (tanpa nol di belakang, maks. 6 desimal).</summary>
    public static string Rate(decimal value) => value.ToString("#,##0.######", Culture);
}

// ===================== Bagan Akun =====================

public class AccountViewModel
{
    public Guid Uid { get; set; }
    public long Id { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string Type { get; set; } = "asset";
    public string NormalBalance { get; set; } = "debit";
    public long? ParentId { get; set; }
    public string? ParentCode { get; set; }
    public string? ParentName { get; set; }
    public bool IsPostable { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public bool IsControl { get; set; }
    public string? CashFlowCategory { get; set; }
    public string? Description { get; set; }
    public int Level { get; set; }
    public bool IsUsed { get; set; }
    public bool HasChildren { get; set; }
}

public class AccountOptionViewModel
{
    public long Id { get; set; }
    public Guid Uid { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string Type { get; set; } = "";
    public string NormalBalance { get; set; } = "";
    public bool IsPostable { get; set; }
    public bool IsControl { get; set; }

    public string Label => $"{Code} — {Name}";
}

public class ChartOfAccountsIndexViewModel
{
    public List<AccountViewModel> Accounts { get; set; } = new();
    public string? Q { get; set; }
    public string? Type { get; set; }
    public bool? Active { get; set; }
}

// ===================== Periode =====================

public class AccountingPeriodViewModel
{
    public Guid Uid { get; set; }
    public string Name { get; set; } = "";
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public string Status { get; set; } = "open";
    public string? Note { get; set; }
    public int PostedJournals { get; set; }
    public int DraftJournals { get; set; }
    public PeriodLockEventViewModel? LastEvent { get; set; }

    /// <summary>Level 13: pekerjaan akhir bulan yang belum dijalankan (informasi, bukan gerbang).</summary>
    public PeriodTaskCountsViewModel? Tasks { get; set; }

    public bool IsClosed => Status == "closed";
}

public class PeriodLockEventViewModel
{
    public string Action { get; set; } = "";
    public string Note { get; set; } = "";
    public string? Username { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AccountingPeriodsIndexViewModel
{
    public List<AccountingPeriodViewModel> Periods { get; set; } = new();
    public int? Year { get; set; }
    public List<int> Years { get; set; } = new();
}

// ===================== Jurnal =====================

public class JournalListItemViewModel
{
    public Guid Uid { get; set; }
    public string JournalNo { get; set; } = "";
    public DateOnly JournalDate { get; set; }
    public string? Reference { get; set; }
    public string? Description { get; set; }
    public string Source { get; set; } = "";
    public string? RefType { get; set; }
    public string Status { get; set; } = "";
    public decimal TotalDebit { get; set; }
    public decimal TotalCredit { get; set; }
    public bool IsReversal { get; set; }
    public string? ReversedByNo { get; set; }
    public DateOnly? ReversalDate { get; set; }

    /// <summary>Badge status turunan: Draft / Posted / Dibatalkan / Pembalik.</summary>
    public (string Label, string Css) StatusBadge =>
        Status == "draft" ? ("Draft", "bg-secondary")
        : IsReversal ? ("Pembalik", "bg-warning text-dark")
        : ReversedByNo != null ? ("Dibatalkan", "bg-danger")
        : ("Posted", "bg-success");
}

public class JournalLineViewModel
{
    public long AccountId { get; set; }
    public string? AccountCode { get; set; }
    public string? AccountName { get; set; }
    public string? Description { get; set; }
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }

    /// <summary>Dimensi (Level 11) — hanya akun pendapatan/beban.</summary>
    public long? CostCenterId { get; set; }
    public string? CostCenterCode { get; set; }
    public long? ProjectId { get; set; }
    public string? ProjectCode { get; set; }
}

public class JournalViewModel : JournalListItemViewModel
{
    public string? VoidReason { get; set; }
    public long? RefId { get; set; }
    public string? CreatedByName { get; set; }
    public DateTime? CreatedAt { get; set; }
    public string? PostedByName { get; set; }
    public DateTime? PostedAt { get; set; }
    public Guid? ReversesUid { get; set; }
    public string? ReversesNo { get; set; }
    public Guid? ReversedByUid { get; set; }
    public string? SourceLabel { get; set; }

    /// <summary>uid dokumen sumber (aset / proses penyusutan) untuk link balik.</summary>
    public Guid? RefDocUid { get; set; }
    public bool CanEdit { get; set; }
    public bool CanVoid { get; set; }
    public bool CanReverseAccrual { get; set; }
    public List<JournalLineViewModel> Lines { get; set; } = new();
}

public class JournalFilterViewModel
{
    public string? Q { get; set; }
    public DateOnly? DateFrom { get; set; }
    public DateOnly? DateTo { get; set; }
    public string? Status { get; set; }
    public string? Source { get; set; }
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
        Add("dateFrom", DateFrom?.ToString("yyyy-MM-dd"));
        Add("dateTo", DateTo?.ToString("yyyy-MM-dd"));
        Add("status", Status);
        Add("source", Source);

        return string.Join("&", parts);
    }
}

public class JournalsIndexViewModel
{
    public JournalFilterViewModel Filter { get; set; } = new();
    public PagedResult<JournalListItemViewModel> Journals { get; set; } = new();
}

/// <summary>Form jurnal (buat / ubah draft / revisi).</summary>
public class JournalFormViewModel
{
    public Guid? Uid { get; set; }

    /// <summary>create | edit | revise</summary>
    public string Mode { get; set; } = "create";
    public string? OriginalNo { get; set; }
    public DateOnly JournalDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public string? Reference { get; set; }
    public string? Description { get; set; }
    public DateOnly? ReversalDate { get; set; }
    public string? Reason { get; set; }
    public List<JournalLineViewModel> Lines { get; set; } = new();
    public List<AccountOptionViewModel> Accounts { get; set; } = new();
    public DimensionOptions Dimensions { get; set; } = new();
}

// ===================== Saldo Awal =====================

public class OpeningBalanceViewModel
{
    public DateOnly? Date { get; set; }
    public Guid? JournalUid { get; set; }
    public string? JournalNo { get; set; }
    public List<OpeningBalanceLineViewModel> Lines { get; set; } = new();
}

public class OpeningBalanceLineViewModel
{
    public long AccountId { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string Type { get; set; } = "";
    public string NormalBalance { get; set; } = "";
    public bool IsControl { get; set; }
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
}

// ===================== Buku Besar & Neraca Saldo =====================

public class LedgerLineViewModel
{
    public DateOnly Date { get; set; }
    public Guid JournalUid { get; set; }
    public string JournalNo { get; set; } = "";
    public string? Reference { get; set; }
    public string? Description { get; set; }
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public decimal Balance { get; set; }
}

public class GeneralLedgerViewModel
{
    public AccountOptionViewModel Account { get; set; } = new();
    public DateOnly From { get; set; }
    public DateOnly To { get; set; }
    public decimal Opening { get; set; }
    public decimal TotalDebit { get; set; }
    public decimal TotalCredit { get; set; }
    public decimal Closing { get; set; }
    public List<LedgerLineViewModel> Lines { get; set; } = new();
}

public class GeneralLedgerPageViewModel
{
    public Guid? AccountUid { get; set; }
    public DateOnly From { get; set; }
    public DateOnly To { get; set; }
    public List<AccountOptionViewModel> Accounts { get; set; } = new();
    public GeneralLedgerViewModel? Ledger { get; set; }
}

public class TrialBalanceRowViewModel
{
    public Guid Uid { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string Type { get; set; } = "";
    public string NormalBalance { get; set; } = "";
    public decimal OpeningDebit { get; set; }
    public decimal OpeningCredit { get; set; }
    public decimal MovementDebit { get; set; }
    public decimal MovementCredit { get; set; }
    public decimal ClosingDebit { get; set; }
    public decimal ClosingCredit { get; set; }
}

public class TrialBalanceViewModel
{
    public DateOnly From { get; set; }
    public DateOnly To { get; set; }
    public bool IncludeZero { get; set; }
    public List<TrialBalanceRowViewModel> Rows { get; set; } = new();
    public decimal TotalOpeningDebit { get; set; }
    public decimal TotalOpeningCredit { get; set; }
    public decimal TotalMovementDebit { get; set; }
    public decimal TotalMovementCredit { get; set; }
    public decimal TotalClosingDebit { get; set; }
    public decimal TotalClosingCredit { get; set; }
    public bool IsBalanced { get; set; }
}

// ===================== Laporan Keuangan =====================

public class ReportLineViewModel
{
    public Guid? Uid { get; set; }
    public string? Code { get; set; }
    public string Name { get; set; } = "";
    public decimal Amount { get; set; }
    public bool IsContra { get; set; }
}

public class ReportSectionViewModel
{
    public string Key { get; set; } = "";
    public string Title { get; set; } = "";
    public List<ReportLineViewModel> Lines { get; set; } = new();
    public decimal Total { get; set; }
}

public class IncomeStatementViewModel
{
    public DateOnly From { get; set; }
    public DateOnly To { get; set; }
    public ReportSectionViewModel Revenue { get; set; } = new();
    public ReportSectionViewModel CostOfGoodsSold { get; set; } = new();
    public decimal GrossProfit { get; set; }
    public ReportSectionViewModel Expenses { get; set; } = new();
    public decimal NetIncome { get; set; }
}

public class BalanceSheetViewModel
{
    public DateOnly AsOf { get; set; }
    public ReportSectionViewModel Assets { get; set; } = new();
    public ReportSectionViewModel Liabilities { get; set; } = new();
    public ReportSectionViewModel Equity { get; set; } = new();
    public decimal CurrentEarnings { get; set; }
    public decimal TotalAssets { get; set; }
    public decimal TotalLiabilitiesAndEquity { get; set; }
    public bool IsBalanced { get; set; }
}

public class CashFlowViewModel
{
    public DateOnly From { get; set; }
    public DateOnly To { get; set; }
    public List<string> CashAccounts { get; set; } = new();
    public decimal OpeningCash { get; set; }
    public ReportSectionViewModel Operating { get; set; } = new();
    public ReportSectionViewModel Investing { get; set; } = new();
    public ReportSectionViewModel Financing { get; set; } = new();

    /// <summary>Pengaruh perubahan kurs terhadap kas valas (revaluasi, jual/beli valas).</summary>
    public ReportSectionViewModel FxEffect { get; set; } = new();
    public decimal NetChange { get; set; }
    public decimal ClosingCash { get; set; }
    public decimal ClosingCashLedger { get; set; }
    public bool IsReconciled { get; set; }
}

public class EquityChangeRowViewModel
{
    public Guid? Uid { get; set; }
    public string? Code { get; set; }
    public string Name { get; set; } = "";
    public decimal Opening { get; set; }
    public decimal Movement { get; set; }
    public decimal Closing { get; set; }
}

public class EquityChangesViewModel
{
    public DateOnly From { get; set; }
    public DateOnly To { get; set; }
    public List<EquityChangeRowViewModel> Rows { get; set; } = new();
    public decimal TotalOpening { get; set; }
    public decimal TotalMovement { get; set; }
    public decimal TotalClosing { get; set; }
    public decimal BalanceSheetEquity { get; set; }
    public bool IsReconciled { get; set; }
}

public class ClosingYearViewModel
{
    public int Year { get; set; }
    public int ClosedMonths { get; set; }
    public List<string> OpenMonths { get; set; } = new();
    public bool IsClosed { get; set; }
    public Guid? JournalUid { get; set; }
    public string? JournalNo { get; set; }
    public decimal NetIncome { get; set; }
    public bool CanClose { get; set; }
    public string? BlockReason { get; set; }
}

public class ClosingPreviewViewModel
{
    public int Year { get; set; }
    public List<ReportLineViewModel> Revenue { get; set; } = new();
    public List<ReportLineViewModel> Expenses { get; set; } = new();
    public decimal NetIncome { get; set; }
    public string? RetainedEarningsAccount { get; set; }
    public ClosingYearViewModel Status { get; set; } = new();
}

public class ClosingBookPageViewModel
{
    public List<ClosingYearViewModel> Years { get; set; } = new();
    public ClosingPreviewViewModel? Preview { get; set; }
}

/// <summary>Model partial _ReportRows: satu kelompok laporan (layar atau cetak).</summary>
public record ReportRowsModel(ReportSectionViewModel Section, bool Print, string? TotalLabel = null, DateOnly? LedgerFrom = null, DateOnly? LedgerTo = null);

// ===================== Kas & Bank =====================

public class CashAccountViewModel
{
    public Guid Uid { get; set; }
    public long Id { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string Type { get; set; } = "cash";
    public long CoaAccountId { get; set; }
    public string? CoaCode { get; set; }
    public string? CoaName { get; set; }
    public string? BankName { get; set; }
    public string? AccountNo { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Note { get; set; }

    /// <summary>Saldo buku besar (IDR).</summary>
    public decimal Balance { get; set; }
    public string CurrencyCode { get; set; } = Acc.FunctionalCurrency;

    /// <summary>Saldo dalam mata uang rekening.</summary>
    public decimal ForeignBalance { get; set; }
    public bool IsUsed { get; set; }

    /// <summary>Rekonsiliasi bank (Level 12): Balance = saldo buku = BankBalance + InTransit.</summary>
    public bool ReconcileEnabled { get; set; }
    public DateOnly? ReconcileFrom { get; set; }
    public decimal BankBalance { get; set; }
    public decimal InTransit { get; set; }

    public bool IsForeign => Acc.IsForeign(CurrencyCode);

    /// <summary>Label dropdown: nama + saldo dalam mata uang rekening.</summary>
    public string OptionLabel(string balanceWord) =>
        $"{Name}{(IsForeign ? $" [{CurrencyCode}]" : "")} ({balanceWord} {Acc.Fx(IsForeign ? ForeignBalance : Balance, CurrencyCode)})";
}

public class CashAccountsIndexViewModel
{
    public List<CashAccountViewModel> Accounts { get; set; } = new();
    public List<AccountOptionViewModel> CoaOptions { get; set; } = new();
    public List<CurrencyViewModel> Currencies { get; set; } = new();
}

public class CashTransactionViewModel
{
    public Guid Uid { get; set; }
    public string TrxNo { get; set; } = "";
    public DateOnly TrxDate { get; set; }
    public string Type { get; set; } = "";
    public long CashAccountId { get; set; }
    public string? CashAccountName { get; set; }
    public long? DestCashAccountId { get; set; }
    public string? DestCashAccountName { get; set; }
    public long? CounterAccountId { get; set; }
    public string? CounterCode { get; set; }
    public string? CounterName { get; set; }
    public long? CostCenterId { get; set; }
    public string? CostCenterCode { get; set; }
    public long? ProjectId { get; set; }
    public string? ProjectCode { get; set; }

    /// <summary>Nominal dalam mata uang rekening sumber.</summary>
    public decimal Amount { get; set; }
    public string CurrencyCode { get; set; } = Acc.FunctionalCurrency;
    public string? DestCurrencyCode { get; set; }
    public decimal? DestAmount { get; set; }
    public decimal FxRate { get; set; } = 1;
    public decimal FunctionalAmount { get; set; }
    public decimal FxGainLoss { get; set; }
    public string? Description { get; set; }
    public string? Reference { get; set; }
    public string? Attachment { get; set; }
    public string Status { get; set; } = "";

    /// <summary>Ada sisi valas (rekening sumber atau tujuan).</summary>
    public bool HasForeign => Acc.IsForeign(CurrencyCode) || Acc.IsForeign(DestCurrencyCode);
    public Guid? JournalUid { get; set; }
    public string? JournalNo { get; set; }
    public string? CreatedByName { get; set; }
    public DateTime? CreatedAt { get; set; }
    public bool RequiresApproval { get; set; }

    /// <summary>Pengajuan persetujuan terakhir yang relevan: petty_cash_expense (draft) / cash_transaction_unpost (posted).</summary>
    public string? ApprovalDocType { get; set; }

    /// <summary>pending | approved | rejected | cancelled; null = belum pernah diajukan.</summary>
    public string? ApprovalStatus { get; set; }

    public bool ApprovalPending => ApprovalStatus == "pending";
    public decimal Signed { get; set; }

    public string TypeLabel => Type switch { "in" => "Kas Masuk", "out" => "Kas Keluar", "transfer" => "Transfer", _ => Type };

    public string Counterpart => Type == "transfer"
        ? $"{CashAccountName} → {DestCashAccountName}"
        : $"{CounterCode} {CounterName}";

    public (string Label, string Css) StatusBadge => Status switch
    {
        "posted" => ApprovalPending ? ("Posted — pembatalan menunggu persetujuan", "bg-warning text-dark") : ("Posted", "bg-success"),
        "void" => ("Dibatalkan", "bg-danger"),
        _ => ApprovalPending ? ("Menunggu persetujuan", "bg-warning text-dark")
            : ApprovalStatus == "rejected" && RequiresApproval ? ("Draft — ditolak", "bg-danger")
            : RequiresApproval ? ("Draft — perlu persetujuan", "bg-secondary")
            : ("Draft", "bg-secondary")
    };
}

public class CashTransactionFilterViewModel
{
    public string? Q { get; set; }
    public string? Type { get; set; }
    public string? Status { get; set; }
    public Guid? CashAccountUid { get; set; }
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
        Add("type", Type);
        Add("status", Status);
        Add("cashAccountUid", CashAccountUid?.ToString());
        Add("dateFrom", DateFrom?.ToString("yyyy-MM-dd"));
        Add("dateTo", DateTo?.ToString("yyyy-MM-dd"));

        return string.Join("&", parts);
    }
}

public class CashTransactionsIndexViewModel
{
    public CashTransactionFilterViewModel Filter { get; set; } = new();
    public PagedResult<CashTransactionViewModel> Transactions { get; set; } = new();
    public List<CashAccountViewModel> CashAccounts { get; set; } = new();
}

public class CashTransactionFormViewModel
{
    public Guid? Uid { get; set; }
    public string? TrxNo { get; set; }
    public DateOnly TrxDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public string Type { get; set; } = "in";
    public long CashAccountId { get; set; }
    public long? DestCashAccountId { get; set; }
    public long? CounterAccountId { get; set; }

    /// <summary>Dimensi (Level 11) baris akun lawan — hanya akun pendapatan/beban.</summary>
    public long? CostCenterId { get; set; }
    public long? ProjectId { get; set; }
    public decimal Amount { get; set; }

    /// <summary>Kurs sisi valas (kosong = kurs tabel). Hanya dipakai bila salah satu rekening valas.</summary>
    public decimal? FxRate { get; set; }
    public string? Description { get; set; }
    public string? Reference { get; set; }
    public List<CashAccountViewModel> CashAccounts { get; set; } = new();
    public List<AccountOptionViewModel> CounterAccounts { get; set; } = new();
    public DimensionOptions Dimensions { get; set; } = new();
}

public class PettyCashSummaryViewModel
{
    public CashAccountViewModel? Account { get; set; }
    public decimal? ApprovalLimit { get; set; }
    public int DraftCount { get; set; }
    public List<CashAccountViewModel> Sources { get; set; } = new();
}

public class PettyCashPageViewModel
{
    public PettyCashSummaryViewModel Summary { get; set; } = new();
    public PagedResult<CashTransactionViewModel> Transactions { get; set; } = new();
    public List<AccountOptionViewModel> ExpenseAccounts { get; set; } = new();
    public DimensionOptions Dimensions { get; set; } = new();
    public DateOnly? DateFrom { get; set; }
    public DateOnly? DateTo { get; set; }
}
