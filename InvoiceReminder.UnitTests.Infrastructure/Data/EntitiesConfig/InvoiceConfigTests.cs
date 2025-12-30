using InvoiceReminder.Data.Persistence.EntitiesConfig;
using Shouldly;

namespace InvoiceReminder.UnitTests.Infrastructure.Data.EntitiesConfig;

[TestClass]
public sealed class InvoiceConfigTests
{
    [TestMethod]
    public void InvoiceConfig_ShouldNotThrowErrorWhenInstantiated()
    {
        // Arrange && Act
        Action action = () => _ = new InvoiceConfig();

        // Assert
        action.ShouldNotThrow();
    }
}
