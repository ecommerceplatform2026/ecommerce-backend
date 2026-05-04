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

        public static UserAddress Create(
            Guid userId,
            string receiverName,
            string phoneNumber,
            string addressLine,
            string? ward,
            string? district,
            string? province,
            bool isDefault)
        {
            return new UserAddress
            {
                UserId = userId,
                ReceiverName = NormalizeRequired(receiverName),
                PhoneNumber = NormalizeRequired(phoneNumber),
                AddressLine = NormalizeRequired(addressLine),
                Ward = NormalizeOptional(ward),
                District = NormalizeOptional(district),
                Province = NormalizeOptional(province),
                IsDefault = isDefault
            };
        }

        public void UpdateDetails(
            string receiverName,
            string phoneNumber,
            string addressLine,
            string? ward,
            string? district,
            string? province,
            bool isDefault)
        {
            ReceiverName = NormalizeRequired(receiverName);
            PhoneNumber = NormalizeRequired(phoneNumber);
            AddressLine = NormalizeRequired(addressLine);
            Ward = NormalizeOptional(ward);
            District = NormalizeOptional(district);
            Province = NormalizeOptional(province);
            IsDefault = isDefault;
        }

        public void SetDefault(bool isDefault)
        {
            IsDefault = isDefault;
        }

        public void MarkDeleted(string deletedBy)
        {
            SetDeleted(deletedBy);
        }

        private static string NormalizeRequired(string value)
        {
            var normalizedValue = value.Trim();

            if (string.IsNullOrWhiteSpace(normalizedValue))
            {
                throw new ArgumentException("Value cannot be empty or whitespace.", nameof(value));
            }

            return normalizedValue;
        }

        private static string? NormalizeOptional(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
