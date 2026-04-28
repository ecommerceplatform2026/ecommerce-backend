using Domain.Common;
using System;

namespace Domain.Entities
{
    public class UserAddress : BaseEntity
    {
        public Guid UserId { get; set; }
        public string ReceiverName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string AddressLine { get; set; } = string.Empty;
        public string? Ward { get; set; }
        public string? District { get; set; }
        public string? Province { get; set; }
        public bool IsDefault { get; set; }

        public User? User { get; set; }
    }
}
