using ERP.Web.Models;
using ERP.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Web.Controllers;

// Akuntansi > laporan keuangan & tutup buku. Satu controller per menu (hak akses per menu).
// Cetak memakai tampilan Print masing-masing (layout laporan); PDF/Excel memakai ListExport dengan baris laporan diratakan.

public class IncomeStatementController : Controller
{
    private readonly AccountingApiClient _api;
    private readonly Translator _t;

    public IncomeStatementController(AccountingApiClient api, Translator t)
    {
        _api = api;
        _t = t;
    }

    [HttpGet]
    public async Task<IActionResult> Index(DateOnly? from, DateOnly? to) =>
        View(await Load(from, to));

    [HttpGet]
    public async Task<IActionResult> Print(DateOnly? from, DateOnly? to) =>
        View(await Load(from, to));

    [HttpGet]
    public async Task<IActionResult> ExportExcel(DateOnly? from, DateOnly? to) =>
        this.ExcelFile(await BuildExportAsync(from, to));

    [HttpGet]
    public async Task<IActionResult> ExportPdf(DateOnly? from, DateOnly? to) =>
        await this.PdfFile(await BuildExportAsync(from, to));

    [HttpGet]
    public async Task<IActionResult> PrintList(DateOnly? from, DateOnly? to) =>
        this.PrintView(await BuildExportAsync(from, to));

    private async Task<ListExport> BuildExportAsync(DateOnly? from, DateOnly? to)
    {
        var m = await Load(from, to);
        var rows = new List<object?[]>();

        ReportExport.AddSection(rows, _t, m.Revenue, "Total Pendapatan");
        ReportExport.AddSection(rows, _t, m.CostOfGoodsSold, "Total Beban Pokok Penjualan");
        rows.Add(new object?[] { null, _t["LABA KOTOR"], m.GrossProfit });
        ReportExport.AddSection(rows, _t, m.Expenses, "Total Beban");

        return ReportExport.Create(_t, _t["Laba Rugi"], ReportExport.Period(_t, m.From, m.To), "laba_rugi", rows,
            new object?[] { null, _t[m.NetIncome >= 0 ? "LABA BERSIH" : "RUGI BERSIH"], m.NetIncome });
    }

    private Task<IncomeStatementViewModel> Load(DateOnly? from, DateOnly? to)
    {
        var (start, end) = ReportDates.Range(from, to);
        return _api.GetIncomeStatementAsync(start, end);
    }
}

public class BalanceSheetController : Controller
{
    private readonly AccountingApiClient _api;
    private readonly Translator _t;

    public BalanceSheetController(AccountingApiClient api, Translator t)
    {
        _api = api;
        _t = t;
    }

    [HttpGet]
    public async Task<IActionResult> Index(DateOnly? asOf) =>
        View(await Load(asOf));

    [HttpGet]
    public async Task<IActionResult> Print(DateOnly? asOf) =>
        View(await Load(asOf));

    [HttpGet]
    public async Task<IActionResult> ExportExcel(DateOnly? asOf) =>
        this.ExcelFile(await BuildExportAsync(asOf));

    [HttpGet]
    public async Task<IActionResult> ExportPdf(DateOnly? asOf) =>
        await this.PdfFile(await BuildExportAsync(asOf));

    [HttpGet]
    public async Task<IActionResult> PrintList(DateOnly? asOf) =>
        this.PrintView(await BuildExportAsync(asOf));

    private async Task<ListExport> BuildExportAsync(DateOnly? asOf)
    {
        var m = await Load(asOf);
        var rows = new List<object?[]>();

        ReportExport.AddSection(rows, _t, m.Assets, "TOTAL ASET");
        ReportExport.AddSection(rows, _t, m.Liabilities, "Total Liabilitas");
        ReportExport.AddSection(rows, _t, m.Equity, "Total Ekuitas");

        return ReportExport.Create(_t, _t["Neraca"], _t["Per {0}", Acc.Date(m.AsOf)], "neraca", rows,
            new object?[] { null, _t["TOTAL LIABILITAS + EKUITAS"], m.TotalLiabilitiesAndEquity });
    }

    private Task<BalanceSheetViewModel> Load(DateOnly? asOf) =>
        _api.GetBalanceSheetAsync(asOf ?? DateOnly.FromDateTime(DateTime.Today));
}

public class CashFlowController : Controller
{
    private readonly AccountingApiClient _api;
    private readonly Translator _t;

    public CashFlowController(AccountingApiClient api, Translator t)
    {
        _api = api;
        _t = t;
    }

    [HttpGet]
    public async Task<IActionResult> Index(DateOnly? from, DateOnly? to) =>
        View(await Load(from, to));

    [HttpGet]
    public async Task<IActionResult> Print(DateOnly? from, DateOnly? to) =>
        View(await Load(from, to));

