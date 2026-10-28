using System.ComponentModel.DataAnnotations;

namespace ERP.Web.Models;

public class MenuViewModel
{
    public long? Id { get; set; }
    public string? Uid { get; set; }

    [Display(Name = "Menu Induk")]
    public long? ParentId { get; set; }

    [Required, MaxLength(100)]
    [Display(Name = "Judul Menu")]
    public string Title { get; set; } = string.Empty;

    [MaxLength(100)]
    [Display(Name = "Judul (English)")]
    public string? TitleEn { get; set; }

    [Display(Name = "URL / Slug")]
    public string UrlSlug { get; set; } = string.Empty;

    [Display(Name = "Ikon (Bootstrap Icons")]
    public string Icon { get; set; } = string.Empty;

    [Display(Name = "Urutan")]
    public int SortOrder { get; set; }

    [Display(Name = "Akun Aktif")]
    public bool IsActive { get; set; } = true;

}