using BudgetManager.Domain.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BudgetManager.Infrastructure.Persistence.Configuration;

public static class EntityBuilderExtensions
{

    public static void ConfigureEntity<T>(this EntityTypeBuilder<T> builder) where T : Entity
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedOnAdd()
            .IsRequired();
    }
}
