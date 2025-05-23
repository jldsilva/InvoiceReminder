using InvoiceReminder.Application.AppServices;
using InvoiceReminder.Application.Interfaces;
using Mono.Cecil;
using NetArchTest.Rules;
using Shouldly;

namespace InvoiceReminder.ArchitectureTests.Services;

[TestClass]
public sealed class AppServicesTests
{
    [TestMethod]
    public void GivenApplicationLayer_ThenAppServicesShouldHaveDependencieOnlyOnDataAndDomainLayerAndJobScheduler()
    {
        // Arrange && Act
        var result = Types
            .InAssembly(typeof(BaseAppService<,>).Assembly)
            .That()
                .Inherit(typeof(BaseAppService<,>)).And()
                .DoNotImplementInterface(typeof(IBaseAppService<,>))
            .Should()
                .HaveDependencyOnAll("Data", "Domain", "JobScheduler")
            .GetResult();

        // Assert
        result.IsSuccessful.ShouldBeTrue();
    }

    [TestMethod]
    public void GivenApplicationLayer_AppServicesMustEndWithAppService()
    {
        // Arrange && Act
        var result = Types
            .InAssembly(typeof(BaseAppService<,>).Assembly)
            .That()
                .ResideInNamespace("InvoiceReminder.Application.AppServices").And()
                .DoNotHaveNameStartingWith("Base")
            .Should()
                .HaveNameEndingWith("AppService").And()
                .Inherit(typeof(BaseAppService<,>)).And()
                .ImplementInterface(typeof(IBaseAppService<,>))
            .GetResult();

        // Assert
        result.IsSuccessful.ShouldBeTrue();
    }

    [TestMethod]
    public void GivenAppServiceInterfaces_ThenShouldBePublicAndBeInterfacesAndStartWithIAndEndWithAppService()
    {
        // Arrange
        var interfaceRule = new InterfaceAppServiceRule();

        // Act
        var result = Types
            .InAssembly(typeof(IBaseAppService<,>).Assembly)
            .That()
                .ResideInNamespace("InvoiceReminder.Application.Interfaces").And()
                .DoNotHaveNameStartingWith("IBase")
            .Should()
                .MeetCustomRule(interfaceRule)
            .GetResult();

        // Assert
        result.IsSuccessful.ShouldBeTrue();
    }
}

public class InterfaceAppServiceRule : ICustomRule
{
    public bool MeetsRule(TypeDefinition type)
    {
        return type.IsInterface &&
               type.IsPublic &&
               type.Name.StartsWith('I') &&
               type.Name.EndsWith("AppService");
    }
}
