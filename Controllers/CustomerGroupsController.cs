using ERP.Web.Models;
using ERP.Web.Services;
using Microsoft.AspNetCore.Mvc;
using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ERP.Web.Controllers;

public class CustomerGroupsController : Controller
{
    private readonly ApiClient _api;

    public CustomerGroupsController(ApiClient api)
    {
        _api = api;
    }

    // =========================================================
    // INDEX
    // =========================================================

    [HttpGet]
    public async Task<IActionResult> Index(
        string? search = null,
        bool? isActive = null,
        int page = 1,
        int pageSize = 10)
    {
        var result = await _api.GetCustomerGroupsAsync(
            search, isActive, page, pageSize);

        ViewBag.Search = search;
        ViewBag.IsActive = isActive;
        ViewBag.PageSize = pageSize;

        return View(result);
    }

    // =========================================================
    // GET BY ID
    // =========================================================

    [HttpGet]
    public async Task<IActionResult> GetById(long id)
    {
        var customerGroup = await _api.GetCustomerGroupAsync(id);

        if (customerGroup == null)
            return NotFound();

        return Json(customerGroup);
    }

    // =========================================================
    // CREATE - GET
    // =========================================================

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var priceLists = await _api.GetPriceListsAsync(
            null, true, 1, 100);

        ViewBag.PriceLists = priceLists?.Data
            ?? new List<PriceListViewModel>();

        return View(new CustomerGroupsViewModel
        {
            DiscountPercent = 0,
            IsActive = true
        });
    }

    // =========================================================
    // CREATE - POST
    // =========================================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        CustomerGroupsViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = string.Join(" | ",
                ModelState
                    .Where(x => x.Value?.Errors.Count > 0)
                    .SelectMany(x => x.Value!.Errors.Select(
                        e => $"{x.Key}: {e.ErrorMessage}")));

            return RedirectToAction(nameof(Index));
        }

        var result = await _api.CreateCustomerGroupAsync(model);

        TempData[result.Success ? "Success" : "Error"] =
            result.Success
                ? "Customer group berhasil ditambahkan."
                : result.Error ?? "Gagal menambahkan customer group.";

        return RedirectToAction(nameof(Index));
    }

    // =========================================================
    // EDIT - GET
    // =========================================================

    [HttpGet]
    public async Task<IActionResult> Edit(long id)
    {
        if (id <= 0)
        {
            TempData["Error"] = "ID customer group tidak valid.";
            return RedirectToAction(nameof(Index));
        }

        var customerGroup = await _api.GetCustomerGroupAsync(id);

        if (customerGroup == null)
        {
            TempData["Error"] = "Customer group tidak ditemukan.";
            return RedirectToAction(nameof(Index));
        }

        var priceLists = await _api.GetPriceListsAsync(
            null, true, 1, 100);

        ViewBag.PriceLists = priceLists?.Data
            ?? new List<PriceListViewModel>();

        return View(customerGroup);
    }

    // =========================================================
    // EDIT - POST
    // =========================================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        CustomerGroupsViewModel model)
    {
        if (model.Id <= 0)
        {
            TempData["Error"] = "ID customer group tidak valid.";
            return RedirectToAction(nameof(Index));
        }

        if (!ModelState.IsValid)
        {
            TempData["Error"] = string.Join(" | ",
                ModelState
                    .Where(x => x.Value?.Errors.Count > 0)
                    .SelectMany(x => x.Value!.Errors.Select(
                        e => $"{x.Key}: {e.ErrorMessage}")));

            return RedirectToAction(nameof(Index));
        }

        var result = await _api.UpdateCustomerGroupAsync(model);

        TempData[result.Success ? "Success" : "Error"] =
            result.Success
                ? "Customer group berhasil diubah."
                : result.Error ?? "Gagal mengubah customer group.";

        return RedirectToAction(nameof(Index));
    }

// =========================================================
// DELETE CUSTOMER GROUP - SOFT DELETE
// =========================================================

