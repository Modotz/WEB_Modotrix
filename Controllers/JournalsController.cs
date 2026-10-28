using ERP.Web.Models;
using ERP.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Web.Controllers;

/// <summary>
/// Akuntansi &gt; Jurnal Umum. Draft bisa diubah/dihapus; jurnal posted dikoreksi lewat
/// Batalkan (jurnal pembalik) atau Revisi (pembalik + jurnal baru).
/// </summary>
public class JournalsController : Controller
{
    private static readonly int[] AllowedPageSizes = { 10, 25, 50, 100 };

    private readonly AccountingApiClient _api;
    private readonly Translator _t;

    public JournalsController(AccountingApiClient api, Translator t)
    {
        _api = api;
        _t = t;
    }

    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] JournalFilterViewModel filter)
    {
        filter.Page = Math.Max(filter.Page, 1);
        filter.PageSize = AllowedPageSizes.Contains(filter.PageSize) ? filter.PageSize : 25;

        return View(new JournalsIndexViewModel
        {
            Filter = filter,
            Journals = await _api.GetJournalsAsync(filter)
        });
    }

    [HttpGet]
    public async Task<IActionResult> Details(Guid uid)
    {
        var journal = await _api.GetJournalAsync(uid);
        if (journal == null)
            return NotFoundRedirect();

        return View(journal);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var form = new JournalFormViewModel
        {
            Lines = new List<JournalLineViewModel> { new(), new() }
        };

        return await FormView(form);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(JournalFormViewModel form, string? submitAction)
    {
        form.Mode = "create";
        return await Submit(form, post: submitAction == "post");
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid uid)
    {
        var journal = await _api.GetJournalAsync(uid);
        if (journal == null)
            return NotFoundRedirect();

        if (!journal.CanEdit)
        {
            TempData["Error"] = "Hanya jurnal manual berstatus draft yang dapat diubah.";
            return RedirectToAction(nameof(Details), new { uid });
        }

        return await FormView(FromJournal(journal, "edit"));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(JournalFormViewModel form, string? submitAction)
    {
        form.Mode = "edit";
        return await Submit(form, post: submitAction == "post");
    }

    /// <summary>Form revisi: isi awal dari jurnal posted; simpan = pembalik + jurnal baru.</summary>
    [HttpGet]
    public async Task<IActionResult> Revise(Guid uid)
    {
        var journal = await _api.GetJournalAsync(uid);
        if (journal == null)
            return NotFoundRedirect();

        if (!journal.CanVoid)
        {
            TempData["Error"] = "Jurnal ini tidak dapat direvisi.";
            return RedirectToAction(nameof(Details), new { uid });
        }

        return await FormView(FromJournal(journal, "revise"));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [ActionName("Revise")]
    public async Task<IActionResult> RevisePost(JournalFormViewModel form)
    {
        form.Mode = "revise";
        return await Submit(form, post: true);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid uid)
    {
        var result = await _api.DeleteJournalAsync(uid);
        TempData[result.Success ? "Success" : "Error"] = result.Message;

        return result.Success ? RedirectToAction(nameof(Index)) : RedirectToAction(nameof(Details), new { uid });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Post(Guid uid)
    {
        return BackToDetails(await _api.PostJournalAsync(uid), uid);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Void(Guid uid, string? reason, DateOnly? date)
    {
        return BackToDetails(await _api.VoidJournalAsync(uid, reason, date), uid);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReverseAccrual(Guid uid)
    {
        return BackToDetails(await _api.ReverseAccrualAsync(uid), uid);
    }

    [HttpGet]
    public async Task<IActionResult> ExportExcel([FromQuery] JournalFilterViewModel filter) =>
        this.ExcelFile(await BuildExportAsync(filter, withLines: true));

    [HttpGet]
    public async Task<IActionResult> ExportPdf([FromQuery] JournalFilterViewModel filter) =>
        await this.PdfFile(await BuildExportAsync(filter, withLines: false));

    [HttpGet]
    public async Task<IActionResult> PrintList([FromQuery] JournalFilterViewModel filter) =>
        this.PrintView(await BuildExportAsync(filter, withLines: false));

    /// <summary>Excel memuat baris jurnal (detail akun); PDF/cetak satu baris per jurnal.</summary>
    private async Task<ListExport> BuildExportAsync(JournalFilterViewModel filter, bool withLines)
    {
        var journals = await _api.GetJournalsForExportAsync(filter);
        var subtitle = filter.DateFrom != null || filter.DateTo != null
            ? _t["Periode"] + " " + Acc.Date(filter.DateFrom) + " " + _t["s/d"] + " " + Acc.Date(filter.DateTo)
            : null;

        if (withLines)
        {
            return new ListExport
            {
                Title = _t["Jurnal Umum"],
                Subtitle = subtitle,
                FileName = "jurnal",
                Headers = new[] { "No. Jurnal", "Tanggal", "Referensi", "Keterangan", "Sumber", "Status", "Kode Akun", "Nama Akun", "Keterangan Baris", "Debit", "Kredit" }
                    .Select(h => _t[h]).ToArray(),
                Rows = journals.SelectMany(j => j.Lines.Select(l => new object?[]
                {
                    j.JournalNo, j.JournalDate, j.Reference, j.Description,
                    j.Source == "manual" ? _t["Manual"] : _t["Sistem"], _t[j.StatusBadge.Label],
                    l.AccountCode, l.AccountName, l.Description, l.Debit, l.Credit
                })).ToList()
            };
        }

        return new ListExport
        {
            Title = _t["Jurnal Umum"],
            Subtitle = subtitle,
            FileName = "jurnal",
            Headers = new[] { "No. Jurnal", "Tanggal", "Referensi", "Keterangan", "Sumber", "Total", "Status" }.Select(h => _t[h]).ToArray(),
            Rows = journals.Select(j => new object?[]
            {
                j.JournalNo, j.JournalDate, j.Reference, j.Description,
                j.Source == "manual" ? _t["Manual"] : _t["Sistem"], j.TotalDebit, _t[j.StatusBadge.Label]
            }).ToList(),
            Footer = new object?[] { _t["Total"], null, null, null, null, journals.Sum(j => j.TotalDebit), null }
        };
    }

    [HttpGet]
    public async Task<IActionResult> Print(Guid uid)
    {
        var journal = await _api.GetJournalAsync(uid);
        if (journal == null)
            return NotFoundRedirect();

        return View(journal);
    }

    // ------------------------------------------------------------------

    private async Task<IActionResult> Submit(JournalFormViewModel form, bool post)
    {
        form.Lines ??= new List<JournalLineViewModel>();

        var result = await _api.SaveJournalAsync(form, post);

        if (!result.Success)
        {
            TempData["Error"] = result.Message;
            return await FormView(form);
        }

        TempData["Success"] = result.Message;

        return result.Uid != null
            ? RedirectToAction(nameof(Details), new { uid = result.Uid })
            : RedirectToAction(nameof(Index));
    }

    private async Task<IActionResult> FormView(JournalFormViewModel form)
    {
        form.Accounts = await _api.GetAccountOptionsAsync(postable: true, manual: true);
        form.Dimensions = await _api.GetDimensionOptionsAsync(form.Lines.Select(l => l.CostCenterId), form.Lines.Select(l => l.ProjectId));

        while (form.Lines.Count < 2)
            form.Lines.Add(new JournalLineViewModel());

        return View("Form", form);
    }

    private static JournalFormViewModel FromJournal(JournalViewModel j, string mode) => new()
    {
        Uid = j.Uid,
        Mode = mode,
        OriginalNo = j.JournalNo,
        JournalDate = j.JournalDate,
        Reference = mode == "revise" ? null : j.Reference,
        Description = j.Description,
        ReversalDate = j.ReversalDate,
        Lines = j.Lines.Select(l => new JournalLineViewModel
        {
            AccountId = l.AccountId,
            Description = l.Description,
            Debit = l.Debit,
            Credit = l.Credit,
            CostCenterId = l.CostCenterId,
            ProjectId = l.ProjectId
        }).ToList()
    };

    private IActionResult BackToDetails(ApiResult result, Guid uid)
    {
        TempData[result.Success ? "Success" : "Error"] = result.Message;
        return RedirectToAction(nameof(Details), new { uid = result.Success && result.Uid != null ? result.Uid : uid });
    }

    private IActionResult NotFoundRedirect()
    {
        TempData["Error"] = "Jurnal tidak ditemukan.";
        return RedirectToAction(nameof(Index));
    }
}
