using BudgetManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BudgetManager.Infrastructure.Persistence.Configuration;

public class DeallocationConfiguration : IEntityTypeConfiguration<Deallocation>
{
    public void Configure(EntityTypeBuilder<Deallocation> builder)
    {
        builder.ToTable("Deallocations");

        builder.ConfigureBudgetTransactionEntity();

        builder.HasOne(x => x.Fund)
            .WithMany(x => x.Deallocations)
            .HasForeignKey(x => x.FundId);
    }
}
