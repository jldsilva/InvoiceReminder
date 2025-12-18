using Bogus;
using InvoiceReminder.Domain.Entities;
using InvoiceReminder.Domain.Extensions;
using Shouldly;

namespace InvoiceReminder.DomainEntities.UnitTests.Extensions;

[TestClass]
public sealed class UserExtensionsTests
{
    private readonly Faker<Invoice> _invoiceFaker;
    private readonly Faker<JobSchedule> _jobScheduleFaker;
    private readonly Faker<EmailAuthToken> _emailAuthTokenFaker;
    private readonly Faker<ScanEmailDefinition> _scanEmailDefinitionFaker;

    public UserExtensionsTests()
    {
        _invoiceFaker = new Faker<Invoice>()
            .RuleFor(e => e.Id, faker => faker.Random.Guid());

        _jobScheduleFaker = new Faker<JobSchedule>()
            .RuleFor(e => e.Id, faker => faker.Random.Guid());

        _emailAuthTokenFaker = new Faker<EmailAuthToken>()
            .RuleFor(e => e.Id, faker => faker.Random.Guid());

        _scanEmailDefinitionFaker = new Faker<ScanEmailDefinition>()
            .RuleFor(e => e.Id, faker => faker.Random.Guid());
    }

    private static Faker<User> UserFaker(
        ICollection<Invoice> invoices = default,
        ICollection<JobSchedule> jobSchedules = default,
        ICollection<EmailAuthToken> emailAuthTokens = default,
        ICollection<ScanEmailDefinition> scanEmailDefinitions = default)
    {
        return new Faker<User>()
            .RuleFor(e => e.Id, faker => faker.Random.Guid())
            .RuleFor(e => e.Name, faker => faker.Person.FullName)
            .RuleFor(u => u.Invoices, faker => invoices ?? new HashSet<Invoice>())
            .RuleFor(u => u.JobSchedules, faker => jobSchedules ?? new HashSet<JobSchedule>())
            .RuleFor(u => u.EmailAuthTokens, faker => emailAuthTokens ?? new HashSet<EmailAuthToken>())
            .RuleFor(u => u.ScanEmailDefinitions, faker => scanEmailDefinitions ?? new HashSet<ScanEmailDefinition>());
    }

    [TestMethod]
    public void Handle_NewUser_AddsUserToResult()
    {
        // Arrange
        var result = new Dictionary<Guid, User>();
        var user = UserFaker().Generate();
        var parameters = new UserParameters();

        // Act
        _ = result.Handle(ref user, parameters);

        // Assert
        result.ShouldSatisfyAllConditions(result =>
        {
            result.ShouldContainKey(user.Id);
            result[user.Id].ShouldBeSameAs(user);
        });

        user.ShouldSatisfyAllConditions(user =>
        {
            user.Invoices.ShouldBeEmpty();
            user.JobSchedules.ShouldBeEmpty();
            user.EmailAuthTokens.ShouldBeEmpty();
            user.ScanEmailDefinitions.ShouldBeEmpty();
        });
    }

    [TestMethod]
    public void Handle_NewUserWithInvoice_AddsInvoiceToUserAndResult()
    {
        // Arrange
        var result = new Dictionary<Guid, User>();
        var user = UserFaker().Generate();
        var invoice = _invoiceFaker.Generate();
        var parameters = new UserParameters { Invoice = invoice };

        // Act
        _ = result.Handle(ref user, parameters);

        // Assert
        result.ShouldSatisfyAllConditions(result =>
        {
            result.ShouldContainKey(user.Id);
            result[user.Id].Invoices.ShouldContain(invoice);
        });

        user.ShouldSatisfyAllConditions(user =>
        {
            user.Invoices.ShouldContain(invoice);
            user.JobSchedules.ShouldBeEmpty();
            user.EmailAuthTokens.ShouldBeEmpty();
            user.ScanEmailDefinitions.ShouldBeEmpty();
        });
    }

