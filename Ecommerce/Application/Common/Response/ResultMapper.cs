using Application.Common.Enum;
using Domain.Entities;

namespace Application.Common.Response
{
    public static class ResultMapper
    {
        public static Result<T> MapUserError<T>(Result<User> result)
        {
            return result.ErrorType == ErrorType.NotFound
                ? Result<T>.NotFound(result.Errors.First())
                : Result<T>.Unauthorized(result.Errors.First());
        }
    }
}
