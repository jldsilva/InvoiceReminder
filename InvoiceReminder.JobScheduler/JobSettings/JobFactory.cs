using Microsoft.Extensions.DependencyInjection;
using Quartz;
using Quartz.Spi;
using System.Diagnostics.CodeAnalysis;

namespace InvoiceReminder.JobScheduler.JobSettings;

[ExcludeFromCodeCoverage]
public class JobFactory : IJobFactory
{
    private readonly IServiceProvider _serviceProvider;

    public JobFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public IJob NewJob(TriggerFiredBundle bundle, IScheduler scheduler)
    {
        _ = bundle ?? throw new ArgumentNullException(typeof(TriggerFiredBundle).Name);

        return _serviceProvider.GetRequiredService(bundle.JobDetail.JobType) as IJob;
    }

    public void ReturnJob(IJob job)
    {
        // DI container will handle this
    }
}
