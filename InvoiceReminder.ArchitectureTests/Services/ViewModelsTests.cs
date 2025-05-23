using InvoiceReminder.Application.ViewModels;
using NetArchTest.Rules;
using Shouldly;

namespace InvoiceReminder.ArchitectureTests.Services;

[TestClass]
public sealed class ViewModelsTests
{
    [TestMethod]
    public void GivenApplicationLayer_WhenViewModelsAreAccessedFromPresentation_ThenTheyShouldBeVisible()
    {
        // Arrange && Act
        var result = Types
            .InAssembly(typeof(ViewModelDefaults).Assembly)
            .That()
                .ResideInNamespace("InvoiceReminder.Application.ViewModels").And()
                .DoNotHaveName("ViewModelDefaults")
            .Should()
                .BePublic().And()
                .BeClasses().And()
                .NotBeStatic().And()
                .Inherit(typeof(ViewModelDefaults))
            .GetResult();

        // Assert
        result.IsSuccessful.ShouldBeTrue();
    }
}
