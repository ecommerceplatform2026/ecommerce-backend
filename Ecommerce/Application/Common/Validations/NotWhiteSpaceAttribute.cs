using System.ComponentModel.DataAnnotations;

namespace Application.Common.Validations
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class NotWhiteSpaceAttribute : ValidationAttribute
    {
        public NotWhiteSpaceAttribute()
        {
            ErrorMessage = "The field cannot be empty or whitespace.";
        }

        public override bool IsValid(object? value)
        {
            return value is null || value is not string stringValue || !string.IsNullOrWhiteSpace(stringValue);
        }
    }
}
