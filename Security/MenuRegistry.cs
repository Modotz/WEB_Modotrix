namespace ERP.Web.Security;

/// <summary>
/// Peta antara URL menu di tabel <c>menus</c> (warisan aplikasi PHP) dan halaman MVC di .NET.
/// Dipakai untuk link sidebar dan untuk menentukan izin menu yang dicek pada setiap action.
/// Saat modul baru dibuat di .NET, daftarkan di <see cref="Pages"/> agar muncul aktif di sidebar
/// dan terlindungi hak akses.
/// </summary>
public static class MenuRegistry
{
    public record Page(string MenuUrl, string Controller, string Action = "Index");

    /// <summary>Menu yang sudah punya halaman di .NET.</summary>
    public static readonly IReadOnlyList<Page> Pages = new List<Page>
    {
        new("dashboard", "Dashboard"),

        // Master Data & Pengaturan
        new("users", "Users"),
        new("menus", "Menus"),
        new("roles", "Roles"),
        new("access-role", "AccessRole"),
        new("msfield", "Msfields"),
        new("audit-log", "AuditLog"),
        new("settings", "Settings"),
        new("settings/wipe", "Settings", "Wipe"),
        new("schema-map", "SchemaMap"),

        // Inventory
        new("products", "Products"),
        new("goods-receipts", "GoodsReceipts"),
        new("product-categories", "ProductCategories"),
        new("product-attributes", "ProductAttributes"),
        new("units", "Units"),
        new("brands", "Brands"),
        new("warehouses", "Warehouse"),

        // Pembelian
        new("goods-rfqs", "GoodsRfq"),
        new("goods-purchase-orders", "GoodsPurchaseOrders"),
        new("receiving-queue", "GoodsPurchaseOrders", "ReceivingQueue"),
        new("suppliers", "Suppliers"),

        // Penjualan
        new("customers", "Customers"),
        new("customer-groups", "CustomerGroups"),
        new("sales-forces", "SalesForces"),
        new("price-lists", "PriceLists"),
        new("shipping-areas", "ShippingAreas"),

        // Produksi
        new("work-centers", "WorkCenters"),

        // Akuntansi
        new("chart-of-accounts", "ChartOfAccounts"),
        new("accounting-periods", "AccountingPeriods"),
        new("exchange-rates", "ExchangeRates"),
        new("tax-rates", "TaxRates"),
        new("pph23-rates", "Pph23Rates"),
        new("pph23", "Pph23"),

        // Rekonsiliasi bank (Level 12)
        new("bank-reconciliations", "BankReconciliations"),
        new("reports/bank-outstanding", "BankOutstanding"),

        // Adjusting entries (Level 13)
        new("reports/accruals", "Accruals"),
        new("deferral-schedules", "DeferralSchedules"),
        new("deferral-recognition", "DeferralRecognition"),
        new("doubtful-receivables", "DoubtfulReceivables"),

        // Anggaran & Biaya (Level 11)
        new("cost-centers", "CostCenters"),
        new("projects", "Projects"),
        new("budgets", "Budgets"),
        new("cost-allocations", "CostAllocations"),
        new("reports/cost-center-pnl", "CostCenterPnl"),
        new("reports/project-profitability", "ProjectProfitability"),
        new("reports/budget-vs-actual", "BudgetVsActual"),
        new("fx-revaluations", "FxRevaluations"),
        new("journals", "Journals"),
        new("opening-balances", "OpeningBalances"),
        new("general-ledger", "GeneralLedger"),
        new("trial-balance", "TrialBalance"),
        new("income-statement", "IncomeStatement"),
        new("balance-sheet", "BalanceSheet"),
        new("cash-flow", "CashFlow"),
        new("reports/equity", "EquityChanges"),
        new("closing-book", "ClosingBook"),
        new("cash-accounts", "CashAccounts"),
        new("cash-transactions", "CashTransactions"),
        new("petty-cash", "PettyCash"),
        new("purchase-invoices", "PurchaseInvoices"),
        new("supplier-payments", "SupplierPayments"),
        new("ap-aging", "ApAging"),
        new("reports/ap-card", "ApCard"),
        new("sales-invoices", "SalesInvoices"),
        new("customer-payments", "CustomerPayments"),
        new("ar-aging", "ArAging"),
        new("reports/ar-card", "ArCard"),
        new("reports/vat", "Vat"),

        // Persetujuan
        new("approvals", "Approvals"),
        new("approval-workflows", "ApprovalWorkflows"),

        // Manajemen Aset
        new("asset", "Assets"),
        new("asset-categories", "AssetCategories"),
        new("asset-assignments", "AssetAssignments"),
        new("asset-depreciation", "AssetDepreciation"),
        new("asset-maintenance", "AssetMaintenance"),
        new("reports/asset-book-value", "AssetBookValue"),

        // Pembelian Aset (PR Aset di bawah Purchase Request; RFQ/PO/Retur Aset di bawah Pembelian)
        new("asset-purchase-requests", "AssetPurchaseRequests"),
        new("asset-rfqs", "AssetRfqs"),
        new("asset-purchase-orders", "AssetPurchaseOrders"),
        new("asset-purchase-returns", "AssetPurchaseReturns"),
    };

