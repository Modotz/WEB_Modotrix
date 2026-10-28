using System.Security.Claims;
using System.Text.Json;
using ERP.Web.Models;
using ERP.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace ERP.Web.Controllers;

/// <summary>
/// Persetujuan &gt; Kotak Persetujuan: tab Menunggu Saya / Diajukan Saya / Riwayat (+ Semua Pending untuk
/// Administrator), aksi setujui / tolak / tarik / ajukan. Hak memutuskan dicek mesin di API.
/// </summary>
public class ApprovalsController : Controller
{
    private static readonly TimeSpan BadgeCache = TimeSpan.FromSeconds(30);
    private static readonly int[] AllowedPageSizes = { 10, 25, 50, 100 };
    private const int ExportPageSize = 100;

    private readonly AccountingApiClient _api;
    private readonly AccessService _access;
    private readonly IMemoryCache _cache;
    private readonly Translator _t;

    public ApprovalsController(AccountingApiClient api, AccessService access, IMemoryCache cache, Translator t)
    {
        _api = api;
        _access = access;
        _cache = cache;
        _t = t;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? tab, string? q, int page = 1, int pageSize = 25)
    {
        var isAdmin = await _access.IsAdminAsync();
        tab = NormalizeTab(tab, isAdmin);
        pageSize = AllowedPageSizes.Contains(pageSize) ? pageSize : 25;

        return View(new ApprovalsIndexViewModel
        {
            Tab = tab,
            Q = q,
            IsAdmin = isAdmin,
            WaitingCount = await WaitingCountAsync(_api, _cache, User),
            Requests = await _api.GetApprovalsAsync(tab, q, Math.Max(page, 1), pageSize)
        });
    }

    [HttpGet]
    public async Task<IActionResult> ExportExcel(string? tab, string? q) => this.ExcelFile(await BuildExportAsync(tab, q));

    [HttpGet]
    public async Task<IActionResult> ExportPdf(string? tab, string? q) => await this.PdfFile(await BuildExportAsync(tab, q));

    [HttpGet]
    public async Task<IActionResult> PrintList(string? tab, string? q) => this.PrintView(await BuildExportAsync(tab, q));

    /// <summary>Tab aktif + pencarian, semua halaman (API membatasi pageSize 100).</summary>
    private async Task<ListExport> BuildExportAsync(string? tab, string? q)
    {
        tab = NormalizeTab(tab, await _access.IsAdminAsync());

        var rows = new List<ApprovalRequestViewModel>();
        for (var page = 1; page <= 500; page++)
        {
            var result = await _api.GetApprovalsAsync(tab, q, page, ExportPageSize);
            rows.AddRange(result.Data);
            if (result.Data.Count < ExportPageSize || rows.Count >= result.TotalCount)
                break;
        }

        var tabLabel = tab switch
        {
            "submitted" => "Diajukan Saya",
            "history" => "Riwayat",
            "all" => "Semua Pending",
            _ => "Menunggu Saya"
        };
        var subtitle = _t[tabLabel] + (string.IsNullOrWhiteSpace(q) ? "" : " · " + _t["Cari"] + ": " + q);

        return new ListExport
        {
            Title = _t["Kotak Persetujuan"],
            Subtitle = subtitle,
            FileName = "kotak_persetujuan_" + tab,
            Headers = new[] { "Dokumen", "Nomor", "Alur", "Alasan", "Nominal", "Diajukan Oleh", "Tanggal Diajukan", "Progres", "Status" }
                .Select(h => _t[h]).ToArray(),
            Rows = rows.Select(r => new object?[]
            {
                _t[r.DocTypeLabel], r.DocNo, r.WorkflowName, r.Reason, r.Amount, r.RequestedByName, r.RequestedAt,
                r.Status == "pending"
                    ? _t["Level {0} dari {1}", r.CurrentLevel, r.LevelCount] + " — " + r.CurrentLevelName + " (" + _t["{0} dari {1} persetujuan", r.CurrentApproved, r.CurrentRequired] + ")"
                    : r.DecidedAt?.ToString("dd/MM/yyyy HH:mm"),
                _t[Appr.RequestBadge(r.Status).Label]
            }).ToList(),
            Footer = new object?[] { _t["Total"], null, null, null, rows.Sum(r => r.Amount), null, null, null, null }
        };
    }

    private static string NormalizeTab(string? tab, bool isAdmin) =>
        tab is "submitted" or "history" || (tab == "all" && isAdmin) ? tab : "mine";

    /// <summary>Satu pengajuan (untuk jenis dokumen yang belum punya halaman detail di .NET).</summary>
    [HttpGet]
    public async Task<IActionResult> Details(Guid uid)
    {
        var request = uid == Guid.Empty ? null : await _api.GetApprovalAsync(uid);
        if (request == null)
        {
            TempData["Error"] = "Pengajuan persetujuan tidak ditemukan.";
            return RedirectToAction(nameof(Index));
        }

        return View(new ApprovalDocumentViewModel
        {
            DocType = request.DocType,
            DocTypeLabel = request.DocTypeLabel,
            DocUid = request.DocUid ?? Guid.Empty,
            DocNo = request.DocNo,
            Amount = request.Amount,
            DetailPath = request.DetailPath,
            Requests = new List<ApprovalRequestViewModel> { request },
            HasPending = request.Status == "pending",
            AlwaysShow = true,
            ReturnUrl = Url.Action(nameof(Details), new { uid })
        });
    }

