using InvoiceReminder.Domain.Enums;
using InvoiceReminder.ExternalServices.BarcodeReader;
using NSubstitute;
using Shouldly;

namespace InvoiceReminder.UnitTests.ExternalServices.BarcodeReader;

[TestClass]
public sealed class BarcodeHandlerFactoryTests
{
    private readonly IInvoiceBarcodeHandler _accountInvoiceHandler;
    private readonly IInvoiceBarcodeHandler _bankInvoiceHandler;
    private BarcodeHandlerFactory _barcodeHandlerFactory;

    public BarcodeHandlerFactoryTests()
    {
        _accountInvoiceHandler = Substitute.For<IInvoiceBarcodeHandler>();
        _bankInvoiceHandler = Substitute.For<IInvoiceBarcodeHandler>();

        _ = _accountInvoiceHandler.InvoiceType.Returns(InvoiceType.AccountInvoice);
        _ = _bankInvoiceHandler.InvoiceType.Returns(InvoiceType.BankInvoice);
    }

    [TestMethod]
    public void Constructor_ShouldRegisterHandlers_WhenMultipleHandlersProvided()
    {
        // Arrange
        var handlers = new List<IInvoiceBarcodeHandler> { _accountInvoiceHandler, _bankInvoiceHandler };

        // Act
        _barcodeHandlerFactory = new BarcodeHandlerFactory(handlers);

        // Assert
        _ = _barcodeHandlerFactory.ShouldNotBeNull();
    }

    [TestMethod]
    public void GetHandler_ShouldReturnAccountInvoiceHandler_WhenAccountInvoiceTypeRequested()
    {
        // Arrange
        var handlers = new List<IInvoiceBarcodeHandler> { _accountInvoiceHandler, _bankInvoiceHandler };
        _barcodeHandlerFactory = new BarcodeHandlerFactory(handlers);

        // Act
        var handler = _barcodeHandlerFactory.GetHandler(InvoiceType.AccountInvoice);

        // Assert
        handler.ShouldBeSameAs(_accountInvoiceHandler);
    }

    [TestMethod]
    public void GetHandler_ShouldReturnBankInvoiceHandler_WhenBankInvoiceTypeRequested()
    {
        // Arrange
        var handlers = new List<IInvoiceBarcodeHandler> { _accountInvoiceHandler, _bankInvoiceHandler };
        _barcodeHandlerFactory = new BarcodeHandlerFactory(handlers);

        // Act
        var handler = _barcodeHandlerFactory.GetHandler(InvoiceType.BankInvoice);

        // Assert
        handler.ShouldBeSameAs(_bankInvoiceHandler);
    }

    [TestMethod]
    public void GetHandler_ShouldThrowNotSupportedException_WhenInvoiceTypeNotFound()
    {
        // Arrange
        var handlers = new List<IInvoiceBarcodeHandler> { _accountInvoiceHandler };
        _barcodeHandlerFactory = new BarcodeHandlerFactory(handlers);

        // Act && Assert
        var exception = Should.Throw<NotSupportedException>(() =>
            _barcodeHandlerFactory.GetHandler(InvoiceType.BankInvoice));

        exception.Message.ShouldContain("No barcode handler found for invoice type:");
    }

    [TestMethod]
    public void GetHandler_ShouldThrowNotSupportedException_WhenNoHandlersProvided()
    {
        // Arrange
        var handlers = new List<IInvoiceBarcodeHandler>();
        _barcodeHandlerFactory = new BarcodeHandlerFactory(handlers);

        // Act && Assert
        var exception = Should.Throw<NotSupportedException>(() =>
            _barcodeHandlerFactory.GetHandler(InvoiceType.AccountInvoice));

        exception.Message.ShouldContain("No barcode handler found for invoice type:");
    }
}
