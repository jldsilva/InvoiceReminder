using InvoiceReminder.Domain.Entities;
using InvoiceReminder.UnitTests.Assets;

namespace InvoiceReminder.DomainEntities.UnitTests.Entities;

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
        tester.IgnoredProperties.Add("ScanEmailDefinitions");
        tester.TestProperties();
    }
}
