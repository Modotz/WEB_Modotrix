using System.ComponentModel.DataAnnotations;

namespace ERP.Web.Models;

public class RoleViewModel
{
    public long? Id { get; set; }

    public string? Uid { get; set; }

    [Required(ErrorMessage = "Nama role wajib diisi.")]
    [StringLength(100, ErrorMessage = "Nama role maksimal 100 karakter.")]
    public string Name { get; set; } = string.Empty;

    [StringLength(255, ErrorMessage = "Deskripsi maksimal 255 karakter.")]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;
}