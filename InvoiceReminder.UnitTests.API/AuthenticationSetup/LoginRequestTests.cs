using InvoiceReminder.API.AuthenticationSetup;
using InvoiceReminder.UnitTests.SUT.Assets;

namespace InvoiceReminder.UnitTests.API.AuthenticationSetup;

[TestClass]
public sealed class LoginRequestTests
{
    [TestMethod]
    public void TestProperties()
    {
        var sut = new LoginRequest();
        var tester = new PropertyTester(sut);

        tester.TestProperties();
    }
}
