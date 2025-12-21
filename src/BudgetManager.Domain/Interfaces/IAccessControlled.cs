namespace BudgetManager.Domain.Interfaces;

public interface IAccessControlled
{
  public Guid Id { get; set; }
  public Guid OwnerId { get; set; }
}
