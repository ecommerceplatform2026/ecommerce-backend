namespace Application.Configurations
{
    public sealed class ShippingSettings
    {
        public string DefaultCarrier { get; set; } = "GHN";
        public int DefaultWeight { get; set; } = 500;
        public int DefaultLength { get; set; } = 10;
        public int DefaultWidth { get; set; } = 10;
        public int DefaultHeight { get; set; } = 10;
    }
}
