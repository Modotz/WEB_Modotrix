using System.ComponentModel.DataAnnotations;
namespace ERP.Web.Models;

public class UnitViewModel  
{
    public long Id { get; set; }
    public string? Uid { get; set; }

    [Required]
    [Display(Name = "Kode")]
    public string Code { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    [Display(Name = "Nama Satuan")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(250)]
    [Display(Name = "Simbol")]
    public string? Symbol { get; set; } = string.Empty;

    [MaxLength(250)]
    [Display(Name = "Deskripsi")]
    public string? Description { get; set; } = string.Empty;

}
