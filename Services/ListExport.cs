using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ERP.Web.Services;

/// <summary>
/// Data tabel daftar/laporan yang diekspor seragam ke Excel, PDF, dan halaman cetak (Views/Shared/ListPrint).
/// Nilai sel: string, angka (decimal/int → rata kanan + format ribuan), DateOnly/DateTime (dd/MM/yyyy), null → kosong.
/// Judul & header sudah diterjemahkan oleh pemanggil.
/// </summary>
public sealed class ListExport
{
    public string Title { get; init; } = "";

    /// <summary>Keterangan di bawah judul (mis. periode / filter aktif).</summary>
    public string? Subtitle { get; init; }

    public string[] Headers { get; init; } = Array.Empty<string>();

    public List<object?[]> Rows { get; init; } = new();

    /// <summary>Baris total opsional (dicetak tebal di bawah tabel).</summary>
    public object?[]? Footer { get; init; }

    /// <summary>Nama file tanpa ekstensi & timestamp, mis. "jurnal".</summary>
    public string FileName { get; init; } = "export";

    public bool Landscape { get; init; } = true;

    public const string ExcelMime = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    public string Stamp => DateTime.Now.ToString("yyyyMMdd_HHmmss");

    public static bool IsNumber(object? v) => v is decimal or double or float or int or long or short;

    public static string Text(object? v) => v switch
    {
        null => "",
        decimal d => d.ToString("N2", ERP.Web.Models.Acc.Culture),
        double d => d.ToString("N2", ERP.Web.Models.Acc.Culture),
        float d => d.ToString("N2", ERP.Web.Models.Acc.Culture),
        int or long or short => Convert.ToInt64(v).ToString("N0", ERP.Web.Models.Acc.Culture),
        DateOnly d => d.ToString("dd/MM/yyyy"),
        DateTime d => d.TimeOfDay == TimeSpan.Zero ? d.ToString("dd/MM/yyyy") : d.ToString("dd/MM/yyyy HH:mm"),
        _ => v.ToString() ?? ""
    };

    /// <summary>Kolom yang berisi angka (dideteksi dari baris data) — dipakai untuk rata kanan.</summary>
    public bool[] NumericColumns()
    {
        var result = new bool[Headers.Length];
        for (var c = 0; c < Headers.Length; c++)
            result[c] = Rows.Any(r => c < r.Length && IsNumber(r[c])) && Rows.All(r => c >= r.Length || r[c] == null || IsNumber(r[c]));
        return result;
    }

    public byte[] ToExcel()
    {
        using var workbook = new XLWorkbook();
        var sheetName = new string(Title.Where(ch => !"[]:*?/\\".Contains(ch)).ToArray());
        var sheet = workbook.Worksheets.Add(string.IsNullOrWhiteSpace(sheetName) ? "Data" : sheetName[..Math.Min(31, sheetName.Length)]);

        for (var i = 0; i < Headers.Length; i++)
            sheet.Cell(1, i + 1).Value = Headers[i];

        var header = sheet.Range(1, 1, 1, Math.Max(Headers.Length, 1));
        header.Style.Font.Bold = true;
        header.Style.Font.FontColor = XLColor.White;
        header.Style.Fill.BackgroundColor = XLColor.FromHtml("#0D6EFD");

        var rowNo = 2;
        foreach (var row in Footer == null ? Rows : Rows.Append(Footer))
        {
            for (var c = 0; c < row.Length && c < Headers.Length; c++)
            {
                var cell = sheet.Cell(rowNo, c + 1);
                switch (row[c])
                {
                    case null: break;
                    case decimal d: cell.Value = d; cell.Style.NumberFormat.Format = "#,##0.00"; break;
                    case double d: cell.Value = d; cell.Style.NumberFormat.Format = "#,##0.00"; break;
                    case float d: cell.Value = d; cell.Style.NumberFormat.Format = "#,##0.00"; break;
                    case int or long or short: cell.Value = Convert.ToInt64(row[c]); cell.Style.NumberFormat.Format = "#,##0"; break;
                    case DateOnly d: cell.Value = d.ToDateTime(TimeOnly.MinValue); cell.Style.DateFormat.Format = "dd/MM/yyyy"; break;
                    case DateTime d: cell.Value = d; cell.Style.DateFormat.Format = d.TimeOfDay == TimeSpan.Zero ? "dd/MM/yyyy" : "dd/MM/yyyy HH:mm"; break;
                    default: cell.Value = row[c]!.ToString(); break;
                }
            }

            if (Footer != null && ReferenceEquals(row, Footer))
                sheet.Row(rowNo).Style.Font.Bold = true;

            rowNo++;
        }

        sheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public byte[] ToPdf(string appName, string printedLabel)
    {
        var numeric = NumericColumns();

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(Landscape ? PageSizes.A4.Landscape() : PageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(8.5f).FontColor("#333333"));

                page.Header().Column(col =>
                {
                    col.Item().Text(appName).Bold().FontSize(16).FontColor("#1a1a1a");
                    col.Item().Text(Title).SemiBold().FontSize(12);
                    if (!string.IsNullOrWhiteSpace(Subtitle))
                        col.Item().Text(Subtitle).FontSize(9).FontColor("#6c757d");
                    col.Item().Text($"{printedLabel}: {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(8).FontColor("#6c757d");
                    col.Item().PaddingBottom(6);
                });

                page.Content().Table(table =>
                {
                    table.ColumnsDefinition(cols =>
                    {
                        for (var i = 0; i < Headers.Length; i++)
                        {
                            if (numeric[i]) cols.ConstantColumn(85);
                            else cols.RelativeColumn();
                        }
                    });

                    table.Header(h =>
                    {
                        for (var i = 0; i < Headers.Length; i++)
                        {
                            var cell = h.Cell().Background("#0D6EFD").Padding(4);
                            var text = numeric[i] ? cell.AlignRight().Text(Headers[i]) : cell.Text(Headers[i]);
                            text.FontColor("#FFFFFF").SemiBold();
                        }
                    });

                    var index = 0;
                    foreach (var row in Rows)
                    {
                        var bg = index++ % 2 == 0 ? "#FFFFFF" : "#F5F7FA";
                        for (var i = 0; i < Headers.Length; i++)
                        {
                            var cell = table.Cell().Background(bg).BorderBottom(0.5f).BorderColor("#E2E8F0").Padding(3);
                            var value = i < row.Length ? Text(row[i]) : "";
                            if (numeric[i]) cell.AlignRight().Text(value);
                            else cell.Text(value);
                        }
                    }

                    if (Footer != null)
                    {
                        for (var i = 0; i < Headers.Length; i++)
                        {
                            var cell = table.Cell().BorderTop(1).BorderColor("#333333").Padding(3);
                            var value = i < Footer.Length ? Text(Footer[i]) : "";
                            (numeric[i] ? cell.AlignRight().Text(value) : cell.Text(value)).Bold();
                        }
                    }
                });

                page.Footer().AlignRight().Text(t =>
                {
                    t.CurrentPageNumber();
                    t.Span(" / ");
                    t.TotalPages();
                });
            });
        }).GeneratePdf();
    }
}
