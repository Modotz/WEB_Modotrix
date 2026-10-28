namespace ERP.Web.Models;

public class AppSettingViewModel
{
    public long Id { get; set; }

    public string Key { get; set; } = string.Empty;

    public string? Value { get; set; }

    public string? Type { get; set; }

    public string? Label { get; set; }

    public string? Group { get; set; }
}

public class OptionViewModel
{
    public long Id { get; set; }

    public string Label { get; set; } = string.Empty;
}

public class SettingsPageViewModel
{
    public List<AppSettingViewModel> Settings { get; set; } = new();

    public List<OptionViewModel> CoaAccounts { get; set; } = new();

    public List<OptionViewModel> Warehouses { get; set; } = new();

    public string DatabaseName { get; set; } = string.Empty;

    public string? BackupFolder { get; set; }

    public long AuditCount { get; set; }

    public DateTime? AuditOldest { get; set; }

    public long SerialMovementCount { get; set; }

    public DateTime? SerialMovementOldest { get; set; }

    /// <summary>Judul tampilan untuk kode grup di tabel app_settings.</summary>
    public static string GroupTitle(string? group) => group switch
    {
        null or "" or "general" => "Umum",
        "attendance" => "Absensi",
        "payroll" => "Payroll",
        "inventory" => "Akun Inventory",
        "issue" => "Akun Pengeluaran Barang",
        "penjualan" => "Penjualan",
        _ => group
    };

    public static string GroupIcon(string? group) => group switch
    {
        null or "" or "general" => "bi-gear",
        "attendance" => "bi-fingerprint",
        "payroll" => "bi-cash-stack",
        "inventory" or "issue" => "bi-journal-text",
        "penjualan" => "bi-percent",
        "Printer Struk" => "bi-printer",
        "Toko Online" => "bi-shop",
        "Faktur Otomatis" => "bi-receipt",
        _ => "bi-sliders"
    };

    /// <summary>Pilihan tetap untuk setting bertipe select selain gudang.</summary>
    public static IReadOnlyList<(string Value, string Label)> FixedOptions(string key) => key switch
    {
        "receipt_print_mode" => new[]
        {
            ("browser", "Browser (dialog print biasa)"),
            ("server", "Server → COM port (printer di PC server)"),
            ("webserial", "Web Serial (Chrome/Edge → printer di PC user)")
        },
        _ => Array.Empty<(string, string)>()
    };
}

public class BrandingViewModel
{
    public string AppName { get; set; } = "ERPLITE";

    public string? ShortName { get; set; }

    public string? Logo { get; set; }

    public string? Footer { get; set; }
}

public class WipeGroupViewModel
{
    public string Group { get; set; } = string.Empty;

    public List<string> Tables { get; set; } = new();

    public long Rows { get; set; }
}

public class SchemaMapViewModel
{
    public string Database { get; set; } = string.Empty;

    public List<SchemaTableViewModel> Tables { get; set; } = new();

    public List<SchemaMenuMapViewModel> MenuMap { get; set; } = new();
}

public class SchemaTableViewModel
{
    public string Name { get; set; } = string.Empty;

    public long Rows { get; set; }

    public int Columns { get; set; }

    public int FkOut { get; set; }

    public int FkIn { get; set; }
}

public class SchemaMenuMapViewModel
{
    public string Group { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string? Url { get; set; }

    public string? Table { get; set; }
}

public class SchemaTableDetailViewModel
{
    public string Name { get; set; } = string.Empty;

    public long RowCount { get; set; }

    public List<SchemaColumnViewModel> Columns { get; set; } = new();

    public List<SchemaRelationViewModel> FkOut { get; set; } = new();

    public List<SchemaRelationViewModel> FkIn { get; set; } = new();

    public List<SchemaInferredViewModel> Inferred { get; set; } = new();

    public List<string> AllTables { get; set; } = new();
}

public class SchemaColumnViewModel
{
    public string Name { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;

    public bool IsNullable { get; set; }

    public bool IsIdentity { get; set; }

    public bool IsPrimaryKey { get; set; }

    public bool IsUnique { get; set; }

    public bool IsIndexed { get; set; }

    public string? DefaultValue { get; set; }
}

public class SchemaRelationViewModel
{
    public string ColumnName { get; set; } = string.Empty;

    public string OtherTable { get; set; } = string.Empty;

    public string OtherColumn { get; set; } = string.Empty;

    public string OnDelete { get; set; } = string.Empty;
}

public class SchemaInferredViewModel
{
    public string Column { get; set; } = string.Empty;

    public string Guess { get; set; } = string.Empty;
}
