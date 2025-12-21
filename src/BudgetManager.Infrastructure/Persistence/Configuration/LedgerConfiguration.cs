using BudgetManager.Domain;
using BudgetManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BudgetManager.Infrastructure.Persistence.Configuration;

public class LedgerConfiguration : IEntityTypeConfiguration<Ledger>
{
    public void Configure(EntityTypeBuilder<Ledger> builder)
    {
        builder.ToTable("Ledgers");

        builder.ConfigureEntity();

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(Constants.MaxNameLength);

        builder.Property(x => x.Description)
            .HasMaxLength(Constants.MaxCommentLength);

        builder.HasMany(x => x.Budgets)
            .WithOne(x => x.Ledger)
            .HasForeignKey(x => x.LedgerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Accounts)
            .WithOne(x => x.Ledger)
            .HasForeignKey(x => x.LedgerId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.OwnerId);
    }
}
