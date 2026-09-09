using BudgetManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Testcontainers.PostgreSql;

namespace BudgetManager.IntegrationTests;

public static class TestDatabase
{
    private static readonly SemaphoreSlim ContainerGate = new(1, 1);
    private static PostgreSqlContainer? _container;

    public static string CreateMigratedDatabase()
        => Task.Run(CreateMigratedDatabaseAsync).GetAwaiter().GetResult();

    private static async Task<string> CreateMigratedDatabaseAsync()
    {
        var container = await StartContainerAsync();
        var connectionString = new NpgsqlConnectionStringBuilder(container.GetConnectionString())
        {
            Database = $"budgetmanager_test_{Guid.NewGuid():N}",
            IncludeErrorDetail = true
        }.ConnectionString;

        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(connectionString).Options;
        await using var context = new ApplicationDbContext(options);
        await context.Database.MigrateAsync();

        return connectionString;
    }

    private static async Task<PostgreSqlContainer> StartContainerAsync()
    {
        await ContainerGate.WaitAsync();
        try
        {
            if (_container is null)
            {
                _container = new PostgreSqlBuilder("postgres:17").Build();
                await _container.StartAsync();
            }

            return _container;
        }
        finally
        {
            ContainerGate.Release();
        }
    }
}
