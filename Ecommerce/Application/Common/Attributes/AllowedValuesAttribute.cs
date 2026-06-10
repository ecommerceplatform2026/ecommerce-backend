using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Application.Common.Attributes
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public sealed class AllowedValuesAttribute : ValidationAttribute
    {
        public HashSet<string> Values { get; }

        public AllowedValuesAttribute(params string[] values)
        {
            Values = values.ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        public override bool IsValid(object? value)
        {
            return value is string s && Values.Contains(s);
        }

        public override string FormatErrorMessage(string name)
        {
            return $"The field {name} can only be: {string.Join(", ", Values)}.";
        }
    }
}
