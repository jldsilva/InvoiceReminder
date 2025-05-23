using InvoiceReminder.Data.Interfaces;
using InvoiceReminder.Data.Repository;
using Mono.Cecil;
using NetArchTest.Rules;
using Shouldly;

namespace InvoiceReminder.ArchitectureTests.Infrastructure;

[TestClass]
public sealed class RepositoriesTests
{
    [TestMethod]
    public void GivenDataLayer_ThenRepositoriesShouldHaveDependencieOnlyOnDomainLayer()
    {
        // Arrange && Act
        var result = Types
            .InAssembly(typeof(BaseRepository<,>).Assembly)
            .That()
                .Inherit(typeof(BaseRepository<,>)).And()
                .DoNotImplementInterface(typeof(IBaseRepository<>)).And()
                .DoNotImplementInterface(typeof(IUnitOfWork))
            .Should()
                .HaveDependencyOn("Domain")
            .GetResult();

        // Assert
        result.IsSuccessful.ShouldBeTrue();
    }

    [TestMethod]
    public void GivenDataLayer_RepositoriesMustEndWithRepository()
    {
        // Arrange && Act
        var result = Types
            .InAssembly(typeof(BaseRepository<,>).Assembly)
            .That()
                .ResideInNamespace("InvoiceReminder.Data.Repository").And()
                .DoNotImplementInterface(typeof(IUnitOfWork)).And()
                .DoNotHaveNameStartingWith("Base")
            .Should()
                .HaveNameEndingWith("Repository").And()
                .Inherit(typeof(BaseRepository<,>)).And()
                .ImplementInterface(typeof(IBaseRepository<>))
            .GetResult();

        // Assert
        result.IsSuccessful.ShouldBeTrue();
    }

    [TestMethod]
    public void GivenRepositoryInterfaces_ThenShouldBePublicAndBeInterfacesAndStartWithIAndEndWithRepository()
    {
        // Arrange
        var interfaceRule = new InterfaceRepositoryRule();

        // Act
        var result = Types
            .InAssembly(typeof(IBaseRepository<>).Assembly)
            .That()
                .ResideInNamespace("InvoiceReminder.Data.Interfaces").And()
                .DoNotHaveNameStartingWith("IBase").And()
                .DoNotHaveName("IUnitOfWork")
            .Should()
                .MeetCustomRule(interfaceRule)
            .GetResult();

        // Assert
        result.IsSuccessful.ShouldBeTrue();
    }
}

public class InterfaceRepositoryRule : ICustomRule
{
    public bool MeetsRule(TypeDefinition type)
    {
        return type.IsInterface &&
               type.IsPublic &&
               type.Name.StartsWith('I') &&
               type.Name.EndsWith("Repository");
    }
}
