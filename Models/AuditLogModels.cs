namespace ERP.Web.Models;

public class AuditLogViewModel
{
    public long Id { get; set; }

    public long? UserId { get; set; }

    public string? Username { get; set; }

    public string Action { get; set; } = string.Empty;

    public string? Module { get; set; }

    public string? RecordId { get; set; }

    public string? Description { get; set; }

    public string? OldData { get; set; }

    public string? NewData { get; set; }

    public string? IpAddress { get; set; }

    public string? UserAgent { get; set; }

    public DateTime? CreatedAt { get; set; }

    /// <summary>Label Indonesia + kelas badge Bootstrap (padanan audit_action_label() di PHP).</summary>
    public static (string Label, string Badge) ActionLabel(string action) => action switch
    {
        "login" => ("Login", "text-bg-success"),
        "logout" => ("Logout", "text-bg-secondary"),
        "login_failed" => ("Login Gagal", "text-bg-danger"),
        "create" => ("Tambah", "text-bg-primary"),
        "update" => ("Ubah", "text-bg-warning"),
        "delete" => ("Hapus", "text-bg-danger"),
        "export" => ("Ekspor", "text-bg-info"),
        "print" => ("Cetak", "text-bg-info"),
        "import" => ("Impor", "text-bg-info"),
        "reset_request" => ("Minta Reset", "text-bg-secondary"),
        _ => (action.Length > 0 ? char.ToUpperInvariant(action[0]) + action[1..] : action, "text-bg-light")
    };
}

public class AuditLogFilterViewModel
{
    public string? Q { get; set; }

    public string? Username { get; set; }

    public string? Action { get; set; }

    public string? Module { get; set; }

    public DateOnly? DateFrom { get; set; }

    public DateOnly? DateTo { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 25;

    /// <summary>Query string filter (tanpa page) untuk link ekspor, cetak dan paging.</summary>
    public string ToQueryString(int? page = null)
    {
        var parts = new List<string>();

        void Add(string key, string? value)
        {
            if (!string.IsNullOrWhiteSpace(value))
                parts.Add($"{key}={Uri.EscapeDataString(value)}");
        }

        Add("q", Q);
        Add("username", Username);
        Add("action", Action);
        Add("module", Module);
        Add("dateFrom", DateFrom?.ToString("yyyy-MM-dd"));
        Add("dateTo", DateTo?.ToString("yyyy-MM-dd"));
        if (PageSize != 25) Add("pageSize", PageSize.ToString());
        if (page is > 1) Add("page", page.ToString());

        return string.Join("&", parts);
    }
}

public class AuditLogFilterOptionsViewModel
{
    public List<string> Actions { get; set; } = new();

    public List<string> Modules { get; set; } = new();
}

public class AuditLogIndexViewModel
{
    public PagedResult<AuditLogViewModel> Logs { get; set; } = new();

    public AuditLogFilterViewModel Filter { get; set; } = new();

    public AuditLogFilterOptionsViewModel Options { get; set; } = new();
}
