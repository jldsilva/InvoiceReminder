using InvoiceReminder.Domain.Entities;
using NetArchTest.Rules;
using Shouldly;

namespace InvoiceReminder.ArchitectureTests.Domain;

[TestClass]
public sealed class ExtensionsTests
{
    [TestMethod]
    public void GivenDomainLayer_WhenExtensionsAreAccessedFromRepositoryClasses_ThenTheyShouldBeVisible()
    {
        // Arrange && Act
        var result = Types
            .InAssembly(typeof(EntityDefaults).Assembly)
            .That()
                .ResideInNamespace("InvoiceReminder.Domain.Extensions").And()
                .ArePublic().And()
                .AreStatic().And()
                .AreClasses()
            .Should()
                .HaveNameEndingWith("Extensions")
            .GetResult();

        // Assert
        result.IsSuccessful.ShouldBeTrue();
    }
}
