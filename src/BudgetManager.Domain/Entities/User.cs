namespace BudgetManager.Domain.Entities;

public class User : Entity
{
    public string? Name { get; set; }
    public required string Email { get; set; }
    public required string HashedPassword { get; set; }

    public virtual ICollection<Ledger> Ledgers { get; set; } = [];
}
