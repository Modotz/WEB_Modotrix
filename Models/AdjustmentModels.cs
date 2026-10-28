namespace ERP.Web.Models;

// Level 13 — adjusting entries (API Docs/AKUNTANSI.md §12).

public class AccrualJournalViewModel
{
    public Guid Uid { get; set; }
    public string JournalNo { get; set; } = "";
    public DateOnly JournalDate { get; set; }
    public DateOnly ReversalDate { get; set; }
    public string? Reference { get; set; }
    public string? Description { get; set; }
    public decimal Amount { get; set; }

    /// <summary>pending | reversed | voided</summary>
    public string Status { get; set; } = "";
    public bool IsDue { get; set; }
    public Guid? ReversalUid { get; set; }
    public string? ReversalNo { get; set; }
    public DateOnly? ReversalJournalDate { get; set; }
}

public class AccrualsIndexViewModel
{
    public DateOnly? From { get; set; }
    public DateOnly? To { get; set; }
    public string? Status { get; set; }
    public List<AccrualJournalViewModel> Rows { get; set; } = new();

    public static (string Label, string Css) Badge(string status) => status switch
    {
        "pending" => ("Menunggu pembalikan", "bg-warning text-dark"),
        "reversed" => ("Sudah dibalik", "bg-success"),
        _ => ("Dibatalkan", "bg-danger")
    };
}

// ===================== Prepaid / deferred =====================

public class DeferralLineViewModel
{
    public string Period { get; set; } = "";
    public decimal Amount { get; set; }
    public bool IsRecognized { get; set; }
    public Guid? JournalUid { get; set; }
    public string? JournalNo { get; set; }
    public string? RecognitionNo { get; set; }
}

public class DeferralScheduleViewModel
{
    public Guid Uid { get; set; }

    /// <summary>purchase_invoice (prepaid) | sales_invoice (deferred revenue)</summary>
    public string SourceType { get; set; } = "";
    public Guid? SourceUid { get; set; }
    public string? SourceNo { get; set; }
    public string? PartyName { get; set; }
    public string? Description { get; set; }
    public string? DeferralAccountCode { get; set; }
    public string? DeferralAccountName { get; set; }
    public string? TargetAccountCode { get; set; }
    public string? TargetAccountName { get; set; }
    public string? CostCenterCode { get; set; }
    public string? ProjectCode { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal RecognizedAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public string StartPeriod { get; set; } = "";
    public string EndPeriod { get; set; } = "";
    public int Months { get; set; }
    public int RecognizedMonths { get; set; }
    public DateOnly StartDate { get; set; }
    public string Status { get; set; } = "";
    public List<DeferralLineViewModel> Lines { get; set; } = new();

    public bool IsPrepaid => SourceType == "purchase_invoice";
    public string SideLabel => IsPrepaid ? "Prepaid" : "Diterima di muka";
    public string SourceController => IsPrepaid ? "PurchaseInvoices" : "SalesInvoices";

    public static (string Label, string Css) Badge(string status) => status switch
    {
        "active" => ("Berjalan", "bg-info text-dark"),
        "completed" => ("Selesai", "bg-success"),
        _ => ("Dibatalkan", "bg-danger")
    };
}

public class DeferralSchedulesIndexViewModel
{
    public DateOnly AsOf { get; set; }
    public string? Side { get; set; }
    public string? Status { get; set; }
    public List<DeferralScheduleViewModel> Schedules { get; set; } = new();
    public List<ControlCheckViewModel> Checks { get; set; } = new();
}

public class DeferralPendingLineViewModel
{
    public string Period { get; set; } = "";
    public Guid ScheduleUid { get; set; }
    public string SourceType { get; set; } = "";
    public string? Description { get; set; }
    public string? TargetAccountCode { get; set; }
    public string? TargetAccountName { get; set; }
    public decimal Amount { get; set; }
}

public class DeferralRecognitionViewModel
{
    public Guid Uid { get; set; }
    public string RecognitionNo { get; set; } = "";
    public string Period { get; set; } = "";
    public DateOnly RecognitionDate { get; set; }
    public decimal TotalAmount { get; set; }
    public int LineCount { get; set; }
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

public class DeferralRunViewModel
{
    public string Period { get; set; } = "";
    public List<DeferralPendingLineViewModel> Lines { get; set; } = new();
    public List<string> LockedPeriods { get; set; } = new();
    public List<DeferralRecognitionViewModel> History { get; set; } = new();
}

// ===================== Cadangan kerugian piutang =====================

public class AllowanceEstimateViewModel
{
    public Guid Uid { get; set; }
    public string JournalNo { get; set; } = "";
    public DateOnly Date { get; set; }
    public decimal Amount { get; set; }
    public string? Description { get; set; }
    public bool IsReversed { get; set; }
    public Guid? ReversalUid { get; set; }
    public string? ReversalNo { get; set; }
}

public class WriteOffViewModel
{
    public Guid Uid { get; set; }
    public string WriteOffNo { get; set; } = "";
    public DateOnly WriteOffDate { get; set; }
    public Guid InvoiceUid { get; set; }
    public string InvoiceNo { get; set; } = "";
    public string? CustomerName { get; set; }
    public decimal Amount { get; set; }

    /// <summary>allowance | direct</summary>
    public string Method { get; set; } = "";
    public string Reason { get; set; } = "";
    public string Status { get; set; } = "";
    public Guid? JournalUid { get; set; }
    public string? JournalNo { get; set; }
    public string? VoidReason { get; set; }
    public DateOnly? VoidDate { get; set; }

    public static string MethodLabel(string method) => method == "allowance" ? "Via cadangan" : "Langsung";
}

public class WriteOffCandidateViewModel
{
    public Guid Uid { get; set; }
    public string InvoiceNo { get; set; } = "";
    public string? CustomerName { get; set; }
    public DateOnly InvoiceDate { get; set; }
    public DateOnly? DueDate { get; set; }
    public int DaysOverdue { get; set; }
    public decimal Total { get; set; }
    public decimal Outstanding { get; set; }
}

public class DoubtfulReceivablesViewModel
{
    public DateOnly AsOf { get; set; }
    public decimal ReceivableBalance { get; set; }
    public decimal AllowanceBalance { get; set; }
    public decimal NetReceivable { get; set; }
    public decimal EstimateTotal { get; set; }
    public decimal WriteOffAllowanceTotal { get; set; }
    public decimal WriteOffDirectTotal { get; set; }
    public List<AllowanceEstimateViewModel> Estimates { get; set; } = new();
    public List<WriteOffViewModel> WriteOffs { get; set; } = new();
    public List<WriteOffCandidateViewModel> OpenInvoices { get; set; } = new();
}

/// <summary>Badge Periode Akuntansi: pekerjaan akhir bulan yang belum dijalankan.</summary>
public class PeriodTaskCountsViewModel
{
    public int PendingAccrualReversals { get; set; }
    public int PendingRecognitions { get; set; }
    public int PendingDepreciations { get; set; }

    public bool Any => PendingAccrualReversals + PendingRecognitions + PendingDepreciations > 0;
}
