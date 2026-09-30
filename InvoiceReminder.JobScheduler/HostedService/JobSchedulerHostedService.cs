using InvoiceReminder.Domain.Entities;
using InvoiceReminder.JobScheduler.JobSettings;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Quartz;

namespace InvoiceReminder.JobScheduler.HostedService;

public class JobSchedulerHostedService : IHostedService
{
    private readonly ILogger<JobSchedulerHostedService> _logger;
    private readonly ISchedulerFactory _schedulerFactory;
    private readonly IEnumerable<JobSchedule> _schedules;

    public IScheduler Scheduler { get; private set; }

    public JobSchedulerHostedService(
        ILogger<JobSchedulerHostedService> logger,
        ISchedulerFactory schedulerFactory,
        IEnumerable<JobSchedule> schedules)
    {
        _logger = logger;
        _schedulerFactory = schedulerFactory;
        _schedules = schedules ?? [];
    }

    public JobSchedulerHostedService(ISchedulerFactory schedulerFactory)
    {
        _schedulerFactory = schedulerFactory;
        _schedules = [];
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        Scheduler = await _schedulerFactory.GetScheduler(cancellationToken);

        foreach (var schedule in _schedules)
        {
            if (!ValidateCronExpression(schedule.CronExpression))
            {
                if (_logger?.IsEnabled(LogLevel.Error) ?? false)
                {
                    _logger?.LogError("CronJob inválido: {JobId}", schedule.Id);
                }

                continue;
            }

            var job = CreateJob(schedule);
            var trigger = CreateTrigger(schedule);
            var opts = new ScheduleJobOptions { Replace = false };

            _ = await Scheduler.ScheduleJob(job, trigger, opts, cancellationToken);
        }

        await Scheduler.Start(cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (Scheduler is not null)
        {
            await Scheduler.Shutdown(waitForJobsToComplete: true, cancellationToken);
        }
    }

    public async Task ScheduleJobAsync(JobSchedule schedule, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(schedule);

        await EnsureSchedulerInitializedAsync(cancellationToken);

        var job = CreateJob(schedule);
        var trigger = CreateTrigger(schedule);
        var opts = new ScheduleJobOptions { Replace = true };

        _ = await Scheduler.ScheduleJob(job, trigger, opts, cancellationToken);

        await Scheduler.Start(cancellationToken);
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

        try
        {
            _ = await Scheduler.DeleteJob(jobKey, cancellationToken);
        }
        catch (SchedulerException)
        {
            // Job doesn't exist, safe to continue
        }
    }

    public async Task PauseJobAsync(JobSchedule schedule, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(schedule);

        await EnsureSchedulerInitializedAsync(cancellationToken);

        var jobKey = new JobKey($"{schedule.Id}.job");
        var triggerKey = new TriggerKey($"{schedule.Id}.trigger");

        _ = await Scheduler.PauseTrigger(triggerKey, cancellationToken);
        _ = await Scheduler.PauseJob(jobKey, cancellationToken);
    }

    public async Task ResumeJobAsync(JobSchedule schedule, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(schedule);

        await EnsureSchedulerInitializedAsync(cancellationToken);

        var jobKey = new JobKey($"{schedule.Id}.job");
        var triggerKey = new TriggerKey($"{schedule.Id}.trigger");

        _ = await Scheduler.ResumeJob(jobKey, cancellationToken);
        _ = await Scheduler.ResumeTrigger(triggerKey, cancellationToken);
    }

    private async Task EnsureSchedulerInitializedAsync(CancellationToken cancellationToken)
    {
        Scheduler ??= await _schedulerFactory.GetScheduler(cancellationToken);
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
        return JobBuilder.Create<CronJob>()
            .WithIdentity($"{schedule.Id}.job")
            .WithDescription($"CronJob [{schedule.UserId}].job")
            .UsingJobData("UserId", schedule.UserId)
            .Build();
    }

    private static bool ValidateCronExpression(string cronExpression)
    {
        try
        {
            _ = new CronExpression(cronExpression);
            return true;
        }
        catch
        {
            return false;
        }
    }
}

