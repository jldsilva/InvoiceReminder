using InvoiceReminder.Data.Persistence.EntitiesConfig;
using Shouldly;

namespace InvoiceReminder.UnitTests.Infrastructure.Data.EntitiesConfig;

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
