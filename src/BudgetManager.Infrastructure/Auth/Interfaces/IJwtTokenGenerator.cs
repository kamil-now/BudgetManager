using BudgetManager.Infrastructure.Auth.Models;
using Microsoft.IdentityModel.Tokens;

namespace BudgetManager.Infrastructure.Auth.Interfaces;

public interface IJwtTokenGenerator
{
  string GenerateToken(UserDTO user);
  TokenValidationParameters GetTokenValidationParameters();
}
