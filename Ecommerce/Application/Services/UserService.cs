using Application.Common.Response;
using Application.DTOs.User;
using Application.Interfaces.Repositories.Base;
using Application.Interfaces.Services;
using Application.Mappings;
using Domain.Entities;

namespace Application.Services
{
    public class UserService : IUserService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IProductImageStorage _productImageStorage;

        public UserService(
            IUnitOfWork unitOfWork, 
            ICurrentUserService currentUserService,
            IProductImageStorage productImageStorage)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _productImageStorage = productImageStorage;
        }

        public async Task<Result<UserResponse>> GetUserAsync(
            CancellationToken cancellationToken = default)
        {
            var currentUserResult = await GetCurrentUserWithAddressesAsync(cancellationToken);
            if (!currentUserResult.IsSuccess)
            {
                return ResultMapper.MapUserError<UserResponse>(currentUserResult);
            }

            var user = currentUserResult.Value!;

            return Result<UserResponse>.Success(user.ToUserResponse());
        }

        public async Task<Result<UserResponse>> UpdateUserAsync(
            UpdateUserRequest request,
            CancellationToken cancellationToken = default)
        {
            var currentUserResult = await GetCurrentUserWithAddressesAsync(cancellationToken);
            if (!currentUserResult.IsSuccess)
            {
                return ResultMapper.MapUserError<UserResponse>(currentUserResult);
            }

            var user = currentUserResult.Value!;

            user.UpdateProfile(
                request.FullName,
                request.Avatar,
                request.PhoneNumber,
                request.DateOfBirth);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<UserResponse>.Success(user.ToUserResponse());
        }

        public async Task<Result<User>> GetCurrentUserWithAddressesAsync(CancellationToken cancellationToken = default)
        {
            var userIdResult = GetCurrentUserIdResult();
            if (!userIdResult.IsSuccess)
            {
                return Result<User>.Unauthorized(userIdResult.Errors.First());
            }

            var userRepository = _unitOfWork.GetRepository<User>();
            var user = await userRepository.GetByIdAsync(userIdResult.Value!, cancellationToken, user => user.UserAddresses);

            return user == null
                ? Result<User>.NotFound("User not found.")
                : Result<User>.Success(user);
        }

        public async Task<Result<UserResponse>> UploadAvatarAsync(System.IO.Stream fileStream, string fileName, string contentType, CancellationToken cancellationToken = default)
        {
            var currentUserResult = await GetCurrentUserWithAddressesAsync(cancellationToken);
            if (!currentUserResult.IsSuccess)
            {
                return ResultMapper.MapUserError<UserResponse>(currentUserResult);
            }

            var user = currentUserResult.Value!;

            try
            {
                var uploadResult = await _productImageStorage.UploadAsync(fileStream, fileName, contentType, cancellationToken);
                
                // Update User avatar url manually
                user.AvatarUrl = uploadResult.ImageUrl;
                _unitOfWork.GetRepository<User>().Update(user);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                return Result<UserResponse>.Success(user.ToUserResponse());
            }
            catch (Exception ex)
            {
                return Result<UserResponse>.Failure($"Failed to upload avatar: {ex.Message}");
            }
        }

        private Result<Guid> GetCurrentUserIdResult()
        {
            var userId = _currentUserService.GetUserIdOrNull();

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Result<Guid>.Unauthorized("User not authenticated.");
            }

            return Guid.TryParse(userId, out var parsedUserId)
                ? Result<Guid>.Success(parsedUserId)
                : Result<Guid>.Unauthorized("UserId claim invalid.");
        }
    }
}
