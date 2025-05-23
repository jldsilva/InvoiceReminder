using InvoiceReminder.Data.Persistence;
using Microsoft.EntityFrameworkCore;
using NetArchTest.Rules;
using Shouldly;

namespace InvoiceReminder.ArchitectureTests.Infrastructure;

[TestClass]
public sealed class EntitiesConfigTest
{
    [TestMethod]
    public void GivenDataLayer_EntitiesConfigMustdBeImplementedAsFollow()
    {
        // Arrange && Act
        var result = Types
            .InAssembly(typeof(CoreDbContext).Assembly)
            .That()
                .ResideInNamespace("InvoiceReminder.Data.Persistence.EntitiesConfig").And()
                .AreNotStatic().And()
                .AreClasses()
            .Should()
                .NotBePublic().And()
                .ImplementInterface(typeof(IEntityTypeConfiguration<>)).And()
                .HaveNameEndingWith("Config")
            .GetResult();

        // Assert
        result.IsSuccessful.ShouldBeTrue();
    }
}
