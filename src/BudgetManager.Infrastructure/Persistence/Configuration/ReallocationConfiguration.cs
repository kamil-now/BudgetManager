using BudgetManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BudgetManager.Infrastructure.Persistence.Configuration;

public class ReallocationConfiguration : IEntityTypeConfiguration<Reallocation>
{
    public void Configure(EntityTypeBuilder<Reallocation> builder)
    {
        builder.ToTable("Reallocations");

        builder.ConfigureBudgetTransactionEntity();

        builder.HasOne(x => x.Fund)
            .WithMany(x => x.OutgoingReallocations)
            .HasForeignKey(x => x.FundId);

        builder.HasOne(x => x.TargetFund)
            .WithMany(x => x.IncomingReallocations)
            .HasForeignKey(x => x.TargetFundId);
    }
}
