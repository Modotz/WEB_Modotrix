using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ERP.Web.Models;

namespace ERP.Web.Services;

/// <summary>Hasil aksi tulis ke API: sukses/gagal, pesan dari API, dan uid record (bila ada).</summary>
public record ApiResult(bool Success, string Message, Guid? Uid = null);

/// <summary>Modul Akuntansi (Bagan Akun, Periode, Jurnal, Saldo Awal, Buku Besar, Neraca Saldo, laporan).</summary>
public partial class AccountingApiClient
{
    private readonly HttpClient _http;

    public AccountingApiClient(HttpClient http)
    {
        _http = http;
    }

    // ---------- Bagan Akun ----------

    public async Task<List<AccountViewModel>> GetAccountsAsync(string? q = null, string? type = null, bool? active = null) =>
        await GetAsync<List<AccountViewModel>>(
            $"api/chart-of-accounts?q={Esc(q)}&type={Esc(type)}&active={active?.ToString().ToLowerInvariant()}") ?? new();

    public async Task<List<AccountOptionViewModel>> GetAccountOptionsAsync(bool postable = true, bool manual = false, string? type = null) =>
        await GetAsync<List<AccountOptionViewModel>>(
            $"api/chart-of-accounts/options?postable={postable.ToString().ToLowerInvariant()}&manual={manual.ToString().ToLowerInvariant()}&type={Esc(type)}") ?? new();

    public Task<AccountViewModel?> GetAccountAsync(Guid uid) =>
        GetAsync<AccountViewModel>($"api/chart-of-accounts/{uid}");

    public Task<ApiResult> SaveAccountAsync(AccountViewModel model) =>
        PostAsync(model.Uid == Guid.Empty ? "api/chart-of-accounts/create" : "api/chart-of-accounts/update", new
        {
            uid = model.Uid == Guid.Empty ? (Guid?)null : model.Uid,
            model.Code,
            model.Name,
            model.Type,
            model.NormalBalance,
            model.ParentId,
            model.IsPostable,
            model.IsActive,
            model.IsControl,
            model.CashFlowCategory,
            model.Description
        });

    public Task<ApiResult> DeleteAccountAsync(Guid uid) =>
        PostAsync("api/chart-of-accounts/delete", new { uid });

    // ---------- Periode ----------

    public async Task<List<AccountingPeriodViewModel>> GetPeriodsAsync(int? year = null) =>
        await GetAsync<List<AccountingPeriodViewModel>>($"api/accounting-periods?year={year}") ?? new();

    public async Task<List<PeriodLockEventViewModel>> GetPeriodEventsAsync(Guid uid) =>
        await GetAsync<List<PeriodLockEventViewModel>>($"api/accounting-periods/{uid}/events") ?? new();

    public Task<ApiResult> GeneratePeriodsAsync(int year) =>
        PostAsync("api/accounting-periods/generate", new { year });

    public Task<ApiResult> SavePeriodAsync(Guid? uid, string name, DateOnly start, DateOnly end, string? note) =>
        PostAsync(uid == null ? "api/accounting-periods/create" : "api/accounting-periods/update",
            new { uid, name, startDate = start, endDate = end, note });

    public Task<ApiResult> DeletePeriodAsync(Guid uid) =>
        PostAsync("api/accounting-periods/delete", new { uid });

    public Task<ApiResult> ClosePeriodAsync(Guid uid, string? note) =>
        PostAsync($"api/accounting-periods/{uid}/close", new { note });

    public Task<ApiResult> ReopenPeriodAsync(Guid uid, string? note) =>
        PostAsync($"api/accounting-periods/{uid}/reopen", new { note });

    // ---------- Jurnal ----------

    public async Task<PagedResult<JournalListItemViewModel>> GetJournalsAsync(JournalFilterViewModel filter) =>
        await GetAsync<PagedResult<JournalListItemViewModel>>(
            $"api/journals?{filter.ToQueryString()}&page={filter.Page}&pageSize={filter.PageSize}") ?? new();

