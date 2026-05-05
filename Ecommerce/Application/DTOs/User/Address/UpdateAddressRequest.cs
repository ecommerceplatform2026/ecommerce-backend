using Application.Common.Validations;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.User.Address
{
    public class UpdateAddressRequest
    {
        [Required]
        [NotWhiteSpace]
        [MaxLength(100)]
        public string ReceiverName { get; set; } = string.Empty;

        [Required]
        [NotWhiteSpace]
        [Phone]
        [MaxLength(20)]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required]
        [NotWhiteSpace]
        [MaxLength(255)]
        public string AddressLine { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? Ward { get; set; }

        [MaxLength(100)]
        public string? District { get; set; }

        [MaxLength(100)]
        public string? Province { get; set; }

        public bool IsDefault { get; set; }
    }
}
