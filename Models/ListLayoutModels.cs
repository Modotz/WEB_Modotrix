using Microsoft.AspNetCore.Html;

namespace ERP.Web.Models;

/// <summary>
/// Header kartu tabel standar (seperti menu Gudang): judul + badge jumlah + deskripsi, toolbar PDF / Excel / Cetak /
/// Refresh / Lipat dan tombol tambah (teal). Teks sudah diterjemahkan oleh pemanggil. URL null = tombol disembunyikan.
/// </summary>
public class ListHeaderModel
{
    public string Title { get; set; } = "";

    public int? Count { get; set; }

    public string? Description { get; set; }

    public string? PdfUrl { get; set; }

    public string? ExcelUrl { get; set; }

    public string? PrintUrl { get; set; }

    /// <summary>Tombol tambah: link (AddUrl) atau JS (AddOnClick).</summary>
    public string? AddUrl { get; set; }

    public string? AddOnClick { get; set; }

    public string? AddText { get; set; }

    public string AddIcon { get; set; } = "bi-plus-circle";

    /// <summary>Izin menu untuk tombol tambah (data-perm), default create.</summary>
    public string AddPerm { get; set; } = "create";

    /// <summary>Tombol tambahan (HTML) di sebelah kiri tombol tambah, mis. aksi khusus modul.</summary>
    public IHtmlContent? Extra { get; set; }

    /// <summary>Id elemen yang dilipat tombol toggle (bungkus tabel + pagination).</summary>
    public string CollapseId { get; set; } = "listBody";

    public bool ShowRefresh { get; set; } = true;

    public bool ShowToggle { get; set; } = true;
}

/// <summary>Footer pagination standar: "Menampilkan a – b dari n data" + Sebelumnya / nomor / Berikutnya.</summary>
public class ListPaginationModel
{
    public int Page { get; set; }

    public int PageSize { get; set; }

    public int TotalCount { get; set; }

    public int TotalPage => PageSize <= 0 ? 1 : Math.Max(1, (int)Math.Ceiling(TotalCount / (double)PageSize));

    /// <summary>URL untuk nomor halaman tertentu (sudah membawa filter aktif).</summary>
    public Func<int, string> PageUrl { get; set; } = p => $"?page={p}";
}
