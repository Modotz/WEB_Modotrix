using ERP.Web.Models;
using Microsoft.AspNetCore.Http;
using ERP.Web.Services;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ERP.Web.Controllers;

public class UsersController : Controller
{
    private readonly ApiClient _api;

    public UsersController(ApiClient api)
    {
        _api = api;
    }

    //[HttpGet]
    //public async Task<IActionResult> IndexOld(string? search = null)
    //{
    //    ViewBag.Search = search;
    //    var users = await _api.GetUsersAsync(search);
    //    return View(users);
    //}

    [HttpGet]
    public async Task<IActionResult> Index(
        string? search = null,
        int? roleId = null,
        bool? isActive = null,
        int page = 1,
        int pageSize = 10)
    {
        var result = await _api.GetUsersAsync(
            search,
            roleId,
            isActive,
            page,
            pageSize
        );

        var roles = await _api.GetAllRolesAsync();

        ViewBag.Search = search;
        ViewBag.RoleId = roleId;
        ViewBag.IsActive = isActive;
        ViewBag.Roles = roles;

        return View(result);
    }


    // GET DATA USER BERDASARKAN ID
    [HttpGet]
    public async Task<IActionResult> GetById(long id)
    {
        var user = await _api.GetUserAsync(id);

        if (user is null)
            return NotFound();

        return Json(user);
    }


