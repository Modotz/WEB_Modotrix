namespace ERP.Web.Models;

public class GoodsRfqItemViewModel
{
    public long Id { get; set; }

    public long RfqId { get; set; }

    public long ProductId { get; set; }

    public long? UnitId { get; set; }

    public decimal UnitQty { get; set; } = 1;

    public decimal? DimLength { get; set; }

    public decimal? DimWidth { get; set; }

    public decimal? DimHeight { get; set; }

    public decimal? DimCount { get; set; }

    public decimal Quantity { get; set; } = 1;

    public decimal EstPrice { get; set; }

    public decimal QuotedPrice { get; set; }

    public decimal Subtotal { get; set; }

    public string? Note { get; set; }

    // DATA PRODUK
    public string? ProductName { get; set; }

    public string? Sku { get; set; }

    public string? UnitSymbol { get; set; }

    public decimal? CostPrice { get; set; }
}

public class GoodsRfqItemProduct
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Sku { get; set; }
    public long? UnitId { get; set; }
    public string? UnitSymbol { get; set; }
    public decimal? CostPrice { get; set; }
}

public class GoodsRfqQuoteItem
{
    public long Id { get; set; }
    public decimal QuotedPrice { get; set; }
}