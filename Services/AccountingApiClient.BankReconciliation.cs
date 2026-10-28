using System.Net.Http.Json;
using ERP.Web.Models;

namespace ERP.Web.Services;

/// <summary>Level 12: rekonsiliasi bank.</summary>
public partial class AccountingApiClient
{
    public async Task<List<BankStatementListViewModel>> GetBankStatementsAsync(long? cashAccountId, string? status) =>
        await GetAsync<List<BankStatementListViewModel>>($"api/bank-reconciliations?cashAccountId={cashAccountId}&status={Esc(status)}") ?? new();

    public async Task<List<CashAccountViewModel>> GetReconcilableAccountsAsync() =>
        await GetAsync<List<CashAccountViewModel>>("api/bank-reconciliations/accounts") ?? new();

    /// <summary>Usulan header rekening koran berikutnya (JSON mentah untuk form).</summary>
    public async Task<string?> GetBankStatementDefaultsJsonAsync(long cashAccountId)
    {
        var response = await _http.GetAsync($"api/bank-reconciliations/defaults?cashAccountId={cashAccountId}");
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            response.EnsureSuccessStatusCode();

        return response.IsSuccessStatusCode ? await response.Content.ReadAsStringAsync() : null;
    }

    public Task<BankStatementViewModel?> GetBankStatementAsync(Guid uid) =>
        GetAsync<BankStatementViewModel>($"api/bank-reconciliations/{uid}");

    public Task<ApiResult> SaveBankStatementAsync(BankStatementFormViewModel m) =>
        PostAsync(m.Uid == null ? "api/bank-reconciliations/create" : "api/bank-reconciliations/update", new
        {
            m.Uid, m.CashAccountId, m.Reference, m.PeriodFrom, m.StatementDate, m.OpeningBalance, m.ClosingBalance, m.Note
        });

    public Task<ApiResult> DeleteBankStatementAsync(Guid uid) => PostAsync("api/bank-reconciliations/delete", new { uid });

    public Task<ApiResult> SaveBankStatementLineAsync(Guid uid, BankStatementLineFormViewModel m) =>
        PostAsync($"api/bank-reconciliations/{uid}/lines/save", new { m.Uid, m.LineDate, m.Description, m.Reference, m.Amount, m.Direction });

    public Task<ApiResult> DeleteBankStatementLineAsync(Guid uid, Guid lineUid) =>
        PostAsync($"api/bank-reconciliations/{uid}/lines/delete", new { uid = lineUid });

    public Task<ApiResult> MatchBankLineAsync(Guid uid, Guid lineUid, long journalLineId) =>
        PostAsync($"api/bank-reconciliations/{uid}/match", new { lineUid, journalLineId });

    public Task<ApiResult> UnmatchBankLineAsync(Guid uid, Guid lineUid) =>
        PostAsync($"api/bank-reconciliations/{uid}/unmatch", new { uid = lineUid });

    public Task<ApiResult> CompleteBankStatementAsync(Guid uid) => PostAsync($"api/bank-reconciliations/{uid}/complete", new { });

    public async Task<OutstandingBankViewModel> GetBankOutstandingAsync(DateOnly asOf, long? cashAccountId) =>
        await GetAsync<OutstandingBankViewModel>($"api/reports/bank-outstanding?asOf={asOf:yyyy-MM-dd}&cashAccountId={cashAccountId}") ?? new();
}
