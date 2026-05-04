using Application.Common.Validations;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Auth
{
    public class RegisterRequest
    {
        [Required]
        [NotWhiteSpace]
        [MaxLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [NotWhiteSpace]
        [EmailAddress]
        [MaxLength(150)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [NotWhiteSpace]
        [MinLength(6)]
        [MaxLength(100)]
        public string Password { get; set; } = string.Empty;
    }
}
