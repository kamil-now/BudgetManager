using BudgetManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BudgetManager.Infrastructure.Persistence.Configuration;

public class TransferConfiguration : IEntityTypeConfiguration<Transfer>
{
    public void Configure(EntityTypeBuilder<Transfer> builder)
    {
        builder.ToTable("Transfers");

        builder.ConfigureAccountTransactionEntity();

        builder.HasOne(x => x.Account)
            .WithMany(x => x.OutgoingTransfers)
            .HasForeignKey(x => x.AccountId);

        builder.HasOne(x => x.TargetAccount)
            .WithMany(x => x.IncomingTransfers)
            .HasForeignKey(x => x.TargetAccountId);
    }
}
