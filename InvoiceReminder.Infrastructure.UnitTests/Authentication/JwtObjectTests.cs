using InvoiceReminder.Authentication.Jwt;
using InvoiceReminder.UnitTests.Assets;

namespace InvoiceReminder.Infrastructure.UnitTests.Authentication;

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
