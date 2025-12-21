using BudgetManager.Domain;
using BudgetManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BudgetManager.Infrastructure.Persistence.Configuration;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.ConfigureEntity();

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(Constants.MaxNameLength);

        builder.Property(x => x.HashedPassword)
            .IsRequired()
            .HasMaxLength(Constants.HashedPasswordLength);

        builder.Property(x => x.Email)
            .HasMaxLength(Constants.MaxEmailLength)
            .HasAnnotation("RegularExpression", Constants.EmailRegexp)
            .IsRequired();

        builder.HasMany(x => x.Ledgers)
            .WithOne(x => x.Owner)
            .HasForeignKey(x => x.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Budgets)
            .WithOne(x => x.Owner)
            .HasForeignKey(x => x.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.Email).IsUnique();
    }
}
