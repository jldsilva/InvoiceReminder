using InvoiceReminder.Data.Persistence.EntitiesConfig;
using Shouldly;

namespace InvoiceReminder.Infrastructure.UnitTests.Data.EntitiesConfig;

[TestClass]
public sealed class JobScheduleConfigTests
{
    [TestMethod]
    public void JobScheduleConfig_ShouldNotThrowErrorWhenInstantiated()
    {
        // Arrange && Act
        Action action = () => _ = new JobScheduleConfig();

        // Assert
        action.ShouldNotThrow();
    }
}
