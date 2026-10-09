using BudgetManager.Domain;
using BudgetManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BudgetManager.Infrastructure.Persistence.Configuration;

public class AllocationTemplateConfiguration : IEntityTypeConfiguration<AllocationTemplate>
{
    public void Configure(EntityTypeBuilder<AllocationTemplate> builder)
    {
        builder.ToTable("AllocationTemplates");

        builder.ConfigureEntity();

        builder.Property(x => x.Currency)
            .IsRequired()
            .HasMaxLength(Constants.CurrencyCodeLength);

        builder.HasMany(x => x.Lines)
            .WithOne(x => x.AllocationTemplate)
            .HasForeignKey(x => x.AllocationTemplateId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.BudgetId, x.Currency })
            .IsUnique();
    }
}
