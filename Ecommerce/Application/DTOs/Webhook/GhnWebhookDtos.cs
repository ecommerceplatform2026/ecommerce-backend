using System.Collections.Generic;
using Domain.Enums;

namespace Application.DTOs.Webhook
{
    public sealed class GhnWebhookPayload
    {
        public string order_code { get; set; } = string.Empty;
        public string status { get; set; } = string.Empty;
        public string? timestamp { get; set; }
        public GhnWebhookData? data { get; set; }
    }

    public sealed class GhnWebhookData
    {
        public string? current_status { get; set; }
        public string? previous_status { get; set; }
        public string? order_code { get; set; }
        public string? reason { get; set; }
    }

    public static class GhnStatusMapper
    {
        private static readonly Dictionary<string, DeliveryStatus> Map = new()
        {
            ["ready_to_pick"] = DeliveryStatus.Created,
            ["picking"] = DeliveryStatus.PickedUp,
            ["picked"] = DeliveryStatus.InTransit,
            ["storing"] = DeliveryStatus.InTransit,
            ["transporting"] = DeliveryStatus.InTransit,
            ["sorting"] = DeliveryStatus.InTransit,
            ["delivering"] = DeliveryStatus.OutForDelivery,
            ["delivered"] = DeliveryStatus.Delivered,
            ["delivery_fail"] = DeliveryStatus.Failed,
            ["return"] = DeliveryStatus.Returned,
            ["returned"] = DeliveryStatus.Returned,
            ["cancel"] = DeliveryStatus.Cancelled,
            ["damage"] = DeliveryStatus.Exception,
            ["lost"] = DeliveryStatus.Exception,
        };

        public static DeliveryStatus ToDeliveryStatus(string ghnStatus)
        {
            if (Map.TryGetValue(ghnStatus.ToLowerInvariant(), out var status))
                return status;
            throw new ArgumentException($"Unknown GHN status: '{ghnStatus}'.");
        }
    }
}
