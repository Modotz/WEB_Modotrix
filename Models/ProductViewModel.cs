using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
namespace ERP.Web.Models;

public class ProductViewModel  
{
    public long Id { get; set; }
    public string? Uid { get; set; }

    [Display(Name = "Nomor Produk")]
    public string? ProductNumber { get; set; }

    [Required, MaxLength(100)]
    [Display(Name = "SKU")]
    public string Sku { get; set; } = string.Empty;

    [Display(Name = "Barcode")]
    public string? Barcode { get; set; }

    [Display(Name = "Kode Internal")]
    public string? InternalCode { get; set; }

    [Display(Name = "Kode Eksternal 1")]
    public string? ExternalCode1 { get; set; }

    [Display(Name = "Kode Eksternal 2")]
    public string? ExternalCode2 { get; set; }

    [Display(Name = "Kode Eksternal 3")]
    public string? ExternalCode3 { get; set; }

    [Required, MaxLength(191)]
    [Display(Name = "Nama Produk")]
    public string Name { get; set; } = string.Empty;

    //relasi
    public long? BrandId { get; set; }
    public string? BrandName { get; set; }

    public long? CategoryId { get; set; }
    public string? CategoryName { get; set; }

    public long? SubCategoryId { get; set; }
    public string? SubCategoryName { get; set; }
    public long? ParentId { get; set; }
    /// <summary>Uid produk induk (navigasi varian → induk).</summary>
    public string? ParentUid { get; set; }
    public long? UnitId { get; set; }
    public string? UnitName { get; set; } 

    [Display(Name = "Tipe Produk")]
    public string ProductType { get; set; } = "RAW_MATERIAL";

    [Display(Name = "Memiliki Varian")]
    public bool HasVariants { get; set; } = false; 
    public int VariantCount { get; set; }

    [Display(Name = "Mode Dimensi")]
    public string? DimMode { get; set; } = "none";

    [Display(Name = "Panjang")]
    public decimal? DefLength { get; set; }

    [Display(Name = "Lebar")]
    public decimal? DefWidth { get; set; }

    [Display(Name = "Tinggi")]
    public decimal? DefHeight { get; set; }

    [Display(Name = "Mode Harga")]
    public string? PriceMode { get; set; } = "fixed";

    [Display(Name = "Harga Modal per M³")]
    public decimal? CostPerM3 { get; set; }

    [Display(Name = "Harga Jual per M³")]
    public decimal? SellPerM3 { get; set; }

    [Display(Name = "Harga Modal")]
    public decimal? CostPrice { get; set; }

    [Display(Name = "Harga Jual")]
    public decimal? SellPrice { get; set; }

    [Display(Name = "Minimum Stok")]
    public decimal? MinStock { get; set; }

    [Display(Name = "Tracking Stok")]
    public bool TrackStock { get; set; } = true;

    [Display(Name = "Tracking Serial")]
    public bool TrackSerial { get; set; } = false;

    [Display(Name = "Masa Garansi")]
    public int? WarrantyMonths { get; set; } = 0;

    [Display(Name = "Deskripsi")]
    public string? Description { get; set; }

    [Display(Name = "Gambar")]
    public string? Image { get; set; }

    [JsonIgnore]
    public IFormFile? ImageFile { get; set; }

    [Display(Name = "Status")]
    public bool IsActive { get; set; } = true;

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public DateTime? DeletedAt { get; set; }

}

