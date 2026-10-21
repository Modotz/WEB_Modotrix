using System.ComponentModel.DataAnnotations;
namespace ERP.Web.Models;

public class SupplierViewModel  
{
    public long? Id { get; set; }
    public string? Uid { get; set; }

    [Required]
    [Display(Name = "Code")]
    public string Code { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    [Display(Name = "Name")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Contact Person")]
    public string? ContactPerson { get; set; } = string.Empty;

    [Display(Name = "Phone")]
    public string? Phone { get; set; } = string.Empty;

    [EmailAddress, MaxLength(191)]
    public string Email { get; set; } = string.Empty;

    [Display(Name = "NPWP")]
    public string Npwp { get; set; } = string.Empty;

    [MaxLength(250)]
    [Display(Name = "Address")]
    public string? Address { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

}
