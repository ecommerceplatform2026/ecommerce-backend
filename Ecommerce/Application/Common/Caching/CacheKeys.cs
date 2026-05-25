namespace Application.Common.Caching
{
    public static class CacheKeys
    {
        public const string CategoriesAll = "categories:all";
        public const string ProductsAll = "products:all";

        public static string GetProductDetailKey(Guid id) => $"products:detail:{id}";

        // Prefixes for wildcard invalidation
        public const string ProductsPrefix = "products:";
    }
}
