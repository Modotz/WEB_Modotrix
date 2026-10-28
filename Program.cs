using QuestPDF.Infrastructure;
using ERP.Web.Security;
using ERP.Web.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;

var builder = WebApplication.CreateBuilder(args);

QuestPDF.Settings.License = LicenseType.Community;

builder.Services.AddControllersWithViews(options =>
{
    // Semua halaman wajib login, kecuali yang diberi [AllowAnonymous] (halaman login).
    options.Filters.Add(new AuthorizeFilter(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build()));
    options.Filters.Add<MenuPermissionFilter>();
    // Setelah filter izin: hanya ekspor/cetak yang diizinkan yang dicatat.
    options.Filters.Add<ActivityAuditFilter>();
    options.Filters.Add<ApiUnauthorizedExceptionFilter>();
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddMemoryCache();
builder.Services.AddTransient<ApiTokenHandler>();
builder.Services.AddScoped<AccessService>();
builder.Services.AddScoped<BrandingService>();
builder.Services.AddSingleton<Translator>();

var apiBaseUrl = new Uri(builder.Configuration["ApiSettings:BaseUrl"] ?? "https://localhost:7101/");

builder.Services.AddHttpClient<ApiClient>(client => client.BaseAddress = apiBaseUrl)
    .ConfigurePrimaryHttpMessageHandler(CreateApiHandler)
    .AddHttpMessageHandler<ApiTokenHandler>();

builder.Services.AddHttpClient<AuthApiClient>(client => client.BaseAddress = apiBaseUrl)
    .ConfigurePrimaryHttpMessageHandler(CreateApiHandler)
    .AddHttpMessageHandler<ApiTokenHandler>();

builder.Services.AddHttpClient<AuditLogApiClient>(client => client.BaseAddress = apiBaseUrl)
    .ConfigurePrimaryHttpMessageHandler(CreateApiHandler)
    .AddHttpMessageHandler<ApiTokenHandler>();

builder.Services.AddHttpClient<SettingsApiClient>(client => client.BaseAddress = apiBaseUrl)
    .ConfigurePrimaryHttpMessageHandler(CreateApiHandler)
    .AddHttpMessageHandler<ApiTokenHandler>();

builder.Services.AddHttpClient<AccountingApiClient>(client => client.BaseAddress = apiBaseUrl)
    .ConfigurePrimaryHttpMessageHandler(CreateApiHandler)
    .AddHttpMessageHandler<ApiTokenHandler>();

builder.Services.AddHttpClient<AssetApiClient>(client => client.BaseAddress = apiBaseUrl)
    .ConfigurePrimaryHttpMessageHandler(CreateApiHandler)
    .AddHttpMessageHandler<ApiTokenHandler>();

builder.Services.AddHttpClient<AssetPurchaseApiClient>(client => client.BaseAddress = apiBaseUrl)
    .ConfigurePrimaryHttpMessageHandler(CreateApiHandler)
    .AddHttpMessageHandler<ApiTokenHandler>();

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "erp_auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.SlidingExpiration = false;

        // Request AJAX menerima 401/403 (bukan redirect ke halaman login).
        options.Events.OnRedirectToLogin = context =>
        {
            if (RequestKind.IsAjax(context.Request))
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            else
                context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = context =>
        {
            if (RequestKind.IsAjax(context.Request))
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
            else
                context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        };
    });

var app = builder.Build();

app.WarnUnregisteredControllers();

app.UseHttpsRedirection();
app.UseStaticFiles();

// Bahasa UI dari cookie (id-ID default / en-US). Format angka & tanggal untuk parsing form tetap en-US.
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture(culture: "en-US", uiCulture: "id-ID"),
    SupportedCultures = new[] { new System.Globalization.CultureInfo("en-US") },
    SupportedUICultures = new[] { new System.Globalization.CultureInfo("id-ID"), new System.Globalization.CultureInfo("en-US") },
    RequestCultureProviders = new List<Microsoft.AspNetCore.Localization.IRequestCultureProvider>
    {
        new Microsoft.AspNetCore.Localization.CookieRequestCultureProvider()
    }
});

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{uid?}");

app.Run();

static HttpMessageHandler CreateApiHandler() => new HttpClientHandler
{
    ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
};