    public async Task<List<JournalViewModel>> GetJournalsForExportAsync(JournalFilterViewModel filter) =>
        await GetAsync<List<JournalViewModel>>($"api/journals/export?{filter.ToQueryString()}") ?? new();

    public Task<JournalViewModel?> GetJournalAsync(Guid uid) =>
        GetAsync<JournalViewModel>($"api/journals/{uid}");

    public Task<ApiResult> SaveJournalAsync(JournalFormViewModel form, bool post)
    {
        var body = new
        {
            uid = form.Uid,
            journalDate = form.JournalDate,
            reference = form.Reference,
            description = form.Description,
            reversalDate = form.ReversalDate,
            post,
            reason = form.Reason,
            lines = form.Lines
                .Where(l => l.AccountId > 0 || l.Debit != 0 || l.Credit != 0)
                .Select(l => new { l.AccountId, l.Description, l.Debit, l.Credit, l.CostCenterId, l.ProjectId })
        };

        var url = form.Mode switch
        {
            "edit" => "api/journals/update",
            "revise" => $"api/journals/{form.Uid}/revise",
            _ => "api/journals/create"
        };

        return PostAsync(url, body);
    }

    public Task<ApiResult> DeleteJournalAsync(Guid uid) => PostAsync("api/journals/delete", new { uid });

    public Task<ApiResult> PostJournalAsync(Guid uid) => PostAsync($"api/journals/{uid}/post", new { });

    public Task<ApiResult> VoidJournalAsync(Guid uid, string? reason, DateOnly? date) =>
        PostAsync($"api/journals/{uid}/void", new { reason, date });

    public Task<ApiResult> ReverseAccrualAsync(Guid uid) => PostAsync($"api/journals/{uid}/reverse-accrual", new { });

    // ---------- Saldo Awal, Buku Besar, Neraca Saldo ----------

    public async Task<OpeningBalanceViewModel> GetOpeningBalanceAsync() =>
        await GetAsync<OpeningBalanceViewModel>("api/opening-balances") ?? new();

    public Task<ApiResult> SaveOpeningBalanceAsync(DateOnly date, IEnumerable<OpeningBalanceLineViewModel> lines) =>
        PostAsync("api/opening-balances/save", new
        {
            date,
            lines = lines.Where(l => l.Debit != 0 || l.Credit != 0).Select(l => new { l.AccountId, l.Debit, l.Credit })
        });

    public Task<GeneralLedgerViewModel?> GetGeneralLedgerAsync(Guid accountUid, DateOnly from, DateOnly to) =>
        GetAsync<GeneralLedgerViewModel>($"api/general-ledger?accountUid={accountUid}&from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}");

    public async Task<TrialBalanceViewModel> GetTrialBalanceAsync(DateOnly from, DateOnly to, bool includeZero) =>
        await GetAsync<TrialBalanceViewModel>(
            $"api/trial-balance?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}&includeZero={includeZero.ToString().ToLowerInvariant()}")
        ?? new TrialBalanceViewModel { From = from, To = to };

    // ---------- Laporan Keuangan & Tutup Buku ----------

    public async Task<IncomeStatementViewModel> GetIncomeStatementAsync(DateOnly from, DateOnly to) =>
        await GetAsync<IncomeStatementViewModel>($"api/income-statement?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}") ?? new();

    public async Task<BalanceSheetViewModel> GetBalanceSheetAsync(DateOnly asOf) =>
        await GetAsync<BalanceSheetViewModel>($"api/balance-sheet?asOf={asOf:yyyy-MM-dd}") ?? new();

    public async Task<CashFlowViewModel> GetCashFlowAsync(DateOnly from, DateOnly to) =>
        await GetAsync<CashFlowViewModel>($"api/cash-flow?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}") ?? new();

    public async Task<EquityChangesViewModel> GetEquityChangesAsync(DateOnly from, DateOnly to) =>
        await GetAsync<EquityChangesViewModel>($"api/equity-changes?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}") ?? new();

    public async Task<List<ClosingYearViewModel>> GetClosingYearsAsync() =>
        await GetAsync<List<ClosingYearViewModel>>("api/closing-book") ?? new();

