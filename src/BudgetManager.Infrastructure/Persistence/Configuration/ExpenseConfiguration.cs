using BudgetManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BudgetManager.Infrastructure.Persistence.Configuration;

public class ExpenseConfiguration : IEntityTypeConfiguration<Expense>
{
    public void Configure(EntityTypeBuilder<Expense> builder)
    {
        builder.ToTable("Expenses");

        builder.ConfigureAccountTransactionEntity();

        builder.HasOne(x => x.Account)
            .WithMany(x => x.Expenses)
            .HasForeignKey(x => x.AccountId);
    }
}
