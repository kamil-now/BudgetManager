# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

Build/test target the root `BudgetManager.sln` (contains all 7 projects). `src/BudgetManager.sln` and `tests/BudgetManager.Tests.sln` are partial solutions.

```powershell
dotnet build BudgetManager.sln
dotnet test                                    # all tests
dotnet test tests/BudgetManager.UnitTests/BudgetManager.UnitTests.csproj
dotnet test --filter "FullyQualifiedName~CreateLedgerTests"
dotnet test -l "console;verbosity=detailed"
dotnet format                                  # applies .editorconfig; pre-commit hook runs it on staged .cs files
```

Coverage (needs `dotnet tool install -g dotnet-reportgenerator-globaltool`):

```powershell
Get-ChildItem -Recurse -Filter "coverage.cobertura.xml" | Remove-Item
dotnet test --settings coverlet.runsettings
reportgenerator -reports:**/coverage.cobertura.xml -targetdir:./coveragereport -reporttypes:Html
```

Run the API:

```powershell
dotnet run --project src/BudgetManager.Api        # http://localhost:5219, `/` redirects to `/scalar`
docker/run.ps1                                   # dotnet watch in docker + postgres 17, needs docker/.env (see .env.example)
scripts/setup.ps1                                # installs the git pre-commit hook
```

Migrations (run from `/src`):

```powershell
dotnet ef migrations add [Name] --context ApplicationDbContext --project BudgetManager.Infrastructure --startup-project BudgetManager.Api
```

All integration tests run against a `postgres:17` container started by Testcontainers (`TestDatabase`), one migrated database per fixture, so Docker has to be running. Unit tests need nothing.

## Architecture

.NET 10, layered: `Api` → `Application` → `Domain`, with `Infrastructure` implementing domain/application interfaces and `Common` at the bottom (no dependencies on the others).

**Hand-rolled mediator** (`BudgetManager.Common/Mediator.cs`) — not MediatR. `IRequest<TResponse>` / `IRequestHandler<,>`, resolved via reflection from the DI container; handlers are auto-registered by scanning the Application assembly (`UseMediator()`). `BudgetManager.Common` is a global using in Api and Application, so `IMediator`/`IRequest` need no import.

**Request flow**: controller takes the command/query record straight as its request body/query model and passes it to `mediator.Send`. Controllers do no validation and no mapping. `Mediator.Send` first calls `IRequestAuthorizer` (`Application/Security/RequestAuthorizer`), which authenticates every request except those marked `IAnonymousRequest` and checks ownership of the `Resources` a request declares through `IRequiresAccess`. Handlers then validate (`ValidationExtensions` throwing `ValidationException`) and mutate through `IBudgetManagerService`; they never authenticate or authorize, and read the current user from `ICurrentUserService.UserId`.

**Exceptions map to status codes** in `ExceptionHandlingMiddleware`: `NotFoundException` 404, `ValidationException` 422, `AuthenticationException` 401, `AuthorizationException` 403. Nothing else is caught.

**Data access** goes through `IBudgetManagerService` (`Domain/Interfaces`), a generic-plus-bespoke facade over `ApplicationDbContext`. Handlers never touch the DbContext. Rules: don't call `SaveChangesAsync` inside a service method — the handler calls it once so EF wraps the whole unit of work; use `RunInTransactionAsync` for multi-step operations. Add methods to the service when a handler needs a new query.

**Authorization is ownership-based**, not role-based. `MapControllers().RequireAuthorization()` makes every endpoint authenticated (`AuthController` is `[AllowAnonymous]`); JWT bearer settings come from the `JwtTokenSettings` config section. Per-resource access is checked in the mediator's authorizer, comparing the owner of each declared resource with the user id from the token. Only `Ledger` stores `OwnerId`; every other entity reaches it through its parent, declared once per entity as `IAccessControlled<T>.OwnerPath` (`Account` → `x.Ledger.OwnerId`, `AccountTransaction` → `x.Account.Ledger.OwnerId`). A request that implements neither `IRequiresAccess` nor `IAnonymousRequest` is refused by the authorizer, so a create with no parent to check declares `IRequiresAccess` with an empty resource list.

**Domain**: entities derive from `Entity` (Guid `Id` defaulted at construction, `CreatedAt`/`UpdatedAt`, identity equality). Money is a `Money(Amount, Currency)` record mapped as an EF owned type into `Amount`/`Currency` columns; multi-currency totals use `Balance : Dictionary<string, decimal>`, which drops zero entries. Transfers are modelled as two `AccountTransaction` rows (negated expense + income) linked by an `AccountTransfer`.

**Persistence**: postgres/Npgsql, code-first migrations, one `IEntityTypeConfiguration` per entity applied by assembly scan, `ConfigureEntity()` for the shared key setup. `ApplicationDbContext` stamps `CreatedAt`/`UpdatedAt` — in the `SaveChanges` override only, not `SaveChangesAsync`.

## Conventions

`src/GUIDELINES.md` is the source of truth for code style, test layout, and database rules — read it before adding entities, handlers, or tests. Key points:

- commands/queries/DTOs are `records`; commands/queries are the request models, DTOs the response models
- constants live in `Domain/Constants.cs` (Constants Class pattern), including max lengths used by both validators and EF configuration
- entities: `required` scalars, `= null!` navigation properties, `= []` collections
- tests: xUnit + Shouldly + NSubstitute, `MethodName_WhenCondition_ShouldResult`, Arrange/Act/Assert, `Theory` for parameterized cases, one logical outcome per test
- four test kinds with their own fixtures: unit (no DI, no DB), Application integration (`ApplicationFixture`, containerised postgres + mediator — should cover every command/query), Api integration (`ApiFixture`, real `Program.cs` with the DbContext repointed at a containerised postgres — should cover every endpoint incl. auth and status codes), Persistence integration (`PersistenceFixture`, containerised postgres, `ApplicationDbContext` only)
