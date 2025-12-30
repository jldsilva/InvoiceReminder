using InvoiceReminder.Domain.Entities;
using InvoiceReminder.UnitTests.SUT.Assets;

namespace InvoiceReminder.UnitTests.Domain.Entities;

[TestClass]
public sealed class EmailAuthTokenTests
{
    [TestMethod]
    public void TestProperties()
    {
        var sut = new EmailAuthToken();
        var tester = new PropertyTester(sut);

        tester.TestProperties();
    }
}
