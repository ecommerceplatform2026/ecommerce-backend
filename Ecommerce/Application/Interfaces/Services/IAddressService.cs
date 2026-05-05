using Application.Common.Response;
using Application.DTOs.User.Address;

namespace Application.Interfaces.Services
{
    public interface IAddressService
    {
        Task<Result<List<AddressResponse>>> GetAddressesAsync(CancellationToken cancellationToken = default);
        Task<Result<AddressResponse>> GetAddressByIdAsync(Guid addressId, CancellationToken cancellationToken = default);
        Task<Result<AddressResponse>> CreateAddressAsync(CreateAddressRequest request, CancellationToken cancellationToken = default);
        Task<Result<AddressResponse>> UpdateAddressAsync(Guid addressId, UpdateAddressRequest request, CancellationToken cancellationToken = default);
        Task<Result<bool>> DeleteAddressAsync(Guid addressId, CancellationToken cancellationToken = default);
    }
}
