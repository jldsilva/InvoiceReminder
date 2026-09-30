using InvoiceReminder.JobScheduler.HostedService;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Quartz;
using Shouldly;

namespace InvoiceReminder.UnitTests.JobScheduler.HostedService;

[TestClass]
public sealed class JobSchedulerHostedServiceTests
{
    private readonly ILogger<JobSchedulerHostedService> _logger;
    private readonly ISchedulerFactory _schedulerFactory;
    private readonly IScheduler _scheduler;

    public TestContext TestContext { get; set; }

    public JobSchedulerHostedServiceTests()
    {
        _logger = Substitute.For<ILogger<JobSchedulerHostedService>>();
        _schedulerFactory = Substitute.For<ISchedulerFactory>();
        _scheduler = Substitute.For<IScheduler>();
    }

    [TestMethod]
    public async Task StartAsync_WithValidSchedulerFactory_ShouldInitializeScheduler()
    {
        // Arrange
        var service = new JobSchedulerHostedService(_logger, _schedulerFactory, []);

        _ = _schedulerFactory.GetScheduler(Arg.Any<CancellationToken>())
            .Returns(await new ValueTask<IScheduler>(_scheduler));

        // Act
        await service.StartAsync(TestContext.CancellationToken);

        // Assert
        _ = service.Scheduler.ShouldNotBeNull();
        _ = await _schedulerFactory.Received(1).GetScheduler(Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task StopAsync_WithValidScheduler_ShouldNotThrow()
    {
        // Arrange
        var service = new JobSchedulerHostedService(_logger, _schedulerFactory, []);

        _ = _schedulerFactory.GetScheduler(Arg.Any<CancellationToken>())
            .Returns(await new ValueTask<IScheduler>(_scheduler));

        // Act
        await service.StartAsync(TestContext.CancellationToken);

        // Assert
        await Should.NotThrowAsync(async () => await service.StopAsync(TestContext.CancellationToken));
    }

    [TestMethod]
    public void Constructor_ShouldCreateServiceWithEmptySchedules()
    {
        // Act
        var service = new JobSchedulerHostedService(_schedulerFactory);

        // Assert
        _ = service.ShouldNotBeNull();
    }

    [TestMethod]
    public async Task ScheduleJobAsync_WithNullSchedule_ShouldThrowArgumentNullException()
    {
        // Arrange
        var service = new JobSchedulerHostedService(_schedulerFactory);

        // Act & Assert
        _ = await Should.ThrowAsync<ArgumentNullException>(
            async () => await service.ScheduleJobAsync(null, TestContext.CancellationToken));
    }

    [TestMethod]
    public async Task DeleteJobAsync_WithNullSchedule_ShouldThrowArgumentNullException()
    {
        // Arrange
        var service = new JobSchedulerHostedService(_schedulerFactory);

        // Act & Assert
        _ = await Should.ThrowAsync<ArgumentNullException>(
            async () => await service.DeleteJobAsync(null, TestContext.CancellationToken));
    }

    [TestMethod]
    public async Task PauseJobAsync_WithNullSchedule_ShouldThrowArgumentNullException()
    {
        // Arrange
        var service = new JobSchedulerHostedService(_schedulerFactory);

        // Act & Assert
        _ = await Should.ThrowAsync<ArgumentNullException>(
            async () => await service.PauseJobAsync(null, TestContext.CancellationToken));
    }

    [TestMethod]
    public async Task ResumeJobAsync_WithNullSchedule_ShouldThrowArgumentNullException()
    {
        // Arrange
        var service = new JobSchedulerHostedService(_schedulerFactory);

        // Act & Assert
        _ = await Should.ThrowAsync<ArgumentNullException>(
            async () => await service.ResumeJobAsync(null, TestContext.CancellationToken));
    }
}
