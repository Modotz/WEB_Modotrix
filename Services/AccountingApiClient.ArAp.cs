using System.Net.Http.Json;
using ERP.Web.Models;

namespace ERP.Web.Services;

/// <summary>Hutang/piutang: faktur, pembayaran, umur, kartu &amp; PPN (apiBase dari <see cref="ArApPageConfig"/>).</summary>
public partial class AccountingApiClient
{
    // ---------- Faktur ----------

    public async Task<PagedResult<InvoiceViewModel>> GetInvoicesAsync(string apiBase, ArApFilterViewModel f) =>
        await GetAsync<PagedResult<InvoiceViewModel>>($"{apiBase}?{f.ToQueryString()}&page={f.Page}&pageSize={f.PageSize}") ?? new();

    public async Task<List<InvoiceViewModel>> GetInvoicesForExportAsync(string apiBase, ArApFilterViewModel f) =>
        await GetAsync<List<InvoiceViewModel>>($"{apiBase}/export?{f.ToQueryString()}") ?? new();

    public Task<InvoiceViewModel?> GetInvoiceAsync(string apiBase, Guid uid) =>
        GetAsync<InvoiceViewModel>($"{apiBase}/{uid}");

    public async Task<List<PartyOptionViewModel>> GetPartiesAsync(string apiBase) =>
        await GetAsync<List<PartyOptionViewModel>>($"{apiBase}/parties") ?? new();

    public async Task<List<ReceiptOptionViewModel>> GetReceiptOptionsAsync() =>
        await GetAsync<List<ReceiptOptionViewModel>>("api/purchase-invoices/receipt-options") ?? new();

    /// <summary>PO Aset dengan penerimaan yang belum difakturkan.</summary>
    public async Task<List<ApoInvoiceOptionViewModel>> GetApoInvoiceOptionsAsync() =>
        await GetAsync<List<ApoInvoiceOptionViewModel>>("api/purchase-invoices/apo-options") ?? new();

    public Task<ApiResult> SaveInvoiceAsync(string apiBase, InvoiceFormViewModel m, bool post) =>
        PostAsync(m.Uid == null ? $"{apiBase}/create" : $"{apiBase}/update", new
        {
            uid = m.Uid,
            partyId = m.PartyId,
            partyRef = m.PartyRef,
            invoiceDate = m.InvoiceDate,
            dueDate = m.DueDate,
            accountId = m.AccountId,
            description = m.Description,
            amount = m.Amount,
            taxRateId = m.TaxRateId,
            taxPercent = m.TaxRateId == null ? m.TaxPercent : 0,
            taxInvoiceNo = m.TaxInvoiceNo,
            pph23RateId = m.Pph23RateId,
            pph23HasNpwp = m.Pph23HasNpwp,
            pph23Base = m.Pph23Base,
            costCenterId = m.CostCenterId,
            projectId = m.ProjectId,
            currencyCode = m.CurrencyCode,
            fxRate = m.FxRate,
            receiptUid = m.Uid == null ? m.ReceiptUid : null,
            apoUid = m.Uid == null && m.ReceiptUid == null ? m.ApoUid : null,
            deferralMonths = m.IsDeferred ? m.DeferralMonths : null,
            deferralStart = m.IsDeferred && DateOnly.TryParseExact(m.DeferralStartMonth + "-01", "yyyy-MM-dd", out var start) ? start : (DateOnly?)null,
            post
        });

    public Task<ApiResult> DeleteInvoiceAsync(string apiBase, Guid uid) => PostAsync($"{apiBase}/delete", new { uid });

    public Task<ApiResult> PostInvoiceAsync(string apiBase, Guid uid) => PostAsync($"{apiBase}/{uid}/post", new { });

    public Task<ApiResult> VoidInvoiceAsync(string apiBase, Guid uid, string? reason, DateOnly? date) =>
        PostAsync($"{apiBase}/{uid}/void", new { reason, date });

    // ---------- Pembayaran ----------

    public async Task<PagedResult<PaymentViewModel>> GetPaymentsAsync(string apiBase, ArApFilterViewModel f) =>
        await GetAsync<PagedResult<PaymentViewModel>>($"{apiBase}?{f.ToQueryString()}&page={f.Page}&pageSize={f.PageSize}") ?? new();

    public Task<PaymentViewModel?> GetPaymentAsync(string apiBase, Guid uid) =>
        GetAsync<PaymentViewModel>($"{apiBase}/{uid}");

    public async Task<List<OpenInvoiceViewModel>> GetOpenInvoicesAsync(string apiBase, Guid partyUid, Guid? paymentUid) =>
        await GetAsync<List<OpenInvoiceViewModel>>($"{apiBase}/open-invoices?partyUid={partyUid}&paymentUid={paymentUid}") ?? new();

    public Task<ApiResult> SavePaymentAsync(string apiBase, PaymentFormViewModel m, bool post) =>
        PostAsync(m.Uid == null ? $"{apiBase}/create" : $"{apiBase}/update", new
        {
            uid = m.Uid,
            partyId = m.PartyId,
            paymentDate = m.PaymentDate,
            cashAccountId = m.CashAccountId,
            fxRate = m.FxRate,
            reference = m.Reference,
            description = m.Description,
            post,
            allocations = m.Allocations.Where(a => a.Amount != 0).Select(a => new { a.InvoiceId, a.Amount })
        });

    public Task<ApiResult> DeletePaymentAsync(string apiBase, Guid uid) => PostAsync($"{apiBase}/delete", new { uid });

