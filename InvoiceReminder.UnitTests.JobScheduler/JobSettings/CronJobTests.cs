using InvoiceReminder.ExternalServices.SendMessage;
using InvoiceReminder.JobScheduler.JobSettings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Quartz;
using Shouldly;

namespace InvoiceReminder.UnitTests.JobScheduler.JobSettings;

[TestClass]
public sealed class CronJobTests
{
    private readonly ILogger<CronJob> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IServiceScope _serviceScope;
    private readonly IServiceProvider _serviceProvider;
    private readonly ISendMessageService _sendMessageService;
    private readonly IJobExecutionContext _jobExecutionContext;
    private readonly IJobDetail _jobDetail;
    private readonly JobDataMap _jobDataMap;

    public TestContext TestContext { get; set; }

    public CronJobTests()
    {
        _logger = Substitute.For<ILogger<CronJob>>();
        _serviceScopeFactory = Substitute.For<IServiceScopeFactory>();
        _serviceScope = Substitute.For<IServiceScope>();
        _sendMessageService = Substitute.For<ISendMessageService>();
        _jobExecutionContext = Substitute.For<IJobExecutionContext>();
        _jobDetail = Substitute.For<IJobDetail>();
        _jobDataMap = [.. (IDictionary<string, object>)new Dictionary<string, object> { { "UserId", Guid.NewGuid() } }];

        var services = new ServiceCollection();
        _ = services.AddSingleton(provider => _sendMessageService);
        _serviceProvider = services.BuildServiceProvider();

        _ = _serviceScopeFactory.CreateScope().Returns(_serviceScope);
        _ = _serviceScope.ServiceProvider.Returns(_serviceProvider);
        _ = _jobExecutionContext.MergedJobDataMap.Returns(_jobDataMap);
        _ = _jobExecutionContext.JobDetail.Returns(_jobDetail);
        _ = _jobDetail.Description.Returns("Test Job Description");
    }

    [TestMethod]
    public async Task Execute_ShouldCreateScopeResolveServiceAndSendMessage()
    {
        // Arrange
        var cronJob = new CronJob(_logger, _serviceScopeFactory);

        _ = _logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);

        // Act
        await cronJob.Execute(_jobExecutionContext);

        // Assert
        _ = _serviceScopeFactory.Received(1).CreateScope();

        _ = _sendMessageService.Received(1).SendMessage(Arg.Any<Guid>(), Arg.Any<CancellationToken>());

        _logger.Received(1).Log(
             LogLevel.Information,
             Arg.Any<EventId>(),
             Arg.Any<object>(),
             null,
             Arg.Any<Func<object, Exception, string>>()
         );
    }

    [TestMethod]
    public async Task Execute_NullContext_ShouldThrowArgumentNullException()
    {
        // Arrange
        var cronJob = new CronJob(_logger, _serviceScopeFactory);

        // Act & Assert
        _ = await Should.ThrowAsync<ArgumentNullException>(async () => await cronJob.Execute(null));
    }

    [TestMethod]
    public async Task Execute_MissingUserIdInJobDataMap_ShouldThrowNullReferenceException()
    {
        // Arrange
        var cronJob = new CronJob(_logger, _serviceScopeFactory);
        _ = _jobExecutionContext.MergedJobDataMap.Returns([]);

        // Act & Assert
        _ = await Should.ThrowAsync<NullReferenceException>(async () => await cronJob.Execute(_jobExecutionContext));
    }

    [TestMethod]
    public async Task Execute_SendMessageFails_ShouldNotThrowExceptionAndStillLog()
    {
        // Arrange
        var cronJob = new CronJob(_logger, _serviceScopeFactory);

        _ = _sendMessageService.SendMessage(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns("Total messages sent: 0");

        _ = _logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);

        // Act
        await cronJob.Execute(_jobExecutionContext);

        // Assert
        _ = _serviceScopeFactory.Received(1).CreateScope();
        _ = _sendMessageService.Received(1).SendMessage(Arg.Any<Guid>(), Arg.Any<CancellationToken>());

        _logger.Received(1).Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString().Contains("Test Job Description triggered...")),
            null,
            Arg.Any<Func<object, Exception, string>>()
        );
    }
}
