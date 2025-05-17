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

    public QuartzHostedServiceTests()
    {
        _logger = Substitute.For<ILogger<QuartzHostedService>>();
        _schedulerFactory = Substitute.For<ISchedulerFactory>();
        _jobFactory = Substitute.For<IJobFactory>();
        _scheduler = Substitute.For<IScheduler>();
        _schedules = [];

        _ = _schedulerFactory.GetScheduler(Arg.Any<CancellationToken>()).Returns(_scheduler);
        _ = _schedulerFactory.GetScheduler().Returns(_scheduler);
    }

    [TestMethod]
    public async Task ScheduleJobAsync_ShouldScheduleJobAndStartScheduler()
    {
        // Arrange
        var schedule = new JobSchedule { Id = Guid.NewGuid(), CronExpression = "0 0 * * * ?", UserId = Guid.NewGuid() };
        var service = new QuartzHostedService(_logger, _schedulerFactory, _jobFactory, _schedules);

        // Act
        await service.ScheduleJobAsync(schedule);

        // Assert
        _ = _scheduler.Received(1).ScheduleJob(Arg.Is<IJobDetail>(j => j.Key.Name == $"{schedule.Id}.job"),
            Arg.Is<ITrigger>(t => t.Key.Name == $"{schedule.Id}.trigger"));

        _scheduler.Received(1).JobFactory = Arg.Is(_jobFactory);

        await _scheduler.Received(1).Start();
    }

    [TestMethod]
    public async Task ScheduleJobAsync_NullSchedule_ShouldThrowArgumentNullException()
    {
        // Arrange
        var service = new QuartzHostedService(_logger, _schedulerFactory, _jobFactory, _schedules);

        // Act & Assert
        _ = await Should.ThrowAsync<ArgumentNullException>(async () => await service.ScheduleJobAsync(null));
    }

    [TestMethod]
    public async Task ReScheduleJobAsync_JobExists_ShouldDeleteAndReschedule()
    {
        // Arrange
        var schedule = new JobSchedule { Id = Guid.NewGuid(), CronExpression = "0 0 * * * ?", UserId = Guid.NewGuid() };
        var service = new QuartzHostedService(_logger, _schedulerFactory, _jobFactory, _schedules);

        _ = _scheduler.CheckExists(Arg.Any<JobKey>()).Returns(true);

        // Act
        await service.ReScheduleJobAsync(schedule);

        // Assert
        _ = _scheduler.Received(1).DeleteJob(Arg.Is<JobKey>(k => k.Name == $"{schedule.Id}.job"),
            Arg.Any<CancellationToken>());

        _ = _scheduler.Received(1).ScheduleJob(Arg.Is<IJobDetail>(j => j.Key.Name == $"{schedule.Id}.job"),
            Arg.Is<ITrigger>(t => t.Key.Name == $"{schedule.Id}.trigger"));

        _scheduler.Received(1).JobFactory = Arg.Is(_jobFactory);

        await _scheduler.Received(1).Start();
    }

    [TestMethod]
    public async Task ReScheduleJobAsync_JobDoesNotExist_ShouldScheduleNewJob()
    {
        // Arrange
        var schedule = new JobSchedule { Id = Guid.NewGuid(), CronExpression = "0 0 * * * ?", UserId = Guid.NewGuid() };
        var service = new QuartzHostedService(_logger, _schedulerFactory, _jobFactory, _schedules);

        _ = _scheduler.CheckExists(Arg.Any<JobKey>()).Returns(false);

        // Act
        await service.ReScheduleJobAsync(schedule);

        // Assert
        _ = _scheduler.DidNotReceive().DeleteJob(Arg.Any<JobKey>(), Arg.Any<CancellationToken>());

        _ = _scheduler.Received(1).ScheduleJob(Arg.Is<IJobDetail>(j => j.Key.Name == $"{schedule.Id}.job"),
            Arg.Is<ITrigger>(t => t.Key.Name == $"{schedule.Id}.trigger"));

        _scheduler.Received(1).JobFactory = Arg.Is(_jobFactory);

        await _scheduler.Received(1).Start();
    }

    [TestMethod]
    public async Task ReScheduleJobAsync_NullSchedule_ShouldThrowArgumentNullException()
    {
        // Arrange
        var service = new QuartzHostedService(_logger, _schedulerFactory, _jobFactory, _schedules);

        // Act & Assert
        _ = await Should.ThrowAsync<ArgumentNullException>(async () => await service.ReScheduleJobAsync(null));
    }

    [TestMethod]
    public async Task RemoveJobAsync_ShouldDeleteJob()
    {
        // Arrange
        var schedule = new JobSchedule { Id = Guid.NewGuid() };
        var service = new QuartzHostedService(_logger, _schedulerFactory, _jobFactory, _schedules);

        // Act
        await service.RemoveJobAsync(schedule);

        // Assert
        _ = _scheduler.Received(1).DeleteJob(Arg.Is<JobKey>(k => k.Name == $"{schedule.Id}.job"),
            Arg.Any<CancellationToken>());

        _scheduler.Received(1).JobFactory = Arg.Is(_jobFactory);
    }

    [TestMethod]
    public async Task RemoveJobAsync_NullSchedule_ShouldThrowArgumentNullException()
    {
        // Arrange
        var service = new QuartzHostedService(_logger, _schedulerFactory, _jobFactory, _schedules);

        // Act & Assert
        _ = await Should.ThrowAsync<ArgumentNullException>(async () => await service.RemoveJobAsync(null));
    }

    [TestMethod]
    public async Task PauseJobAsync_ShouldPauseTriggerAndJob()
    {
        // Arrange
        var schedule = new JobSchedule { Id = Guid.NewGuid() };
        var service = new QuartzHostedService(_logger, _schedulerFactory, _jobFactory, _schedules);

        // Act
        await service.PauseJobAsync(schedule);

        // Assert
        _ = _scheduler.Received(1).PauseTrigger(Arg.Is<TriggerKey>(k => k.Name == $"{schedule.Id}.trigger"),
            Arg.Any<CancellationToken>());

        _ = _scheduler.Received(1).PauseJob(Arg.Is<JobKey>(k => k.Name == $"{schedule.Id}.job"),
            Arg.Any<CancellationToken>());

        _scheduler.Received(1).JobFactory = Arg.Is(_jobFactory);
    }

    [TestMethod]
    public async Task PauseJobAsync_NullSchedule_ShouldThrowArgumentNullException()
    {
        // Arrange
        var service = new QuartzHostedService(_logger, _schedulerFactory, _jobFactory, _schedules);

        // Act & Assert
        _ = await Should.ThrowAsync<ArgumentNullException>(async () => await service.PauseJobAsync(null));
    }

    [TestMethod]
    public async Task ResumeJobAsync_ShouldResumeTriggerAndJob()
    {
        // Arrange
        var schedule = new JobSchedule { Id = Guid.NewGuid() };
        var service = new QuartzHostedService(_logger, _schedulerFactory, _jobFactory, _schedules);

        // Act
        await service.ResumeJobAsync(schedule);

        // Assert
        _ = _scheduler.Received(1).ResumeJob(Arg.Is<JobKey>(k => k.Name == $"{schedule.Id}.job"));

        _ = _scheduler.Received(1).ResumeTrigger(Arg.Is<TriggerKey>(k => k.Name == $"{schedule.Id}.trigger"));

        _scheduler.Received(1).JobFactory = Arg.Is(_jobFactory);
    }

    [TestMethod]
    public async Task ResumeJobAsync_NullSchedule_ShouldThrowArgumentNullException()
    {
        // Arrange
        var service = new QuartzHostedService(_logger, _schedulerFactory, _jobFactory, _schedules);

        // Act & Assert
        _ = await Should.ThrowAsync<ArgumentNullException>(async () => await service.ResumeJobAsync(null));
    }

    [TestMethod]
    public async Task StartAsync_ShouldScheduleAndStartAllSchedules()
    {
        // Arrange
        var schedule1 = new JobSchedule { Id = Guid.NewGuid(), CronExpression = "0 0 * * * ?", UserId = Guid.NewGuid() };
        var schedule2 = new JobSchedule { Id = Guid.NewGuid(), CronExpression = "0 0 1 * * ?", UserId = Guid.NewGuid() };
        _schedules.AddRange([schedule1, schedule2]);
        var service = new QuartzHostedService(_logger, _schedulerFactory, _jobFactory, _schedules);

        // Act
        await service.StartAsync(CancellationToken.None);

        // Assert
        _ = _scheduler.Received(2).ScheduleJob(Arg.Any<IJobDetail>(), Arg.Any<ITrigger>(), Arg.Any<CancellationToken>());
        _ = _scheduler.Received(2).Start(Arg.Any<CancellationToken>());

        _scheduler.Received(1).JobFactory = Arg.Is(_jobFactory);
    }

    [TestMethod]
    public async Task StartAsync_SchedulerException_ShouldLogError()
    {
        // Arrange
        var schedule = new JobSchedule { Id = Guid.NewGuid(), CronExpression = "invalid cron", UserId = Guid.NewGuid() };
        _schedules.Add(schedule);

        var service = new QuartzHostedService(_logger, _schedulerFactory, _jobFactory, _schedules);

        // Act && Assert
        _ = await Should.ThrowAsync<SchedulerException>(service.StartAsync(CancellationToken.None));

        _ = _scheduler.DidNotReceive().ScheduleJob(Arg.Any<IJobDetail>(), Arg.Any<ITrigger>(), Arg.Any<CancellationToken>());
        _ = _scheduler.DidNotReceive().Start(Arg.Any<CancellationToken>());

        _logger.Received(1).Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString() == "Starting Job raised an Excepetion: invalid cron"),
            Arg.Any<SchedulerException>(),
            Arg.Any<Func<object, Exception, string>>());

        _scheduler.Received(1).JobFactory = Arg.Is(_jobFactory);
    }

    [TestMethod]
    public async Task StopAsync_ShouldShutdownScheduler()
    {
        // Arrange
        var schedule = new JobSchedule { Id = Guid.NewGuid(), CronExpression = "0 0 * * * ?", UserId = Guid.NewGuid() };
        _schedules.Add(schedule);

        var service = new QuartzHostedService(_logger, _schedulerFactory, _jobFactory, _schedules);

        // Act
        await service.StartAsync(CancellationToken.None);
        await service.StopAsync(CancellationToken.None);

        // Assert
        _ = _scheduler.Received(1).Shutdown(CancellationToken.None);
    }
}
