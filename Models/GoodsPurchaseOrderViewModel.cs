using System.ComponentModel.DataAnnotations;

namespace ERP.Web.Models;

public class GoodsPurchaseOrderViewModel
{
    public long Id { get; set; }

    public string? Uid { get; set; }

    [Required(ErrorMessage = "Nomor PO wajib diisi.")]
    [StringLength(50)]
    public string PONo { get; set; } = string.Empty;

    [Required(ErrorMessage = "Tanggal PO wajib diisi.")]
    public DateOnly PODate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [Range(1, long.MaxValue, ErrorMessage = "Supplier wajib dipilih.")]
    public long SupplierId { get; set; }
    public string? SupplierName { get; set; }

    public long? PRId { get; set; }
    public long? RFQId { get; set; }

    [Range(1, long.MaxValue, ErrorMessage = "Gudang wajib dipilih.")]
    public long WarehouseId { get; set; }
    public string? WarehouseName { get; set; }

    public DateOnly? ExpectedDate { get; set; }
    public string? Note { get; set; }

    public decimal SubtotalAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxPercent { get; set; }
    public decimal TaxAmount { get; set; }

    public string Status { get; set; } = "draft";
    public decimal TotalAmount { get; set; }

    public uint? CreatedBy { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public List<GoodsPurchaseOrderItemViewModel> Details { get; set; } = new();
}