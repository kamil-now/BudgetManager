using BudgetManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BudgetManager.Infrastructure.Persistence.Configuration;

public class ReallocationConfiguration : IEntityTypeConfiguration<Reallocation>
{
    public void Configure(EntityTypeBuilder<Reallocation> builder)
    {
        builder.ToTable("Reallocations");

        builder.ConfigureBudgetTransactionEntity();
    }
}
