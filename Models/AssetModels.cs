namespace ERP.Web.Models;

/// <summary>Label &amp; badge bersama modul Manajemen Aset (teks Indonesia = kunci terjemahan).</summary>
public static class AssetLabels
{
    public static readonly (string Value, string Label)[] Methods =
    {
        ("straight_line", "Garis lurus"), ("declining_balance", "Saldo menurun"), ("sum_of_years", "Jumlah angka tahun"),
        ("units_of_production", "Unit produksi"), ("none", "Tidak disusutkan")
    };

    /// <summary>Penyesuaian nilai (T3 lanjutan, Level 13).</summary>
    public static string Adjustment(string kind, string? direction) => kind switch
    {
        "capex" => "Tambah biaya",
        "revaluation" => direction == "up" ? "Revaluasi naik" : "Revaluasi turun",
        "impairment" => "Impairment",
        _ => "Pembalikan impairment"
    };

    public static string AdjustmentCss(string kind) => kind switch
    {
        "capex" => "bg-primary",
        "revaluation" => "bg-info text-dark",
        "impairment" => "bg-danger",
        _ => "bg-success"
    };

    public static readonly (string Value, string Label)[] Sources =
    {
        ("cash", "Pembelian tunai (kas/bank)"), ("stock", "Dari stok gudang"), ("opening", "Saldo awal"), ("purchase_order", "PO Aset")
    };

    public static readonly (string Value, string Label)[] Statuses =
    {
        ("draft", "Draft"), ("active", "Aktif"), ("in_repair", "Perbaikan"), ("disposed", "Dilepas"), ("lost", "Hilang"), ("void", "Dibatalkan")
    };

    public static readonly (string Value, string Label)[] DisposalTypes =
    {
        ("sale", "Dijual"), ("scrap", "Dibuang / rusak"), ("lost", "Hilang")
    };

    public static readonly (string Value, string Label)[] MaintenanceTypes =
    {
        ("service", "Servis Rutin"), ("repair", "Perbaikan"), ("inspection", "Inspeksi"), ("calibration", "Kalibrasi"), ("other", "Lainnya")
    };

    private static string Find((string Value, string Label)[] list, string? value) =>
        list.FirstOrDefault(x => x.Value == value).Label ?? value ?? "-";

    public static string Method(string? v) => Find(Methods, v);
    public static string Source(string? v) => Find(Sources, v);
    public static string Status(string? v) => Find(Statuses, v);
    public static string MaintenanceType(string? v) => Find(MaintenanceTypes, v);

    public static string Disposal(string? v) => v switch
    {
        "sale" => "Dijual",
        "scrap" => "Dibuang / rusak",
        "lost" => "Hilang",
        "return" => "Retur ke supplier",
        _ => "-"
    };

    public static string StatusCss(string? v) => v switch
    {
        "draft" => "bg-secondary",
        "active" => "bg-success",
        "in_repair" => "bg-warning text-dark",
        "disposed" => "bg-dark",
        "lost" => "bg-danger",
        "void" => "bg-secondary",
        _ => "bg-secondary"
    };

    public static string RefType(string? v) => v switch
    {
        "asset_acquisition" => "Perolehan",
        "asset_disposal" => "Pelepasan",
        "asset_maintenance" => "Maintenance",
        "depreciation" => "Penyusutan",
        "asset_adjustment" => "Penyesuaian nilai",
        _ => v ?? "-"
    };

    /// <summary>yyyy-MM → "Okt 2026" gaya singkat (tetap angka agar netral bahasa).</summary>
    public static string Period(string? p) => string.IsNullOrEmpty(p) || p.Length != 7 ? p ?? "-" : $"{p[5..]}/{p[..4]}";
}

// ===================== Kategori =====================