    [TestMethod]
    public void Handle_NewUserWithJobSchedule_AddsJobScheduleToUserAndResult()
    {
        // Arrange
        var result = new Dictionary<Guid, User>();
        var user = UserFaker().Generate();
        var jobSchedule = _jobScheduleFaker.Generate();
        var parameters = new UserParameters { JobSchedule = jobSchedule };

        // Act
        _ = result.Handle(ref user, parameters);

        // Assert
        result.ShouldSatisfyAllConditions(result =>
        {
            result.ShouldContainKey(user.Id);
            result[user.Id].JobSchedules.ShouldContain(jobSchedule);
        });

        user.ShouldSatisfyAllConditions(user =>
        {
            user.Invoices.ShouldBeEmpty();
            user.JobSchedules.ShouldContain(jobSchedule);
            user.EmailAuthTokens.ShouldBeEmpty();
            user.ScanEmailDefinitions.ShouldBeEmpty();
        });
    }

    [TestMethod]
    public void Handle_NewUserWithEmailAuthToken_AddsEmailAuthTokenToUserAndResult()
    {
        // Arrange
        var result = new Dictionary<Guid, User>();
        var user = UserFaker().Generate();
        var emailAuthToken = _emailAuthTokenFaker.Generate();
        var parameters = new UserParameters { EmailAuthToken = emailAuthToken };

        // Act
        _ = result.Handle(ref user, parameters);

        // Assert
        result.ShouldSatisfyAllConditions(result =>
        {
            result.ShouldContainKey(user.Id);
            result[user.Id].EmailAuthTokens.ShouldContain(emailAuthToken);
        });

        user.ShouldSatisfyAllConditions(user =>
        {
            user.Invoices.ShouldBeEmpty();
            user.JobSchedules.ShouldBeEmpty();
            user.EmailAuthTokens.ShouldContain(emailAuthToken);
            user.ScanEmailDefinitions.ShouldBeEmpty();
        });
    }

    [TestMethod]
    public void Handle_NewUserWithScanEmailDefinition_AddsScanEmailDefinitionToUserAndResult()
    {
        // Arrange
        var result = new Dictionary<Guid, User>();
        var user = UserFaker().Generate();
        var scanEmailDefinition = _scanEmailDefinitionFaker.Generate();
        var parameters = new UserParameters { ScanEmailDefinition = scanEmailDefinition };

        // Act
        _ = result.Handle(ref user, parameters);

        // Assert
        result.ShouldSatisfyAllConditions(result =>
        {
            result.ShouldContainKey(user.Id);
            result[user.Id].ScanEmailDefinitions.ShouldContain(scanEmailDefinition);
        });
        user.ShouldSatisfyAllConditions(user =>
        {
            user.Invoices.ShouldBeEmpty();
            user.JobSchedules.ShouldBeEmpty();
            user.EmailAuthTokens.ShouldBeEmpty();
            user.ScanEmailDefinitions.ShouldContain(scanEmailDefinition);
        });
    }

    [TestMethod]
    public void Handle_ExistingUserWithNewInvoice_AddsInvoiceToExistingUserInResult()
    {
        // Arrange
        var existingInvoice = _invoiceFaker.Generate();
        var existingUser = UserFaker(invoices: [existingInvoice]).Generate();
        var result = new Dictionary<Guid, User> { { existingUser.Id, existingUser } };
        var newUserReference = existingUser;
        var newInvoice = _invoiceFaker.Generate();
        var parameters = new UserParameters { Invoice = newInvoice };

        // Act
        _ = result.Handle(ref newUserReference, parameters);

        // Assert
        result.ShouldSatisfyAllConditions(result =>
        {
            result.ShouldContainKey(existingUser.Id);
            result[existingUser.Id].Invoices.ShouldContain(existingInvoice);
            result[existingUser.Id].Invoices.ShouldContain(newInvoice);
        });

        newUserReference.ShouldSatisfyAllConditions(newUserReference =>
        {
            newUserReference.Invoices.Count.ShouldBe(2);
            newUserReference.Invoices.ShouldContain(existingInvoice);
            newUserReference.Invoices.ShouldContain(newInvoice);
            newUserReference.JobSchedules.ShouldBeEmpty();
            newUserReference.EmailAuthTokens.ShouldBeEmpty();
            newUserReference.ScanEmailDefinitions.ShouldBeEmpty();
        });
    }

