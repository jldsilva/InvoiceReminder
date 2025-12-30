using InvoiceReminder.Application.ViewModels;
using InvoiceReminder.UnitTests.SUT.Assets;

namespace InvoiceReminder.UnitTests.Application.ViewModels;

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