public class AssetCategoryViewModel
{
    public Guid Uid { get; set; }
    public long Id { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public int UsefulLifeMonths { get; set; }
    public string DepreciationMethod { get; set; } = "straight_line";
    public decimal? DecliningFactor { get; set; }
    public decimal SalvagePercent { get; set; }
    public long? AssetAccountId { get; set; }
    public string? AssetAccountLabel { get; set; }
    public long? AccumAccountId { get; set; }
    public string? AccumAccountLabel { get; set; }
    public long? ExpenseAccountId { get; set; }
    public string? ExpenseAccountLabel { get; set; }
    public bool IsActive { get; set; } = true;
    public int AssetCount { get; set; }
}

public class AssetAccountOptionViewModel
{
    public long Id { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string Label => $"{Code} — {Name}";
}

public class AssetCategoryFormOptionsViewModel
{
    public List<AssetAccountOptionViewModel> AssetAccounts { get; set; } = new();
    public List<AssetAccountOptionViewModel> AccumAccounts { get; set; } = new();
    public List<AssetAccountOptionViewModel> ExpenseAccounts { get; set; } = new();
}

public class AssetCategoriesIndexViewModel
{
    public string? Q { get; set; }
    public int Page { get; set; } = 1;
    public PagedResult<AssetCategoryViewModel> Categories { get; set; } = new();
    public AssetCategoryFormOptionsViewModel Options { get; set; } = new();
}

// ===================== Aset =====================

public class AssetFilterViewModel
{
    public string? Q { get; set; }
    public string? Status { get; set; }
    public Guid? CategoryUid { get; set; }
    public string? Source { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;

    public string ToQueryString()
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(Q)) parts.Add("q=" + Uri.EscapeDataString(Q));
        if (!string.IsNullOrWhiteSpace(Status)) parts.Add("status=" + Uri.EscapeDataString(Status));
        if (CategoryUid != null) parts.Add("categoryUid=" + CategoryUid);
        if (!string.IsNullOrWhiteSpace(Source)) parts.Add("source=" + Uri.EscapeDataString(Source));
        return string.Join("&", parts);
    }
}

public class AssetListItemViewModel
{
    public Guid Uid { get; set; }
    public string AssetNo { get; set; } = "";
    public string Name { get; set; } = "";
    public string? CategoryName { get; set; }
    public string? AcquisitionSource { get; set; }
    public string Status { get; set; } = "";
    public DateOnly? AcquisitionDate { get; set; }
    public decimal AcquisitionCost { get; set; }
    public decimal AccumulatedDepreciation { get; set; }
    public decimal BookValue { get; set; }
    public string? HolderName { get; set; }
    public string? Location { get; set; }
    public string? SerialNo { get; set; }
    public string DepreciationMethod { get; set; } = "";
    public bool HasRevaluation { get; set; }
    public bool HasImpairment { get; set; }
}

public class AssetScheduleRowViewModel
{
    public string Period { get; set; } = "";
    public decimal Amount { get; set; }
    public decimal AccumulatedAfter { get; set; }
    public decimal BookValueAfter { get; set; }
    public bool Posted { get; set; }
    public Guid? JournalUid { get; set; }
    public string? JournalNo { get; set; }
    public string? RunNo { get; set; }
    public string? RebaseNote { get; set; }
    public decimal? DecliningCandidate { get; set; }
    public decimal? StraightLineCandidate { get; set; }
    public decimal? Units { get; set; }
}

public class AssetUsageViewModel
{
    public Guid Uid { get; set; }
    public string Period { get; set; } = "";
    public decimal Units { get; set; }
    public string? Note { get; set; }

