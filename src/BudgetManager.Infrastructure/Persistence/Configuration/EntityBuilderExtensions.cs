using System.Linq.Expressions;
using BudgetManager.Domain;
using BudgetManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BudgetManager.Infrastructure.Persistence.Configuration;

public static class EntityBuilderExtensions
{
    public static void ConfigureAccountTransactionEntity<T>(this EntityTypeBuilder<T> builder) where T : AccountTransaction
    {
        builder.ConfigureEntity();

        builder.OwnsOne(x => x.Amount, money =>
        {
            money.Property(m => m.Amount)
               .HasColumnName("Amount")
               .HasPrecision(18, 2)
               .IsRequired();
            money.Property(m => m.Currency)
               .HasColumnName("Currency")
               .HasMaxLength(Constants.CurrencyCodeLength)
               .IsRequired();
        });

        builder.Property(x => x.Date)
            .IsRequired();

        builder.Property(x => x.Title)
            .HasMaxLength(Constants.MaxTitleLength);

        builder.Property(x => x.Comment)
            .HasMaxLength(Constants.MaxCommentLength);

        builder.Property(x => x.Tags)
            .HasConversion(
                tags => tags == null ? null : string.Join(',', tags),
                value => value == null ? null : value.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList())
            .HasMaxLength(Constants.MaxTagsLength)
            .Metadata.SetValueComparer(
                new ValueComparer<List<string>>(
                (c1, c2) => c1 != null && c2 != null && c1.SequenceEqual(c2),
                c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                c => c.ToList()));
    }

    public static void ConfigureBudgetTransactionEntity<T>(this EntityTypeBuilder<T> builder) where T : BudgetTransaction
    {
        builder.ConfigureEntity();

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
            .HasMaxLength(Constants.MaxTitleLength);

        builder.Property(x => x.Comment)
            .HasMaxLength(Constants.MaxCommentLength);
    }

    public static void ConfigureEntity<T>(this EntityTypeBuilder<T> builder) where T : Entity
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedOnAdd()
            .IsRequired();
    }
}
