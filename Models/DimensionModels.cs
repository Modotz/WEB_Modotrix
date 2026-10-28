namespace ERP.Web.Models;

// Level 11: cost center & project (dimensi baris jurnal), anggaran, alokasi biaya, laporan per dimensi.

public class CostCenterViewModel
{
    public Guid Uid { get; set; }
    public long Id { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public int UsageCount { get; set; }
}

public class ProjectViewModel
{
    public static readonly (string Value, string Label)[] Types = { ("internal", "Internal"), ("client", "Client") };
    public static readonly (string Value, string Label)[] StatusList = { ("open", "Berjalan"), ("completed", "Selesai"), ("cancelled", "Dibatalkan") };

    public Guid Uid { get; set; }
    public long Id { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string ProjectType { get; set; } = "internal";
    public long? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public string Status { get; set; } = "open";
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public decimal EstimatedBudget { get; set; }
    public string? Description { get; set; }
    public int UsageCount { get; set; }

    public static string TypeLabel(string? type) => type == "client" ? "Client" : "Internal";

    public static string StatusLabel(string? status) => status switch
    {
        "completed" => "Selesai",
        "cancelled" => "Dibatalkan",
        _ => "Berjalan"
    };

    public static string StatusCss(string? status) => status switch
    {
        "completed" => "bg-success",
        "cancelled" => "bg-secondary",
        _ => "bg-info text-dark"
    };
}

public class ProjectsPageViewModel
{
    public List<ProjectViewModel> Projects { get; set; } = new();
    public List<CustomerOptionViewModel> Customers { get; set; } = new();
    public string? Status { get; set; }
}

public class CustomerOptionViewModel
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
}

/// <summary>Pilihan dropdown dimensi di form.</summary>
public class DimensionOptionViewModel
{
    public long Id { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string? ProjectType { get; set; }
}

/// <summary>Daftar pilihan cost center &amp; project untuk form dokumen.</summary>
public class DimensionOptions
{
    public List<DimensionOptionViewModel> CostCenters { get; set; } = new();
    public List<DimensionOptionViewModel> Projects { get; set; } = new();
}

public class BudgetViewModel
{
    public Guid Uid { get; set; }
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public string BudgetAgainst { get; set; } = "cost_center";
    public long? CostCenterId { get; set; }
    public long? ProjectId { get; set; }
    public string DimensionCode { get; set; } = "";
    public string DimensionName { get; set; } = "";
    public long AccountId { get; set; }
    public string AccountCode { get; set; } = "";
    public string AccountName { get; set; } = "";
    public int FiscalYear { get; set; } = DateTime.Today.Year;
    public decimal Amount { get; set; }
    public string? Note { get; set; }
    public decimal Actual { get; set; }
    public decimal Pending { get; set; }
    public decimal Remaining { get; set; }
    public decimal UsedPercent { get; set; }

    /// <summary>safe | warning | over</summary>
    public string State { get; set; } = "safe";

