using System;

namespace Domain.Common
{
    public record Money
    {
        public long Amount { get; }
        public string Currency { get; }

        private Money()
        {
            Amount = 0;
            Currency = "VND";
        }

        public Money(long amount, string currency = "VND")
        {
            if (amount < 0)
                throw new ArgumentException("Amount must be non-negative.", nameof(amount));
            if (string.IsNullOrWhiteSpace(currency))
                throw new ArgumentException("Currency cannot be empty.", nameof(currency));

            Amount = amount;
            Currency = currency.Trim().ToUpperInvariant();
        }

        public static Money Zero(string currency = "VND") => new(0, currency);

        public static Money operator +(Money left, Money right)
        {
            if (left.Currency != right.Currency)
                throw new InvalidOperationException($"Cannot add money of different currencies: {left.Currency} and {right.Currency}");

            return new Money(left.Amount + right.Amount, left.Currency);
        }

        public static Money operator *(Money money, long multiplier)
        {
            return new Money(money.Amount * multiplier, money.Currency);
        }

        public static Money operator *(long multiplier, Money money) => money * multiplier;
    }
}
