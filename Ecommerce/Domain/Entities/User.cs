using Domain.Common;
using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Domain.Entities
{
    public class User : BaseEntity
    {
        public string FullName { get; set; } = string.Empty;
        public string? Username { get; set; }
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string PasswordHash { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public UserRole Role { get; set; }
        public UserStatus Status { get; set; }
        public bool EmailConfirmed { get; set; } = false;

        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
        public virtual ICollection<UserAddress> UserAddresses { get; set; } = new List<UserAddress>();
        public virtual ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
        public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
        public virtual ICollection<WishlistItem> WishlistItems { get; set; } = new List<WishlistItem>();
        public virtual ICollection<RecentlyViewedProduct> RecentlyViewedProducts { get; set; } = new List<RecentlyViewedProduct>();
        public virtual LoyaltyAccount? LoyaltyAccount { get; set; }

        public static User Create(string fullName, string email, string passwordHash)
        {
            return new User
            {
                FullName = NormalizeRequired(fullName),
                Email = NormalizeEmail(email),
                PasswordHash = passwordHash,
                Role = UserRole.User,
                Status = UserStatus.Active,
                EmailConfirmed = false
            };
        }

        public void UpdateProfile(
            string fullName,
            string? avatarUrl,
            string? phoneNumber,
            DateTime? dateOfBirth)
        {
            FullName = NormalizeRequired(fullName);
            AvatarUrl = NormalizeOptional(avatarUrl);
            PhoneNumber = NormalizeOptional(phoneNumber);
            DateOfBirth = NormalizeDate(dateOfBirth);
        }

        public void UpsertDefaultAddress(
            string receiverName,
            string phoneNumber,
            string addressLine,
            string? ward,
            string? district,
            string? province)
        {
            var defaultAddress = UserAddresses.FirstOrDefault(address => address.IsDefault);

            if (defaultAddress is null)
            {
                defaultAddress = UserAddress.Create(Id, receiverName, phoneNumber, addressLine, ward, district, province, true);
                UserAddresses.Add(defaultAddress);
                return;
            }

            defaultAddress.UpdateDetails(receiverName, phoneNumber, addressLine, ward, district, province, true);
        }

        public static string NormalizeEmail(string email)
        {
            return NormalizeRequired(email).ToLowerInvariant();
        }

        public UserAddress AddAddress(
            string receiverName,
            string phoneNumber,
            string addressLine,
            string? ward,
            string? district,
            string? province,
            bool isDefault)
        {
            var hasActiveAddress = UserAddresses.Any(address => !address.IsDeleted);
            var shouldSetDefault = isDefault || !hasActiveAddress;

            if (shouldSetDefault)
            {
                ClearDefaultAddresses();
            }

            var address = UserAddress.Create(
                Id,
                receiverName,
                phoneNumber,
                addressLine,
                ward,
                district,
                province,
                shouldSetDefault);

            UserAddresses.Add(address);
            return address;
        }

        public UserAddress? UpdateAddress(
            Guid addressId,
            string receiverName,
            string phoneNumber,
            string addressLine,
            string? ward,
            string? district,
            string? province,
            bool isDefault)
        {
            var address = UserAddresses.FirstOrDefault(item => item.Id == addressId && !item.IsDeleted);
            if (address is null)
            {
                return null;
            }

            if (isDefault)
            {
                ClearDefaultAddresses();
            }

            address.UpdateDetails(receiverName, phoneNumber, addressLine, ward, district, province, isDefault);

            if (!isDefault && address.IsDefault == false)
            {
                var hasOtherActiveAddresses = UserAddresses.Any(item => !item.IsDeleted && item.Id != address.Id);

                if (hasOtherActiveAddresses)
                {
                    EnsureSingleDefaultAddress(excludedAddressId: address.Id);
                }
                else
                {
                    address.SetDefault(true);
                }
            }

            return address;
        }

        public UserAddress? DeleteAddress(Guid addressId, string deletedBy)
        {
            var address = UserAddresses.FirstOrDefault(item => item.Id == addressId && !item.IsDeleted);
            if (address is null)
            {
                return null;
            }

            var wasDefault = address.IsDefault;
            address.MarkDeleted(deletedBy);

            if (wasDefault)
            {
                EnsureSingleDefaultAddress(excludedAddressId: addressId);
            }

            return address;
        }

        private void ClearDefaultAddresses()
        {
            foreach (var address in UserAddresses.Where(item => !item.IsDeleted))
            {
                address.SetDefault(false);
            }
        }

        private void EnsureSingleDefaultAddress(Guid? excludedAddressId = null)
        {
            var activeAddresses = UserAddresses
                .Where(item => !item.IsDeleted && item.Id != excludedAddressId)
                .OrderByDescending(item => item.CreatedAt)
                .ToList();

            if (activeAddresses.Count == 0)
            {
                return;
            }

            var currentDefault = activeAddresses.FirstOrDefault(item => item.IsDefault);
            if (currentDefault != null)
            {
                return;
            }

            activeAddresses[0].SetDefault(true);
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

        private static DateTime? NormalizeDate(DateTime? value)
        {
            if (!value.HasValue)
            {
                return null;
            }

            return DateTime.SpecifyKind(value.Value.Date, DateTimeKind.Utc);
        }
    }
}
