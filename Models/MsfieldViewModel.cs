using System.ComponentModel.DataAnnotations;

namespace ERP.Web.Models;

public class MsfieldViewModel
{
    public long? Id { get; set; }

    public string? Uid { get; set; }

    [Required, MaxLength(50)]
    [Display(Name = "Field Key")]
    public string FieldKey { get; set; } = string.Empty;

    [Required, MaxLength(255)]
    [Display(Name = "Field Text")]
    public string FieldText { get; set; } = string.Empty;

    [Required, MaxLength(255)]
    [Display(Name = "Filed Value")]
    public string FiledValue { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Description")]
    public string? Description { get; set; }

    [Display(Name = "Akun Aktif")]
    public bool IsActive { get; set; } 

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    [Required]
    [Display(Name = "User")]
    public int UserId { get; set; }
}