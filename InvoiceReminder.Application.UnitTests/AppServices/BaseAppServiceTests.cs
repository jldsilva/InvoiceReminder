using InvoiceReminder.Application.AppServices;
using InvoiceReminder.Data.Interfaces;
using Mapster;
using NSubstitute;
using Shouldly;

namespace InvoiceReminder.Application.UnitTests.AppServices;

[TestClass]
public sealed class BaseAppServiceTests
{
    private readonly BaseAppService<TestEntity, TestEntityViewModel> _appService;
    private readonly IBaseRepository<TestEntity> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public TestContext TestContext { get; set; }

    public BaseAppServiceTests()
    {
        _repository = Substitute.For<IBaseRepository<TestEntity>>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _appService = new BaseAppService<TestEntity, TestEntityViewModel>(_repository, _unitOfWork);
    }

    [TestMethod]
    public async Task AddAsync_Should_Return_Success_When_Entity_Is_Valid()
    {
        // Arrange
        var viewModel = new TestEntityViewModel { Name = "Test" };

        _ = _repository.AddAsync(Arg.Any<TestEntity>(), Arg.Any<CancellationToken>())
            .Returns(viewModel.Adapt<TestEntity>());

        _ = _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Act
        var result = await _appService.AddAsync(viewModel, TestContext.CancellationToken);

        // Assert
        _ = _repository.Received(1).AddAsync(Arg.Any<TestEntity>(), Arg.Any<CancellationToken>());

        result.ShouldSatisfyAllConditions(() =>
        {
            result.IsSuccess.ShouldBeTrue();
            _ = result.Value.ShouldNotBeNull();
            result.Value.Name.ShouldBe(viewModel.Name);
        });
    }

    [TestMethod]
    public async Task AddAsync_Should_Return_Failure_When_ViewModel_Is_Null()
    {
        // Arrange && Act
        var result = await _appService.AddAsync(null, TestContext.CancellationToken);

        // Assert
        result.ShouldSatisfyAllConditions(() =>
        {
            result.IsSuccess.ShouldBeFalse();
            result.Value.ShouldBeNull();
            result.Error.ShouldBe("Parameter viewModel was Null.");
        });
    }

    [TestMethod]
    public async Task BulkInsertAsync_Should_Return_Failure_When_ViewModels_Are_Null()
    {
        // Arrange && Act
        var result = await _appService.BulkInsertAsync(null, TestContext.CancellationToken);

        // Assert
        result.ShouldSatisfyAllConditions(() =>
        {
            result.IsSuccess.ShouldBeFalse();
            result.Value.ShouldBe(0);
            result.Error.ShouldBe("Parameter viewModels was Null or Empty.");
        });
    }

    [TestMethod]
    public async Task BulkInsertAsync_Should_Return_Success_When_ViewModels_Are_Valid()
    {
        // Arrange
        var viewModels = new List<TestEntityViewModel>
        {
            new() { Name = "Test1" },
            new() { Name = "Test2" }
        };

        _ = _repository.BulkInsertAsync(Arg.Any<ICollection<TestEntity>>(), Arg.Any<CancellationToken>())
            .Returns(viewModels.Count);

        _ = _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Act
        var result = await _appService.BulkInsertAsync(viewModels, TestContext.CancellationToken);

        // Assert
        _ = _repository.Received(1).BulkInsertAsync(Arg.Any<ICollection<TestEntity>>(), Arg.Any<CancellationToken>());

        result.ShouldSatisfyAllConditions(() =>
        {
            result.IsSuccess.ShouldBeTrue();
            result.Value.ShouldBe(viewModels.Count);
        });
    }

    [TestMethod]
    public void GetAll_Should_Return_Failure_When_No_Entities_Exist()
    {
        // Arrange
        _ = _repository.GetAll().Returns([]);

        // Act
        var result = _appService.GetAll();

        // Assert
        _ = _repository.Received(1).GetAll();

        result.ShouldSatisfyAllConditions(() =>
        {
            result.IsSuccess.ShouldBeFalse();
            result.Value.ShouldBeNull();
            result.Error.ShouldContain("Empty Result.");
        });
    }

