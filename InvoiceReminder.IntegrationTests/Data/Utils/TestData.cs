using Bogus;
using InvoiceReminder.Domain.Entities;
using InvoiceReminder.Domain.Enums;

namespace InvoiceReminder.IntegrationTests.Data.Utils;

public static class TestData
{
    public static Faker<User> UserFaker()
    {
        return new Faker<User>()
            .RuleFor(u => u.Id, _ => Guid.NewGuid())
            .RuleFor(u => u.TelegramChatId, f => f.Random.Long(100000000, long.MaxValue))
            .RuleFor(u => u.Name, f => f.Person.FullName)
            .RuleFor(u => u.Email, f => f.Internet.Email())
            .RuleFor(u => u.Password, f => f.Internet.Password(length: 16, memorable: false));
    }

    public static Faker<EmailAuthToken> EmailAuthTokenFaker()
    {
        return new Faker<EmailAuthToken>()
            .RuleFor(e => e.Id, faker => faker.Random.Guid())
            .RuleFor(e => e.UserId, faker => faker.Random.Guid())
            .RuleFor(e => e.AccessToken, faker => faker.Random.AlphaNumeric(128))
            .RuleFor(e => e.RefreshToken, faker => faker.Random.AlphaNumeric(128))
            .RuleFor(e => e.TokenProvider, faker => faker.PickRandom("Google", "Microsoft", "GitHub"))
            .RuleFor(e => e.NonceValue, faker => faker.Random.Hash())
            .RuleFor(e => e.AccessTokenExpiry, faker => faker.Date.Future().ToUniversalTime())
            .RuleFor(e => e.CreatedAt, faker => faker.Date.Past().ToUniversalTime())
            .RuleFor(e => e.UpdatedAt, faker => faker.Date.Recent().ToUniversalTime());
    }

    public static Faker<Invoice> InvoiceFaker()
    {
        return new Faker<Invoice>()
            .RuleFor(i => i.Id, faker => faker.Random.Guid())
            .RuleFor(i => i.UserId, faker => faker.Random.Guid())
            .RuleFor(i => i.Bank, faker => faker.PickRandom(
                "Banco do Brasil",
                "Bradesco",
                "Itaú",
                "Caixa Econômica Federal",
                "Santander",
                "Safra",
                "Citibank",
                "BTG Pactual"))
            .RuleFor(i => i.Beneficiary, faker => faker.Person.FullName)
            .RuleFor(i => i.Amount, faker => faker.Finance.Amount(10, 10000))
            .RuleFor(i => i.Barcode, faker => faker.Random.AlphaNumeric(44))
            .RuleFor(i => i.DueDate, faker => faker.Date.Future().ToUniversalTime())
            .RuleFor(i => i.CreatedAt, faker => faker.Date.Past().ToUniversalTime())
            .RuleFor(i => i.UpdatedAt, faker => faker.Date.Recent().ToUniversalTime());
    }

    public static Faker<JobSchedule> JobScheduleFaker()
    {
        return new Faker<JobSchedule>()
            .RuleFor(j => j.Id, faker => faker.Random.Guid())
            .RuleFor(j => j.UserId, faker => faker.Random.Guid())
            .RuleFor(j => j.CronExpression, faker => faker.PickRandom(
                "0 0 * * *",
                "0 */6 * * *",
                "0 */12 * * *",
                "0 9 * * MON",
                "0 9 * * MON-FRI",
                "0 0 1 * *"))
            .RuleFor(j => j.CreatedAt, faker => faker.Date.Past().ToUniversalTime())
            .RuleFor(j => j.UpdatedAt, faker => faker.Date.Recent().ToUniversalTime());
    }

    public static Faker<ScanEmailDefinition> ScanEmailDefinitionFaker()
    {
        return new Faker<ScanEmailDefinition>()
            .RuleFor(s => s.Id, faker => faker.Random.Guid())
            .RuleFor(s => s.UserId, faker => faker.Random.Guid())
            .RuleFor(s => s.InvoiceType, faker => faker.PickRandom(InvoiceType.AccountInvoice, InvoiceType.BankInvoice))
            .RuleFor(s => s.Beneficiary, faker => faker.Person.FullName)
            .RuleFor(s => s.Description, faker => faker.Lorem.Sentence())
            .RuleFor(s => s.SenderEmailAddress, faker => faker.Internet.Email())
            .RuleFor(s => s.AttachmentFileName, faker => faker.System.FileName("pdf"))
            .RuleFor(s => s.CreatedAt, faker => faker.Date.Past().ToUniversalTime())
            .RuleFor(s => s.UpdatedAt, faker => faker.Date.Recent().ToUniversalTime());
    }
}
