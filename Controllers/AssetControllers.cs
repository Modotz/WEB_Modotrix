using ClosedXML.Excel;
using ERP.Web.Models;
using ERP.Web.Services;
using Microsoft.AspNetCore.Mvc;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ERP.Web.Controllers;

/// <summary>Helper bersama halaman daftar aset: ukuran halaman standar, ambil semua halaman untuk export, subjudul filter.</summary>
internal static class AssetListPaging
{
    public static readonly int[] PageSizes = { 10, 25, 50, 100 };

    /// <summary>Batas pageSize API aset (Math.Clamp 10..100).</summary>
    public const int MaxPageSize = 100;

    public static int Size(int pageSize, int fallback = 25) => PageSizes.Contains(pageSize) ? pageSize : fallback;

    /// <summary>Ambil seluruh baris dari endpoint berhalaman (API membatasi 100/halaman).</summary>
    public static async Task<List<T>> AllAsync<T>(Func<int, Task<PagedResult<T>>> fetch)
    {
        var all = new List<T>();
        for (var page = 1; page <= 1000; page++)
        {
            var result = await fetch(page);
            all.AddRange(result.Data);
            if (result.Data.Count == 0 || page >= result.TotalPage)
                break;
        }

        return all;
    }

    /// <summary>"Label: nilai · Label: nilai" untuk filter yang terisi (null bila tidak ada).</summary>
    public static string? Subtitle(Translator t, params (string Label, string? Value)[] parts)
    {
        var text = string.Join(" · ", parts.Where(p => !string.IsNullOrWhiteSpace(p.Value)).Select(p => $"{p.Label}: {p.Value}"));
        return text.Length == 0 ? null : text;
    }
}

/// <summary>Manajemen Aset &gt; Kategori Aset.</summary>
public class AssetCategoriesController : Controller
{
    private readonly AssetApiClient _api;
    private readonly Translator _t;

    public AssetCategoriesController(AssetApiClient api, Translator t)
    {
        _api = api;
        _t = t;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? q, int page = 1, int pageSize = 25) => View(new AssetCategoriesIndexViewModel
    {
        Q = q,
        Page = Math.Max(page, 1),
        Categories = await _api.GetCategoriesAsync(q, Math.Max(page, 1), AssetListPaging.Size(pageSize)),
        Options = await _api.GetCategoryFormOptionsAsync()
    });

    [HttpGet]
    public async Task<IActionResult> ExportExcel(string? q) => this.ExcelFile(await BuildExportAsync(q));

    [HttpGet]
    public async Task<IActionResult> ExportPdf(string? q) => await this.PdfFile(await BuildExportAsync(q));

    [HttpGet]
    public async Task<IActionResult> PrintList(string? q) => this.PrintView(await BuildExportAsync(q));

    private async Task<ListExport> BuildExportAsync(string? q)
    {
        var rows = await AssetListPaging.AllAsync(p => _api.GetCategoriesAsync(q, p, AssetListPaging.MaxPageSize));
        var inv = System.Globalization.CultureInfo.InvariantCulture;

        return new ListExport
        {
            Title = _t["Kategori Aset"],
            Subtitle = AssetListPaging.Subtitle(_t, (_t["Cari"], q)),
            FileName = "kategori_aset",
            Headers = new[] { "Kode", "Nama", "Metode", "Umur (bln)", "Residu %", "Akun aset tetap", "Akun akumulasi penyusutan", "Akun beban penyusutan", "Jumlah Aset", "Status" }
                .Select(h => _t[h]).ToArray(),
            Rows = rows.Select(c => new object?[]
            {
                c.Code, c.Name,
                _t[AssetLabels.Method(c.DepreciationMethod)] + (c.DepreciationMethod == "declining_balance" ? " × " + c.DecliningFactor?.ToString("0.##", inv) : ""),
                c.UsefulLifeMonths, c.SalvagePercent, c.AssetAccountLabel, c.AccumAccountLabel, c.ExpenseAccountLabel, c.AssetCount,
                c.IsActive ? _t["Aktif"] : _t["Nonaktif"]
            }).ToList()
        };
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AssetCategoryViewModel model)
    {
        model.Uid = Guid.Empty;
        return Done(await _api.SaveCategoryAsync(model));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(AssetCategoryViewModel model) =>
        Done(model.Uid == Guid.Empty ? new ApiResult(false, "Kategori aset tidak ditemukan.") : await _api.SaveCategoryAsync(model));

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid uid) => Done(await _api.DeleteCategoryAsync(uid));

