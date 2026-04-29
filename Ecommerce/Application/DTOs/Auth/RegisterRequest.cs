using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Auth
{
    public class RegisterRequest
    {
        [Required]
        [MaxLength(150)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MinLength(6)]
        public string Password { get; set; } = string.Empty;
    }
}
