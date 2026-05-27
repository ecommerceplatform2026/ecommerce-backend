using System;

namespace Domain.Common
{
    public record Sku
    {
        public string Value { get; }

        public Sku(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("SKU value cannot be null, empty, or whitespace.", nameof(value));

            Value = value.Trim().ToUpperInvariant();
        }

        public override string ToString() => Value;
    }
}
