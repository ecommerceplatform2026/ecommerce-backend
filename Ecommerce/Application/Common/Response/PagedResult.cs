namespace Application.Common.Response;

public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();

    private int _page = 1;
    public int Page
    {
        get => _page;
        set => _page = Math.Max(1, value);
    }

    private int _pageSize = 10;
    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = Math.Max(1, value);
    }

    public int TotalCount { get; set; }

    public int TotalPages =>
        TotalCount <= 0
            ? 0
            : (int)Math.Ceiling((double)TotalCount / PageSize);
}