using InvoiceReminder.Domain.Entities;
using InvoiceReminder.UnitTests.Assets;

namespace InvoiceReminder.DomainEntities.UnitTests.Entities;

[TestClass]
public sealed class InvoiceTests
{
    [TestMethod]
    public void TestProperties()
    {
        var sut = new Invoice();
        var tester = new PropertyTester(sut);

        tester.TestProperties();
    }
}
