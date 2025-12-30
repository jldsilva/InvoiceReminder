using InvoiceReminder.Domain.Entities;
using InvoiceReminder.UnitTests.SUT.Assets;

namespace InvoiceReminder.UnitTests.Domain.Entities;

[TestClass]
public sealed class UserTests
{
    [TestMethod]
    public void TestProperties()
    {
        var sut = new User();
        var tester = new PropertyTester(sut);

        tester.IgnoredProperties.Add("Invoices");
        tester.IgnoredProperties.Add("JobSchedules");
        tester.IgnoredProperties.Add("EmailAuthTokens");
        tester.IgnoredProperties.Add("ScanEmailDefinitions");
        tester.TestProperties();
    }
}
