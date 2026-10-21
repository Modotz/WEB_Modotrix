using System.ComponentModel.DataAnnotations;

namespace ERP.Web.Models;

public class GoodsReceiptViewModel
{
    public long Id { get; set; }

    public string? Uid { get; set; }

    [Required(ErrorMessage = "Nomor PO wajib diisi.")]
    [StringLength(50)]
    public string ReceiptNo { get; set; } = string.Empty;

    [Required(ErrorMessage = "Tanggal PO wajib diisi.")]
    public DateOnly ReceiptDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [Range(1, long.MaxValue, ErrorMessage = "Gudang wajib dipilih.")]
    public long WarehouseId { get; set; }
    public string? WarehouseName { get; set; }

    [Range(1, long.MaxValue, ErrorMessage = "Supplier wajib dipilih.")]
    public long SupplierId { get; set; }
    public string? SupplierName { get; set; }

    public long? POId { get; set; }
    public long? RefNo { get; set; }
    public string? Note { get; set; }
    public string Status { get; set; } = "draft"; 
    public uint? CreatedBy { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public List<GoodsReceiptItemViewModel> Details { get; set; } = new();
}