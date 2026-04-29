namespace Application.Common.Response
{
    public class PagingRequest
    {
        private const int MaxPageSize = 100;

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
            set => _pageSize = Math.Clamp(value, 1, MaxPageSize);
        }
    }
}