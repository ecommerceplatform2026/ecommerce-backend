using Application.Common.Response;
using Application.DTOs.User;
using Domain.Entities;

namespace Application.Interfaces.Services
{
    public interface IUserService
    {
        Task<Result<UserResponse>> GetUserAsync(CancellationToken cancellationToken = default);
        Task<Result<UserResponse>> UpdateUserAsync(UpdateUserRequest request, CancellationToken cancellationToken = default);
        Task<Result<User>> GetCurrentUserWithAddressesAsync(CancellationToken cancellationToken = default);
    }
}
