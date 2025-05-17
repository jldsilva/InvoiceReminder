using InvoiceReminder.Application.ViewModels;
using InvoiceReminder.UnitTests.Assets;

namespace InvoiceReminder.Application.UnitTests.ViewModels;

[TestClass]
public sealed class JobScheduleViewmodelTests
{
    [TestMethod]
    public void TestProperties()
    {
        var sut = new JobScheduleViewModel();
        var tester = new PropertyTester(sut);

        tester.TestProperties();
    }
}
