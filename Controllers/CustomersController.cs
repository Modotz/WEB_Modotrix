using ERP.Web.Models;
using ERP.Web.Services;
using Microsoft.AspNetCore.Mvc;
using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ERP.Web.Controllers;

public class CustomersController : Controller
{
    private readonly ApiClient _api;

    public CustomersController(ApiClient api)
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
        var result = await _api.GetCustomersAsync(
            search,
            isActive,
            page,
            pageSize);

        ViewBag.Search = search;
        ViewBag.IsActive = isActive;

        return View(result);
    }

    // =========================================================
    // CREATE - GET
    // =========================================================

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await LoadDropdowns();

        return View(new CustomerViewModel
        {
            IsActive = true
        });
    }

    // =========================================================
    // CREATE - POST
    // =========================================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        CustomerViewModel model,
        IFormFile? file)
    {
        if (!ModelState.IsValid)
        {
            await LoadDropdowns();

            return View(model);
        }

        // =====================================================
        // SIMPAN CUSTOMER MELALUI API
        // =====================================================

        var customer =
            await _api.CreateCustomerAsync(model);

        if (customer == null)
        {
            ModelState.AddModelError(
                "",
                "Customer gagal ditambahkan.");

            await LoadDropdowns();

            return View(model);
        }

        // =====================================================
        // UPLOAD FOTO MELALUI API
        // =====================================================

        if (file != null && file.Length > 0)
        {
            try
            {
                var imagePath =
                    await _api.UploadCustomerImageAsync(
                        customer.Id,
                        file);

                if (string.IsNullOrWhiteSpace(imagePath))
                {
                    TempData["Error"] =
                        "Customer berhasil ditambahkan, tetapi foto gagal disimpan.";

                    return RedirectToAction(nameof(Index));
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] =
                    $"Customer berhasil ditambahkan, tetapi foto gagal diupload: {ex.Message}";

                return RedirectToAction(nameof(Index));
            }
        }

        TempData["Success"] =
            file != null && file.Length > 0
                ? "Customer dan foto berhasil ditambahkan."
                : "Customer berhasil ditambahkan.";

        return RedirectToAction(nameof(Index));
    }

    // =========================================================
    // EDIT - GET
    // =========================================================

    [HttpGet]
    public async Task<IActionResult> Edit(long id)
    {
        var result =
            await _api.GetCustomersAsync(
                page: 1,
                pageSize: 100);

        var customer =
            result.Data.FirstOrDefault(
                x => x.Id == id);

        if (customer == null)
        {
            TempData["Error"] =
                "Customer tidak ditemukan.";

            return RedirectToAction(
                nameof(Index));
        }

        await LoadDropdowns();

        // =====================================================
        // SHIPPING ADDRESSES
        // =====================================================

        await LoadShippingAddresses(id);

        return View(customer);
    }

    // =========================================================
    // EDIT - POST
    // =========================================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        CustomerViewModel model,
        IFormFile? imageFile)
    {
        if (!ModelState.IsValid)
        {
            await LoadDropdowns();

            await LoadShippingAddresses(model.Id);

            return View(model);
        }

        // =====================================================
        // UPDATE DATA CUSTOMER
        // =====================================================

        var success =
            await _api.UpdateCustomerAsync(model);

        if (!success)
        {
            ModelState.AddModelError(
                "",
                "Customer gagal diperbarui.");

            await LoadDropdowns();

            await LoadShippingAddresses(model.Id);

            return View(model);
        }

        // =====================================================
        // UPLOAD FOTO MELALUI API
        // =====================================================

        if (imageFile != null && imageFile.Length > 0)
        {
            try
            {
                var imagePath =
                    await _api.UploadCustomerImageAsync(
                        model.Id,
                        imageFile);

                if (string.IsNullOrWhiteSpace(imagePath))
                {
                    TempData["Error"] =
                        "Data customer berhasil diperbarui, tetapi foto gagal disimpan.";

                    return RedirectToAction(
                        nameof(Edit),
                        new { id = model.Id });
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] =
                    $"Data customer berhasil diperbarui, tetapi foto gagal diupload: {ex.Message}";

                return RedirectToAction(
                    nameof(Edit),
                    new { id = model.Id });
            }
        }

        TempData["Success"] =
            imageFile != null && imageFile.Length > 0
                ? "Customer dan foto berhasil diperbarui."
                : "Customer berhasil diperbarui.";

        return RedirectToAction(nameof(Index));
    }

    // =========================================================
    // DELETE CUSTOMER
    // =========================================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(long id)
    {
        var success =
            await _api.DeleteCustomerAsync(id);

        if (success)
        {
            TempData["Success"] =
                "Customer berhasil dihapus.";
        }
        else
        {
            TempData["Error"] =
                "Customer gagal dihapus.";
        }

        return RedirectToAction(nameof(Index));
    }

    // ACTIVATE CUSTOMER
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Activate(long id)
    {
        var success =
            await _api.ActivateCustomerAsync(id);

        if (success)
        {
            TempData["Success"] =
                "Customer berhasil diaktifkan kembali.";
        }
        else
        {
            TempData["Error"] =
                "Customer gagal diaktifkan kembali.";
        }

        return RedirectToAction(nameof(Index));
    }

    // =========================================================
    // UPLOAD IMAGE
    // =========================================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadImage(
        long customerId,
        IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            TempData["Error"] =
                "File gambar wajib dipilih.";

            return RedirectToAction(
                nameof(Edit),
                new { id = customerId });
        }

        try
        {
            var imagePath =
                await _api.UploadCustomerImageAsync(
                    customerId,
                    file);

            if (string.IsNullOrWhiteSpace(imagePath))
            {
                TempData["Error"] =
                    "Gagal menyimpan gambar.";

                return RedirectToAction(
                    nameof(Edit),
                    new { id = customerId });
            }

            TempData["Success"] =
                "Foto customer berhasil diupload.";
        }
        catch (Exception ex)
        {
            TempData["Error"] =
                $"Foto customer gagal diupload: {ex.Message}";
        }

        return RedirectToAction(
            nameof(Edit),
            new { id = customerId });
    }

    // ==========================================
    // EXPORT EXCEL
    // ==========================================
    [HttpGet]
    public async Task<IActionResult> ExportExcel(
        string? search = null,
        bool? isActive = null)
    {
        var customers =
            await GetAllCustomersForExport(
                search,
                isActive);

        using var workbook =
            new ClosedXML.Excel.XLWorkbook();

        var worksheet =
            workbook.Worksheets.Add("Customers");

        worksheet.Cell(1, 1).Value = "No";
        worksheet.Cell(1, 2).Value = "Full Name";
        worksheet.Cell(1, 3).Value = "Email";
        worksheet.Cell(1, 4).Value = "Phone";
        worksheet.Cell(1, 5).Value = "NPWP";
        worksheet.Cell(1, 6).Value = "Is Active";

        var header =
            worksheet.Range("A1:F1");

        header.Style.Font.Bold = true;

        header.Style.Alignment.Horizontal =
            ClosedXML.Excel.XLAlignmentHorizontalValues.Center;

        int row = 2;
        int no = 1;

        foreach (var item in customers)
        {
            worksheet.Cell(row, 1).Value = no;

            worksheet.Cell(row, 2).Value =
                string.IsNullOrWhiteSpace(item.FullName)
                    ? "-"
                    : item.FullName;

            worksheet.Cell(row, 3).Value =
                string.IsNullOrWhiteSpace(item.Email)
                    ? "-"
                    : item.Email;

            worksheet.Cell(row, 4).Value =
                string.IsNullOrWhiteSpace(item.Phone)
                    ? "-"
                    : item.Phone;

            worksheet.Cell(row, 5).Value =
                string.IsNullOrWhiteSpace(item.Npwp)
                    ? "-"
                    : item.Npwp;

            worksheet.Cell(row, 6).Value =
                item.IsActive
                    ? "Aktif"
                    : "Nonaktif";

            row++;
            no++;
        }

        worksheet.Columns().AdjustToContents();

        using var stream =
            new MemoryStream();

        workbook.SaveAs(stream);

        return File(
            stream.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"Customers_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
        );
    }


    // ==========================================
    // EXPORT PDF
    // ==========================================
    [HttpGet]
    public async Task<IActionResult> ExportPdf(
        string? search = null,
        bool? isActive = null)
    {
        var customers =
            await GetAllCustomersForExport(
                search,
                isActive);

        var listCustomers =
            customers.ToList();

        var document =
            QuestPDF.Fluent.Document.Create(container =>
            {
                container.Page(page =>
                {
                    // ==========================================
                    // PAGE SETTING
                    // ==========================================
                    page.Size(
                        QuestPDF.Helpers.PageSizes.A4.Landscape());

                    page.Margin(30);

                    page.DefaultTextStyle(
                        x => x
                            .FontSize(9.5f)
                            .FontColor("#333333"));


                    // ==========================================
                    // HEADER DOKUMEN
                    // ==========================================
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
                                    text.Span("Customers")
                                        .FontColor("#6c757d");

                                    text.Span(" · ")
                                        .FontColor("#6c757d");

                                    text.Span(
                                        $"Dicetak: {DateTime.Now:dd/MM/yyyy HH:mm}")
                                        .FontColor("#6c757d");

                                    text.Span(" · ")
                                        .FontColor("#6c757d");

                                    text.Span(
                                        $"Total: {listCustomers.Count} data")
                                        .FontColor("#6c757d");
                                });
                        });


                    // ==========================================
                    // ISI TABEL
                    // ==========================================
                    page.Content()
                        .PaddingTop(15)
                        .Table(table =>
                        {
                            // ==========================================
                            // DEFINISI KOLOM
                            // ==========================================
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(35);  // No
                                columns.RelativeColumn(2.5f); // Full Name
                                columns.RelativeColumn(2.5f); // Email
                                columns.RelativeColumn(1.5f); // Phone
                                columns.RelativeColumn(2f);   // NPWP
                                columns.ConstantColumn(65);   // Is Active
                            });


                            // ==========================================
                            // HEADER TABEL
                            // ==========================================
                            table.Header(header =>
                            {
                                header.Cell()
                                    .Element(HeaderStyle)
                                    .Text("No");

                                header.Cell()
                                    .Element(HeaderStyle)
                                    .Text("Full Name");

                                header.Cell()
                                    .Element(HeaderStyle)
                                    .Text("Email");

                                header.Cell()
                                    .Element(HeaderStyle)
                                    .Text("Phone");

                                header.Cell()
                                    .Element(HeaderStyle)
                                    .Text("NPWP");

                                header.Cell()
                                    .Element(HeaderStyle)
                                    .Text("Is Active");


                                static QuestPDF.Infrastructure.IContainer
                                    HeaderStyle(
                                        QuestPDF.Infrastructure.IContainer c)
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
                                                .FontColor("#ffffff"));
                                }
                            });


                            // ==========================================
                            // DATA BARIS
                            // ==========================================
                            int no = 1;

                            foreach (var item in listCustomers)
                            {
                                // Zebra striping
                                string bgColor =
                                    (no % 2 == 0)
                                        ? "#f8f9fa"
                                        : "#ffffff";


                                table.Cell()
                                    .Element(c =>
                                        Cell(c, bgColor))
                                    .Text(no.ToString());


                                table.Cell()
                                    .Element(c =>
                                        Cell(c, bgColor))
                                    .Text(
                                        string.IsNullOrWhiteSpace(
                                            item.FullName)
                                            ? "-"
                                            : item.FullName);


                                table.Cell()
                                    .Element(c =>
                                        Cell(c, bgColor))
                                    .Text(
                                        string.IsNullOrWhiteSpace(
                                            item.Email)
                                            ? "-"
                                            : item.Email);


                                table.Cell()
                                    .Element(c =>
                                        Cell(c, bgColor))
                                    .Text(
                                        string.IsNullOrWhiteSpace(
                                            item.Phone)
                                            ? "-"
                                            : item.Phone);


                                table.Cell()
                                    .Element(c =>
                                        Cell(c, bgColor))
                                    .Text(
                                        string.IsNullOrWhiteSpace(
                                            item.Npwp)
                                            ? "-"
                                            : item.Npwp);


                                table.Cell()
                                    .Element(c =>
                                        Cell(c, bgColor))
                                    .Text(
                                        item.IsActive
                                            ? "Aktif"
                                            : "Nonaktif");


                                no++;
                            }


                            // ==========================================
                            // STYLE CELL
                            // ==========================================
                            static QuestPDF.Infrastructure.IContainer Cell(
                                QuestPDF.Infrastructure.IContainer c,
                                string backgroundColor)
                            {
                                return c
                                    .Border(1)
                                    .BorderColor("#dee2e6")
                                    .Background(backgroundColor)
                                    .PaddingVertical(6)
                                    .PaddingHorizontal(8);
                            }
                        });


                    // ==========================================
                    // FOOTER
                    // ==========================================
                    page.Footer()
                        .AlignLeft()
                        .PaddingTop(10)
                        .Text(
                            "Dokumen dihasilkan otomatis oleh sistem.")
                        .FontSize(8.5f)
                        .FontColor("#888888");
                });
            });


        var pdf =
            document.GeneratePdf();

        return File(
            pdf,
            "application/pdf",
            $"Customers_{DateTime.Now:yyyyMMdd_HHmmss}.pdf"
        );
    }


    // ==========================================
    // PRINT
    // ==========================================
    [HttpGet]
    public async Task<IActionResult> Print(
        string? search = null,
        bool? isActive = null)
    {
        var customers =
            await GetAllCustomersForExport(
                search,
                isActive);

        var model =
            new PagedResult<CustomerViewModel>
            {
                Data = customers,
                Page = 1,
                PageSize = customers.Count,
                TotalCount = customers.Count,
                TotalData = customers.Count
            };

        return View(
            "Print",
            model);
    }

    // =========================================================
    // LOAD DROPDOWNS
    // =========================================================

    private async Task LoadDropdowns()
    {
        ViewBag.CustomerGroups =  await _api.GetCustomerGroupsForCustomerAsync(); 
        ViewBag.SalesForces =  await _api.GetSalesForcesForCustomerAsync(); 
        ViewBag.PriceLists = await _api.GetPriceListsForCustomerAsync(); 
        ViewBag.ShippingAreas = await _api.GetShippingAreasForCustomerAsync();
    }

    // =========================================================
    // LOAD SHIPPING ADDRESSES
    // =========================================================

    private async Task LoadShippingAddresses(
        long customerId)
    {
        var shippingAddresses =
            await _api.GetShippingAddressesByCustomerAsync(
                customerId);

        ViewBag.ShippingAddresses =
            shippingAddresses;

        ViewBag.ShippingAddressesCount =
            shippingAddresses.Count;
    }

    // =========================================================
    // GET ALL DATA FOR EXPORT / PRINT
    // =========================================================

    private async Task<List<CustomerViewModel>>
        GetAllCustomersForExport(
            string? search = null,
            bool? isActive = null)
    {
        var allCustomers =
            new List<CustomerViewModel>();

        var page = 1;

        const int pageSize = 100;

        while (true)
        {
            var result =
                await _api.GetCustomersAsync(
                    search,
                    isActive,
                    page,
                    pageSize);

            if (result.Data == null ||
                result.Data.Count == 0)
            {
                break;
            }

            allCustomers.AddRange(
                result.Data);

            if (result.Data.Count < pageSize)
            {
                break;
            }

            if (result.TotalCount > 0 &&
                allCustomers.Count >=
                result.TotalCount)
            {
                break;
            }

            page++;
        }

        return allCustomers;
    }

    // =========================================================
    // CREATE SHIPPING ADDRESS
    // =========================================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult>
        CreateShippingAddress(
        long customerId,
        string name,
        string address,
        long? shippingAreaId)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["Error"] =
                "Nama alamat wajib diisi.";

            return RedirectToAction(
                nameof(Edit),
                new { id = customerId });
        }

        if (string.IsNullOrWhiteSpace(address))
        {
            TempData["Error"] =
                "Alamat wajib diisi.";

            return RedirectToAction(
                nameof(Edit),
                new { id = customerId });
        }

        var success =
            await _api.CreateShippingAddressAsync(
                customerId,
                name,
                address,
                shippingAreaId);

        if (!success)
        {
            TempData["Error"] =
                "Alamat pengiriman gagal ditambahkan.";
        }
        else
        {
            TempData["Success"] =
                "Alamat pengiriman berhasil ditambahkan.";
        }

        return RedirectToAction(
            nameof(Edit),
            new { id = customerId });
    }

    // =========================================================
    // EDIT SHIPPING ADDRESS
    // =========================================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult>
        EditShippingAddress(
        long id,
        long customerId,
        string name,
        string address,
        long? shippingAreaId)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["Error"] =
                "Nama alamat wajib diisi.";

            return RedirectToAction(
                nameof(Edit),
                new { id = customerId });
        }

        if (string.IsNullOrWhiteSpace(address))
        {
            TempData["Error"] =
                "Alamat wajib diisi.";

            return RedirectToAction(
                nameof(Edit),
                new { id = customerId });
        }

        var success =
            await _api.UpdateShippingAddressAsync(
                id,
                customerId,
                name,
                address,
                shippingAreaId);

        if (!success)
        {
            TempData["Error"] =
                "Alamat pengiriman gagal diperbarui.";
        }
        else
        {
            TempData["Success"] =
                "Alamat pengiriman berhasil diperbarui.";
        }

        return RedirectToAction(
            nameof(Edit),
            new { id = customerId });
    }

    // =========================================================
    // DELETE SHIPPING ADDRESS
    // =========================================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteShippingAddress(
        long id,
        long customerId)
    {
        var success =
            await _api.DeleteShippingAddressAsync(id);

        if (success)
        {
            TempData["Success"] =
                "Alamat pengiriman berhasil dihapus.";
        }
        else
        {
            TempData["Error"] =
                "Alamat pengiriman gagal dihapus.";
        }

        return RedirectToAction(
            nameof(Edit),
            new { id = customerId });
    }

}