    [TestMethod]
    public void GetAll_Should_Return_Success_When_Entities_Exist()
    {
        // Arrange
        var entities = new List<TestEntity>
        {
            new() { Name = "Entity1" },
            new() { Name = "Entity2" }
        };

        _ = _repository.GetAll().Returns(entities);

        // Act
        var result = _appService.GetAll();

        // Assert
        _ = _repository.Received(1).GetAll();

        result.ShouldSatisfyAllConditions(() =>
        {
            result.IsSuccess.ShouldBeTrue();
            _ = result.Value.ShouldNotBeNull();
            result.Value.Count().ShouldBe(2);
        });
    }

    [TestMethod]
    public async Task GetByIdAsync_Should_Return_Success_When_Entity_Exists()
    {
        // Arrange
        var id = Guid.Parse("42ab20e5-0742-4d32-a76e-7c45fd74dac3");
        var entity = new TestEntity { Name = "Test" };

        _ = _repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(entity);

        // Act
        var result = await _appService.GetByIdAsync(id, TestContext.CancellationToken);

        // Assert
        _ = _repository.Received(1).GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());

        result.ShouldSatisfyAllConditions(() =>
        {
            result.IsSuccess.ShouldBeTrue();
            _ = result.Value.ShouldNotBeNull();
            result.Value.Name.ShouldBe(entity.Name);
        });
    }

    [TestMethod]
    public async Task GetByIdAsync_Should_Return_Failure_When_Entity_Does_Not_Exist()
    {
        // Arrange
        var id = Guid.Parse("42ab20e5-0742-4d32-a76e-7c45fd74dac3");
        _ = _repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<TestEntity>(null));

        // Act
        var result = await _appService.GetByIdAsync(id, TestContext.CancellationToken);

        // Assert
        _ = _repository.Received(1).GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());

        result.ShouldSatisfyAllConditions(() =>
        {
            result.IsSuccess.ShouldBeFalse();
            result.Value.ShouldBeNull();
            result.Error.ShouldContain($"TEntityViewModel with id {id} not Found.");
        });
    }

    [TestMethod]
    public async Task RemoveAsync_Should_Return_Success_When_Entity_Exists()
    {
        // Arrange
        var entity = new TestEntityViewModel { Name = "Test" };

        _repository.Remove(Arg.Any<TestEntity>());
        _ = _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

        // Act
        var result = await _appService.RemoveAsync(entity, TestContext.CancellationToken);

        // Assert
        _repository.Received(1).Remove(Arg.Any<TestEntity>());

        result.ShouldSatisfyAllConditions(() =>
        {
            result.IsSuccess.ShouldBeTrue();
            result.Value.ShouldBeNull();
        });
    }

    [TestMethod]
    public async Task RemoveAsync_Should_Return_Failure_When_ViewModel_Is_Null()
    {
        // Arrange && Act
        var result = await _appService.RemoveAsync(null, TestContext.CancellationToken);

        // Assert
        result.ShouldSatisfyAllConditions(() =>
        {
            result.IsSuccess.ShouldBeFalse();
            result.Value.ShouldBeNull();
            result.Error.ShouldBe("Parameter viewModel was Null.");
        });
    }

    [TestMethod]
    public async Task UpdateAsync_Should_Return_Success_When_ViewModel_Is_Valid()
    {
        // Arrange
        var viewModel = new TestEntityViewModel { Name = "Updated" };

        _ = _repository.Update(Arg.Any<TestEntity>()).Returns(viewModel.Adapt<TestEntity>());
        _ = _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

        // Act
        var result = await _appService.UpdateAsync(viewModel, TestContext.CancellationToken);

        // Assert
        _ = _repository.Received(1).Update(Arg.Any<TestEntity>());

        result.ShouldSatisfyAllConditions(() =>
        {
            result.IsSuccess.ShouldBeTrue();
            _ = result.Value.ShouldNotBeNull();
            result.Value.Name.ShouldBe(viewModel.Name);
        });
    }

    [TestMethod]
    public async Task UpdateAsync_Should_Return_Failure_When_ViewModel_Is_Null()
    {
        // Arrange && Act
        var result = await _appService.UpdateAsync(null, TestContext.CancellationToken);

        // Assert
        result.ShouldSatisfyAllConditions(() =>
        {
            result.IsSuccess.ShouldBeFalse();
            result.Value.ShouldBeNull();
            result.Error.ShouldBe("Parameter viewModel was Null.");
        });
    }
}

public class TestEntity
{
    public string Name { get; set; }
}

public class TestEntityViewModel
{
    public string Name { get; set; }
}
