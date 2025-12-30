using Bogus;
using InvoiceReminder.Data.Exceptions;
using InvoiceReminder.Data.Persistence;
using InvoiceReminder.Data.Repository;
using InvoiceReminder.Domain.Entities;
using InvoiceReminder.IntegrationTests.Data.ContainerSetup;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;

namespace InvoiceReminder.IntegrationTests.Data.Repository;

[TestClass]
public sealed class InvoiceRepositoryIntegrationTests
{
    private readonly CoreDbContext _dbContext;
    private readonly ILogger<InvoiceRepository> _repositoryLogger;
    private readonly ILogger<UnitOfWork> _unitOfWorkLogger;
    private readonly InvoiceRepository _repository;
    private readonly UnitOfWork _unitOfWork;

    public TestContext TestContext { get; set; }

    public InvoiceRepositoryIntegrationTests()
    {
        var options = new DbContextOptionsBuilder<CoreDbContext>()
            .UseNpgsql(DatabaseFixture.ConnectionString)
            .Options;

        _dbContext = new CoreDbContext(options);
        _repositoryLogger = Substitute.For<ILogger<InvoiceRepository>>();
        _unitOfWorkLogger = Substitute.For<ILogger<UnitOfWork>>();
        _repository = new InvoiceRepository(_dbContext, _repositoryLogger);
        _unitOfWork = new UnitOfWork(_dbContext, _unitOfWorkLogger);
    }

    #region Helper Methods

    private static Faker<User> UserFaker()
    {
        return new Faker<User>()
            .RuleFor(u => u.Id, _ => Guid.NewGuid())
            .RuleFor(u => u.TelegramChatId, f => f.Random.Long(100000000, long.MaxValue))
            .RuleFor(u => u.Name, f => f.Person.FullName)
            .RuleFor(u => u.Email, f => f.Internet.Email())
            .RuleFor(u => u.Password, f => f.Internet.Password(length: 16, memorable: false));
    }

    private static Faker<Invoice> InvoiceFaker()
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

    private async Task<User> CreateAndSaveUserAsync(User user = null)
    {
        user ??= UserFaker().Generate();
        var logger = Substitute.For<ILogger<UserRepository>>();
        var userRepository = new UserRepository(_dbContext, logger);

        _ = await userRepository.AddAsync(user, TestContext.CancellationToken);
        await _unitOfWork.SaveChangesAsync(TestContext.CancellationToken);

        return user;
    }

    private async Task<Invoice> CreateAndSaveInvoiceAsync(Invoice invoice = null)
    {
        var user = await CreateAndSaveUserAsync();
        invoice ??= InvoiceFaker()
            .RuleFor(i => i.UserId, _ => user.Id)
            .Generate();

        _ = await _repository.AddAsync(invoice, TestContext.CancellationToken);
        await _unitOfWork.SaveChangesAsync(TestContext.CancellationToken);

        return invoice;
    }

    #endregion

    #region GetByIdAsync Tests

    [TestMethod]
    public async Task GetByIdAsync_Should_Return_Invoice_By_Id()
    {
        // Arrange
        var invoice = await CreateAndSaveInvoiceAsync();

        // Act
        var result = await _repository.GetByIdAsync(invoice.Id, TestContext.CancellationToken);

        // Assert
        result.ShouldSatisfyAllConditions(() =>
        {
            _ = result.ShouldBeAssignableTo<Invoice>();
            _ = result.ShouldBeOfType<Invoice>();
            _ = result.ShouldNotBeNull();
            result.Id.ShouldBe(invoice.Id);
        });
    }

    [TestMethod]
    public async Task GetByIdAsync_Should_Return_Null_For_NonExistent_Invoice()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _repository.GetByIdAsync(nonExistentId, TestContext.CancellationToken);

