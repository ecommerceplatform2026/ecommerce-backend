using Application.Common.Enum;
using System.Text.Json.Serialization;

namespace Application.Common.Response
{
    public class Result<T>
    {
        public bool IsSuccess { get; }

        public T? Value { get; }

        public List<string> Errors { get; }

        public ErrorType ErrorType { get; }

        private Result(bool isSuccess, T? value, List<string> errors, ErrorType errorType)
        {
            IsSuccess = isSuccess;
            Value = value;
            Errors = errors;
            ErrorType = errorType;
        }

        public static Result<T> Success(T value)
            => new(true, value, new List<string>(), ErrorType.None);

        public static Result<T> Failure(string error)
            => new(false, default, new List<string> { error }, ErrorType.Validation);

        public static Result<T> Failure(List<string> errors)
            => new(false, default, errors, ErrorType.Validation);

        public static Result<T> Unauthorized(string error)
            => new(false, default, new List<string> { error }, ErrorType.Unauthorized);

        public static Result<T> Forbidden(string error)
            => new(false, default, new List<string> { error }, ErrorType.Forbidden);

        public static Result<T> NotFound(string error)
            => new(false, default, new List<string> { error }, ErrorType.NotFound);

        public static Result<T> Conflict(string error)
            => new(false, default, new List<string> { error }, ErrorType.Conflict);
    }
}