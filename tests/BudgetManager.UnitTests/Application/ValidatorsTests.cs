using Shouldly;
using BudgetManager.Application.Validators;

namespace BudgetManager.UnitTests.Application;

public class ValidatorsTests
{
  [Fact]
  public void EnsureValid_WhenValueIsNotValidGuid_ShouldThrowValidationException()
  {
    // Arrange
    var input = "not a valid guid";

    // Act & Assert
    var ex = Should.Throw<ValidationException>(() => input.EnsureValid());
    ex.Message.ShouldBeEquivalentTo("");
  }
  // TODO
}