using Bogus;
using InvoiceReminder.Domain.Entities;
using InvoiceReminder.JobScheduler.HostedService;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Quartz;
using Quartz.Spi;
using Shouldly;

namespace InvoiceReminder.JobScheduler.UnitTests.HostedService;

[TestClass]
public sealed class QuartzHostedServiceTests
{
    private readonly ILogger<QuartzHostedService> _logger;
    private readonly ISchedulerFactory _schedulerFactory;
    private readonly IJobFactory _jobFactory;
    private readonly IScheduler _scheduler;
    private readonly List<JobSchedule> _schedules;
    private readonly QuartzHostedService _service;
    private Faker<JobSchedule> _jobScheduleFaker;

    public TestContext TestContext { get; set; }

    public QuartzHostedServiceTests()
    {
        _logger = Substitute.For<ILogger<QuartzHostedService>>();
        _schedulerFactory = Substitute.For<ISchedulerFactory>();
        _jobFactory = Substitute.For<IJobFactory>();
        _scheduler = Substitute.For<IScheduler>();
        _schedules = [];
        _service = Substitute.For<QuartzHostedService>(_logger, _schedulerFactory, _jobFactory, _schedules);

        _ = _schedulerFactory.GetScheduler(Arg.Any<CancellationToken>()).Returns(Task.FromResult(_scheduler));
    }

    [TestInitialize]
    public void Setup()
    {
        InitializeFaker();
        _schedules.Clear();
    }

    private void InitializeFaker()
    {
        _jobScheduleFaker = new Faker<JobSchedule>()
            .RuleFor(j => j.Id, _ => Guid.NewGuid())
            .RuleFor(j => j.UserId, _ => Guid.NewGuid())
            .RuleFor(j => j.CronExpression, _ => "0/5 * * * * ?");
    }

    [TestMethod]
    public async Task ScheduleJobAsync_ShouldScheduleJobAndStartScheduler()
    {
        // Arrange
        var schedule = _jobScheduleFaker.Generate();
        _schedules.Add(schedule);

        // Act
        await _service.ScheduleJobAsync(schedule, TestContext.CancellationToken);

        // Assert
        _ = await _schedulerFactory.Received(1)
            .GetScheduler(Arg.Is<CancellationToken>(ct => ct == TestContext.CancellationToken));

        _ = _scheduler.Received(1).ScheduleJob(
            Arg.Is<IJobDetail>(j => j.Key.Name == $"{schedule.Id}.job"),
            Arg.Is<ITrigger>(t => t.Key.Name == $"{schedule.Id}.trigger"),
            Arg.Is<CancellationToken>(ct => ct == TestContext.CancellationToken));

        await _scheduler.Received(1).Start(Arg.Is<CancellationToken>(ct => ct == TestContext.CancellationToken));
    }

