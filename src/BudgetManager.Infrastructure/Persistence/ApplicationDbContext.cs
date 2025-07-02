using BudgetManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BudgetManager.Infrastructure.Persistence;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
  public DbSet<User> Users { get; set; }
  public DbSet<Ledger> Ledgers { get; set; }
  public DbSet<Budget> Budgets { get; set; }
  public DbSet<Fund> Funds { get; set; }
  public DbSet<Income> Incomes { get; set; }
  public DbSet<Expense> Expenses { get; set; }
  public DbSet<Transfer> Transfers { get; set; }
  public DbSet<Allocation> Allocations { get; set; }
  public DbSet<Reallocation> Reallocations { get; set; }
  public DbSet<Deallocation> Deallocations { get; set; }

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    base.OnModelCreating(modelBuilder);

    modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
  }

  public override int SaveChanges()
  {
    foreach (var entry in ChangeTracker.Entries<Entity>())
    {
      switch (entry.State)
      {
        case EntityState.Added:
          entry.Entity.CreatedAt = DateTimeOffset.UtcNow;
          break;

        case EntityState.Modified:
          entry.Entity.UpdatedAt = DateTimeOffset.UtcNow;
          break;
      }
    }

    return base.SaveChanges();
  }
}
