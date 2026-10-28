using ERP.Web.Models;

namespace ERP.Web.Services;

/// <summary>Level 13: adjusting entries — akrual, prepaid/deferred &amp; rekognisi, cadangan kerugian piutang.</summary>
public partial class AccountingApiClient
{
    public async Task<List<AccrualJournalViewModel>> GetAccrualsAsync(DateOnly? from, DateOnly? to, string? status) =>
        await GetAsync<List<AccrualJournalViewModel>>($"api/reports/accruals?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}&status={Esc(status)}") ?? new();

    // ---------- Prepaid / deferred ----------

    public async Task<DeferralSchedulesIndexViewModel> GetDeferralSchedulesAsync(DateOnly asOf, string? side, string? status) =>
        await GetAsync<DeferralSchedulesIndexViewModel>($"api/deferral-schedules?asOf={asOf:yyyy-MM-dd}&side={Esc(side)}&status={Esc(status)}") ?? new();

    public Task<DeferralScheduleViewModel?> GetDeferralScheduleAsync(Guid uid) =>
        GetAsync<DeferralScheduleViewModel>($"api/deferral-schedules/{uid}");

    public Task<ApiResult> RescheduleDeferralAsync(Guid uid, int months) =>
        PostAsync($"api/deferral-schedules/{uid}/reschedule", new { months });

    public async Task<DeferralRunViewModel> GetDeferralRunAsync(string? period) =>
        await GetAsync<DeferralRunViewModel>($"api/deferral-recognition?period={Esc(period)}") ?? new();

    public Task<ApiResult> RunDeferralRecognitionAsync(string period, string? note) =>
        PostAsync("api/deferral-recognition/run", new { period, note });

    public Task<ApiResult> ReverseLastDeferralRecognitionAsync(string? reason) =>
        PostAsync("api/deferral-recognition/reverse-last", new { reason });

    // ---------- Cadangan kerugian piutang ----------

    public async Task<DoubtfulReceivablesViewModel> GetDoubtfulReceivablesAsync(DateOnly asOf) =>
        await GetAsync<DoubtfulReceivablesViewModel>($"api/doubtful-receivables?asOf={asOf:yyyy-MM-dd}") ?? new();

    public Task<ApiResult> EstimateAllowanceAsync(DateOnly date, decimal amount, string? note) =>
        PostAsync("api/doubtful-receivables/estimate", new { date, amount, note });

    public Task<ApiResult> ReverseAllowanceEstimateAsync(Guid uid, DateOnly? date, string? reason) =>
        PostAsync("api/doubtful-receivables/estimate/reverse", new { uid, date, reason });

    public Task<ApiResult> WriteOffReceivableAsync(Guid invoiceUid, DateOnly date, decimal amount, string method, string? reason) =>
        PostAsync("api/doubtful-receivables/write-off", new { invoiceUid, date, amount, method, reason });

    public Task<ApiResult> VoidWriteOffAsync(Guid uid, DateOnly? date, string? reason) =>
        PostAsync("api/doubtful-receivables/write-off/void", new { uid, date, reason });
}
