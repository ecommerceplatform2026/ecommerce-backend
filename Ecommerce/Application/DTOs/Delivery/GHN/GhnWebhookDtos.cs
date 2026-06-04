using Application.Common.Attributes;
using Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace Application.DTOs.Delivery.GHN
{
    public sealed class GhnWebhookPayload
    {
        [DefaultValue(3000000)]
        public long CODAmount { get; set; }

        [DefaultValue(null)]
        public string? CODTransferDate { get; set; }

        [DefaultValue("")]
        public string ClientOrderCode { get; set; } = string.Empty;

        [DefaultValue(200)]
        public int ConvertedWeight { get; set; }

        [DefaultValue("Đổi trạng thái")]
        public string Description { get; set; } = string.Empty;

        public GhnWebhookFee? Fee { get; set; }

        [DefaultValue(10)]
        public int Height { get; set; }

        [DefaultValue(false)]
        public bool IsPartialReturn { get; set; }

        [DefaultValue(10)]
        public int Length { get; set; }

        [DefaultValue("PLEASE_ENTER_ORDER_CODE_HERE")]
        public string OrderCode { get; set; } = string.Empty;

        [DefaultValue("")]
        public string PartialReturnCode { get; set; } = string.Empty;

        [DefaultValue(1)]
        public int PaymentType { get; set; }

        [DefaultValue("")]
        public string Reason { get; set; } = string.Empty;

        [DefaultValue("")]
        public string ReasonCode { get; set; } = string.Empty;

        [DefaultValue(200536)]
        public int ShopID { get; set; }

        [DefaultValue("ready_to_pick")]
        [AllowedValues("ready_to_pick", "picking", "picked", "storing", "transporting", "sorting", "delivering", "delivered", "delivery_fail", "return", "returned", "cancel", "damage", "lost")]
        public string Status { get; set; } = string.Empty;

        [DefaultValue("2021-11-11T03:52:50.158Z")]
        public string Time { get; set; } = string.Empty;

        [DefaultValue(71400)]
        public long TotalFee { get; set; }

        [DefaultValue("switch_status")]
        [AllowedValues("create", "switch_status", "update_weight", "update_cod", "update_fee")]
        public string Type { get; set; } = string.Empty;

        [DefaultValue("Bưu Cục 229 Quan Nhân-Q.Thanh Xuân-HN")]
        public string Warehouse { get; set; } = string.Empty;

        [DefaultValue(10)]
        public int Weight { get; set; }

        [DefaultValue(10)]
        public int Width { get; set; }
    }

    public sealed class GhnWebhookFee
    {
        [DefaultValue(0)]
        public int CODFailedFee { get; set; }

        [DefaultValue(0)]
        public int CODFee { get; set; }

        [DefaultValue(0)]
        public int Coupon { get; set; }

        [DefaultValue(0)]
        public int DeliverRemoteAreasFee { get; set; }

        [DefaultValue(0)]
        public int DocumentReturn { get; set; }

        [DefaultValue(0)]
        public int DoubleCheck { get; set; }

        [DefaultValue(17500)]
        public long Insurance { get; set; }

        [DefaultValue(53900)]
        public long MainService { get; set; }

        [DefaultValue(53900)]
        public long PickRemoteAreasFee { get; set; }

        [DefaultValue(0)]
        public int R2S { get; set; }

        [DefaultValue(0)]
        public int Return { get; set; }

        [DefaultValue(0)]
        public int StationDO { get; set; }

        [DefaultValue(0)]
        public int StationPU { get; set; }

        [DefaultValue(0)]
        public long Total { get; set; }
    }

    public sealed class GhnWebhookResponse
    {
        public long CODAmount { get; set; }
        public string? CODTransferDate { get; set; }
        public string ClientOrderCode { get; set; } = string.Empty;
        public int ConvertedWeight { get; set; }
        public string Description { get; set; } = string.Empty;
        public GhnWebhookFee? Fee { get; set; }
        public int Height { get; set; }
        public bool IsPartialReturn { get; set; }
        public int Length { get; set; }
        public string OrderCode { get; set; } = string.Empty;
        public string PartialReturnCode { get; set; } = string.Empty;
        public int PaymentType { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string ReasonCode { get; set; } = string.Empty;
        public int ShopID { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Time { get; set; } = string.Empty;
        public long TotalFee { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Warehouse { get; set; } = string.Empty;
        public int Weight { get; set; }
        public int Width { get; set; }
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
