using BudgetManager.Domain;
using BudgetManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BudgetManager.Infrastructure.Persistence.Configuration;

public class FundConfiguration : IEntityTypeConfiguration<Fund>
{
    public void Configure(EntityTypeBuilder<Fund> builder)
    {
        builder.ToTable("Funds");

        builder.ConfigureEntity();

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(Constants.MaxNameLength);

        builder.Property(x => x.Description)
            .HasMaxLength(Constants.MaxCommentLength);

        builder.HasIndex(x => x.BudgetId);

        builder.HasMany(x => x.Transactions)
            .WithOne(x => x.Fund)
            .HasForeignKey(x => x.FundId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
