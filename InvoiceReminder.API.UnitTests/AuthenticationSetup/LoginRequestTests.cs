using InvoiceReminder.API.AuthenticationSetup;
using InvoiceReminder.UnitTests.Assets;

namespace InvoiceReminder.API.UnitTests.AuthenticationSetup;

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
