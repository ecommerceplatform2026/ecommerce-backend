using API.Common.Responses;
using System.Text.Json;

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
            _logger.LogError(ex, ex.Message);

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;

            var response = new ApiResponse<object>
            {
                Success = false,
                Data = null,
                Errors = new List<string> { "Internal server error" } 
            };

            var json = JsonSerializer.Serialize(response);

            await context.Response.WriteAsync(json);
        }
    }
}