using InvoiceReminder.Authentication.Jwt;
using InvoiceReminder.UnitTests.Assets;

namespace InvoiceReminder.Infrastructure.UnitTests.Authentication;

[TestClass]
public sealed class JwtOptionsTests
{
    [TestMethod]
    public void TestProperties()
    {
        var sut = new JwtOptions();
        var tester = new PropertyTester(sut);

        tester.TestProperties();
    }
}
