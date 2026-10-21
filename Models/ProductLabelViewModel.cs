using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
namespace ERP.Web.Models;

public class ProductLabelViewModel  
{
    public string Uid { get; set; } = string.Empty;

    public string ProductName { get; set; } = string.Empty;

    public string Sku { get; set; } = string.Empty;

    public int PerLabel { get; set; }

    public int LabelCount { get; set; }

    public int TotalPcs { get; set; }

    public string QrCodeBase64 { get; set; } = string.Empty;

    public string? ProductNumber { get; set; }

    public string? OperatorName { get; set; }

    public string? BarcodeValue { get; set; }

    public decimal? DefLength { get; set; }

    public decimal? DefWidth { get; set; }

    public decimal? DefHeight { get; set; }

    public string? Dimensions { get; set; }

    public DateTime? PrintDate { get; set; }
}

