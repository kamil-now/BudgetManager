namespace BudgetManager.Common.Models;

public sealed record Money(decimal Amount, string Currency) : IEquatable<Money>
{
  public static Money Zero(string currency) => new(0m, currency);
  public bool IsPositive() => Amount > 0;

  public Money Add(Money other)
  {
    if (Currency != other.Currency)
      throw new InvalidOperationException("Cannot add amounts with different currencies.");
    return new Money(Amount + other.Amount, Currency);
  }

  public Money Subtract(Money other)
  {
    if (Currency != other.Currency)
      throw new InvalidOperationException("Cannot subtract amounts with different currencies.");
    return new Money(Amount - other.Amount, Currency);
  }

  public bool Equals(Money? other) =>
      other is not null &&
      Amount == other.Amount &&
      Currency == other.Currency;

  public override int GetHashCode() =>
      HashCode.Combine(Amount, Currency);

  public override string ToString() => $"{Amount} {Currency}";

  public static Money operator +(Money left, Money right) => left.Add(right);
  public static Money operator -(Money left, Money right) => left.Subtract(right);
  public static Money operator *(Money money, decimal factor)
  {
    return new Money(money.Amount * factor, money.Currency);
  }
  public static Money operator /(Money money, decimal divisor)
  {
    if (divisor == 0)
      throw new DivideByZeroException("Cannot divide by zero.");
    return new Money(money.Amount / divisor, money.Currency);
  }
  public static Money operator -(Money money)
  {
    return new Money(-money.Amount, money.Currency);
  }
}