    /// <summary>pending | planned | posted | zero</summary>
    public string Status { get; set; } = "";
    public decimal? Amount { get; set; }
}

public class AssetAdjustmentViewModel
{
    public Guid Uid { get; set; }
    public string AdjustmentNo { get; set; } = "";
    public string Kind { get; set; } = "";
    public string? Direction { get; set; }
    public DateOnly AdjustmentDate { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; } = "";
    public string? Note { get; set; }
    public string? VoidReason { get; set; }
    public string? CashAccountName { get; set; }
    public Guid? JournalUid { get; set; }
    public string? JournalNo { get; set; }
    public string? ReversesNo { get; set; }
    public string? ReversedByNo { get; set; }
    public bool CanReverse { get; set; }
    public bool CanVoid { get; set; }
}

public class AssetAssignmentViewModel
{
    public Guid Uid { get; set; }
    public Guid AssetUid { get; set; }
    public string? AssetNo { get; set; }
    public string? AssetName { get; set; }
    public string HolderName { get; set; } = "";
    public string? HolderDepartment { get; set; }
    public string? Location { get; set; }
    public DateOnly? AssignedDate { get; set; }
    public DateOnly? ReturnedDate { get; set; }
    public string Status { get; set; } = "";
    public string? Note { get; set; }
    public string? ReturnNote { get; set; }
}

public class AssetMaintenanceViewModel
{
    public Guid Uid { get; set; }
    public Guid AssetUid { get; set; }
    public string? AssetNo { get; set; }
    public string? AssetName { get; set; }
    public DateOnly? MaintenanceDate { get; set; }
    public string Type { get; set; } = "";
    public string? Description { get; set; }
    public decimal Cost { get; set; }
    public string? Vendor { get; set; }
    public DateOnly? NextDueDate { get; set; }
    public string? Note { get; set; }
    public string? CashAccountName { get; set; }
    public string? ExpenseAccountLabel { get; set; }
    public Guid? JournalUid { get; set; }
    public string? JournalNo { get; set; }
}

public class AssetJournalViewModel
{
    public Guid Uid { get; set; }
    public string JournalNo { get; set; } = "";
    public DateOnly JournalDate { get; set; }
    public string? Description { get; set; }
    public string? RefType { get; set; }
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public bool IsReversal { get; set; }
    public bool IsReversed { get; set; }
}

public class AssetViewModel : AssetListItemViewModel
{
    public long? AssetCategoryId { get; set; }
    public Guid? CategoryUid { get; set; }
    public long? SupplierId { get; set; }
    public string? SupplierName { get; set; }
    public long? ProductId { get; set; }
    public Guid? ProductUid { get; set; }
    public string? ProductName { get; set; }
    public long? SourceWarehouseId { get; set; }
    public Guid? WarehouseUid { get; set; }
    public string? WarehouseName { get; set; }
    public long? CashAccountId { get; set; }
    public string? CashAccountName { get; set; }
    public long? CounterAccountId { get; set; }
    public string? CounterAccountLabel { get; set; }
    public DateOnly? OpeningDate { get; set; }
    public decimal OpeningAccumulated { get; set; }
    public string? HolderDepartment { get; set; }
    public DateOnly? WarrantyUntil { get; set; }
    public decimal? DecliningFactor { get; set; }
    public int UsefulLifeMonths { get; set; }
    public decimal SalvageValue { get; set; }
    public DateOnly? DepreciationStart { get; set; }
    public string? Note { get; set; }
    public string? AssetAccountLabel { get; set; }
    public string? AccumAccountLabel { get; set; }
    public string? ExpenseAccountLabel { get; set; }
    public Guid? AcquisitionJournalUid { get; set; }
    public string? AcquisitionJournalNo { get; set; }
    public DateOnly? DisposalDate { get; set; }
    public string? DisposalType { get; set; }
    public decimal? DisposalProceeds { get; set; }
    public string? DisposalCashAccountName { get; set; }
    public string? DisposalNote { get; set; }
    public Guid? DisposalJournalUid { get; set; }
    public string? DisposalJournalNo { get; set; }
    public string? VoidReason { get; set; }
    public string? CreatedByName { get; set; }
    public DateTime? CreatedAt { get; set; }
    public decimal GlCost { get; set; }
    public decimal GlAccumulated { get; set; }
    public decimal PlannedTotal { get; set; }
    public decimal PostedTotal { get; set; }

    /// <summary>T3 lanjutan (Level 13): unit produksi, rincian harga perolehan, penyesuaian nilai.</summary>
    public decimal? TotalEstimatedUnits { get; set; }
    public decimal UnitsUsed { get; set; }
    public string? MissingUsagePeriod { get; set; }
    public decimal OriginalCost { get; set; }
    public decimal CapexTotal { get; set; }
    public decimal RevaluationNet { get; set; }
    public decimal ImpairmentNet { get; set; }
    public decimal RevaluationSurplus { get; set; }
    public List<AssetUsageViewModel> UsageRecords { get; set; } = new();
    public List<AssetAdjustmentViewModel> Adjustments { get; set; } = new();
    public bool CanAdjust { get; set; }