    public static (string Label, string Css) StateBadge(string? state) => state switch
    {
        "over" => ("Melebihi", "bg-danger"),
        "warning" => ("Mendekati", "bg-warning text-dark"),
        _ => ("Aman", "bg-success")
    };
}

public class BudgetsPageViewModel
{
    public int Year { get; set; } = DateTime.Today.Year;
    public string? Against { get; set; }
    public List<BudgetViewModel> Budgets { get; set; } = new();
    public List<DimensionOptionViewModel> CostCenters { get; set; } = new();
    public List<DimensionOptionViewModel> Projects { get; set; } = new();
    public List<AccountOptionViewModel> ExpenseAccounts { get; set; } = new();
}

public class CostAllocationLineViewModel
{
    public long ToCostCenterId { get; set; }
    public string CostCenterCode { get; set; } = "";
    public string CostCenterName { get; set; } = "";
    public decimal Percentage { get; set; }
}

public class CostAllocationRowViewModel
{
    public long AccountId { get; set; }
    public string AccountCode { get; set; } = "";
    public string AccountName { get; set; } = "";
    public decimal Basis { get; set; }
    public List<decimal> Shares { get; set; } = new();
}

public class CostAllocationViewModel
{
    public Guid Uid { get; set; }
    public string AllocationNo { get; set; } = "";
    public string Name { get; set; } = "";
    public long FromCostCenterId { get; set; }
    public string FromCostCenterCode { get; set; } = "";
    public string FromCostCenterName { get; set; } = "";
    public DateOnly AllocationDate { get; set; }
    public DateOnly BasisFrom { get; set; }
    public string Status { get; set; } = "draft";
    public decimal TotalAmount { get; set; }
    public Guid? JournalUid { get; set; }
    public string? JournalNo { get; set; }
    public string? Description { get; set; }
    public string? VoidReason { get; set; }
    public DateOnly? VoidDate { get; set; }
    public string? CreatedByName { get; set; }
    public List<CostAllocationLineViewModel> Lines { get; set; } = new();
    public List<CostAllocationRowViewModel> Rows { get; set; } = new();
}

public class CostAllocationFormViewModel
{
    public Guid? Uid { get; set; }
    public string? AllocationNo { get; set; }
    public string? Name { get; set; }
    public long FromCostCenterId { get; set; }
    public DateOnly AllocationDate { get; set; } = new(DateTime.Today.Year, DateTime.Today.Month, DateTime.DaysInMonth(DateTime.Today.Year, DateTime.Today.Month));
    public string? Description { get; set; }
    public List<CostAllocationLineViewModel> Lines { get; set; } = new();
    public List<DimensionOptionViewModel> CostCenters { get; set; } = new();
}

// ---------- Laporan ----------

public class DimensionColumnViewModel
{
    public long? Id { get; set; }
    public Guid? Uid { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
}

public class CostCenterPnlRowViewModel
{
    public long AccountId { get; set; }
    public Guid? AccountUid { get; set; }
    public string AccountCode { get; set; } = "";
    public string AccountName { get; set; } = "";
    public List<decimal> Amounts { get; set; } = new();
    public decimal Untagged { get; set; }
    public decimal Total { get; set; }
}

public class CostCenterPnlViewModel
{
    public DateOnly From { get; set; }
    public DateOnly To { get; set; }
    public List<DimensionColumnViewModel> CostCenters { get; set; } = new();
    public List<CostCenterPnlRowViewModel> Rows { get; set; } = new();
    public List<decimal> Totals { get; set; } = new();
    public decimal UntaggedTotal { get; set; }
    public decimal GrandTotal { get; set; }
    public decimal TaggedRevenue { get; set; }
}

public class ProjectProfitRowViewModel
{
    public Guid Uid { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string ProjectType { get; set; } = "";
    public string? CustomerName { get; set; }
    public string Status { get; set; } = "";
    public decimal EstimatedBudget { get; set; }
    public decimal Revenue { get; set; }
    public decimal Cost { get; set; }
    public decimal Margin { get; set; }
    public decimal? MarginPercent { get; set; }
}

public class ProjectProfitabilityViewModel
{
    public DateOnly From { get; set; }
    public DateOnly To { get; set; }
    public List<ProjectProfitRowViewModel> Rows { get; set; } = new();
    public decimal TotalRevenue { get; set; }
    public decimal TotalCost { get; set; }
}

public class ProjectSourceRowViewModel
{
    public string Source { get; set; } = "";
    public decimal Revenue { get; set; }
    public decimal Cost { get; set; }

    public static string Label(string source) => source switch
    {
        "purchase_invoice" => "Faktur Pembelian",
        "sales_invoice" => "Faktur Penjualan",
        "cash" => "Kas & Bank",
        "journal" => "Jurnal Umum",
        _ => "Lainnya"
    };
}

public class ProjectAccountRowViewModel
{
    public string AccountCode { get; set; } = "";
    public string AccountName { get; set; } = "";
    public string AccountType { get; set; } = "";
    public decimal Amount { get; set; }
}

public class ProjectLedgerRowViewModel
{
    public DateOnly Date { get; set; }
    public Guid? JournalUid { get; set; }
    public string JournalNo { get; set; } = "";
    public string? Reference { get; set; }
    public string? Description { get; set; }
    public string AccountCode { get; set; } = "";
    public string AccountName { get; set; } = "";
    public string? CostCenterCode { get; set; }
    public decimal Revenue { get; set; }
    public decimal Cost { get; set; }
}

public class ProjectProfitDetailViewModel
{
    public ProjectProfitRowViewModel Project { get; set; } = new();
    public DateOnly From { get; set; }
    public DateOnly To { get; set; }
    public List<ProjectSourceRowViewModel> Sources { get; set; } = new();
    public List<ProjectAccountRowViewModel> Accounts { get; set; } = new();
    public List<ProjectLedgerRowViewModel> Lines { get; set; } = new();
    public decimal InvoiceRevenue { get; set; }
    public decimal InvoiceCost { get; set; }
}

public class BudgetVsActualViewModel
{
    public int Year { get; set; } = DateTime.Today.Year;
    public List<BudgetViewModel> Rows { get; set; } = new();
    public decimal TotalBudget { get; set; }
    public decimal TotalActual { get; set; }
    public decimal TotalPending { get; set; }
}
