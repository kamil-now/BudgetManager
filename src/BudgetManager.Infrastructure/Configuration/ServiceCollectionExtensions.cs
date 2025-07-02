using BudgetManager.Infrastructure.Auth.Interfaces;
using BudgetManager.Infrastructure.Auth.Models;
using BudgetManager.Infrastructure.Auth.Services;
using BudgetManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BudgetManager.Infrastructure.Configuration;

public static class ServiceCollectionExtensions
{
  public static IServiceCollection UseBudgetManagerAuth(this IServiceCollection services, IConfiguration configuration)
  {
    services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
    services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();

    services.Configure<JwtTokenSettings>(options => configuration.GetSection("JWT").Bind(options));

    return services;
  }

  public static IServiceCollection UsePostgreSQL(this IServiceCollection services, IConfiguration configuration)
  {
    var connectionString = configuration.GetConnectionString("DefaultConnection");
    services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(connectionString));
    return services;
  }
}
