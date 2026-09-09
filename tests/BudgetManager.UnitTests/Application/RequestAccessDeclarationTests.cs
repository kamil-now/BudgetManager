using BudgetManager.Application.Configuration;
using BudgetManager.Application.Security;
using BudgetManager.Common;
using Shouldly;

namespace BudgetManager.UnitTests.Application;

public class RequestAccessDeclarationTests
{
    [Fact]
    public void EveryRequest_ShouldDeclareItsAccessRequirements()
    {
        // Arrange
        var requests = typeof(IAssemblyMarker).Assembly.GetTypes().Where(IsRequest);

        // Act
        var undeclared = requests
            .Where(x => !typeof(IAnonymousRequest).IsAssignableFrom(x))
            .Where(x => !typeof(IRequiresAccess).IsAssignableFrom(x))
            .Select(x => x.Name);

        // Assert
        undeclared.ShouldBeEmpty();
    }

    private static bool IsRequest(Type type)
        => !type.IsAbstract
        && !type.IsInterface
        && type.GetInterfaces().Any(x => x == typeof(IRequest) || (x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IRequest<>)));
}
