using Application.Common.Response;
using System.Security.Claims;

namespace Presentation.Common.Extensions
{
    public static class ClaimsPrincipalExtensions
    {
        public static Result<Guid> GetUserIdOrResult(this ClaimsPrincipal user)
        {
            if (user == null)
                return Result<Guid>.Failure("Unauthorized: User not authenticated.");

            var userIdStr = user.FindFirstValue("sub")
                            ?? user.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userIdStr))
                return Result<Guid>.Failure("Unauthorized: UserId claim not found.");

            if (!Guid.TryParse(userIdStr, out var userId))
                return Result<Guid>.Failure("Unauthorized: UserId claim invalid.");

            return Result<Guid>.Success(userId);
        }
    }
}
