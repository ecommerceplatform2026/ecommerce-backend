using Application.DTOs.User;
using Application.DTOs.User.Address;
using Domain.Entities;

namespace Application.Mappings
{
    public static class UserAddressMappingExtensions
    {
        public static ProfileAddressResponse ToProfileAddressResponse(this UserAddress address)
        {
            return new ProfileAddressResponse
            {
                ReceiverName = address.ReceiverName,
                PhoneNumber = address.PhoneNumber,
                AddressLine = address.AddressLine,
                Ward = address.Ward,
                District = address.District,
                Province = address.Province
            };
        }

        public static AddressResponse ToAddressResponse(this UserAddress address)
        {
            return new AddressResponse
            {
                Id = address.Id,
                ReceiverName = address.ReceiverName,
                PhoneNumber = address.PhoneNumber,
                AddressLine = address.AddressLine,
                Ward = address.Ward,
                District = address.District,
                Province = address.Province,
                IsDefault = address.IsDefault
            };
        }
    }
}