    [TestMethod]
    public void Handle_ExistingUserWithNewJobSchedule_AddsJobScheduleToExistingUserInResult()
    {
        // Arrange
        var existingJobSchedule = _jobScheduleFaker.Generate();
        var existingUser = UserFaker(jobSchedules: [existingJobSchedule]).Generate();
        var result = new Dictionary<Guid, User> { { existingUser.Id, existingUser } };
        var newUserReference = existingUser;
        var newJobSchedule = _jobScheduleFaker.Generate();
        var parameters = new UserParameters { JobSchedule = newJobSchedule };

        // Act
        _ = result.Handle(ref newUserReference, parameters);

        // Assert
        result.ShouldSatisfyAllConditions(result =>
        {
            result.ShouldContainKey(existingUser.Id);
            result[existingUser.Id].JobSchedules.ShouldContain(existingJobSchedule);
            result[existingUser.Id].JobSchedules.ShouldContain(newJobSchedule);
        });

        newUserReference.ShouldSatisfyAllConditions(newUserReference =>
        {
            newUserReference.Invoices.ShouldBeEmpty();
            newUserReference.JobSchedules.Count.ShouldBe(2);
            newUserReference.JobSchedules.ShouldContain(existingJobSchedule);
            newUserReference.JobSchedules.ShouldContain(newJobSchedule);
            newUserReference.EmailAuthTokens.ShouldBeEmpty();
            newUserReference.ScanEmailDefinitions.ShouldBeEmpty();
        });
    }

    [TestMethod]
    public void Handle_ExistingUserWithNewEmailAuthToken_AddsEmailAuthTokenToExistingUserInResult()
    {
        // Arrange
        var existingEmailAuthToken = _emailAuthTokenFaker.Generate();
        var existingUser = UserFaker(emailAuthTokens: [existingEmailAuthToken]).Generate();
        var result = new Dictionary<Guid, User> { { existingUser.Id, existingUser } };
        var newUserReference = existingUser;
        var newEmailAuthToken = _emailAuthTokenFaker.Generate();
        var parameters = new UserParameters { EmailAuthToken = newEmailAuthToken };

        // Act
        _ = result.Handle(ref newUserReference, parameters);

        // Assert
        result.ShouldSatisfyAllConditions(result =>
        {
            result.ShouldContainKey(existingUser.Id);
            result[existingUser.Id].EmailAuthTokens.ShouldContain(existingEmailAuthToken);
            result[existingUser.Id].EmailAuthTokens.ShouldContain(newEmailAuthToken);
        });

        newUserReference.ShouldSatisfyAllConditions(newUserReference =>
        {
            newUserReference.Invoices.ShouldBeEmpty();
            newUserReference.JobSchedules.ShouldBeEmpty();
            newUserReference.EmailAuthTokens.Count.ShouldBe(2);
            newUserReference.EmailAuthTokens.ShouldContain(existingEmailAuthToken);
            newUserReference.EmailAuthTokens.ShouldContain(newEmailAuthToken);
            newUserReference.ScanEmailDefinitions.ShouldBeEmpty();
        });
    }

    [TestMethod]
    public void Handle_ExistingUserWithNewScanEmailDefinition_AddsScanEmailDefinitionToExistingUserInResult()
    {
        // Arrange
        var existingScanEmailDefinition = _scanEmailDefinitionFaker.Generate();
        var existingUser = UserFaker(scanEmailDefinitions: [existingScanEmailDefinition]).Generate();
        var result = new Dictionary<Guid, User> { { existingUser.Id, existingUser } };
        var newUserReference = existingUser;
        var newScanEmailDefinition = _scanEmailDefinitionFaker.Generate();
        var parameters = new UserParameters { ScanEmailDefinition = newScanEmailDefinition };

        // Act
        _ = result.Handle(ref newUserReference, parameters);

        // Assert
        result.ShouldSatisfyAllConditions(result =>
        {
            result.ShouldContainKey(existingUser.Id);
            result[existingUser.Id].ScanEmailDefinitions.ShouldContain(existingScanEmailDefinition);
            result[existingUser.Id].ScanEmailDefinitions.ShouldContain(newScanEmailDefinition);
        });

        newUserReference.ShouldSatisfyAllConditions(newUserReference =>
        {
            newUserReference.Invoices.ShouldBeEmpty();
            newUserReference.JobSchedules.ShouldBeEmpty();
            newUserReference.EmailAuthTokens.ShouldBeEmpty();
            newUserReference.ScanEmailDefinitions.Count.ShouldBe(2);
            newUserReference.ScanEmailDefinitions.ShouldContain(existingScanEmailDefinition);
            newUserReference.ScanEmailDefinitions.ShouldContain(newScanEmailDefinition);
        });
    }

