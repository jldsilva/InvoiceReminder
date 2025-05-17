using InvoiceReminder.ExternalServices.SendMessage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quartz;

namespace InvoiceReminder.JobScheduler.JobSettings;

public class CronJob : IJob
{
    private readonly ILogger<CronJob> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public CronJob(ILogger<CronJob> logger, IServiceScopeFactory serviceScopeFactory)
    {
        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        using var scope = _serviceScopeFactory.CreateScope();
        var id = context.MergedJobDataMap.GetGuidValue("UserId");
        var service = scope.ServiceProvider.GetRequiredService<ISendMessageService>();
        var message = $"{DateTime.Now:HH:mm:ss} - {context.JobDetail.Description} triggered...";

        _logger.LogInformation("{Message}", message);
        // possível uso de um agendamento de envio de mensagem para lembrete no dia do vencimento?...
        _ = await service.SendMessage(id);
    }
}
