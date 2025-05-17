using InvoiceReminder.Application.ViewModels;
using InvoiceReminder.UnitTests.Assets;

namespace InvoiceReminder.Application.UnitTests.ViewModels;

[TestClass]
public sealed class ScanEmailDefinitionViewModelTests
{
    [TestMethod]
    public void TestProperties()
    {
        var sut = new ScanEmailDefinitionViewModel();
        var tester = new PropertyTester(sut);

        tester.TestProperties();
    }
}
