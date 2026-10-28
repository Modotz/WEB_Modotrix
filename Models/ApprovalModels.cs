namespace ERP.Web.Models;

// ===================== Persetujuan (mesin approval) =====================

/// <summary>Label &amp; warna status untuk pengajuan, level, dan step persetujuan.</summary>
public static class Appr
{
    public static (string Label, string Css, string Icon) RequestBadge(string? status) => status switch
    {
        "pending" => ("Menunggu", "bg-warning text-dark", "bi-hourglass-split"),
        "approved" => ("Disetujui", "bg-success", "bi-check-circle"),
        "rejected" => ("Ditolak", "bg-danger", "bi-x-circle"),
        "cancelled" => ("Ditarik", "bg-secondary", "bi-slash-circle"),
        _ => ("-", "bg-secondary", "bi-dot")
    };

    public static (string Label, string Css) LevelBadge(string? status) => status switch
    {
        "pending" => ("Sedang berjalan", "bg-info text-dark"),
        "approved" => ("Terpenuhi", "bg-success"),
        "rejected" => ("Ditolak", "bg-danger"),
        "skipped" => ("Dilewati", "bg-secondary"),
        _ => ("Menunggu level sebelumnya", "bg-secondary")
    };

    public static (string Label, string Css) StepBadge(string? status) => status switch
    {
        "approved" => ("Setuju", "bg-success"),
        "rejected" => ("Tolak", "bg-danger"),
        "skipped" => ("Dilewati", "bg-secondary"),
        _ => ("Menunggu", "bg-light text-dark")
    };

    public static string DecisionLabel(string? decision) => decision switch
    {
        "submit" => "Mengajukan",
        "approve" => "Menyetujui",
        "reject" => "Menolak",
        "cancel" => "Menarik pengajuan",
        _ => decision ?? "-"
    };

    /// <summary>Teks quorum (kunci kamus): ALL / ANY / COUNT n.</summary>
    public static string Quorum(string? type) => type switch
    {
        "any" => "ANY — cukup 1 orang",
        "count" => "COUNT — {0} orang berbeda",
        _ => "ALL — semua approver"
    };

    /// <summary>Status dokumen modul (kunci kamus).</summary>
    public static string DocStatusLabel(string? status) => status switch
    {
        "draft" => "Draft",
        "posted" => "Posted",
        "void" => "Dibatalkan",
        "pending_approval" => "Menunggu persetujuan",
        "submitted" => "Diajukan",
        "approved" => "Disetujui",
        "ordered" => "Dipesan",
        "partial" => "Diterima sebagian",
        "received" => "Diterima",
        "cancelled" => "Dibatalkan",
        _ => status ?? "-"
    };

    public static readonly (string Value, string Label)[] QuorumTypes =
    {
        ("all", "ALL — semua approver"), ("any", "ANY — cukup 1 orang"), ("count", "COUNT — n orang")
    };
}

public class ApprovalDocTypeViewModel
{
    public string DocType { get; set; } = "";
    public string Label { get; set; } = "";
    public bool Available { get; set; }
}

public class ApprovalStepViewModel
{
    public string UserName { get; set; } = "";
    public string? RoleName { get; set; }
    public string Status { get; set; } = "";
    public DateTime? ActedAt { get; set; }
    public string? Note { get; set; }
}

public class ApprovalRequestLevelViewModel
{
    public int StepLevel { get; set; }
    public string? Name { get; set; }
    public string QuorumType { get; set; } = "all";
    public int? QuorumCount { get; set; }
    public int RequiredCount { get; set; }
    public int ApprovedCount { get; set; }
    public int PoolSize { get; set; }
    public string Status { get; set; } = "";
    public DateTime? DecidedAt { get; set; }
    public List<ApprovalStepViewModel> Steps { get; set; } = new();
}

public class ApprovalActionViewModel
{
    public string? UserName { get; set; }
    public int? StepLevel { get; set; }
    public string Decision { get; set; } = "";
    public string? Note { get; set; }
    public DateTime ActedAt { get; set; }
}

public class ApprovalRequestViewModel
{
    public Guid Uid { get; set; }
    public string DocType { get; set; } = "";
    public string DocTypeLabel { get; set; } = "";
    public Guid? DocUid { get; set; }
    public string? DocNo { get; set; }
    public decimal Amount { get; set; }
    public string? Reason { get; set; }
    public DateOnly? ActionDate { get; set; }
    public string Status { get; set; } = "";
    public string? WorkflowName { get; set; }
    public int? CurrentLevel { get; set; }
    public int LevelCount { get; set; }
    public string? CurrentLevelName { get; set; }
    public int CurrentApproved { get; set; }
    public int CurrentRequired { get; set; }
    public string? RequestedByName { get; set; }
    public DateTime? RequestedAt { get; set; }
    public DateTime? DecidedAt { get; set; }
    public string? DocCreatedByName { get; set; }
    public string DetailPath { get; set; } = "";
    public string? DocMenuUrl { get; set; }
    public bool WaitingForMe { get; set; }
    public bool CanDecide { get; set; }
    public bool CanWithdraw { get; set; }
    public string? DecideBlockedReason { get; set; }
    public List<ApprovalRequestLevelViewModel> Levels { get; set; } = new();
    public List<ApprovalActionViewModel> Actions { get; set; } = new();
}

