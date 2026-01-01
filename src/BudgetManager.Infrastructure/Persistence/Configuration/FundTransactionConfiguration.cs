using BudgetManager.Domain;
using BudgetManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BudgetManager.Infrastructure.Persistence.Configuration;

public class FundTransactionConfiguration : IEntityTypeConfiguration<FundTransaction>
{
    public void Configure(EntityTypeBuilder<FundTransaction> builder)
    {
        builder.ToTable("FundTransactions");

        builder.ConfigureEntity();

        builder.OwnsOne(x => x.Value, money =>
        {
            money.Property(m => m.Amount)
               .HasColumnName("Amount")
               .HasPrecision(18, 2)
               .IsRequired();
            money.Property(m => m.Currency)
               .HasColumnName("Currency")
               .HasMaxLength(3)
               .IsRequired();
        });

        builder.Property(x => x.Date)
            .IsRequired();

        builder.Property(x => x.Title)
            .HasMaxLength(Constants.MaxTitleLength);

        builder.Property(x => x.Comment)
            .HasMaxLength(Constants.MaxCommentLength);

        builder.HasOne(x => x.Fund)
            .WithMany(x => x.Transactions)
            .HasForeignKey(x => x.FundId);
    }
}
