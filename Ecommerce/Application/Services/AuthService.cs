using Application.Common.Response;
using Application.DTOs.Auth;
using Application.Interfaces.Repositories.Base;
using Application.Interfaces.Security;
using Application.Interfaces.Services;
using Application.Mappings;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IJwtService _jwtService;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IUniqueConstraintChecker _uniqueConstraintChecker;

        public AuthService(
            IUnitOfWork unitOfWork,
            IJwtService jwtService,
            IPasswordHasher passwordHasher,
            IUniqueConstraintChecker uniqueConstraintChecker)
        {
            _unitOfWork = unitOfWork;
            _jwtService = jwtService;
            _passwordHasher = passwordHasher;
            _uniqueConstraintChecker = uniqueConstraintChecker;
        }

        public async Task<Result<AuthResponse>> RegisterAsync(
            RegisterRequest request,
            CancellationToken cancellationToken = default)
        {
            var userRepository = _unitOfWork.GetRepository<User>();

            var normalizedEmail = request.Email.Trim().ToLowerInvariant();
            var existingUser = await userRepository.FindAsync(
                u => u.Email == normalizedEmail,
                true,
                cancellationToken);

            if (existingUser != null)
            {
                return Result<AuthResponse>.Conflict("Email is already registered.");
            }

            var passwordHash = _passwordHasher.Hash(request.Password);
            var user = User.Create(request.FullName.Trim(), normalizedEmail, passwordHash);

            await userRepository.AddAsync(user, cancellationToken);

            try
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException ex) when (_uniqueConstraintChecker.IsUniqueViolation(ex, "IX_Users_Email"))
            {
                return Result<AuthResponse>.Conflict("Email is already registered.");
            }

            return Result<AuthResponse>.Success(CreateAuthResponse(user));
        }

        public async Task<Result<AuthResponse>> LoginAsync(
            LoginRequest request,
            CancellationToken cancellationToken = default)
        {
            var userRepository = _unitOfWork.GetRepository<User>();
            var normalizedEmail = request.Email.Trim().ToLowerInvariant();

            var user = await userRepository.FindAsync(
                u => u.Email == normalizedEmail,
                false,
                cancellationToken);

            if (user == null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
            {
                return Result<AuthResponse>.Unauthorized("Invalid email or password.");
            }

            if (user.Status != UserStatus.Active)
            {
                return Result<AuthResponse>.Forbidden("Your account is not active.");
            }

            return Result<AuthResponse>.Success(CreateAuthResponse(user));
        }

        private AuthResponse CreateAuthResponse(User user)
        {
            var token = _jwtService.GenerateToken(user);
            return user.ToAuthResponse(token);
        }
    }
}
