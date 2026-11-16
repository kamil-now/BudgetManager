using BudgetManager.Domain;
using BudgetManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BudgetManager.Infrastructure.Persistence.Configuration;

public class FundTransactionConfiguration : IEntityTypeConfiguration<FundTransaction>
{
    public void Configure(EntityTypeBuilder<FundTransaction> builder)
    {
        // a row comes either from an account transaction (income allocation, expense payment) or from an allocation operation
        builder.ToTable("FundTransactions", table => table.HasCheckConstraint(
            "CK_FundTransactions_Source",
            "num_nonnulls(\"AccountTransactionId\", \"AllocationId\") = 1"));

        builder.ConfigureEntity();

        builder.OwnsOne(x => x.Value, money =>
        {
            money.Property(m => m.Amount)
               .HasColumnName("Amount")
               .HasPrecision(Constants.MoneyPrecision, Constants.MoneyDecimalPlaces)
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

        builder.HasOne(x => x.Fund)
            .WithMany(x => x.Transactions)
            .HasForeignKey(x => x.FundId);

        builder.HasOne(x => x.AccountTransaction)
            .WithMany()
            .HasForeignKey(x => x.AccountTransactionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.AccountTransactionId);

        builder.HasIndex(x => x.AllocationId);

        // also serves the foreign key, which then gets no index of its own
        builder.HasIndex(x => new { x.FundId, x.Date });
    }
}