    public Task<ClosingPreviewViewModel?> GetClosingPreviewAsync(int year) =>
        GetAsync<ClosingPreviewViewModel>($"api/closing-book/{year}");

    public Task<ApiResult> CloseYearAsync(int year) => PostAsync("api/closing-book/close", new { year });

    public Task<ApiResult> CancelClosingAsync(int year, string? reason) => PostAsync("api/closing-book/cancel", new { year, reason });

    // ---------- Kas & Bank ----------

    public async Task<List<CashAccountViewModel>> GetCashAccountsAsync() =>
        await GetAsync<List<CashAccountViewModel>>("api/cash-accounts") ?? new();

    public async Task<List<CashAccountViewModel>> GetCashAccountOptionsAsync() =>
        await GetAsync<List<CashAccountViewModel>>("api/cash-accounts/options") ?? new();

    public Task<ApiResult> SaveCashAccountAsync(CashAccountViewModel m) =>
        PostAsync(m.Uid == Guid.Empty ? "api/cash-accounts/create" : "api/cash-accounts/update", new
        {
            uid = m.Uid == Guid.Empty ? (Guid?)null : m.Uid,
            m.Code, m.Name, m.Type, m.CoaAccountId, m.BankName, m.AccountNo, m.IsActive, m.Note, m.CurrencyCode,
            m.ReconcileEnabled, m.ReconcileFrom
        });

    public Task<ApiResult> DeleteCashAccountAsync(Guid uid) => PostAsync("api/cash-accounts/delete", new { uid });

    public async Task<PagedResult<CashTransactionViewModel>> GetCashTransactionsAsync(CashTransactionFilterViewModel f) =>
        await GetAsync<PagedResult<CashTransactionViewModel>>($"api/cash-transactions?{f.ToQueryString()}&page={f.Page}&pageSize={f.PageSize}") ?? new();

    public async Task<List<CashTransactionViewModel>> GetCashTransactionsForExportAsync(CashTransactionFilterViewModel f) =>
        await GetAsync<List<CashTransactionViewModel>>($"api/cash-transactions/export?{f.ToQueryString()}") ?? new();

    public Task<CashTransactionViewModel?> GetCashTransactionAsync(Guid uid) =>
        GetAsync<CashTransactionViewModel>($"api/cash-transactions/{uid}");

    public Task<ApiResult> SaveCashTransactionAsync(CashTransactionFormViewModel m, bool post) =>
        PostAsync(m.Uid == null ? "api/cash-transactions/create" : "api/cash-transactions/update", new
        {
            uid = m.Uid, trxDate = m.TrxDate, m.Type, m.CashAccountId, m.DestCashAccountId, m.CounterAccountId,
            m.CostCenterId, m.ProjectId, m.Amount, m.FxRate, m.Description, m.Reference, post
        });

    public Task<ApiResult> DeleteCashTransactionAsync(Guid uid) => PostAsync("api/cash-transactions/delete", new { uid });

    public Task<ApiResult> PostCashTransactionAsync(Guid uid) => PostAsync($"api/cash-transactions/{uid}/post", new { });

    public Task<ApiResult> VoidCashTransactionAsync(Guid uid, string? reason, DateOnly? date) =>
        PostAsync($"api/cash-transactions/{uid}/void", new { reason, date });

    // ---------- Kas Kecil ----------

    public async Task<PettyCashSummaryViewModel> GetPettyCashSummaryAsync() =>
        await GetAsync<PettyCashSummaryViewModel>("api/petty-cash") ?? new();

    public async Task<PagedResult<CashTransactionViewModel>> GetPettyCashTransactionsAsync(DateOnly? from, DateOnly? to, int page, int pageSize = 25) =>
        await GetAsync<PagedResult<CashTransactionViewModel>>(
            $"api/petty-cash/transactions?dateFrom={from:yyyy-MM-dd}&dateTo={to:yyyy-MM-dd}&page={page}&pageSize={pageSize}") ?? new();

