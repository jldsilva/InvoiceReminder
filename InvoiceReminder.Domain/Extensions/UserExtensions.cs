using InvoiceReminder.Domain.Entities;

namespace InvoiceReminder.Domain.Extensions;

public static class UserExtensions
{
    public static IDictionary<Guid, User> Handle(
        this IDictionary<Guid, User> result,
        ref User user,
        UserParameters parameters)
    {
        if (!result.TryGetValue(user.Id, out var existingUser))
        {
            parameters.Invoice.AddIfNotExists(user.Invoices);
            parameters.JobSchedule.AddIfNotExists(user.JobSchedules);
            parameters.EmailAuthToken.AddIfNotExists(user.EmailAuthTokens);
            parameters.ScanEmailDefinition.AddIfNotExists(user.ScanEmailDefinitions);

            result.Add(user.Id, user);
        }
        else
        {
            parameters.Invoice.AddIfNotExists(existingUser.Invoices);
            parameters.JobSchedule.AddIfNotExists(existingUser.JobSchedules);
            parameters.EmailAuthToken.AddIfNotExists(existingUser.EmailAuthTokens);
            parameters.ScanEmailDefinition.AddIfNotExists(existingUser.ScanEmailDefinitions);
        }

        return result;
    }
}
