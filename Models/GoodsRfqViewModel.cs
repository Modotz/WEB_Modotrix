
using ERP.Web.Models;

namespace ERP.Web.ViewModels;

public class GoodsRfqViewModel
{
    public long Id { get; set; }
    public string? Uid { get; set; }
    public string RfqNo { get; set; } = string.Empty;
    public DateTime RfqDate { get; set; }
    public DateTime? ValidUntil { get; set; }
    public long SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public long? PrId { get; set; }
    public string? Note { get; set; }
    public string Status { get; set; } = "draft";
    public decimal SubtotalAmount { get; set; }
    public DateTime? CreatedAt { get; set; }

    public List<GoodsRfqItemViewModel> Items { get; set; } = new();
}