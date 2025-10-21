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
            user.Invoices ??= [];
            user.JobSchedules ??= [];
            user.EmailAuthTokens ??= [];
            user.ScanEmailDefinitions ??= [];

            if (parameters.Invoice is not null &&
                user.Invoices.FirstOrDefault(i => i.Id == parameters.Invoice.Id) is null)
            {
                user.Invoices.Add(parameters.Invoice);
            }

            if (parameters.JobSchedule is not null &&
                user.JobSchedules.FirstOrDefault(js => js.Id == parameters.JobSchedule.Id) is null)
            {
                user.JobSchedules.Add(parameters.JobSchedule);
            }

            if (parameters.EmailAuthToken is not null &&
                user.EmailAuthTokens.FirstOrDefault(eat => eat.Id == parameters.EmailAuthToken.Id) is null)
            {
                user.EmailAuthTokens.Add(parameters.EmailAuthToken);
            }

            if (parameters.ScanEmailDefinition is not null &&
                user.ScanEmailDefinitions.FirstOrDefault(sed => sed.Id == parameters.ScanEmailDefinition.Id) is null)
            {
                user.ScanEmailDefinitions.Add(parameters.ScanEmailDefinition);
            }

            result.Add(user.Id, user);
        }
        else
        {
            if (parameters.Invoice is not null &&
                existingUser.Invoices.FirstOrDefault(i => i.Id == parameters.Invoice.Id) is null)
            {
                existingUser.Invoices.Add(parameters.Invoice);
            }

            if (parameters.JobSchedule is not null &&
                existingUser.JobSchedules.FirstOrDefault(js => js.Id == parameters.JobSchedule.Id) is null)
            {
                existingUser.JobSchedules.Add(parameters.JobSchedule);
            }

            if (parameters.EmailAuthToken is not null &&
                existingUser.EmailAuthTokens.FirstOrDefault(eat => eat.Id == parameters.EmailAuthToken.Id) is null)
            {
                existingUser.EmailAuthTokens.Add(parameters.EmailAuthToken);
            }

            if (parameters.ScanEmailDefinition is not null &&
                existingUser.ScanEmailDefinitions.FirstOrDefault(sed => sed.Id == parameters.ScanEmailDefinition.Id) is null)
            {
                existingUser.ScanEmailDefinitions.Add(parameters.ScanEmailDefinition);
            }
        }

        return result;
    }
}
