using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ERP.Web.Models;

public class WarehouseViewModel
{
    [JsonPropertyName("id")]
    public long? Id { get; set; }

    [JsonPropertyName("uid")]
    public string? Uid { get; set; }

    [Required(ErrorMessage = "Kode gudang wajib diisi.")]
    [MaxLength(20, ErrorMessage = "Kode gudang maksimal 20 karakter.")]
    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "Nama gudang wajib diisi.")]
    [MaxLength(100, ErrorMessage = "Nama gudang maksimal 100 karakter.")]
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("address")]
    public string? Address { get; set; }

    [MaxLength(30, ErrorMessage = "Nomor telepon maksimal 30 karakter.")]
    [JsonPropertyName("phone")]
    public string? Phone { get; set; }

    [MaxLength(100, ErrorMessage = "Nama PIC maksimal 100 karakter.")]
    [JsonPropertyName("picName")]
    public string? PicName { get; set; }

    [JsonPropertyName("isDefault")]
    public bool IsDefault { get; set; } = false;

    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; } = true;

    [JsonPropertyName("createdAt")]
    public DateTime? CreatedAt { get; set; }

    [JsonPropertyName("updatedAt")]
    public DateTime? UpdatedAt { get; set; }

    [JsonPropertyName("deletedAt")]
    public DateTime? DeletedAt { get; set; }
}