using Presentation.Common.Responses;
using Application.Common.Enum;
using Application.Common.Response;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Common.Extensions;

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

        return result.ErrorType switch
        {
            ErrorType.Unauthorized => controller.Unauthorized(response),
            ErrorType.Forbidden => controller.StatusCode(StatusCodes.Status403Forbidden, response),
            ErrorType.NotFound => controller.NotFound(response),
            ErrorType.Conflict => controller.Conflict(response),
            ErrorType.Validation => controller.BadRequest(response),
            _ => controller.BadRequest(response)
        };
    }
}