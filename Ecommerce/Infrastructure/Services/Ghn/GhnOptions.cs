namespace Infrastructure.Services.Ghn
{
    public sealed class GhnOptions
    {
        public string Token { get; set; } = string.Empty;
        public int ShopId { get; set; }
        public string BaseUrl { get; set; } = "https://dev-online-gateway.ghn.vn/shiip/public-api/";
        public int DefaultWeight { get; set; } = 500;
        public int DefaultLength { get; set; } = 10;
        public int DefaultWidth { get; set; } = 10;
        public int DefaultHeight { get; set; } = 10;
        public int ServiceTypeId { get; set; } = 2;
        public int PaymentTypeId { get; set; } = 2;
        public string RequiredNote { get; set; } = "CHOTHUHANG";
    }
}
