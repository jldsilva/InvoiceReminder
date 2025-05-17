using InvoiceReminder.Data.Persistence.EntitiesConfig;
using Shouldly;

namespace InvoiceReminder.Infrastructure.UnitTests.Data.EntitiesConfig;

[TestClass]
public sealed class ScanEmailDefinitionConfigTests
{
    [TestMethod]
    public void ScanEmailDefinitionConfig_ShouldNotThrowErrorWhenInstantiated()
    {
        // Arrange && Act
        Action action = () => _ = new ScanEmailDefinitionConfig();

        // Assert
        action.ShouldNotThrow();
    }
}
