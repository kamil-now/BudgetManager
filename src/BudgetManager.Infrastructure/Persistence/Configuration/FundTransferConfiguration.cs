using BudgetManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BudgetManager.Infrastructure.Persistence.Configuration;

public class FundTransferConfiguration : IEntityTypeConfiguration<FundTransfer>
{
    public void Configure(EntityTypeBuilder<FundTransfer> builder)
    {
        builder.ToTable("FundTransfers");

        builder.ConfigureEntity();

        builder.HasOne(x => x.Allocation)
            .WithOne(x => x.InTransfer)
            .HasForeignKey<FundTransfer>(x => x.AllocationId);

        builder.HasOne(x => x.Deallocation)
            .WithOne(x => x.OutTransfer)
            .HasForeignKey<FundTransfer>(x => x.DeallocationId);
    }
}
