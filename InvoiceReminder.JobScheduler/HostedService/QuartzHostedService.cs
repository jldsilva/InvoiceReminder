using InvoiceReminder.Domain.Entities;
using InvoiceReminder.JobScheduler.JobSettings;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Quartz;
using Quartz.Spi;

namespace InvoiceReminder.JobScheduler.HostedService;

public class QuartzHostedService : IHostedService
{
    private readonly ILogger<QuartzHostedService> _logger;
    private readonly ISchedulerFactory _schedulerFactory;
    private readonly IJobFactory _jobFactory;
    private readonly IEnumerable<JobSchedule> _schedules;

    public IScheduler Scheduler { get; private set; }

    public QuartzHostedService(
        ILogger<QuartzHostedService> logger,
        ISchedulerFactory schedulerFactory,
        IJobFactory jobFactory,
        IEnumerable<JobSchedule> schedules)
    {
        _logger = logger;
        _schedulerFactory = schedulerFactory;
        _jobFactory = jobFactory;
        _schedules = schedules ?? [];
    }

    public QuartzHostedService(IJobFactory jobFactory, ISchedulerFactory schedulerFactory)
    {
        _jobFactory = jobFactory;
        _schedulerFactory = schedulerFactory;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        Scheduler = await _schedulerFactory.GetScheduler(cancellationToken);
        Scheduler.JobFactory = _jobFactory;

        foreach (var schedule in _schedules)
        {
            if (!CronExpression.IsValidExpression(schedule.CronExpression))
            {
                if (_logger.IsEnabled(LogLevel.Error))
                {
                    _logger.LogError("CronJob inválido: {JobId}", schedule.Id);
                }

                continue;
            }

            var job = CreateJob(schedule);
            var trigger = CreateTrigger(schedule);

            _ = await Scheduler.ScheduleJob(job, trigger, cancellationToken);
        }

        if (!Scheduler.IsStarted)
        {
            await Scheduler.Start(cancellationToken);
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (Scheduler is not null && !Scheduler.IsShutdown)
        {
            await Scheduler.Shutdown(cancellationToken);
        }
    }

    public async Task ScheduleJobAsync(JobSchedule schedule, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(schedule);

        await EnsureSchedulerInitializedAsync(cancellationToken);

        _ = await Scheduler.ScheduleJob(CreateJob(schedule), CreateTrigger(schedule), cancellationToken);

        if (!Scheduler.IsStarted)
        {
            await Scheduler.Start(cancellationToken);
        }
    }

    public async Task UpdateJobScheduleAsync(JobSchedule schedule, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(schedule);

        await DeleteJobAsync(schedule, cancellationToken);

        await ScheduleJobAsync(schedule, cancellationToken);
    }

    public async Task DeleteJobAsync(JobSchedule schedule, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(schedule);

        await EnsureSchedulerInitializedAsync(cancellationToken);

        var jobKey = new JobKey($"{schedule.Id}.job");

        if (await Scheduler.CheckExists(jobKey, cancellationToken))
        {
            _ = await Scheduler.DeleteJob(jobKey, cancellationToken);
        }
    }

    public async Task PauseJobAsync(JobSchedule schedule, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(schedule);

        await EnsureSchedulerInitializedAsync(cancellationToken);

        var jobKey = new JobKey($"{schedule.Id}.job");
        var triggerKey = new TriggerKey($"{schedule.Id}.trigger");

        await Scheduler.PauseTrigger(triggerKey, cancellationToken);
        await Scheduler.PauseJob(jobKey, cancellationToken);
    }

    public async Task ResumeJobAsync(JobSchedule schedule, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(schedule);

        await EnsureSchedulerInitializedAsync(cancellationToken);

        var jobKey = new JobKey($"{schedule.Id}.job");
        var triggerKey = new TriggerKey($"{schedule.Id}.trigger");

        await Scheduler.ResumeJob(jobKey, cancellationToken);
        await Scheduler.ResumeTrigger(triggerKey, cancellationToken);
    }

    private async Task EnsureSchedulerInitializedAsync(CancellationToken cancellationToken)
    {
        if (Scheduler is null)
        {
            Scheduler = await _schedulerFactory.GetScheduler(cancellationToken);
            Scheduler.JobFactory = _jobFactory;
        }
    }

    private static ITrigger CreateTrigger(JobSchedule schedule)
    {
        var jobType = typeof(CronJob);

        return TriggerBuilder.Create()
            .WithIdentity($"{schedule.Id}.trigger")
            .WithCronSchedule(schedule.CronExpression)
            .WithDescription($"{jobType.Name} [{schedule.UserId}].trigger")
            .Build();
    }

    private static IJobDetail CreateJob(JobSchedule schedule)
    {
        var jobType = typeof(CronJob);
        var data = new JobDataMap(new Dictionary<string, Guid>
        {
            { "UserId", schedule.UserId }
        });

        return JobBuilder.Create(jobType)
            .WithIdentity($"{schedule.Id}.job")
            .WithDescription($"{jobType.Name} [{schedule.UserId}].job")
            .UsingJobData(data)
            .Build();
    }
}
