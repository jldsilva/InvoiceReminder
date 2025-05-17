using InvoiceReminder.Application.ViewModels;
using InvoiceReminder.UnitTests.Assets;

namespace InvoiceReminder.Application.UnitTests.ViewModels;

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
