using System.ComponentModel.DataAnnotations;

namespace ERP.Web.Models;

public class ProductCategoryViewModel
{
    public long? Id { get; set; }

    public long? ParentId { get; set; }

    public string? Uid { get; set; }

    [StringLength(30)]
    public string? Code { get; set; }

    [Required(ErrorMessage = "Nama kategori wajib diisi.")]
    [StringLength(100, ErrorMessage = "Nama kategori maksimal 100 karakter.")]
    public string Name { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public DateTime? DeletedAt { get; set; }
}