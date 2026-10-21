using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ERP.Web.Models;

public class CustomerViewModel
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("uid")]
    public string? Uid { get; set; }

    // CUSTOMER DATA

    [Required(ErrorMessage = "Nama lengkap wajib diisi.")]
    [MaxLength(100, ErrorMessage = "Nama lengkap maksimal 100 karakter.")]
    [JsonPropertyName("fullName")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email wajib diisi.")]
    [EmailAddress(ErrorMessage = "Format email tidak valid.")]
    [MaxLength(100, ErrorMessage = "Email maksimal 100 karakter.")]
    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Nomor telepon wajib diisi.")]
    [MaxLength(13, ErrorMessage = "Nomor telepon maksimal 13 karakter.")]
    [JsonPropertyName("phone")]
    public string Phone { get; set; } = string.Empty;

    [MaxLength(16, ErrorMessage = "NPWP maksimal 16 karakter.")]
    [JsonPropertyName("npwp")]
    public string? Npwp { get; set; }

    [MaxLength(16, ErrorMessage = "NIK maksimal 16 karakter.")]
    [JsonPropertyName("nik")]
    public string? Nik { get; set; }

    [Required(ErrorMessage = "Alamat wajib diisi.")]
    [JsonPropertyName("address")]
    public string Address { get; set; } = string.Empty;

    [JsonPropertyName("image")]
    public string? Image { get; set; }

    // CUSTOMER GROUP

    [JsonPropertyName("customerGroupId")]
    public long? CustomerGroupId { get; set; }

    [JsonPropertyName("customerGroupName")]
    public string? CustomerGroupName { get; set; }

    // SALES FORCE

    [JsonPropertyName("salesForceId")]
    public long? SalesForceId { get; set; }

    [JsonPropertyName("salesForceName")]
    public string? SalesForceName { get; set; }

    // PRICE LIST

    [JsonPropertyName("priceListId")]
    public long? PriceListId { get; set; }

    [JsonPropertyName("priceListName")]
    public string? PriceListName { get; set; }

    // SHIPPING AREA

    [JsonPropertyName("shippingAreaId")]
    public long? ShippingAreaId { get; set; }

    [JsonPropertyName("shippingAreaName")]
    public string? ShippingAreaName { get; set; }

    // OTHER DATA

    [JsonPropertyName("registeredOnline")]
    public bool? RegisteredOnline { get; set; }

    [JsonPropertyName("shopStatus")]
    public string? ShopStatus { get; set; }

    [JsonPropertyName("userId")]
    public long? UserId { get; set; }

    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; } = true;

    // PASSWORD

    [DataType(DataType.Password)]
    [JsonPropertyName("password")]
    public string? Password { get; set; }

    // TIMESTAMPS

    [JsonPropertyName("createdAt")]
    public DateTime? CreatedAt { get; set; }

    [JsonPropertyName("updatedAt")]
    public DateTime? UpdatedAt { get; set; }

    [JsonPropertyName("deletedAt")]
    public DateTime? DeletedAt { get; set; }
}