    /// <summary>Halaman turunan yang izinnya mengikuti menu induknya.</summary>
    private static readonly Dictionary<string, string> SubPageControllers = new(StringComparer.OrdinalIgnoreCase)
    {
        ["ProductUnits"] = "products",
        ["ProductVariants"] = "products",
        ["AttributeVariants"] = "product-attributes",
    };

    /// <summary>
    /// Halaman turunan yang SEMUA aksi tulisnya dihitung sebagai "update" pada menu induknya
    /// (mengubah isi dokumen/master induk), sesuai rute aplikasi PHP.
    /// </summary>
    private static readonly HashSet<string> WritesAsUpdateControllers = new(StringComparer.OrdinalIgnoreCase)
    {
        "AttributeVariants", // PHP: product-attributes/variants/* = product-attributes,update
        "ProductUnits",      // satuan & konversi = bagian dari edit produk
    };

    /// <summary>Pengecualian per action, sesuai filter "permission:menu,aksi" di rute PHP.</summary>
    private static readonly Dictionary<(string, string), string> ActionOverrides = new()
    {
        [("goodsrfq", "converttopo")] = "update",            // PHP: goods-rfqs/convert = goods-rfqs,update
        [("pricelists", "additem")] = "update",               // PHP: price-lists/item = price-lists,update
        [("pricelists", "addallitems")] = "update",
        [("pricelists", "updateitem")] = "update",
        [("pricelists", "deleteitem")] = "update",
        [("pricelists", "deleteitems")] = "update",
        [("customers", "deleteshippingaddress")] = "update",  // PHP: customers/address-delete = customers,update
        [("auditlog", "clear")] = "delete",                  // PHP: audit-log/clear = audit-log,delete
        [("settings", "prunelogs")] = "update",               // PHP: settings/prune-logs = settings,update
        [("settings", "pruneserialmovements")] = "update",
        [("settings", "backup")] = "update",                  // PHP: settings/export-db = settings,update
        [("settings", "backupdownload")] = "update",
        [("settings", "wipe")] = "delete",                    // PHP: settings/wipe = settings,delete
        [("settings", "wiperun")] = "delete",
        [("accountingperiods", "generate")] = "create",      // PHP: accounting-periods/generate = accounting-periods,create
        [("accountingperiods", "close")] = "update",         // PHP: accounting-periods/toggle = accounting-periods,update
        [("accountingperiods", "reopen")] = "update",
        [("journals", "post")] = "update",                   // PHP: journals/post = journals,update
        [("journals", "void")] = "update",                   // pengganti unpost PHP (jurnal pembalik)
        [("journals", "revise")] = "update",
        [("journals", "reverseaccrual")] = "update",
        [("closingbook", "close")] = "create",                // PHP: closing-book/post = closing-book,create
        [("closingbook", "cancel")] = "delete",               // PHP: closing-book/reverse = closing-book,delete
        [("cashtransactions", "post")] = "update",           // PHP: cash-transactions/post = cash-transactions,update
        [("cashtransactions", "void")] = "update",           // pengganti unpost PHP (jurnal pembalik)
        [("pettycash", "topup")] = "create",                 // PHP: petty-cash/topup = petty-cash,create
        [("pettycash", "expense")] = "create",               // PHP: petty-cash/expense = petty-cash,create
        [("pettycash", "post")] = "update",
        [("pettycash", "void")] = "update",
        [("purchaseinvoices", "post")] = "update",
        [("purchaseinvoices", "void")] = "update",
        [("salesinvoices", "post")] = "update",
        [("salesinvoices", "void")] = "update",
        [("supplierpayments", "post")] = "update",
        [("supplierpayments", "void")] = "update",
        [("customerpayments", "post")] = "update",
        [("customerpayments", "void")] = "update",
        [("vat", "close")] = "create",                       // tutup masa PPN (jurnal netting)
        [("fxrevaluations", "void")] = "delete",             // batal revaluasi kurs (jurnal pembalik)
        [("pph23", "voiddeposit")] = "delete",               // batal setoran PPh 23 (jurnal pembalik)
        [("bankreconciliations", "match")] = "update",        // cocokkan baris rekening koran (jurnal clearing)
        [("bankreconciliations", "unmatch")] = "update",      // batal cocok (jurnal pembalik clearing)
        [("bankreconciliations", "complete")] = "update",
        [("bankreconciliations", "deleteline")] = "update",    // hapus baris rekening koran = ubah rekening koran
        [("deferralschedules", "reschedule")] = "update",     // ubah jadwal prepaid/deferred (catch-up)
        [("deferralrecognition", "run")] = "create",          // proses rekognisi (jurnal)
        [("deferralrecognition", "reverselast")] = "delete",  // batal rekognisi terakhir (jurnal pembalik)
        [("doubtfulreceivables", "estimate")] = "create",     // estimasi cadangan (jurnal)
        [("doubtfulreceivables", "reverseestimate")] = "delete",
        [("doubtfulreceivables", "writeoff")] = "create",     // write-off piutang (jurnal)
        [("doubtfulreceivables", "voidwriteoff")] = "delete", // batal write-off (jurnal pembalik)
        [("costallocations", "post")] = "create",            // posting alokasi biaya (jurnal baru)
        [("costallocations", "void")] = "delete",            // batal alokasi (jurnal pembalik)
        [("costallocations", "preview")] = "view",
        [("approvals", "approve")] = "view",                 // hak memutuskan dicek mesin persetujuan (snapshot approver)
        [("approvals", "reject")] = "view",
        [("approvals", "withdraw")] = "view",
        [("approvals", "submit")] = "view",                  // API mengecek izin ubah menu modul dokumen
        [("assets", "changeschedule")] = "update",           // ubah umur/residu/metode (prospektif)
        [("assets", "disposeasset")] = "update",             // pelepasan aset (jurnal pelepasan)
        [("assets", "recordusage")] = "update",              // catat unit produksi (units of production)
        [("assets", "adjustvalue")] = "update",              // tambah biaya / revaluasi / impairment (+ pembalikan)
        [("assets", "voidadjustment")] = "update",           // batal penyesuaian nilai terakhir (jurnal pembalik)
        [("assetassignments", "returnasset")] = "update",    // PHP: asset-assignments/return = update
        [("assetdepreciation", "run")] = "create",           // proses penyusutan (jurnal)
        [("assetdepreciation", "reverselast")] = "delete",   // batal proses terakhir (jurnal pembalik)
        [("assetpurchaserequests", "submit")] = "update",       // PHP: asset-purchase-requests/submit = update
        [("assetpurchaserequests", "converttopo")] = "update",  // PHP: asset-purchase-requests/convert = update
        [("assetrfqs", "converttopo")] = "update",              // PHP: asset-rfqs/convert = update
        [("assetpurchaseorders", "submit")] = "update",         // PHP: asset-purchase-orders/submit = update
        [("assetpurchaseorders", "requestcancel")] = "update",  // PHP: asset-purchase-orders/request-cancel = update
        [("assetpurchaseorders", "receive")] = "update",        // PHP: asset-purchase-orders/receive = update
    };

