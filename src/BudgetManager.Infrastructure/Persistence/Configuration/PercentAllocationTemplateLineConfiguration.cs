using BudgetManager.Domain;
using BudgetManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BudgetManager.Infrastructure.Persistence.Configuration;

public class PercentAllocationTemplateLineConfiguration : IEntityTypeConfiguration<PercentAllocationTemplateLine>
{
    public void Configure(EntityTypeBuilder<PercentAllocationTemplateLine> builder)
    {
        builder.Property(x => x.Percent)
            .HasPrecision(Constants.PercentPrecision, Constants.PercentDecimalPlaces);
    }
}
