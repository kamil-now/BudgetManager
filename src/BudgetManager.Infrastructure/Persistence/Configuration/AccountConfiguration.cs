using BudgetManager.Domain;
using BudgetManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BudgetManager.Infrastructure.Persistence.Configuration;

public class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.ToTable("Accounts");

        builder.ConfigureEntity();

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(Constants.MaxNameLength);

        builder
            .HasIndex(a => new { a.Name, a.OwnerId })
            .IsUnique();

        builder.Property(x => x.Description)
            .HasMaxLength(Constants.MaxCommentLength);

        builder.HasMany(x => x.Transactions)
            .WithOne(x => x.Account)
            .HasForeignKey(x => x.AccountId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
