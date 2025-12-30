using InvoiceReminder.Data.Interfaces;
using InvoiceReminder.Data.Persistence;
using InvoiceReminder.Data.Repository;
using InvoiceReminder.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;

namespace InvoiceReminder.UnitTests.Infrastructure.Data.Repository;

[TestClass]
public sealed class JobScheduleRepositoryTests
{
    private readonly CoreDbContext _dbContext;
    private readonly ILogger<JobScheduleRepository> _logger;

    public JobScheduleRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<CoreDbContext>()
            .UseNpgsql(default)
            .Options;

        _dbContext = Substitute.ForPartsOf<CoreDbContext>(options);
        _logger = Substitute.For<ILogger<JobScheduleRepository>>();
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
            _ = repository.ShouldBeAssignableTo<IBaseRepository<JobSchedule>>();
            _ = repository.ShouldBeAssignableTo<BaseRepository<CoreDbContext, JobSchedule>>();

            _ = repository.ShouldNotBeNull();
            _ = repository.ShouldBeOfType<JobScheduleRepository>();
        });
    }
}