    /// <summary>
    /// Action yang izinnya diambil dari menu LAIN daripada menu sidebar-nya.
    /// Contoh: halaman Reset Data tampil sebagai menu "settings/wipe", tetapi di PHP dilindungi "settings,delete".
    /// </summary>
    private static readonly Dictionary<(string, string), string> PermissionMenuOverrides = new()
    {
        [("settings", "wipe")] = "settings",
        [("settings", "wiperun")] = "settings",
    };

    /// <summary>Controller yang cukup mensyaratkan login (tanpa cek menu).</summary>
    private static readonly HashSet<string> AuthenticatedOnly = new(StringComparer.OrdinalIgnoreCase)
    {
        "Account",
        "Dashboard",
        "Language",
        "Lookup", // kurs & tarif PPN untuk form faktur/pembayaran/kas (data referensi, bukan rahasia)
    };

    private static readonly Dictionary<string, Page> ByMenuUrl =
        Pages.ToDictionary(p => p.MenuUrl, StringComparer.OrdinalIgnoreCase);

    private static readonly Dictionary<(string, string), string> ByControllerAction =
        Pages.Where(p => p.Action != "Index")
            .ToDictionary(p => (p.Controller.ToLowerInvariant(), p.Action.ToLowerInvariant()), p => p.MenuUrl);

