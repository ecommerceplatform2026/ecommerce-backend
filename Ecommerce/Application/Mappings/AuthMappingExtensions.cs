using Application.DTOs.Auth;
using Domain.Entities;

namespace Application.Mappings
{
    public static class AuthMappingExtensions
    {
        public static AuthResponse ToAuthResponse(this User user, string token, string refreshToken = "")
        {
            return new AuthResponse
            {
                Token = token,
                FullName = user.FullName,
                Email = user.Email,
                Role = user.Role.ToString(),
                RefreshToken = refreshToken
            };
        }
    }
}
