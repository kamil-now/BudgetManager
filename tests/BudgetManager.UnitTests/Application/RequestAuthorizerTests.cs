using System.Linq.Expressions;
using BudgetManager.Application.Interfaces;
using BudgetManager.Application.Security;
using BudgetManager.Application.Services;
using BudgetManager.Application.Validators;
using BudgetManager.Domain.Entities;
using BudgetManager.Domain.Interfaces;
using NSubstitute;
using Shouldly;

namespace BudgetManager.UnitTests.Application;

public class RequestAuthorizerTests
{
    private record UndeclaredRequest;

    private record AnonymousRequest : IAnonymousRequest;

    private record AccountRequest(params Guid[] AccountIds) : IRequiresAccess
    {
        IEnumerable<Resource> IRequiresAccess.Resources => [.. AccountIds.Select(Resource.Of<Account>)];
    }

    [Fact]
    public async Task AuthorizeAsync_WhenRequestDeclaresNothing_ShouldRequireAuthentication()
    {
        // Arrange
        var authorizer = CreateAuthorizer(null, out _);

        // Act & Assert
        await Should.ThrowAsync<AuthenticationException>(() => authorizer.AuthorizeAsync(new UndeclaredRequest(), default));
    }

    [Fact]
    public async Task AuthorizeAsync_WhenRequestIsAnonymous_ShouldNotRequireAUser()
    {
        // Arrange
        var authorizer = CreateAuthorizer(null, out _);

        // Act & Assert
        await Should.NotThrowAsync(() => authorizer.AuthorizeAsync(new AnonymousRequest(), default));
    }

    [Fact]
    public async Task AuthorizeAsync_WhenResourceIsOwnedByAnotherUser_ShouldThrowAuthorizationException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var authorizer = CreateAuthorizer(userId, out var ownerReader);
        ownerReader.ReadOwnerIdAsync(accountId, Arg.Any<Expression<Func<Account, Guid>>>(), Arg.Any<CancellationToken>()).Returns(Guid.NewGuid());

        // Act & Assert
        var ex = await Should.ThrowAsync<AuthorizationException>(() => authorizer.AuthorizeAsync(new AccountRequest(accountId), default));
        ex.Message.ShouldBeEquivalentTo($"Account with ID '{accountId}' cannot be accessed by user with ID '{userId}'.");
    }

    [Fact]
    public async Task AuthorizeAsync_WhenResourceDoesNotExist_ShouldThrowAuthorizationException()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var authorizer = CreateAuthorizer(Guid.NewGuid(), out var ownerReader);
        ownerReader.ReadOwnerIdAsync(accountId, Arg.Any<Expression<Func<Account, Guid>>>(), Arg.Any<CancellationToken>()).Returns((Guid?)null);

        // Act & Assert
        await Should.ThrowAsync<AuthorizationException>(() => authorizer.AuthorizeAsync(new AccountRequest(accountId), default));
    }

    [Fact]
    public async Task AuthorizeAsync_WhenOneOfTheResourcesIsOwnedByAnotherUser_ShouldThrowAuthorizationException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var ownAccountId = Guid.NewGuid();
        var otherAccountId = Guid.NewGuid();
        var authorizer = CreateAuthorizer(userId, out var ownerReader);
        ownerReader.ReadOwnerIdAsync(ownAccountId, Arg.Any<Expression<Func<Account, Guid>>>(), Arg.Any<CancellationToken>()).Returns(userId);
        ownerReader.ReadOwnerIdAsync(otherAccountId, Arg.Any<Expression<Func<Account, Guid>>>(), Arg.Any<CancellationToken>()).Returns(Guid.NewGuid());

        // Act & Assert
        await Should.ThrowAsync<AuthorizationException>(() => authorizer.AuthorizeAsync(new AccountRequest(ownAccountId, otherAccountId), default));
    }

    [Fact]
    public async Task AuthorizeAsync_WhenAllResourcesAreOwnedByTheUser_ShouldCheckEveryResource()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var targetAccountId = Guid.NewGuid();
        var authorizer = CreateAuthorizer(userId, out var ownerReader);
        ownerReader.ReadOwnerIdAsync(Arg.Any<Guid>(), Arg.Any<Expression<Func<Account, Guid>>>(), Arg.Any<CancellationToken>()).Returns(userId);

        // Act
        await authorizer.AuthorizeAsync(new AccountRequest(accountId, targetAccountId), default);

        // Assert
        await ownerReader.Received(1).ReadOwnerIdAsync(accountId, Arg.Any<Expression<Func<Account, Guid>>>(), Arg.Any<CancellationToken>());
        await ownerReader.Received(1).ReadOwnerIdAsync(targetAccountId, Arg.Any<Expression<Func<Account, Guid>>>(), Arg.Any<CancellationToken>());
    }

    private static RequestAuthorizer CreateAuthorizer(Guid? userId, out IResourceOwnerReader ownerReader)
    {
        var currentUser = Substitute.For<ICurrentUserService>();
        if (userId is Guid id)
        {
            currentUser.UserId.Returns(id);
        }
        else
        {
            currentUser.UserId.Returns(_ => throw new AuthenticationException("User ID is invalid."));
        }

        ownerReader = Substitute.For<IResourceOwnerReader>();

        return new RequestAuthorizer(currentUser, ownerReader);
    }
}
