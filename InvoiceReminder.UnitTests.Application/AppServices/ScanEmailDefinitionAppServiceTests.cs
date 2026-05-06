using Bogus;
using InvoiceReminder.Application.AppServices;
using InvoiceReminder.Application.Interfaces;
using InvoiceReminder.Application.ViewModels;
using InvoiceReminder.Data.Interfaces;
using InvoiceReminder.Domain.Entities;
using InvoiceReminder.Domain.Enums;
using InvoiceReminder.Domain.Services.Configuration;
using Mapster;
using NSubstitute;
using Shouldly;

namespace InvoiceReminder.UnitTests.Application.AppServices;

[TestClass]
public sealed class ScanEmailDefinitionAppServiceTests
{
    private readonly IConfigurationService _configuration;
    private readonly IScanEmailDefinitionRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly Faker _faker;

    public TestContext TestContext { get; set; }

    public ScanEmailDefinitionAppServiceTests()
    {
        _configuration = Substitute.For<IConfigurationService>();
        _repository = Substitute.For<IScanEmailDefinitionRepository>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _faker = new Faker();
    }

    private static Faker<ScanEmailDefinition> CreateScanEmailDefinitionFaker()
    {
        return new Faker<ScanEmailDefinition>()
            .RuleFor(s => s.Id, faker => faker.Random.Guid())
            .RuleFor(s => s.UserId, faker => faker.Random.Guid())
            .RuleFor(s => s.InvoiceType, faker => faker.PickRandom<InvoiceType>())
            .RuleFor(s => s.Beneficiary, faker => faker.Person.FullName)
            .RuleFor(s => s.Description, faker => faker.Lorem.Sentence())
            .RuleFor(s => s.SenderEmailAddress, faker => faker.Internet.Email())
            .RuleFor(s => s.AttachmentFileName, faker => faker.System.FileName("pdf"))
            .RuleFor(s => s.FilePassword, faker => faker.Internet.Password())
            .RuleFor(s => s.CreatedAt, faker => faker.Date.Past().ToUniversalTime())
            .RuleFor(s => s.UpdatedAt, faker => faker.Date.Recent().ToUniversalTime());
    }

    [TestMethod]
    public void ScanEmailDefinitionAppService_ShouldBeAssignableToItsInterface_And_GenericInterface_And_GenericAppService()
    {
        // Arrange && Act
        var appService = new ScanEmailDefinitionAppService(_configuration, _repository, _unitOfWork);

        // Assert
        appService.ShouldSatisfyAllConditions(() =>
        {
            _ = appService.ShouldBeAssignableTo<IScanEmailDefinitionAppService>();
            _ = appService.ShouldNotBeNull();
            _ = appService.ShouldBeOfType<ScanEmailDefinitionAppService>();
        });
    }

