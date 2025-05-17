using InvoiceReminder.Data.Persistence.EntitiesConfig;
using Shouldly;

namespace InvoiceReminder.Infrastructure.UnitTests.Data.EntitiesConfig;

[TestClass]
public sealed class UserConfigTests
{
    [TestMethod]
    public void UserConfig_ShouldNotThrowErrorWhenInstantiated()
    {
        // Arrange && Act
        Action action = () => _ = new UserConfig();

        // Assert
        action.ShouldNotThrow();
    }
}
