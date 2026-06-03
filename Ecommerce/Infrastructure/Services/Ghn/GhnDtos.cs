using System.Collections.Generic;
using Domain.Enums;

namespace Infrastructure.Services.Ghn
{
    /// <summary>
    /// All GHN-specific data models. Not exposed to Application layer.
    /// </summary>
    public sealed class GhnApiResponse<T>
    {
        public int Code { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
        public bool IsSuccess => Code == 200;
    }

    public sealed class GhnProvince
    {
        public int ProvinceID { get; set; }
        public string ProvinceName { get; set; } = string.Empty;
    }

    public sealed class GhnDistrict
    {
        public int DistrictID { get; set; }
        public string DistrictName { get; set; } = string.Empty;
    }

    public sealed class GhnWard
    {
        public string WardCode { get; set; } = string.Empty;
        public string WardName { get; set; } = string.Empty;
    }

    // snake_case for GHN API
    public sealed class GhnCreateOrderRequest
    {
        public string to_name { get; set; } = string.Empty;
        public string to_phone { get; set; } = string.Empty;
        public string to_address { get; set; } = string.Empty;
        public string to_ward_code { get; set; } = string.Empty;
        public int to_district_id { get; set; }
        public string? from_name { get; set; }
        public string? from_phone { get; set; }
        public string? from_address { get; set; }
        public int? from_district_id { get; set; }
        public string? from_ward_code { get; set; }
        public int weight { get; set; }
        public int length { get; set; }
        public int width { get; set; }
        public int height { get; set; }
        public int service_type_id { get; set; } = 2;
        public int payment_type_id { get; set; } = 2;
        public string required_note { get; set; } = "CHOTHUHANG";
        public int? cod_amount { get; set; }
        public int? insurance_value { get; set; }
        public List<GhnCreateOrderItem>? items { get; set; }
        public string? client_order_code { get; set; }
        public string? note { get; set; }
    }

    public sealed class GhnCreateOrderItem
    {
        public string name { get; set; } = string.Empty;
        public string code { get; set; } = string.Empty;
        public int quantity { get; set; }
        public int price { get; set; }
        public int length { get; set; }
        public int width { get; set; }
        public int height { get; set; }
        public int weight { get; set; }
        public GhnItemCategory? category { get; set; }
    }

    public sealed class GhnItemCategory
    {
        public string level1 { get; set; } = string.Empty;
    }

    public sealed class GhnCreateOrderResponse
    {
        public string order_code { get; set; } = string.Empty;
        public GhnFee fee { get; set; } = new();
        public long total_fee { get; set; }
        public string? expected_delivery_time { get; set; }
    }

    public sealed class GhnFee
    {
        public int main_service { get; set; }
        public int insurance { get; set; }
    }

    /// <summary>
    /// Webhook payload from GHN when shipment status changes.
    /// GHN sends POST with Content-Type: application/json to our registered URL.
    /// </summary>
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

    /// <summary>
    /// Maps GHN webhook status string to internal DeliveryStatus.
    /// </summary>
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
