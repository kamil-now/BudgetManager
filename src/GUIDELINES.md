# Development Guidelines

## Table of Contents
- [Code Style](#code-style)
- [Tests](#tests)
- [Database](#database)
- [API & Application](#api--application)

## Code Style

- use `Constants Class` pattern for constants
- use `Arrange, Act, Assert` pattern
- use `Theory` for parameterized tests

## Tests
To run tests go to `/tests` and run `dotnet test -l "console;verbosity=detailed"`
To run peristance integration tests download and install postgreSQL, use the default port, and after it's installed create user `dev` with password `kmwtw` and  priviledges `create database` and `login`.
If you wish to make changes to your postgresql install remember to update `ConnectionStrings__DefaultConnection` in appsettings.

`xUnit` with `Shouldly` and `NSubstitute`

`MethodName_WhenCondition_ShouldResult`

*Try to avoid tests with multiple conditions/results - prefer testing one logical outcome per test*

 `MethodName_WhenConditionA_Or_ConditionB_ShouldResultA_And_ResultB`

### Unit tests
Only where it makes sense, no test fixtures, no database, no dependency injection, **keep it simple**.

### Application integration tests
- **test all commands and queries - should provide the most coverage**
- `ApplicationFixture` (in-memory database, mediator, limited DI)
  
### API integration tests
- **test all endpoints**
- authorization and authentication
- response codes
- `ApiFixture` (`Program.cs` setup with overriden in-memory database, full DI),

### Persistence integration tests
- **test the most important entities dependencies and constraints**,
- test `ApplicationDbContext` only
- `PersistenceFixture` (with local postgres instance)

## Database
- use code-first migrations (postgres with Npgsql)
- create configuration file for each new entity
- use required properties with `required` keyword
- use navigation properties with `= null!`
- use collections with `= []`
- in application command/query handlers use `BudgetManagerService` for database operations
- create new `BudgetManagerService` methods when needed
- avoid using `SaveChangesAsync()` in `BudgetManagerService` methods - saving should be invoked by command/query handler after all changes are applied (`SaveChangesAsync` then wraps all in single transaction)
- use explicit transactions for complex operations with `BudgetManagerService.RunInTransactionAsync`

### Migrations
- use Entity Framework Tools (`dotnet tool install --global dotnet-ef`)
- go to `/src` and execute `dotnet ef migrations add [MigrationName] --context ApplicationDbContext --project BudgetManager.Infrastructure --startup-project BudgetManager.Api --verbose`

## API & Application
- use commands/queries models as request models
- use DTOs as response models
- use `records` for commands/queries/DTOs
- avoid request validation in controllers 
- validate all commands/queries in handlers
- use `ValidationException` with `ValidationExtensions`