    public bool IsUnits => DepreciationMethod == "units_of_production";
    public List<AssetScheduleRowViewModel> Schedule { get; set; } = new();
    public List<AssetAssignmentViewModel> Assignments { get; set; } = new();
    public List<AssetMaintenanceViewModel> Maintenances { get; set; } = new();
    public List<AssetJournalViewModel> Journals { get; set; } = new();
    public bool CanEdit { get; set; }
    public bool CanEditFinancial { get; set; }
    public bool CanActivate { get; set; }
    public bool CanChangeSchedule { get; set; }
    public bool CanDispose { get; set; }
    public bool CanCancelDisposal { get; set; }
    public bool CanDelete { get; set; }
    public bool CanVoid { get; set; }
}

public class AssetOptionViewModel
{
    public long Id { get; set; }
    public Guid? Uid { get; set; }
    public string Name { get; set; } = "";
    public string? Code { get; set; }
    public string Label => string.IsNullOrEmpty(Code) ? Name : $"{Code} — {Name}";
}

public class AssetCategoryOptionViewModel : AssetOptionViewModel
{
    public int UsefulLifeMonths { get; set; }
    public string DepreciationMethod { get; set; } = "";
    public decimal? DecliningFactor { get; set; }
    public decimal SalvagePercent { get; set; }
}

public class AssetProductOptionViewModel : AssetOptionViewModel
{
    public bool TrackSerial { get; set; }
    public decimal CostPrice { get; set; }
}

public class AssetCashOptionViewModel : AssetOptionViewModel
{
    public decimal Balance { get; set; }
}

public class AssetFormOptionsViewModel
{
    public List<AssetCategoryOptionViewModel> Categories { get; set; } = new();
    public List<AssetCashOptionViewModel> CashAccounts { get; set; } = new();
    public List<AssetOptionViewModel> Suppliers { get; set; } = new();
    public List<AssetOptionViewModel> Warehouses { get; set; } = new();
    public List<AssetProductOptionViewModel> Products { get; set; } = new();
    public List<AssetOptionViewModel> EquityAccounts { get; set; } = new();
    public List<AssetOptionViewModel> ExpenseAccounts { get; set; } = new();
    public List<AssetOptionViewModel> Assets { get; set; } = new();
    public long? DefaultCounterAccountId { get; set; }
    public long? DefaultMaintenanceExpenseId { get; set; }
}

public class AssetFormViewModel
{
    public Guid? Uid { get; set; }
    public string? AssetNo { get; set; }
    public string Status { get; set; } = "draft";
    public string? Name { get; set; }
    public long? AssetCategoryId { get; set; }
    public string AcquisitionSource { get; set; } = "cash";
    public DateOnly AcquisitionDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public decimal AcquisitionCost { get; set; }
    public long? SupplierId { get; set; }
    public long? CashAccountId { get; set; }
    public long? ProductId { get; set; }
    public long? SourceWarehouseId { get; set; }
    public string? SerialNo { get; set; }
    public long? CounterAccountId { get; set; }
    public DateOnly? OpeningDate { get; set; }
    public decimal OpeningAccumulated { get; set; }
    public string DepreciationMethod { get; set; } = "straight_line";
    public decimal? DecliningFactor { get; set; }
    public int UsefulLifeMonths { get; set; }
    public decimal SalvageValue { get; set; }
    public DateOnly? DepreciationStart { get; set; }

    /// <summary>Units of production: estimasi total unit.</summary>
    public decimal? TotalEstimatedUnits { get; set; }
    public string? Location { get; set; }
    public DateOnly? WarrantyUntil { get; set; }
    public string? Note { get; set; }

    public AssetFormOptionsViewModel Options { get; set; } = new();

