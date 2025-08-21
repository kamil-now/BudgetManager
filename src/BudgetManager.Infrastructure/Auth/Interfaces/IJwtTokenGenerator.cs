using BudgetManager.Infrastructure.Auth.Models;

namespace BudgetManager.Infrastructure.Auth.Interfaces;

public interface IJwtTokenGenerator
{
    string GenerateToken(UserDTO user);
}
