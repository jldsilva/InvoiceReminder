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

    public TestContext TestContext { get; set; }

    public InvoiceAppServiceTests()
    {
        _repository = Substitute.For<IInvoiceRepository>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
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
        var barcode = "12345678901234567890";
        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            Barcode = barcode,
            Amount = 100.00m,
            DueDate = DateTime.UtcNow.AddDays(30),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _ = _repository.GetByBarCodeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(invoice);

        // Act
        var result = await appService.GetByBarcodeAsync(barcode, TestContext.CancellationToken);

        // Assert
        _ = _repository.Received(1).GetByBarCodeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());

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
        var barcode = "12345678901234567890";

        _ = _repository.GetByBarCodeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((Invoice)null);

        // Act
        var result = await appService.GetByBarcodeAsync(barcode, TestContext.CancellationToken);

        // Assert
        _ = _repository.Received(1).GetByBarCodeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());

        result.ShouldSatisfyAllConditions(() =>
        {
            _ = result.ShouldNotBeNull();
            result.Value.ShouldBeNull();
            result.IsSuccess.ShouldBeFalse();
            result.Error.ShouldBe("Invoice not Found.");
        });
    }
}
