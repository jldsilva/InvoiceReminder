using InvoiceReminder.Domain.Entities;
using InvoiceReminder.Domain.Extensions;
using Shouldly;

namespace InvoiceReminder.DomainEntities.UnitTests.Extensions;

[TestClass]
public class UserExtensionsTests
{
    [TestMethod]
    public void Handle_NewUser_AddsUserToResult()
    {
        // Arrange
        var result = new Dictionary<Guid, User>();
        var user = new User { Id = Guid.NewGuid(), Name = "Test User" };
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
            user.ScanEmailDefinitions.ShouldBeEmpty();
        });
    }

    [TestMethod]
    public void Handle_NewUserWithInvoice_AddsInvoiceToUserAndResult()
    {
        // Arrange
        var result = new Dictionary<Guid, User>();
        var user = new User { Id = Guid.NewGuid(), Name = "Test User" };
        var invoice = new Invoice { Id = Guid.NewGuid() };
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
            user.ScanEmailDefinitions.ShouldBeEmpty();
        });
    }

    [TestMethod]
    public void Handle_NewUserWithJobSchedule_AddsJobScheduleToUserAndResult()
    {
        // Arrange
        var result = new Dictionary<Guid, User>();
        var user = new User { Id = Guid.NewGuid(), Name = "Test User" };
        var jobSchedule = new JobSchedule { Id = Guid.NewGuid() };
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
            user.ScanEmailDefinitions.ShouldBeEmpty();
        });
    }

    [TestMethod]
    public void Handle_NewUserWithScanEmailDefinition_AddsScanEmailDefinitionToUserAndResult()
    {
        // Arrange
        var result = new Dictionary<Guid, User>();
        var user = new User { Id = Guid.NewGuid(), Name = "Test User" };
        var scanEmailDefinition = new ScanEmailDefinition { Id = Guid.NewGuid() };
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
            user.ScanEmailDefinitions.ShouldContain(scanEmailDefinition);
        });
    }

    [TestMethod]
    public void Handle_ExistingUserWithNewInvoice_AddsInvoiceToExistingUserInResult()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var existingInvoice = new Invoice { Id = Guid.NewGuid() };
        var existingUser = new User
        {
            Id = userId,
            Name = "Existing User",
            Invoices = [existingInvoice],
            JobSchedules = [],
            ScanEmailDefinitions = []
        };
        var result = new Dictionary<Guid, User> { { userId, existingUser } };
        var newUserReference = existingUser;
        var newInvoice = new Invoice { Id = Guid.NewGuid() };
        var parameters = new UserParameters { Invoice = newInvoice };

        // Act
        _ = result.Handle(ref newUserReference, parameters);

        // Assert
        result.ShouldSatisfyAllConditions(result =>
        {
            result.ShouldContainKey(userId);
            result[userId].Invoices.ShouldContain(existingInvoice);
            result[userId].Invoices.ShouldContain(newInvoice);
        });

        newUserReference.ShouldSatisfyAllConditions(newUserReference =>
        {
            newUserReference.Invoices.Count.ShouldBe(2);
            newUserReference.Invoices.ShouldContain(existingInvoice);
            newUserReference.Invoices.ShouldContain(newInvoice);
            newUserReference.JobSchedules.ShouldBeEmpty();
            newUserReference.ScanEmailDefinitions.ShouldBeEmpty();
        });
    }

    [TestMethod]
    public void Handle_ExistingUserWithNewJobSchedule_AddsJobScheduleToExistingUserInResult()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var existingJobSchedule = new JobSchedule { Id = Guid.NewGuid() };
        var existingUser = new User
        {
            Id = userId,
            Name = "Existing User",
            Invoices = [],
            JobSchedules = [existingJobSchedule],
            ScanEmailDefinitions = []
        };
        var result = new Dictionary<Guid, User> { { userId, existingUser } };
        var newUserReference = existingUser;
        var newJobSchedule = new JobSchedule { Id = Guid.NewGuid() };
        var parameters = new UserParameters { JobSchedule = newJobSchedule };

        // Act
        _ = result.Handle(ref newUserReference, parameters);

        // Assert
        result.ShouldSatisfyAllConditions(result =>
        {
            result.ShouldContainKey(userId);
            result[userId].JobSchedules.ShouldContain(existingJobSchedule);
            result[userId].JobSchedules.ShouldContain(newJobSchedule);
        });

        newUserReference.ShouldSatisfyAllConditions(newUserReference =>
        {
            newUserReference.Invoices.ShouldBeEmpty();
            newUserReference.JobSchedules.Count.ShouldBe(2);
            newUserReference.JobSchedules.ShouldContain(existingJobSchedule);
            newUserReference.JobSchedules.ShouldContain(newJobSchedule);
            newUserReference.ScanEmailDefinitions.ShouldBeEmpty();
        });
    }

    [TestMethod]
    public void Handle_ExistingUserWithNewScanEmailDefinition_AddsScanEmailDefinitionToExistingUserInResult()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var existingScanEmailDefinition = new ScanEmailDefinition { Id = Guid.NewGuid() };
        var existingUser = new User
        {
            Id = userId,
            Name = "Existing User",
            Invoices = [],
            JobSchedules = [],
            ScanEmailDefinitions = [existingScanEmailDefinition]
        };
        var result = new Dictionary<Guid, User> { { userId, existingUser } };
        var newUserReference = existingUser;
        var newScanEmailDefinition = new ScanEmailDefinition { Id = Guid.NewGuid() };
        var parameters = new UserParameters { ScanEmailDefinition = newScanEmailDefinition };

        // Act
        _ = result.Handle(ref newUserReference, parameters);

        // Assert
        result.ShouldSatisfyAllConditions(result =>
        {
            result.ShouldContainKey(userId);
            result[userId].ScanEmailDefinitions.ShouldContain(existingScanEmailDefinition);
            result[userId].ScanEmailDefinitions.ShouldContain(newScanEmailDefinition);
        });

        newUserReference.ShouldSatisfyAllConditions(newUserReference =>
        {
            newUserReference.Invoices.ShouldBeEmpty();
            newUserReference.JobSchedules.ShouldBeEmpty();
            newUserReference.ScanEmailDefinitions.Count.ShouldBe(2);
            newUserReference.ScanEmailDefinitions.ShouldContain(existingScanEmailDefinition);
            newUserReference.ScanEmailDefinitions.ShouldContain(newScanEmailDefinition);
        });
    }

    [TestMethod]
    public void Handle_ExistingUserWithSameInvoice_DoesNotAddDuplicate()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var existingInvoice = new Invoice { Id = Guid.NewGuid() };
        var existingUser = new User { Id = userId, Name = "Existing User", Invoices = [existingInvoice] };
        var result = new Dictionary<Guid, User> { { userId, existingUser } };
        var newUserReference = existingUser;
        var parameters = new UserParameters { Invoice = existingInvoice };

        // Act
        _ = result.Handle(ref newUserReference, parameters);

        // Assert
        result.ShouldSatisfyAllConditions(result =>
        {
            result.ShouldContainKey(userId);
            result[userId].Invoices.Count.ShouldBe(1);
            result[userId].Invoices.ShouldContain(existingInvoice);
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
        var userId = Guid.NewGuid();
        var existingJobSchedule = new JobSchedule { Id = Guid.NewGuid() };
        var existingUser = new User { Id = userId, Name = "Existing User", JobSchedules = [existingJobSchedule] };
        var result = new Dictionary<Guid, User> { { userId, existingUser } };
        var newUserReference = existingUser;
        var parameters = new UserParameters { JobSchedule = existingJobSchedule };

        // Act
        _ = result.Handle(ref newUserReference, parameters);

        // Assert
        result.ShouldSatisfyAllConditions(result =>
        {
            result.ShouldContainKey(userId);
            result[userId].JobSchedules.Count.ShouldBe(1);
            result[userId].JobSchedules.ShouldContain(existingJobSchedule);
        });

        newUserReference.ShouldSatisfyAllConditions(newUserReference =>
        {
            newUserReference.JobSchedules.Count.ShouldBe(1);
            newUserReference.JobSchedules.ShouldContain(existingJobSchedule);
        });
    }

    [TestMethod]
    public void Handle_ExistingUserWithSameScanEmailDefinition_DoesNotAddDuplicate()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var existingScanEmailDefinition = new ScanEmailDefinition { Id = Guid.NewGuid() };
        var existingUser = new User
        {
            Id = userId,
            Name = "Existing User",
            ScanEmailDefinitions = [existingScanEmailDefinition]
        };
        var result = new Dictionary<Guid, User> { { userId, existingUser } };
        var newUserReference = existingUser;
        var parameters = new UserParameters { ScanEmailDefinition = existingScanEmailDefinition };

        // Act
        _ = result.Handle(ref newUserReference, parameters);

        // Assert
        result.ShouldSatisfyAllConditions(result =>
        {
            result.ShouldContainKey(userId);
            result[userId].ScanEmailDefinitions.Count.ShouldBe(1);
            result[userId].ScanEmailDefinitions.ShouldContain(existingScanEmailDefinition);
        });

        newUserReference.ShouldSatisfyAllConditions(newUserReference =>
        {
            newUserReference.ScanEmailDefinitions.Count.ShouldBe(1);
            newUserReference.ScanEmailDefinitions.ShouldContain(existingScanEmailDefinition);
        });
    }

    [TestMethod]
    public void Handle_NewUserWithNullCollections_InitializesCollections()
    {
        // Arrange
        var result = new Dictionary<Guid, User>();
        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = "Test User",
            JobSchedules = null,
            ScanEmailDefinitions = null
        };
        var invoice = new Invoice { Id = Guid.NewGuid() };
        var jobSchedule = new JobSchedule { Id = Guid.NewGuid() };
        var scanEmailDefinition = new ScanEmailDefinition { Id = Guid.NewGuid() };
        var parameters = new UserParameters
        {
            Invoice = invoice,
            JobSchedule = jobSchedule,
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
            result[user.Id].ScanEmailDefinitions.ShouldContain(scanEmailDefinition);
        });

        user.ShouldSatisfyAllConditions(user =>
        {
            user.Invoices.Count.ShouldBe(1);
            user.JobSchedules.Count.ShouldBe(1);
            user.ScanEmailDefinitions.Count.ShouldBe(1);
            user.Invoices.ShouldContain(invoice);
            user.JobSchedules.ShouldContain(jobSchedule);
            user.ScanEmailDefinitions.ShouldContain(scanEmailDefinition);
        });
    }

    [TestMethod]
    public void Handle_ExistingUserWithNullCollections_AddsNewItems()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var existingUser = new User
        {
            Id = userId,
            Name = "Existing User",
            Invoices = [],
            JobSchedules = [],
            ScanEmailDefinitions = []
        };
        var result = new Dictionary<Guid, User> { { userId, existingUser } };
        var newUserReference = existingUser;
        var invoice = new Invoice { Id = Guid.NewGuid() };
        var jobSchedule = new JobSchedule { Id = Guid.NewGuid() };
        var scanEmailDefinition = new ScanEmailDefinition { Id = Guid.NewGuid() };
        var parameters = new UserParameters
        {
            Invoice = invoice,
            JobSchedule = jobSchedule,
            ScanEmailDefinition = scanEmailDefinition
        };

        // Act
        _ = result.Handle(ref newUserReference, parameters);

        // Assert
        result.ShouldSatisfyAllConditions(result =>
        {
            result.ShouldContainKey(userId);
            result[userId].Invoices.Count.ShouldBe(1);
            result[userId].JobSchedules.Count.ShouldBe(1);
            result[userId].ScanEmailDefinitions.Count.ShouldBe(1);
            result[userId].Invoices.ShouldContain(invoice);
            result[userId].JobSchedules.ShouldContain(jobSchedule);
            result[userId].ScanEmailDefinitions.ShouldContain(scanEmailDefinition);
        });

        newUserReference.ShouldSatisfyAllConditions(newUserReference =>
        {
            newUserReference.Invoices.Count.ShouldBe(1);
            newUserReference.JobSchedules.Count.ShouldBe(1);
            newUserReference.ScanEmailDefinitions.Count.ShouldBe(1);
            newUserReference.Invoices.ShouldContain(invoice);
            newUserReference.JobSchedules.ShouldContain(jobSchedule);
            newUserReference.ScanEmailDefinitions.ShouldContain(scanEmailDefinition);
        });
    }

    [TestMethod]
    public void Handle_ExistingUserWithNullParameters_DoesNotModifyUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var existingInvoice = new Invoice { Id = Guid.NewGuid() };
        var existingJobSchedule = new JobSchedule { Id = Guid.NewGuid() };
        var existingScanEmailDefinition = new ScanEmailDefinition { Id = Guid.NewGuid() };
        var existingUser = new User
        {
            Id = userId,
            Name = "Existing User",
            Invoices = [existingInvoice],
            JobSchedules = [existingJobSchedule],
            ScanEmailDefinitions = [existingScanEmailDefinition]
        };
        var result = new Dictionary<Guid, User> { { userId, existingUser } };
        var newUserReference = existingUser;
        var parameters = new UserParameters
        {
            Invoice = null,
            JobSchedule = null,
            ScanEmailDefinition = null
        };

        // Act
        _ = result.Handle(ref newUserReference, parameters);

        // Assert
        result.ShouldSatisfyAllConditions(result =>
        {
            result.ShouldContainKey(userId);
            result[userId].Invoices.ShouldContain(existingInvoice);
            result[userId].JobSchedules.ShouldContain(existingJobSchedule);
            result[userId].ScanEmailDefinitions.ShouldContain(existingScanEmailDefinition);
            result[userId].Invoices.Count.ShouldBe(1);
            result[userId].JobSchedules.Count.ShouldBe(1);
            result[userId].ScanEmailDefinitions.Count.ShouldBe(1);
        });

        newUserReference.ShouldSatisfyAllConditions(newUserReference =>
        {
            newUserReference.Invoices.Count.ShouldBe(1);
            newUserReference.JobSchedules.Count.ShouldBe(1);
            newUserReference.ScanEmailDefinitions.Count.ShouldBe(1);
            newUserReference.Invoices.ShouldContain(existingInvoice);
            newUserReference.JobSchedules.ShouldContain(existingJobSchedule);
            newUserReference.ScanEmailDefinitions.ShouldContain(existingScanEmailDefinition);
        });
    }

    [TestMethod]
    public void Handle_NewUserWithNullParameters_AddsUserWithEmptyCollections()
    {
        // Arrange
        var result = new Dictionary<Guid, User>();
        var user = new User { Id = Guid.NewGuid(), Name = "Test User" };
        var parameters = new UserParameters
        {
            Invoice = null,
            JobSchedule = null,
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
            result[user.Id].ScanEmailDefinitions.ShouldBeEmpty();
        });

        user.ShouldSatisfyAllConditions(user =>
        {
            user.Invoices.ShouldBeEmpty();
            user.JobSchedules.ShouldBeEmpty();
            user.ScanEmailDefinitions.ShouldBeEmpty();
        });
    }
}
