using BudgetManager.Domain.Entities;
using BudgetManager.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BudgetManager.Infrastructure.Persistence.Configuration;

public class AllocationTemplateLineConfiguration : IEntityTypeConfiguration<AllocationTemplateLine>
{
    public void Configure(EntityTypeBuilder<AllocationTemplateLine> builder)
    {
        builder.ToTable("AllocationTemplateLines");

        builder.ConfigureEntity();

        builder.HasDiscriminator<AllocationType>("Type")
            .HasValue<FixedAllocationTemplateLine>(AllocationType.Fixed)
            .HasValue<PercentAllocationTemplateLine>(AllocationType.Percent);

        builder.HasIndex(x => new { x.AllocationTemplateId, x.FundId })
            .IsUnique();
    }
}
