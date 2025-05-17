using InvoiceReminder.Application.AppServices;
using InvoiceReminder.Application.Interfaces;
using InvoiceReminder.Application.ViewModels;
using InvoiceReminder.Data.Interfaces;
using InvoiceReminder.Domain.Entities;
using NSubstitute;
using Quartz;
using Quartz.Spi;
using Shouldly;

namespace InvoiceReminder.Application.UnitTests.AppServices;

[TestClass]
public sealed class JobScheduleAppServiceTests
{
    private readonly IJobScheduleRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IJobFactory _jobFactory;
    private readonly ISchedulerFactory _schedulerFactory;

    public JobScheduleAppServiceTests()
    {
        _repository = Substitute.For<IJobScheduleRepository>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _jobFactory = Substitute.For<IJobFactory>();
        _schedulerFactory = Substitute.For<ISchedulerFactory>();
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
        var result = await appService.AddNewJobAsync(viewModel);

        // Assert
        _ = await _repository.DidNotReceive().AddAsync(Arg.Any<JobSchedule>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync();

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
        var viewModel = new JobScheduleViewModel
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            CronExpression = "0 0/5 * * * ?",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act
        var result = await appService.AddNewJobAsync(viewModel);

        // Assert
        _ = await _repository.Received(1).AddAsync(Arg.Is<JobSchedule>(x => x.Id == viewModel.Id));
        await _unitOfWork.Received(1).SaveChangesAsync();

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
        var userId = Guid.NewGuid();
        var jobSchedules = new List<JobSchedule>
        {
            new() {
                Id = Guid.NewGuid(),
                UserId = userId,
                CronExpression = "0 0/5 * * * ?",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        _ = _repository.GetByUserIdAsync(Arg.Any<Guid>()).Returns(jobSchedules);

        // Act
        var result = await appService.GetByUserIdAsync(userId);

        // Assert
        _ = await _repository.Received(1).GetByUserIdAsync(userId);

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
        var userId = Guid.NewGuid();

        _ = _repository.GetByUserIdAsync(Arg.Any<Guid>()).Returns([]);

        // Act
        var result = await appService.GetByUserIdAsync(userId);

        // Assert
        _ = _repository.Received(1).GetByUserIdAsync(userId);

        result.ShouldSatisfyAllConditions(() =>
        {
            result.IsSuccess.ShouldBeFalse();
            result.Error.ShouldBe("Empty Result");
        });
    }
}
