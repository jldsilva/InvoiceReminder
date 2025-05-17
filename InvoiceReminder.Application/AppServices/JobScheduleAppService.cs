using InvoiceReminder.Application.Abstractions;
using InvoiceReminder.Application.Interfaces;
using InvoiceReminder.Application.ViewModels;
using InvoiceReminder.Data.Interfaces;
using InvoiceReminder.Domain.Entities;
using InvoiceReminder.JobScheduler.HostedService;
using Mapster;
using Quartz;
using Quartz.Spi;

namespace InvoiceReminder.Application.AppServices;

public class JobScheduleAppService : AppServiceBase<JobSchedule, JobScheduleViewModel>, IJobScheduleAppService
{
    private readonly QuartzHostedService _quartz;
    private readonly IJobScheduleRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public JobScheduleAppService(
        IJobScheduleRepository repository,
        ISchedulerFactory schedulerFactory,
        IJobFactory jobFactory,
        IUnitOfWork unitOfWork) : base(repository, unitOfWork)
    {
        _quartz = new QuartzHostedService(jobFactory, schedulerFactory);
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<JobScheduleViewModel>> AddNewJobAsync(JobScheduleViewModel viewModel)
    {
        if (viewModel is null)
        {
            return Result<JobScheduleViewModel>.Failure($"Parameter {nameof(viewModel)} was Null.");
        }

        var entity = viewModel.Adapt<JobSchedule>();

        _ = await _repository.AddAsync(entity);
        await _unitOfWork.SaveChangesAsync();
        await _quartz.ScheduleJobAsync(entity);

        return Result<JobScheduleViewModel>.Success(entity.Adapt<JobScheduleViewModel>());
    }

    public async Task<Result<IEnumerable<JobScheduleViewModel>>> GetByUserIdAsync(Guid id)
    {
        var entity = await _repository.GetByUserIdAsync(id);

        return !entity.Any()
            ? Result<IEnumerable<JobScheduleViewModel>>.Failure("Empty Result")
            : Result<IEnumerable<JobScheduleViewModel>>.Success(entity.Adapt<IEnumerable<JobScheduleViewModel>>());
    }
}
