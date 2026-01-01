using System.Linq.Expressions;
using BudgetManager.Application.Services;
using BudgetManager.Application.Validators;
using BudgetManager.Common.Models;
using BudgetManager.Domain;
using BudgetManager.Domain.Entities;
using BudgetManager.Domain.Interfaces;
using NSubstitute;
using Shouldly;

namespace BudgetManager.UnitTests.Application;

public class ValidatorsTests
{
    [Fact]
    public async Task EnsureAccessibleAsync_WhenEntityDoesNotExists_ShouldThrowAuthenticationException()
    {
        // Arrange
        var cancellationToken = new CancellationToken();
        var budgetManagerService = Substitute.For<IBudgetManagerService>();
        var accountId = Guid.NewGuid();
        budgetManagerService.GetOwnerIdAsync<Account>(accountId, cancellationToken).Returns((Guid?)null);

        // Act & Assert
        var ex = await Should.ThrowAsync<AuthenticationException>(() => accountId.EnsureAccessibleAsync<Account>(Guid.NewGuid(), budgetManagerService, cancellationToken));
        ex.Message.ShouldBeEquivalentTo($"User with ID '{accountId}' does not exist.");
    }

    [Fact]
    public async Task EnsureAccessibleAsync_WhenEntityIsNotAccessible_ShouldThrowAccessException()
    {
        // Arrange
        var cancellationToken = new CancellationToken();
        var budgetManagerService = Substitute.For<IBudgetManagerService>();
        var accountId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var currentUserId = Guid.NewGuid();

        budgetManagerService.GetOwnerIdAsync<Account>(accountId, cancellationToken).Returns(ownerId);

        // Act & Assert
        var ex = await Should.ThrowAsync<BudgetManager.Application.Validators.AuthorizationException>(() => accountId.EnsureAccessibleAsync<Account>(currentUserId, budgetManagerService, cancellationToken));
        ex.Message.ShouldBeEquivalentTo($"Account with ID '{accountId}' cannot be accessed by user with ID '{currentUserId}'.");
    }

    [Fact]
    public async Task EnsureAccessibleAsync_WhenEntityIsAccessible_ShouldReturnId()
    {
        // Arrange
        var cancellationToken = new CancellationToken();
        var budgetManagerService = Substitute.For<IBudgetManagerService>();
        var accountId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();

        budgetManagerService.GetOwnerIdAsync<Account>(accountId, cancellationToken).Returns(ownerId);

        // Act 
        var result = await accountId.EnsureAccessibleAsync<Account>(ownerId, budgetManagerService, cancellationToken);

        // Assert
        result.ShouldBeEquivalentTo(accountId);
    }

    [Theory]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    [InlineData("not a valid guid")]
    [InlineData("")]
    [InlineData(null)]
    public async Task EnsureExistsAsync_WhenUserIdIsInvalid_ShouldThrowAuthenticationException(string? userId)
    {
        // Arrange
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.Id.Returns(userId);

        // Act & Assert
        await Should.ThrowAsync<AuthenticationException>(() => currentUser.EnsureAuthenticatedAsync(Substitute.For<IBudgetManagerService>(), default));
    }

    [Fact]
    public async Task EnsureExistsAsync_WhenUserDoesNotExist_ShouldThrowUnauthenticatedException()
    {
        // Arrange
        var currentUser = Substitute.For<ICurrentUserService>();
        var id = Guid.NewGuid().ToString();
        currentUser.Id.Returns(id);

        // Act & Assert
        await Should.ThrowAsync<AuthenticationException>(() => currentUser.EnsureAuthenticatedAsync(Substitute.For<IBudgetManagerService>(), default));
    }

    [Fact]
    public async Task EnsureExistsAsync_WhenIdIsEmptyGuid_ShouldThrowValidationException()
    {
        // Arrange
        var cancellationToken = new CancellationToken();
        var id = Guid.Empty;

        // Act & Assert
        var ex = await Should.ThrowAsync<ValidationException>(() => id.EnsureExistsAsync<Budget>(Substitute.For<IBudgetManagerService>(), cancellationToken));
        ex.Message.ShouldBeEquivalentTo($"Budget ID cannot be empty.");
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
        var result = await currentUser.EnsureAuthenticatedAsync(budgetManagerService, cancellationToken);

        // Assert
        result.ShouldBeEquivalentTo(id);
    }

