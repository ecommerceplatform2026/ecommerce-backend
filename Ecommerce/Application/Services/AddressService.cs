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

        public AddressService(IUnitOfWork unitOfWork, IUserService userService)
        {
            _unitOfWork = unitOfWork;
            _userService = userService;
        }

        public async Task<Result<List<AddressResponse>>> GetAddressesAsync(
            CancellationToken cancellationToken = default)
        {
            var currentUserResult = await _userService.GetCurrentUserWithAddressesAsync(cancellationToken);
            if (!currentUserResult.IsSuccess)
            {
                return ResultMapper.MapUserError<List<AddressResponse>>(currentUserResult);
            }

            var user = currentUserResult.Value!;
            var addressRepository = _unitOfWork.GetRepository<UserAddress>();
            var addresses = await addressRepository
                .GetQueryable()
                .Where(address => address.UserId == user.Id)
                .OrderByDescending(address => address.IsDefault)
                .ThenByDescending(address => address.CreatedAt)
                .ToListAsync(cancellationToken);

            var responses = addresses
                .Select(address => address.ToAddressResponse())
                .ToList();

            return Result<List<AddressResponse>>.Success(responses);
        }

        public async Task<Result<AddressResponse>> GetAddressByIdAsync(
            Guid addressId,
            CancellationToken cancellationToken = default)
        {
            var currentUserResult = await _userService.GetCurrentUserWithAddressesAsync(cancellationToken);
            if (!currentUserResult.IsSuccess)
            {
                return ResultMapper.MapUserError<AddressResponse>(currentUserResult);
            }

            var user = currentUserResult.Value!;
            var addressRepository = _unitOfWork.GetRepository<UserAddress>();
            var address = await addressRepository
                .GetQueryable()
                .FirstOrDefaultAsync(
                    item => item.Id == addressId && item.UserId == user.Id,
                    cancellationToken);

            if (address == null)
            {
                return Result<AddressResponse>.NotFound("Address not found.");
            }

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

            var userRepository = _unitOfWork.GetRepository<User>();
            userRepository.Update(user);
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

            var userRepository = _unitOfWork.GetRepository<User>();
            userRepository.Update(user);
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
            if (address == null)
            {
                return Result<bool>.NotFound("Address not found.");
            }

            var userRepository = _unitOfWork.GetRepository<User>();
            userRepository.Update(user);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<bool>.Success(true);
        }

    }
}
