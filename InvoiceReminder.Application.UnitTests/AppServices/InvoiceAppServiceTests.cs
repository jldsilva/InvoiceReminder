using Bogus;
using InvoiceReminder.Application.AppServices;
using InvoiceReminder.Application.Interfaces;
using InvoiceReminder.Application.ViewModels;
using InvoiceReminder.Data.Interfaces;
using InvoiceReminder.Domain.Entities;
using NSubstitute;
using Shouldly;

namespace InvoiceReminder.Application.UnitTests.AppServices;

[TestClass]
public sealed class InvoiceAppServiceTests
{
    private readonly IInvoiceRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly Faker _faker;

    public TestContext TestContext { get; set; }

    public InvoiceAppServiceTests()
    {
        _repository = Substitute.For<IInvoiceRepository>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _faker = new Faker();
    }

    private static Faker<Invoice> CreateInvoiceFaker()
    {
        return new Faker<Invoice>()
            .RuleFor(i => i.Id, faker => faker.Random.Guid())
            .RuleFor(i => i.UserId, faker => faker.Random.Guid())
            .RuleFor(i => i.Bank, faker => faker.PickRandom(
                "Banco do Brasil",
                "Bradesco",
                "Itaú",
                "Caixa Econômica Federal",
                "Santander",
                "Safra",
                "Citibank",
                "BTG Pactual"))
            .RuleFor(i => i.Beneficiary, faker => faker.Person.FullName)
            .RuleFor(i => i.Amount, faker => faker.Finance.Amount(10, 10000))
            .RuleFor(i => i.Barcode, faker => faker.Random.AlphaNumeric(44))
            .RuleFor(i => i.DueDate, faker => faker.Date.Future().ToUniversalTime())
            .RuleFor(i => i.CreatedAt, faker => faker.Date.Past().ToUniversalTime())
            .RuleFor(i => i.UpdatedAt, faker => faker.Date.Recent().ToUniversalTime());
    }

    [TestMethod]
    public void InvoiceAppService_ShouldBeAssignableToItsInterface_And_GenericInterface_And_GenericAppService()
    {
        // Arrange && Act
        var appService = new InvoiceAppService(_repository, _unitOfWork);

        // Assert
        appService.ShouldSatisfyAllConditions(() =>
        {
            _ = appService.ShouldBeAssignableTo<IInvoiceAppService>();
            _ = appService.ShouldNotBeNull();
            _ = appService.ShouldBeOfType<InvoiceAppService>();
        });
    }

    [TestMethod]
    public async Task GetByBarcodeAsync_WhenInvoiceExists_ShouldReturnSuccess_WithResultFound()
    {
        // Arrange
        var appService = new InvoiceAppService(_repository, _unitOfWork);
        var barcode = _faker.Random.AlphaNumeric(44);
        var invoice = CreateInvoiceFaker()
            .RuleFor(i => i.Barcode, barcode)
            .Generate();

        _ = _repository.GetByBarcodeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(invoice);

        // Act
        var result = await appService.GetByBarcodeAsync(barcode, TestContext.CancellationToken);

        // Assert
        _ = _repository.Received(1).GetByBarcodeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());

        result.ShouldSatisfyAllConditions(() =>
        {
            _ = result.ShouldNotBeNull();
            _ = result.Value.ShouldNotBeNull();
            _ = result.Value.ShouldBeOfType<InvoiceViewModel>();
            result.IsSuccess.ShouldBeTrue();
            result.Value.Barcode.ShouldBe(barcode);
        });
    }

    [TestMethod]
    public async Task GetByBarcodeAsync_WhenInvoiceDoesNotExist_ShouldReturnFailure_WithResultNotFound()
    {
        // Arrange
        var appService = new InvoiceAppService(_repository, _unitOfWork);
        var barcode = _faker.Random.AlphaNumeric(44);

        _ = _repository.GetByBarcodeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((Invoice)null);

        // Act
        var result = await appService.GetByBarcodeAsync(barcode, TestContext.CancellationToken);

        // Assert
        _ = _repository.Received(1).GetByBarcodeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());

        result.ShouldSatisfyAllConditions(() =>
        {
            _ = result.ShouldNotBeNull();
            result.Value.ShouldBeNull();
            result.IsSuccess.ShouldBeFalse();
            result.Error.ShouldBe("Invoice not Found.");
        });
    }
}
