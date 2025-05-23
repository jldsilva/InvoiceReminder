using InvoiceReminder.Domain.Entities;
using NetArchTest.Rules;
using Shouldly;

namespace InvoiceReminder.ArchitectureTests.Domain;

[TestClass]
public sealed class EntitiesTests
{
    [TestMethod]
    public void GivenDomainLayer_ThenShouldNotHaveAnyDependencies()
    {
        // Arrange && Act
        var result = Types
            .InAssembly(typeof(EntityDefaults).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny("Application", "CrossCutting", "Data", "ExternalServices")
            .GetResult();

        // Assert
        result.IsSuccessful.ShouldBeTrue();
    }

    [TestMethod]
    public void GivenDomainLayer_WhenEntitiesAreAccessedFromOtherProjects_ThenTheyShouldBeVisible()
    {
        // Arrange && Act
        var result = Types
            .InAssembly(typeof(EntityDefaults).Assembly)
            .That()
                .ResideInNamespace("InvoiceReminder.Domain.Entities").And()
                .DoNotHaveName("EntityDefaults")
            .Should()
                .BePublic().And()
                .BeClasses().And()
                .NotBeStatic().And()
                .Inherit(typeof(EntityDefaults))
            .GetResult();

        // Assert
        result.IsSuccessful.ShouldBeTrue();
    }
}
