using InvoiceReminder.Application.ViewModels;
using InvoiceReminder.UnitTests.SUT.Assets;

namespace InvoiceReminder.UnitTests.Application.ViewModels;

[TestClass]
public sealed class UserViewModelTests
{
    [TestMethod]
    public void TestProperties()
    {
        var sut = new UserViewModel();
        var tester = new PropertyTester(sut);

        tester.IgnoredProperties.Add("Invoices");
        tester.IgnoredProperties.Add("JobSchedules");
        tester.IgnoredProperties.Add("ScanEmailDefinitions");
        tester.TestProperties();
    }
}
