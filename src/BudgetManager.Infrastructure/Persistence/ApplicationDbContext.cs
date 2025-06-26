using BudgetManager.Domain.Entities;
using BudgetManager.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace BudgetManager.Infrastructure.Persistence;

public class ApplicationDbContext(DbContextOptions<DbContext> options) : DbContext(options)
{
  public DbSet<Expense> Expenses { get; set; }
  public DbSet<Fund> Funds { get; set; }
  public DbSet<Allocation> Allocations { get; set; }

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    base.OnModelCreating(modelBuilder);

    modelBuilder.ApplyConfiguration(new ExpenseConfiguration());
  }
}
