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
    }
}
