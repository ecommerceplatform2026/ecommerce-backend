using Application.Common.Response;
using Application.DTOs.Delivery.GHN;

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
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = null
        };

        public GhnWebhookService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public async Task<Result<string>> ProcessStatusUpdateAsync(object payload, CancellationToken ct)
        {
            if (payload is not GhnWebhookPayload webhook)
                return BuildErrorResponse("Invalid webhook payload type.");

            if (string.IsNullOrWhiteSpace(webhook.OrderCode))
                return BuildErrorResponse("Missing OrderCode in webhook payload.", webhook);

            var type = webhook.Type?.ToLowerInvariant() ?? "switch_status";
            var errors = "";

            var deliveryRepo = _unitOfWork.GetRepository<Delivery>();
            var orderRepo = _unitOfWork.GetRepository<Order>();

            var delivery = await deliveryRepo.FindAsync(
                d => d.CarrierOrderCode == webhook.OrderCode && !d.IsDeleted,
                asNoTracking: false,
                ct);

            if (delivery == null)
                return BuildErrorResponse($"Delivery with carrier order code '{webhook.OrderCode}' not found.", webhook);

            var order = await orderRepo.FindAsync(
                o => o.Id == delivery.OrderId && !o.IsDeleted,
                asNoTracking: false,
                ct);

            if (order == null)
                return BuildErrorResponse($"Order for delivery {delivery.Id} not found.", webhook);

            try
            {
                switch (type)
                {
                    case "create":
                        break;

                    case "switch_status":
                        errors = ProcessStatusUpdate(delivery, order, webhook);
                        break;

                    case "update_weight":
                        delivery.UpdateWeight(webhook.Weight > 0 ? webhook.Weight : webhook.ConvertedWeight);
                        break;

                    case "update_cod":
                        delivery.UpdateCodAmount(webhook.CODAmount);
                        break;

                    case "update_fee":
                        delivery.UpdateShippingFee(webhook.TotalFee);
                        break;

                    default:
                        errors = $"Unknown webhook type: {webhook.Type}";
                        break;
                }
            }
            catch (ArgumentException ex)
            {
                errors = ex.Message;
            }

            if (string.IsNullOrEmpty(errors))
            {
                orderRepo.Update(order);
                await _unitOfWork.SaveChangesAsync(ct);
            }

            return BuildSuccessResponse(webhook, delivery, order, errors);
        }

        private static string ProcessStatusUpdate(Delivery delivery, Order order, GhnWebhookPayload webhook)
        {
            if (string.IsNullOrWhiteSpace(webhook.Status))
                return "Missing Status in webhook payload.";

            var newStatus = GhnStatusMapper.ToDeliveryStatus(webhook.Status);

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
                    delivery.MarkFailed(webhook.Reason ?? "Delivery failed");
                    break;

                case DeliveryStatus.Cancelled:
                    delivery.MarkCancelled();
                    break;

                case DeliveryStatus.Returned:
                    delivery.MarkReturned();
                    break;

                case DeliveryStatus.Exception:
                    delivery.MarkException(webhook.Reason);
                    break;

                default:
                    delivery.MarkException($"Unhandled GHN status: {webhook.Status}");
                    break;
            }

            return "";
        }

        private static Result<string> BuildSuccessResponse(
            GhnWebhookPayload webhook, Delivery delivery, Order order, string error)
        {
            var paymentType = order.PaymentMethod == PaymentMethod.COD ? 2 : 1;

            var response = new GhnWebhookResponse
            {
                CODAmount = delivery.CodAmount,
                CODTransferDate = null,
                ClientOrderCode = $"ORD-{order.OrderCode}",
                ConvertedWeight = delivery.Weight,
                Description = GetDescription(webhook.Type),
                Fee = new GhnWebhookFee
                {
                    CODFailedFee = 0,
                    CODFee = 0,
                    Coupon = 0,
                    DeliverRemoteAreasFee = 0,
                    DocumentReturn = 0,
                    DoubleCheck = 0,
                    Insurance = delivery.InsuranceValue,
                    MainService = delivery.ShippingFee,
                    PickRemoteAreasFee = 0,
                    R2S = 0,
                    Return = 0,
                    StationDO = 0,
                    StationPU = 0,
                    Total = delivery.ShippingFee
                },
                Height = delivery.Height,
                IsPartialReturn = false,
                Length = delivery.Length,
                OrderCode = delivery.CarrierOrderCode ?? webhook.OrderCode,
                PartialReturnCode = "",
                PaymentType = paymentType,
                Reason = error,
                ReasonCode = string.IsNullOrEmpty(error) ? "" : "ERROR",
                ShopID = webhook.ShopID,
                Status = webhook.Status,
                Time = DateTime.UtcNow.ToString("o"),
                TotalFee = delivery.ShippingFee,
                Type = webhook.Type?.ToLowerInvariant() ?? "switch_status",
                Warehouse = webhook.Warehouse ?? "",
                Weight = delivery.Weight,
                Width = delivery.Width
            };

            var json = JsonSerializer.Serialize(response, JsonOptions);
            return string.IsNullOrEmpty(error)
                ? Result<string>.Success(json)
                : Result<string>.Success(json);
        }

        private static Result<string> BuildErrorResponse(string error, GhnWebhookPayload? webhook = null)
        {
            var response = new GhnWebhookResponse
            {
                CODAmount = 0,
                CODTransferDate = null,
                ClientOrderCode = webhook?.ClientOrderCode ?? "",
                ConvertedWeight = 0,
                Description = GetDescription(webhook?.Type),
                Fee = new GhnWebhookFee(),
                Height = 0,
                IsPartialReturn = false,
                Length = 0,
                OrderCode = webhook?.OrderCode ?? "",
                PartialReturnCode = "",
                PaymentType = 0,
                Reason = error,
                ReasonCode = "ERROR",
                ShopID = webhook?.ShopID ?? 0,
                Status = webhook?.Status ?? "",
                Time = DateTime.UtcNow.ToString("o"),
                TotalFee = 0,
                Type = webhook?.Type?.ToLowerInvariant() ?? "switch_status",
                Warehouse = webhook?.Warehouse ?? "",
                Weight = 0,
                Width = 0
            };

            var json = JsonSerializer.Serialize(response, JsonOptions);
            return Result<string>.Success(json);
        }

        private static string GetDescription(string? type)
        {
            return type?.ToLowerInvariant() switch
            {
                "create" => "Tạo đơn hàng",
                "switch_status" => "Cập nhật trạng thái",
                "update_weight" => "Cập nhật cân nặng",
                "update_cod" => "Cập nhật COD",
                "update_fee" => "Cập nhật phí vận chuyển",
                _ => "Xử lý đơn hàng"
            };
        }
    }
}