    [TestMethod]
    public async Task ScheduleJobAsync_NullSchedule_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        _ = await Should.ThrowAsync<ArgumentNullException>(async () =>
                await _service.ScheduleJobAsync(null, TestContext.CancellationToken)
            );
    }

    [TestMethod]
    public async Task DeleteJobAsync_ShouldDeleteJobIfExists()
    {
        // Arrange
        var schedule = _jobScheduleFaker.Generate();
        _schedules.Add(schedule);
        var jobKey = new JobKey($"{schedule.Id}.job");

        _ = _scheduler.CheckExists(jobKey, Arg.Is<CancellationToken>(ct => ct == TestContext.CancellationToken))
            .Returns(true);

        // Act
        await _service.DeleteJobAsync(schedule, TestContext.CancellationToken);

        // Assert
        _ = await _scheduler.Received(1)
            .CheckExists(jobKey, Arg.Is<CancellationToken>(ct => ct == TestContext.CancellationToken));

        _ = await _scheduler.Received(1)
            .DeleteJob(jobKey, Arg.Is<CancellationToken>(ct => ct == TestContext.CancellationToken));
    }

    [TestMethod]
    public async Task DeleteJobAsync_WhenJobScheduleIsNull_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        _ = await Should.ThrowAsync<ArgumentNullException>(async () =>
                await _service.DeleteJobAsync(null, TestContext.CancellationToken)
            );
    }

    [TestMethod]
    public async Task PauseJobAsync_ShouldPauseTriggerAndJob()
    {
        // Arrange
        var schedule = _jobScheduleFaker.Generate();
        _schedules.Add(schedule);

        // Act
        await _service.PauseJobAsync(schedule, TestContext.CancellationToken);

        // Assert
        _ = _scheduler.Received(1).PauseTrigger(Arg.Is<TriggerKey>(k => k.Name == $"{schedule.Id}.trigger"),
            Arg.Is<CancellationToken>(ct => ct == TestContext.CancellationToken));

        _ = _scheduler.Received(1).PauseJob(Arg.Is<JobKey>(k => k.Name == $"{schedule.Id}.job"),
            Arg.Is<CancellationToken>(ct => ct == TestContext.CancellationToken));

        _scheduler.Received(1).JobFactory = Arg.Is(_jobFactory);
    }

    [TestMethod]
    public async Task PauseJobAsync_NullSchedule_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        _ = await Should.ThrowAsync<ArgumentNullException>(async () =>
                await _service.PauseJobAsync(null, TestContext.CancellationToken)
            );
    }

    [TestMethod]
    public async Task ResumeJobAsync_ShouldResumeTriggerAndJob()
    {
        // Arrange
        var schedule = _jobScheduleFaker.Generate();
        _schedules.Add(schedule);
        var service = new QuartzHostedService(_logger, _schedulerFactory, _jobFactory, _schedules);

        // Act
        await service.ResumeJobAsync(schedule, TestContext.CancellationToken);

        // Assert
        _ = _scheduler.Received(1).ResumeJob(Arg.Is<JobKey>(k => k.Name == $"{schedule.Id}.job"),
            Arg.Is<CancellationToken>(ct => ct == TestContext.CancellationToken));

        _ = _scheduler.Received(1).ResumeTrigger(Arg.Is<TriggerKey>(k => k.Name == $"{schedule.Id}.trigger"),
            Arg.Is<CancellationToken>(ct => ct == TestContext.CancellationToken));

        _scheduler.Received(1).JobFactory = Arg.Is(_jobFactory);
    }

    [TestMethod]
    public async Task ResumeJobAsync_NullSchedule_ShouldThrowArgumentNullException()
    {
        // Arrange
        var service = new QuartzHostedService(_logger, _schedulerFactory, _jobFactory, _schedules);

        // Act & Assert
        _ = await Should.ThrowAsync<ArgumentNullException>(async () =>
                await service.ResumeJobAsync(null, TestContext.CancellationToken)
            );
    }

    [TestMethod]
    public async Task StartAsync_ShouldScheduleAndStartAllSchedules()
    {
        // Arrange
        _schedules.Add(_jobScheduleFaker.Generate());

        // Act
        await _service.StartAsync(TestContext.CancellationToken);

        // Assert
        _ = _scheduler.Received(1).ScheduleJob(
            Arg.Any<IJobDetail>(),
            Arg.Any<ITrigger>(),
            Arg.Is<CancellationToken>(ct => ct == TestContext.CancellationToken));

        _ = _scheduler.Received(1).Start(Arg.Is<CancellationToken>(ct => ct == TestContext.CancellationToken));

        _scheduler.Received(1).JobFactory = Arg.Is(_jobFactory);
    }

    [TestMethod]
    public async Task StartAsync_InvalidCronExpression_ShouldLogError()
    {
        // Arrange
        var schedule = _jobScheduleFaker
            .RuleFor(j => j.CronExpression, _ => "invalid cron")
            .Generate();

        _schedules.Add(schedule);
        var service = new QuartzHostedService(_logger, _schedulerFactory, _jobFactory, _schedules);

        _ = _logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);

        // Act && Assert
        await service.StartAsync(TestContext.CancellationToken);

        _ = _scheduler.DidNotReceive().ScheduleJob(Arg.Any<IJobDetail>(), Arg.Any<ITrigger>(), Arg.Any<CancellationToken>());

        _scheduler.Received(1).JobFactory = Arg.Is(_jobFactory);

        var eventId = Arg.Any<EventId>();
        var state = Arg.Is<object>(o => o.ToString().Contains("CronJob inválido:"));
        var loggedException = Arg.Is<Exception>(e => e == null);
        var formatter = Arg.Any<Func<object, Exception, string>>();

        _logger.Received(1).Log(LogLevel.Error, eventId, state, loggedException, formatter);
    }

    [TestMethod]
    public async Task StopAsync_ShouldShutdownScheduler()
    {
        // Arrange
        _schedules.Add(_jobScheduleFaker.Generate());

        // Act
        await _service.StartAsync(TestContext.CancellationToken);
        await _service.StopAsync(TestContext.CancellationToken);

        // Assert
        _ = _scheduler.Received(1).Shutdown(TestContext.CancellationToken);
    }
}
