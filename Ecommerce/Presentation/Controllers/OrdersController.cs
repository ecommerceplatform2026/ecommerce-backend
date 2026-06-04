using Application.DTOs.Order;
using Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Common.Extensions;

namespace Presentation.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/orders")]
    public sealed class OrdersController : ControllerBase
    {
        private readonly IOrderService _orderService;

        public OrdersController(IOrderService orderService)
        {
            _orderService = orderService ?? throw new ArgumentNullException(nameof(orderService));
        }

        [HttpGet]
        public async Task<IActionResult> GetMyOrders([FromQuery] GetOrdersRequest request, CancellationToken cancellationToken)
        {
            var result = await _orderService.GetMyOrdersAsync(request, cancellationToken);
            return this.FromResult(result);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetOrderById(Guid id, CancellationToken cancellationToken)
        {
            var result = await _orderService.GetOrderByIdAsync(id, cancellationToken);
            return this.FromResult(result);
        }
    }
}
