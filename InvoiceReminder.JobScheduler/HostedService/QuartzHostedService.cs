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

    public IScheduler Scheduler { get; set; }

    public QuartzHostedService(
        ILogger<QuartzHostedService> logger,
        ISchedulerFactory schedulerFactory,
        IJobFactory jobFactory,
        IEnumerable<JobSchedule> schedules)
    {
        _logger = logger;
        _schedulerFactory = schedulerFactory;
        _jobFactory = jobFactory;
        _schedules = schedules;
    }

    public QuartzHostedService(IJobFactory jobFactory, ISchedulerFactory schedulerFactory)
    {
        _jobFactory = jobFactory;
        _schedulerFactory = schedulerFactory;
    }

    public async Task ScheduleJobAsync(JobSchedule schedule)
    {
        _ = schedule ?? throw new ArgumentNullException(nameof(schedule));

        Scheduler = await _schedulerFactory.GetScheduler();
        Scheduler.JobFactory = _jobFactory;

        _ = await Scheduler.ScheduleJob(CreateJob(schedule), CreateTrigger(schedule));
        await Scheduler.Start();
    }

    public async Task ReScheduleJobAsync(JobSchedule schedule)
    {
        _ = schedule ?? throw new ArgumentNullException(nameof(schedule));

        using var cts = new CancellationTokenSource();
        var jobKey = new JobKey($"{schedule.Id}.job");

        Scheduler = await _schedulerFactory.GetScheduler(cts.Token);
        Scheduler.JobFactory = _jobFactory;

        if (await Scheduler.CheckExists(jobKey))
        {
            await cts.CancelAsync();
            _ = await Scheduler.DeleteJob(jobKey, cts.Token);
            await Task.Delay(500);
        }

        _ = await Scheduler.ScheduleJob(CreateJob(schedule), CreateTrigger(schedule));
        await Scheduler.Start();
    }

    public async Task RemoveJobAsync(JobSchedule schedule)
    {
        _ = schedule ?? throw new ArgumentNullException(nameof(schedule));

        using var cts = new CancellationTokenSource();
        Scheduler = await _schedulerFactory.GetScheduler(cts.Token);
        Scheduler.JobFactory = _jobFactory;

        await cts.CancelAsync();
        var jobKey = new JobKey($"{schedule.Id}.job");
        _ = await Scheduler.DeleteJob(jobKey, cts.Token);
    }

    public async Task PauseJobAsync(JobSchedule schedule)
    {
        _ = schedule ?? throw new ArgumentNullException(nameof(schedule));

        using var cts = new CancellationTokenSource();
        Scheduler = await _schedulerFactory.GetScheduler();
        Scheduler.JobFactory = _jobFactory;
        await cts.CancelAsync();

        var triggerKey = new TriggerKey($"{schedule.Id}.trigger");
        await Scheduler.PauseTrigger(triggerKey, cts.Token);

        var jobKey = new JobKey($"{schedule.Id}.job");
        await Scheduler.PauseJob(jobKey, cts.Token);
    }

    public async Task ResumeJobAsync(JobSchedule schedule)
    {
        _ = schedule ?? throw new ArgumentNullException(nameof(schedule));

        var jobKey = new JobKey($"{schedule.Id}.job");
        var triggerKey = new TriggerKey($"{schedule.Id}.trigger");

        Scheduler = await _schedulerFactory.GetScheduler();
        Scheduler.JobFactory = _jobFactory;

        await Scheduler.ResumeJob(jobKey);
        await Scheduler.ResumeTrigger(triggerKey);
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _schedulerFactory.GetScheduler(cancellationToken).Dispose();

        Scheduler = await _schedulerFactory.GetScheduler(cancellationToken);
        Scheduler.JobFactory = _jobFactory;

        foreach (var schedule in _schedules)
        {
            if (!CronExpression.IsValidExpression(schedule.CronExpression))
            {
                var exception = new SchedulerException("Invalid Cron expression");

                _logger.LogError(exception, "Starting Job raised an Excepetion: {CronExpression}", schedule.CronExpression);

                throw exception;
            }

            var job = CreateJob(schedule);
            var trigger = CreateTrigger(schedule);

            _ = await Scheduler.ScheduleJob(job, trigger, cancellationToken);
            await Scheduler.Start(cancellationToken);
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await Scheduler.Shutdown(cancellationToken);
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
