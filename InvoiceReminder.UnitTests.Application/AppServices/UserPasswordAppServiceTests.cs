using Bogus;
using InvoiceReminder.Application.AppServices;
using InvoiceReminder.Application.Interfaces;
using InvoiceReminder.Application.ViewModels;
using InvoiceReminder.Data.Interfaces;
using InvoiceReminder.Domain.Entities;
using InvoiceReminder.Domain.Services.Configuration;
using Mapster;
using NSubstitute;
using Shouldly;

namespace InvoiceReminder.UnitTests.Application.AppServices;

[TestClass]
public sealed class UserPasswordAppServiceTests
{
    private readonly IConfigurationService _configuration;
    private readonly IUserPasswordRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly Faker _faker;

    public TestContext TestContext { get; set; }

    public UserPasswordAppServiceTests()
    {
        _configuration = Substitute.For<IConfigurationService>();
        _repository = Substitute.For<IUserPasswordRepository>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _faker = new Faker();

        _ = _configuration.GetValue<int>("Security:ParallelismFactor").Returns(2);
    }

    private static Faker<UserPassword> CreateUserPasswordFaker()
    {
        return new Faker<UserPassword>()
            .RuleFor(u => u.Id, faker => faker.Random.Guid())
            .RuleFor(u => u.UserId, faker => faker.Random.Guid())
            .RuleFor(u => u.PasswordHash, faker => faker.Random.AlphaNumeric(88))
            .RuleFor(u => u.PasswordSalt, faker => faker.Random.AlphaNumeric(24))
            .RuleFor(u => u.CreatedAt, faker => faker.Date.Past().ToUniversalTime())
            .RuleFor(u => u.UpdatedAt, faker => faker.Date.Recent().ToUniversalTime());
    }

    private static Faker<UserPasswordViewModel> CreateUserPasswordViewModelFaker()
    {
        return new Faker<UserPasswordViewModel>()
            .RuleFor(u => u.Id, faker => faker.Random.Guid())
            .RuleFor(u => u.UserId, faker => faker.Random.Guid())
            .RuleFor(u => u.PasswordHash, faker => faker.Internet.Password(12, false, "[A-Z]", "abc123"))
            .RuleFor(u => u.PasswordSalt, faker => faker.Random.AlphaNumeric(24))
            .RuleFor(u => u.CreatedAt, faker => faker.Date.Past().ToUniversalTime())
            .RuleFor(u => u.UpdatedAt, faker => faker.Date.Recent().ToUniversalTime());
    }

    [TestMethod]
    public void UserPasswordAppService_ShouldBeAssignableToItsInterface_And_GenericInterface_And_GenericAppService()
    {
        // Arrange && Act
        var appService = new UserPasswordAppService(_configuration, _repository, _unitOfWork);

        // Assert
        appService.ShouldSatisfyAllConditions(() =>
        {
            _ = appService.ShouldBeAssignableTo<IUserPasswordAppService>();
            _ = appService.ShouldNotBeNull();
            _ = appService.ShouldBeOfType<UserPasswordAppService>();
        });
    }

    #region AddAsync Tests

    [TestMethod]
    public async Task AddAsync_Should_Hash_Password_Before_Adding()
    {
        // Arrange
        var appService = new UserPasswordAppService(_configuration, _repository, _unitOfWork);
        var viewModel = CreateUserPasswordViewModelFaker().Generate();
        var plainPassword = viewModel.PasswordHash;

        _ = _repository.AddAsync(Arg.Any<UserPassword>(), Arg.Any<CancellationToken>())
            .Returns(x => x.Arg<UserPassword>());

        _ = _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Act
        var result = await appService.AddAsync(viewModel, TestContext.CancellationToken);

        // Assert
        _ = _repository.Received(1).AddAsync(Arg.Any<UserPassword>(), Arg.Any<CancellationToken>());
        _ = _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());

