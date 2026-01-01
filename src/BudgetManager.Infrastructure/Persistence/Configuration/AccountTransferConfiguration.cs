using BudgetManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BudgetManager.Infrastructure.Persistence.Configuration;

public class AccountTransferConfiguration : IEntityTypeConfiguration<AccountTransfer>
{
    public void Configure(EntityTypeBuilder<AccountTransfer> builder)
    {
        builder.ToTable("AccountTransfers");

        builder.ConfigureEntity();

        builder.HasOne(x => x.Income)
            .WithOne(x => x.InTransfer)
            .HasForeignKey<AccountTransfer>(x => x.IncomeId);

        builder.HasOne(x => x.Expense)
            .WithOne(x => x.OutTransfer)
            .HasForeignKey<AccountTransfer>(x => x.ExpenseId);
    }
}
