using System.ComponentModel.DataAnnotations;
namespace ERP.Web.Models;

public class ShippingAreaViewModel  
{
    public long? Id { get; set; }
    public string? Uid { get; set; }

    [Required]
    [Display(Name = "Kode")]
    public string Code { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    [Display(Name = "Nama Area")]
    public string Name { get; set; } = string.Empty;

    public decimal Price { get; set; } = 0;

    [MaxLength(250)]
    [Display(Name = "Deskripsi")]
    public string? Description { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

}
