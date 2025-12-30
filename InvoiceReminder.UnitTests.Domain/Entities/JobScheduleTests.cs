using InvoiceReminder.Domain.Entities;
using InvoiceReminder.UnitTests.SUT.Assets;

namespace InvoiceReminder.UnitTests.Domain.Entities;

[TestClass]
public sealed class JobScheduleTests
{
    [TestMethod]
    public void TestProperties()
    {
        var sut = new JobSchedule();
        var tester = new PropertyTester(sut);

        tester.TestProperties();
    }
}