    /// <summary>Ringkasan dokumen + panel persetujuan (mis. pengeluaran Kas Kecil, pembatalan kas).</summary>
    [HttpGet]
    public async Task<IActionResult> Document(string docType, Guid docUid)
    {
        var doc = string.IsNullOrWhiteSpace(docType) || docUid == Guid.Empty ? null : await _api.GetApprovalDocumentAsync(docType, docUid);
        if (doc == null)
        {
            TempData["Error"] = "Dokumen tidak ditemukan.";
            return RedirectToAction(nameof(Index));
        }

        doc.AlwaysShow = true;
        doc.ReturnUrl = Url.Action(nameof(Document), new { docType, docUid });
        return View(doc);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(string docType, Guid docUid, string? reason, string? returnUrl) =>
        Done(await _api.SubmitApprovalAsync(docType, docUid, reason), returnUrl);

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(Guid uid, string? note, string? returnUrl) =>
        Done(await _api.ApproveApprovalAsync(uid, note), returnUrl);

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(Guid uid, string? note, string? returnUrl) =>
        Done(await _api.RejectApprovalAsync(uid, note), returnUrl);

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Withdraw(Guid uid, string? note, string? returnUrl) =>
        Done(await _api.WithdrawApprovalAsync(uid, note), returnUrl);

    private IActionResult Done(ApiResult result, string? returnUrl)
    {
        TempData[result.Success ? "Success" : "Error"] = result.Message;
        ForgetCount(_cache, User);

        return !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? Redirect(returnUrl)
            : RedirectToAction(nameof(Index));
    }

    // ---------- badge sidebar (cache singkat per user) ----------

    private static string CountKey(ClaimsPrincipal user) => $"approvals:count:{user.FindFirstValue(ClaimTypes.NameIdentifier)}";

    public static async Task<int> WaitingCountAsync(AccountingApiClient api, IMemoryCache cache, ClaimsPrincipal user) =>
        await cache.GetOrCreateAsync(CountKey(user), async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = BadgeCache;
            return await api.GetApprovalCountAsync();
        });

    public static void ForgetCount(IMemoryCache cache, ClaimsPrincipal user) => cache.Remove(CountKey(user));
}

