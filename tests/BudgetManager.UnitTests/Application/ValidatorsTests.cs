using System.Linq.Expressions;
using BudgetManager.Application.Services;
using BudgetManager.Application.Validators;
using BudgetManager.Domain.Entities;
using BudgetManager.Domain.Interfaces;
using NSubstitute;
using Shouldly;

namespace BudgetManager.UnitTests.Application;

public class ValidatorsTests
{
    [Fact]
    public async Task EnsureExistsAsync_WhenUserIdIsInvalid_ShouldThrowValidationException()
    {
        // Arrange
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.Id.Returns("not a valid guid");

        // Act & Assert
        var ex = await Should.ThrowAsync<ValidationException>(() => currentUser.EnsureExistsAsync(Substitute.For<IBudgetManagerService>(), default));
        ex.Message.ShouldBeEquivalentTo("Id value 'not a valid guid' is invalid.");
    }

    [Fact]
    public async Task EnsureExistsAsync_WhenUserDoesNotExist_ShouldThrowValidationException()
    {
        // Arrange
        var currentUser = Substitute.For<ICurrentUserService>();
        var id = Guid.NewGuid();
        currentUser.Id.Returns(id.ToString());

        // Act & Assert
        var ex = await Should.ThrowAsync<ValidationException>(() => currentUser.EnsureExistsAsync(Substitute.For<IBudgetManagerService>(), default));
        ex.Message.ShouldBeEquivalentTo($"Entity with ID '{id}' does not exist.");
    }

    [Fact]
    public async Task EnsureExistsAsync_WhenUserExists_ShouldReturnUserId()
    {
        // Arrange
        var cancellationToken = new CancellationToken();
        var currentUser = Substitute.For<ICurrentUserService>();
        var budgetManagerService = Substitute.For<IBudgetManagerService>();
        var id = Guid.NewGuid();
        currentUser.Id.Returns(id.ToString());
        budgetManagerService.ExistsAsync(Arg.Any<Expression<Func<User, bool>>>(), cancellationToken).Returns(true);

        // Act 
        var result = await currentUser.EnsureExistsAsync(budgetManagerService, cancellationToken);

        // Assert
        result.ShouldBeEquivalentTo(id);
    }

    [Fact]
    public void EnsureValid_WhenValueIsNotValidGuid_ShouldThrowValidationException()
    {
        // Arrange
        var input = "not a valid guid";

        // Act & Assert
        var ex = Should.Throw<ValidationException>(() => input.EnsureValidId());
        ex.Message.ShouldBeEquivalentTo("input value 'not a valid guid' is invalid.");
    }
    // TODO
}
