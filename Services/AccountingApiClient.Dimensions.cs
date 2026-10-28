using System.Net.Http.Json;
using ERP.Web.Models;

namespace ERP.Web.Services;

/// <summary>Level 11: cost center, project, anggaran, alokasi biaya &amp; laporan per dimensi.</summary>
public partial class AccountingApiClient
{
    // ---------- Cost center ----------

    public async Task<List<CostCenterViewModel>> GetCostCentersAsync() =>
        await GetAsync<List<CostCenterViewModel>>("api/cost-centers") ?? new();

    public Task<ApiResult> SaveCostCenterAsync(CostCenterViewModel m) =>
        PostAsync(m.Uid == Guid.Empty ? "api/cost-centers/create" : "api/cost-centers/update", new
        {
            uid = m.Uid == Guid.Empty ? (Guid?)null : m.Uid,
            m.Code,
            m.Name,
            m.Description,
            m.IsActive
        });

    public Task<ApiResult> DeleteCostCenterAsync(Guid uid) => PostAsync("api/cost-centers/delete", new { uid });

    // ---------- Project ----------

    public async Task<List<ProjectViewModel>> GetProjectsAsync(string? status = null) =>
        await GetAsync<List<ProjectViewModel>>($"api/projects?status={Esc(status)}") ?? new();

    public async Task<List<CustomerOptionViewModel>> GetProjectCustomersAsync() =>
        await GetAsync<List<CustomerOptionViewModel>>("api/projects/customers") ?? new();

    public Task<ApiResult> SaveProjectAsync(ProjectViewModel m) =>
        PostAsync(m.Uid == Guid.Empty ? "api/projects/create" : "api/projects/update", new
        {
            uid = m.Uid == Guid.Empty ? (Guid?)null : m.Uid,
            m.Code,
            m.Name,
            m.ProjectType,
            customerId = m.ProjectType == "client" ? m.CustomerId : null,
            m.Status,
            m.StartDate,
            m.EndDate,
            m.EstimatedBudget,
            m.Description
        });

    public Task<ApiResult> DeleteProjectAsync(Guid uid) => PostAsync("api/projects/delete", new { uid });

    /// <summary>Pilihan dimensi untuk form dokumen: aktif/open + yang sedang dipakai dokumen.</summary>
    public async Task<DimensionOptions> GetDimensionOptionsAsync(long? costCenterId = null, long? projectId = null) => new()
    {
        CostCenters = await GetAsync<List<DimensionOptionViewModel>>($"api/cost-centers/options?include={costCenterId}") ?? new(),
        Projects = await GetAsync<List<DimensionOptionViewModel>>($"api/projects/options?include={projectId}") ?? new()
    };

    /// <summary>Pilihan untuk form dengan banyak baris (jurnal umum): sertakan semua dimensi yang dipakai baris.</summary>
    public async Task<DimensionOptions> GetDimensionOptionsAsync(IEnumerable<long?> costCenterIds, IEnumerable<long?> projectIds)
    {
        var result = await GetDimensionOptionsAsync();

        foreach (var id in costCenterIds.Where(i => i > 0).Distinct().Where(i => result.CostCenters.All(c => c.Id != i)))
            result.CostCenters.AddRange((await GetAsync<List<DimensionOptionViewModel>>($"api/cost-centers/options?include={id}") ?? new())
                .Where(c => c.Id == id));

        foreach (var id in projectIds.Where(i => i > 0).Distinct().Where(i => result.Projects.All(p => p.Id != i)))
            result.Projects.AddRange((await GetAsync<List<DimensionOptionViewModel>>($"api/projects/options?include={id}") ?? new())
                .Where(p => p.Id == id));

        return result;
    }

    // ---------- Anggaran ----------

    public async Task<List<BudgetViewModel>> GetBudgetsAsync(int year, string? against = null) =>
        await GetAsync<List<BudgetViewModel>>($"api/budgets?year={year}&against={Esc(against)}") ?? new();

