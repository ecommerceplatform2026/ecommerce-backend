using Application.Common.Response;
using Application.DTOs.Auth;
using Application.Interfaces.Repositories.Base;
using Application.Interfaces.Security;
using Application.Interfaces.Services;
using Application.Mappings;
using Domain.Entities;
using Domain.Enums;
using System;

namespace Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IJwtService _jwtService;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IUniqueConstraintChecker _uniqueConstraintChecker;
        private readonly ICacheService _cacheService;

        public AuthService(
            IUnitOfWork unitOfWork,
            IJwtService jwtService,
            IPasswordHasher passwordHasher,
            IUniqueConstraintChecker uniqueConstraintChecker,
            ICacheService cacheService)
        {
            _unitOfWork = unitOfWork;
            _jwtService = jwtService;
            _passwordHasher = passwordHasher;
            _uniqueConstraintChecker = uniqueConstraintChecker;
            _cacheService = cacheService;
        }

        public async Task<Result<AuthResponse>> RegisterAsync(
            RegisterRequest request,
            CancellationToken cancellationToken = default)
        {
            var userRepository = _unitOfWork.GetRepository<User>();

            var normalizedEmail = User.NormalizeEmail(request.Email);
            var existingUser = await userRepository.FindAsync(
                u => u.Email == normalizedEmail,
                true,
                cancellationToken);

            if (existingUser != null)
            {
                return Result<AuthResponse>.Conflict("Email is already registered.");
            }

            var passwordHash = _passwordHasher.Hash(request.Password);
            var user = User.Create(request.FullName, normalizedEmail, passwordHash);

            await userRepository.AddAsync(user, cancellationToken);

            try
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex) when (_uniqueConstraintChecker.IsUniqueViolation(ex, "IX_Users_Email"))
            {
                return Result<AuthResponse>.Conflict("Email is already registered.");
            }

            return Result<AuthResponse>.Success(await CreateAuthResponseAsync(user, cancellationToken));
        }

        public async Task<Result<AuthResponse>> LoginAsync(
            LoginRequest request,
            CancellationToken cancellationToken = default)
        {
            var userRepository = _unitOfWork.GetRepository<User>();
            var normalizedEmail = User.NormalizeEmail(request.Email);

            var user = await userRepository.FindAsync(
                u => u.Email == normalizedEmail,
                true,
                cancellationToken);

            if (user == null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
            {
                return Result<AuthResponse>.Unauthorized("Invalid email or password.");
            }

            if (user.Status != UserStatus.Active)
            {
                return Result<AuthResponse>.Forbidden("Your account is not active.");
            }

            return Result<AuthResponse>.Success(await CreateAuthResponseAsync(user, cancellationToken));
        }

        public async Task<Result<AuthResponse>> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.RefreshToken))
            {
                return Result<AuthResponse>.Failure("Refresh token is required.");
            }

            var userIdStr = await _cacheService.GetAsync<string>($"refresh_token:{request.RefreshToken}", cancellationToken);
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            {
                return Result<AuthResponse>.Unauthorized("Invalid or expired refresh token.");
            }

            var userRepository = _unitOfWork.GetRepository<User>();
            var user = await userRepository.GetByIdAsync(userId, cancellationToken);
            if (user == null)
            {
                return Result<AuthResponse>.Unauthorized("User not found.");
            }

            if (user.Status != UserStatus.Active)
            {
                return Result<AuthResponse>.Forbidden("Your account is not active.");
            }

            // Invalidate old refresh token
            await _cacheService.RemoveAsync($"refresh_token:{request.RefreshToken}", cancellationToken);

            // Generate new token pair
            var authResponse = await CreateAuthResponseAsync(user, cancellationToken);
            return Result<AuthResponse>.Success(authResponse);
        }

        public async Task<Result<object>> LogoutAsync(LogoutRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.RefreshToken))
            {
                return Result<object>.Failure("Refresh token is required.");
            }

            await _cacheService.RemoveAsync($"refresh_token:{request.RefreshToken}", cancellationToken);
            return Result<object>.Success(new { Message = "Logged out successfully." });
        }

        private async Task<AuthResponse> CreateAuthResponseAsync(User user, CancellationToken cancellationToken)
        {
            var token = _jwtService.GenerateToken(user);
            var refreshToken = Guid.NewGuid().ToString("N");

            // Save refresh token to Redis for 7 days
            await _cacheService.SetAsync(
                $"refresh_token:{refreshToken}",
                user.Id.ToString(),
                TimeSpan.FromDays(7),
                cancellationToken);

            return user.ToAuthResponse(token, refreshToken);
        }
    }
}