    [TestMethod]
    public void Handle_ExistingUserWithSameInvoice_DoesNotAddDuplicate()
    {
        // Arrange
        var existingInvoice = _invoiceFaker.Generate();
        var existingUser = UserFaker(invoices: [existingInvoice]).Generate();
        var result = new Dictionary<Guid, User> { { existingUser.Id, existingUser } };
        var newUserReference = existingUser;
        var parameters = new UserParameters { Invoice = existingInvoice };

        // Act
        _ = result.Handle(ref newUserReference, parameters);

        // Assert
        result.ShouldSatisfyAllConditions(result =>
        {
            result.ShouldContainKey(existingUser.Id);
            result[existingUser.Id].Invoices.Count.ShouldBe(1);
            result[existingUser.Id].Invoices.ShouldContain(existingInvoice);
        });

        newUserReference.ShouldSatisfyAllConditions(newUserReference =>
        {
            newUserReference.Invoices.Count.ShouldBe(1);
            newUserReference.Invoices.ShouldContain(existingInvoice);
        });
    }

    [TestMethod]
    public void Handle_ExistingUserWithSameJobSchedule_DoesNotAddDuplicate()
    {
        // Arrange
        var existingJobSchedule = _jobScheduleFaker.Generate();
        var existingUser = UserFaker(jobSchedules: [existingJobSchedule]).Generate();
        var result = new Dictionary<Guid, User> { { existingUser.Id, existingUser } };
        var newUserReference = existingUser;
        var parameters = new UserParameters { JobSchedule = existingJobSchedule };

        // Act
        _ = result.Handle(ref newUserReference, parameters);

        // Assert
        result.ShouldSatisfyAllConditions(result =>
        {
            result.ShouldContainKey(existingUser.Id);
            result[existingUser.Id].JobSchedules.Count.ShouldBe(1);
            result[existingUser.Id].JobSchedules.ShouldContain(existingJobSchedule);
        });

        newUserReference.ShouldSatisfyAllConditions(newUserReference =>
        {
            newUserReference.JobSchedules.Count.ShouldBe(1);
            newUserReference.JobSchedules.ShouldContain(existingJobSchedule);
        });
    }

    [TestMethod]
    public void Handle_ExistingUserWithSameEmailAuthToken_DoesNotAddDuplicate()
    {
        // Arrange
        var existingEmailAuthToken = _emailAuthTokenFaker.Generate();
        var existingUser = UserFaker(emailAuthTokens: [existingEmailAuthToken]).Generate();
        var result = new Dictionary<Guid, User> { { existingUser.Id, existingUser } };
        var newUserReference = existingUser;
        var parameters = new UserParameters { EmailAuthToken = existingEmailAuthToken };

        // Act
        _ = result.Handle(ref newUserReference, parameters);

        // Assert
        result.ShouldSatisfyAllConditions(result =>
        {
            result.ShouldContainKey(existingUser.Id);
            result[existingUser.Id].EmailAuthTokens.Count.ShouldBe(1);
            result[existingUser.Id].EmailAuthTokens.ShouldContain(existingEmailAuthToken);
        });

        newUserReference.ShouldSatisfyAllConditions(newUserReference =>
        {
            newUserReference.EmailAuthTokens.Count.ShouldBe(1);
            newUserReference.EmailAuthTokens.ShouldContain(existingEmailAuthToken);
        });
    }

    [TestMethod]
    public void Handle_ExistingUserWithSameScanEmailDefinition_DoesNotAddDuplicate()
    {
        // Arrange
        var existingScanEmailDefinition = _scanEmailDefinitionFaker.Generate();
        var existingUser = UserFaker(scanEmailDefinitions: [existingScanEmailDefinition]).Generate();
        var result = new Dictionary<Guid, User> { { existingUser.Id, existingUser } };
        var newUserReference = existingUser;
        var parameters = new UserParameters { ScanEmailDefinition = existingScanEmailDefinition };

        // Act
        _ = result.Handle(ref newUserReference, parameters);

        // Assert
        result.ShouldSatisfyAllConditions(result =>
        {
            result.ShouldContainKey(existingUser.Id);
            result[existingUser.Id].ScanEmailDefinitions.Count.ShouldBe(1);
            result[existingUser.Id].ScanEmailDefinitions.ShouldContain(existingScanEmailDefinition);
        });

        newUserReference.ShouldSatisfyAllConditions(newUserReference =>
        {
            newUserReference.ScanEmailDefinitions.Count.ShouldBe(1);
            newUserReference.ScanEmailDefinitions.ShouldContain(existingScanEmailDefinition);
        });
    }

