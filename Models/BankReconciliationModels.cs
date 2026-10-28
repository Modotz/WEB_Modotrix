namespace ERP.Web.Models;

// Level 12: rekonsiliasi bank (rekening koran, pencocokan, item belum terkonfirmasi).

public class BankStatementListViewModel
{
    public Guid Uid { get; set; }
    public string StatementNo { get; set; } = "";
    public long CashAccountId { get; set; }
    public string CashAccountName { get; set; } = "";
    public string? Reference { get; set; }
    public DateOnly PeriodFrom { get; set; }
    public DateOnly StatementDate { get; set; }
    public decimal OpeningBalance { get; set; }
    public decimal ClosingBalance { get; set; }
    public decimal ComputedClosing { get; set; }
    public decimal Difference { get; set; }
    public string Status { get; set; } = "draft";
    public int LineCount { get; set; }
    public int MatchedCount { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? CompletedByName { get; set; }

    public static (string Label, string Css) StatusBadge(string status) =>
        status == "completed" ? ("Selesai", "bg-success") : ("Draft", "bg-secondary");
}

public class BankStatementLineViewModel
{
    public Guid Uid { get; set; }
    public DateOnly LineDate { get; set; }
    public string Description { get; set; } = "";
    public string? Reference { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; } = "unmatched";
    public long? JournalLineId { get; set; }
    public string? JournalNo { get; set; }
    public Guid? JournalUid { get; set; }
    public string? JournalDescription { get; set; }
    public string? ClearingJournalNo { get; set; }
    public Guid? ClearingJournalUid { get; set; }
}

public class ReconcileCandidateViewModel
{
    public long JournalLineId { get; set; }
    public Guid? JournalUid { get; set; }
    public string JournalNo { get; set; } = "";
    public DateOnly Date { get; set; }
    public string? Reference { get; set; }
    public string? Description { get; set; }
    public string AccountCode { get; set; } = "";
    public string AccountName { get; set; } = "";
    public string Kind { get; set; } = "bank";
    public decimal Amount { get; set; }
}

public class BankStatementViewModel : BankStatementListViewModel
{
    public string? Note { get; set; }
    public DateOnly? ReconcileFrom { get; set; }
    public List<BankStatementLineViewModel> Lines { get; set; } = new();
    public List<ReconcileCandidateViewModel> Candidates { get; set; } = new();
    public decimal BookBankBalance { get; set; }
    public decimal UnconfirmedBank { get; set; }
    public decimal ConfirmedBank { get; set; }
    public decimal BookDifference { get; set; }
    public decimal InTransitPayments { get; set; }
    public decimal InTransitReceipts { get; set; }
    public decimal? PreviousClosing { get; set; }
}

public class BankStatementFormViewModel
{
    public Guid? Uid { get; set; }
    public long CashAccountId { get; set; }
    public string? Reference { get; set; }
    public DateOnly PeriodFrom { get; set; }
    public DateOnly StatementDate { get; set; }
    public decimal OpeningBalance { get; set; }
    public decimal ClosingBalance { get; set; }
    public string? Note { get; set; }
}

public class BankStatementLineFormViewModel
{
    public Guid? Uid { get; set; }
    public DateOnly LineDate { get; set; }
    public string? Description { get; set; }
    public string? Reference { get; set; }
    public decimal Amount { get; set; }
    public string Direction { get; set; } = "in";
}

public class BankReconciliationsIndexViewModel
{
    public long? CashAccountId { get; set; }
    public string? Status { get; set; }
    public List<CashAccountViewModel> Accounts { get; set; } = new();
    public List<BankStatementListViewModel> Statements { get; set; } = new();
}

public class OutstandingBankItemViewModel
{
    public Guid? JournalUid { get; set; }
    public string JournalNo { get; set; } = "";
    public DateOnly Date { get; set; }
    public string? Reference { get; set; }
    public string? Description { get; set; }
    public string CashAccountName { get; set; } = "";
    public string AccountCode { get; set; } = "";
    public decimal Amount { get; set; }
    public int AgeDays { get; set; }
    public DateOnly? ReconciledDate { get; set; }
}

public class OutstandingBankGroupViewModel
{
    public string AccountCode { get; set; } = "";
    public string AccountName { get; set; } = "";
    public List<OutstandingBankItemViewModel> Items { get; set; } = new();
    public decimal Total { get; set; }
    public decimal LedgerBalance { get; set; }
}

public class OutstandingBankViewModel
{
    public DateOnly AsOf { get; set; }
    public long? CashAccountId { get; set; }
    public List<OutstandingBankGroupViewModel> Groups { get; set; } = new();
    public List<OutstandingBankItemViewModel> BankItems { get; set; } = new();
    public List<CashAccountViewModel> Accounts { get; set; } = new();
}
