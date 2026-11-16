using BudgetManager.Domain;
using BudgetManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BudgetManager.Infrastructure.Persistence.Configuration;

public class AllocationConfiguration : IEntityTypeConfiguration<Allocation>
{
    public void Configure(EntityTypeBuilder<Allocation> builder)
    {
        builder.ToTable("Allocations");

        builder.ConfigureEntity();

        builder.Property(x => x.Currency)
            .HasMaxLength(Constants.CurrencyCodeLength)
            .IsRequired();

        builder.Property(x => x.Date)
            .IsRequired();

        builder.Property(x => x.IsInitial)
            .IsRequired();

        builder.HasOne(x => x.Ledger)
            .WithMany()
            .HasForeignKey(x => x.LedgerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Transactions)
            .WithOne(x => x.Allocation)
            .HasForeignKey(x => x.AllocationId)
            .OnDelete(DeleteBehavior.Cascade);

        // also serves the foreign key, which then gets no index of its own
        builder.HasIndex(x => new { x.LedgerId, x.Date });
    }
}
