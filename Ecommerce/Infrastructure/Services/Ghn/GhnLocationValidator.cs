using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Infrastructure.Services.Ghn
{
    /// <summary>
    /// Validates receiver address against GHN location DB.
    /// Converts free-text province/district/ward to GHN IDs.
    /// Used internally by GhnShippingProvider.
    /// </summary>
    public sealed class GhnLocationValidator
    {
        private readonly GhnHttpClient _client;

        public GhnLocationValidator(GhnHttpClient client)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }

        public async Task<(bool IsValid, int? DistrictId, string? WardCode, string? Error)>
            ValidateAsync(string province, string district, string ward, CancellationToken ct = default)
        {
            var provinces = await _client.PostAsync<GhnProvince[]>("master-data/province", new { }, ct);
            if (!provinces.IsSuccess || provinces.Data == null)
                return (false, null, null, "Failed to fetch provinces from GHN.");

            var p = provinces.Data.FirstOrDefault(x =>
                x.ProvinceName.Contains(province, StringComparison.OrdinalIgnoreCase));
            if (p == null) return (false, null, null, $"Province '{province}' not found.");

            var districts = await _client.PostAsync<GhnDistrict[]>("master-data/district",
                new { province_id = p.ProvinceID }, ct);
            if (!districts.IsSuccess || districts.Data == null)
                return (false, null, null, "Failed to fetch districts from GHN.");

            var d = districts.Data.FirstOrDefault(x =>
                x.DistrictName.Contains(district, StringComparison.OrdinalIgnoreCase));
            if (d == null) return (false, null, null, $"District '{district}' not found.");

            var wards = await _client.PostAsync<GhnWard[]>("master-data/ward",
                new { district_id = d.DistrictID }, ct);
            if (!wards.IsSuccess || wards.Data == null)
                return (false, null, null, "Failed to fetch wards from GHN.");

            var w = wards.Data.FirstOrDefault(x =>
                x.WardName.Contains(ward, StringComparison.OrdinalIgnoreCase));
            if (w == null) return (false, null, null, $"Ward '{ward}' not found.");

            return (true, d.DistrictID, w.WardCode, null);
        }
    }
}
