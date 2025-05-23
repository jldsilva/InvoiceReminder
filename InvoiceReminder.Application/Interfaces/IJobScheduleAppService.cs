using InvoiceReminder.Application.Abstractions;
using InvoiceReminder.Application.ViewModels;
using InvoiceReminder.Domain.Entities;

namespace InvoiceReminder.Application.Interfaces;
public interface IJobScheduleAppService : IBaseAppService<JobSchedule, JobScheduleViewModel>
{
    Task<Result<JobScheduleViewModel>> AddNewJobAsync(JobScheduleViewModel viewModel);
    Task<Result<IEnumerable<JobScheduleViewModel>>> GetByUserIdAsync(Guid id);
}
