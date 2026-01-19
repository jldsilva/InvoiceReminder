using InvoiceReminder.Domain.Entities;
using InvoiceReminder.UnitTests.SUT.Assets;

namespace InvoiceReminder.UnitTests.Domain.Entities;

[TestClass]
public sealed class UserPasswordTests
{
    [TestMethod]
    public void TestProperties()
    {
        var sut = new UserPassword();
        var tester = new PropertyTester(sut);

        tester.TestProperties();
    }
}
