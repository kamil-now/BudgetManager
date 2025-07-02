using BudgetManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BudgetManager.Infrastructure.Persistence.Configuration;

public static class EntityBuilderExtensions
{
    public static void ConfigureTransactionEntity<T>(this EntityTypeBuilder<T> builder) where T : Transaction
    {
        builder.ConfigureEntity();

        builder.Property(x => x.LedgerId)
            .IsRequired();

        builder.Property(x => x.AccountId)
            .IsRequired();

        builder.OwnsOne(x => x.Amount, money =>
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
            .IsRequired()
            .HasMaxLength(Constants.MaxTitleLength);

        builder.Property(x => x.Description)
            .HasMaxLength(Constants.MaxDescriptionLength);

        builder.Property(x => x.Tags)
            .HasConversion(
                tags => tags == null ? null : string.Join(',', tags),
                value => value == null ? null : value.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList())
            .HasMaxLength(Constants.MaxTagsLength);

        builder.HasIndex(x => x.AccountId);
  }

    public static void ConfigureBudgetTransactionEntity<T>(this EntityTypeBuilder<T> builder) where T : BudgetTransaction
    {
        builder.ConfigureEntity();

        builder.Property(x => x.FundId)
            .IsRequired();

        builder.OwnsOne(x => x.Amount, money =>
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

        builder.Property(x => x.Description)
            .HasMaxLength(Constants.MaxDescriptionLength);
        
        builder.HasIndex(x => x.FundId);
  }

  public static void ConfigureEntity<T>(this EntityTypeBuilder<T> builder) where T : Entity
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedOnAdd()
            .IsRequired();
    }
}