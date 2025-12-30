using InvoiceReminder.Domain.Extensions;
using InvoiceReminder.UnitTests.SUT.Assets;

namespace InvoiceReminder.UnitTests.Domain.Extensions;

[TestClass]
public sealed class UserParametersTests
{
    [TestMethod]
    public void TestProperties()
    {
        var sut = new UserParameters();
        var tester = new PropertyTester(sut);

        tester.TestProperties();
    }
}