    [HttpPost]
    public async Task<IActionResult> UploadPhoto(
      [FromForm(Name = "photo")] IFormFile photo)
    {
        if (photo == null || photo.Length == 0)
        {
            return BadRequest(new
            {
                message = "Foto belum dipilih di ERP.Web."
            });
        }

        var result =
            await _api.UploadUserPhotoAsync(photo);

        if (!result.Success)
        {
            return BadRequest(new
            {
                message =
                    string.IsNullOrWhiteSpace(result.Error)
                        ? "API gagal mengupload foto."
                        : result.Error
            });
        }

        return Ok(new
        {
            photo = result.Photo
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateOld(UserViewModel model)
    {
        if (string.IsNullOrWhiteSpace(model.Password))
            ModelState.AddModelError(nameof(model.Password), "Password wajib diisi.");

        if (!ModelState.IsValid)
        {
            var errors = ModelState
                .Where(x => x.Value != null && x.Value.Errors.Count > 0)
                .SelectMany(x => x.Value!.Errors.Select(e =>
                    $"{x.Key}: {e.ErrorMessage}"
                ))
                .ToList();

            return Content(string.Join(
                Environment.NewLine,
                errors
            ));
        }

        var result = await _api.CreateUserAsync(model);

        TempData[result.Success ? "Success" : "Error"] =
            result.Success
                ? "User berhasil ditambahkan."
                : result.Error;

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(UserViewModel model)
    {
        if (string.IsNullOrWhiteSpace(model.Password))
        {
            ModelState.AddModelError(
                nameof(model.Password),
                "Password wajib diisi."
            );
        }

        //if (!ModelState.IsValid)
        //{
        //    var errors = ModelState
        //        .Where(x => x.Value != null && x.Value.Errors.Count > 0)
        //        .Select(x => new
        //        {
        //            Field = x.Key,
        //            Errors = x.Value!.Errors
        //                .Select(e => e.ErrorMessage)
        //                .ToList()
        //        })
        //        .ToList();

        //    foreach (var error in errors)
        //    {
        //        Console.WriteLine(
        //            $"FIELD ERROR: {error.Field} -> " +
        //            string.Join(", ", error.Errors)
        //        );
        //    }

        //    TempData["Error"] = string.Join(
        //        " | ",
        //        errors.SelectMany(x =>
        //            x.Errors.Select(e => $"{x.Field}: {e}")
        //        )
        //    );

        //    return RedirectToAction(nameof(Index));
        //}

        //var result = await _api.CreateUserAsync(model);

        //if (!result.Success)
        //{
        //    TempData["Error"] =
        //        result.Error ?? "Gagal menambahkan user.";

        //    return RedirectToAction(nameof(Index));
        //}

        //TempData["Success"] =
        //    "User berhasil ditambahkan.";

        //return RedirectToAction(nameof(Index));

        if (!ModelState.IsValid)
        {
            var errors = ModelState
                .Where(x => x.Value != null && x.Value.Errors.Count > 0)
                .SelectMany(x => x.Value!.Errors.Select(e =>
                    $"{x.Key}: {e.ErrorMessage}"
                ))
                .ToList();

            TempData["Error"] = string.Join(" | ", errors);

            return RedirectToAction(nameof(Index));
        }

        var result = await _api.CreateUserAsync(model);

        TempData[result.Success ? "Success" : "Error"] =
            result.Success
                ? "User berhasil ditambahkan."
                : result.Error;

        return RedirectToAction(nameof(Index));
    }

    // EDIT di MVC juga POST.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditOld(UserViewModel model)
    {
        ModelState.Remove(nameof(model.Password));

        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Data user belum lengkap.";
            return RedirectToAction(nameof(Index));
        }

        var result = await _api.UpdateUserAsync(model);

        TempData[result.Success ? "Success" : "Error"] =
            result.Success
                ? "User berhasil diubah."
                : result.Error ?? "Gagal mengubah user.";

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(UserViewModel model)
    {
        if (!model.Id.HasValue)
        {
            TempData["Error"] = "ID user tidak valid.";
            return RedirectToAction(nameof(Index));
        }

        ModelState.Remove(nameof(model.Password));

        if (!ModelState.IsValid)
        {
            var errors = ModelState
                .Where(x => x.Value != null && x.Value.Errors.Count > 0)
                .SelectMany(x => x.Value!.Errors.Select(e =>
                    $"{x.Key}: {e.ErrorMessage}"
                ))
                .ToList();

            TempData["Error"] = string.Join(" | ", errors);
            return RedirectToAction(nameof(Index));
        }

        var result = await _api.UpdateUserAsync(model);

        TempData[result.Success ? "Success" : "Error"] =
            result.Success
                ? "User berhasil diubah."
                : result.Error ?? "Gagal mengubah user.";

        return RedirectToAction(nameof(Index));
    }

    // DELETE di MVC juga POST.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(long id)
    {
        var result = await _api.DeleteUserAsync(id);

        TempData[result.Success ? "Success" : "Error"] =
            result.Success
                ? "User berhasil dihapus."
                : result.Error ?? "Gagal menghapus user.";

        return RedirectToAction(nameof(Index));
    }

    //export-excel
    [HttpGet]
    public async Task<IActionResult> ExportExcel()
    {
        var users = await _api.GetUsersForExportAsync();

        using var workbook = new ClosedXML.Excel.XLWorkbook();

        var worksheet = workbook.Worksheets.Add("User");

        worksheet.Cell(1, 1).Value = "No";
        worksheet.Cell(1, 2).Value = "Nama";
        worksheet.Cell(1, 3).Value = "Username";
        worksheet.Cell(1, 4).Value = "Email";
        worksheet.Cell(1, 5).Value = "Role";
        worksheet.Cell(1, 6).Value = "Status";

        var header = worksheet.Range("A1:H1");

        header.Style.Font.Bold = true;
        header.Style.Alignment.Horizontal =
            ClosedXML.Excel.XLAlignmentHorizontalValues.Center;

        int row = 2;
        int no = 1;

        foreach (var item in users)
        {
            worksheet.Cell(row, 1).Value = no;
            worksheet.Cell(row, 2).Value = item.Name;
            worksheet.Cell(row, 3).Value = item.Username;
            worksheet.Cell(row, 4).Value = item.Email;
            worksheet.Cell(row, 5).Value = item.RoleName;
            worksheet.Cell(row, 6).Value =
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
            $"User_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
        );
    }

    //export-pdf
    [HttpGet]
    public async Task<IActionResult> ExportPdf()
    {
        var users = await _api.GetUsersForExportAsync();
        var listUsers = users.ToList(); // Mengubah ke List untuk mendapatkan Total Count

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
                        text.Span("User").FontColor("#6c757d");
                        text.Span(" · ").FontColor("#6c757d");
                        text.Span($"Dicetak: {DateTime.Now:dd/MM/yyyy HH:mm}").FontColor("#6c757d");
                        text.Span(" · ").FontColor("#6c757d");
                        text.Span($"Total: {listUsers.Count} data").FontColor("#6c757d");
                    });
                });

                // Isi Tabel
                page.Content().PaddingTop(15).Table(table =>
                {
                    // Definisi Lebar Kolom (Sesuai dengan gambar tanpa kolom Address)
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(35);   // No
                        columns.RelativeColumn(2);    // Name
                        columns.RelativeColumn(2);    // Username
                        columns.RelativeColumn(2.5f); // Email
                        columns.RelativeColumn(2.2f); // Role
                        columns.ConstantColumn(65);   // Status
                    });

                    // Header Tabel (Biru dengan teks Putih)
                    table.Header(header =>
                    {
                        header.Cell().Element(HeaderStyle).Text("No");
                        header.Cell().Element(HeaderStyle).Text("Name");
                        header.Cell().Element(HeaderStyle).Text("Username");
                        header.Cell().Element(HeaderStyle).Text("Email");
                        header.Cell().Element(HeaderStyle).Text("Role");
                        header.Cell().Element(HeaderStyle).Text("Status");

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
                    foreach (var item in listUsers)
                    {
                        // Latar belakang selang-seling (Zebra striping)
                        string bgBgColor = (no % 2 == 0) ? "#f8f9fa" : "#ffffff";

                        table.Cell().Element(c => Cell(c, bgBgColor)).Text(no.ToString());
                        table.Cell().Element(c => Cell(c, bgBgColor)).Text(item.Name ?? "");
                        table.Cell().Element(c => Cell(c, bgBgColor)).Text(item.Username ?? "");
                        table.Cell().Element(c => Cell(c, bgBgColor)).Text(item.Email ?? "");
                        table.Cell().Element(c => Cell(c, bgBgColor)).Text(item.RoleName ?? "");
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
            $"User_{DateTime.Now:yyyyMMdd_HHmmss}.pdf"
        );
    }

    //print
    [HttpGet]
    public async Task<IActionResult> Print()
    {
        var users = await _api.GetUsersForExportAsync();

        var model = new PagedResult<UserViewModel>
        {
            Data = users,
            Page = 1,
            PageSize = users.Count,
            TotalCount = users.Count,
            TotalData = users.Count,

        };

        return View(model);
    }

}
