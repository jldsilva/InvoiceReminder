using InvoiceReminder.Domain.Entities;

namespace InvoiceReminder.Data.Interfaces;

public interface IJobScheduleRepository : IBaseRepository<JobSchedule>
{
    Task<IEnumerable<JobSchedule>> GetByUserIdAsync(Guid id, CancellationToken cancellationToken = default);
}
