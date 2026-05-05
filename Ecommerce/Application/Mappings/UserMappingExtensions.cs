using Application.DTOs.User;
using Domain.Entities;
using System.Linq;

namespace Application.Mappings
{
    public static class UserMappingExtensions
    {
        public static UserResponse ToUserResponse(this User user)
        {
            return new UserResponse
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                Avatar = user.AvatarUrl,
                PhoneNumber = user.PhoneNumber,
                DateOfBirth = user.DateOfBirth,
                Address = user.UserAddresses
                    .FirstOrDefault(address => address.IsDefault)
                    ?.ToProfileAddressResponse()
            };
        }
    }
}
