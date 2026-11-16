using BudgetManager.Domain;
using BudgetManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BudgetManager.Infrastructure.Persistence.Configuration;

public class LedgerAllocationTemplateLineConfiguration : IEntityTypeConfiguration<LedgerAllocationTemplateLine>
{
    public void Configure(EntityTypeBuilder<LedgerAllocationTemplateLine> builder)
    {
        builder.ToTable("LedgerAllocationTemplateLines");

        builder.ConfigureEntity();

        builder.Property(x => x.Amount)
            .HasPrecision(Constants.MoneyPrecision, Constants.MoneyDecimalPlaces);

        builder.Property(x => x.Percent)
            .HasPrecision(Constants.PercentPrecision, Constants.PercentDecimalPlaces);

        builder.HasOne(x => x.Budget)
            .WithMany()
            .HasForeignKey(x => x.BudgetId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
