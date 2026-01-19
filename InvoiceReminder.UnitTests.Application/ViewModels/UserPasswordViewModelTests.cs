using InvoiceReminder.Application.ViewModels;
using InvoiceReminder.UnitTests.SUT.Assets;

namespace InvoiceReminder.UnitTests.Application.ViewModels;

[TestClass]
public sealed class UserPasswordViewModelTests
{
    [TestMethod]
    public void TestProperties()
    {
        var sut = new UserPasswordViewModel();
        var tester = new PropertyTester(sut);

        tester.TestProperties();
    }
}
