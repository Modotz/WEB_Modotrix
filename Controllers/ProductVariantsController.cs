using ERP.Web.Models;
using ERP.Web.Services;
using Microsoft.AspNetCore.Mvc;
using QuestPDF.Fluent;
using QuestPDF.Helpers;

namespace ERP.Web.Controllers;

public class ProductVariantsController : Controller
{
    private readonly ApiClient _api;
    private readonly IWebHostEnvironment _environment;
    private readonly Translator _t;

    public ProductVariantsController(
        ApiClient api,
        IWebHostEnvironment environment,
        Translator t)
    {
        _api = api;
        _environment = environment;
        _t = t;
    }
     
    [HttpGet("product-variants/{uid:guid}")]
    public async Task<IActionResult> Index(
        Guid uid,
        string? search = null,
        bool? isActive = null,
        bool? trackStock = null,
        int page = 1,
        int pageSize = 10)
    {
        // Ambil produk induk
        var parentProduct = await _api.GetProductAsync(uid);

        if (parentProduct == null)
        {
            TempData["Error"] = "Produk tidak ditemukan.";
            return RedirectToAction("Index", "Products");
        }

        // Ambil semua variant dari produk induk
        var allVariants = await _api.GetVariantsAsync(uid);

        // SEARCH
        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();

            allVariants = allVariants
                .Where(x =>
                    (x.Name?.Contains(
                        search,
                        StringComparison.OrdinalIgnoreCase
                    ) ?? false)

                    ||

                    (x.Sku?.Contains(
                        search,
                        StringComparison.OrdinalIgnoreCase
                    ) ?? false)

                    ||

                    (x.ProductNumber?.Contains(
                        search,
                        StringComparison.OrdinalIgnoreCase
                    ) ?? false)

                    ||

                    (x.Barcode?.Contains(
                        search,
                        StringComparison.OrdinalIgnoreCase
                    ) ?? false)
                )
                .ToList();
        }

        // FILTER IS ACTIVE
        if (isActive.HasValue)
        {
            allVariants = allVariants
                .Where(x => x.IsActive == isActive.Value)
                .ToList();
        }

        // FILTER TRACK STOCK
        if (trackStock.HasValue)
        {
            allVariants = allVariants
                .Where(x => x.TrackStock == trackStock.Value)
                .ToList();
        }

        // PAGINATION
        page = Math.Max(1, page);

        pageSize = pageSize is 10 or 25 or 50 or 100
            ? pageSize
            : 10;

        var totalCount = allVariants.Count;

        var totalPage = totalCount == 0
            ? 1
            : (int)Math.Ceiling(
                totalCount / (double)pageSize
            );

        if (page > totalPage)
        {
            page = totalPage;
        }

        var pagedData = allVariants
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var result = new PagedResult<ProductViewModel>
        {
            Data = pagedData,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalData = totalCount
        };

        ViewBag.ParentProduct = parentProduct;

