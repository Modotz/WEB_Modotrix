namespace ERP.Web.Models;

public class ProductVariantViewModel
{
    public long? Id { get; set; }

    public long AttributeId { get; set; }

    public string? Uid { get; set; }

    public string? Code { get; set; }

    public string Name { get; set; } = string.Empty;

    public decimal? DefHeight { get; set; }

    public decimal? DefWidth { get; set; }

    public decimal? DefLength { get; set; }

    public string? Description { get; set; }

    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public DateTime? DeletedAt { get; set; }
}