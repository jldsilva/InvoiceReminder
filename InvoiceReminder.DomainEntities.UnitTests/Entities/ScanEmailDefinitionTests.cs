using InvoiceReminder.Domain.Entities;
using InvoiceReminder.UnitTests.Assets;

namespace InvoiceReminder.DomainEntities.UnitTests.Entities;

[TestClass]
public sealed class ScanEmailDefinitionTests
{
    [TestMethod]
    public void TestProperties()
    {
        var sut = new ScanEmailDefinition();
        var tester = new PropertyTester(sut);

        tester.TestProperties();
    }
}
