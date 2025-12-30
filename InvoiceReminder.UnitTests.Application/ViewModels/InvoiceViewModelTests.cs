using InvoiceReminder.Application.ViewModels;
using InvoiceReminder.UnitTests.SUT.Assets;

namespace InvoiceReminder.UnitTests.Application.ViewModels;

[TestClass]
public sealed class InvoiceViewModelTests
{
    [TestMethod]
    public void TestProperties()
    {
        var sut = new InvoiceViewModel();
        var tester = new PropertyTester(sut);

        tester.TestProperties();
    }
}
