using InvoiceReminder.Application.ViewModels;
using InvoiceReminder.UnitTests.SUT.Assets;

namespace InvoiceReminder.UnitTests.Application.ViewModels;

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