        // Assert
        result.ShouldBeNull();
    }

    [TestMethod]
    public async Task GetByIdAsync_Should_Throw_Exception_On_Database_Error()
    {
        // Arrange
        var disposedContext = new CoreDbContext(new DbContextOptionsBuilder<CoreDbContext>()
            .UseNpgsql(DatabaseFixture.ConnectionString)
            .Options);

        var logger = Substitute.For<ILogger<InvoiceRepository>>();

        var repository = new InvoiceRepository(disposedContext, logger);

        // Act & Assert
        await disposedContext.DisposeAsync();

        _ = await Should.ThrowAsync<ObjectDisposedException>(
            async () => await repository.GetByIdAsync(Guid.NewGuid(), TestContext.CancellationToken)
        );
    }

    #endregion

    #region GetByBarcodeAsync Tests

    [TestMethod]
    public async Task GetByBarcodeAsync_Should_Return_Invoice_By_Barcode()
    {
        // Arrange
        var invoice = await CreateAndSaveInvoiceAsync();

        // Act
        var result = await _repository.GetByBarcodeAsync(invoice.Barcode, TestContext.CancellationToken);

        // Assert
        result.ShouldSatisfyAllConditions(() =>
        {
            _ = result.ShouldBeAssignableTo<Invoice>();
            _ = result.ShouldBeOfType<Invoice>();
            _ = result.ShouldNotBeNull();
            result.Barcode.ShouldBe(invoice.Barcode);
        });
    }

    [TestMethod]
    public async Task GetByBarcodeAsync_Should_Return_Null_For_NonExistent_Barcode()
    {
        // Arrange
        var nonExistentBarcode = "00000000000000000000000000000000000000000000";

        // Act
        var result = await _repository.GetByBarcodeAsync(nonExistentBarcode, TestContext.CancellationToken);

        // Assert
        result.ShouldBeNull();
    }

    [TestMethod]
    public async Task GetByBarcodeAsync_Should_Find_Invoice_With_Whitespace_In_Barcode()
    {
        // Arrange
        var invoice = await CreateAndSaveInvoiceAsync();

        // Act
        var result = await _repository.GetByBarcodeAsync($"  {invoice.Barcode}  ", TestContext.CancellationToken);

        // Assert
        result.ShouldSatisfyAllConditions(() =>
        {
            _ = result.ShouldBeAssignableTo<Invoice>();
            _ = result.ShouldBeOfType<Invoice>();
            _ = result.ShouldNotBeNull();
            result.Barcode.ShouldBe(invoice.Barcode);
        });
    }

    [TestMethod]
    public async Task GetByBarcodeAsync_Should_Retrieve_Correct_Invoice_When_Multiple_Exist()
    {
        // Arrange
        var user = await CreateAndSaveUserAsync();
        var invoice1 = InvoiceFaker()
            .RuleFor(i => i.UserId, _ => user.Id)
            .Generate();
        var invoice2 = InvoiceFaker()
            .RuleFor(i => i.UserId, _ => user.Id)
            .Generate();

        _ = await _repository.AddAsync(invoice1, TestContext.CancellationToken);
        _ = await _repository.AddAsync(invoice2, TestContext.CancellationToken);
        await _unitOfWork.SaveChangesAsync(TestContext.CancellationToken);

        // Act
        var result = await _repository.GetByBarcodeAsync(invoice2.Barcode, TestContext.CancellationToken);

        // Assert
        result.ShouldSatisfyAllConditions(() =>
        {
            _ = result.ShouldNotBeNull();
            _ = result.ShouldBeOfType<Invoice>();
            result.Barcode.ShouldBe(invoice2.Barcode);
            result.Id.ShouldBe(invoice2.Id);
        });
    }

    [TestMethod]
    public async Task GetByBarcodeAsync_Should_Throw_Exception_On_Database_Error()
    {
        // Arrange
        var disposedContext = new CoreDbContext(new DbContextOptionsBuilder<CoreDbContext>()
            .UseNpgsql(DatabaseFixture.ConnectionString)
            .Options);

        var logger = Substitute.For<ILogger<InvoiceRepository>>();

        var repository = new InvoiceRepository(disposedContext, logger);

        _ = logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);

        // Act & Assert
        await disposedContext.DisposeAsync();

        _ = await Should.ThrowAsync<DataLayerException>(
            async () => await repository.GetByBarcodeAsync("00000000000000000000000000000000000000000000",
                TestContext.CancellationToken)
        );

        logger.Received(1).Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception, string>>()
        );
    }

    #endregion

    #region CancellationToken Tests

    [TestMethod]
    public async Task GetByBarcodeAsync_Should_Handle_Cancellation_Request()
    {
        // Arrange
        var invoice = await CreateAndSaveInvoiceAsync();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        _ = _repositoryLogger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);

        // Act & Assert
        _ = await Should.ThrowAsync<OperationCanceledException>(
            async () => await _repository.GetByBarcodeAsync(invoice.Barcode, cts.Token)
        );

        _repositoryLogger.Received(1).Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<OperationCanceledException>(),
            Arg.Any<Func<object, Exception, string>>()
        );
    }

    #endregion
}
