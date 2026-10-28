using ERP.Web.Models;
using ERP.Web.Services;
using Microsoft.AspNetCore.Mvc;

using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ERP.Web.Controllers;

public class WarehouseController : Controller
{
    private readonly ApiClient _api;
    private readonly Translator _t;

    public WarehouseController(ApiClient api, Translator t)
    {
        _api = api;
        _t = t;
    }


    // ==========================================
    // GET: /Warehouse
    // ==========================================
    [HttpGet]
    public async Task<IActionResult> Index(
        string? search = null,
        bool? isDefault = null,
        bool? isActive = null,
        int page = 1,
        int pageSize = 10)
    {
        // ==========================================
        // DEFAULT PAGE
        // ==========================================
        page = page < 1
            ? 1
            : page;

        // ==========================================
        // ALLOWED PAGE SIZE
        // ==========================================
        var allowedPageSizes =
            new[] { 10, 25, 50, 100 };

        if (!allowedPageSizes.Contains(pageSize))
        {
            pageSize = 10;
        }

        // ==========================================
        // GET DATA FROM API
        // ==========================================
        var result =
            await _api.GetWarehousesAsync(
                search,
                isDefault,
                isActive);

        // ==========================================
        // API FAILED
        // ==========================================
        if (!result.Success)
        {
            TempData["Error"] =
                result.Error ??
                "Gagal mengambil data gudang.";

            return View(new PagedResult<WarehouseViewModel>
            {
                Data = [],
                Page = 1,
                PageSize = pageSize,
                TotalCount = 0,
                TotalData = 0
            });
        }

        // ==========================================
        // API SUCCESS
        // ==========================================
        var warehouses =
            result.Data ?? new List<WarehouseViewModel>();

        var totalCount =
            warehouses.Count;

        var totalPage =
            totalCount == 0
                ? 1
                : (int)Math.Ceiling(
                    totalCount / (double)pageSize
                );

        // ==========================================
        // PAGE LIMIT
        // ==========================================
        if (page > totalPage)
        {
            page = totalPage;
        }

        // ==========================================
        // PAGINATION
        // ==========================================
        var data =
            warehouses
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

        // ==========================================
        // RESULT
        // ==========================================
        var model =
            new PagedResult<WarehouseViewModel>
            {
                Data = data,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalData = totalCount
            };

        return View(model);
    }


    // ==========================================
    // GET: /Warehouse/GetById/{uid}
    // ==========================================
    [HttpGet]
    public async Task<IActionResult> GetById(Guid uid)
    {
        // ==========================================
        // VALIDATE UID
        // ==========================================
        if (uid == Guid.Empty)
        {
            return NotFound(new
            {
                success = false,
                message = _t["Data gudang tidak ditemukan."]
            });
        }

        // ==========================================
        // GET DATA FROM API
        // ==========================================
        var warehouse =
            await _api.GetWarehouseAsync(uid);

        // ==========================================
        // DATA NOT FOUND
        // ==========================================
        if (warehouse == null)
        {
            return NotFound(new
            {
                success = false,
                message = _t["Data gudang tidak ditemukan."]
            });
        }

        // ==========================================
        // RETURN DATA
        // ==========================================
        return Json(warehouse);
    }


    // ==========================================
    // POST: /Warehouse/Create
    // ==========================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        WarehouseViewModel model)
    {
        // ==========================================
        // ID TIDAK DIPERLUKAN SAAT CREATE
        // ==========================================
        model.Id = null;

        // ==========================================
        // VALIDATE FORM
        // ==========================================
        if (!ModelState.IsValid)
        {
            var validationErrors =
                ModelState
                    .Where(x => x.Value != null && x.Value.Errors.Count > 0)
                    .SelectMany(x =>
                        x.Value!.Errors.Select(error =>
                            string.IsNullOrWhiteSpace(error.ErrorMessage)
                                ? $"{x.Key}: {_t["Input tidak valid."]}"
                                : $"{x.Key}: {error.ErrorMessage}"))
                    .ToList();

            TempData["Error"] =
                validationErrors.Count > 0
                    ? string.Join(" ", validationErrors)
                    : "Input data gudang tidak valid.";

            return RedirectToAction(nameof(Index));
        }

        var result =
            await _api.CreateWarehouseAsync(model);

        if (!result.Success)
        {
            TempData["Error"] =
                result.Error ??
                "Gagal menambahkan data gudang.";

            return RedirectToAction(nameof(Index));
        }

        TempData["Success"] =
            "Data gudang berhasil ditambahkan.";

        return RedirectToAction(nameof(Index));
    }


