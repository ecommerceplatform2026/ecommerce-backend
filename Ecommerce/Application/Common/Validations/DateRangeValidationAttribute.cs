using System.ComponentModel.DataAnnotations;

namespace Application.Common.Validations
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class DateRangeValidationAttribute : ValidationAttribute
    {
        private readonly string _fromPropertyName;
        private readonly string _toPropertyName;

        public DateRangeValidationAttribute(string fromPropertyName, string toPropertyName)
        {
            _fromPropertyName = fromPropertyName;
            _toPropertyName = toPropertyName;
            ErrorMessage = "{0} cannot be after {1}.";
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            var fromProp = validationContext.ObjectType.GetProperty(_fromPropertyName);
            var toProp = validationContext.ObjectType.GetProperty(_toPropertyName);

            if (fromProp == null || toProp == null)
                return ValidationResult.Success;

            var fromValue = fromProp.GetValue(validationContext.ObjectInstance) as DateTime?;
            var toValue = toProp.GetValue(validationContext.ObjectInstance) as DateTime?;

            if (fromValue.HasValue && toValue.HasValue && fromValue.Value > toValue.Value)
            {
                return new ValidationResult(string.Format(ErrorMessage ?? "{0} cannot be after {1}.", _fromPropertyName, _toPropertyName));
            }

            return ValidationResult.Success;
        }
    }
}