    [TestMethod]
    public void Handle_NewUserWithEmptyCollections_InitializesCollections()
    {
        // Arrange
        var result = new Dictionary<Guid, User>();
        var user = UserFaker().Generate();
        var invoice = _invoiceFaker.Generate();
        var jobSchedule = _jobScheduleFaker.Generate();
        var scanEmailDefinition = _scanEmailDefinitionFaker.Generate();
        var emailAuthToken = _emailAuthTokenFaker.Generate();
        var parameters = new UserParameters
        {
            Invoice = invoice,
            JobSchedule = jobSchedule,
            EmailAuthToken = emailAuthToken,
            ScanEmailDefinition = scanEmailDefinition
        };

        // Act
        _ = result.Handle(ref user, parameters);

        // Assert
        result.ShouldSatisfyAllConditions(result =>
        {
            result.ShouldContainKey(user.Id);
            result[user.Id].Invoices.ShouldContain(invoice);
            result[user.Id].JobSchedules.ShouldContain(jobSchedule);
            result[user.Id].EmailAuthTokens.ShouldContain(emailAuthToken);
            result[user.Id].ScanEmailDefinitions.ShouldContain(scanEmailDefinition);
        });

        user.ShouldSatisfyAllConditions(user =>
        {
            user.Invoices.Count.ShouldBe(1);
            user.JobSchedules.Count.ShouldBe(1);
            user.EmailAuthTokens.Count.ShouldBe(1);
            user.ScanEmailDefinitions.Count.ShouldBe(1);
            user.Invoices.ShouldContain(invoice);
            user.JobSchedules.ShouldContain(jobSchedule);
            user.EmailAuthTokens.ShouldContain(emailAuthToken);
            user.ScanEmailDefinitions.ShouldContain(scanEmailDefinition);
        });
    }

    [TestMethod]
    public void Handle_ExistingUserWithNullCollections_AddsNewItems()
    {
        // Arrange
        var existingUser = UserFaker().Generate();
        var result = new Dictionary<Guid, User> { { existingUser.Id, existingUser } };
        var newUserReference = existingUser;
        var invoice = _invoiceFaker.Generate();
        var jobSchedule = _jobScheduleFaker.Generate();
        var scanEmailDefinition = _scanEmailDefinitionFaker.Generate();
        var emailAuthToken = _emailAuthTokenFaker.Generate();
        var parameters = new UserParameters
        {
            Invoice = invoice,
            JobSchedule = jobSchedule,
            EmailAuthToken = emailAuthToken,
            ScanEmailDefinition = scanEmailDefinition
        };

        // Act
        _ = result.Handle(ref newUserReference, parameters);

        // Assert
        result.ShouldSatisfyAllConditions(result =>
        {
            result.ShouldContainKey(existingUser.Id);
            result[existingUser.Id].Invoices.Count.ShouldBe(1);
            result[existingUser.Id].JobSchedules.Count.ShouldBe(1);
            result[existingUser.Id].EmailAuthTokens.Count.ShouldBe(1);
            result[existingUser.Id].ScanEmailDefinitions.Count.ShouldBe(1);
            result[existingUser.Id].Invoices.ShouldContain(invoice);
            result[existingUser.Id].JobSchedules.ShouldContain(jobSchedule);
            result[existingUser.Id].EmailAuthTokens.ShouldContain(emailAuthToken);
            result[existingUser.Id].ScanEmailDefinitions.ShouldContain(scanEmailDefinition);
        });

        newUserReference.ShouldSatisfyAllConditions(newUserReference =>
        {
            newUserReference.Invoices.Count.ShouldBe(1);
            newUserReference.JobSchedules.Count.ShouldBe(1);
            newUserReference.EmailAuthTokens.Count.ShouldBe(1);
            newUserReference.ScanEmailDefinitions.Count.ShouldBe(1);
            newUserReference.Invoices.ShouldContain(invoice);
            newUserReference.JobSchedules.ShouldContain(jobSchedule);
            newUserReference.EmailAuthTokens.ShouldContain(emailAuthToken);
            newUserReference.ScanEmailDefinitions.ShouldContain(scanEmailDefinition);
        });
    }

