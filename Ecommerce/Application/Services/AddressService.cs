using Application.Common.Response;
using Application.DTOs.User.Address;
using Application.Interfaces.Repositories.Base;
using Application.Interfaces.Services;
using Application.Mappings;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Application.Services
{
    public class AddressService : IAddressService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IUserService _userService;
        private readonly ICurrentUserService _currentUserService;

        public AddressService(IUnitOfWork unitOfWork, IUserService userService, ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _userService = userService;
            _currentUserService = currentUserService;
        }

        public async Task<Result<List<AddressResponse>>> GetAddressesAsync(
            CancellationToken cancellationToken = default)
        {
            var currentUserId = _currentUserService.GetUserIdOrNull();

            if (!Guid.TryParse(currentUserId, out var userId))
                return Result<List<AddressResponse>>.Unauthorized("Unauthorized.");

            var addresses = await _unitOfWork
                .GetRepository<UserAddress>()
                .GetQueryable()
                .AsNoTracking()
                .Where(address => address.UserId == userId)
                .OrderByDescending(address => address.IsDefault)
                .ThenByDescending(address => address.CreatedAt)
                .Select(address => address.ToAddressResponse())
                .ToListAsync(cancellationToken);

            return Result<List<AddressResponse>>.Success(addresses);
        }

        public async Task<Result<AddressResponse>> GetAddressByIdAsync(
            Guid addressId,
            CancellationToken cancellationToken = default)
        {
            var currentUserId = _currentUserService.GetUserIdOrNull();

            if (!Guid.TryParse(currentUserId, out var userId))
                return Result<AddressResponse>.Unauthorized("Unauthorized.");

            var addressRepository = _unitOfWork.GetRepository<UserAddress>();
            var address = await addressRepository
                .GetQueryable()
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    item => item.Id == addressId && item.UserId == userId,
                    cancellationToken);

            if (address == null)
                return Result<AddressResponse>.NotFound("Address not found.");

            return Result<AddressResponse>.Success(address.ToAddressResponse());
        }

        public async Task<Result<AddressResponse>> CreateAddressAsync(
            CreateAddressRequest request,
            CancellationToken cancellationToken = default)
        {
            var currentUserResult = await _userService.GetCurrentUserWithAddressesAsync(cancellationToken);
            if (!currentUserResult.IsSuccess)
            {
                return ResultMapper.MapUserError<AddressResponse>(currentUserResult);
            }

            var user = currentUserResult.Value!;
            var address = user.AddAddress(
                request.ReceiverName,
                request.PhoneNumber,
                request.AddressLine,
                request.Ward,
                request.District,
                request.Province,
                request.IsDefault);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<AddressResponse>.Success(address.ToAddressResponse());
        }

        public async Task<Result<AddressResponse>> UpdateAddressAsync(
            Guid addressId,
            UpdateAddressRequest request,
            CancellationToken cancellationToken = default)
        {
            var currentUserResult = await _userService.GetCurrentUserWithAddressesAsync(cancellationToken);
            if (!currentUserResult.IsSuccess)
            {
                return ResultMapper.MapUserError<AddressResponse>(currentUserResult);
            }

            var user = currentUserResult.Value!;
            var address = user.UpdateAddress(
                addressId,
                request.ReceiverName,
                request.PhoneNumber,
                request.AddressLine,
                request.Ward,
                request.District,
                request.Province,
                request.IsDefault);

            if (address == null)
            {
                return Result<AddressResponse>.NotFound("Address not found.");
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<AddressResponse>.Success(address.ToAddressResponse());
        }

        public async Task<Result<bool>> DeleteAddressAsync(
            Guid addressId,
            CancellationToken cancellationToken = default)
        {
            var currentUserResult = await _userService.GetCurrentUserWithAddressesAsync(cancellationToken);
            if (!currentUserResult.IsSuccess)
            {
                return ResultMapper.MapUserError<bool>(currentUserResult);
            }

            var user = currentUserResult.Value!;
            var address = user.DeleteAddress(addressId, user.Id.ToString());
            if (address is null)
            {
                return Result<bool>.NotFound("Address not found.");
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<bool>.Success(true);
        }

    }
}
