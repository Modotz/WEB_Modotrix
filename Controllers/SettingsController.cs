using ERP.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Web.Controllers;

/// <summary>
/// Pengaturan &gt; App Settings (pengaturan, logo, pemeliharaan data, backup) dan Reset Data.
/// </summary>
public class SettingsController : Controller
{
    private const string SettingsFieldPrefix = "settings[";

    private readonly SettingsApiClient _api;
    private readonly BrandingService _branding;

    public SettingsController(SettingsApiClient api, BrandingService branding)
    {
        _api = api;
        _branding = branding;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        return View(await _api.GetSettingsAsync());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestFormLimits(ValueCountLimit = 5000)]
    public async Task<IActionResult> Save(IFormFile? appLogo)
    {
        // Field berbentuk settings[key]. Switch (boolean) mengirim hidden "0" lalu checkbox "1":
        // nilai terakhir yang dipakai (sama seperti PHP).
        var values = Request.Form
            .Where(f => f.Key.StartsWith(SettingsFieldPrefix, StringComparison.Ordinal) && f.Key.EndsWith(']'))
            .ToDictionary(
                f => f.Key[SettingsFieldPrefix.Length..^1],
                f => (string?)f.Value[^1]);

        var messages = new List<string>();
        var failed = false;

        if (values.Count > 0)
        {
            var (success, message) = await _api.SaveAsync(values);
            failed |= !success;
            messages.Add(message);
        }

        if (appLogo is { Length: > 0 })
        {
            var (success, message) = await _api.UploadLogoAsync(appLogo);
            failed |= !success;
            messages.Add(message);
        }

        _branding.Invalidate();
        TempData[failed ? "Error" : "Success"] = string.Join(" ", messages.DefaultIfEmpty("Tidak ada perubahan."));

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PruneLogs(int months) =>
        Result(await _api.PruneLogsAsync(months));

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PruneSerialMovements(int months) =>
        Result(await _api.PruneSerialMovementsAsync(months));

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Backup(string? folder)
    {
        if (string.IsNullOrWhiteSpace(folder))
        {
            TempData["Error"] = "Folder tujuan wajib diisi.";
            return RedirectToAction(nameof(Index));
        }

        return Result(await _api.BackupAsync(folder));
    }

    [HttpGet]
    public async Task<IActionResult> BackupDownload()
    {
        var response = await _api.BackupDownloadAsync();

        if (!response.IsSuccessStatusCode)
        {
            var (_, message) = await SettingsApiClient.ResultAsync(response, "Backup gagal.");
            response.Dispose();
            TempData["Error"] = message;
            return RedirectToAction(nameof(Index));
        }

        HttpContext.Response.RegisterForDispose(response);

        var fileName = response.Content.Headers.ContentDisposition?.FileNameStar
                       ?? response.Content.Headers.ContentDisposition?.FileName?.Trim('"')
                       ?? $"backup_{DateTime.Now:yyyyMMdd_HHmmss}.bak";

        return File(await response.Content.ReadAsStreamAsync(), "application/octet-stream", fileName);
    }

    // ===================== Reset Data =====================

    [HttpGet]
    public async Task<IActionResult> Wipe()
    {
        return View(await _api.GetWipeGroupsAsync());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> WipeRun(string? confirm)
    {
        var (success, message) = await _api.WipeAsync(confirm ?? string.Empty);
        TempData[success ? "Success" : "Error"] = message;

        return RedirectToAction(nameof(Wipe));
    }

    private IActionResult Result((bool Success, string Message) result)
    {
        TempData[result.Success ? "Success" : "Error"] = result.Message;
        return RedirectToAction(nameof(Index));
    }
}
