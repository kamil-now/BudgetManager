﻿namespace BudgetManager.Infrastructure.DependencyInjection;

using BudgetManager.Infrastructure.Models;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

public static class DependencyInjection
{
  private static readonly string DATABASE_NAME = "BudgetManager";
  private static readonly string USER_BUDGETS_COLLECTION = "UserBudgets";
  public static IServiceCollection UseMongoDB(this IServiceCollection services, string connectionString)
  {
    services.AddSingleton<IUserBudgetRepository>(_ =>
      new UserBudgetRepository(
        new MongoClient(connectionString)
          .GetDatabase(DATABASE_NAME)
          .GetCollection<UserBudget>(USER_BUDGETS_COLLECTION)
        )
    );
    return services;
  }
}