[HttpPost]
public async Task<IActionResult> Delete(long id)
    {
        var result =
            await _api.DeleteCustomerGroupAsync(id);

        TempData[result.Success
            ? "Success"
            : "Error"] =
            result.Success
                ? "Customer group berhasil dihapus."
                : result.Error ??
                  "Gagal menghapus customer group.";

        return RedirectToAction(
            nameof(Index)
        );
    }

    // =========================================================
    // DATA EXPORT TANPA PAGINATION + NAMA PRICE LIST
    // =========================================================

    private async Task<List<CustomerGroupsViewModel>>
        GetCustomerGroupsForOutputAsync(
            string? search,
            bool? isActive)
    {
        var groups = await _api.GetCustomerGroupsForExportAsync(
            search, isActive);

        var priceListsResult = await _api.GetPriceListsAsync(
            null, null, 1, 10000);

        var priceLists = priceListsResult?.Data
            ?? new List<PriceListViewModel>();

        var priceListNames = priceLists.ToDictionary(
            x => x.Id,
            x => x.Name);

        foreach (var group in groups)
        {
            group.PriceListName =
                group.PriceListId.HasValue &&
                priceListNames.TryGetValue(
                    group.PriceListId.Value,
                    out var priceListName)
                    ? priceListName
                    : "-";
        }

        return groups;
    }

    // =========================================================
    // EXPORT EXCEL
    // =========================================================

    [HttpGet]
    public async Task<IActionResult> ExportExcel(
        string? search = null,
        bool? isActive = null)
    {
        var groups = await GetCustomerGroupsForOutputAsync(
            search, isActive);

        using var workbook = new XLWorkbook();

        var worksheet = workbook.Worksheets.Add(
            "Kelompok Customer");

        worksheet.Cell(1, 1).Value = "No";
        worksheet.Cell(1, 2).Value = "Kode";
        worksheet.Cell(1, 3).Value = "Nama Kelompok";
        worksheet.Cell(1, 4).Value = "Diskon (%)";
        worksheet.Cell(1, 5).Value = "Price List";
        worksheet.Cell(1, 6).Value = "Keterangan";
        worksheet.Cell(1, 7).Value = "Status";

        var header = worksheet.Range("A1:G1");

        header.Style.Font.Bold = true;
        header.Style.Font.FontColor = XLColor.White;
        header.Style.Fill.BackgroundColor =
            XLColor.FromHtml("#0D6EFD");

        header.Style.Alignment.Horizontal =
            XLAlignmentHorizontalValues.Center;

        int row = 2;

        for (int i = 0; i < groups.Count; i++)
        {
            var item = groups[i];

            worksheet.Cell(row, 1).Value = i + 1;
            worksheet.Cell(row, 2).Value = item.Code ?? "-";
            worksheet.Cell(row, 3).Value = item.Name ?? "-";
            worksheet.Cell(row, 4).Value =
                item.DiscountPercent;
            worksheet.Cell(row, 5).Value =
                item.PriceListName ?? "-";
            worksheet.Cell(row, 6).Value =
                item.Description ?? "-";
            worksheet.Cell(row, 7).Value =
                item.IsActive ? "Aktif" : "Tidak Aktif";

            row++;
        }

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        return File(
            stream.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"Kelompok_Customer_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
    }

    // =========================================================
    // EXPORT PDF
    // =========================================================

    [HttpGet]
    public async Task<IActionResult> ExportPdf(
        string? search = null,
        bool? isActive = null)
    {
        var groups = await GetCustomerGroupsForOutputAsync(
            search, isActive);

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(9));

                page.Header().Column(col =>
                {
                    col.Item()
                        .Text("ERP-LITE")
                        .Bold()
                        .FontSize(18);

                    col.Item()
                        .PaddingTop(4)
                        .Text(
                            $"Daftar Kelompok Customer | " +
                            $"Dicetak: {DateTime.Now:dd/MM/yyyy HH:mm} | " +
                            $"Total: {groups.Count} data")
                        .FontColor("#666666");
                });

                page.Content()
                    .PaddingTop(15)
                    .Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(30);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(3);
                            columns.RelativeColumn(1.5f);
                            columns.RelativeColumn(2.5f);
                            columns.RelativeColumn(3);
                            columns.RelativeColumn(1.5f);
                        });

                        table.Header(header =>
                        {
                            string[] headers =
                            {
                                "No", "Kode", "Nama Kelompok",
                                "Diskon (%)", "Price List",
                                "Keterangan", "Status"
                            };

                            foreach (var text in headers)
                            {
                                header.Cell()
                                    .Border(1)
                                    .BorderColor("#0D6EFD")
                                    .Background("#0D6EFD")
                                    .Padding(5)
                                    .Text(text)
                                    .Bold()
                                    .FontColor("#FFFFFF");
                            }
                        });

                        for (int i = 0; i < groups.Count; i++)
                        {
                            var item = groups[i];
                            var background = i % 2 == 0
                                ? "#FFFFFF"
                                : "#F2F5F9";

                            void AddCell(string value)
                            {
                                table.Cell()
                                    .Border(1)
                                    .BorderColor("#D5D9DE")
                                    .Background(background)
                                    .Padding(5)
                                    .Text(value);
                            }

                            AddCell((i + 1).ToString());
                            AddCell(item.Code ?? "-");
                            AddCell(item.Name ?? "-");
                            AddCell(
                                item.DiscountPercent.ToString("0.##") + "%");
                            AddCell(item.PriceListName ?? "-");
                            AddCell(item.Description ?? "-");
                            AddCell(
                                item.IsActive ? "Aktif" : "Tidak Aktif");
                        }
                    });

                page.Footer()
                    .PaddingTop(10)
                    .Text("Dokumen dihasilkan otomatis oleh sistem.")
                    .FontSize(8)
                    .FontColor("#888888");
            });
        });

        var pdf = document.GeneratePdf();

        return File(
            pdf,
            "application/pdf",
            $"Kelompok_Customer_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
    }

    // =========================================================
    // PRINT
    // =========================================================

    [HttpGet]
    public async Task<IActionResult> Print(
        string? search = null,
        bool? isActive = null)
    {
        var groups = await GetCustomerGroupsForOutputAsync(
            search, isActive);

        var model = new PagedResult<CustomerGroupsViewModel>
        {
            Data = groups,
            Page = 1,
            PageSize = groups.Count,
            TotalData = groups.Count
        };

        return View(model);
    }
}
