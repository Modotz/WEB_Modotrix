using DocumentFormat.OpenXml.Spreadsheet;
using ERP.Web.Models;
using ERP.Web.Services;
using Microsoft.AspNetCore.Mvc;
using QRCoder;
using QuestPDF.Fluent;
using QuestPDF.Helpers;

namespace ERP.Web.Controllers;

public class ProductsController : Controller
{
    private readonly ApiClient _api;

    public ProductsController(ApiClient api)
    {
        _api = api;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
    string? search = null,
    long? brandId = null,
    long? categoryId = null,
    long? subCategoryId = null,
    string? productType = null,
    long? unitId = null,
    bool? trackStock = null,
    bool? isActive = null,
    bool showVariant = false,
    int page = 1,
    int pageSize = 10)
    {
        var result = await _api.GetProductsAsync(
            search,
            brandId,
            categoryId,
            subCategoryId,
            productType,
            unitId,
            trackStock,
            isActive,
            showVariant,
            page,
            pageSize
        );

        var allCategories = await _api.GetCategoriesAsync();

        ViewBag.Categories = allCategories
            .Where(x =>
                !x.ParentId.HasValue ||
                x.ParentId.Value == 0)
            .ToList();

        //ViewBag.SubCategories = allCategories
        //    .Where(x =>
        //        x.ParentId.HasValue &&
        //        x.ParentId.Value > 0)
        //    .ToList();

        ViewBag.Units = await _api.GetUnitsForDropdownAsync(); 
        ViewBag.Brands = await _api.GetBrandsForDropdownAsync();

        return View(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetById(long id)
    {
        var product = await _api.GetProductAsync(id);

        if (product is null)
            return NotFound();

        return Json(product);
    }


    [HttpGet]
    public async Task<IActionResult> GetSubCategories(long categoryId)
    {
        if (categoryId <= 0)
        {
            return Json(new List<DropdownItemViewModel>());
        }

        var subCategories = await _api.GetSubCategoriesAsync(categoryId);

        return Json(subCategories);
    }


    private async Task LoadProductDropdowns(ProductViewModel model)
    { 
        var allCategories = await _api.GetCategoriesAsync();

        // Kategori utama: ParentId kosong atau 0
        ViewBag.Categories = allCategories
            .Where(x => !x.ParentId.HasValue || x.ParentId.Value == 0)
            .ToList();

        // Seluruh subkategori: ParentId memiliki nilai
        ViewBag.SubCategories = allCategories
            .Where(x => x.ParentId.HasValue && x.ParentId.Value > 0)
            .ToList();

        ViewBag.Units = await _api.GetUnitsForDropdownAsync();
        ViewBag.Brands = await _api.GetBrandsForDropdownAsync();
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var model = new ProductViewModel
        {
            IsActive = true,
            TrackStock = true,
            TrackSerial = false,
            WarrantyMonths = 0
        };

        await LoadProductDropdowns(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProductViewModel model)
    {
        // Validasi manual
        if (string.IsNullOrWhiteSpace(model.Sku))
        {
            ModelState.AddModelError(
                nameof(model.Sku),
                "SKU wajib diisi."
            );
        }

        if (string.IsNullOrWhiteSpace(model.Name))
        {
            ModelState.AddModelError(
                nameof(model.Name),
                "Nama Produk wajib diisi."
            );
        }

        if (string.IsNullOrWhiteSpace(model.ProductType))
        {
            ModelState.AddModelError(
                nameof(model.ProductType),
                "Jenis Produk wajib dipilih."
            );
        }

        if (!model.UnitId.HasValue || model.UnitId.Value <= 0)
        {
            ModelState.AddModelError(
                nameof(model.UnitId),
                "Unit wajib dipilih."
            );
        }

        // Jika tidak valid, kembali ke halaman dengan error
        if (!ModelState.IsValid)
        {
            await LoadProductDropdowns(model);

            return View(model);
        }


        try
        {

            if (model.ImageFile != null && model.ImageFile.Length > 0)
            {
                var uploadResult = await _api.UploadProductImageAsync(model.ImageFile);

                if (!uploadResult.Success)
                {
                    TempData["Error"] =
                        uploadResult.Error ?? "Gagal mengunggah gambar produk.";

                    await LoadProductDropdowns(model);
                    return View(model);
                }

                model.Image = uploadResult.Image;
            }

            var result = await _api.CreateProductAsync(model);

            Console.WriteLine(
                $"CreateProductAsync -> Success: {result.Success}"
            );

            Console.WriteLine(
                $"CreateProductAsync -> Error: {result.Error ?? "(tidak ada error)"}"
            );

            if (!result.Success)
            {
                TempData["Error"] =
                    result.Error ?? "Gagal menambahkan produk.";

                await LoadProductDropdowns(model);

                return View(model);
            }

            TempData["Success"] =
                "Produk berhasil ditambahkan.";

            return RedirectToAction(nameof(Index));
        }

        catch (Exception ex)
        {
            Console.WriteLine(
                $"ERROR CREATE PRODUCT: {ex}"
            );

            TempData["Error"] =
                $"Terjadi kesalahan: {ex.Message}";

            await LoadProductDropdowns(model);

            return View(model);
        }
    }


    [HttpGet]
    public async Task<IActionResult> Edit(long id)
    {
        var product = await _api.GetProductAsync(id);

        if (product is null)
        {
            return NotFound();
        }

        await LoadProductDropdowns(product);

        return View(product);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ProductViewModel model)
    {
        ModelState.Remove(nameof(model.Sku));
        ModelState.Remove(nameof(model.ImageFile));

        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Data produk belum lengkap.";

            await LoadProductDropdowns(model);

            return View(model);
        }

        try
        { 
            var existingProduct = await _api.GetProductAsync(model.Id);

            if (existingProduct is null)
            {
                return NotFound();
            }

            if (model.ImageFile != null && model.ImageFile.Length > 0)
            {
                var uploadResult = await _api.UploadProductImageAsync(model.ImageFile);

                if (!uploadResult.Success)
                {
                    TempData["Error"] =
                        uploadResult.Error ?? "Gagal mengunggah gambar produk.";

                    await LoadProductDropdowns(model);
                    return View(model);
                }

                model.Image = uploadResult.Image;
            }
            else
            {
                // Pertahankan gambar lama jika tidak memilih gambar baru.
                model.Image = existingProduct.Image;
            }

            var result = await _api.UpdateProductAsync(model);

            TempData[result.Success ? "Success" : "Error"] =
                result.Success
                    ? "Produk berhasil diubah."
                    : result.Error ?? "Gagal mengubah produk.";

            if (!result.Success)
            {
                await LoadProductDropdowns(model);
                return View(model);
            }

            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR EDIT PRODUCT: {ex}");

            TempData["Error"] =
                $"Terjadi kesalahan: {ex.Message}";

            await LoadProductDropdowns(model);

            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Copy(long id)
    {
        var product = await _api.GetProductAsync(id);

        if (product is null)
        {
            return NotFound();
        }

        await LoadProductDropdowns(product); 
        return View(product);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Copy(ProductViewModel model)
    {
        // ID yang dikirim dari form adalah ID produk ASLI
        var originalId = model.Id;

        // Sku dan ImageFile tidak perlu divalidasi sebagai data edit
        ModelState.Remove(nameof(model.Sku));
        ModelState.Remove(nameof(model.ImageFile));

        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Data produk belum lengkap.";

            await LoadProductDropdowns(model);

            return View(model);
        }

        try
        { 
            var existingProduct = await _api.GetProductAsync(originalId);

            if (existingProduct is null)
            {
                TempData["Error"] =
                    $"Produk dengan ID {originalId} tidak ditemukan.";

                await LoadProductDropdowns(model);

                return View(model);
            }


            if (model.ImageFile != null && model.ImageFile.Length > 0)
            {
                var uploadResult = await _api.UploadProductImageAsync(model.ImageFile);

                if (!uploadResult.Success)
                {
                    TempData["Error"] =
                        uploadResult.Error ?? "Gagal mengunggah gambar produk.";

                    await LoadProductDropdowns(model);
                    return View(model);
                }

                model.Image = uploadResult.Image;
            }
            else
            {
                // Pertahankan gambar lama jika tidak memilih gambar baru.
                model.Image = existingProduct.Image;
            }


            model.Id = 0; 
            model.Uid = null; 
            model.ProductNumber = null; 
            model.ParentId = null; 
            model.HasVariants = false;
             
            if (string.IsNullOrWhiteSpace(model.Sku))
            {
                model.Sku = $"{existingProduct.Sku}-Salinan";
            }

            if (string.IsNullOrWhiteSpace(model.Name))
            {
                model.Name = $"{existingProduct.Name} (Salinan)";
            }
             
            var result = await _api.CreateProductAsync(model);

            if (!result.Success)
            {
                TempData["Error"] =
                    result.Error ??
                    "Gagal menyalin produk.";

                await LoadProductDropdowns(model);

                return View(model);
            }
             
            TempData["Success"] = "Produk berhasil disalin."; 
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR COPY PRODUCT: {ex}");

            TempData["Error"] =
                $"Terjadi kesalahan: {ex.Message}";

            await LoadProductDropdowns(model);

            return View(model);
        }
    }

    // DELETE di MVC juga POST.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(long id)
    {
        var result = await _api.DeleteProductAsync(id);

        TempData[result.Success ? "Success" : "Error"] =
            result.Success
                ? "Produk berhasil dihapus."
                : result.Error ?? "Gagal menghapus produk.";

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Unit(long id)
    {
        var product = await _api.GetProductAsync(id);

        if (product is null)
        {
            return NotFound();
        }

        await LoadProductDropdowns(product);

        return View(product);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SyncVariantPrices(long id)
    {
        if (id <= 0)
        {
            TempData["Error"] = "ID produk tidak valid.";
            return RedirectToAction(nameof(Edit), new { id });
        }

        try
        { 
            var result = await _api.SyncPricesToVariantsAsync(id);

            if (!result.Success)
            {
                TempData["Error"] =
                    result.Error ?? "Gagal menyamakan harga varian.";

                return RedirectToAction(nameof(Edit), new { id });
            }

            TempData["Success"] =
                result.Error ?? "Harga varian berhasil disamakan dengan produk induk.";

            return RedirectToAction(nameof(Edit), new { id });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR SYNC VARIANT PRICES: {ex}");

            TempData["Error"] =
                $"Terjadi kesalahan saat menyamakan harga varian: {ex.Message}";

            return RedirectToAction(nameof(Edit), new { id });
        }
    }

    //export-excel
    [HttpGet]
    public async Task<IActionResult> ExportExcel()
    {
        var products = await _api.GetProductsForExportAsync();

        using var workbook = new ClosedXML.Excel.XLWorkbook();

        var worksheet = workbook.Worksheets.Add("Product");

        worksheet.Cell(1, 1).Value = "No";
        worksheet.Cell(1, 2).Value = "Sku";
        worksheet.Cell(1, 3).Value = "Barcode";
        worksheet.Cell(1, 4).Value = "Name";
        worksheet.Cell(1, 5).Value = "Category";
        worksheet.Cell(1, 6).Value = "Unit";
        worksheet.Cell(1, 7).Value = "Cost Price";
        worksheet.Cell(1, 8).Value = "SellPrice";
        worksheet.Cell(1, 9).Value = "Min Stock";
        worksheet.Cell(1, 10).Value = "Track Stock";
        worksheet.Cell(1, 11).Value = "Status";
        worksheet.Cell(1, 12).Value = "Image";

        var header = worksheet.Range("A1:L1");

        header.Style.Font.Bold = true;
        header.Style.Alignment.Horizontal =
            ClosedXML.Excel.XLAlignmentHorizontalValues.Center;

        int row = 2;
        int no = 1;

        foreach (var item in products)
        {
            worksheet.Cell(row, 1).Value = no;
            worksheet.Cell(row, 2).Value = item.Sku;
            worksheet.Cell(row, 3).Value = item.Barcode;
            worksheet.Cell(row, 4).Value = item.Name;
            worksheet.Cell(row, 5).Value = item.CategoryId;
            worksheet.Cell(row, 6).Value = item.UnitId;
            worksheet.Cell(row, 7).Value = item.CostPrice;
            worksheet.Cell(row, 8).Value = item.SellPrice;
            worksheet.Cell(row, 9).Value = item.MinStock;
            worksheet.Cell(row, 10).Value = item.TrackStock ? "Ya" : "Tidak";
            worksheet.Cell(row, 11).Value =
                item.IsActive ? "Aktif" : "Nonaktif";
            worksheet.Cell(row, 12).Value = item.Image;

            row++;
            no++;
        }

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();

        workbook.SaveAs(stream);

        return File(
            stream.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"Product_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
        );
    }

    //export-pdf
    [HttpGet]
    public async Task<IActionResult> ExportPdf()
    {
        var products = await _api.GetProductsForExportAsync();
        var listProducts = products.ToList();  

        var document = QuestPDF.Fluent.Document.Create(container =>
        {
            container.Page(page =>
            { 
                page.Size(QuestPDF.Helpers.PageSizes.A4.Landscape());
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(9.5f).FontColor("#333333"));
                 
                page.Header().Column(col =>
                {
                    col.Item().Text("ERP-LITE")
                        .Bold()
                        .FontSize(18)
                        .FontColor("#1a1a1a");

                    col.Item().PaddingTop(2).Text(text =>
                    {
                        text.Span("Product").FontColor("#6c757d");
                        text.Span(" · ").FontColor("#6c757d");
                        text.Span($"Dicetak: {DateTime.Now:dd/MM/yyyy HH:mm}").FontColor("#6c757d");
                        text.Span(" · ").FontColor("#6c757d");
                        text.Span($"Total: {listProducts.Count} data").FontColor("#6c757d");
                    });
                });

                // Isi Tabel
                page.Content().PaddingTop(15).Table(table =>
                {
                    // Definisi Lebar Kolom (Sesuai dengan gambar tanpa kolom Address)
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(4);   // No (4%)
                        columns.RelativeColumn(15);  // Sku (15%)
                        columns.RelativeColumn(20);  // Barcode (20%)
                        columns.RelativeColumn(20);  // Name (20%)
                        columns.RelativeColumn(8);   // Kategori (8%)
                        columns.RelativeColumn(5);   // Unit (5%)
                        columns.RelativeColumn(5);   // Cost Price (5%)
                        columns.RelativeColumn(5);   // Sell Price (5%)
                        columns.RelativeColumn(5);   // Min Stock (5%)
                        columns.RelativeColumn(5);   // Track Stock (5%)
                        columns.RelativeColumn(5);   // Status / Is Active (5%)
                        columns.RelativeColumn(20);  // Image (20%)
                    });

                    // Header Tabel (Biru dengan teks Putih)
                    table.Header(header =>
                    {
                        header.Cell().Element(HeaderStyle).Text("No");
                        header.Cell().Element(HeaderStyle).Text("Sku");
                        header.Cell().Element(HeaderStyle).Text("Barcode");
                        header.Cell().Element(HeaderStyle).Text("Name");
                        header.Cell().Element(HeaderStyle).Text("Category");
                        header.Cell().Element(HeaderStyle).Text("Unit");
                        header.Cell().Element(HeaderStyle).Text("Cost Price");
                        header.Cell().Element(HeaderStyle).Text("Sell Price");
                        header.Cell().Element(HeaderStyle).Text("Min Stock");
                        header.Cell().Element(HeaderStyle).Text("Track Stock");
                        header.Cell().Element(HeaderStyle).Text("Is Active");
                        header.Cell().Element(HeaderStyle).Text("Image");

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
                     
                    int no = 1;
                    foreach (var item in listProducts)
                    { 
                        string bgBgColor = (no % 2 == 0) ? "#f8f9fa" : "#ffffff";

                        table.Cell().Element(c => Cell(c, bgBgColor)).Text(no.ToString());
                        table.Cell().Element(c => Cell(c, bgBgColor)).Text(item.Sku ?? "");
                        table.Cell().Element(c => Cell(c, bgBgColor)).Text(item.Barcode ?? "");
                        table.Cell().Element(c => Cell(c, bgBgColor)).Text(item.Name ?? "");
                        table.Cell().Element(c => Cell(c, bgBgColor)).Text(item.CategoryId.ToString() ?? "");
                        table.Cell().Element(c => Cell(c, bgBgColor)).Text(item.UnitId.ToString() ?? "");
                        table.Cell().Element(c => Cell(c, bgBgColor)).Text(item.CostPrice?.ToString("C") ?? "");
                        table.Cell().Element(c => Cell(c, bgBgColor)).Text(item.SellPrice?.ToString("C") ?? "");
                        table.Cell().Element(c => Cell(c, bgBgColor)).Text(item.MinStock?.ToString() ?? "");
                        table.Cell().Element(c => Cell(c, bgBgColor)).Text(item.TrackStock ? "Ya" : "Tidak");
                        table.Cell().Element(c => Cell(c, bgBgColor)).Text(item.IsActive ? "Aktif" : "Nonaktif");
                        table.Cell().Element(c => Cell(c, bgBgColor)).Text(item.Image?.ToString() ?? "");

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
            $"Product_{DateTime.Now:yyyyMMdd_HHmmss}.pdf"
        );
    }

    //print
    [HttpGet]
    public async Task<IActionResult> Print()
    {
        var products = await _api.GetProductsForExportAsync();

        var model = new PagedResult<ProductViewModel>
        {
            Data = products,
            Page = 1,
            PageSize = products.Count,
            TotalCount = products.Count,
            TotalData = products.Count,
             
        };

        return View(model);
    }


    [HttpGet]
    [Route("Products/Label/{uid}")]
    public async Task<IActionResult> Label(
        string uid,
        int per = 10,
        int count = 1)
    { 
        if (string.IsNullOrWhiteSpace(uid))
        {
            return NotFound();
        }
         
        if (per <= 0)
        {
            per = 1;
        }

        if (count <= 0)
        {
            count = 1;
        }
          
        var product = await _api.GetProductByUidAsync(uid);

        if (product == null)
        {
            return NotFound();
        }
         
        var culture = System.Globalization.CultureInfo.InvariantCulture;

        string FormatDimension(decimal? value)
        {
            return value.HasValue
                ? value.Value.ToString("0.##", culture)
                : "";
        }

        var length = FormatDimension(product.DefLength);
        var width = FormatDimension(product.DefWidth);
        var height = FormatDimension(product.DefHeight);
         
        var dimensionsAvailable =
            !string.IsNullOrWhiteSpace(length) &&
            !string.IsNullOrWhiteSpace(width) &&
            !string.IsNullOrWhiteSpace(height);

        var dimensions = dimensionsAvailable
            ? $"{length}X{width}X{height}"
            : "";
         
        var barcodeValue = dimensions;
         
        var qrContent =
            $"UID: {product.Uid ?? uid}\n" +
            $"Nama Produk: {product.Name ?? "-"}\n" +
            $"SKU: {product.Sku ?? "-"}\n" +
            $"Nomor Produk: {product.ProductNumber ?? "-"}\n" +
            $"Panjang: {(length == "" ? "-" : length)}\n" +
            $"Lebar: {(width == "" ? "-" : width)}\n" +
            $"Tinggi: {(height == "" ? "-" : height)}\n" +
            $"Ukuran: {(dimensionsAvailable ? dimensions : "Belum tersedia")}";

        // 7. Generate QR Code satu kali
        using var qrGenerator = new QRCoder.QRCodeGenerator();

        using var qrCodeData = qrGenerator.CreateQrCode(
            qrContent,
            QRCoder.QRCodeGenerator.ECCLevel.Q
        );

        var pngQrCode = new QRCoder.PngByteQRCode(qrCodeData);

        byte[] qrBytes = pngQrCode.GetGraphic(10);

        var qrCodeBase64 = Convert.ToBase64String(qrBytes);
         
        var model = new ProductLabelViewModel
        {
            Uid = product.Uid ?? uid,

            ProductName = product.Name ?? "",
            Sku = product.Sku ?? "",
            ProductNumber = product.ProductNumber,

            DefLength = product.DefLength,
            DefWidth = product.DefWidth,
            DefHeight = product.DefHeight,

            PerLabel = per,
            LabelCount = count,
            TotalPcs = per * count,

            QrCodeBase64 = qrCodeBase64,

            Dimensions = dimensions,
            BarcodeValue = barcodeValue,

            OperatorName = User.Identity?.Name ?? "ADMINISTRATOR",
            PrintDate = DateTime.Now
        };
         
        System.Diagnostics.Debug.WriteLine(
            $"[LABEL] UID: {model.Uid} | " +
            $"Nama: {model.ProductName} | " +
            $"SKU: {model.Sku} | " +
            $"Nomor Produk: {model.ProductNumber} | " +
            $"Panjang: {model.DefLength} | " +
            $"Lebar: {model.DefWidth} | " +
            $"Tinggi: {model.DefHeight} | " +
            $"Dimensions: {model.Dimensions} | " +
            $"BarcodeValue: {model.BarcodeValue}"
        );
         
        return View(model);
    }




}
