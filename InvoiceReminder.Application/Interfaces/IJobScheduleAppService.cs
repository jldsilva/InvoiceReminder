using InvoiceReminder.Application.ViewModels;
using InvoiceReminder.Domain.Abstractions;
using InvoiceReminder.Domain.Entities;

namespace InvoiceReminder.Application.Interfaces;
public interface IJobScheduleAppService : IBaseAppService<JobSchedule, JobScheduleViewModel>
{
    Task<Result<JobScheduleViewModel>> AddNewJobAsync(JobScheduleViewModel viewModel, CancellationToken cancellationToken = default);
    Task<Result<IEnumerable<JobScheduleViewModel>>> GetByUserIdAsync(Guid id);
}
