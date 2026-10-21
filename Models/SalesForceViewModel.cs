using System.ComponentModel.DataAnnotations;

namespace ERP.Web.Models;

public class SalesForceViewModel
{
    public long? Id { get; set; }

    public string? Uid { get; set; }

    [Required(ErrorMessage = "Kode wajib diisi.")]
    [StringLength(20, ErrorMessage = "Kode maksimal 20 karakter.")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "Nama wajib diisi.")]
    [StringLength(100, ErrorMessage = "Nama maksimal 100 karakter.")]
    public string Name { get; set; } = string.Empty;

    [StringLength(30, ErrorMessage = "Nomor telepon maksimal 30 karakter.")]
    public string? Phone { get; set; }

    [EmailAddress(ErrorMessage = "Format email tidak valid.")]
    [StringLength(100, ErrorMessage = "Email maksimal 100 karakter.")]
    public string? Email { get; set; }

    [StringLength(100, ErrorMessage = "Area maksimal 100 karakter.")]
    public string? Area { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public DateTime? DeletedAt { get; set; }
}