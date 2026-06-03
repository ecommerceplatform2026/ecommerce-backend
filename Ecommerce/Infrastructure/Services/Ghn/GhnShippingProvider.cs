using Application.Common.Response;
using Application.DTOs.Delivery;
using Application.Interfaces.Services;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Infrastructure.Services.Ghn
{
    public sealed class GhnShippingProvider : IShippingProvider
    {
        public string CarrierCode => "GHN";

        private readonly GhnHttpClient _client;
        private readonly GhnLocationValidator _locationValidator;
        private readonly GhnShopService _shopService;
        private readonly IOptions<GhnOptions> _options;

        public GhnShippingProvider(
            GhnHttpClient client,
            GhnLocationValidator locationValidator,
            GhnShopService shopService,
            IOptions<GhnOptions> options)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _locationValidator = locationValidator ?? throw new ArgumentNullException(nameof(locationValidator));
            _shopService = shopService ?? throw new ArgumentNullException(nameof(shopService));
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public async Task<Result<ShipmentResponse>> CreateShipmentAsync(
            Guid orderId,
            CreateGhnShipmentRequest info,
            CancellationToken ct = default)
        {
            var (isValid, districtId, wardCode, error) =
                await _locationValidator.ValidateAsync(info.Province, info.District, info.Ward, ct);

            if (!isValid)
                return Result<ShipmentResponse>.Failure(error ?? "Invalid address.");

            var items = info.Items.Select(i => new GhnCreateOrderItem
            {
                name = i.Name,
                code = i.Sku,
                quantity = i.Quantity,
                price = (int)i.Price,
                length = _options.Value.DefaultLength,
                width = _options.Value.DefaultWidth,
                height = _options.Value.DefaultHeight,
                weight = i.Weight > 0 ? i.Weight : _options.Value.DefaultWeight,
                category = new GhnItemCategory { level1 = string.IsNullOrEmpty(i.CategoryName) ? "Hàng hóa" : i.CategoryName }
            }).ToList();

            var pickup = await _shopService.GetPickupAddressAsync(ct);

            var request = new GhnCreateOrderRequest
            {
                from_name = pickup.Name,
                from_phone = pickup.Phone,
                from_address = pickup.Address,
                from_district_id = pickup.DistrictId,
                from_ward_code = pickup.WardCode,
                to_name = info.ReceiverName,
                to_phone = info.ReceiverPhone,
                to_address = info.AddressLine,
                to_ward_code = wardCode!,
                to_district_id = districtId!.Value,
                weight = info.TotalWeight,
                length = _options.Value.DefaultLength,
                width = _options.Value.DefaultWidth,
                height = _options.Value.DefaultHeight,
                service_type_id = _options.Value.ServiceTypeId,
                payment_type_id = _options.Value.PaymentTypeId,
                required_note = _options.Value.RequiredNote,
                cod_amount = info.CodAmount > 0 ? (int)info.CodAmount : null,
                insurance_value = info.InsuranceValue > 0 ? (int)info.InsuranceValue : null,
                items = items,
                client_order_code = info.OrderCode,
                note = info.OrderCode
            };

            var response = await _client.PostAsync<GhnCreateOrderResponse>(
                "v2/shipping-order/create", request, ct);

            if (!response.IsSuccess || response.Data == null)
                return Result<ShipmentResponse>.Failure(
                    $"GHN API error: {response.Message}");

            Console.WriteLine("======================================================================================");
            Console.WriteLine($"GHN Create Order Response: {response.Data.order_code}, Fee: {response.Data.total_fee}");

            // long fee = long.TryParse(response.Data.total_fee, out var f) ? f : 0;

            var fee = response.Data.fee;
            var breakdown = new ShipmentFeeBreakdown(
                mainService: fee.main_service,
                insurance: fee.insurance,
                codFee: fee.cod_fee,
                stationDo: fee.station_do,
                stationPu: fee.station_pu,
                returns: fee.@return,
                r2s: fee.r2s,
                returnAgain: fee.return_again,
                coupon: fee.coupon,
                documentReturn: fee.document_return,
                doubleCheck: fee.double_check,
                doubleCheckDeliver: fee.double_check_deliver,
                pickRemoteAreasFee: fee.pick_remote_areas_fee,
                deliverRemoteAreasFee: fee.deliver_remote_areas_fee,
                pickRemoteAreasFeeReturn: fee.pick_remote_areas_fee_return,
                deliverRemoteAreasFeeReturn: fee.deliver_remote_areas_fee_return,
                codFailedFee: fee.cod_failed_fee,
                changeToAddressFee: fee.change_to_address_fee,
                changeReturnAddressFee: fee.change_return_address_fee
            );

            var result = new ShipmentResponse(
                response.Data.order_code,
                response.Data.order_code,
                response.Data.total_fee,
                response.Data.expected_delivery_time,
                sortCode: response.Data.sort_code,
                transportType: response.Data.trans_type,
                feeBreakdown: breakdown
            );

            return Result<ShipmentResponse>.Success(result);
        }
    }
}
