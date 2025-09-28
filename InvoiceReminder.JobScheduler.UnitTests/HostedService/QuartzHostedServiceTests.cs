using InvoiceReminder.Domain.Entities;
using InvoiceReminder.JobScheduler.HostedService;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Quartz;
using Quartz.Spi;
using Shouldly;

namespace InvoiceReminder.JobScheduler.UnitTests.HostedService;

[TestClass]
public class QuartzHostedServiceTests
{
    private readonly ILogger<QuartzHostedService> _logger;
    private readonly ISchedulerFactory _schedulerFactory;
    private readonly IJobFactory _jobFactory;
    private readonly IScheduler _scheduler;
    private readonly List<JobSchedule> _schedules;
    private readonly QuartzHostedService _service;

    public TestContext TestContext { get; set; }

    public QuartzHostedServiceTests()
    {
        _logger = Substitute.For<ILogger<QuartzHostedService>>();
        _schedulerFactory = Substitute.For<ISchedulerFactory>();
        _jobFactory = Substitute.For<IJobFactory>();
        _scheduler = Substitute.For<IScheduler>();
        _schedules =
        [
            new() {
                Id = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                CronExpression = "0/5 * * * * ?"
            }
        ];
        _service = Substitute.For<QuartzHostedService>(_logger, _schedulerFactory, _jobFactory, _schedules);

        _ = _schedulerFactory.GetScheduler(Arg.Any<CancellationToken>()).Returns(Task.FromResult(_scheduler));
    }

    [TestMethod]
    public async Task ScheduleJobAsync_ShouldScheduleJobAndStartScheduler()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var schedule = _schedules[0];

        // Act
        await _service.ScheduleJobAsync(schedule, cts.Token);

        // Assert
        _ = _scheduler.Received(1).ScheduleJob(
            Arg.Is<IJobDetail>(j => j.Key.Name == $"{schedule.Id}.job"),
            Arg.Is<ITrigger>(t => t.Key.Name == $"{schedule.Id}.trigger"),
            Arg.Is<CancellationToken>(ct => ct == cts.Token));

        await _scheduler.Received(1).Start(Arg.Is<CancellationToken>(ct => ct == cts.Token));
    }

    [TestMethod]
    public async Task ScheduleJobAsync_NullSchedule_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        _ = await Should.ThrowAsync<ArgumentNullException>(async () =>
                await _service.ScheduleJobAsync(null, TestContext.CancellationTokenSource.Token)
            );
    }

    [TestMethod]
    public async Task DeleteJobAsync_ShouldDeleteJobIfExists()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var schedule = _schedules[0];
        var jobKey = new JobKey($"{schedule.Id}.job");

        _ = _scheduler.CheckExists(jobKey, Arg.Any<CancellationToken>()).Returns(true);

        // Act
        await _service.DeleteJobAsync(schedule, cts.Token);

        // Assert
        _ = await _scheduler.Received(1).DeleteJob(jobKey, Arg.Is<CancellationToken>(ct => ct == cts.Token));
    }

    [TestMethod]
    public async Task DeleteJobAsync_WhenJobScheduleIsNull_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        _ = await Should.ThrowAsync<ArgumentNullException>(async () =>
                await _service.DeleteJobAsync(null, TestContext.CancellationTokenSource.Token)
            );
    }

    [TestMethod]
    public async Task PauseJobAsync_ShouldPauseTriggerAndJob()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var schedule = _schedules[0];

        // Act
        await _service.PauseJobAsync(schedule, cts.Token);

        // Assert
        _ = _scheduler.Received(1).PauseTrigger(Arg.Is<TriggerKey>(k => k.Name == $"{schedule.Id}.trigger"),
            Arg.Is<CancellationToken>(ct => ct == cts.Token));

        _ = _scheduler.Received(1).PauseJob(Arg.Is<JobKey>(k => k.Name == $"{schedule.Id}.job"),
            Arg.Is<CancellationToken>(ct => ct == cts.Token));

        _scheduler.Received(1).JobFactory = Arg.Is(_jobFactory);
    }

    [TestMethod]
    public async Task PauseJobAsync_NullSchedule_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        _ = await Should.ThrowAsync<ArgumentNullException>(async () =>
                await _service.PauseJobAsync(null, TestContext.CancellationTokenSource.Token)
            );
    }

    [TestMethod]
    public async Task ResumeJobAsync_ShouldResumeTriggerAndJob()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var schedule = new JobSchedule { Id = Guid.NewGuid() };
        var service = new QuartzHostedService(_logger, _schedulerFactory, _jobFactory, _schedules);

        // Act
        await service.ResumeJobAsync(schedule, cts.Token);

        // Assert
        _ = _scheduler.Received(1).ResumeJob(Arg.Is<JobKey>(k => k.Name == $"{schedule.Id}.job"),
            Arg.Is<CancellationToken>(ct => ct == cts.Token));

        _ = _scheduler.Received(1).ResumeTrigger(Arg.Is<TriggerKey>(k => k.Name == $"{schedule.Id}.trigger"),
            Arg.Is<CancellationToken>(ct => ct == cts.Token));

        _scheduler.Received(1).JobFactory = Arg.Is(_jobFactory);
    }

    [TestMethod]
    public async Task ResumeJobAsync_NullSchedule_ShouldThrowArgumentNullException()
    {
        // Arrange
        var service = new QuartzHostedService(_logger, _schedulerFactory, _jobFactory, _schedules);

        // Act & Assert
        _ = await Should.ThrowAsync<ArgumentNullException>(async () =>
                await service.ResumeJobAsync(null, TestContext.CancellationTokenSource.Token)
            );
    }

    [TestMethod]
    public async Task StartAsync_ShouldScheduleAndStartAllSchedules()
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        // Act
        await _service.StartAsync(cts.Token);

        // Assert
        _ = _scheduler.Received(1).ScheduleJob(
            Arg.Any<IJobDetail>(),
            Arg.Any<ITrigger>(),
            Arg.Is<CancellationToken>(ct => ct == cts.Token));

        _ = _scheduler.Received(1).Start(Arg.Is<CancellationToken>(ct => ct == cts.Token));

        _scheduler.Received(1).JobFactory = Arg.Is(_jobFactory);
    }

    [TestMethod]
    public async Task StartAsync_InvalidCronExpression_ShouldLogError()
    {
        // Arrange
        var schedule = new JobSchedule { Id = Guid.NewGuid(), CronExpression = "invalid cron", UserId = Guid.NewGuid() };
        var schedules = new List<JobSchedule> { schedule };

        var service = new QuartzHostedService(_logger, _schedulerFactory, _jobFactory, schedules);

        // Act && Assert
        await service.StartAsync(CancellationToken.None);

        _ = _scheduler.DidNotReceive().ScheduleJob(Arg.Any<IJobDetail>(), Arg.Any<ITrigger>(), Arg.Any<CancellationToken>());

        _logger.Received(1).Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString().Contains("CronJob inválido:")),
            Arg.Any<SchedulerException>(),
            Arg.Any<Func<object, Exception, string>>()
        );

        _scheduler.Received(1).JobFactory = Arg.Is(_jobFactory);
    }

    [TestMethod]
    public async Task StopAsync_ShouldShutdownScheduler()
    {
        // Arrange & Act
        await _service.StartAsync(CancellationToken.None);
        await _service.StopAsync(CancellationToken.None);

        // Assert
        _ = _scheduler.Received(1).Shutdown(CancellationToken.None);
    }
}
