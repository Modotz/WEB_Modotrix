namespace ERP.Web.Models;

public class PriceListItemViewModel
{
    public long Id { get; set; }

    public long PriceListId { get; set; }

    public long ProductId { get; set; }

    public long? UnitId { get; set; }

    // Harga khusus pada Price List
    public decimal Price { get; set; }

    // PPN termasuk harga atau tidak
    public bool? TaxInclusive { get; set; }


    // =========================
    // DATA PRODUK
    // =========================

    public string? ProductNumber { get; set; }

    public string? ProductName { get; set; }

    public string? Sku { get; set; }

    public string? CategoryName { get; set; }

    public string? BrandName { get; set; }

    public string? ProductType { get; set; }

    public string? UnitName { get; set; }

    public decimal? CostPrice { get; set; }

    public decimal? SellPrice { get; set; }
}
