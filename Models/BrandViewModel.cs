using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ERP.Web.Models;

public class BrandViewModel
{
    public long? Id { get; set; }

    public string? Uid { get; set; }

    [Required]
    [MaxLength(50)]
    [Display(Name = "Nama Brand")]
    public string Name { get; set; } = string.Empty;

    public string? Image { get; set; }

    [JsonPropertyName("is_active")]
    [Display(Name = "Brand Aktif")]
    public bool IsActive { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime? CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime? UpdatedAt { get; set; }
}