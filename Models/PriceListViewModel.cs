namespace ERP.Web.Models;

public class PriceListViewModel
{
    public long? Id { get; set; }

    public string? Uid { get; set; }

    public string? Code { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Note { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public int ItemCount { get; set; }
}