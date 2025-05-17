using InvoiceReminder.Data.Interfaces;
using InvoiceReminder.Domain.Services.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace InvoiceReminder.ExternalServices.BackgroundServices;

[ExcludeFromCodeCoverage]
public class TelegramBotBackgroundService : BackgroundService
{
    private readonly ILogger<TelegramBotBackgroundService> _logger;
    private readonly TelegramBotClient _botClient;
    private readonly TelegramUpdateHandler _updateHandler;

    public TelegramBotBackgroundService(
        ILogger<TelegramBotBackgroundService> logger,
        IConfigurationService configService,
        IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _updateHandler = new TelegramUpdateHandler(scopeFactory);
        _botClient = new TelegramBotClient(configService.GetSecret("appKeys", "TelegramBotToken"));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await ClearMessageQueue(stoppingToken);

        _botClient.StartReceiving(
            _updateHandler,
            receiverOptions: new ReceiverOptions { AllowedUpdates = [] },
            cancellationToken: stoppingToken
        );

        _logger.LogInformation("Telegram Bot Service is now listening for chat interactions...");

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Telegram Bot Service is now stopping...");

        await base.StopAsync(cancellationToken);
    }

    private async Task ClearMessageQueue(CancellationToken cancellationToken)
    {
        var updates = await _botClient.GetUpdates(offset: -1, cancellationToken: cancellationToken);

        if (updates.Length > 0)
        {
            _ = await _botClient.GetUpdates(offset: updates[^1].Id + 1, cancellationToken: cancellationToken);
        }
    }
}

[ExcludeFromCodeCoverage]
internal sealed class TelegramUpdateHandler : IUpdateHandler
{
    private readonly ILogger<TelegramUpdateHandler> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    public TelegramUpdateHandler(IServiceScopeFactory scopeFactory)
    {
        _logger = new LoggerFactory().CreateLogger<TelegramUpdateHandler>();
        _scopeFactory = scopeFactory;
    }

    public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        if (update.Message is null || string.IsNullOrWhiteSpace(update.Message.Text))
        {
            return;
        }

        _ = await botClient.SendMessage(
            chatId: update.Message.Chat.Id,
            text: await HandleMessageAsync(update.Message),
            parseMode: ParseMode.Html,
            cancellationToken: cancellationToken
        );
    }

    public async Task HandleErrorAsync(
        ITelegramBotClient botClient,
        Exception exception,
        HandleErrorSource source,
        CancellationToken cancellationToken)
    {
        if (exception is ApiRequestException apiException)
        {
            _logger.LogError(
                exception,
                "Telegram API Error: [{ErrorCode}] {Message}",
                apiException.ErrorCode,
                exception.Message
            );
        }
        else
        {
            _logger.LogError(exception, "Unexpected Error: {Message}", exception.Message);
        }

        await Task.CompletedTask;
    }

    private async Task<string> HandleMessageAsync(Message message)
    {
        string response;
        var command = message.Text.Split(' ').FirstOrDefault()?.ToLowerInvariant().Trim();

        switch (command)
        {
            case "/get_id":
                response = $"O Id deste chat é <b>{message.Chat.Id}</b>";
                break;
            case "/set_id":
                {
                    using var scope = _scopeFactory.CreateScope();
                    var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                    var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
                    var content = message.Text.Split(' ').LastOrDefault()?.ToLowerInvariant().Trim();
                    var result = await userRepository.GetByEmailAsync(content);

                    if (result is null)
                    {
                        response = "Usuário não encontrado!";
                        break;
                    }

                    result.TelegramChatId = message.Chat.Id;
                    _ = userRepository.Update(result);
                    await unitOfWork.SaveChangesAsync();
                    response = "Id de chat adicionado com sucesso!";
                }
                break;
            default:
                response = """
                <b>Os comandos disponíveis atualmente são:</b>
                <b>• /get_id</b> - Retorna o Id deste chat
                <b>• /set_id</b> - Adiciona o Id deste chat ao usuário
                no seguinte formato: /set_id seuemail@gmail.com
                """;
                break;
        }

        return response;
    }
}
