using BudgetManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BudgetManager.Infrastructure.Persistence.Configuration;

public class AllocationConfiguration : IEntityTypeConfiguration<Allocation>
{
    public void Configure(EntityTypeBuilder<Allocation> builder)
    {
        builder.ToTable("Allocations");

        builder.ConfigureBudgetTransactionEntity();

        builder.HasOne(x => x.Fund)
            .WithMany(x => x.Allocations)
            .HasForeignKey(x => x.FundId);
    }
}