    private IActionResult Done(ApiResult result)
    {
        TempData[result.Success ? "Success" : "Error"] = result.Message;
        return RedirectToAction(nameof(Index));
    }
}

/// <summary>Manajemen Aset &gt; Daftar Aset (draft, aktivasi, jadwal, pelepasan).</summary>
public class AssetsController : Controller
{
    private readonly AssetApiClient _api;
    private readonly Translator _t;

    public AssetsController(AssetApiClient api, Translator t)
    {
        _api = api;
        _t = t;
    }

    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] AssetFilterViewModel filter)
    {
        filter.Page = Math.Max(filter.Page, 1);
        filter.PageSize = AssetListPaging.Size(filter.PageSize);

        return View(new AssetsIndexViewModel
        {
            Filter = filter,
            Assets = await _api.GetAssetsAsync(filter),
            Categories = (await _api.GetAssetFormOptionsAsync()).Categories
        });
    }

    [HttpGet]
    public async Task<IActionResult> ExportExcel([FromQuery] AssetFilterViewModel filter) => this.ExcelFile(await BuildExportAsync(filter));

    [HttpGet]
    public async Task<IActionResult> ExportPdf([FromQuery] AssetFilterViewModel filter) => await this.PdfFile(await BuildExportAsync(filter));

    [HttpGet]
    public async Task<IActionResult> PrintList([FromQuery] AssetFilterViewModel filter) => this.PrintView(await BuildExportAsync(filter));

    private async Task<ListExport> BuildExportAsync(AssetFilterViewModel filter)
    {
        var rows = await AssetListPaging.AllAsync(p => _api.GetAssetsAsync(new AssetFilterViewModel
        {
            Q = filter.Q,
            Status = filter.Status,
            CategoryUid = filter.CategoryUid,
            Source = filter.Source,
            Page = p,
            PageSize = AssetListPaging.MaxPageSize
        }));

        string? category = null;
        if (filter.CategoryUid != null)
            category = (await _api.GetAssetFormOptionsAsync()).Categories.FirstOrDefault(c => c.Uid == filter.CategoryUid)?.Name;

        static bool HasBookValue(AssetListItemViewModel a) => a.Status is not ("draft" or "void");

        return new ListExport
        {
            Title = _t["Daftar Aset"],
            Subtitle = AssetListPaging.Subtitle(_t,
                (_t["Cari"], filter.Q),
                (_t["Kategori"], category),
                (_t["Status"], string.IsNullOrEmpty(filter.Status) ? null : _t[AssetLabels.Status(filter.Status)]),
                (_t["Sumber"], string.IsNullOrEmpty(filter.Source) ? null : _t[AssetLabels.Source(filter.Source)])),
            FileName = "daftar_aset",
            Headers = new[] { "No. Aset", "Nama", "Serial", "Kategori", "Sumber", "Tgl Perolehan", "Harga Perolehan", "Nilai Buku", "Pemegang", "Lokasi", "Status" }
                .Select(h => _t[h]).ToArray(),
            Rows = rows.Select(a => new object?[]
            {
                a.AssetNo, a.Name, a.SerialNo, a.CategoryName, _t[AssetLabels.Source(a.AcquisitionSource)], a.AcquisitionDate,
                a.AcquisitionCost, HasBookValue(a) ? a.BookValue : null, a.HolderName, a.Location, _t[AssetLabels.Status(a.Status)]
            }).ToList(),
            Footer = new object?[]
            {
                _t["Total"], null, null, null, null, null, rows.Sum(a => a.AcquisitionCost), rows.Where(HasBookValue).Sum(a => a.BookValue), null, null, null
            }
        };
    }

    [HttpGet]
    public async Task<IActionResult> Details(Guid uid)
    {
        var asset = await _api.GetAssetAsync(uid);
        if (asset == null)
            return NotFoundRedirect();

        return View(new AssetDetailsPageViewModel
        {
            Asset = asset,
            CashAccounts = asset.CanDispose || asset.CanAdjust ? (await _api.GetAssetFormOptionsAsync()).CashAccounts : new()
        });
    }

    [HttpGet]
    public async Task<IActionResult> Create() => await FormView(new AssetFormViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AssetFormViewModel form, string? submitAction)
    {
        form.Uid = null;
        return await Submit(form, submitAction == "activate");
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid uid)
    {
        var a = await _api.GetAssetAsync(uid);
        if (a == null)
            return NotFoundRedirect();

        if (!a.CanEdit)
        {
            TempData["Error"] = "Aset yang sudah dilepas/dibatalkan tidak dapat diubah.";
            return RedirectToAction(nameof(Details), new { uid });
        }

        return await FormView(new AssetFormViewModel
        {
            Uid = a.Uid,
            AssetNo = a.AssetNo,
            Status = a.Status,
            Name = a.Name,
            AssetCategoryId = a.AssetCategoryId,
            AcquisitionSource = a.AcquisitionSource ?? "cash",
            AcquisitionDate = a.AcquisitionDate ?? DateOnly.FromDateTime(DateTime.Today),
            AcquisitionCost = a.AcquisitionCost,
            SupplierId = a.SupplierId,
            CashAccountId = a.CashAccountId,
            ProductId = a.ProductId,
            SourceWarehouseId = a.SourceWarehouseId,
            SerialNo = a.SerialNo,
            CounterAccountId = a.CounterAccountId,
            OpeningDate = a.OpeningDate,
            OpeningAccumulated = a.OpeningAccumulated,
            DepreciationMethod = a.DepreciationMethod,
            DecliningFactor = a.DecliningFactor,
            UsefulLifeMonths = a.UsefulLifeMonths,
            SalvageValue = a.SalvageValue,
            DepreciationStart = a.DepreciationStart,
            TotalEstimatedUnits = a.TotalEstimatedUnits,
            Location = a.Location,
            WarrantyUntil = a.WarrantyUntil,
            Note = a.Note
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(AssetFormViewModel form, string? submitAction)
    {
        if (form.Uid == null)
            return NotFoundRedirect();

        return await Submit(form, submitAction == "activate");
    }

    /// <summary>Serial in_stock untuk sumber stok (AJAX, uid produk &amp; gudang).</summary>
    [HttpGet]
    public async Task<IActionResult> Serials(Guid productUid, Guid warehouseUid, string? q) =>
        Json(await _api.GetSerialsAsync(productUid, warehouseUid, q));

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Activate(Guid uid) => Back(await _api.ActivateAssetAsync(uid), uid);

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeSchedule(Guid uid, int usefulLifeMonths, decimal salvageValue, string? depreciationMethod,
        decimal? decliningFactor, string? reason, decimal? totalEstimatedUnits) =>
        Back(await _api.ChangeScheduleAsync(uid, usefulLifeMonths, salvageValue, depreciationMethod, decliningFactor, reason, totalEstimatedUnits), uid, "schedule");

    // ---------- T3 lanjutan (Level 13) ----------

    /// <summary>Catat unit produksi satu bulan (units of production).</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RecordUsage(Guid uid, string? period, decimal units, string? note) =>
        Back(await _api.RecordUsageAsync(uid, period, units, note), uid, "usage");

    /// <summary>Tambah biaya / revaluasi / impairment / pembalikan impairment — <paramref name="kind"/> dari tombol modal.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AdjustValue(Guid uid, string kind, DateOnly? date, decimal amount, string? note, string? direction,
        long? cashAccountId, Guid? reversesUid)
    {
        if (kind is not ("capex" or "revaluation" or "impairment" or "impairment-reversal"))
            return NotFoundRedirect();

        return Back(await _api.AdjustAssetAsync(uid, kind, date, amount, note, direction, cashAccountId, reversesUid), uid, "adjustments");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> VoidAdjustment(Guid uid, Guid adjustmentUid, string? reason) =>
        Back(await _api.VoidAdjustmentAsync(uid, adjustmentUid, reason), uid, "adjustments");

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DisposeAsset(Guid uid, DateOnly? date, string? type, decimal proceeds, long? cashAccountId, string? note) =>
        Back(await _api.DisposeAssetAsync(uid, date, type, proceeds, cashAccountId, note), uid);

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelDisposal(Guid uid, DateOnly? date, string? reason) =>
        Back(await _api.CancelDisposalAsync(uid, date, reason), uid);

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid uid, DateOnly? date, string? reason)
    {
        var result = await _api.DeleteAssetAsync(uid, date, reason);
        TempData[result.Success ? "Success" : "Error"] = result.Message;

        // Draft terhapus → kembali ke daftar; perolehan dibatalkan → aset tetap tampil (status Dibatalkan).
        return result.Success && result.Uid == null ? RedirectToAction(nameof(Index)) : RedirectToAction(nameof(Details), new { uid });
    }

    // ------------------------------------------------------------------

    private async Task<IActionResult> FormView(AssetFormViewModel form)
    {
        form.Options = await _api.GetAssetFormOptionsAsync();
        return View("Form", form);
    }

    private async Task<IActionResult> Submit(AssetFormViewModel form, bool activate)
    {
        var result = await _api.SaveAssetAsync(form, activate);

        if (!result.Success)
        {
            TempData["Error"] = result.Message;
            return await FormView(form);
        }

        TempData["Success"] = result.Message;
        var uid = result.Uid ?? form.Uid;
        return uid == null ? RedirectToAction(nameof(Index)) : RedirectToAction(nameof(Details), new { uid });
    }

    private IActionResult Back(ApiResult result, Guid uid, string? tab = null)
    {
        TempData[result.Success ? "Success" : "Error"] = result.Message;
        var url = Url.Action(nameof(Details), new { uid });
        return Redirect(tab == null ? url! : $"{url}#{tab}");
    }

    private IActionResult NotFoundRedirect()
    {
        TempData["Error"] = "Aset tidak ditemukan.";
        return RedirectToAction(nameof(Index));
    }
}

/// <summary>Manajemen Aset &gt; Serah Terima.</summary>
public class AssetAssignmentsController : Controller
{
    private readonly AssetApiClient _api;
    private readonly Translator _t;

    public AssetAssignmentsController(AssetApiClient api, Translator t)
    {
        _api = api;
        _t = t;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? q, string? status, Guid? assetUid, int page = 1, int pageSize = 25) => View(new AssetAssignmentsIndexViewModel
    {
        Q = q,
        Status = status,
        Page = Math.Max(page, 1),
        Rows = await _api.GetAssignmentsAsync(q, status, Math.Max(page, 1), AssetListPaging.Size(pageSize)),
        Assets = await _api.GetAssignableAssetsAsync(),
        PreselectAssetUid = assetUid
    });

    [HttpGet]
    public async Task<IActionResult> ExportExcel(string? q, string? status) => this.ExcelFile(await BuildExportAsync(q, status));

    [HttpGet]
    public async Task<IActionResult> ExportPdf(string? q, string? status) => await this.PdfFile(await BuildExportAsync(q, status));

    [HttpGet]
    public async Task<IActionResult> PrintList(string? q, string? status) => this.PrintView(await BuildExportAsync(q, status));

    private async Task<ListExport> BuildExportAsync(string? q, string? status)
    {
        var rows = await AssetListPaging.AllAsync(p => _api.GetAssignmentsAsync(q, status, p, AssetListPaging.MaxPageSize));
        string StatusLabel(string? s) => s == "active" ? _t["Aktif"] : _t["Kembali"];

        return new ListExport
        {
            Title = _t["Serah Terima Aset"],
            Subtitle = AssetListPaging.Subtitle(_t, (_t["Cari"], q), (_t["Status"], string.IsNullOrEmpty(status) ? null : StatusLabel(status))),
            FileName = "serah_terima_aset",
            Headers = new[] { "No. Aset", "Nama Aset", "Pemegang", "Departemen", "Lokasi", "Tanggal serah terima", "Tanggal kembali", "Status", "Catatan" }
                .Select(h => _t[h]).ToArray(),
            Rows = rows.Select(x => new object?[]
            {
                x.AssetNo, x.AssetName, x.HolderName, x.HolderDepartment, x.Location, x.AssignedDate, x.ReturnedDate, StatusLabel(x.Status),
                string.Join(" ", new[] { x.Note, x.ReturnNote }.Where(n => !string.IsNullOrWhiteSpace(n)))
            }).ToList()
        };
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Guid assetUid, string? holderName, string? holderDepartment, string? location,
        DateOnly? assignedDate, string? note, string? returnTo)
    {
        var result = await _api.CreateAssignmentAsync(assetUid, holderName, holderDepartment, location, assignedDate, note);
        return Done(result, returnTo, assetUid);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReturnAsset(Guid uid, DateOnly? returnedDate, string? returnNote, string? returnTo, Guid? assetUid) =>
        Done(await _api.ReturnAssignmentAsync(uid, returnedDate, returnNote), returnTo, assetUid);

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid uid, string? returnTo, Guid? assetUid) =>
        Done(await _api.DeleteAssignmentAsync(uid), returnTo, assetUid);

    private IActionResult Done(ApiResult result, string? returnTo, Guid? assetUid)
    {
        TempData[result.Success ? "Success" : "Error"] = result.Message;

        return returnTo == "asset" && assetUid != null
            ? Redirect(Url.Action("Details", "Assets", new { uid = assetUid }) + "#assignments")
            : RedirectToAction(nameof(Index));
    }
}

/// <summary>Manajemen Aset &gt; Maintenance.</summary>
public class AssetMaintenanceController : Controller
{
    private readonly AssetApiClient _api;
    private readonly Translator _t;

    public AssetMaintenanceController(AssetApiClient api, Translator t)
    {
        _api = api;
        _t = t;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? q, string? type, Guid? assetUid, int page = 1, int pageSize = 25) => View(new AssetMaintenanceIndexViewModel
    {
        Q = q,
        Type = type,
        Page = Math.Max(page, 1),
        Rows = await _api.GetMaintenancesAsync(q, type, Math.Max(page, 1), AssetListPaging.Size(pageSize)),
        Reminders = await _api.GetMaintenanceRemindersAsync(),
        Options = await _api.GetMaintenanceFormOptionsAsync(),
        PreselectAssetUid = assetUid
    });

    [HttpGet]
    public async Task<IActionResult> ExportExcel(string? q, string? type) => this.ExcelFile(await BuildExportAsync(q, type));

    [HttpGet]
    public async Task<IActionResult> ExportPdf(string? q, string? type) => await this.PdfFile(await BuildExportAsync(q, type));

    [HttpGet]
    public async Task<IActionResult> PrintList(string? q, string? type) => this.PrintView(await BuildExportAsync(q, type));

    private async Task<ListExport> BuildExportAsync(string? q, string? type)
    {
        var rows = await AssetListPaging.AllAsync(p => _api.GetMaintenancesAsync(q, type, p, AssetListPaging.MaxPageSize));

        return new ListExport
        {
            Title = _t["Maintenance Aset"],
            Subtitle = AssetListPaging.Subtitle(_t, (_t["Cari"], q), (_t["Jenis"], string.IsNullOrEmpty(type) ? null : _t[AssetLabels.MaintenanceType(type)])),
            FileName = "maintenance_aset",
            Headers = new[] { "Tanggal", "No. Aset", "Nama Aset", "Jenis", "Keterangan", "Vendor", "Biaya", "Jurnal", "Kas / Bank", "Akun beban", "Jadwal berikut" }
                .Select(h => _t[h]).ToArray(),
            Rows = rows.Select(m => new object?[]
            {
                m.MaintenanceDate, m.AssetNo, m.AssetName, _t[AssetLabels.MaintenanceType(m.Type)], m.Description, m.Vendor, m.Cost,
                m.JournalNo ?? _t["Hanya catatan"], m.CashAccountName, m.ExpenseAccountLabel, m.NextDueDate
            }).ToList(),
            Footer = new object?[] { _t["Total"], null, null, null, null, null, rows.Sum(m => m.Cost), null, null, null, null }
        };
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Guid assetUid, DateOnly? maintenanceDate, string? type, string? description, decimal cost,
        string? vendor, DateOnly? nextDueDate, string? note, long? cashAccountId, long? expenseAccountId, string? setStatus, string? returnTo)
    {
        var result = await _api.CreateMaintenanceAsync(new
        {
            assetUid,
            maintenanceDate,
            type,
            description,
            cost,
            vendor,
            nextDueDate,
            note,
            cashAccountId,
            expenseAccountId = cashAccountId == null ? null : expenseAccountId,
            setStatus
        });

        return Done(result, returnTo, assetUid);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid uid, DateOnly? date, string? reason, string? returnTo, Guid? assetUid) =>
        Done(await _api.DeleteMaintenanceAsync(uid, date, reason), returnTo, assetUid);

    private IActionResult Done(ApiResult result, string? returnTo, Guid? assetUid)
    {
        TempData[result.Success ? "Success" : "Error"] = result.Message;

        return returnTo == "asset" && assetUid != null
            ? Redirect(Url.Action("Details", "Assets", new { uid = assetUid }) + "#maintenance")
            : RedirectToAction(nameof(Index));
    }
}

/// <summary>Manajemen Aset &gt; Penyusutan (proses s/d periode, batal proses terakhir).</summary>
public class AssetDepreciationController : Controller
{
    private readonly AssetApiClient _api;
    private readonly Translator _t;

    public AssetDepreciationController(AssetApiClient api, Translator t)
    {
        _api = api;
        _t = t;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? period) => View(await _api.GetDepreciationPageAsync(period));

    /// <summary>Export riwayat proses penyusutan (daftar utama halaman; tidak berhalaman).</summary>
    [HttpGet]
    public async Task<IActionResult> ExportExcel(string? period) => this.ExcelFile(await BuildExportAsync(period));

    [HttpGet]
    public async Task<IActionResult> ExportPdf(string? period) => await this.PdfFile(await BuildExportAsync(period));

    [HttpGet]
    public async Task<IActionResult> PrintList(string? period) => this.PrintView(await BuildExportAsync(period));

    private async Task<ListExport> BuildExportAsync(string? period)
    {
        var runs = (await _api.GetDepreciationPageAsync(period)).Runs;

        return new ListExport
        {
            Title = _t["Riwayat Proses Penyusutan"],
            FileName = "proses_penyusutan",
            Headers = new[] { "No. Proses", "S/d Periode", "Tgl Jurnal", "Jenis", "Catatan", "Jumlah Aset", "Baris", "Total", "Jurnal", "Jurnal Pembalik", "Status", "Alasan Batal", "Dibuat Oleh", "Dibuat" }
                .Select(h => _t[h]).ToArray(),
            Rows = runs.Select(r => new object?[]
            {
                r.RunNo, AssetLabels.Period(r.Period), r.RunDate, r.Kind == "disposal" ? _t["Catch-up pelepasan"] : _t["Proses"], r.Note,
                r.AssetCount, r.RowCount, r.TotalAmount, r.JournalNo, r.ReversalJournalNo,
                r.Status == "posted" ? _t["Posted"] : _t["Dibatalkan"], r.ReverseReason, r.CreatedByName, r.CreatedAt
            }).ToList()
        };
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Run(string? period, string? note)
    {
        var result = await _api.RunDepreciationAsync(period, note);
        TempData[result.Success ? "Success" : "Error"] = result.Message;
        return RedirectToAction(nameof(Index), new { period });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReverseLast(string? reason, string? period)
    {
        var result = await _api.ReverseLastRunAsync(reason);
        TempData[result.Success ? "Success" : "Error"] = result.Message;
        return RedirectToAction(nameof(Index), new { period });
    }
}

/// <summary>Laporan Nilai Buku Aset per tanggal (per kategori, cek silang GL) + Excel/PDF/cetak.</summary>
public class AssetBookValueController : Controller
{
    private readonly AssetApiClient _api;
    private readonly Translator _t;

    public AssetBookValueController(AssetApiClient api, Translator t)
    {
        _api = api;
        _t = t;
    }

    [HttpGet]
    public async Task<IActionResult> Index(DateOnly? asOf, Guid? categoryUid)
    {
        var model = await LoadAsync(asOf, categoryUid);
        model.CategoryOptions = await _api.GetBookValueCategoriesAsync();
        return View(model);
    }

    /// <summary>Cetak laporan (toolbar standar) — memakai tampilan cetak laporan yang sudah ada (subtotal + cek silang GL).</summary>
    [HttpGet]
    public async Task<IActionResult> PrintList(DateOnly? asOf, Guid? categoryUid) => View("Print", await LoadAsync(asOf, categoryUid));

    [HttpGet]
    public async Task<IActionResult> Print(DateOnly? asOf, Guid? categoryUid) => View(await LoadAsync(asOf, categoryUid));

    [HttpGet]
    public async Task<IActionResult> ExportExcel(DateOnly? asOf, Guid? categoryUid)
    {
        var r = await LoadAsync(asOf, categoryUid);

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add(_t["Nilai Buku Aset"]);

        sheet.Cell(1, 1).Value = Title(r);
        sheet.Cell(1, 1).Style.Font.Bold = true;

        var headers = new[] { "No. Aset", "Nama Aset", "Kategori", "Tgl Perolehan", "Status", "Harga Perolehan", "Akumulasi Penyusutan", "Nilai Buku" }
            .Select(h => _t[h]).ToArray();
        for (var i = 0; i < headers.Length; i++)
            sheet.Cell(3, i + 1).Value = headers[i];

        var header = sheet.Range(3, 1, 3, headers.Length);
        header.Style.Font.Bold = true;
        header.Style.Font.FontColor = XLColor.White;
        header.Style.Fill.BackgroundColor = XLColor.FromHtml("#0D6EFD");

        var row = 4;
        foreach (var c in r.Categories)
        {
            foreach (var x in c.Rows)
            {
                sheet.Cell(row, 1).Value = x.AssetNo;
                sheet.Cell(row, 2).Value = x.Name;
                sheet.Cell(row, 3).Value = c.CategoryName;
                if (x.AcquisitionDate != null)
                {
                    sheet.Cell(row, 4).Value = x.AcquisitionDate.Value.ToDateTime(TimeOnly.MinValue);
                    sheet.Cell(row, 4).Style.DateFormat.Format = "dd/MM/yyyy";
                }
                sheet.Cell(row, 5).Value = _t[AssetLabels.Status(x.Status)];
                sheet.Cell(row, 6).Value = x.Cost;
                sheet.Cell(row, 7).Value = x.Accumulated;
                sheet.Cell(row, 8).Value = x.BookValue;
                row++;
            }

            sheet.Cell(row, 2).Value = _t["Subtotal {0}", c.CategoryName];
            sheet.Cell(row, 6).Value = c.Cost;
            sheet.Cell(row, 7).Value = c.Accumulated;
            sheet.Cell(row, 8).Value = c.BookValue;
            sheet.Range(row, 1, row, 8).Style.Font.Bold = true;
            row++;
        }

        sheet.Cell(row, 2).Value = _t["TOTAL"];
        sheet.Cell(row, 6).Value = r.TotalCost;
        sheet.Cell(row, 7).Value = r.TotalAccumulated;
        sheet.Cell(row, 8).Value = r.TotalBookValue;
        sheet.Range(row, 1, row, 8).Style.Font.Bold = true;
        sheet.Range(4, 6, row, 8).Style.NumberFormat.Format = "#,##0.00";

        row += 2;
        sheet.Cell(row, 1).Value = _t["Cek silang buku besar"];
        sheet.Cell(row, 1).Style.Font.Bold = true;
        row++;
        foreach (var (h, i) in new[] { "Akun", "Subledger aset", "Buku besar", "Selisih" }.Select((h, i) => (h, i)))
            sheet.Cell(row, i + 1).Value = _t[h];
        sheet.Range(row, 1, row, 4).Style.Font.Bold = true;
        row++;
        var checkStart = row;
        foreach (var g in r.GlCheck)
        {
            sheet.Cell(row, 1).Value = $"{g.AccountCode} {g.AccountName}";
            sheet.Cell(row, 2).Value = g.Subledger;
            sheet.Cell(row, 3).Value = g.Ledger;
            sheet.Cell(row, 4).Value = g.Difference;
            row++;
        }
        sheet.Range(checkStart, 2, Math.Max(row - 1, checkStart), 4).Style.NumberFormat.Format = "#,##0.00";
        sheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"nilai_buku_aset_{r.AsOf:yyyyMMdd}.xlsx");
    }

    [HttpGet]
    public async Task<IActionResult> ExportPdf(DateOnly? asOf, Guid? categoryUid)
    {
        var r = await LoadAsync(asOf, categoryUid);
        string M(decimal v) => Acc.Money(v);

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(8.5f).FontColor("#333333"));

                page.Header().Column(col =>
                {
                    col.Item().Text(_t["Nilai Buku Aset"]).Bold().FontSize(16).FontColor("#1a1a1a");
                    col.Item().PaddingTop(2).Text(Title(r)).FontColor("#6c757d");
                });

                page.Content().PaddingTop(12).Column(col =>
                {
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.ConstantColumn(95);
                            c.RelativeColumn(3);
                            c.ConstantColumn(65);
                            c.ConstantColumn(60);
                            c.RelativeColumn(1.4f);
                            c.RelativeColumn(1.4f);
                            c.RelativeColumn(1.4f);
                        });

                        table.Header(h =>
                        {
                            foreach (var title in new[] { "No. Aset", "Nama Aset", "Tgl Perolehan", "Status", "Harga Perolehan", "Akumulasi", "Nilai Buku" })
                                h.Cell().Element(HeaderCell).Text(_t[title]);
                        });

                        foreach (var c in r.Categories)
                        {
                            table.Cell().ColumnSpan(7).Element(GroupCell).Text(c.CategoryName).Bold();

                            foreach (var x in c.Rows)
                            {
                                table.Cell().Element(BodyCell).Text(x.AssetNo);
                                table.Cell().Element(BodyCell).Text(x.Name);
                                table.Cell().Element(BodyCell).Text(Acc.Date(x.AcquisitionDate));
                                table.Cell().Element(BodyCell).Text(_t[AssetLabels.Status(x.Status)]);
                                table.Cell().Element(BodyCell).AlignRight().Text(M(x.Cost));
                                table.Cell().Element(BodyCell).AlignRight().Text(M(x.Accumulated));
                                table.Cell().Element(BodyCell).AlignRight().Text(M(x.BookValue));
                            }

                            table.Cell().ColumnSpan(4).Element(BodyCell).Text(_t["Subtotal {0}", c.CategoryName]).Bold();
                            table.Cell().Element(BodyCell).AlignRight().Text(M(c.Cost)).Bold();
                            table.Cell().Element(BodyCell).AlignRight().Text(M(c.Accumulated)).Bold();
                            table.Cell().Element(BodyCell).AlignRight().Text(M(c.BookValue)).Bold();
                        }

                        table.Cell().ColumnSpan(4).Element(TotalCell).Text(_t["TOTAL"]).Bold();
                        table.Cell().Element(TotalCell).AlignRight().Text(M(r.TotalCost)).Bold();
                        table.Cell().Element(TotalCell).AlignRight().Text(M(r.TotalAccumulated)).Bold();
                        table.Cell().Element(TotalCell).AlignRight().Text(M(r.TotalBookValue)).Bold();
                    });

                    col.Item().PaddingTop(14).Text(_t["Cek silang buku besar"]).Bold();
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(3);
                            c.RelativeColumn(1.5f);
                            c.RelativeColumn(1.5f);
                            c.RelativeColumn(1.5f);
                        });
                        table.Header(h =>
                        {
                            foreach (var title in new[] { "Akun", "Subledger aset", "Buku besar", "Selisih" })
                                h.Cell().Element(HeaderCell).Text(_t[title]);
                        });
                        foreach (var g in r.GlCheck)
                        {
                            table.Cell().Element(BodyCell).Text($"{g.AccountCode} {g.AccountName}");
                            table.Cell().Element(BodyCell).AlignRight().Text(M(g.Subledger));
                            table.Cell().Element(BodyCell).AlignRight().Text(M(g.Ledger));
                            table.Cell().Element(BodyCell).AlignRight().Text(M(g.Difference));
                        }
                    });
                    col.Item().PaddingTop(4).Text(r.IsReconciled
                            ? _t["Subledger aset sama dengan saldo buku besar."]
                            : _t["Subledger aset berbeda dengan saldo buku besar — periksa jurnal pada akun aset tetap."])
                        .FontColor(r.IsReconciled ? "#198754" : "#b02a37");
                });

                page.Footer().AlignRight().Text(t =>
                {
                    t.Span(_t["Halaman"] + " ");
                    t.CurrentPageNumber();
                    t.Span(" / ");
                    t.TotalPages();
                });
            });
        });

        return File(document.GeneratePdf(), "application/pdf", $"nilai_buku_aset_{r.AsOf:yyyyMMdd}.pdf");

        static IContainer HeaderCell(IContainer c) =>
            c.Background("#0D6EFD").Padding(4).DefaultTextStyle(x => x.FontColor(Colors.White).Bold());

        static IContainer BodyCell(IContainer c) =>
            c.BorderBottom(0.5f).BorderColor("#dee2e6").Padding(4);

        static IContainer GroupCell(IContainer c) =>
            c.PaddingTop(8).PaddingBottom(2).PaddingHorizontal(4);

        static IContainer TotalCell(IContainer c) =>
            c.BorderTop(1.5f).BorderColor("#333333").Padding(4);
    }

    private string Title(BookValueReportViewModel r) =>
        r.CategoryName == null
            ? _t["Nilai Buku Aset per {0}", Acc.Date(r.AsOf)]
            : _t["Nilai Buku Aset per {0} — kategori {1}", Acc.Date(r.AsOf), r.CategoryName];

    private Task<BookValueReportViewModel> LoadAsync(DateOnly? asOf, Guid? categoryUid) =>
        _api.GetBookValueAsync(asOf ?? DateOnly.FromDateTime(DateTime.Today), categoryUid);
}
