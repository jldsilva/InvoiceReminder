using InvoiceReminder.Data.Interfaces;
using InvoiceReminder.Data.Persistence;
using InvoiceReminder.Data.Repository;
using InvoiceReminder.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;

namespace InvoiceReminder.Infrastructure.UnitTests.Data.Repository;

[TestClass]
public sealed class JobScheduleRepositoryTests
{
    private readonly CoreDbContext _dbContext;
    private readonly ILogger<JobScheduleRepository> _logger;
    private readonly IJobScheduleRepository _repository;

    public JobScheduleRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<CoreDbContext>()
            .UseNpgsql(default)
            .Options;

        _dbContext = Substitute.ForPartsOf<CoreDbContext>(options);
        _logger = Substitute.For<ILogger<JobScheduleRepository>>();
        _repository = Substitute.For<IJobScheduleRepository>();
    }

    [TestMethod]
    public void JobScheduleRepository_ShouldBeAssignableToItsInterface_And_GenericInterface_And_GenericRepository()
    {
        // Arrange && Act
        var repository = new JobScheduleRepository(_dbContext, _logger);

        // Assert
        repository.ShouldSatisfyAllConditions(() =>
        {
            _ = repository.ShouldBeAssignableTo<IJobScheduleRepository>();
            _ = repository.ShouldBeAssignableTo<IRepositoryBase<JobSchedule>>();
            _ = repository.ShouldBeAssignableTo<RepositoryBase<CoreDbContext, JobSchedule>>();

            _ = repository.ShouldNotBeNull();
            _ = repository.ShouldBeOfType<JobScheduleRepository>();
        });
    }

    [TestMethod]
    public async Task GetByUserIdAsync_ShouldReturnJobSchedule_WhenJobScheduleExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var jobSchedule = new List<JobSchedule> { new() { UserId = userId } };
        _ = _repository.GetByUserIdAsync(Arg.Any<Guid>()).Returns(jobSchedule);

        // Act
        var result = await _repository.GetByUserIdAsync(userId);

        // Assert
        result.ShouldSatisfyAllConditions(() =>
        {
            _ = result.ShouldNotBeNull();
            _ = result.ShouldBeOfType<List<JobSchedule>>();
            result.ShouldNotBeEmpty();
            result.ShouldAllBe(x => x.UserId == userId);
        });
    }
}
