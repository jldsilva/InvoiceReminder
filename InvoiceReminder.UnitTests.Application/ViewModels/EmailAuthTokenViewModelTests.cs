using InvoiceReminder.Application.ViewModels;
using InvoiceReminder.UnitTests.SUT.Assets;
using Shouldly;

namespace InvoiceReminder.UnitTests.Application.ViewModels;

[TestClass]
public sealed class EmailAuthTokenViewModelTests
{
    [TestMethod]
    public void TestProperties()
    {
        var sut = new EmailAuthTokenViewModel();
        var tester = new PropertyTester(sut);

        tester.TestProperties();
    }

    [TestMethod]
    public void IsStale_WhenAccessTokenExpired_ReturnsTrue()
    {
        var sut = new EmailAuthTokenViewModel
        {
            AccessTokenExpiry = DateTime.UtcNow.AddMinutes(-1)
        };

        sut.IsStale.ShouldBe(true);
    }

    [TestMethod]
    public void IsStale_WhenAccessTokenValid_ReturnsFalse()
    {
        var sut = new EmailAuthTokenViewModel
        {
            AccessTokenExpiry = DateTime.UtcNow.AddMinutes(10)
        };

        sut.IsStale.ShouldBe(false);
    }
}
