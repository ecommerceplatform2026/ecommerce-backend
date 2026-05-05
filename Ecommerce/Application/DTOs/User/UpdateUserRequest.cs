using Application.Common.Validations;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.User
{
    public class UpdateUserRequest
    {
        [Required]
        [NotWhiteSpace]
        [MaxLength(150)]
        public string FullName { get; set; } = string.Empty;

        [Url]
        [MaxLength(500)]
        public string? Avatar { get; set; }

        [Phone]
        [MaxLength(20)]
        public string? PhoneNumber { get; set; }

        [DateNotInFuture]
        public DateTime? DateOfBirth { get; set; }
    }
}