        return View(
            "~/Views/ProductVariants/Index.cshtml",
            result
        );
    }
     
    [HttpGet]
    public async Task<IActionResult> GetById(Guid uid)
    {
        if (uid == Guid.Empty)
        {
            return BadRequest();
        }

        var product = await _api.GetProductAsync(uid);

        if (product == null)
        {
            return NotFound();
        }

        return Json(product);
    }

     
    [HttpGet]
    public async Task<IActionResult> GetSubCategories(long categoryId)
    {
        if (categoryId <= 0)
        {
            return Json(
                new List<DropdownItemViewModel>()
            );
        }

        var subCategories = await _api.GetSubCategoriesAsync(categoryId);

        return Json(subCategories);
    }

     
    private async Task LoadProductDropdowns(
        ProductViewModel model)
    {
        var allCategories = await _api.GetCategoriesAsync();
         
        ViewBag.Categories = allCategories
            .Where(x =>
                !x.ParentId.HasValue ||
                x.ParentId.Value == 0
            )
            .ToList();
         
        ViewBag.SubCategories = allCategories
            .Where(x =>
                x.ParentId.HasValue &&
                x.ParentId.Value > 0
            )
            .ToList();
         
        ViewBag.Units = await _api.GetUnitsForDropdownAsync(); 
        ViewBag.Brands =  await _api.GetBrandsForDropdownAsync();
    }

     
    [HttpGet("product-variants/create/{uid:guid}")]
    public async Task<IActionResult> Create(Guid uid)
    {
        if (uid == Guid.Empty)
        {
            TempData["Error"] = "Produk tidak ditemukan.";
            return RedirectToAction("Index", "Products");
        }
         
        var parentProduct = await _api.GetProductAsync(uid);

        if (parentProduct == null)
        {
            TempData["Error"] = "Produk tidak ditemukan.";
            return RedirectToAction("Index", "Products");
        }
         
        var model = new ProductViewModel
        {
            // PENTING
            ParentId = parentProduct.Id,
            ParentUid = parentProduct.Uid,

            // Mengikuti produk induk
            BrandId = parentProduct.BrandId,
            CategoryId = parentProduct.CategoryId,
            SubCategoryId = parentProduct.SubCategoryId,
            ProductType = parentProduct.ProductType,
            UnitId = parentProduct.UnitId,

            // Dimensi
            DimMode = parentProduct.DimMode,
            PriceMode = parentProduct.PriceMode,

            DefLength = parentProduct.DefLength,
            DefWidth = parentProduct.DefWidth,
            DefHeight = parentProduct.DefHeight,

            // Harga volume
            CostPerM3 = parentProduct.CostPerM3,
            SellPerM3 = parentProduct.SellPerM3,

            // Harga normal
            CostPrice = parentProduct.CostPrice,
            SellPrice = parentProduct.SellPrice,
            MinStock = parentProduct.MinStock,

            // Default variant
            IsActive = true,
            TrackStock = true,
            TrackSerial = false,
            WarrantyMonths = 0
        };

        ViewBag.ParentProduct = parentProduct;

        await LoadProductDropdowns(model);

        return View(model);
    }

     
    [HttpPost("product-variants/create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        ProductViewModel model)
    {
        // ParentUid (uid produk induk) harus berasal dari form hidden
        if (!Guid.TryParse(model.ParentUid, out var parentUid) ||
            parentUid == Guid.Empty)
        {
            TempData["Error"] =
                "Produk induk tidak ditemukan.";

            return RedirectToAction(
                "Index",
                "Products"
            );
        }

        // Ambil produk induk
        var parentProduct =
            await _api.GetProductAsync(
                parentUid
            );

        if (parentProduct == null)
        {
            TempData["Error"] =
                "Produk induk tidak ditemukan.";

            return RedirectToAction(
                "Index",
                "Products"
            );
        }
         

        if (string.IsNullOrWhiteSpace(model.Sku))
        {
            ModelState.AddModelError(
                nameof(model.Sku),
                _t["SKU wajib diisi."]
            );
        }

        if (string.IsNullOrWhiteSpace(model.Name))
        {
            ModelState.AddModelError(
                nameof(model.Name),
                _t["Nama Produk wajib diisi."]
            );
        }

        if (string.IsNullOrWhiteSpace(model.ProductType))
        {
            ModelState.AddModelError(
                nameof(model.ProductType),
                _t["Jenis Produk wajib dipilih."]
            );
        }

        if (!model.UnitId.HasValue ||
            model.UnitId.Value <= 0)
        {
            ModelState.AddModelError(
                nameof(model.UnitId),
                _t["Unit wajib dipilih."]
            );
        }

        // Jika validasi gagal
        if (!ModelState.IsValid)
        {
            ViewBag.ParentProduct =
                parentProduct;

            await LoadProductDropdowns(model);

            return View(model);
        }

        try
        { 
            if (model.ImageFile != null &&
                model.ImageFile.Length > 0)
            {
                model.Image =
                    await SaveProductImageAsync(
                        model.ImageFile
                    );
            }
             
            model.ParentId = parentProduct.Id;
            model.ParentUid = parentProduct.Uid;
              
            var result =
                await _api.CreateProductAsync(model);

            if (!result.Success)
            {
                TempData["Error"] =
                    result.Error ??
                    "Gagal menambahkan varian.";

                ViewBag.ParentProduct =
                    parentProduct;

                await LoadProductDropdowns(model);

                return View(model);
            }
             
            parentProduct.HasVariants = true;

            var updateParentResult =
                await _api.UpdateProductAsync(
                    parentProduct
                );

            if (!updateParentResult.Success)
            {
                TempData["Error"] =
                    "Varian berhasil ditambahkan, " +
                    "tetapi status produk induk gagal diperbarui.";
            }
            else
            {
                TempData["Success"] =
                    "Varian berhasil ditambahkan.";
            }

            // Kembali ke Index variant
            return RedirectToAction(
                nameof(Index),
                new
                {
                    uid = parentProduct.Uid
                }
            );
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"ERROR CREATE VARIANT: {ex}"
            );

            TempData["Error"] =
                $"Terjadi kesalahan: {ex.Message}";

            ViewBag.ParentProduct =
                parentProduct;

            await LoadProductDropdowns(model);

            return View(model);
        }
    }

     
    private async Task<string?> SaveProductImageAsync(
        IFormFile? file)
    {
        if (file == null ||
            file.Length == 0)
        {
            return null;
        }

        var extension =
            Path.GetExtension(
                file.FileName
            ).ToLowerInvariant();

        var allowedExtensions =
            new[]
            {
                ".jpg",
                ".jpeg",
                ".png",
                ".webp"
            };

        if (!allowedExtensions.Contains(
            extension))
        {
            throw new InvalidOperationException(
                "Format gambar tidak valid."
            );
        }

        var folderPath =
            Path.Combine(
                _environment.WebRootPath,
                "uploads",
                "products"
            );

        Directory.CreateDirectory(
            folderPath
        );

        var fileName =
            $"{Guid.NewGuid()}{extension}";

        var filePath =
            Path.Combine(
                folderPath,
                fileName
            );

        await using (
            var stream =
                new FileStream(
                    filePath,
                    FileMode.Create
                ))
        {
            await file.CopyToAsync(stream);
        }

        return $"/uploads/products/{fileName}";
    }

    //edit
    [HttpGet("product-variants/edit/{uid:guid}")]
    public async Task<IActionResult> Edit(Guid uid)
    {
        if (uid == Guid.Empty)
        {
            TempData["Error"] = "Produk tidak ditemukan.";
            return RedirectToAction("Index", "Products");
        }

        // Ambil variant
        var product =
            await _api.GetProductAsync(uid);

        if (product == null)
        {
            TempData["Error"] = "Produk tidak ditemukan.";
            return RedirectToAction("Index", "Products");
        }

        // Ambil produk induk
        ProductViewModel? parentProduct = null;

        if (Guid.TryParse(product.ParentUid, out var parentUid) &&
            parentUid != Guid.Empty)
        {
            parentProduct =
                await _api.GetProductAsync(
                    parentUid
                );
        }

        ViewBag.ParentProduct =
            parentProduct;

        await LoadProductDropdowns(
            product
        );

        return View(
            "~/Views/ProductVariants/Edit.cshtml",
            product
        );
    }

     
    [HttpPost("product-variants/edit/{uid:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        Guid uid,
        ProductViewModel model)
    {
        // UID dari URL selalu menjadi UID variant
        model.Uid = uid.ToString();
        var existingProduct =
            await _api.GetProductAsync(uid);

        if (existingProduct == null)
        {
            TempData["Error"] = "Produk tidak ditemukan.";
            return RedirectToAction("Index", "Products");
        }
         
        if (!Guid.TryParse(existingProduct.ParentUid, out var parentUid) ||
            parentUid == Guid.Empty)
        {
            TempData["Error"] =
                "Produk induk tidak ditemukan.";

            return RedirectToAction(
                "Index",
                "Products"
            );
        } 

        model.ParentId = existingProduct.ParentId;
        model.ParentUid = existingProduct.ParentUid;
        model.HasVariants = existingProduct.HasVariants; 
        model.ProductNumber = existingProduct.ProductNumber;

        // Ambil produk induk
        var parentProduct =
            await _api.GetProductAsync(
                parentUid
            );

        if (parentProduct == null)
        {
            TempData["Error"] =
                "Produk induk tidak ditemukan.";

            return RedirectToAction(
                "Index",
                "Products"
            );
        }
         
        ModelState.Remove(
            nameof(model.Sku)
        );

        ModelState.Remove(
            nameof(model.ImageFile)
        );

        if (!ModelState.IsValid)
        {
            ViewBag.ParentProduct =
                parentProduct;

            await LoadProductDropdowns(
                model
            );

            return View(
                "~/Views/ProductVariants/Edit.cshtml",
                model
            );
        }

        try
        { 
            if (model.ImageFile != null &&
                model.ImageFile.Length > 0)
            {
                model.Image =
                    await SaveProductImageAsync(
                        model.ImageFile
                    );
            }
            else
            {
                model.Image =
                    existingProduct.Image;
            } 

            var result =
                await _api.UpdateProductAsync(
                    model
                );

            if (!result.Success)
            {
                TempData["Error"] =
                    result.Error ??
                    "Gagal mengubah varian.";

                ViewBag.ParentProduct =
                    parentProduct;

                await LoadProductDropdowns(
                    model
                );

                return View(
                    "~/Views/ProductVariants/Edit.cshtml",
                    model
                );
            }

            TempData["Success"] = "Varian berhasil diubah.";
             
            return RedirectToAction(
                nameof(Index),
                new
                {
                    uid = parentUid
                }
            );
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"ERROR EDIT VARIANT: {ex}"
            );

            TempData["Error"] =
                $"Terjadi kesalahan: {ex.Message}";

            ViewBag.ParentProduct =
                parentProduct;

            await LoadProductDropdowns(
                model
            );

            return View(
                "~/Views/ProductVariants/Edit.cshtml",
                model
            );
        }
    }

    //copy
    [HttpGet("product-variants/copy/{uid:guid}")]
    public async Task<IActionResult> Copy(Guid uid)
    {
        if (uid == Guid.Empty)
        {
            TempData["Error"] = "Produk tidak ditemukan.";
            return RedirectToAction("Index", "Products");
        }

        // Ambil variant
        var product =
            await _api.GetProductAsync(uid);

        if (product == null)
        {
            TempData["Error"] = "Produk tidak ditemukan.";
            return RedirectToAction("Index", "Products");
        }

        // Ambil produk induk
        ProductViewModel? parentProduct = null;

        if (Guid.TryParse(product.ParentUid, out var parentUid) &&
            parentUid != Guid.Empty)
        {
            parentProduct =
                await _api.GetProductAsync(
                    parentUid
                );
        }

        ViewBag.ParentProduct =
            parentProduct;

        await LoadProductDropdowns(
            product
        );

        return View(
            "~/Views/ProductVariants/Copy.cshtml",
            product
        );
    }


    [HttpPost("product-variants/copy/{uid:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Copy(
        Guid uid,
        ProductViewModel model)
    {
        // UID dari URL selalu menjadi UID variant
        model.Uid = uid.ToString();
        var existingProduct = await _api.GetProductAsync(uid);

        if (existingProduct == null)
        {
            TempData["Error"] = "Produk tidak ditemukan.";
            return RedirectToAction("Index", "Products");
        }

        if (!Guid.TryParse(existingProduct.ParentUid, out var parentUid) ||
            parentUid == Guid.Empty)
        {
            TempData["Error"] =
                "Produk induk tidak ditemukan.";

            return RedirectToAction(
                "Index",
                "Products"
            );
        }

        model.ParentId = existingProduct.ParentId;
        model.ParentUid = existingProduct.ParentUid;
        model.HasVariants = existingProduct.HasVariants;
        model.ProductNumber = existingProduct.ProductNumber;

        // Ambil produk induk
        var parentProduct =
            await _api.GetProductAsync(
                parentUid
            );

        if (parentProduct == null)
        {
            TempData["Error"] =
                "Produk induk tidak ditemukan.";

            return RedirectToAction(
                "Index",
                "Products"
            );
        }

        ModelState.Remove(
            nameof(model.Sku)
        );

        ModelState.Remove(
            nameof(model.ImageFile)
        );

        if (!ModelState.IsValid)
        {
            ViewBag.ParentProduct =
                parentProduct;

            await LoadProductDropdowns(
                model
            );

            return View(
                "~/Views/ProductVariants/Copy.cshtml",
                model
            );
        }

        try
        {
            if (model.ImageFile != null &&
                model.ImageFile.Length > 0)
            {
                model.Image =
                    await SaveProductImageAsync(
                        model.ImageFile
                    );
            }
            else
            {
                model.Image =
                    existingProduct.Image;
            }

            var result =
                await _api.CreateProductAsync(
                    model
                );

            if (!result.Success)
            {
                TempData["Error"] =
                    result.Error ??
                    "Gagal mengubah varian.";

                ViewBag.ParentProduct =
                    parentProduct;

                await LoadProductDropdowns(
                    model
                );

                return View(
                    "~/Views/ProductVariants/Copy.cshtml",
                    model
                );
            }

            TempData["Success"] = "Varian berhasil diubah.";

            return RedirectToAction(
                nameof(Index),
                new
                {
                    uid = parentUid
                }
            );
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"ERROR COPY VARIANT: {ex}"
            );

            TempData["Error"] =
                $"Terjadi kesalahan: {ex.Message}";

            ViewBag.ParentProduct =
                parentProduct;

            await LoadProductDropdowns(
                model
            );

            return View(
                "~/Views/ProductVariants/Copy.cshtml",
                model
            );
        }
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(
        Guid uid)
    {
        if (uid == Guid.Empty)
        {
            TempData["Error"] =
                "Produk tidak ditemukan.";

            return RedirectToAction(
                "Index",
                "Products"
            );
        }
         
        var product =
            await _api.GetProductAsync(uid);

        if (product == null)
        {
            TempData["Error"] =
                "Produk tidak ditemukan.";

            return RedirectToAction(
                "Index",
                "Products"
            );
        }
         
        var parentUidText = product.ParentUid;

        var result = await _api.DeleteProductAsync(uid);

        TempData[
            result.Success
                ? "Success"
                : "Error"
        ] =
            result.Success
                ? "Produk berhasil dihapus."
                : result.Error ??
                  "Gagal menghapus produk.";

        if (Guid.TryParse(parentUidText, out var parentUid) &&
            parentUid != Guid.Empty)
        {
            return RedirectToAction(
                nameof(Index),
                new
                {
                    uid = parentUid
                }
            );
        }

        return RedirectToAction(
            "Index",
            "Products"
        );
    }
     
    
    [HttpGet]
    public async Task<IActionResult> ExportExcel()
    {
        var products =
            await _api.GetProductsForExportAsync();

        using var workbook =
            new ClosedXML.Excel.XLWorkbook();

        var worksheet =
            workbook.Worksheets.Add("Product");

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

        var header =
            worksheet.Range("A1:L1");

        header.Style.Font.Bold = true;

        header.Style.Alignment.Horizontal =
            ClosedXML.Excel
                .XLAlignmentHorizontalValues.Center;

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
            worksheet.Cell(row, 10).Value =
                item.TrackStock
                    ? _t["Ya"]
                    : _t["Tidak"];

            worksheet.Cell(row, 11).Value =
                item.IsActive
                    ? _t["Aktif"]
                    : _t["Nonaktif"];

            worksheet.Cell(row, 12).Value =
                item.Image;

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
            $"Product_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
        );
    }

     
    [HttpGet]
    public async Task<IActionResult> ExportPdf()
    {
        var products =
            await _api.GetProductsForExportAsync();

        var listProducts =
            products.ToList();

        var document =
            QuestPDF.Fluent.Document.Create(
                container =>
                {
                    container.Page(page =>
                    {
                        page.Size(
                            QuestPDF.Helpers
                                .PageSizes
                                .A4
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
                                        text.Span("Product")
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
                                            _t["Total: {0} data", listProducts.Count]
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
                                        columns.RelativeColumn(4);
                                        columns.RelativeColumn(15);
                                        columns.RelativeColumn(20);
                                        columns.RelativeColumn(20);
                                        columns.RelativeColumn(8);
                                        columns.RelativeColumn(5);
                                        columns.RelativeColumn(5);
                                        columns.RelativeColumn(5);
                                        columns.RelativeColumn(5);
                                        columns.RelativeColumn(5);
                                        columns.RelativeColumn(5);
                                        columns.RelativeColumn(20);
                                    }
                                );

                                table.Header(header =>
                                {
                                    header.Cell()
                                        .Element(HeaderStyle)
                                        .Text("No");

                                    header.Cell()
                                        .Element(HeaderStyle)
                                        .Text("Sku");

                                    header.Cell()
                                        .Element(HeaderStyle)
                                        .Text("Barcode");

                                    header.Cell()
                                        .Element(HeaderStyle)
                                        .Text("Name");

                                    header.Cell()
                                        .Element(HeaderStyle)
                                        .Text("Category");

                                    header.Cell()
                                        .Element(HeaderStyle)
                                        .Text("Unit");

                                    header.Cell()
                                        .Element(HeaderStyle)
                                        .Text("Cost Price");

                                    header.Cell()
                                        .Element(HeaderStyle)
                                        .Text("Sell Price");

                                    header.Cell()
                                        .Element(HeaderStyle)
                                        .Text("Min Stock");

                                    header.Cell()
                                        .Element(HeaderStyle)
                                        .Text("Track Stock");

                                    header.Cell()
                                        .Element(HeaderStyle)
                                        .Text("Is Active");

                                    header.Cell()
                                        .Element(HeaderStyle)
                                        .Text("Image");

                                    static QuestPDF
                                        .Infrastructure
                                        .IContainer HeaderStyle(
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
                                                    .FontColor("#ffffff")
                                            );
                                    }
                                });

                                int no = 1;

                                foreach (var item in listProducts)
                                {
                                    string bgColor =
                                        no % 2 == 0
                                            ? "#f8f9fa"
                                            : "#ffffff";

                                    table.Cell()
                                        .Element(c =>
                                            Cell(c, bgColor))
                                        .Text(no.ToString());

                                    table.Cell()
                                        .Element(c =>
                                            Cell(c, bgColor))
                                        .Text(item.Sku ?? "");

                                    table.Cell()
                                        .Element(c =>
                                            Cell(c, bgColor))
                                        .Text(item.Barcode ?? "");

                                    table.Cell()
                                        .Element(c =>
                                            Cell(c, bgColor))
                                        .Text(item.Name ?? "");

                                    table.Cell()
                                        .Element(c =>
                                            Cell(c, bgColor))
                                        .Text(
                                            item.CategoryId
                                                .ToString()
                                        );

                                    table.Cell()
                                        .Element(c =>
                                            Cell(c, bgColor))
                                        .Text(
                                            item.UnitId
                                                .ToString()
                                        );

                                    table.Cell()
                                        .Element(c =>
                                            Cell(c, bgColor))
                                        .Text(
                                            item.CostPrice?
                                                .ToString("C") ??
                                            ""
                                        );

                                    table.Cell()
                                        .Element(c =>
                                            Cell(c, bgColor))
                                        .Text(
                                            item.SellPrice?
                                                .ToString("C") ??
                                            ""
                                        );

                                    table.Cell()
                                        .Element(c =>
                                            Cell(c, bgColor))
                                        .Text(
                                            item.MinStock?
                                                .ToString() ??
                                            ""
                                        );

                                    table.Cell()
                                        .Element(c =>
                                            Cell(c, bgColor))
                                        .Text(
                                            item.TrackStock
                                                ? _t["Ya"]
                                                : _t["Tidak"]
                                        );

                                    table.Cell()
                                        .Element(c =>
                                            Cell(c, bgColor))
                                        .Text(
                                            item.IsActive
                                                ? _t["Aktif"]
                                                : _t["Nonaktif"]
                                        );

                                    table.Cell()
                                        .Element(c =>
                                            Cell(c, bgColor))
                                        .Text(
                                            item.Image?.ToString() ??
                                            ""
                                        );

                                    no++;
                                }

                                static QuestPDF
                                    .Infrastructure
                                    .IContainer Cell(
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
            $"Product_{DateTime.Now:yyyyMMdd_HHmmss}.pdf"
        );
    }

     
    [HttpGet]
    public async Task<IActionResult> UnitConversion(Guid uid)
    {
        if (uid == Guid.Empty)
        {
            TempData["Error"] = "Produk tidak ditemukan.";
            return RedirectToAction("Index", "Products");
        }

        var product = await _api.GetProductAsync(uid);

        if (product is null)
        {
            TempData["Error"] = "Produk tidak ditemukan.";
            return RedirectToAction("Index", "Products");
        }

        // Data dropdown satuan
        await LoadProductDropdowns(product);

        // Produk yang sedang dikelola
        ViewBag.Product = product;
        ViewBag.BaseUnitId = product.UnitId;
        ViewBag.BaseUnitName = product.UnitName;

        return View(product);
    }
}