using InvoiceReminder.Domain.Entities;

namespace InvoiceReminder.Data.Interfaces;

public interface IJobScheduleRepository : IRepositoryBase<JobSchedule>
{
    Task<IEnumerable<JobSchedule>> GetByUserIdAsync(Guid id);
}
