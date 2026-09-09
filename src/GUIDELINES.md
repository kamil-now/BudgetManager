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
Integration tests need Docker running - they start a `postgres:17` container (`TestDatabase`) and give each fixture its own migrated database on it.

`xUnit` with `Shouldly` and `NSubstitute`

`MethodName_WhenCondition_ShouldResult`

*Try to avoid tests with multiple conditions/results - prefer testing one logical outcome per test*

 `MethodName_WhenConditionA_Or_ConditionB_ShouldResultA_And_ResultB`

### Unit tests
Only where it makes sense, no test fixtures, no database, no dependency injection, **keep it simple**.

### Application integration tests
- **test all commands and queries - should provide the most coverage**
- `ApplicationFixture` (containerised postgres, mediator, limited DI)
  
### API integration tests
- **test all endpoints**
- authorization and authentication
- response codes
- `ApiFixture` (`Program.cs` setup with the `ApplicationDbContext` pointed at a containerised postgres, full DI),

### Persistence integration tests
- **test the most important entities dependencies and constraints**,
- test `ApplicationDbContext` only
- `PersistenceFixture` (containerised postgres)


### Tests coverage report
To get proper full coverage report delete old report files before running dotnet test `Get-ChildItem -Recurse -Filter "coverage.cobertura.xml" | Remove-Item`.

Then run `dotnet test --settings coverlet.runsettings`

Merged code coverage report can be generated using `dotnet-reportgenerator-globaltool`:
- if not yet installed run `dotnet tool install -g dotnet-reportgenerator-globaltool`
- to get merged report run `reportgenerator -reports:**/coverage.cobertura.xml -targetdir:./coveragereport -reporttypes:Html`
- then full report should be available in `/coverage/index.html`


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

### Authentication and authorization
- never authenticate or authorize in handlers - `RequestAuthorizer` does both for every request sent through the mediator
- every request needs a signed in user unless it implements `IAnonymousRequest`
- to guard a resource implement `IRequiresAccess` and list its `Resources`:
  - `Resource.Of<Ledger>(LedgerId)` - every entity declares where its owner is as `IAccessControlled<T>.OwnerPath`, so the call is the same for a ledger and for anything owned through a parent
- a resource owned by somebody else and a resource that does not exist both give `AuthorizationException`
- handlers that need the current user read `ICurrentUserService.UserId`
- a request that declares neither is refused by `RequestAuthorizer`, and `RequestAccessDeclarationTests` fails on it; a create with no parent to check declares `IRequiresAccess` with an empty list
