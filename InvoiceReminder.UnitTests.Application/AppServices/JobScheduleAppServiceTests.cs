using Bogus;
using InvoiceReminder.Application.AppServices;
using InvoiceReminder.Application.Interfaces;
using InvoiceReminder.Application.ViewModels;
using InvoiceReminder.Data.Interfaces;
using InvoiceReminder.Domain.Entities;
using NSubstitute;
using Quartz;
using Quartz.Spi;
using Shouldly;

namespace InvoiceReminder.UnitTests.Application.AppServices;

[TestClass]
public sealed class JobScheduleAppServiceTests
{
    private readonly IJobScheduleRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IJobFactory _jobFactory;
    private readonly ISchedulerFactory _schedulerFactory;
    private readonly Faker _faker;
    private readonly string[] _validCronExpressions;

    public TestContext TestContext { get; set; }

    public JobScheduleAppServiceTests()
    {
        _repository = Substitute.For<IJobScheduleRepository>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _jobFactory = Substitute.For<IJobFactory>();
        _schedulerFactory = Substitute.For<ISchedulerFactory>();
        _faker = new Faker();
        _validCronExpressions = [
            "0 0/5 * * * ?",// Every 5 minutes
            "0 0 * * * ?",  // Every hour
            "0 0 0 * * ?",  // Every day at midnight
            "0 0 0 ? * MON",// Every Monday
            "0 0 0 1 * ?",  // First day of month
            "0 0 0 1 1 ?",  // Every January 1st
            "0 0 12 * * ?"  // Every noon
        ];
    }

    private Faker<JobSchedule> CreateJobScheduleFaker()
    {
        return new Faker<JobSchedule>()
            .RuleFor(j => j.Id, faker => faker.Random.Guid())
            .RuleFor(j => j.UserId, faker => faker.Random.Guid())
            .RuleFor(j => j.CronExpression, faker => faker.PickRandom(_validCronExpressions))
            .RuleFor(j => j.CreatedAt, faker => faker.Date.Past(refDate: DateTime.UtcNow).ToUniversalTime())
            .RuleFor(j => j.UpdatedAt, (faker, j) => faker.Date.Between(j.CreatedAt, DateTime.UtcNow).ToUniversalTime());
    }

    private Faker<JobScheduleViewModel> CreateJobScheduleViewModelFaker()
    {
        return new Faker<JobScheduleViewModel>()
            .RuleFor(j => j.Id, faker => faker.Random.Guid())
            .RuleFor(j => j.UserId, faker => faker.Random.Guid())
            .RuleFor(j => j.CronExpression, faker => faker.PickRandom(_validCronExpressions))
            .RuleFor(j => j.CreatedAt, faker => faker.Date.Past(refDate: DateTime.UtcNow).ToUniversalTime())
            .RuleFor(j => j.UpdatedAt, (faker, j) => faker.Date.Between(j.CreatedAt, DateTime.UtcNow).ToUniversalTime());
    }

    [TestMethod]
    public void JobScheduleAppService_ShouldBeAssignableToItsInterface_And_GenericInterface_And_GenericAppService()
    {
        // Arrange && Act
        var appService = new JobScheduleAppService(_repository, _schedulerFactory, _jobFactory, _unitOfWork);

        // Assert
        appService.ShouldSatisfyAllConditions(() =>
        {
            _ = appService.ShouldBeAssignableTo<IJobScheduleAppService>();
            _ = appService.ShouldNotBeNull();
            _ = appService.ShouldBeOfType<JobScheduleAppService>();
        });
    }

    [TestMethod]
    public async Task AddNewJobAsync_ShouldReturnFailure_WhenViewModelIsNull()
    {
        // Arrange
        var appService = new JobScheduleAppService(_repository, _schedulerFactory, _jobFactory, _unitOfWork);
        JobScheduleViewModel viewModel = null;

        // Act
        var result = await appService.AddNewJobAsync(viewModel, TestContext.CancellationToken);

        // Assert
        _ = await _repository.DidNotReceive().AddAsync(Arg.Any<JobSchedule>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());

        result.ShouldSatisfyAllConditions(() =>
        {
            _ = result.ShouldNotBeNull();
            result.IsSuccess.ShouldBeFalse();
            result.Error.ShouldBe("Parameter viewModel was Null.");
        });
    }

    [TestMethod]
    public async Task AddNewJobAsync_ShouldReturnSuccess_WhenViewModelIsValid()
    {
        // Arrange
        var appService = new JobScheduleAppService(_repository, _schedulerFactory, _jobFactory, _unitOfWork);
        var viewModel = CreateJobScheduleViewModelFaker().Generate();

        // Act
        var result = await appService.AddNewJobAsync(viewModel, TestContext.CancellationToken);

        // Assert
        _ = await _repository.Received(1)
            .AddAsync(Arg.Is<JobSchedule>(x => x.Id == viewModel.Id), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());

        result.ShouldSatisfyAllConditions(() =>
        {
            result.IsSuccess.ShouldBeTrue();
            _ = result.ShouldNotBeNull();
            _ = result.Value.ShouldNotBeNull();
            result.Value.Id.ShouldBe(viewModel.Id);
            result.Value.UserId.ShouldBe(viewModel.UserId);
            result.Value.CronExpression.ShouldBe(viewModel.CronExpression);
        });
    }

    [TestMethod]
    public async Task GetByUserIdAsync_ShouldReturnSuccess_WhenUserHasJobSchedules()
    {
        // Arrange
        var appService = new JobScheduleAppService(_repository, _schedulerFactory, _jobFactory, _unitOfWork);
        var userId = _faker.Random.Guid();
        var jobSchedules = CreateJobScheduleFaker()
            .RuleFor(j => j.UserId, userId)
            .Generate(3)
            .ToList();

        _ = _repository.GetByUserIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(jobSchedules);

        // Act
        var result = await appService.GetByUserIdAsync(userId, TestContext.CancellationToken);

        // Assert
        _ = await _repository.Received(1).GetByUserIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());

        result.ShouldSatisfyAllConditions(() =>
        {
            result.IsSuccess.ShouldBeTrue();
            _ = result.ShouldNotBeNull();
            _ = result.Value.ShouldNotBeNull();
            result.Value.Count().ShouldBe(jobSchedules.Count);
            result.Value.ShouldAllBe(x => x.UserId == userId);
        });
    }

    [TestMethod]
    public async Task GetByUserIdAsync_ShouldReturnFailure_WhenUserHasNoJobSchedules()
    {
        // Arrange
        var appService = new JobScheduleAppService(_repository, _schedulerFactory, _jobFactory, _unitOfWork);
        var userId = _faker.Random.Guid();

        _ = _repository.GetByUserIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns([]);

        // Act
        var result = await appService.GetByUserIdAsync(userId, TestContext.CancellationToken);

        // Assert
        _ = _repository.Received(1).GetByUserIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());

        result.ShouldSatisfyAllConditions(() =>
        {
            result.IsSuccess.ShouldBeFalse();
            result.Error.ShouldBe("Empty Result");
        });
    }
}