    // ==========================================
    // POST: /Warehouse/Edit
    // ==========================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        WarehouseViewModel model)
    {
        // ==========================================
        // VALIDATE UID (record tidak diidentifikasi dengan id angka)
        // ==========================================
        if (string.IsNullOrWhiteSpace(model.Uid))
        {
            TempData["Error"] =
                "Data gudang tidak ditemukan.";

            return RedirectToAction(nameof(Index));
        }

        // ==========================================
        // VALIDATE FORM
        // ==========================================
        if (!ModelState.IsValid)
        {
            var validationErrors =
                ModelState
                    .Where(x => x.Value != null && x.Value.Errors.Count > 0)
                    .SelectMany(x =>
                        x.Value!.Errors.Select(error =>
                            string.IsNullOrWhiteSpace(error.ErrorMessage)
                                ? $"{x.Key}: {_t["Input tidak valid."]}"
                                : $"{x.Key}: {error.ErrorMessage}"))
                    .ToList();

            TempData["Error"] =
                validationErrors.Count > 0
                    ? string.Join(" ", validationErrors)
                    : "Input data gudang tidak valid.";

            return RedirectToAction(nameof(Index));
        }

        // ==========================================
        // SEND TO API
        // ==========================================
        var result =
            await _api.UpdateWarehouseAsync(model);

        // ==========================================
        // API ERROR
        // ==========================================
        if (!result.Success)
        {
            TempData["Error"] =
                result.Error ??
                "Gagal mengubah data gudang.";

            return RedirectToAction(nameof(Index));
        }

        // ==========================================
        // SUCCESS
        // ==========================================
        TempData["Success"] =
            "Data gudang berhasil diperbarui.";

        return RedirectToAction(nameof(Index));
    }


    // ==========================================
    // POST: /Warehouse/Delete
    // ==========================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid uid)
    {
        // ==========================================
        // VALIDATE UID
        // ==========================================
        if (uid == Guid.Empty)
        {
            TempData["Error"] =
                "Data gudang tidak ditemukan.";

            return RedirectToAction(nameof(Index));
        }

        // ==========================================
        // SEND TO API
        // ==========================================
        var result =
            await _api.DeleteWarehouseAsync(uid);

        // ==========================================
        // SUCCESS
        // ==========================================
        if (result.Success)
        {
            TempData["Success"] =
                "Data gudang berhasil dihapus.";
        }
        else
        {
            // ==========================================
            // API ERROR
            // ==========================================
            TempData["Error"] =
                result.Error ??
                "Gagal menghapus data gudang.";
        }

        return RedirectToAction(nameof(Index));
    }


    // ==========================================
    // GET: /Warehouse/ExportExcel
    // ==========================================
    [HttpGet]
    public async Task<IActionResult> ExportExcel()
    {
        var warehouses =
            await _api.GetWarehousesForExportAsync();

        using var workbook =
            new XLWorkbook();

        var worksheet =
            workbook.Worksheets.Add(_t["Gudang"]);

        worksheet.Cell(1, 1).Value = "No";
        worksheet.Cell(1, 2).Value = "Code";
        worksheet.Cell(1, 3).Value = "Name";
        worksheet.Cell(1, 4).Value = "Address";
        worksheet.Cell(1, 5).Value = "Phone";
        worksheet.Cell(1, 6).Value = "PIC Name";
        worksheet.Cell(1, 7).Value = "Is Default";
        worksheet.Cell(1, 8).Value = "Is Active";

        var header =
            worksheet.Range("A1:H1");

        header.Style.Font.Bold = true;

        header.Style.Alignment.Horizontal =
            XLAlignmentHorizontalValues.Center;

        int row = 2;
        int no = 1;

        foreach (var item in warehouses)
        {
            worksheet.Cell(row, 1).Value = no;
            worksheet.Cell(row, 2).Value = item.Code;
            worksheet.Cell(row, 3).Value = item.Name;
            worksheet.Cell(row, 4).Value =
                item.Address ?? "";
            worksheet.Cell(row, 5).Value =
                item.Phone ?? "";
            worksheet.Cell(row, 6).Value =
                item.PicName ?? "";

            worksheet.Cell(row, 7).Value =
                item.IsDefault
                    ? _t["Ya"]
                    : _t["Tidak"];

            worksheet.Cell(row, 8).Value =
                item.IsActive
                    ? _t["Aktif"]
                    : _t["Nonaktif"];

            row++;
            no++;
        }

        worksheet.Columns()
            .AdjustToContents();

        using var stream =
            new MemoryStream();

        workbook.SaveAs(stream);

        return File(
            stream.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"Warehouse_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
        );
    }