    [TestMethod]
    public void Handle_ExistingUserWithNullParameters_DoesNotModifyUser()
    {
        // Arrange
        var existingInvoice = _invoiceFaker.Generate();
        var existingJobSchedule = _jobScheduleFaker.Generate();
        var exitingEmailAuthToken = _emailAuthTokenFaker.Generate();
        var existingScanEmailDefinition = _scanEmailDefinitionFaker.Generate();
        var existingUser = UserFaker(
            invoices: [existingInvoice],
            jobSchedules: [existingJobSchedule],
            emailAuthTokens: [exitingEmailAuthToken],
            scanEmailDefinitions: [existingScanEmailDefinition])
            .Generate();
        var result = new Dictionary<Guid, User> { { existingUser.Id, existingUser } };
        var newUserReference = existingUser;
        var parameters = new UserParameters
        {
            Invoice = null,
            JobSchedule = null,
            EmailAuthToken = null,
            ScanEmailDefinition = null
        };

        // Act
        _ = result.Handle(ref newUserReference, parameters);

        // Assert
        result.ShouldSatisfyAllConditions(result =>
        {
            result.ShouldContainKey(existingUser.Id);
            result[existingUser.Id].Invoices.ShouldContain(existingInvoice);
            result[existingUser.Id].JobSchedules.ShouldContain(existingJobSchedule);
            result[existingUser.Id].EmailAuthTokens.ShouldContain(exitingEmailAuthToken);
            result[existingUser.Id].ScanEmailDefinitions.ShouldContain(existingScanEmailDefinition);
            result[existingUser.Id].Invoices.Count.ShouldBe(1);
            result[existingUser.Id].JobSchedules.Count.ShouldBe(1);
            result[existingUser.Id].EmailAuthTokens.Count.ShouldBe(1);
            result[existingUser.Id].ScanEmailDefinitions.Count.ShouldBe(1);
        });

        newUserReference.ShouldSatisfyAllConditions(newUserReference =>
        {
            newUserReference.Invoices.Count.ShouldBe(1);
            newUserReference.JobSchedules.Count.ShouldBe(1);
            newUserReference.EmailAuthTokens.Count.ShouldBe(1);
            newUserReference.ScanEmailDefinitions.Count.ShouldBe(1);
            newUserReference.Invoices.ShouldContain(existingInvoice);
            newUserReference.JobSchedules.ShouldContain(existingJobSchedule);
            newUserReference.EmailAuthTokens.ShouldContain(exitingEmailAuthToken);
            newUserReference.ScanEmailDefinitions.ShouldContain(existingScanEmailDefinition);
        });
    }

    [TestMethod]
    public void Handle_NewUserWithNullParameters_AddsUserWithEmptyCollections()
    {
        // Arrange
        var result = new Dictionary<Guid, User>();
        var user = UserFaker().Generate();
        var parameters = new UserParameters
        {
            Invoice = null,
            JobSchedule = null,
            EmailAuthToken = null,
            ScanEmailDefinition = null
        };

        // Act
        _ = result.Handle(ref user, parameters);

        // Assert
        result.ShouldSatisfyAllConditions(result =>
        {
            result.ShouldContainKey(user.Id);
            result[user.Id].Invoices.ShouldBeEmpty();
            result[user.Id].JobSchedules.ShouldBeEmpty();
            result[user.Id].EmailAuthTokens.ShouldBeEmpty();
            result[user.Id].ScanEmailDefinitions.ShouldBeEmpty();
        });

        user.ShouldSatisfyAllConditions(user =>
        {
            user.Invoices.ShouldBeEmpty();
            user.JobSchedules.ShouldBeEmpty();
            user.EmailAuthTokens.ShouldBeEmpty();
            user.ScanEmailDefinitions.ShouldBeEmpty();
        });
    }
}
