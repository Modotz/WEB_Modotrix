using ERP.Web.Models;
using Microsoft.AspNetCore.Http;
using ERP.Web.Services;
using Microsoft.AspNetCore.Mvc;

using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ERP.Web.Controllers;

public class BrandsController : Controller
{
    private readonly ApiClient _api;
    private readonly Translator _t;

    public BrandsController(ApiClient api, Translator t)
    {
        _api = api;
        _t = t;
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
        var result =
            await _api.GetBrandsAsync(
                search,
                isActive,
                page,
                pageSize
            );

        ViewBag.Search = search;
        ViewBag.IsActive = isActive;
        ViewBag.PageSize = pageSize;

        return View(result);
    }


    // =========================================================
    // GET DATA BRAND BERDASARKAN ID
    // =========================================================

    [HttpGet]
    public async Task<IActionResult> GetById(Guid uid)
    {
        var brand =
            await _api.GetBrandAsync(uid);

        if (brand is null)
            return NotFound();

        return Json(brand);
    }


    // =========================================================
    // CREATE BRAND - FORM
    // =========================================================

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var result =
            await _api.GetBrandsAsync(
                null,
                null,
                1,
                10
            );

        ViewBag.FormMode = "Create";

        ViewBag.FormModel =
            new BrandViewModel
            {
                IsActive = true
            };