    private static readonly Dictionary<string, string> ByController =
        Pages.Where(p => p.Action == "Index")
            .GroupBy(p => p.Controller, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().MenuUrl, StringComparer.OrdinalIgnoreCase);

    public static bool TryGetPage(string? menuUrl, out Page page)
    {
        page = null!;
        return !string.IsNullOrEmpty(menuUrl) && ByMenuUrl.TryGetValue(menuUrl, out page!);
    }

    public static bool IsAuthenticatedOnly(string controller) => AuthenticatedOnly.Contains(controller);

    /// <summary>URL menu yang menjadi dasar izin sebuah action, atau null bila belum terdaftar.</summary>
    public static string? ResolveMenuUrl(string controller, string action)
    {
        return PermissionMenuOverrides.TryGetValue((controller.ToLowerInvariant(), action.ToLowerInvariant()), out var url)
            ? url
            : ResolvePageMenuUrl(controller, action);
    }

    /// <summary>URL menu sidebar yang mewakili halaman (untuk menandai menu aktif).</summary>
    public static string? ResolvePageMenuUrl(string controller, string action)
    {
        if (ByControllerAction.TryGetValue((controller.ToLowerInvariant(), action.ToLowerInvariant()), out var url))
            return url;

        if (ByController.TryGetValue(controller, out url))
            return url;

        return SubPageControllers.TryGetValue(controller, out url) ? url : null;
    }

    private static readonly string[] DeletePrefixes = { "Delete", "Remove" };
    private static readonly string[] CreatePrefixes = { "Create", "Add", "Copy", "ConvertTo", "Store" };
    private static readonly string[] UpdatePrefixes =
        { "Edit", "Update", "Save", "Reorder", "Approve", "Activate", "Cancel", "Send", "Quote", "Sync" };

    /// <summary>
    /// Izin yang dibutuhkan sebuah action berdasarkan namanya (cukup salah satu):
    /// Delete* = hapus, Create*/Add*/Copy* = tambah, Edit*/Update*/Save*/Approve* dst = ubah,
    /// Upload* = tambah atau ubah (dipakai di form tambah maupun edit), selain itu = lihat.
    /// </summary>
    public static string[] RequiredActions(string controller, string action)
    {
        if (ActionOverrides.TryGetValue((controller.ToLowerInvariant(), action.ToLowerInvariant()), out var overridden))
            return new[] { overridden };

        var byName = RequiredActions(action);

        return WritesAsUpdateControllers.Contains(controller) && byName[0] != "view"
            ? new[] { "update" }
            : byName;
    }

    private static string[] RequiredActions(string action)
    {
        bool Starts(string[] prefixes) => prefixes.Any(p => action.StartsWith(p, StringComparison.OrdinalIgnoreCase));

        if (Starts(DeletePrefixes)) return new[] { "delete" };
        if (Starts(CreatePrefixes)) return new[] { "create" };
        if (action.StartsWith("Upload", StringComparison.OrdinalIgnoreCase)) return new[] { "create", "update" };
        if (Starts(UpdatePrefixes)) return new[] { "update" };
        return new[] { "view" };
    }
}
