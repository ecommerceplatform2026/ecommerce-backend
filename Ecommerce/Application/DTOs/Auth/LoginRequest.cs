using Application.Common.Validations;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Auth
{
    public class LoginRequest
    {
        [Required]
        [NotWhiteSpace]
        [EmailAddress]
        [MaxLength(150)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [NotWhiteSpace]
        [MaxLength(100)]
        public string Password { get; set; } = string.Empty;
    }
}