    public Task<ApiResult> PettyCashTopupAsync(DateOnly date, long sourceId, decimal amount, string? description, string? reference) =>
        PostAsync("api/petty-cash/create-topup", new { trxDate = date, cashAccountId = sourceId, amount, description, reference });

    public async Task<ApiResult> PettyCashExpenseAsync(DateOnly date, long counterAccountId, decimal amount, string? description,
        string? reference, bool post, IFormFile? attachment, long? costCenterId = null, long? projectId = null)
    {
        using var content = new MultipartFormDataContent
        {
            { new StringContent(date.ToString("yyyy-MM-dd")), "TrxDate" },
            { new StringContent(counterAccountId.ToString()), "CounterAccountId" },
            { new StringContent(costCenterId?.ToString() ?? ""), "CostCenterId" },
            { new StringContent(projectId?.ToString() ?? ""), "ProjectId" },
            { new StringContent(amount.ToString(System.Globalization.CultureInfo.InvariantCulture)), "Amount" },
            { new StringContent(description ?? ""), "Description" },
            { new StringContent(reference ?? ""), "Reference" },
            { new StringContent(post ? "true" : "false"), "Post" }
        };

        if (attachment != null && attachment.Length > 0)
        {
            var file = new StreamContent(attachment.OpenReadStream());
            file.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(
                string.IsNullOrEmpty(attachment.ContentType) ? "application/octet-stream" : attachment.ContentType);
            content.Add(file, "attachment", attachment.FileName);
        }

        return await ResultFromAsync(await _http.PostAsync("api/petty-cash/create-expense", content));
    }

    public Task<ApiResult> PostPettyCashAsync(Guid uid) => PostAsync($"api/petty-cash/{uid}/post", new { });

    public Task<ApiResult> VoidPettyCashAsync(Guid uid, string? reason, DateOnly? date) =>
        PostAsync($"api/petty-cash/{uid}/void", new { reason, date });

    public Task<ApiResult> DeletePettyCashAsync(Guid uid) => PostAsync("api/petty-cash/delete", new { uid });

    // ---------- helper ----------

    private async Task<T?> GetAsync<T>(string url)
    {
        var response = await _http.GetAsync(url);

        if (response.StatusCode == HttpStatusCode.NotFound)
            return default;

        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            response.EnsureSuccessStatusCode(); // ditangani ApiUnauthorizedExceptionFilter

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException(await ReadMessageAsync(response) ?? $"API mengembalikan status {(int)response.StatusCode}.");

        return await response.Content.ReadFromJsonAsync<T>();
    }

    private async Task<ApiResult> PostAsync(string url, object body) =>
        await ResultFromAsync(await _http.PostAsJsonAsync(url, body));

    private static async Task<ApiResult> ResultFromAsync(HttpResponseMessage response)
    {
        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            response.EnsureSuccessStatusCode();

        var json = await ReadJsonAsync(response);
        var message = json is { ValueKind: JsonValueKind.Object } && json.Value.TryGetProperty("message", out var m)
            ? m.GetString()
            : null;
        Guid? uid = json is { ValueKind: JsonValueKind.Object } && json.Value.TryGetProperty("uid", out var u)
                    && Guid.TryParse(u.GetString(), out var g)
            ? g
            : null;

        return new ApiResult(
            response.IsSuccessStatusCode,
            message ?? (response.IsSuccessStatusCode ? "Berhasil." : $"Gagal ({(int)response.StatusCode})."),
            uid);
    }

    private static async Task<JsonElement?> ReadJsonAsync(HttpResponseMessage response)
    {
        try
        {
            return await response.Content.ReadFromJsonAsync<JsonElement>();
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static async Task<string?> ReadMessageAsync(HttpResponseMessage response)
    {
        var json = await ReadJsonAsync(response);

        return json is { ValueKind: JsonValueKind.Object } && json.Value.TryGetProperty("message", out var m) ? m.GetString() : null;
    }

    private static string Esc(string? value) => string.IsNullOrWhiteSpace(value) ? "" : Uri.EscapeDataString(value);
}
