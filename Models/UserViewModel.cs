using System.ComponentModel.DataAnnotations;

namespace ERP.Web.Models;

public class UserViewModel
{
    public long? Id { get; set; }
    public string? Uid { get; set; }

    [Required]
    [Display(Name = "Role")]
    public long RoleId { get; set; }
    public string? RoleName { get; set; }

    [Required, MaxLength(100)]
    [Display(Name = "Nama Lengkap")]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(50)]
    public string Username { get; set; } = string.Empty;

    [Required, EmailAddress, MaxLength(191)]
    public string Email { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    public string? Password { get; set; }

    public string? Photo { get; set; }

    public string? Signature { get; set; }

    [Display(Name = "Akun Aktif")]
    public bool IsActive { get; set; } = true;

    public DateTime? LastLogin { get; set; }
}