    public Task<ApiResult> SaveBudgetAsync(BudgetViewModel m) =>
        PostAsync(m.Uid == Guid.Empty ? "api/budgets/create" : "api/budgets/update", new
        {
            uid = m.Uid == Guid.Empty ? (Guid?)null : m.Uid,
            m.Name,
            m.BudgetAgainst,
            costCenterId = m.BudgetAgainst == "cost_center" ? m.CostCenterId : null,
            projectId = m.BudgetAgainst == "project" ? m.ProjectId : null,
            m.AccountId,
            m.FiscalYear,
            m.Amount,
            m.Note
        });

    public Task<ApiResult> DeleteBudgetAsync(Guid uid) => PostAsync("api/budgets/delete", new { uid });

    /// <summary>Cek anggaran baris beban faktur (JSON mentah diteruskan ke form).</summary>
    public async Task<string?> CheckBudgetJsonAsync(long accountId, long? costCenterId, long? projectId, DateOnly date, decimal amount, Guid? invoiceUid)
    {
        var response = await _http.PostAsJsonAsync("api/budgets/check", new { accountId, costCenterId, projectId, date, amount, invoiceUid });
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            response.EnsureSuccessStatusCode();

        return response.IsSuccessStatusCode ? await response.Content.ReadAsStringAsync() : null;
    }

    // ---------- Alokasi biaya ----------

    public async Task<List<CostAllocationViewModel>> GetCostAllocationsAsync(string? status = null) =>
        await GetAsync<List<CostAllocationViewModel>>($"api/cost-allocations?status={Esc(status)}") ?? new();

    public Task<CostAllocationViewModel?> GetCostAllocationAsync(Guid uid) =>
        GetAsync<CostAllocationViewModel>($"api/cost-allocations/{uid}");

    private static object AllocationBody(CostAllocationFormViewModel m, bool post) => new
    {
        uid = m.Uid,
        m.Name,
        m.FromCostCenterId,
        m.AllocationDate,
        m.Description,
        lines = m.Lines.Where(l => l.ToCostCenterId > 0 || l.Percentage != 0).Select(l => new { l.ToCostCenterId, l.Percentage }),
        post
    };

    public Task<ApiResult> SaveCostAllocationAsync(CostAllocationFormViewModel m, bool post) =>
        PostAsync(m.Uid == null ? "api/cost-allocations/create" : "api/cost-allocations/update", AllocationBody(m, post));

    /// <summary>Preview basis &amp; bagian per penerima (JSON mentah; 400 berisi pesan validasi).</summary>
    public async Task<(bool Ok, string Json)> PreviewCostAllocationJsonAsync(CostAllocationFormViewModel m)
    {
        var response = await _http.PostAsJsonAsync("api/cost-allocations/preview", AllocationBody(m, false));
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            response.EnsureSuccessStatusCode();

        return (response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
    }

    public Task<ApiResult> PostCostAllocationAsync(Guid uid) => PostAsync($"api/cost-allocations/{uid}/post", new { });

    public Task<ApiResult> VoidCostAllocationAsync(Guid uid, string? reason, DateOnly? date) =>
        PostAsync($"api/cost-allocations/{uid}/void", new { reason, date });

    public Task<ApiResult> DeleteCostAllocationAsync(Guid uid) => PostAsync("api/cost-allocations/delete", new { uid });

    // ---------- Laporan ----------

    public async Task<CostCenterPnlViewModel> GetCostCenterPnlAsync(DateOnly from, DateOnly to) =>
        await GetAsync<CostCenterPnlViewModel>($"api/reports/cost-center-pnl?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}") ?? new();

    public async Task<ProjectProfitabilityViewModel> GetProjectProfitabilityAsync(DateOnly from, DateOnly to) =>
        await GetAsync<ProjectProfitabilityViewModel>($"api/reports/project-profitability?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}") ?? new();

    public Task<ProjectProfitDetailViewModel?> GetProjectProfitDetailAsync(Guid uid, DateOnly from, DateOnly to) =>
        GetAsync<ProjectProfitDetailViewModel>($"api/reports/project-profitability/{uid}?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}");

    public async Task<BudgetVsActualViewModel> GetBudgetVsActualAsync(int year) =>
        await GetAsync<BudgetVsActualViewModel>($"api/reports/budget-vs-actual?year={year}") ?? new();
}
