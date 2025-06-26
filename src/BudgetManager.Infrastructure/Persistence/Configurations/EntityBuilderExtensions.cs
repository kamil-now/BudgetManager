using BudgetManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BudgetManager.Infrastructure.Persistence.Configurations;

public static class EntityBuilderExtensions
{
  public static void ConfigureTransactionEntity<T>(this EntityTypeBuilder<T> builder) where T : Transaction
  {
    builder.ConfigureEntity();

    builder.Property(t => t.LedgerId)
        .IsRequired();

    builder.Property(t => t.AccountId)
        .IsRequired();

    builder.OwnsOne(t => t.Amount, money =>
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

    builder.Property(t => t.Date)
        .IsRequired();

    builder.Property(t => t.Title)
        .IsRequired()
        .HasMaxLength(Constants.MaxTitleLength);

    builder.Property(t => t.Description)
        .HasMaxLength(Constants.MaxDescriptionLength);

    builder.Property(t => t.Tags)
        .HasConversion(
            tags => tags == null ? null : string.Join(',', tags),
            value => value == null ? null : value.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList())
        .HasMaxLength(Constants.MaxTagsLength);
  }
  public static void ConfigureEntity<T>(this EntityTypeBuilder<T> builder) where T : Entity
  {
    builder.HasKey(x => x.Id);
    builder.Property(e => e.CreatedAt).HasDefaultValueSql("SYSDATETIMEOFFSET()");
  }
}