    [HttpGet]
    public async Task<IActionResult> ExportExcel(DateOnly? from, DateOnly? to) =>
        this.ExcelFile(await BuildExportAsync(from, to));

    [HttpGet]
    public async Task<IActionResult> ExportPdf(DateOnly? from, DateOnly? to) =>
        await this.PdfFile(await BuildExportAsync(from, to));

    [HttpGet]
    public async Task<IActionResult> PrintList(DateOnly? from, DateOnly? to) =>
        this.PrintView(await BuildExportAsync(from, to));

    private async Task<ListExport> BuildExportAsync(DateOnly? from, DateOnly? to)
    {
        var m = await Load(from, to);
        var rows = new List<object?[]>
        {
            new object?[] { null, _t["Saldo kas & bank awal ({0})", Acc.Date(m.From)], m.OpeningCash }
        };

        ReportExport.AddSection(rows, _t, m.Operating, "Arus kas bersih dari aktivitas operasi");
        ReportExport.AddSection(rows, _t, m.Investing, "Arus kas bersih dari aktivitas investasi");
        ReportExport.AddSection(rows, _t, m.Financing, "Arus kas bersih dari aktivitas pendanaan");
        if (m.FxEffect.Lines.Count > 0)
            ReportExport.AddSection(rows, _t, m.FxEffect, "Pengaruh perubahan kurs terhadap kas");
        rows.Add(new object?[] { null, _t["Kenaikan (penurunan) kas bersih"], m.NetChange });

        var subtitle = ReportExport.Period(_t, m.From, m.To) + " · " + _t["Akun kas:"] + " " + string.Join(", ", m.CashAccounts);

        return ReportExport.Create(_t, _t["Arus Kas"], subtitle, "arus_kas", rows,
            new object?[] { null, _t["Saldo kas & bank akhir ({0})", Acc.Date(m.To)], m.ClosingCash });
    }

    private Task<CashFlowViewModel> Load(DateOnly? from, DateOnly? to)
    {
        var (start, end) = ReportDates.Range(from, to);
        return _api.GetCashFlowAsync(start, end);
    }
}

public class EquityChangesController : Controller
{
    private readonly AccountingApiClient _api;
    private readonly Translator _t;

    public EquityChangesController(AccountingApiClient api, Translator t)
    {
        _api = api;
        _t = t;
    }

    [HttpGet]
    public async Task<IActionResult> Index(DateOnly? from, DateOnly? to) =>
        View(await Load(from, to));

    [HttpGet]
    public async Task<IActionResult> Print(DateOnly? from, DateOnly? to) =>
        View(await Load(from, to));

    [HttpGet]
    public async Task<IActionResult> ExportExcel(DateOnly? from, DateOnly? to) =>
        this.ExcelFile(await BuildExportAsync(from, to));

    [HttpGet]
    public async Task<IActionResult> ExportPdf(DateOnly? from, DateOnly? to) =>
        await this.PdfFile(await BuildExportAsync(from, to));

    [HttpGet]
    public async Task<IActionResult> PrintList(DateOnly? from, DateOnly? to) =>
        this.PrintView(await BuildExportAsync(from, to));

    private async Task<ListExport> BuildExportAsync(DateOnly? from, DateOnly? to)
    {
        var m = await Load(from, to);

        return new ListExport
        {
            Title = _t["Perubahan Ekuitas"],
            Subtitle = ReportExport.Period(_t, m.From, m.To),
            FileName = "perubahan_ekuitas",
            Landscape = false,
            Headers = new[] { "Kode", "Komponen Ekuitas", "Saldo Awal", "Perubahan", "Saldo Akhir" }.Select(h => _t[h]).ToArray(),
            Rows = m.Rows.Select(r => new object?[]
            {
                r.Code, r.Uid == null || r.Name.StartsWith("Dikurangi: ") ? _t[r.Name] : r.Name, r.Opening, r.Movement, r.Closing
            }).ToList(),
            Footer = new object?[] { null, _t["TOTAL EKUITAS"], m.TotalOpening, m.TotalMovement, m.TotalClosing }
        };
    }

    private Task<EquityChangesViewModel> Load(DateOnly? from, DateOnly? to)
    {
        var (start, end) = ReportDates.Range(from, to, yearToDate: true);
        return _api.GetEquityChangesAsync(start, end);
    }
}

/// <summary>Tutup Buku tahunan: daftar tahun, pratinjau jurnal penutup, tutup &amp; batal.</summary>
public class ClosingBookController : Controller
{
    private readonly AccountingApiClient _api;
    private readonly Translator _t;

    public ClosingBookController(AccountingApiClient api, Translator t)
    {
        _api = api;
        _t = t;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int? year) =>
        View(await Load(year));