public class ApprovalPreviewLevelViewModel
{
    public int StepLevel { get; set; }
    public string? Name { get; set; }
    public string QuorumType { get; set; } = "all";
    public int? QuorumCount { get; set; }
    public int PoolSize { get; set; }
    public List<string> Approvers { get; set; } = new();
}

public class ApprovalPreviewViewModel
{
    public bool Required { get; set; }
    public Guid? WorkflowUid { get; set; }
    public string? WorkflowName { get; set; }
    public List<ApprovalPreviewLevelViewModel> Levels { get; set; } = new();
}

/// <summary>Data panel persetujuan dokumen (partial _ApprovalPanel).</summary>
public class ApprovalDocumentViewModel
{
    public string DocType { get; set; } = "";
    public string DocTypeLabel { get; set; } = "";
    public Guid DocUid { get; set; }
    public string? DocNo { get; set; }
    public decimal Amount { get; set; }
    public string? DocStatus { get; set; }
    public string? DocCreatedByName { get; set; }
    public string DetailPath { get; set; } = "";
    public bool RequiresApproval { get; set; }
    public bool HasPending { get; set; }
    public bool CanSubmit { get; set; }
    public ApprovalPreviewViewModel Preview { get; set; } = new();
    public List<ApprovalRequestViewModel> Requests { get; set; } = new();

    /// <summary>Judul panel (opsional, mis. "Persetujuan Pembatalan").</summary>
    public string? Title { get; set; }

    /// <summary>URL kembali setelah aksi (halaman dokumen).</summary>
    public string? ReturnUrl { get; set; }

    /// <summary>Tampilkan panel walau belum pernah diajukan dan tidak butuh persetujuan.</summary>
    public bool AlwaysShow { get; set; }

    public bool ShouldRender => AlwaysShow || Requests.Count > 0 || RequiresApproval;
}

public class ApprovalsIndexViewModel
{
    public string Tab { get; set; } = "mine";
    public string? Q { get; set; }
    public bool IsAdmin { get; set; }
    public int WaitingCount { get; set; }
    public PagedResult<ApprovalRequestViewModel> Requests { get; set; } = new();
}

public class ApprovalRequestPageViewModel
{
    public ApprovalRequestViewModel Request { get; set; } = new();
}

// ===================== Alur Persetujuan =====================

public class ApprovalApproverViewModel
{
    public string Type { get; set; } = "user";
    public long Id { get; set; }
    public string? Name { get; set; }
    public int Members { get; set; }
}

public class ApprovalLevelDefViewModel
{
    public int StepLevel { get; set; }
    public string? Name { get; set; }
    public string QuorumType { get; set; } = "all";
    public int? QuorumCount { get; set; }
    public int PoolSize { get; set; }
    public List<ApprovalApproverViewModel> Approvers { get; set; } = new();
}

public class ApprovalWorkflowViewModel
{
    public Guid Uid { get; set; }
    public string Name { get; set; } = "";
    public string DocType { get; set; } = "";
    public string DocTypeLabel { get; set; } = "";
    public bool Available { get; set; }
    public string Mode { get; set; } = "serial";
    public decimal MinAmount { get; set; }
    public decimal? MaxAmount { get; set; }
    public int Priority { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Description { get; set; }
    public int UsedCount { get; set; }
    public int LevelCount { get; set; }
    public int ApproverCount { get; set; }
    public List<ApprovalLevelDefViewModel> Levels { get; set; } = new();
}

public class ApprovalCoverageGapViewModel
{
    public string DocType { get; set; } = "";
    public string Label { get; set; } = "";
    public decimal From { get; set; }
    public decimal? To { get; set; }
}

public class ApprovalWorkflowListViewModel
{
    public List<ApprovalWorkflowViewModel> Data { get; set; } = new();
    public List<ApprovalCoverageGapViewModel> Gaps { get; set; } = new();
}

public class ApprovalUserOptionViewModel
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public string Username { get; set; } = "";
    public string? RoleName { get; set; }
}

public class ApprovalRoleOptionViewModel
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public int Members { get; set; }
}

public class ApprovalWorkflowOptionsViewModel
{
    public List<ApprovalDocTypeViewModel> DocTypes { get; set; } = new();
    public List<ApprovalUserOptionViewModel> Users { get; set; } = new();
    public List<ApprovalRoleOptionViewModel> Roles { get; set; } = new();
}

/// <summary>Form alur: field biasa + level (JSON dari editor level di halaman).</summary>
public class ApprovalWorkflowFormViewModel
{
    public Guid? Uid { get; set; }
    public string? Name { get; set; }
    public string DocType { get; set; } = "";
    public decimal MinAmount { get; set; }
    public decimal? MaxAmount { get; set; }
    public int Priority { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Description { get; set; }
    public int UsedCount { get; set; }

    /// <summary>JSON array level: [{name, quorumType, quorumCount, approvers:[{type,id}]}].</summary>
    public string LevelsJson { get; set; } = "[]";

    public ApprovalWorkflowOptionsViewModel Options { get; set; } = new();

    /// <summary>Alur lain (untuk peringatan celah/tumpang-tindih rentang nominal di form).</summary>
    public ApprovalWorkflowListViewModel Existing { get; set; } = new();
}
