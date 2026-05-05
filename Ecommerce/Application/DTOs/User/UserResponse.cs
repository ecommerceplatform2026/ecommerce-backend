using Application.DTOs.User.Address;

namespace Application.DTOs.User
{
    public class UserResponse
    {
        public Guid Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Avatar { get; set; }
        public string? PhoneNumber { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public ProfileAddressResponse? Address { get; set; }
    }
}
