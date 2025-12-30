using InvoiceReminder.Authentication.Jwt;
using InvoiceReminder.UnitTests.SUT.Assets;

namespace InvoiceReminder.UnitTests.Infrastructure.Authentication;

[TestClass]
public sealed class JwtObjectTests
{
    [TestMethod]
    public void TestProperties()
    {
        var sut = new JwtObject();
        var tester = new PropertyTester(sut);

        tester.TestProperties();
    }
}