    [Fact]
    public void EnsureValidTags_WhenValueIsNull_ShouldReturnNull()
    {
        // Arrange
        string[]? tags = null;

        // Act 
        var result = tags.EnsureValidTags();

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void EnsureValidTags_WhenValueIsValid_ShouldReturnInputValue()
    {
        // Arrange
        string[] tags = ["some tag", "another", "12345", "@#!$%^&*()_=+-;':\\/[]{}|<>?,."];

        // Act 
        var result = tags.EnsureValidTags();

        // Assert
        result.ShouldBeEquivalentTo(tags);
    }

    [Fact]
    public void EnsureValidTags_WhenSomeTagIsInvalid_ShouldThrowValidationException()
    {
        // Arrange
        string[] tags = ["some tag", " "];

        // Act & Assert
        var ex = Should.Throw<ValidationException>(() => tags.EnsureValidTags());
        ex.Message.ShouldBeEquivalentTo("Each tag must have a value.");
    }

    [Fact]
    public void EnsureValidTags_WhenCombinedTagsLengthExceedsMax_ShouldThrowValidationException()
    {
        // Arrange
        var longTag = new string('t', Constants.MaxTagsLength - 4 + 1); // -4 to account for 'tag' and a tag separator
        string[] tags = ["tag", longTag];

        // Act & Assert
        var ex = Should.Throw<ValidationException>(() => tags.EnsureValidTags());
        ex.Message.ShouldBeEquivalentTo($"Tags are too long. Max combined tags length is {Constants.MaxTagsLength}.");
    }

    [Fact]
    public void EnsureNotLongerThan_WhenCountExceedsMax_ShouldThrowValidationException()
    {
        // Arrange
        var max = 1;
        var input = (string[])["1", "2"];

        // Act & Assert
        var ex = Should.Throw<ValidationException>(() => input.EnsureNotLongerThan(max));
        ex.Message.ShouldBeEquivalentTo($"input value is too long. Max length is {max}.");
    }

    [Fact]
    public void EnsureNotLongerThan_WhenCountDoesNotExceedMax_ShouldReturnInput()
    {
        // Arrange
        var max = 2;
        var input = (string[])["1", "2"];

        // Act 
        var result = input.EnsureNotLongerThan(max);

        // Assert
        result.ShouldBeEquivalentTo(input);
    }

    [Fact]
    public void EnsureNonnegative_WhenValueIsNegative_ShouldThrowValidationException()
    {
        // Arrange
        var input = -1;

        // Act & Assert
        var ex = Should.Throw<ValidationException>(() => input.EnsureNonnegative("ParamName"));
        ex.Message.ShouldBeEquivalentTo("ParamName must be greater than or equal zero.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void EnsureNonnegative_WhenValueIsNonNegative_ShouldReturnInput(int input)
    {
        // Act 
        var result = input.EnsureNonnegative();

        // Assert
        result.ShouldBeEquivalentTo(input);
    }

    [Fact]
    public void EnsureNotEmpty_WhenCollectionIsEmpty_ShouldThrowValidationException()
    {
        // Arrange
        var input = Array.Empty<int>();

        // Act & Assert
        var ex = Should.Throw<ValidationException>(() => input.EnsureNotEmpty());
        ex.Message.ShouldBeEquivalentTo("input cannot be empty.");
    }

    [Fact]
    public void EnsureNotEmpty_WhenCollectionIsNotEmpty_ShouldReturnInput()
    {
        // Arrange
        var input = (string[])["1"];

        // Act 
        var result = input.EnsureNotEmpty();

        // Assert
        result.ShouldBeEquivalentTo(input);
    }

    [Theory]
    [InlineData(0.000000000000000001, "US", "input currency value 'US' is not a valid currency code.")]
    [InlineData(1, "usds", "input currency value 'usds' is not a valid currency code.")]
    [InlineData(0, "u", "input amount cannot be zero.")]
    [InlineData(-1, "?", "input currency value '?' is not a valid currency code.")]
    public void EnsureValid_WhenMoneyIsInvalid_ShouldThrowValidationException(decimal amount, string currency, string expectedException)
    {
        // Arrange
        var input = new Money(amount, currency);

        // Act & Assert
        var ex = Should.Throw<ValidationException>(() => input.EnsureValid());
        ex.Message.ShouldBeEquivalentTo(expectedException);
    }

    [Theory]
    [InlineData(123456789.123456789, "USD")]
    [InlineData(0.0000000000000000000001, "usd")]
    [InlineData(1, "eur")]
    [InlineData(1, "XCD")]
    public void EnsureValid_WhenMoneyIsValid_ShouldReturnInput(decimal amount, string currency)
    {
        // Arrange
        var input = new Money(amount, currency);

        // Act 
        var result = input.EnsureValid();

        // Assert
        result.ShouldBeEquivalentTo(input);
    }
}