        result.ShouldSatisfyAllConditions(() =>
        {
            result.IsSuccess.ShouldBeTrue();
            _ = result.Value.ShouldNotBeNull();
            _ = result.Value.ShouldBeOfType<UserPasswordViewModel>();
            result.Value.UserId.ShouldBe(viewModel.UserId);
            // Verify that password was hashed (different from original)
            result.Value.PasswordHash.ShouldNotBe(plainPassword);
        });
    }

    [TestMethod]
    public async Task AddAsync_Should_Return_Failure_When_ViewModel_Is_Null()
    {
        // Arrange
        var appService = new UserPasswordAppService(_configuration, _repository, _unitOfWork);

        // Act
        var result = await appService.AddAsync(null, TestContext.CancellationToken);

        // Assert
        result.ShouldSatisfyAllConditions(() =>
        {
            result.IsSuccess.ShouldBeFalse();
            result.Value.ShouldBeNull();
            result.Error.ShouldBe("The provided obejct data was Null.");
        });
    }

    [TestMethod]
    public async Task AddAsync_Should_Generate_Different_Hash_For_Same_Password()
    {
        // Arrange
        var appService = new UserPasswordAppService(_configuration, _repository, _unitOfWork);
        var password = _faker.Internet.Password(12, false, "[A-Z]", "abc123");

        var viewModel1 = CreateUserPasswordViewModelFaker()
            .RuleFor(u => u.PasswordHash, _ => password)
            .Generate();

        var viewModel2 = CreateUserPasswordViewModelFaker()
            .RuleFor(u => u.PasswordHash, _ => password)
            .Generate();

        _ = _repository.AddAsync(Arg.Any<UserPassword>(), Arg.Any<CancellationToken>())
            .Returns(x => x.Arg<UserPassword>());

        _ = _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Act
        var result1 = await appService.AddAsync(viewModel1, TestContext.CancellationToken);
        var result2 = await appService.AddAsync(viewModel2, TestContext.CancellationToken);

        // Assert
        result1.ShouldSatisfyAllConditions(() =>
        {
            result1.IsSuccess.ShouldBeTrue();
            result2.IsSuccess.ShouldBeTrue();
            // Argon2id should produce different hashes even for the same password
            result1.Value.PasswordHash.ShouldNotBe(result2.Value.PasswordHash);
        });
    }

    #endregion

    #region BulkInsertAsync Tests

    [TestMethod]
    public async Task BulkInsertAsync_Should_Hash_All_Passwords_Before_Inserting()
    {
        // Arrange
        var appService = new UserPasswordAppService(_configuration, _repository, _unitOfWork);
        var viewModels = CreateUserPasswordViewModelFaker().Generate(3).ToList();
        var plainPasswords = viewModels.Select(v => v.PasswordHash).ToList();

        _ = _repository.BulkInsertAsync(Arg.Any<ICollection<UserPassword>>(), Arg.Any<CancellationToken>())
            .Returns(3);

        // Act
        var result = await appService.BulkInsertAsync(viewModels, TestContext.CancellationToken);

        // Assert
        _ = _repository.Received(1).BulkInsertAsync(Arg.Any<ICollection<UserPassword>>(), Arg.Any<CancellationToken>());

        result.ShouldSatisfyAllConditions(() =>
        {
            result.IsSuccess.ShouldBeTrue();
            result.Value.ShouldBe(3);
            // All original passwords should have been hashed (changed)
            viewModels.Any(v => plainPasswords.Contains(v.PasswordHash)).ShouldBeFalse();
        });
    }

    [TestMethod]
    public async Task BulkInsertAsync_Should_Return_Failure_When_ViewModels_Are_Null()
    {
        // Arrange
        var appService = new UserPasswordAppService(_configuration, _repository, _unitOfWork);

        // Act
        var result = await appService.BulkInsertAsync(null, TestContext.CancellationToken);

        // Assert
        result.ShouldSatisfyAllConditions(() =>
        {
            result.IsSuccess.ShouldBeFalse();
            result.Value.ShouldBe(0);
            result.Error.ShouldBe("The provided object data was Null or Empty.");
        });
    }

    [TestMethod]
    public async Task BulkInsertAsync_Should_Return_Failure_When_ViewModels_Are_Empty()
    {
        // Arrange
        var appService = new UserPasswordAppService(_configuration, _repository, _unitOfWork);

        // Act
        var result = await appService.BulkInsertAsync([], TestContext.CancellationToken);

        // Assert
        result.ShouldSatisfyAllConditions(() =>
        {
            result.IsSuccess.ShouldBeFalse();
            result.Value.ShouldBe(0);
            result.Error.ShouldBe("The provided object data was Null or Empty.");
        });
    }

    #endregion

    #region UpdateAsync Tests

    [TestMethod]
    public async Task UpdateAsync_Should_Hash_Password_Before_Updating()
    {
        // Arrange
        var appService = new UserPasswordAppService(_configuration, _repository, _unitOfWork);
        var viewModel = CreateUserPasswordViewModelFaker().Generate();
        var plainPassword = viewModel.PasswordHash;

        _ = _repository.Update(Arg.Any<UserPassword>())
            .Returns(viewModel.Adapt<UserPassword>());

        _ = _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Act
        var result = await appService.UpdateAsync(viewModel, TestContext.CancellationToken);

        // Assert
        _ = _repository.Received(1).Update(Arg.Any<UserPassword>());
        _ = _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());

        result.ShouldSatisfyAllConditions(() =>
        {
            result.IsSuccess.ShouldBeTrue();
            _ = result.Value.ShouldNotBeNull();
            _ = result.Value.ShouldBeOfType<UserPasswordViewModel>();
            // Verify that password was hashed (different from original)
            result.Value.PasswordHash.ShouldNotBe(plainPassword);
        });
    }

    [TestMethod]
    public async Task UpdateAsync_Should_Return_Failure_When_ViewModel_Is_Null()
    {
        // Arrange
        var appService = new UserPasswordAppService(_configuration, _repository, _unitOfWork);

        // Act
        var result = await appService.UpdateAsync(null, TestContext.CancellationToken);

        // Assert
        result.ShouldSatisfyAllConditions(() =>
        {
            result.IsSuccess.ShouldBeFalse();
            result.Value.ShouldBeNull();
            result.Error.ShouldBe("The provided object data was Null.");
        });
    }

    #endregion

    #region GetByUserIdAsync Tests

    [TestMethod]
    public async Task GetByUserIdAsync_Should_Return_Success_When_UserPassword_Exists()
    {
        // Arrange
        var appService = new UserPasswordAppService(_configuration, _repository, _unitOfWork);
        var userId = Guid.NewGuid();
        var userPassword = CreateUserPasswordFaker()
            .RuleFor(u => u.UserId, _ => userId)
            .Generate();

        _ = _repository.GetByUserIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(userPassword);

        // Act
        var result = await appService.GetByUserIdAsync(userId, TestContext.CancellationToken);

        // Assert
        _ = _repository.Received(1).GetByUserIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());

        result.ShouldSatisfyAllConditions(() =>
        {
            result.IsSuccess.ShouldBeTrue();
            _ = result.Value.ShouldNotBeNull();
            _ = result.Value.ShouldBeOfType<UserPasswordViewModel>();
            result.Value.UserId.ShouldBe(userId);
            result.Value.ShouldBeEquivalentTo(userPassword.Adapt<UserPasswordViewModel>());
        });
    }

    [TestMethod]
    public async Task GetByUserIdAsync_Should_Return_Failure_When_UserPassword_NotExists()
    {
        // Arrange
        var appService = new UserPasswordAppService(_configuration, _repository, _unitOfWork);
        var userId = Guid.NewGuid();

        _ = _repository.GetByUserIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((UserPassword)null);

        // Act
        var result = await appService.GetByUserIdAsync(userId, TestContext.CancellationToken);

        // Assert
        _ = _repository.Received(1).GetByUserIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());

        result.ShouldSatisfyAllConditions(() =>
        {
            result.IsSuccess.ShouldBeFalse();
            result.Value.ShouldBeNull();
            result.Error.ShouldBe("No user password found for the specified user ID.");
        });
    }

    [TestMethod]
    public async Task GetByUserIdAsync_Should_Adapt_Entity_To_ViewModel()
    {
        // Arrange
        var appService = new UserPasswordAppService(_configuration, _repository, _unitOfWork);
        var userPassword = CreateUserPasswordFaker().Generate();

        _ = _repository.GetByUserIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(userPassword);

        // Act
        var result = await appService.GetByUserIdAsync(userPassword.UserId, TestContext.CancellationToken);

        // Assert
        result.ShouldSatisfyAllConditions(() =>
        {
            result.IsSuccess.ShouldBeTrue();
            _ = result.Value.ShouldNotBeNull();
            result.Value.Id.ShouldBe(userPassword.Id);
            result.Value.UserId.ShouldBe(userPassword.UserId);
            result.Value.CreatedAt.ShouldBe(userPassword.CreatedAt);
            result.Value.UpdatedAt.ShouldBe(userPassword.UpdatedAt);
        });
    }

    #endregion
}