    /// <summary>Aset aktif: hanya data non-finansial yang bisa diubah.</summary>
    public bool FinancialLocked => Uid != null && Status != "draft";
}

public class AssetsIndexViewModel
{
    public AssetFilterViewModel Filter { get; set; } = new();
    public PagedResult<AssetListItemViewModel> Assets { get; set; } = new();
    public List<AssetCategoryOptionViewModel> Categories { get; set; } = new();
}

public class AssetDetailsPageViewModel
{
    public AssetViewModel Asset { get; set; } = new();
    public List<AssetCashOptionViewModel> CashAccounts { get; set; } = new();
}

public class AssetSerialOptionViewModel
{
    public string SerialNo { get; set; } = "";
    public decimal Cost { get; set; }
    public DateOnly? WarrantyUntil { get; set; }
}

// ===================== Serah terima & maintenance =====================

public class AssetAssignmentsIndexViewModel
{
    public string? Q { get; set; }
    public string? Status { get; set; }
    public int Page { get; set; } = 1;
    public PagedResult<AssetAssignmentViewModel> Rows { get; set; } = new();
    public List<AssetOptionViewModel> Assets { get; set; } = new();
    public Guid? PreselectAssetUid { get; set; }
}

public class MaintenanceReminderViewModel
{
    public Guid AssetUid { get; set; }
    public string AssetNo { get; set; } = "";
    public string AssetName { get; set; } = "";
    public DateOnly? Date { get; set; }
}

public class MaintenanceRemindersViewModel
{
    public List<MaintenanceReminderViewModel> DueMaintenance { get; set; } = new();
    public List<MaintenanceReminderViewModel> WarrantyExpiring { get; set; } = new();
}

public class AssetMaintenanceIndexViewModel
{
    public string? Q { get; set; }
    public string? Type { get; set; }
    public int Page { get; set; } = 1;
    public PagedResult<AssetMaintenanceViewModel> Rows { get; set; } = new();
    public MaintenanceRemindersViewModel Reminders { get; set; } = new();
    public AssetFormOptionsViewModel Options { get; set; } = new();
    public Guid? PreselectAssetUid { get; set; }
}

// ===================== Penyusutan =====================

public class DepreciationPreviewRowViewModel
{
    public Guid AssetUid { get; set; }
    public string AssetNo { get; set; } = "";
    public string AssetName { get; set; } = "";
    public string PeriodFrom { get; set; } = "";
    public string PeriodTo { get; set; } = "";
    public int Months { get; set; }
    public decimal Amount { get; set; }
}

public class DepreciationRunViewModel
{
    public Guid Uid { get; set; }
    public string RunNo { get; set; } = "";
    public string Period { get; set; } = "";
    public DateOnly RunDate { get; set; }
    public string Kind { get; set; } = "";
    public decimal TotalAmount { get; set; }
    public int AssetCount { get; set; }
    public int RowCount { get; set; }
    public string Status { get; set; } = "";
    public Guid? JournalUid { get; set; }
    public string? JournalNo { get; set; }
    public Guid? ReversalJournalUid { get; set; }
    public string? ReversalJournalNo { get; set; }
    public string? ReverseReason { get; set; }
    public string? Note { get; set; }
    public string? CreatedByName { get; set; }
    public DateTime? CreatedAt { get; set; }
    public bool CanReverse { get; set; }
}

public class DepreciationPageViewModel
{
    public string Period { get; set; } = "";
    public bool PeriodOpen { get; set; }
    public string? PeriodMessage { get; set; }
    public List<DepreciationPreviewRowViewModel> Preview { get; set; } = new();
    public decimal PreviewTotal { get; set; }
    public List<DepreciationRunViewModel> Runs { get; set; } = new();
    public int ActiveAssets { get; set; }
    public int DepreciableAssets { get; set; }
    public decimal TotalCost { get; set; }
    public decimal TotalAccumulated { get; set; }

    /// <summary>Aset units of production yang di-skip: unit bulan berikutnya belum dicatat.</summary>
    public List<MissingUsageViewModel> MissingUnits { get; set; } = new();
}

public class MissingUsageViewModel
{
    public Guid AssetUid { get; set; }
    public string AssetNo { get; set; } = "";
    public string AssetName { get; set; } = "";
    public string Period { get; set; } = "";
}

// ===================== Laporan nilai buku =====================

public class BookValueRowViewModel
{
    public Guid Uid { get; set; }
    public string AssetNo { get; set; } = "";
    public string Name { get; set; } = "";
    public string Status { get; set; } = "";
    public DateOnly? AcquisitionDate { get; set; }
    public decimal Cost { get; set; }
    public decimal Accumulated { get; set; }
    public decimal BookValue { get; set; }
}

public class BookValueCategoryViewModel
{
    public string CategoryName { get; set; } = "";
    public List<BookValueRowViewModel> Rows { get; set; } = new();
    public decimal Cost { get; set; }
    public decimal Accumulated { get; set; }
    public decimal BookValue { get; set; }
}

public class BookValueGlCheckViewModel
{
    public string AccountCode { get; set; } = "";
    public string AccountName { get; set; } = "";
    public string Kind { get; set; } = "";
    public decimal Subledger { get; set; }
    public decimal Ledger { get; set; }
    public decimal Difference { get; set; }
}

public class BookValueReportViewModel
{
    public DateOnly AsOf { get; set; }
    public Guid? CategoryUid { get; set; }
    public string? CategoryName { get; set; }
    public List<BookValueCategoryViewModel> Categories { get; set; } = new();
    public decimal TotalCost { get; set; }
    public decimal TotalAccumulated { get; set; }
    public decimal TotalBookValue { get; set; }
    public List<BookValueGlCheckViewModel> GlCheck { get; set; } = new();
    public bool IsReconciled { get; set; }

    /// <summary>Pilihan filter kategori (halaman web).</summary>
    public List<AssetCategoryOptionViewModel> CategoryOptions { get; set; } = new();
}
