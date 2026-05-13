using System.ComponentModel.DataAnnotations;

namespace Application.Common.Validations
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class NotEmptyGuidAttribute : ValidationAttribute
    {
        public NotEmptyGuidAttribute()
        {
            ErrorMessage = "The GUID field must not be empty.";
        }

        public override bool IsValid(object? value)
        {
            return value is Guid guid && guid != Guid.Empty;
        }
    }
}