/// <summary>Persetujuan &gt; Alur Persetujuan (CRUD alur, level, quorum, approver; diagram alur).</summary>
public class ApprovalWorkflowsController : Controller
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly AccountingApiClient _api;

    private readonly Translator _t;

    public ApprovalWorkflowsController(AccountingApiClient api, Translator t)
    {
        _api = api;
        _t = t;
    }

    /// <summary>Daftar alur; filter (cari / jenis dokumen / status) diterapkan di sini — API mengembalikan semua alur.
    /// Celah cakupan selalu dihitung dari semua alur.</summary>
    [HttpGet]
    public async Task<IActionResult> Index(string? q, string? docType, string? status)
    {
        var list = await _api.GetApprovalWorkflowsAsync();

        ViewData["DocTypes"] = list.Data.Select(w => (w.DocType, w.DocTypeLabel)).Distinct().OrderBy(x => x.DocTypeLabel).ToList();
        ViewData["Q"] = q;
        ViewData["DocType"] = docType;
        ViewData["Status"] = status;

        list.Data = Filter(list.Data, q, docType, status);
        return View(list);
    }

    [HttpGet]
    public async Task<IActionResult> ExportExcel(string? q, string? docType, string? status) => this.ExcelFile(await BuildExportAsync(q, docType, status));

    [HttpGet]
    public async Task<IActionResult> ExportPdf(string? q, string? docType, string? status) => await this.PdfFile(await BuildExportAsync(q, docType, status));

    [HttpGet]
    public async Task<IActionResult> PrintList(string? q, string? docType, string? status) => this.PrintView(await BuildExportAsync(q, docType, status));

    private async Task<ListExport> BuildExportAsync(string? q, string? docType, string? status)
    {
        var all = (await _api.GetApprovalWorkflowsAsync()).Data;
        var rows = Filter(all, q, docType, status);

        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(q)) parts.Add(_t["Cari"] + ": " + q);
        var docLabel = all.FirstOrDefault(w => w.DocType == docType)?.DocTypeLabel;
        if (docLabel != null) parts.Add(_t["Jenis Dokumen"] + ": " + _t[docLabel]);
        if (status is "active" or "inactive") parts.Add(_t["Status"] + ": " + (status == "active" ? _t["Aktif"] : _t["Nonaktif"]));

        string Quorum(ApprovalLevelDefViewModel l) => l.QuorumType switch
        {
            "any" => "ANY",
            "count" => $"COUNT {l.QuorumCount}",
            _ => "ALL"
        };

        return new ListExport
        {
            Title = _t["Alur Persetujuan"],
            Subtitle = parts.Count == 0 ? null : string.Join(" · ", parts),
            FileName = "alur_persetujuan",
            Headers = new[] { "Nama Alur", "Jenis Dokumen", "Nominal Min", "Nominal Maks", "Prioritas", "Level & Approver", "Status", "Dipakai" }
                .Select(h => _t[h]).ToArray(),
            Rows = rows.Select(w => new object?[]
            {
                w.Name, _t[w.DocTypeLabel], w.MinAmount, w.MaxAmount, w.Priority,
                string.Join("; ", w.Levels.Select(l => $"L{l.StepLevel} {Quorum(l)}: " +
                    string.Join(", ", l.Approvers.Select(a => a.Type == "role" ? $"{a.Name} ({_t["role"]})" : a.Name)))),
                w.IsActive ? _t["Aktif"] : _t["Nonaktif"], w.UsedCount
            }).ToList()
        };
    }

    private static List<ApprovalWorkflowViewModel> Filter(List<ApprovalWorkflowViewModel> data, string? q, string? docType, string? status) =>
        data.Where(w =>
                (string.IsNullOrWhiteSpace(q)
                    || w.Name.Contains(q.Trim(), StringComparison.OrdinalIgnoreCase)
                    || (w.Description ?? "").Contains(q.Trim(), StringComparison.OrdinalIgnoreCase))
                && (string.IsNullOrWhiteSpace(docType) || w.DocType == docType)
                && (status switch { "active" => w.IsActive, "inactive" => !w.IsActive, _ => true }))
            .ToList();

    [HttpGet]
    public async Task<IActionResult> Create(string? docType) =>
        await FormView(new ApprovalWorkflowFormViewModel
        {
            DocType = docType ?? "",
            LevelsJson = "[{\"name\":\"Level 1\",\"quorumType\":\"any\",\"approvers\":[]}]"
        });

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ApprovalWorkflowFormViewModel form)
    {
        form.Uid = null;
        return await Save(form);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid uid)
    {
        var w = await _api.GetApprovalWorkflowAsync(uid);
        if (w == null)
        {
            TempData["Error"] = "Alur persetujuan tidak ditemukan.";
            return RedirectToAction(nameof(Index));
        }

        return await FormView(new ApprovalWorkflowFormViewModel
        {
            Uid = w.Uid,
            Name = w.Name,
            DocType = w.DocType,
            MinAmount = w.MinAmount,
            MaxAmount = w.MaxAmount,
            Priority = w.Priority,
            IsActive = w.IsActive,
            Description = w.Description,
            UsedCount = w.UsedCount,
            LevelsJson = JsonSerializer.Serialize(w.Levels.Select(l => new
            {
                name = l.Name,
                quorumType = l.QuorumType,
                quorumCount = l.QuorumCount,
                approvers = l.Approvers.Select(a => new { type = a.Type, id = a.Id })
            }), Json)
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(ApprovalWorkflowFormViewModel form)
    {
        if (form.Uid == null)
        {
            TempData["Error"] = "Alur persetujuan tidak ditemukan.";
            return RedirectToAction(nameof(Index));
        }

        return await Save(form);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid uid)
    {
        var result = await _api.DeleteApprovalWorkflowAsync(uid);
        TempData[result.Success ? "Success" : "Error"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    private async Task<IActionResult> Save(ApprovalWorkflowFormViewModel form)
    {
        List<LevelInput> levels;
        try
        {
            levels = JsonSerializer.Deserialize<List<LevelInput>>(form.LevelsJson ?? "[]", Json) ?? new();
        }
        catch (JsonException)
        {
            levels = new();
        }

        var result = await _api.SaveApprovalWorkflowAsync(form, levels.Select(l => (object)new
        {
            name = l.Name,
            quorumType = l.QuorumType,
            quorumCount = l.QuorumCount,
            approvers = (l.Approvers ?? new()).Select(a => new { type = a.Type, id = a.Id })
        }));

        if (!result.Success)
        {
            TempData["Error"] = result.Message;
            return await FormView(form);
        }

        TempData["Success"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    private async Task<IActionResult> FormView(ApprovalWorkflowFormViewModel form)
    {
        form.Options = await _api.GetApprovalWorkflowOptionsAsync();
        form.Existing = await _api.GetApprovalWorkflowsAsync();
        return View("Form", form);
    }

    private sealed class LevelInput
    {
        public string? Name { get; set; }
        public string QuorumType { get; set; } = "all";
        public int? QuorumCount { get; set; }
        public List<ApproverInput>? Approvers { get; set; }
    }

    private sealed class ApproverInput
    {
        public string Type { get; set; } = "user";
        public long Id { get; set; }
    }
}
