using Application.Common.Response;
using Application.Configurations;
using Application.DTOs.Delivery;
using Application.Interfaces.Repositories.Base;
using Application.Interfaces.Services;
using Domain.Entities;
using Domain.Enums;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Services
{
    /// <summary>
    /// Carrier-agnostic orchestrator.
    /// Resolves IShippingProvider by carrierCode, prepares OrderShipmentInfo
    /// from domain models, delegates to provider, persists Delivery entity,
    /// transitions order to Shipping.
    /// Zero carrier-specific logic — all carrier details in providers.
    /// </summary>
    public sealed class ShippingService : IShippingService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEnumerable<IShippingProvider> _providers;
        private readonly ShippingSettings _settings;

        public ShippingService(
            IUnitOfWork unitOfWork,
            IEnumerable<IShippingProvider> providers,
            IOptions<ShippingSettings> settings)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _providers = providers ?? throw new ArgumentNullException(nameof(providers));
            _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        }

        public async Task<Result<ShipmentResponse>> CreateShipmentAsync(
            Guid orderId,
            string carrierCode,
            CancellationToken cancellationToken = default)
        {
            if (orderId == Guid.Empty)
                return Result<ShipmentResponse>.Failure("Order ID cannot be empty.");

            if (string.IsNullOrWhiteSpace(carrierCode))
                carrierCode = _settings.DefaultCarrier;

            var provider = _providers.FirstOrDefault(p =>
                p.CarrierCode.Equals(carrierCode, StringComparison.OrdinalIgnoreCase));

            if (provider == null)
                return Result<ShipmentResponse>.Failure($"No shipping provider found for carrier '{carrierCode}'.");

            var order = await _unitOfWork.GetRepository<Order>()
                .FindAsync(
                    o => o.Id == orderId && !o.IsDeleted,
                    asNoTracking: false,
                    cancellationToken,
                    o => o.OrderItems,
                    o => o.User!,
                    o => o.User!.UserAddresses);

            if (order == null)
                return Result<ShipmentResponse>.NotFound("Order not found.");

            if (order.Status != OrderStatus.Pending && order.Status != OrderStatus.Confirmed)
                return Result<ShipmentResponse>.Failure($"Cannot create shipment for order in '{order.Status}' status.");

            if (order.Delivery != null)
                return Result<ShipmentResponse>.Failure("Shipment already exists for this order.");

            var address = order.User?.UserAddresses?.FirstOrDefault(a => a.IsDefault && !a.IsDeleted)
                ?? order.User?.UserAddresses?.FirstOrDefault(a => !a.IsDeleted);

            if (address == null)
                return Result<ShipmentResponse>.Failure("User has no shipping address.");

            foreach (var item in order.OrderItems)
            {
                var variant = await _unitOfWork.GetRepository<ProductVariant>()
                    .FindAsync(v => v.Id == item.ProductVariantId, asNoTracking: true, cancellationToken);

                if (variant == null || variant.IsOutOfStock())
                    return Result<ShipmentResponse>.Failure($"Product for variant {item.ProductVariantId} is out of stock.");
            }

            int totalWeight = order.OrderItems.Sum(oi =>
            {
                var snapshot = JsonSerializer.Deserialize<SnapshotData>(oi.ProductSnapshot);
                return snapshot?.Weight ?? _settings.DefaultWeight;
            });

            long codAmount = order.PaymentMethod == PaymentMethod.COD
                ? order.TotalAmount.Amount
                : 0;

            long insuranceValue = order.TotalAmount.Amount;

            var items = order.OrderItems.Select(oi =>
            {
                var snapshot = JsonSerializer.Deserialize<SnapshotData>(oi.ProductSnapshot);
                return new CreateGhnShipmentItemInfo
                {
                    Name = snapshot?.ProductName ?? "Product",
                    Sku = snapshot?.SKU ?? oi.Id.ToString(),
                    Quantity = oi.Quantity,
                    Price = oi.Price.Amount,
                    Weight = snapshot?.Weight ?? _settings.DefaultWeight,
                    CategoryName = snapshot?.CategoryName
                };
            }).ToList();

            var shipmentInfo = new CreateGhnShipmentRequest
            {
                ReceiverName = address.ReceiverName,
                ReceiverPhone = address.PhoneNumber,
                AddressLine = address.AddressLine,
                Province = address.Province ?? string.Empty,
                District = address.District ?? string.Empty,
                Ward = address.Ward ?? string.Empty,
                TotalWeight = Math.Max(totalWeight, _settings.DefaultWeight),
                CodAmount = codAmount,
                InsuranceValue = Math.Min(insuranceValue, 10_000_000),
                OrderCode = $"ORD-{order.OrderCode}",
                Items = items
            };

            var result = await provider.CreateShipmentAsync(order.Id, shipmentInfo, cancellationToken);

            if (!result.IsSuccess || result.Value == null)
                return Result<ShipmentResponse>.Failure(result.Errors?.FirstOrDefault() ?? "Shipping provider failed.");

            var delivery = Delivery.Create(
                order.Id,
                carrierCode,
                shipmentInfo.ReceiverName, shipmentInfo.ReceiverPhone, shipmentInfo.AddressLine,
                shipmentInfo.Province, shipmentInfo.District, shipmentInfo.Ward,
                shipmentInfo.TotalWeight, _settings.DefaultLength, _settings.DefaultWidth, _settings.DefaultHeight,
                codAmount, shipmentInfo.InsuranceValue,
                $"Order #{order.OrderCode}");

            delivery.MarkShipmentCreated(
                result.Value.TrackingCode,
                result.Value.CarrierOrderCode,
                result.Value.ShippingFee);

            await _unitOfWork.GetRepository<Delivery>().AddAsync(delivery, cancellationToken);

            if (order.Status == OrderStatus.Pending || order.Status == OrderStatus.Confirmed)
                order.MarkAsProcessing();

            order.MarkAsShipping();
            _unitOfWork.GetRepository<Order>().Update(order);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<ShipmentResponse>.Success(result.Value);
        }

        private sealed class SnapshotData
        {
            public string? ProductName { get; set; }
            public string? SKU { get; set; }
            public string? CategoryName { get; set; }
            public int Weight { get; set; } = 500;
        }
    }
}
