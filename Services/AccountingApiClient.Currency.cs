using ERP.Web.Models;

namespace ERP.Web.Services;

/// <summary>Multi-currency: mata uang, kurs harian, lookup kurs untuk form dokumen, revaluasi akhir periode.</summary>
public partial class AccountingApiClient
{
    // ---------- Mata uang ----------

    public async Task<List<CurrencyViewModel>> GetCurrenciesAsync() =>
        await GetAsync<List<CurrencyViewModel>>("api/currencies") ?? new();

    /// <summary>Mata uang aktif + kurs terakhir (dropdown form dokumen; cukup login).</summary>
    public async Task<List<CurrencyViewModel>> GetCurrencyOptionsAsync() =>
        await GetAsync<List<CurrencyViewModel>>("api/currencies/options") ?? new();

    public Task<ApiResult> SaveCurrencyAsync(CurrencyViewModel m) =>
        PostAsync(m.Uid == Guid.Empty ? "api/currencies/create" : "api/currencies/update", new
        {
            uid = m.Uid == Guid.Empty ? (Guid?)null : m.Uid,
            m.Code, m.Name, m.Symbol, m.IsActive
        });

    public Task<ApiResult> DeleteCurrencyAsync(Guid uid) => PostAsync("api/currencies/delete", new { uid });

    // ---------- Kurs ----------

    public async Task<PagedResult<ExchangeRateViewModel>> GetExchangeRatesAsync(ExchangeRateFilterViewModel f) =>
        await GetAsync<PagedResult<ExchangeRateViewModel>>($"api/exchange-rates?{f.ToQueryString()}&page={f.Page}&pageSize={f.PageSize}") ?? new();

    public async Task<List<ExchangeRateViewModel>> GetExchangeRatesForExportAsync(ExchangeRateFilterViewModel f) =>
        await GetAsync<List<ExchangeRateViewModel>>($"api/exchange-rates/export?{f.ToQueryString()}") ?? new();

    /// <summary>Kurs terakhir pada/sebelum tanggal; null bila belum ada.</summary>
    public Task<RateLookupViewModel?> LookupRateAsync(string currency, DateOnly date) =>
        GetAsync<RateLookupViewModel>($"api/exchange-rates/lookup?currency={Esc(currency)}&date={date:yyyy-MM-dd}");

    public Task<ApiResult> SaveExchangeRateAsync(Guid? uid, string currencyCode, DateOnly rateDate, decimal rate, string? note) =>
        PostAsync(uid == null ? "api/exchange-rates/create" : "api/exchange-rates/update", new { uid, currencyCode, rateDate, rate, note });

    public Task<ApiResult> SaveDailyRatesAsync(DateOnly rateDate, IEnumerable<(string Currency, decimal? Rate)> rates) =>
        PostAsync("api/exchange-rates/daily", new
        {
            rateDate,
            rates = rates.Select(r => new { currencyCode = r.Currency, rate = r.Rate })
        });

    public Task<ApiResult> DeleteExchangeRateAsync(Guid uid) => PostAsync("api/exchange-rates/delete", new { uid });

    // ---------- Revaluasi ----------

    public async Task<List<FxRevaluationViewModel>> GetFxRevaluationsAsync() =>
        await GetAsync<List<FxRevaluationViewModel>>("api/fx-revaluations") ?? new();

    public async Task<FxRevaluationPreviewViewModel> GetFxRevaluationPreviewAsync(DateOnly? date) =>
        await GetAsync<FxRevaluationPreviewViewModel>($"api/fx-revaluations/preview?date={date:yyyy-MM-dd}") ?? new();

    public Task<FxRevaluationViewModel?> GetFxRevaluationAsync(Guid uid) =>
        GetAsync<FxRevaluationViewModel>($"api/fx-revaluations/{uid}");

    public Task<ApiResult> CreateFxRevaluationAsync(DateOnly revalDate, string? note) =>
        PostAsync("api/fx-revaluations/create", new { revalDate, note });

    public Task<ApiResult> VoidFxRevaluationAsync(Guid uid, string? reason) =>
        PostAsync($"api/fx-revaluations/{uid}/void", new { reason });
}
