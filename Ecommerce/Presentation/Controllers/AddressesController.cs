using Application.Common.Response;
using Application.DTOs.User.Address;
using Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Common.Extensions;

namespace Presentation.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/addresses")]
    public class AddressesController : ControllerBase
    {
        private readonly IAddressService _addressService;

        public AddressesController(IAddressService addressService)
        {
            _addressService = addressService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAddresses(CancellationToken cancellationToken)
        {
            var result = await _addressService.GetAddressesAsync(cancellationToken);
            return this.FromResult(result);
        }

        [HttpGet("{addressId:guid}")]
        public async Task<IActionResult> GetAddressById(Guid addressId, CancellationToken cancellationToken)
        {
            var result = await _addressService.GetAddressByIdAsync(addressId, cancellationToken);
            return this.FromResult(result);
        }

        [HttpPost]
        public async Task<IActionResult> CreateAddress([FromBody] CreateAddressRequest request, CancellationToken cancellationToken)
        {
            var result = await _addressService.CreateAddressAsync(request, cancellationToken);
            return this.FromResult(result);
        }

        [HttpPut("{addressId:guid}")]
        public async Task<IActionResult> UpdateAddress(Guid addressId, [FromBody] UpdateAddressRequest request, CancellationToken cancellationToken)
        {
            var result = await _addressService.UpdateAddressAsync(addressId, request, cancellationToken);
            return this.FromResult(result);
        }

        [HttpDelete("{addressId:guid}")]
        public async Task<IActionResult> DeleteAddress(Guid addressId, CancellationToken cancellationToken)
        {
            var result = await _addressService.DeleteAddressAsync(addressId, cancellationToken);
            return this.FromResult(result);
        }
    }
}