        return View("Index", result);
    }


    // =========================================================
    // CREATE BRAND - SAVE
    // =========================================================

    [HttpPost]
    public async Task<IActionResult> Create(
        BrandViewModel model,
        IFormFile? image)
    {
        // Pastikan IsActive dibaca dari hidden input form.
        // Hidden input adalah satu-satunya input bernama IsActive.

        if (Request.HasFormContentType)
        {
            var isActiveValue =
                Request.Form["IsActive"]
                    .FirstOrDefault();

            if (bool.TryParse(
                    isActiveValue,
                    out var parsedIsActive))
            {
                model.IsActive =
                    parsedIsActive;
            }
        }


        // =====================================================
        // UPLOAD IMAGE JIKA ADA
        // =====================================================

        if (image != null)
        {
            var uploadResult =
                await _api.UploadBrandImageAsync(image);

            if (uploadResult.Success)
            {
                model.Image =
                    uploadResult.Image;
            }
            else
            {
                TempData["Error"] =
                    uploadResult.Error ??
                    "Gagal mengupload gambar.";

                return RedirectToAction(
                    nameof(Create)
                );
            }
        }


        // =====================================================
        // CREATE KE API
        // =====================================================

        var result =
            await _api.CreateBrandAsync(model);


        TempData[result.Success
            ? "Success"
            : "Error"] =
            result.Success
                ? "Brand berhasil ditambahkan."
                : result.Error ??
                  "Gagal menambahkan brand.";


        return RedirectToAction(
            nameof(Index)
        );
    }


    // =========================================================
    // EDIT BRAND - FORM
    // =========================================================

    [HttpGet]
    public async Task<IActionResult> Edit(Guid uid)
    {
        var brand =
            await _api.GetBrandAsync(uid);


        if (brand is null)
        {
            TempData["Error"] =
                "Brand tidak ditemukan.";

            return RedirectToAction(
                nameof(Index)
            );
        }


        var result =
            await _api.GetBrandsAsync(
                null,
                null,
                1,
                10
            );


        ViewBag.FormMode = "Edit";

        ViewBag.FormModel = brand;


        return View(
            "Index",
            result
        );
    }


    // =========================================================
    // EDIT BRAND - SAVE
    // =========================================================

    [HttpPost]
    public async Task<IActionResult> Edit(
        BrandViewModel model,
        IFormFile? image)
    {
        // =====================================================
        // PASTIKAN IsActive DIAMBIL DARI FORM
        // =====================================================

        if (Request.HasFormContentType)
        {
            var isActiveValue =
                Request.Form["IsActive"]
                    .FirstOrDefault();

            if (bool.TryParse(
                    isActiveValue,
                    out var parsedIsActive))
            {
                model.IsActive =
                    parsedIsActive;
            }
        }


        // =====================================================
        // UPLOAD IMAGE BARU JIKA ADA
        // =====================================================

        if (image != null)
        {
            var uploadResult =
                await _api.UploadBrandImageAsync(image);

            if (uploadResult.Success)
            {
                model.Image =
                    uploadResult.Image;
            }
            else
            {
                TempData["Error"] =
                    uploadResult.Error ??
                    "Gagal mengupload gambar.";

                return RedirectToAction(
                    nameof(Edit),
                    new
                    {
                        uid = model.Uid
                    }
                );
            }
        }


        // =====================================================
        // UID WAJIB UNTUK UPDATE (record tidak diidentifikasi dengan id angka)
        // =====================================================

        if (string.IsNullOrWhiteSpace(model.Uid))
        {
            TempData["Error"] =
                "Brand tidak ditemukan.";

            return RedirectToAction(
                nameof(Index)
            );
        }


        // =====================================================
        // UPDATE KE API
        // =====================================================

        var result =
            await _api.UpdateBrandAsync(model);


        TempData[result.Success
            ? "Success"
            : "Error"] =
            result.Success
                ? "Brand berhasil diubah."
                : result.Error ??
                  "Gagal mengubah brand.";


        return RedirectToAction(
            nameof(Index)
        );
    }


    // =========================================================
    // UPLOAD IMAGE BRAND
    // =========================================================

    [HttpPost]
    public async Task<IActionResult> UploadImage(
        [FromForm(Name = "image")] IFormFile image)
    {
        if (image == null ||
            image.Length == 0)
        {
            return BadRequest(new
            {
                message =
                    _t["Gambar belum dipilih di ERP.Web."]
            });
        }


        var result =
            await _api.UploadBrandImageAsync(image);


        if (!result.Success)
        {
            return BadRequest(new
            {
                message =
                    string.IsNullOrWhiteSpace(
                        result.Error)
                        ? _t["API gagal mengupload gambar."]
                        : _t[result.Error]
            });
        }


        return Ok(new
        {
            image = result.Image
        });
    }


    // =========================================================
    // DELETE BRAND - SOFT DELETE
    // =========================================================

    [HttpPost]
    public async Task<IActionResult> Delete(Guid uid)
    {
        var result =
            await _api.DeleteBrandAsync(uid);


        TempData[result.Success
            ? "Success"
            : "Error"] =
            result.Success
                ? "Brand berhasil dihapus."
                : result.Error ??
                  "Gagal menghapus brand.";


        return RedirectToAction(
            nameof(Index)
        );
    }


    // =========================================================
    // EXPORT EXCEL
    // (mengikuti filter search & isActive dari halaman index)
    // =========================================================

    [HttpGet]
    public async Task<IActionResult> ExportExcel(
        string? search = null,
        bool? isActive = null)
    {
        var brands =
            await _api.GetBrandsForExportAsync(
                search,
                isActive
            );

        using var workbook =
            new XLWorkbook();

        var worksheet =
            workbook.Worksheets.Add("Brand");

        worksheet.Cell(1, 1).Value = "No";
        worksheet.Cell(1, 2).Value = _t["Nama Brand"];
        worksheet.Cell(1, 3).Value = "Image";
        worksheet.Cell(1, 4).Value = _t["Status"];

        var header =
            worksheet.Range("A1:D1");

        header.Style.Font.Bold = true;

        header.Style.Alignment.Horizontal =
            XLAlignmentHorizontalValues.Center;

        int row = 2;
        int no = 1;

        foreach (var item in brands)
        {
            worksheet.Cell(row, 1).Value =
                no;

            worksheet.Cell(row, 2).Value =
                item.Name;

            worksheet.Cell(row, 3).Value =
                item.Image ?? "-";

            worksheet.Cell(row, 4).Value =
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
            $"Brand_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
        );
    }


    // =========================================================
    // EXPORT PDF
    // (mengikuti filter search & isActive dari halaman index)
    // =========================================================

    [HttpGet]
    public async Task<IActionResult> ExportPdf(
        string? search = null,
        bool? isActive = null)
    {
        var brands =
            await _api.GetBrandsForExportAsync(
                search,
                isActive
            );

        var listBrands =
            brands.ToList();

        var document =
            QuestPDF.Fluent.Document.Create(
                container =>
                {
                    container.Page(page =>
                    {
                        page.Size(
                            QuestPDF.Helpers.PageSizes.A4
                                .Landscape()
                        );

                        page.Margin(30);

                        page.DefaultTextStyle(
                            x => x
                                .FontSize(9.5f)
                                .FontColor("#333333")
                        );


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
                                        text.Span("Brand")
                                            .FontColor("#6c757d");

                                        text.Span(" · ")
                                            .FontColor("#6c757d");

                                        text.Span(
                                                _t["Dicetak: {0}", DateTime.Now.ToString("dd/MM/yyyy HH:mm")]
                                            )
                                            .FontColor("#6c757d");

                                        text.Span(" · ")
                                            .FontColor("#6c757d");

                                        text.Span(
                                                _t["Total: {0} data", listBrands.Count]
                                            )
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
                                        columns.RelativeColumn(3);
                                        columns.RelativeColumn(4);
                                        columns.ConstantColumn(70);
                                    });


                                table.Header(header =>
                                {
                                    header.Cell()
                                        .Element(HeaderStyle)
                                        .Text("No");

                                    header.Cell()
                                        .Element(HeaderStyle)
                                        .Text(_t["Nama Brand"]);

                                    header.Cell()
                                        .Element(HeaderStyle)
                                        .Text("Image");

                                    header.Cell()
                                        .Element(HeaderStyle)
                                        .Text(_t["Status"]);


                                    static IContainer HeaderStyle(
                                        IContainer c)
                                    {
                                        return c
                                            .Border(1)
                                            .BorderColor("#0d6efd")
                                            .Background("#0d6efd")
                                            .PaddingVertical(6)
                                            .PaddingHorizontal(8)
                                            .DefaultTextStyle(
                                                x => x
                                                    .Bold()
                                                    .FontColor("#ffffff")
                                            );
                                    }
                                });


                                int no = 1;

                                foreach (var item in listBrands)
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
                                        .Text(
                                            no.ToString());

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
                                            item.Image ?? "-");

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
                                _t["Dokumen dihasilkan otomatis oleh sistem."]
                            )
                            .FontSize(8.5f)
                            .FontColor("#888888");
                    });
                });


        var pdf =
            document.GeneratePdf();


        return File(
            pdf,
            "application/pdf",
            $"Brand_{DateTime.Now:yyyyMMdd_HHmmss}.pdf"
        );
    }


    // =========================================================
    // PRINT
    // (mengikuti filter search & isActive dari halaman index)
    // =========================================================

    [HttpGet]
    public async Task<IActionResult> Print(
        string? search = null,
        bool? isActive = null)
    {
        var brands =
            await _api.GetBrandsForExportAsync(
                search,
                isActive
            );

        var model =
            new PagedResult<BrandViewModel>
            {
                Data = brands,
                Page = 1,
                PageSize = brands.Count,
                TotalCount = brands.Count,
                TotalData = brands.Count
            };

        return View(model);
    }
}