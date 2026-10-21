namespace ERP.Web.Models;

public class CustomerGroupsViewModel
{
    public long Id { get; set; }

    public string? Uid { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public decimal DiscountPercent { get; set; }

    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    public long? PriceListId { get; set; }

    public PriceListViewModel? PriceList { get; set; }

    // Nama price list untuk Export Excel, Export PDF, dan Print
    public string? PriceListName { get; set; }
}