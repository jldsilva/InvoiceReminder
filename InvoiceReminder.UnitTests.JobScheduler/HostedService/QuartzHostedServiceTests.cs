using Bogus;
using InvoiceReminder.Domain.Entities;
using InvoiceReminder.JobScheduler.HostedService;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Quartz;
using Quartz.Spi;
using Shouldly;

namespace InvoiceReminder.UnitTests.JobScheduler.HostedService;

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
        _ = _scheduler.IsStarted.Returns(false);
        _ = _scheduler.IsShutdown.Returns(false);
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
            .RuleFor(j => j.CronExpression, _ => "0 9 * * * ?");
    }

    #region StartAsync Tests

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
            Arg.Is<CancellationToken>(ct => ct == TestContext.CancellationToken)
        );

        _ = _scheduler.Received(1).Start(Arg.Is<CancellationToken>(ct => ct == TestContext.CancellationToken));
    }

    [TestMethod]
    public async Task StartAsync_InvalidCronExpression_ShouldLogError()
    {
        // Arrange
        var schedule = _jobScheduleFaker
            .RuleFor(j => j.CronExpression, _ => "invalid cron")
            .Generate();

        _schedules.Add(schedule);

        _ = _logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);

        // Act
        await _service.StartAsync(TestContext.CancellationToken);

        // Assert
        _logger.Received(1).Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString().Contains("CronJob inválido:")),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception, string>>()
        );

        _ = _scheduler.DidNotReceive().ScheduleJob(
            Arg.Any<IJobDetail>(),
            Arg.Any<ITrigger>(),
            Arg.Any<CancellationToken>()
        );
    }

    [TestMethod]
    public async Task StartAsync_WithEmptySchedules_ShouldInitializeSchedulerButNotScheduleJobs()
    {
        // Arrange && Act
        await _service.StartAsync(TestContext.CancellationToken);

        // Assert
        _ = await _schedulerFactory.Received(1).GetScheduler(Arg.Is<CancellationToken>(ct => ct == TestContext.CancellationToken));

        _ = _scheduler.DidNotReceive().ScheduleJob(Arg.Any<IJobDetail>(), Arg.Any<ITrigger>(), Arg.Any<CancellationToken>());

        await _scheduler.Received(1).Start(Arg.Is<CancellationToken>(ct => ct == TestContext.CancellationToken));

        _service.Scheduler.ShouldBeSameAs(_scheduler);
    }

    [TestMethod]
    public async Task StartAsync_WithMultipleSchedules_ShouldScheduleAllValidJobs()
    {
        // Arrange
        var schedule1 = _jobScheduleFaker.Generate();
        var schedule2 = _jobScheduleFaker.Generate();
        _schedules.AddRange([schedule1, schedule2]);

        // Act
        await _service.StartAsync(TestContext.CancellationToken);

        // Assert
        _ = _scheduler.Received(2).ScheduleJob(
            Arg.Any<IJobDetail>(),
            Arg.Any<ITrigger>(),
            Arg.Is<CancellationToken>(ct => ct == TestContext.CancellationToken)
        );

        await _scheduler.Received(1).Start(Arg.Is<CancellationToken>(ct => ct == TestContext.CancellationToken));
    }

    [TestMethod]
    public async Task StartAsync_WithMultipleInvalidCronExpressions_ShouldLogErrorsForEach()
    {
        // Arrange
        var invalidSchedule1 = _jobScheduleFaker
            .RuleFor(j => j.CronExpression, _ => "invalid 1")
            .Generate();

        var invalidSchedule2 = _jobScheduleFaker
            .RuleFor(j => j.CronExpression, _ => "invalid 2")
            .Generate();

        _schedules.AddRange([invalidSchedule1, invalidSchedule2]);

        _ = _logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);

        // Act
        await _service.StartAsync(TestContext.CancellationToken);

        // Assert
        _logger.Received(2).Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString().Contains("CronJob inválido:")),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception, string>>()
        );

        _ = _scheduler.DidNotReceive().ScheduleJob(
            Arg.Any<IJobDetail>(),
            Arg.Any<ITrigger>(),
            Arg.Any<CancellationToken>()
        );
    }

    [TestMethod]
    public async Task StartAsync_WithMixedValidAndInvalidSchedules_ShouldScheduleOnlyValidOnes()
    {
        // Arrange
        var validSchedule = _jobScheduleFaker.Generate();
        var invalidSchedule = _jobScheduleFaker
            .RuleFor(j => j.CronExpression, _ => "invalid cron")
            .Generate();

        _schedules.AddRange([validSchedule, invalidSchedule]);

        _ = _logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);

        // Act
        await _service.StartAsync(TestContext.CancellationToken);

        // Assert
        _logger.Received(1).Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString().Contains("CronJob inválido:")),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception, string>>()
        );

        _ = _scheduler.Received(1).ScheduleJob(
            Arg.Is<IJobDetail>(j => j.Key.Name == $"{validSchedule.Id}.job"),
            Arg.Any<ITrigger>(),
            Arg.Any<CancellationToken>()
        );
    }

    [TestMethod]
    public async Task StartAsync_WhenSchedulerAlreadyStarted_ShouldNotCallStartAgain()
    {
        // Arrange
        var schedule = _jobScheduleFaker.Generate();
        _schedules.Add(schedule);

        _ = _scheduler.IsStarted.Returns(true);

        // Act
        await _service.StartAsync(TestContext.CancellationToken);

        // Assert
        await _scheduler.DidNotReceive().Start(Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task StartAsync_ShouldSetSchedulerPublicProperty()
    {
        // Act
        await _service.StartAsync(TestContext.CancellationToken);

        // Assert
        _service.Scheduler.ShouldBeSameAs(_scheduler);
    }

    #endregion

    #region StopAsync Tests

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

    [TestMethod]
    public async Task StopAsync_WhenSchedulerIsNull_ShouldNotThrowException()
    {
        // Arrange
        var service = new QuartzHostedService(_jobFactory, _schedulerFactory);

        // Act & Assert
        await Should.NotThrowAsync(async () => await service.StopAsync(TestContext.CancellationToken));
    }

    [TestMethod]
    public async Task StopAsync_WhenSchedulerIsAlreadyShutdown_ShouldNotCallShutdownAgain()
    {
        // Arrange
        _schedules.Add(_jobScheduleFaker.Generate());

        _ = _scheduler.IsShutdown.Returns(true);

        // Act
        await _service.StartAsync(TestContext.CancellationToken);
        await _service.StopAsync(TestContext.CancellationToken);

        // Assert
        await _scheduler.DidNotReceive().Shutdown(Arg.Any<CancellationToken>());
    }

    #endregion

    #region ScheduleJobAsync Tests

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
            Arg.Is<CancellationToken>(ct => ct == TestContext.CancellationToken)
        );

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
    public async Task ScheduleJobAsync_WhenSchedulerAlreadyStarted_ShouldNotCallStartAgain()
    {
        // Arrange
        var schedule = _jobScheduleFaker.Generate();

        _ = _scheduler.IsStarted.Returns(true);

        // Act
        await _service.ScheduleJobAsync(schedule, TestContext.CancellationToken);

        // Assert
        await _scheduler.DidNotReceive().Start(Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task ScheduleJobAsync_CreatesJobWithCorrectDataMap()
    {
        // Arrange
        var schedule = _jobScheduleFaker.Generate();

        // Act
        await _service.ScheduleJobAsync(schedule, TestContext.CancellationToken);

        // Assert
        _ = _scheduler.Received(1).ScheduleJob(
            Arg.Is<IJobDetail>(j =>
                j.Key.Name == $"{schedule.Id}.job" &&
                j.JobDataMap.GetGuidValue("UserId") == schedule.UserId),
            Arg.Any<ITrigger>(),
            Arg.Any<CancellationToken>()
        );
    }

    #endregion

    #region UpdateJobScheduleAsync Tests

    [TestMethod]
    public async Task UpdateJobScheduleAsync_WithValidSchedule_ShouldDeleteAndRescheduleJob()
    {
        // Arrange
        var schedule = _jobScheduleFaker.Generate();
        var jobKey = new JobKey($"{schedule.Id}.job");

        _ = _scheduler.CheckExists(jobKey, Arg.Any<CancellationToken>()).Returns(true);

        // Act
        await _service.UpdateJobScheduleAsync(schedule, TestContext.CancellationToken);

        // Assert
        _ = await _scheduler.Received(1).CheckExists(jobKey, Arg.Any<CancellationToken>());

        _ = await _scheduler.Received(1).DeleteJob(jobKey, Arg.Any<CancellationToken>());

        _ = _scheduler.Received(1).ScheduleJob(
            Arg.Is<IJobDetail>(j => j.Key.Name == $"{schedule.Id}.job"),
            Arg.Is<ITrigger>(t => t.Key.Name == $"{schedule.Id}.trigger"),
            Arg.Any<CancellationToken>()
        );

        await _scheduler.Received(1).Start(Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task UpdateJobScheduleAsync_WithNullSchedule_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        _ = await Should.ThrowAsync<ArgumentNullException>(async () =>
                await _service.UpdateJobScheduleAsync(null, TestContext.CancellationToken)
            );
    }

    [TestMethod]
    public async Task UpdateJobScheduleAsync_WhenJobDoesNotExist_ShouldOnlyScheduleNewJob()
    {
        // Arrange
        var schedule = _jobScheduleFaker.Generate();
        var jobKey = new JobKey($"{schedule.Id}.job");

        _ = _scheduler.CheckExists(jobKey, Arg.Any<CancellationToken>()).Returns(false);

        // Act
        await _service.UpdateJobScheduleAsync(schedule, TestContext.CancellationToken);

        // Assert
        _ = await _scheduler.DidNotReceive().DeleteJob(Arg.Any<JobKey>(), Arg.Any<CancellationToken>());

        _ = _scheduler.Received(1).ScheduleJob(
            Arg.Is<IJobDetail>(j => j.Key.Name == $"{schedule.Id}.job"),
            Arg.Any<ITrigger>(),
            Arg.Any<CancellationToken>()
        );
    }

    #endregion

    #region DeleteJobAsync Tests

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
    public async Task DeleteJobAsync_WhenJobDoesNotExist_ShouldNotCallDeleteJob()
    {
        // Arrange
        var schedule = _jobScheduleFaker.Generate();
        var jobKey = new JobKey($"{schedule.Id}.job");

        _ = _scheduler.CheckExists(jobKey, Arg.Any<CancellationToken>()).Returns(false);

        // Act
        await _service.DeleteJobAsync(schedule, TestContext.CancellationToken);

        // Assert
        _ = await _scheduler.Received(1).CheckExists(jobKey, Arg.Any<CancellationToken>());

        _ = await _scheduler.DidNotReceive().DeleteJob(Arg.Any<JobKey>(), Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task DeleteJobAsync_InitializesSchedulerIfNull()
    {
        // Arrange
        var schedule = _jobScheduleFaker.Generate();
        var jobKey = new JobKey($"{schedule.Id}.job");

        _ = _scheduler.CheckExists(jobKey, Arg.Any<CancellationToken>()).Returns(false);

        // Act
        await _service.DeleteJobAsync(schedule, TestContext.CancellationToken);

        // Assert
        _service.Scheduler.ShouldBeSameAs(_scheduler);
    }

    #endregion

    #region PauseJobAsync Tests

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
    public async Task PauseJobAsync_InitializesSchedulerIfNull()
    {
        // Arrange
        var schedule = _jobScheduleFaker.Generate();

        // Act
        await _service.PauseJobAsync(schedule, TestContext.CancellationToken);

        // Assert
        _service.Scheduler.ShouldBeSameAs(_scheduler);
    }

    #endregion

    #region ResumeJobAsync Tests

    [TestMethod]
    public async Task ResumeJobAsync_ShouldResumeTriggerAndJob()
    {
        // Arrange
        var schedule = _jobScheduleFaker.Generate();
        _schedules.Add(schedule);

        // Act
        await _service.ResumeJobAsync(schedule, TestContext.CancellationToken);

        // Assert
        _ = _scheduler.Received(1).ResumeJob(Arg.Is<JobKey>(k => k.Name == $"{schedule.Id}.job"),
            Arg.Is<CancellationToken>(ct => ct == TestContext.CancellationToken));

        _ = _scheduler.Received(1).ResumeTrigger(Arg.Is<TriggerKey>(k => k.Name == $"{schedule.Id}.trigger"),
            Arg.Is<CancellationToken>(ct => ct == TestContext.CancellationToken));
    }

    [TestMethod]
    public async Task ResumeJobAsync_NullSchedule_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        _ = await Should.ThrowAsync<ArgumentNullException>(async () =>
                await _service.ResumeJobAsync(null, TestContext.CancellationToken)
            );
    }

    [TestMethod]
    public async Task ResumeJobAsync_InitializesSchedulerIfNull()
    {
        // Arrange
        var schedule = _jobScheduleFaker.Generate();

        // Act
        await _service.ResumeJobAsync(schedule, TestContext.CancellationToken);

        // Assert
        _service.Scheduler.ShouldBeSameAs(_scheduler);
    }

    #endregion

    #region Constructor Tests

    [TestMethod]
    public void Constructor_WithAllParameters_ShouldInitializeService()
    {
        // Act && Assert
        _ = _service.ShouldNotBeNull();
    }

    [TestMethod]
    public void Constructor_WithTwoParameters_ShouldInitializeService()
    {
        // Act
        var service = new QuartzHostedService(_jobFactory, _schedulerFactory);

        // Assert
        _ = service.ShouldNotBeNull();
    }

    [TestMethod]
    public void Constructor_WithNullSchedules_ShouldInitializeWithEmptyList()
    {
        // Act
        var service = new QuartzHostedService(_logger, _schedulerFactory, _jobFactory, null);

        // Assert
        _ = service.ShouldNotBeNull();
    }

    #endregion
}
