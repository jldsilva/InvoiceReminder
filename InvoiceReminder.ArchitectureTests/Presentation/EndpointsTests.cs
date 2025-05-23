using InvoiceReminder.API.Endpoints;
using NetArchTest.Rules;
using Shouldly;

namespace InvoiceReminder.ArchitectureTests.Presentation;

[TestClass]
public sealed class EndpointsTests
{
    [TestMethod]
    public void GivenPresentationLayer_ThenShouldNotHaveAnyDependencies()
    {
        // Arrange && Act
        var result = Types
            .InAssembly(typeof(IEndpointDefinition).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny("Data", "Domain", "JobScheduler")
            .GetResult();

        // Assert
        result.IsSuccessful.ShouldBeTrue();
    }

    [TestMethod]
    public void GivenPresentationLayer_EndpointsMustdBeImplementedAsFollow()
    {
        // Arrange && Act
        var result = Types
            .InAssembly(typeof(IEndpointDefinition).Assembly)
            .That()
                .ResideInNamespace("InvoiceReminder.API.Endpoints").And()
                .ArePublic().And()
                .AreNotStatic().And()
                .AreClasses()
            .Should()
                .HaveNameEndingWith("Endpoints").And()
                .ImplementInterface(typeof(IEndpointDefinition))
            .GetResult();

        // Assert
        result.IsSuccessful.ShouldBeTrue();
    }
}
