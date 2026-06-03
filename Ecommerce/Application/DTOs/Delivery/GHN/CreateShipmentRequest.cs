using System;
using System.Collections.Generic;

namespace Application.DTOs.Delivery
{
    /// <summary>
    /// Carrier-agnostic input for shipment creation.
    /// Resolved from domain models by the orchestrator before calling provider.
    /// </summary>
    public sealed class CreateGhnShipmentRequest
    {
        public string ReceiverName { get; set; } = string.Empty;
        public string ReceiverPhone { get; set; } = string.Empty;
        public string AddressLine { get; set; } = string.Empty;
        public string Province { get; set; } = string.Empty;
        public string District { get; set; } = string.Empty;
        public string Ward { get; set; } = string.Empty;
        public int TotalWeight { get; set; }
        public long CodAmount { get; set; }
        public long InsuranceValue { get; set; }
        public string OrderCode { get; set; } = string.Empty;
        public List<CreateGhnShipmentItemInfo> Items { get; set; } = new();
    }

    public sealed class CreateGhnShipmentItemInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Sku { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public long Price { get; set; }
        public int Weight { get; set; }
        public string? CategoryName { get; set; }
    }
}