    [HttpGet]
    public async Task<IActionResult> ExportExcel(int? year) =>
        this.ExcelFile(await BuildExportAsync(year));

    [HttpGet]
    public async Task<IActionResult> ExportPdf(int? year) =>
        await this.PdfFile(await BuildExportAsync(year));

    [HttpGet]
    public async Task<IActionResult> PrintList(int? year) =>
        this.PrintView(await BuildExportAsync(year));

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Close(int year) => Done(await _api.CloseYearAsync(year), year);

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int year, string? reason) => Done(await _api.CancelClosingAsync(year, reason), year);

    private async Task<ClosingBookPageViewModel> Load(int? year)
    {
        var model = new ClosingBookPageViewModel { Years = await _api.GetClosingYearsAsync() };

        var selected = year ?? model.Years.FirstOrDefault(y => y.CanClose)?.Year ?? model.Years.FirstOrDefault()?.Year;
        if (selected != null)
            model.Preview = await _api.GetClosingPreviewAsync(selected.Value);

        return model;
    }

    /// <summary>Pratinjau jurnal penutup tahun terpilih, baris persis seperti di layar.</summary>
    private async Task<ListExport> BuildExportAsync(int? year)
    {
        var p = (await Load(year)).Preview;
        var rows = new List<object?[]>();
        static object? Blank(decimal v) => v == 0 ? null : v;

        if (p != null)
        {
            rows.AddRange(p.Revenue.Select(r => new object?[] { r.Code + " " + r.Name, Blank(r.Amount > 0 ? r.Amount : 0), Blank(r.Amount < 0 ? -r.Amount : 0) }));
            rows.AddRange(p.Expenses.Select(e => new object?[] { "    " + e.Code + " " + e.Name, Blank(e.Amount < 0 ? -e.Amount : 0), Blank(e.Amount > 0 ? e.Amount : 0) }));
            rows.Add(new object?[]
            {
                (p.RetainedEarningsAccount ?? _t["Laba Ditahan"]) + " (" + _t[p.NetIncome >= 0 ? "laba" : "rugi"] + ")",
                Blank(p.NetIncome < 0 ? -p.NetIncome : 0), Blank(p.NetIncome > 0 ? p.NetIncome : 0)
            });
        }

        return new ListExport
        {
            Title = _t["Tutup Buku Tahunan"],
            Subtitle = p == null
                ? null
                : _t["Tahun {0}", p.Year] + " · " + _t["Pratinjau jurnal penutup (Laba Ditahan: {0})", p.RetainedEarningsAccount ?? _t["belum ada"]],
            FileName = p == null ? "tutup_buku" : $"tutup_buku_{p.Year}",
            Landscape = false,
            Headers = new[] { "Akun", "Debit", "Kredit" }.Select(h => _t[h]).ToArray(),
            Rows = rows
        };
    }

    private IActionResult Done(ApiResult result, int year)
    {
        TempData[result.Success ? "Success" : "Error"] = result.Message;
        return RedirectToAction(nameof(Index), new { year });
    }
}

internal static class ReportDates
{
    public static (DateOnly From, DateOnly To) Range(DateOnly? from, DateOnly? to, bool yearToDate = false)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var start = from ?? (yearToDate ? new DateOnly(today.Year, 1, 1) : new DateOnly(today.Year, today.Month, 1));

        return (start, to ?? today);
    }
}

/// <summary>Meratakan laporan berkelompok (_ReportRows) menjadi baris ListExport: judul kelompok, akun (menjorok), total.</summary>
internal static class ReportExport
{
    private const string Indent = "    ";

    public static string Period(Translator t, DateOnly from, DateOnly to) => t["Periode {0} s/d {1}", Acc.Date(from), Acc.Date(to)];

    public static void AddSection(List<object?[]> rows, Translator t, ReportSectionViewModel s, string totalLabel)
    {
        rows.Add(new object?[] { null, t[s.Title], null });

        if (s.Lines.Count == 0)
            rows.Add(new object?[] { null, Indent + t["Tidak ada saldo"], null });

        foreach (var l in s.Lines)
        {
            var name = l.Uid == null ? t[l.Name] : l.Name;
            rows.Add(new object?[] { l.Code, Indent + (l.IsContra ? t["Dikurangi:"] + " " : "") + name, l.Amount });
        }

        rows.Add(new object?[] { null, t[totalLabel], s.Total });
    }

    public static ListExport Create(Translator t, string title, string? subtitle, string fileName, List<object?[]> rows, object?[] footer) => new()
    {
        Title = title,
        Subtitle = subtitle,
        FileName = fileName,
        Landscape = false,
        Headers = new[] { "Kode", "Keterangan", "Jumlah" }.Select(h => t[h]).ToArray(),
        Rows = rows,
        Footer = footer
    };
}
