namespace BudgetManager.Domain.Models;

public abstract class MoneyOperation
{
  public string Id { get; }
  public DateTime CreatedDate { get; }
  public string Title { get; private set; }
  public Money Value { get; private set; }
  public DateOnly Date { get; private set; }
  public string Description { get; private set; }

  public MoneyOperation(
    string id,
    string title,
    Money value,
    DateOnly date,
    string description
    )
  {
    Id = id;
    Title = title;
    Value = value;
    Date = date;
    Description = description;

    CreatedDate = DateTime.Now;
  }

  protected void Update(
    string? title,
    Money? value,
    DateOnly? date,
    string? description)
  {
    if (title is not null)
    {
      Title = title;
    }

    if (value is not null)
    {
      Value = (Money)value;
    }

    if (date is not null)
    {
      Date = (DateOnly)date;
    }

    if (description is not null)
    {
      Description = description;
    }
  }
}
