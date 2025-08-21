using BudgetManager.Domain.Entities;

namespace BudgetManager.Application.Models;

public class UserDTO(User user)
{
    public Guid Id => user.Id;
    public string Email => user.Email;
    public string? Name => user.Name;
}
