using System.Net.Http.Json;
using ERP.Web.Models;

namespace ERP.Web.Services;

/// <summary>Persetujuan: Kotak Persetujuan, panel dokumen, Alur Persetujuan.</summary>
public partial class AccountingApiClient
{
    // ---------- Kotak Persetujuan ----------

    public async Task<PagedResult<ApprovalRequestViewModel>> GetApprovalsAsync(string tab, string? q, int page, int pageSize = 25) =>
        await GetAsync<PagedResult<ApprovalRequestViewModel>>($"api/approvals?tab={Esc(tab)}&q={Esc(q)}&page={page}&pageSize={pageSize}") ?? new();

    public Task<ApprovalRequestViewModel?> GetApprovalAsync(Guid uid) =>
        GetAsync<ApprovalRequestViewModel>($"api/approvals/{uid}");

    /// <summary>Jumlah pengajuan menunggu user login (badge). Gagal apa pun = 0, tidak mengganggu halaman.</summary>
    public async Task<int> GetApprovalCountAsync()
    {
        try
        {
            var response = await _http.GetAsync("api/approvals/count");
            if (!response.IsSuccessStatusCode)
                return 0;

            var body = await response.Content.ReadFromJsonAsync<ApprovalCountResponse>();
            return body?.Count ?? 0;
        }
        catch (Exception)
        {
            return 0;
        }
    }

    /// <summary>Panel persetujuan dokumen; null bila dokumen/jenis tidak dikenal atau tidak boleh dilihat.</summary>
    public async Task<ApprovalDocumentViewModel?> GetApprovalDocumentAsync(string docType, Guid docUid)
    {
        var response = await _http.GetAsync($"api/approvals/document?docType={Esc(docType)}&docUid={docUid}");

        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            response.EnsureSuccessStatusCode();

        return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<ApprovalDocumentViewModel>() : null;
    }

    public async Task<ApprovalPreviewViewModel> GetApprovalPreviewAsync(string docType, decimal amount) =>
        await GetAsync<ApprovalPreviewViewModel>(
            $"api/approvals/preview?docType={Esc(docType)}&amount={amount.ToString(System.Globalization.CultureInfo.InvariantCulture)}") ?? new();

    public Task<ApiResult> SubmitApprovalAsync(string docType, Guid docUid, string? reason) =>
        PostAsync("api/approvals/submit", new { docType, docUid, reason });

    public Task<ApiResult> ApproveApprovalAsync(Guid uid, string? note) => PostAsync($"api/approvals/{uid}/approve", new { note });

    public Task<ApiResult> RejectApprovalAsync(Guid uid, string? note) => PostAsync($"api/approvals/{uid}/reject", new { note });

    public Task<ApiResult> WithdrawApprovalAsync(Guid uid, string? note) => PostAsync($"api/approvals/{uid}/withdraw", new { note });

    /// <summary>Setujui PO Barang: langsung ordered, atau diajukan ke mesin persetujuan bila ada alur yang cocok.</summary>
    public Task<ApiResult> ApproveGoodsPurchaseOrderAsync(Guid uid) => PostAsync($"api/goods-purchase-orders/{uid}/approve", new { });

    // ---------- Alur Persetujuan ----------

    public async Task<ApprovalWorkflowListViewModel> GetApprovalWorkflowsAsync() =>
        await GetAsync<ApprovalWorkflowListViewModel>("api/approval-workflows") ?? new();

    public Task<ApprovalWorkflowViewModel?> GetApprovalWorkflowAsync(Guid uid) =>
        GetAsync<ApprovalWorkflowViewModel>($"api/approval-workflows/{uid}");

    public async Task<ApprovalWorkflowOptionsViewModel> GetApprovalWorkflowOptionsAsync() =>
        await GetAsync<ApprovalWorkflowOptionsViewModel>("api/approval-workflows/options") ?? new();

    public Task<ApiResult> SaveApprovalWorkflowAsync(ApprovalWorkflowFormViewModel f, IEnumerable<object> levels) =>
        PostAsync(f.Uid == null ? "api/approval-workflows/create" : "api/approval-workflows/update", new
        {
            uid = f.Uid,
            name = f.Name,
            docType = f.DocType,
            minAmount = f.MinAmount,
            maxAmount = f.MaxAmount,
            priority = f.Priority,
            isActive = f.IsActive,
            description = f.Description,
            levels
        });

    public Task<ApiResult> DeleteApprovalWorkflowAsync(Guid uid) => PostAsync("api/approval-workflows/delete", new { uid });

    private sealed class ApprovalCountResponse
    {
        public int Count { get; set; }
    }
}
