using BudgetManager.Application.Interfaces;

namespace BudgetManager.Infrastructure.Auth.Services;

public class BCryptPasswordHasher : IPasswordHasher
{
    public string Hash(string password)
      => BCrypt.Net.BCrypt.HashPassword(password, BCrypt.Net.BCrypt.GenerateSalt(12));

    public bool Verify(string password, string hash)
      => BCrypt.Net.BCrypt.Verify(password, hash);
}
