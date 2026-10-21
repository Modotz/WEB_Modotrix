using System.ComponentModel.DataAnnotations;

namespace ERP.Web.Models;

public class GoodsReceiptItemViewModel
{
    public long Id { get; set; }

    public long ReceiptId { get; set; }
    public long ProductId { get; set; }
    public string? ProductName { get; set; }

    public decimal? DimLength { get; set; }
    public decimal? DimWidth { get; set; }
    public decimal? DimHeight { get; set; }
    public decimal? DimCount { get; set; }
    public long? UnitId { get; set; }
    public string? UnitCode { get; set; }
    public decimal? UnitQty { get; set; }
    public decimal Qty { get; set; }
    public decimal UnitCost { get; set; }
    public decimal Subtotal { get; set; }
}