public record AccountDto(
    string Id,
    string Name,
    Balance Balance,
    Balance InitialBalance,
    bool IsDeleted);