    [TestMethod]
    public async Task GetBySenderBeneficiaryAsync_WhenSenderBeneficiaryExists_ShouldReturnSuccess_WithResultFound()
    {
        // Arrange
        var appService = new ScanEmailDefinitionAppService(_configuration, _repository, _unitOfWork);
        var userId = _faker.Random.Guid();
        var beneficiary = _faker.Person.FullName;
        var scanEmailDefinition = CreateScanEmailDefinitionFaker()
            .RuleFor(s => s.UserId, userId)
            .RuleFor(s => s.Beneficiary, beneficiary)
            .Generate();

        _ = _repository.GetBySenderBeneficiaryAsync(Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(scanEmailDefinition);

        // Act
        var result = await appService.GetBySenderBeneficiaryAsync(beneficiary, userId, TestContext.CancellationToken);

        // Assert
        _ = _repository.Received(1)
            .GetBySenderBeneficiaryAsync(Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());

        result.ShouldSatisfyAllConditions(() =>
        {
            result.IsSuccess.ShouldBeTrue();
            _ = result.ShouldNotBeNull();
            _ = result.Value.ShouldNotBeNull();
            _ = result.Value.ShouldBeOfType<ScanEmailDefinitionViewModel>();
            result.Value.ShouldBeEquivalentTo(scanEmailDefinition.Adapt<ScanEmailDefinitionViewModel>());
        });
    }

    [TestMethod]
    public async Task GetBySenderBeneficiaryAsync_WhenSenderBeneficiaryNotExists_ShouldReturnFailure_WithResultNotFound()
    {
        // Arrange
        var appService = new ScanEmailDefinitionAppService(_configuration, _repository, _unitOfWork);
        var beneficiary = _faker.Person.FullName;
        var userId = _faker.Random.Guid();

        _ = _repository.GetBySenderBeneficiaryAsync(Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((ScanEmailDefinition)null);

        // Act
        var result = await appService.GetBySenderBeneficiaryAsync(beneficiary, userId, TestContext.CancellationToken);

        // Assert
        _ = _repository.Received(1)
            .GetBySenderBeneficiaryAsync(Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());

        result.ShouldSatisfyAllConditions(() =>
        {
            result.IsSuccess.ShouldBeFalse();
            _ = result.ShouldNotBeNull();
            result.Value.ShouldBeNull();
            result.Error.ShouldBe("ScanEmailDefinition not Found.");
        });
    }

    [TestMethod]
    public async Task GetBySenderEmailAddressAsync_WhenSenderEmailAddressExists_ShouldReturnSuccess_WithResultFound()
    {
        // Arrange
        var appService = new ScanEmailDefinitionAppService(_configuration, _repository, _unitOfWork);
        var userId = _faker.Random.Guid();
        var senderEmail = _faker.Internet.Email();
        var scanEmailDefinition = CreateScanEmailDefinitionFaker()
            .RuleFor(s => s.UserId, userId)
            .RuleFor(s => s.SenderEmailAddress, senderEmail)
            .Generate();

        _ = _repository.GetBySenderEmailAddressAsync(Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(scanEmailDefinition);

        // Act
        var result = await appService.GetBySenderEmailAddressAsync(senderEmail, userId, TestContext.CancellationToken);

        // Assert
        _ = _repository.Received(1)
            .GetBySenderEmailAddressAsync(Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());

        result.ShouldSatisfyAllConditions(() =>
        {
            result.IsSuccess.ShouldBeTrue();
            _ = result.ShouldNotBeNull();
            _ = result.Value.ShouldNotBeNull();
            _ = result.Value.ShouldBeOfType<ScanEmailDefinitionViewModel>();
            result.Value.ShouldBeEquivalentTo(scanEmailDefinition.Adapt<ScanEmailDefinitionViewModel>());
        });
    }

    [TestMethod]
    public async Task GetBySenderEmailAddressAsync_WhenSenderEmailAddressNotExists_ShouldReturnFailure_WithResultNotFound()
    {
        // Arrange
        var appService = new ScanEmailDefinitionAppService(_configuration, _repository, _unitOfWork);
        var senderEmail = _faker.Internet.Email();
        var userId = _faker.Random.Guid();

        _ = _repository.GetBySenderEmailAddressAsync(Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((ScanEmailDefinition)null);

        // Act
        var result = await appService.GetBySenderEmailAddressAsync(senderEmail, userId, TestContext.CancellationToken);

        // Assert
        _ = _repository.Received(1)
            .GetBySenderEmailAddressAsync(Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());

        result.ShouldSatisfyAllConditions(() =>
        {
            result.IsSuccess.ShouldBeFalse();
            _ = result.ShouldNotBeNull();
            result.Value.ShouldBeNull();
            result.Error.ShouldBe("ScanEmailDefinition not Found.");
        });
    }

    [TestMethod]
    public async Task GetByUserIdAsync_WhenUserIdExists_ShouldReturnSuccess_WithResultFound()
    {
        // Arrange
        var appService = new ScanEmailDefinitionAppService(_configuration, _repository, _unitOfWork);
        var userId = _faker.Random.Guid();
        var scanEmailDefinitions = CreateScanEmailDefinitionFaker()
            .RuleFor(s => s.UserId, userId)
            .Generate(3)
            .ToList();

        _ = _repository.GetByUserIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(scanEmailDefinitions);

        // Act
        var result = await appService.GetByUserIdAsync(userId, TestContext.CancellationToken);

        // Assert
        _ = _repository.Received(1).GetByUserIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());

        result.ShouldSatisfyAllConditions(() =>
        {
            result.IsSuccess.ShouldBeTrue();
            _ = result.ShouldNotBeNull();
            _ = result.Value.ShouldNotBeNull();
            result.Value.ShouldAllBe(x => x.UserId == userId);
            result.Value.ShouldBeEquivalentTo(scanEmailDefinitions.Adapt<IEnumerable<ScanEmailDefinitionViewModel>>());
        });
    }

    [TestMethod]
    public async Task GetByUserIdAsync_WhenUserIdNotExists_ShouldReturnFailure_WithResultNotFound()
    {
        // Arrange
        var appService = new ScanEmailDefinitionAppService(_configuration, _repository, _unitOfWork);
        var userId = _faker.Random.Guid();
        _ = _repository.GetByUserIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns([]);

        // Act
        var result = await appService.GetByUserIdAsync(userId, TestContext.CancellationToken);

        // Assert
        _ = _repository.Received(1).GetByUserIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());

        result.ShouldSatisfyAllConditions(() =>
        {
            result.IsSuccess.ShouldBeFalse();
            _ = result.ShouldNotBeNull();
            result.Value.ShouldBeNull();
            result.Error.ShouldBe("Empty Result.");
        });
    }

    [TestMethod]
    public async Task AddAsync_WhenFilePasswordIsProvided_ShouldAttemptToEncryptPasswordBeforeSave()
    {
        // Arrange
        var certificateFileName = "test_certificate.pfx";
        var certificateFilePath = Path.Combine(Path.GetTempPath(), "nonexistent_cert_dir");
        var certificatePassword = "test_password";
        var plainPassword = "MySecurePassword123!@#";
        var userId = _faker.Random.Guid();

        _ = _configuration.GetAppSetting("Security:CertificateFileName").Returns(certificateFileName);
        _ = _configuration.GetAppSetting("Security:CertificateFilePath").Returns(certificateFilePath);
        _ = _configuration.GetAppSetting("Security:CertificatePassword").Returns(certificatePassword);

        var viewModel = new ScanEmailDefinitionViewModel
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            InvoiceType = InvoiceType.AccountInvoice,
            Beneficiary = _faker.Person.FullName,
            Description = _faker.Lorem.Sentence(),
            SenderEmailAddress = _faker.Internet.Email(),
            AttachmentFileName = _faker.System.FileName("pdf"),
            FilePassword = plainPassword
        };

        var entity = viewModel.Adapt<ScanEmailDefinition>();

        _ = _repository.AddAsync(Arg.Any<ScanEmailDefinition>(), Arg.Any<CancellationToken>())
            .Returns(entity);

        _ = _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

        var appService = new ScanEmailDefinitionAppService(_configuration, _repository, _unitOfWork);

        // Act & Assert
        // The encryption will fail due to no certificate file in test environment,
        // but we verify the app service attempts encryption by catching the expected exception
        var exception = await Should.ThrowAsync<FileNotFoundException>(
            async () => await appService.AddAsync(viewModel, TestContext.CancellationToken)
        );

        exception.Message.ShouldContain("Certificado de segurança não encontrado no servidor");

        // Verify repository was not called since encryption failed
        _ = _repository.Received(0)
            .AddAsync(Arg.Any<ScanEmailDefinition>(), Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task AddAsync_WhenFilePasswordIsNull_ShouldNotEncryptAndSaveSuccessfully()
    {
        // Arrange
        var fakeThumbPrint = "fake-thumbprint-12345";
        var userId = _faker.Random.Guid();

        _ = _configuration.GetAppSetting("Security:CertificateThumbprint").Returns(fakeThumbPrint);

        var viewModel = new ScanEmailDefinitionViewModel
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            InvoiceType = InvoiceType.AccountInvoice,
            Beneficiary = _faker.Person.FullName,
            Description = _faker.Lorem.Sentence(),
            SenderEmailAddress = _faker.Internet.Email(),
            AttachmentFileName = _faker.System.FileName("pdf"),
            FilePassword = null
        };

        var entity = viewModel.Adapt<ScanEmailDefinition>();

        _ = _repository.AddAsync(Arg.Any<ScanEmailDefinition>(), Arg.Any<CancellationToken>())
            .Returns(entity);

        _ = _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

        var appService = new ScanEmailDefinitionAppService(_configuration, _repository, _unitOfWork);

        // Act
        var result = await appService.AddAsync(viewModel, TestContext.CancellationToken);

        // Assert
        result.ShouldSatisfyAllConditions(() =>
        {
            result.IsSuccess.ShouldBeTrue();
            _ = result.ShouldNotBeNull();
            _ = result.Value.ShouldNotBeNull();
        });

        _ = _repository.Received(1)
            .AddAsync(Arg.Is<ScanEmailDefinition>(s => string.IsNullOrWhiteSpace(s.FilePassword)),
            Arg.Any<CancellationToken>());

        _ = _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task AddAsync_WhenFilePasswordIsEmptyString_ShouldNotEncryptAndSaveSuccessfully()
    {
        // Arrange
        var fakeThumbPrint = "fake-thumbprint-12345";
        var userId = _faker.Random.Guid();

        _ = _configuration.GetAppSetting("Security:CertificateThumbprint").Returns(fakeThumbPrint);

        var viewModel = new ScanEmailDefinitionViewModel
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            InvoiceType = InvoiceType.AccountInvoice,
            Beneficiary = _faker.Person.FullName,
            Description = _faker.Lorem.Sentence(),
            SenderEmailAddress = _faker.Internet.Email(),
            AttachmentFileName = _faker.System.FileName("pdf"),
            FilePassword = string.Empty
        };

        var entity = viewModel.Adapt<ScanEmailDefinition>();

        _ = _repository.AddAsync(Arg.Any<ScanEmailDefinition>(), Arg.Any<CancellationToken>())
            .Returns(entity);

        _ = _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

        var appService = new ScanEmailDefinitionAppService(_configuration, _repository, _unitOfWork);

        // Act
        var result = await appService.AddAsync(viewModel, TestContext.CancellationToken);

        // Assert
        result.ShouldSatisfyAllConditions(() =>
        {
            result.IsSuccess.ShouldBeTrue();
            _ = result.ShouldNotBeNull();
            _ = result.Value.ShouldNotBeNull();
        });

        _ = _repository.Received(1)
            .AddAsync(Arg.Is<ScanEmailDefinition>(s => string.IsNullOrWhiteSpace(s.FilePassword)),
            Arg.Any<CancellationToken>());

        _ = _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task UpdateAsync_WhenFilePasswordIsProvided_ShouldAttemptToEncryptPasswordBeforeSave()
    {
        // Arrange
        var certificateFileName = "test_certificate.pfx";
        var certificateFilePath = Path.Combine(Path.GetTempPath(), "nonexistent_cert_dir");
        var certificatePassword = "test_password";
        var plainPassword = "UpdatedSecurePassword456!@#";
        var userId = _faker.Random.Guid();

        _ = _configuration.GetAppSetting("Security:CertificateFileName").Returns(certificateFileName);
        _ = _configuration.GetAppSetting("Security:CertificateFilePath").Returns(certificateFilePath);
        _ = _configuration.GetAppSetting("Security:CertificatePassword").Returns(certificatePassword);

        var viewModel = new ScanEmailDefinitionViewModel
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            InvoiceType = InvoiceType.AccountInvoice,
            Beneficiary = _faker.Person.FullName,
            Description = _faker.Lorem.Sentence(),
            SenderEmailAddress = _faker.Internet.Email(),
            AttachmentFileName = _faker.System.FileName("pdf"),
            FilePassword = plainPassword
        };

        var entity = viewModel.Adapt<ScanEmailDefinition>();

        _ = _repository.Update(Arg.Any<ScanEmailDefinition>()).Returns(entity);
        _ = _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

        var appService = new ScanEmailDefinitionAppService(_configuration, _repository, _unitOfWork);

        // Act & Assert
        // The encryption will fail due to no certificate file in test environment,
        // but we verify the app service attempts encryption by catching the expected exception
        var exception = await Should.ThrowAsync<FileNotFoundException>(
            async () => await appService.UpdateAsync(viewModel, TestContext.CancellationToken)
        );

        exception.Message.ShouldContain("Certificado de segurança não encontrado no servidor");

        // Verify repository was not called since encryption failed
        _ = _repository.Received(0)
            .Update(Arg.Any<ScanEmailDefinition>());
    }

    [TestMethod]
    public async Task UpdateAsync_WhenFilePasswordIsNull_ShouldNotEncryptAndSaveSuccessfully()
    {
        // Arrange
        var fakeThumbPrint = "fake-thumbprint-12345";
        var userId = _faker.Random.Guid();

        _ = _configuration.GetAppSetting("Security:CertificateThumbprint").Returns(fakeThumbPrint);

        var viewModel = new ScanEmailDefinitionViewModel
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            InvoiceType = InvoiceType.AccountInvoice,
            Beneficiary = _faker.Person.FullName,
            Description = _faker.Lorem.Sentence(),
            SenderEmailAddress = _faker.Internet.Email(),
            AttachmentFileName = _faker.System.FileName("pdf"),
            FilePassword = null
        };

        var entity = viewModel.Adapt<ScanEmailDefinition>();

        _ = _repository.Update(Arg.Any<ScanEmailDefinition>()).Returns(entity);
        _ = _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

        var appService = new ScanEmailDefinitionAppService(_configuration, _repository, _unitOfWork);

        // Act
        var result = await appService.UpdateAsync(viewModel, TestContext.CancellationToken);

        // Assert
        result.ShouldSatisfyAllConditions(() =>
        {
            result.IsSuccess.ShouldBeTrue();
            _ = result.ShouldNotBeNull();
            _ = result.Value.ShouldNotBeNull();
        });

        _ = _repository.Received(1)
            .Update(Arg.Is<ScanEmailDefinition>(s => string.IsNullOrWhiteSpace(s.FilePassword)));

        _ = _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task UpdateAsync_WhenFilePasswordIsEmptyString_ShouldNotEncryptAndSaveSuccessfully()
    {
        // Arrange
        var fakeThumbPrint = "fake-thumbprint-12345";
        var userId = _faker.Random.Guid();

        _ = _configuration.GetAppSetting("Security:CertificateThumbprint").Returns(fakeThumbPrint);

        var viewModel = new ScanEmailDefinitionViewModel
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            InvoiceType = InvoiceType.AccountInvoice,
            Beneficiary = _faker.Person.FullName,
            Description = _faker.Lorem.Sentence(),
            SenderEmailAddress = _faker.Internet.Email(),
            AttachmentFileName = _faker.System.FileName("pdf"),
            FilePassword = string.Empty
        };

        var entity = viewModel.Adapt<ScanEmailDefinition>();

        _ = _repository.Update(Arg.Any<ScanEmailDefinition>()).Returns(entity);
        _ = _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

        var appService = new ScanEmailDefinitionAppService(_configuration, _repository, _unitOfWork);

        // Act
        var result = await appService.UpdateAsync(viewModel, TestContext.CancellationToken);

        // Assert
        result.ShouldSatisfyAllConditions(() =>
        {
            result.IsSuccess.ShouldBeTrue();
            _ = result.ShouldNotBeNull();
            _ = result.Value.ShouldNotBeNull();
        });

        _ = _repository.Received(1)
            .Update(Arg.Is<ScanEmailDefinition>(s => string.IsNullOrWhiteSpace(s.FilePassword)));

        _ = _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
