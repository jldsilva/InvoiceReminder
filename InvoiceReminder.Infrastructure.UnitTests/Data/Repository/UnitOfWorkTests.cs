using InvoiceReminder.Data.Exceptions;
using InvoiceReminder.Data.Persistence;
using InvoiceReminder.Data.Repository;
using InvoiceReminder.Domain.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using System.Data;

namespace InvoiceReminder.Infrastructure.UnitTests.Data.Repository
{
    [TestClass]
    public sealed class UnitOfWorkTests
    {
        private readonly SqliteConnection _connection;
        private readonly DbContextOptions<CoreDbContext> _contextOptions;
        private readonly ILogger<UnitOfWork> _logger;

        public TestContext TestContext { get; set; }

        public UnitOfWorkTests()
        {
            _connection = new SqliteConnection("Filename=:memory:");

            _contextOptions = new DbContextOptionsBuilder<CoreDbContext>()
                .UseSqlite(_connection)
                .Options;

            _logger = Substitute.For<ILogger<UnitOfWork>>();
        }

        [TestInitialize]
        public void Setup()
        {
            _connection.Open();
        }

        [TestCleanup]
        public void TearDown()
        {
            _connection.Dispose();
        }

        [TestMethod]
        public async Task SaveChangesAsync_Should_OpenConnection_BeginTransaction_SaveChanges_CommitTransaction_CloseConnection()
        {
            // Arrange
            using var context = CreateContext();
            _ = await context.Database.EnsureCreatedAsync(TestContext.CancellationToken);
            var unitOfWork = CreateUnitOfWork(context);

            // Act
            await unitOfWork.SaveChangesAsync(TestContext.CancellationToken);

            // Assert
            context.Database.GetDbConnection().State.ShouldBe(ConnectionState.Closed);
        }

        [TestMethod]
        public async Task SaveChangesAsync_Should_RollbackTransaction_LogError_AndThrowDataLayerException_OnException()
        {
            // Arrange
            using var context = CreateContext();
            _ = await context.Database.EnsureCreatedAsync(TestContext.CancellationToken);
            var unitOfWork = CreateUnitOfWork(context);

            _ = context.Users.Add(new User { Id = Guid.NewGuid() });
            _ = _logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);

            // Act
            var dataLayerException = await Should.ThrowAsync<DataLayerException>(
                async () => await unitOfWork.SaveChangesAsync(TestContext.CancellationToken)
            );

            // Assert
            context.Database.GetDbConnection().State.ShouldBe(ConnectionState.Closed);

            _ = dataLayerException.ShouldNotBeNull();
            _ = dataLayerException.InnerException.ShouldBeOfType<DbUpdateException>();
            dataLayerException.Message.ShouldContain("Exception raised while saving changes");

            var eventId = Arg.Any<EventId>();
            var state = Arg.Any<object>();
            var exception = Arg.Any<Exception>();
            var formatter = Arg.Any<Func<object, Exception, string>>();

            _logger.Received(1).Log(LogLevel.Error, eventId, state, exception, formatter);
        }

        [TestMethod]
        public async Task SaveChangesAsync_Should_HandleConnectionAlreadyOpen()
        {
            // Arrange
            using var context = CreateContext();
            _ = await context.Database.EnsureCreatedAsync(TestContext.CancellationToken);
            await context.Database.OpenConnectionAsync(TestContext.CancellationToken);
            var unitOfWork = CreateUnitOfWork(context);

            // Act
            await unitOfWork.SaveChangesAsync(TestContext.CancellationToken);

            // Assert
            context.Database.GetDbConnection().State.ShouldBe(ConnectionState.Closed);
        }

        [TestMethod]
        public void Dispose_Should_DisposeDbContext()
        {
            // Arrange
            using var context = CreateContext();
            var unitOfWork = CreateUnitOfWork(context);

            // Act
            unitOfWork.Dispose();

            // Assert
            _ = Should.Throw<ObjectDisposedException>(() => context.Users.FirstOrDefault());
        }

        [TestMethod]
        public void Dispose_CalledMultipleTimes_Should_NotThrowException()
        {
            // Arrange
            using var context = CreateContext();
            var unitOfWork = CreateUnitOfWork(context);

            // Act
            unitOfWork.Dispose();
            unitOfWork.Dispose();

            // Assert
            Should.NotThrow(() => { });
        }

        private CoreDbContext CreateContext()
        {
            return new(_contextOptions);
        }

        private UnitOfWork CreateUnitOfWork(CoreDbContext context)
        {
            return new UnitOfWork(context, _logger);
        }
    }
}
