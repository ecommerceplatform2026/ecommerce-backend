namespace Domain.Enums
{
    public enum DeliveryStatus
    {
        Pending = 0,
        Created = 1,
        PickedUp = 2,
        InTransit = 3,
        OutForDelivery = 4,
        Delivered = 5,
        Failed = 6,
        Cancelled = 7,
        Returned = 8,
        Exception = 9
    }
}
