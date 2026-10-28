using System.ComponentModel.DataAnnotations;

namespace ERP.Web.Models;

public class GoodsReceiptViewModel
{
    public long Id { get; set; }

    public string? Uid { get; set; }

    [Required(ErrorMessage = "Nomor penerimaan wajib diisi.")]
    [StringLength(50)]
    public string ReceiptNo { get; set; } = string.Empty;

    [Required(ErrorMessage = "Tanggal penerimaan wajib diisi.")]
    public DateOnly ReceiptDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [Range(1, long.MaxValue, ErrorMessage = "Gudang wajib dipilih.")]
    public long WarehouseId { get; set; }
    public string? WarehouseName { get; set; }

    /// <summary>Opsional (penerimaan tanpa supplier diperbolehkan).</summary>
    public long? SupplierId { get; set; }
    public string? SupplierName { get; set; }

    public long? POId { get; set; }

    /// <summary>Nomor &amp; uid PO asal (untuk tampilan dan link ke detail PO).</summary>
    public string? PONo { get; set; }

    public string? POUid { get; set; }
    public string? RefNo { get; set; }
    public string? Note { get; set; }
    public string Status { get; set; } = "draft"; 
    public uint? CreatedBy { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public List<GoodsReceiptItemViewModel> Details { get; set; } = new();
}