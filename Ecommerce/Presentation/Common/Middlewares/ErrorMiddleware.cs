using Microsoft.EntityFrameworkCore;
using Presentation.Common.Responses;
using System.Text.Json;

namespace Presentation.Common.Middlewares;

public class ErrorMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorMiddleware> _logger;

    public ErrorMiddleware(RequestDelegate next, ILogger<ErrorMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            context.Response.ContentType = "application/json";
            var errors = new List<string>();

            switch (ex)
            {
                case ArgumentException or InvalidOperationException:
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    errors.Add(ex.Message);
                    break;

                case KeyNotFoundException:
                    context.Response.StatusCode = StatusCodes.Status404NotFound;
                    errors.Add(ex.Message);
                    break;

                case UnauthorizedAccessException:
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    errors.Add("Unauthorized access.");
                    break;

                case DbUpdateConcurrencyException:
                    context.Response.StatusCode = StatusCodes.Status409Conflict;
                    errors.Add("A concurrency conflict occurred. Please try again.");
                    break;

                default:
                    _logger.LogError(ex, "An unhandled exception occurred: {Message}", ex.Message);
                    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                    errors.Add("Internal server error");
                    break;
            }

            var response = new ApiResponse<object>
            {
                Success = false,
                Data = null,
                Errors = errors
            };

            var json = JsonSerializer.Serialize(response);
            await context.Response.WriteAsync(json);
        }
    }
}