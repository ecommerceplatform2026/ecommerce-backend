using System.ComponentModel.DataAnnotations;

namespace Application.Common.Validations
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class DateNotInFutureAttribute : ValidationAttribute
    {
        public DateNotInFutureAttribute()
        {
            ErrorMessage = "Date of birth cannot be in the future.";
        }

        public override bool IsValid(object? value)
        {
            if (value is null)
            {
                return true;
            }

            if (value is DateTime dateTimeValue)
            {
                return dateTimeValue.Date <= DateTime.UtcNow.Date;
            }

            return false;
        }
    }
}
