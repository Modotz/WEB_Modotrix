
namespace ERP.Web.Models;

public class ProductUnitViewModel
{
    public long Id { get; set; }
    public string? Uid { get; set; }
    public long ProductId { get; set; }
    public long UnitId { get; set; }
    public string UnitCode { get; set; } = "";
    public string UnitName { get; set; } = "";
    public string? UnitSymbol { get; set; }
    public decimal Conversion { get; set; }
}

public class ProductUnitPageViewModel
{
    public string ProductSku { get; set; } = string.Empty; 
    public long ProductId { get; set; }
    /// <summary>Uid produk (identitas di URL/form).</summary>
    public string? ProductUid { get; set; }
    /// <summary>Uid produk induk bila produk ini varian.</summary>
    public string? ParentUid { get; set; }
    public string ProductName { get; set; } = "";
    public long BaseUnitId { get; set; }
    public string BaseUnitName { get; set; } = "";

    public List<ProductUnitViewModel> Units { get; set; } = new();

    public List<DropdownItemViewModel> AvailableUnits { get; set; } = new();
}

public class CreateProductUnitViewModel
{
    /// <summary>Identitas produk (uid, bukan id angka).</summary>
    public Guid ProductUid { get; set; }
    public long UnitId { get; set; }
    public decimal Conversion { get; set; }
}