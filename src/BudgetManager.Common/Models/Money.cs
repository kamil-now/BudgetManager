namespace BudgetManager.Common.Models;

public sealed record Money(decimal Amount, string Currency)
{  
  public static Money Zero(string currency) => new(0m, currency);

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

  public override string ToString() => $"{Amount} {Currency}";
}

