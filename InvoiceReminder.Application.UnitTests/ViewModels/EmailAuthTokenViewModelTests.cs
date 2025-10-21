using InvoiceReminder.Application.ViewModels;
using InvoiceReminder.UnitTests.Assets;

namespace InvoiceReminder.Application.UnitTests.ViewModels;

[TestClass]
public sealed class EmailAuthTokenViewModelTests
{
    [TestMethod]
    public void TestProperties()
    {
        var sut = new EmailAuthTokenViewModel();
        var tester = new PropertyTester(sut);

        tester.TestProperties();
    }
}
