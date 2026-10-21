namespace ERP.Web.Models;

public class PagedResult<T>
{
    public List<T> Data { get; set; } = new();

    public int Page { get; set; }

    public int PageSize { get; set; }

    public int TotalData{ get; set; }

    public int TotalCount { get; set; }

    public int TotalPage =>
        PageSize > 0
            ? (int)Math.Ceiling((double)TotalCount / PageSize)
            : 0;
}