using BudgetManager.Domain;
using BudgetManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BudgetManager.Infrastructure.Persistence.Configuration;

public class FixedAllocationTemplateLineConfiguration : IEntityTypeConfiguration<FixedAllocationTemplateLine>
{
    public void Configure(EntityTypeBuilder<FixedAllocationTemplateLine> builder)
    {
        builder.Property(x => x.Amount)
            .HasPrecision(Constants.MoneyPrecision, Constants.MoneyDecimalPlaces);
    }
}
