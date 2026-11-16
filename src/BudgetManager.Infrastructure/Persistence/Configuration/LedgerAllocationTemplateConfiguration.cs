using BudgetManager.Domain;
using BudgetManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BudgetManager.Infrastructure.Persistence.Configuration;

public class LedgerAllocationTemplateConfiguration : IEntityTypeConfiguration<LedgerAllocationTemplate>
{
    public void Configure(EntityTypeBuilder<LedgerAllocationTemplate> builder)
    {
        builder.ToTable("LedgerAllocationTemplates");

        builder.ConfigureEntity();

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(Constants.MaxNameLength);

        builder.Property(x => x.Currency)
            .IsRequired()
            .HasMaxLength(Constants.CurrencyCodeLength);

        builder.HasMany(x => x.Lines)
            .WithOne(x => x.LedgerAllocationTemplate)
            .HasForeignKey(x => x.LedgerAllocationTemplateId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.LedgerId, x.Name })
            .IsUnique();
    }
}
