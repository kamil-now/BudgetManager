using BudgetManager.Domain;
using BudgetManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BudgetManager.Infrastructure.Persistence.Configuration;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.ConfigureEntity();

        builder.Property(x => x.Name)
            .HasMaxLength(Constants.MaxNameLength);

        builder.Property(x => x.HashedPassword)
            .IsRequired()
            .HasMaxLength(Constants.HashedPasswordLength);

        builder.Property(x => x.Email)
            .HasMaxLength(Constants.MaxEmailLength)
            .HasAnnotation("RegularExpression", Constants.EmailRegexp)
            .IsRequired();

        builder.OwnsOne(x => x.Preferences, preferences =>
        {
            preferences.Property(x => x.Theme)
                .HasColumnName("Theme")
                .HasMaxLength(Constants.MaxPreferenceValueLength)
                .IsRequired();
            preferences.Property(x => x.DateFormat)
                .HasColumnName("DateFormat")
                .HasMaxLength(Constants.MaxPreferenceValueLength)
                .IsRequired();
            preferences.Property(x => x.DefaultCurrency)
                .HasColumnName("DefaultCurrency")
                .HasMaxLength(Constants.CurrencyCodeLength)
                .IsRequired();
            preferences.Property(x => x.FavoriteCurrencies)
                .HasColumnName("FavoriteCurrencies")
                .HasConversion(
                    currencies => string.Join(',', currencies),
                    value => value.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList())
                .HasMaxLength(Constants.MaxFavoriteCurrenciesLength)
                .IsRequired()
                .Metadata.SetValueComparer(
                    new ValueComparer<List<string>>(
                    (c1, c2) => c1 != null && c2 != null && c1.SequenceEqual(c2),
                    c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                    c => c.ToList()));
            preferences.Property(x => x.Language)
                .HasColumnName("Language")
                .HasMaxLength(Constants.MaxPreferenceValueLength)
                .IsRequired();
        });
        builder.Navigation(x => x.Preferences).IsRequired();

        builder.HasMany(x => x.Ledgers)
            .WithOne(x => x.Owner)
            .HasForeignKey(x => x.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.Email).IsUnique();
    }
}
