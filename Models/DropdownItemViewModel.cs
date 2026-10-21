using System.ComponentModel.DataAnnotations;
namespace ERP.Web.Models;

public class DropdownItemViewModel
{
    public long Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Code { get; set; }

    public long? ParentId { get; set; }
}