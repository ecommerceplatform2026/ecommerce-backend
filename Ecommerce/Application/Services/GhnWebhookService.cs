using Application.Common.Response;
using Application.DTOs.Webhook;
using Application.Interfaces.Repositories.Base;
using Application.Interfaces.Services;
using Domain.Entities;
using Domain.Enums;
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Services
{
    public sealed class GhnWebhookService : IShippingWebhookHandler
    {
        public string CarrierCode => "GHN";

        private readonly IUnitOfWork _unitOfWork;

        public GhnWebhookService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public async Task<Result<bool>> ProcessStatusUpdateAsync(string payload, CancellationToken ct)
        {
            GhnWebhookPayload? webhook;
            try
            {
                webhook = JsonSerializer.Deserialize<GhnWebhookPayload>(payload, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (JsonException ex)
            {
                return Result<bool>.Failure($"Invalid webhook payload: {ex.Message}");
            }

            if (webhook == null || string.IsNullOrWhiteSpace(webhook.order_code))
                return Result<bool>.Failure("Missing order_code in webhook payload.");

            if (string.IsNullOrWhiteSpace(webhook.status))
                return Result<bool>.Failure("Missing status in webhook payload.");

            var deliveryRepo = _unitOfWork.GetRepository<Delivery>();
            var orderRepo = _unitOfWork.GetRepository<Order>();

            var delivery = await deliveryRepo.FindAsync(
                d => d.CarrierOrderCode == webhook.order_code && !d.IsDeleted,
                asNoTracking: false,
                ct);

            if (delivery == null)
                return Result<bool>.NotFound($"Delivery with carrier order code '{webhook.order_code}' not found.");

            var order = await orderRepo.FindAsync(
                o => o.Id == delivery.OrderId && !o.IsDeleted,
                asNoTracking: false,
                ct);

            if (order == null)
                return Result<bool>.NotFound($"Order for delivery {delivery.Id} not found.");

            try
            {
                var newStatus = GhnStatusMapper.ToDeliveryStatus(webhook.status);

                switch (newStatus)
                {
                    case DeliveryStatus.Created:
                        break;

                    case DeliveryStatus.PickedUp:
                        delivery.MarkPickedUp();
                        break;

                    case DeliveryStatus.InTransit:
                        delivery.MarkInTransit();
                        break;

                    case DeliveryStatus.OutForDelivery:
                        delivery.MarkOutForDelivery();
                        if (order.Status != OrderStatus.Shipping)
                            order.MarkAsShipping();
                        break;

                    case DeliveryStatus.Delivered:
                        delivery.MarkDelivered();
                        order.MarkAsDelivered();
                        break;

                    case DeliveryStatus.Failed:
                        delivery.MarkFailed(webhook.data?.reason ?? "Delivery failed");
                        break;

                    case DeliveryStatus.Cancelled:
                        delivery.MarkCancelled();
                        break;

                    case DeliveryStatus.Returned:
                        delivery.MarkReturned();
                        break;

                    case DeliveryStatus.Exception:
                        delivery.MarkException(webhook.data?.reason);
                        break;

                    default:
                        delivery.MarkException($"Unhandled GHN status: {webhook.status}");
                        break;
                }
            }
            catch (ArgumentException ex)
            {
                return Result<bool>.Failure(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return Result<bool>.Failure(ex.Message);
            }

            orderRepo.Update(order);
            await _unitOfWork.SaveChangesAsync(ct);

            return Result<bool>.Success(true);
        }
    }
}
