namespace ERP.Web.Models;

// ===================== Mata Uang & Kurs =====================

public class CurrencyViewModel
{
    public Guid Uid { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Symbol { get; set; }
    public bool IsFunctional { get; set; }
    public bool IsActive { get; set; } = true;
    public decimal? LatestRate { get; set; }
    public DateOnly? LatestRateDate { get; set; }
    public bool IsUsed { get; set; }
}

public class ExchangeRateViewModel
{
    public Guid Uid { get; set; }
    public string CurrencyCode { get; set; } = "";
    public DateOnly RateDate { get; set; }
    public decimal Rate { get; set; }
    public decimal? ChangePercent { get; set; }
    public string? Note { get; set; }
    public string? CreatedByName { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class ExchangeRateFilterViewModel
{
    public string? Currency { get; set; }
    public DateOnly? DateFrom { get; set; }
    public DateOnly? DateTo { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;

    public string ToQueryString()
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(Currency)) parts.Add($"currency={Uri.EscapeDataString(Currency)}");
        if (DateFrom != null) parts.Add($"dateFrom={DateFrom:yyyy-MM-dd}");
        if (DateTo != null) parts.Add($"dateTo={DateTo:yyyy-MM-dd}");
        return string.Join("&", parts);
    }
}

public class ExchangeRatesIndexViewModel
{
    public List<CurrencyViewModel> Currencies { get; set; } = new();
    public ExchangeRateFilterViewModel Filter { get; set; } = new();
    public PagedResult<ExchangeRateViewModel> Rates { get; set; } = new();

    /// <summary>Tanggal default form kurs harian.</summary>
    public DateOnly DailyDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    public IEnumerable<CurrencyViewModel> ForeignActive => Currencies.Where(c => !c.IsFunctional && c.IsActive);
}

public class RateLookupViewModel
{
    public string CurrencyCode { get; set; } = "";
    public decimal Rate { get; set; }
    public DateOnly? RateDate { get; set; }
}

// ===================== Revaluasi Kurs =====================

public class FxRevaluationLineViewModel
{
    public string SourceType { get; set; } = "";
    public string SourceLabel { get; set; } = "";
    public Guid? SourceUid { get; set; }
    public string DocNo { get; set; } = "";
    public string? PartyName { get; set; }
    public string CurrencyCode { get; set; } = "";
    public decimal ForeignBalance { get; set; }
    public decimal BookRate { get; set; }
    public decimal ClosingRate { get; set; }
    public decimal BookFunctional { get; set; }
    public decimal NewFunctional { get; set; }
    public decimal GainLoss { get; set; }

    /// <summary>Halaman sumber baris (faktur / rekening).</summary>
    public string SourceController => SourceType switch
    {
        "purchase_invoice" => "PurchaseInvoices",
        "sales_invoice" => "SalesInvoices",
        _ => "CashTransactions"
    };
}

public class FxClosingRateViewModel
{
    public string CurrencyCode { get; set; } = "";
    public decimal? Rate { get; set; }
    public DateOnly? RateDate { get; set; }
}

public class FxRevaluationPreviewViewModel
{
    public DateOnly RevalDate { get; set; }
    public DateOnly ReversalDate { get; set; }
    public List<FxClosingRateViewModel> Rates { get; set; } = new();
    public List<FxRevaluationLineViewModel> Lines { get; set; } = new();
    public decimal TotalGainLoss { get; set; }
    public List<string> MissingRates { get; set; } = new();
    public bool CanPost { get; set; }
    public string? BlockReason { get; set; }
}

public class FxRevaluationViewModel
{
    public Guid Uid { get; set; }
    public string RevalNo { get; set; } = "";
    public DateOnly RevalDate { get; set; }
    public DateOnly ReversalDate { get; set; }
    public decimal TotalGainLoss { get; set; }
    public string Status { get; set; } = "";
    public string? Note { get; set; }
    public string? VoidReason { get; set; }
    public DateOnly? VoidDate { get; set; }
    public Guid? JournalUid { get; set; }
    public string? JournalNo { get; set; }
    public Guid? ReversalJournalUid { get; set; }
    public string? ReversalJournalNo { get; set; }
    public int LineCount { get; set; }
    public string? Currencies { get; set; }
    public string? CreatedByName { get; set; }
    public DateTime? CreatedAt { get; set; }
    public List<FxRevaluationLineViewModel> Lines { get; set; } = new();
}

public class FxRevaluationsIndexViewModel
{
    public List<FxRevaluationViewModel> Revaluations { get; set; } = new();
    public FxRevaluationPreviewViewModel Preview { get; set; } = new();
}