    // ==========================================
    // GET: /Warehouse/ExportPdf
    // ==========================================
    [HttpGet]
    public async Task<IActionResult> ExportPdf()
    {
        var warehouses =
            await _api.GetWarehousesForExportAsync();

        var listWarehouses =
            warehouses.ToList();

        var document =
            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(
                        PageSizes.A4.Landscape());

                    page.Margin(30);

                    page.DefaultTextStyle(x =>
                        x.FontSize(9.5f)
                         .FontColor("#333333"));

                    page.Header()
                        .Column(col =>
                        {
                            col.Item()
                                .Text("ERP-LITE")
                                .Bold()
                                .FontSize(18)
                                .FontColor("#1a1a1a");

                            col.Item()
                                .PaddingTop(2)
                                .Text(text =>
                                {
                                    text.Span(_t["Gudang"])
                                        .FontColor("#6c757d");

                                    text.Span(" · ")
                                        .FontColor("#6c757d");

                                    text.Span(
                                        _t["Dicetak: {0}", DateTime.Now.ToString("dd/MM/yyyy HH:mm")])
                                        .FontColor("#6c757d");

                                    text.Span(" · ")
                                        .FontColor("#6c757d");

                                    text.Span(
                                        _t["Total: {0} data", listWarehouses.Count])
                                        .FontColor("#6c757d");
                                });
                        });

                    page.Content()
                        .PaddingTop(15)
                        .Table(table =>
                        {
                            table.ColumnsDefinition(
                                columns =>
                                {
                                    columns.ConstantColumn(35);
                                    columns.ConstantColumn(75);
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(3);
                                    columns.ConstantColumn(75);
                                    columns.RelativeColumn(2);
                                    columns.ConstantColumn(65);
                                    columns.ConstantColumn(65);
                                });

                            table.Header(header =>
                            {
                                header.Cell()
                                    .Element(HeaderStyle)
                                    .Text("No");

                                header.Cell()
                                    .Element(HeaderStyle)
                                    .Text("Code");

                                header.Cell()
                                    .Element(HeaderStyle)
                                    .Text("Name");

                                header.Cell()
                                    .Element(HeaderStyle)
                                    .Text("Address");

                                header.Cell()
                                    .Element(HeaderStyle)
                                    .Text("Phone");

                                header.Cell()
                                    .Element(HeaderStyle)
                                    .Text("PIC Name");

                                header.Cell()
                                    .Element(HeaderStyle)
                                    .Text("Is Default");

                                header.Cell()
                                    .Element(HeaderStyle)
                                    .Text("Is Active");

                                static IContainer HeaderStyle(
                                    IContainer c)
                                {
                                    return c
                                        .Border(1)
                                        .BorderColor("#0d6efd")
                                        .Background("#0d6efd")
                                        .PaddingVertical(6)
                                        .PaddingHorizontal(8)
                                        .DefaultTextStyle(x =>
                                            x.Bold()
                                             .FontColor("#ffffff"));
                                }
                            });

                            int no = 1;

                            foreach (var item in listWarehouses)
                            {
                                string backgroundColor =
                                    (no % 2 == 0)
                                        ? "#f8f9fa"
                                        : "#ffffff";

                                table.Cell()
                                    .Element(c =>
                                        Cell(
                                            c,
                                            backgroundColor))
                                    .Text(no.ToString());

                                table.Cell()
                                    .Element(c =>
                                        Cell(
                                            c,
                                            backgroundColor))
                                    .Text(
                                        item.Code ?? "");

                                table.Cell()
                                    .Element(c =>
                                        Cell(
                                            c,
                                            backgroundColor))
                                    .Text(
                                        item.Name ?? "");

                                table.Cell()
                                    .Element(c =>
                                        Cell(
                                            c,
                                            backgroundColor))
                                    .Text(
                                        item.Address ?? "");

                                table.Cell()
                                    .Element(c =>
                                        Cell(
                                            c,
                                            backgroundColor))
                                    .Text(
                                        item.Phone ?? "");

                                table.Cell()
                                    .Element(c =>
                                        Cell(
                                            c,
                                            backgroundColor))
                                    .Text(
                                        item.PicName ?? "");

                                table.Cell()
                                    .Element(c =>
                                        Cell(
                                            c,
                                            backgroundColor))
                                    .Text(
                                        item.IsDefault
                                            ? _t["Ya"]
                                            : _t["Tidak"]);

                                table.Cell()
                                    .Element(c =>
                                        Cell(
                                            c,
                                            backgroundColor))
                                    .Text(
                                        item.IsActive
                                            ? _t["Aktif"]
                                            : _t["Nonaktif"]);

                                no++;
                            }

                            static IContainer Cell(
                                IContainer c,
                                string backgroundColor)
                            {
                                return c
                                    .Border(1)
                                    .BorderColor("#dee2e6")
                                    .Background(
                                        backgroundColor)
                                    .PaddingVertical(6)
                                    .PaddingHorizontal(8);
                            }
                        });

                    page.Footer()
                        .AlignLeft()
                        .PaddingTop(10)
                        .Text(
                            _t["Dokumen dihasilkan otomatis oleh sistem."])
                        .FontSize(8.5f)
                        .FontColor("#888888");
                });
            });

        var pdf =
            document.GeneratePdf();

        return File(
            pdf,
            "application/pdf",
            $"Warehouse_{DateTime.Now:yyyyMMdd_HHmmss}.pdf"
        );
    }


    // ==========================================
    // GET: /Warehouse/Print
    // ==========================================
    [HttpGet]
    public async Task<IActionResult> Print()
    {
        var warehouses =
            await _api.GetWarehousesForExportAsync();

        var model =
            new PagedResult<WarehouseViewModel>
            {
                Data = warehouses,
                Page = 1,
                PageSize =
                    warehouses.Count == 0
                        ? 10
                        : warehouses.Count,
                TotalCount = warehouses.Count,
                TotalData = warehouses.Count
            };

        return View(model);
    }
}