using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Infrastructure.Services.Ghn
{
    public sealed class GhnShopService
    {
        private readonly GhnHttpClient _client;
        private readonly IOptions<GhnOptions> _options;
        private GhnPickupAddress? _cached;

        public GhnShopService(GhnHttpClient client, IOptions<GhnOptions> options)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public async Task<GhnPickupAddress> GetPickupAddressAsync(CancellationToken ct = default)
        {
            if (_cached != null) return _cached;

            var opts = _options.Value;
            _cached = new GhnPickupAddress(opts.FromName, opts.FromPhone, opts.FromAddress, opts.FromDistrictId, opts.FromWardCode);

            var response = await _client.GetAsync<GhnShopListData>("v2/shop/all", ct);

            if (response.IsSuccess && response.Data?.shops != null)
            {
                var shop = response.Data.shops.FirstOrDefault(s => s._id == opts.ShopId);
                if (shop != null)
                {
                    _cached = new GhnPickupAddress(
                        name: Coalesce(shop.name, opts.FromName),
                        phone: Coalesce(shop.phone, opts.FromPhone),
                        address: Coalesce(shop.address_v2, shop.address, opts.FromAddress),
                        districtId: opts.FromDistrictId,
                        wardCode: Coalesce(shop.ward_id_v2 > 0 ? shop.ward_id_v2.ToString() : null, shop.ward_code, opts.FromWardCode)
                    );
                }
            }

            return _cached;
        }

        private static string Coalesce(params string?[] values)
        {
            foreach (var v in values)
                if (!string.IsNullOrWhiteSpace(v))
                    return v;
            return string.Empty;
        }
    }

    public sealed class GhnPickupAddress
    {
        public string Name { get; }
        public string Phone { get; }
        public string Address { get; }
        public int DistrictId { get; }
        public string WardCode { get; }

        public GhnPickupAddress(string name, string phone, string address, int districtId, string wardCode)
        {
            Name = name; Phone = phone; Address = address; DistrictId = districtId; WardCode = wardCode;
        }
    }

    internal sealed class GhnShopListData
    {
        public int last_offset { get; set; }
        public GhnShopDetail[]? shops { get; set; }
    }

    internal sealed class GhnShopDetail
    {
        public int _id { get; set; }
        public string name { get; set; } = string.Empty;
        public string phone { get; set; } = string.Empty;
        public string address { get; set; } = string.Empty;
        public string address_v2 { get; set; } = string.Empty;
        public int ward_id_v2 { get; set; }
        public int province_id_v2 { get; set; }
        public string ward_code { get; set; } = string.Empty;
        public int district_id { get; set; }
    }
}