    public Task<ApiResult> PostPaymentAsync(string apiBase, Guid uid) => PostAsync($"{apiBase}/{uid}/post", new { });

    public Task<ApiResult> VoidPaymentAsync(string apiBase, Guid uid, string? reason, DateOnly? date) =>
        PostAsync($"{apiBase}/{uid}/void", new { reason, date });

    // ---------- Umur, kartu, PPN ----------

    public async Task<AgingViewModel> GetAgingAsync(string apiPath, DateOnly asOf) =>
        await GetAsync<AgingViewModel>($"{apiPath}?asOf={asOf:yyyy-MM-dd}") ?? new AgingViewModel { AsOf = asOf };

    public async Task<PartyCardViewModel> GetPartyCardAsync(string apiPath, Guid? partyUid, DateOnly from, DateOnly to) =>
        await GetAsync<PartyCardViewModel>($"{apiPath}?partyUid={partyUid}&from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}")
        ?? new PartyCardViewModel { From = from, To = to };

    public async Task<VatRecapViewModel> GetVatRecapAsync(int year) =>
        await GetAsync<VatRecapViewModel>($"api/vat?year={year}") ?? new VatRecapViewModel { Year = year };

    public Task<ApiResult> CloseVatAsync(string period, string? note) => PostAsync("api/vat/close", new { period, note });

    public async Task<VatInvoicesViewModel> GetVatInvoicesAsync(string period) =>
        await GetAsync<VatInvoicesViewModel>($"api/vat/{Esc(period)}/invoices") ?? new VatInvoicesViewModel { Period = period };

    /// <summary>Isi/ubah Nomor Faktur Pajak (NSFP) faktur draft/posted.</summary>
    public Task<ApiResult> SetTaxInvoiceNoAsync(string apiBase, Guid uid, string? taxInvoiceNo) =>
        PostAsync($"{apiBase}/{uid}/tax-invoice-no", new { taxInvoiceNo });

    // ---------- Tarif pajak ----------

    public async Task<List<TaxRateViewModel>> GetTaxRatesAsync() =>
        await GetAsync<List<TaxRateViewModel>>("api/tax-rates") ?? new();

    /// <summary>Tarif aktif yang berlaku pada tanggal (+ tarif <paramref name="include"/> milik draft yang diedit).</summary>
    public async Task<List<TaxRateViewModel>> GetTaxRateOptionsAsync(DateOnly date, long? include = null) =>
        await GetAsync<List<TaxRateViewModel>>($"api/tax-rates/options?date={date:yyyy-MM-dd}&include={include}") ?? new();

    public Task<ApiResult> SaveTaxRateAsync(TaxRateViewModel m) =>
        PostAsync(m.Uid == Guid.Empty ? "api/tax-rates/create" : "api/tax-rates/update", new
        {
            uid = m.Uid == Guid.Empty ? (Guid?)null : m.Uid,
            m.Name, m.NominalRate, m.DppNumerator, m.DppDenominator, m.IsInputCreditable, m.ValidFrom, m.ValidTo, m.IsActive, m.Note
        });

    public Task<ApiResult> DeleteTaxRateAsync(Guid uid) => PostAsync("api/tax-rates/delete", new { uid });

    // ---------- PPh 23 ----------

    public async Task<List<Pph23RateViewModel>> GetPph23RatesAsync() =>
        await GetAsync<List<Pph23RateViewModel>>("api/pph23-rates") ?? new();

    public async Task<List<Pph23RateViewModel>> GetPph23RateOptionsAsync(DateOnly date, long? include = null) =>
        await GetAsync<List<Pph23RateViewModel>>($"api/pph23-rates/options?date={date:yyyy-MM-dd}&include={include}") ?? new();

    public Task<ApiResult> SavePph23RateAsync(Pph23RateViewModel m) =>
        PostAsync(m.Uid == Guid.Empty ? "api/pph23-rates/create" : "api/pph23-rates/update", new
        {
            uid = m.Uid == Guid.Empty ? (Guid?)null : m.Uid,
            m.Name, m.ObjectType, m.Rate, noNpwpRate = m.NoNpwpRate == 0 ? (decimal?)null : m.NoNpwpRate, m.ValidFrom, m.ValidTo, m.IsActive, m.Note
        });

    public Task<ApiResult> DeletePph23RateAsync(Guid uid) => PostAsync("api/pph23-rates/delete", new { uid });

    public async Task<Pph23YearViewModel> GetPph23YearAsync(int year) =>
        await GetAsync<Pph23YearViewModel>($"api/pph23?year={year}") ?? new Pph23YearViewModel { Year = year };

    public async Task<Pph23PeriodViewModel> GetPph23PeriodAsync(string period) =>
        await GetAsync<Pph23PeriodViewModel>($"api/pph23/{Esc(period)}") ?? new Pph23PeriodViewModel();

    public Task<ApiResult> CreatePph23DepositAsync(string period, DateOnly depositDate, long cashAccountId, string? ntpn, string? note) =>
        PostAsync("api/pph23/deposits/create", new { period, depositDate, cashAccountId, ntpn, note });

    public Task<ApiResult> VoidPph23DepositAsync(Guid uid, string? reason, DateOnly? date) =>
        PostAsync($"api/pph23/deposits/{uid}/void", new { reason, date });

    public Task<ApiResult> SetPph23CertificateAsync(Guid invoiceUid, string? certNo, DateOnly? certDate) =>
        PostAsync($"api/pph23/invoices/{invoiceUid}/certificate", new { certNo, certDate });
}
