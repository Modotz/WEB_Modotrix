using Microsoft.AspNetCore.Mvc;

namespace ERP.Web.Services;

/// <summary>
/// Hasil export standar untuk halaman daftar/laporan (toolbar PDF / Excel / Cetak — lihat Views/Shared/_ListHeader).
/// Pakai di action ExportExcel / ExportPdf / PrintList controller.
/// </summary>
public static class ListExportExtensions
{
    public static FileContentResult ExcelFile(this Controller controller, ListExport data) =>
        controller.File(data.ToExcel(), ListExport.ExcelMime, $"{data.FileName}_{data.Stamp}.xlsx");

    public static async Task<FileContentResult> PdfFile(this Controller controller, ListExport data)
    {
        var services = controller.HttpContext.RequestServices;
        var branding = await services.GetRequiredService<BrandingService>().GetAsync();
        var t = services.GetRequiredService<Translator>();

        return controller.File(data.ToPdf(branding.AppName, t["Dicetak"]), "application/pdf", $"{data.FileName}_{data.Stamp}.pdf");
    }

    public static ViewResult PrintView(this Controller controller, ListExport data) =>
        controller.View("~/Views/Shared/ListPrint.cshtml", data);
}
