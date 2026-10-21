using ERP.Web.Models;
using ERP.Web.Services;
using Microsoft.AspNetCore.Mvc;
using QuestPDF.Fluent;
using QuestPDF.Helpers;

namespace ERP.Web.Controllers;

public class SuppliersController : Controller
{
    private readonly ApiClient _api;

    public SuppliersController(ApiClient api)
    {
        _api = api;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
    string? search = null,
    bool? isActive = null,
    int page = 1,
    int pageSize = 10)
    {
        var result =
            await _api.GetSuppliersAsync(
                search,
                isActive,
                page,
                pageSize
            );


        return View(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetById(long id)
    {
        var supplier = await _api.GetSupplierAsync(id);

        if (supplier is null)
            return NotFound();

        return Json(supplier);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SupplierViewModel model)
    {
        if (string.IsNullOrWhiteSpace(model.Code))
        {
            ModelState.AddModelError(
                nameof(model.Code),
                "Kode wajib diisi."
            );
        }

        if (!ModelState.IsValid)
        {
            var errors = ModelState
                .Where(x => x.Value != null && x.Value.Errors.Count > 0)
                .Select(x => new
                {
                    Field = x.Key,
                    Errors = x.Value!.Errors
                        .Select(e => e.ErrorMessage)
                        .ToList()
                })
                .ToList();

            foreach (var error in errors)
            {
                Console.WriteLine(
                    $"FIELD ERROR: {error.Field} -> " +
                    string.Join(", ", error.Errors)
                );
            }

            TempData["Error"] = string.Join(
                " | ",
                errors.SelectMany(x =>
                    x.Errors.Select(e => $"{x.Field}: {e}")
                )
            );

            return RedirectToAction(nameof(Index));
        }

        var result = await _api.CreateSupplierAsync(model);

        if (!result.Success)
        {
            TempData["Error"] =
                result.Error ?? "Gagal menambahkan supplier.";

            return RedirectToAction(nameof(Index));
        }

        TempData["Success"] =
            "Supplier berhasil ditambahkan.";

        return RedirectToAction(nameof(Index));
    }


    // EDIT di MVC juga POST.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(SupplierViewModel model)
    {
        ModelState.Remove(nameof(model.Code));

        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Data supplier belum lengkap.";
            return RedirectToAction(nameof(Index));
        }

        var result = await _api.UpdateSupplierAsync(model);

        TempData[result.Success ? "Success" : "Error"] =
            result.Success
                ? "Supplier berhasil diubah."
                : result.Error ?? "Gagal mengubah supplier.";

        return RedirectToAction(nameof(Index));
    }

    // DELETE di MVC juga POST.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(long id)
    {
        var result = await _api.DeleteSupplierAsync(id);

        TempData[result.Success ? "Success" : "Error"] =
            result.Success
                ? "Supplier berhasil dihapus."
                : result.Error ?? "Gagal menghapus supplier.";

        return RedirectToAction(nameof(Index));
    }

    //export-excel
    [HttpGet]
    public async Task<IActionResult> ExportExcel()
    {
        var suppliers = await _api.GetSuppliersForExportAsync();

        using var workbook = new ClosedXML.Excel.XLWorkbook();

        var worksheet = workbook.Worksheets.Add("Supplier");

        worksheet.Cell(1, 1).Value = "No";
        worksheet.Cell(1, 2).Value = "Kode";
        worksheet.Cell(1, 3).Value = "Nama Supplier";
        worksheet.Cell(1, 4).Value = "Contact Person";
        worksheet.Cell(1, 5).Value = "Phone";
        worksheet.Cell(1, 6).Value = "Email";
        worksheet.Cell(1, 7).Value = "NPWP";
        worksheet.Cell(1, 8).Value = "Address";
        worksheet.Cell(1, 9).Value = "Status";

        var header = worksheet.Range("A1:I1");

        header.Style.Font.Bold = true;
        header.Style.Alignment.Horizontal =
            ClosedXML.Excel.XLAlignmentHorizontalValues.Center;

        int row = 2;
        int no = 1;

        foreach (var item in suppliers)
        {
            worksheet.Cell(row, 1).Value = no;
            worksheet.Cell(row, 2).Value = item.Code;
            worksheet.Cell(row, 3).Value = item.Name;
            worksheet.Cell(row, 4).Value = item.ContactPerson;
            worksheet.Cell(row, 5).Value = item.Phone;
            worksheet.Cell(row, 6).Value = item.Email;
            worksheet.Cell(row, 7).Value = item.Npwp;
            worksheet.Cell(row, 8).Value = item.Address;
            worksheet.Cell(row, 9).Value =
                item.IsActive ? "Aktif" : "Nonaktif";

            row++;
            no++;
        }

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();

        workbook.SaveAs(stream);

        return File(
            stream.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"Supplier_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
        );
    }

    //export-pdf
    [HttpGet]
    public async Task<IActionResult> ExportPdf()
    {
        var suppliers = await _api.GetSuppliersForExportAsync();
        var listSuppliers = suppliers.ToList(); // Mengubah ke List untuk mendapatkan Total Count

        var document = QuestPDF.Fluent.Document.Create(container =>
        {
            container.Page(page =>
            {
                // Set orientasi ke Landscape agar kolom pas
                page.Size(QuestPDF.Helpers.PageSizes.A4.Landscape());
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(9.5f).FontColor("#333333"));

                // Header Dokumen
                page.Header().Column(col =>
                {
                    col.Item().Text("ERP-LITE")
                        .Bold()
                        .FontSize(18)
                        .FontColor("#1a1a1a");

                    col.Item().PaddingTop(2).Text(text =>
                    {
                        text.Span("Supplier").FontColor("#6c757d");
                        text.Span(" · ").FontColor("#6c757d");
                        text.Span($"Dicetak: {DateTime.Now:dd/MM/yyyy HH:mm}").FontColor("#6c757d");
                        text.Span(" · ").FontColor("#6c757d");
                        text.Span($"Total: {listSuppliers.Count} data").FontColor("#6c757d");
                    });
                });

                // Isi Tabel
                page.Content().PaddingTop(15).Table(table =>
                {
                    // Definisi Lebar Kolom (Sesuai dengan gambar tanpa kolom Address)
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(35);   // No
                        columns.ConstantColumn(65);   // Code
                        columns.RelativeColumn(2);    // Name
                        columns.RelativeColumn(2);    // Contact Person
                        columns.RelativeColumn(1.8f); // Phone
                        columns.RelativeColumn(2.5f); // Email
                        columns.RelativeColumn(2.2f); // NPWP
                        columns.ConstantColumn(65);   // Is Active
                    });

                    // Header Tabel (Biru dengan teks Putih)
                    table.Header(header =>
                    {
                        header.Cell().Element(HeaderStyle).Text("No");
                        header.Cell().Element(HeaderStyle).Text("Code");
                        header.Cell().Element(HeaderStyle).Text("Name");
                        header.Cell().Element(HeaderStyle).Text("Contact Person");
                        header.Cell().Element(HeaderStyle).Text("Phone");
                        header.Cell().Element(HeaderStyle).Text("Email");
                        header.Cell().Element(HeaderStyle).Text("NPWP");
                        header.Cell().Element(HeaderStyle).Text("Is Active");

                        static QuestPDF.Infrastructure.IContainer HeaderStyle(
                            QuestPDF.Infrastructure.IContainer c)
                        {
                            return c
                                .Border(1)
                                .BorderColor("#0d6efd")
                                .Background("#0d6efd") // Warna biru utama
                                .PaddingVertical(6)
                                .PaddingHorizontal(8)
                                .DefaultTextStyle(x => x.Bold().FontColor("#ffffff"));
                        }
                    });

                    // Data Baris Tabel
                    int no = 1;
                    foreach (var item in listSuppliers)
                    {
                        // Latar belakang selang-seling (Zebra striping)
                        string bgBgColor = (no % 2 == 0) ? "#f8f9fa" : "#ffffff";

                        table.Cell().Element(c => Cell(c, bgBgColor)).Text(no.ToString());
                        table.Cell().Element(c => Cell(c, bgBgColor)).Text(item.Code ?? "");
                        table.Cell().Element(c => Cell(c, bgBgColor)).Text(item.Name ?? "");
                        table.Cell().Element(c => Cell(c, bgBgColor)).Text(item.ContactPerson ?? "");
                        table.Cell().Element(c => Cell(c, bgBgColor)).Text(item.Phone ?? "");
                        table.Cell().Element(c => Cell(c, bgBgColor)).Text(item.Email ?? "");
                        table.Cell().Element(c => Cell(c, bgBgColor)).Text(item.Npwp ?? "");
                        table.Cell().Element(c => Cell(c, bgBgColor)).Text(item.IsActive ? "Aktif" : "Nonaktif");

                        no++;
                    }

                    static QuestPDF.Infrastructure.IContainer Cell(
                        QuestPDF.Infrastructure.IContainer c, string backgroundColor)
                    {
                        return c
                            .Border(1)
                            .BorderColor("#dee2e6") // Warna border abu-abu halus
                            .Background(backgroundColor)
                            .PaddingVertical(6)
                            .PaddingHorizontal(8);
                    }
                });

                // Footer Dokumen
                page.Footer()
                    .AlignLeft()
                    .PaddingTop(10)
                    .Text("Dokumen dihasilkan otomatis oleh sistem.")
                    .FontSize(8.5f)
                    .FontColor("#888888");
            });
        });

        var pdf = document.GeneratePdf();

        return File(
            pdf,
            "application/pdf",
            $"Supplier_{DateTime.Now:yyyyMMdd_HHmmss}.pdf"
        );
    }

    //print
    [HttpGet]
    public async Task<IActionResult> Print()
    {
        var suppliers = await _api.GetSuppliersForExportAsync();

        var model = new PagedResult<SupplierViewModel>
        {
            Data = suppliers,
            Page = 1,
            PageSize = suppliers.Count,
            TotalCount = suppliers.Count,
            TotalData = suppliers.Count,
          
        };

        return View(model);
    }

}
