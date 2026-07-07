using Application.Common.Response;
using Application.DTOs.Delivery;
using Application.DTOs.Delivery.GHN;
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
                length = i.Length,
                width = i.Width,
                height = i.Height,
                weight = i.Weight,
                category = new GhnItemCategory { level1 = string.IsNullOrEmpty(i.CategoryName) ? "Hàng hóa" : i.CategoryName }
            }).ToList();

            var pickup = await _shopService.GetPickupAddressAsync(ct);

            var pkgLength = info.Items.Count > 0 ? info.Items.Max(i => i.Length) : 0;
            var pkgWidth = info.Items.Count > 0 ? info.Items.Max(i => i.Width) : 0;
            var pkgHeight = info.Items.Sum(i => i.Height);

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
                length = pkgLength,
                width = pkgWidth,
                height = pkgHeight,
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

            var result = new ShipmentResponse(
                response.Data.order_code,
                response.Data.order_code,
                response.Data.total_fee,
                response.Data.expected_delivery_time,
                response.Data.sort_code,
                response.Data.trans_type
            );

            return Result<ShipmentResponse>.Success(result);
        }
    }
}
