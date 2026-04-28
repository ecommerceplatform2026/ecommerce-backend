using API.Common.Responses;
using Application.Common.Response;
using Microsoft.AspNetCore.Mvc;

namespace API.Common.Extensions
{
    public static class ControllerExtensions
    {
        public static IActionResult FromResult<T>(this ControllerBase controller, Result<T> result)
        {
            var response = new ApiResponse<T>
            {
                Success = result.IsSuccess,
                Data = result.Value,
                Errors = result.Errors ?? new List<string>()
            };

            if (result.IsSuccess)
                return controller.Ok(response);

            if (result.Errors != null && result.Errors.Any(e => e.Contains("Unauthorized")))
                return controller.Unauthorized(response);

            return controller.BadRequest(response);
        }

    }
}