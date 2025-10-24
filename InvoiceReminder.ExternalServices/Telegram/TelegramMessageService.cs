using InvoiceReminder.Domain.Services.Configuration;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types.Enums;

namespace InvoiceReminder.ExternalServices.Telegram;

[ExcludeFromCodeCoverage]
public class TelegramMessageService : ITelegramMessageService
{
    private readonly ILogger<TelegramMessageService> _logger;
    private readonly ITelegramBotClient _botClient;

    public TelegramMessageService(ILogger<TelegramMessageService> logger, IConfigurationService configurationService)
    {
        var botToken = configurationService.GetSecret("appKeys", "TelegramBotToken");

        _logger = logger;
        _botClient = new TelegramBotClient(botToken);
    }

    public async Task SendMessageAsync(long chatId, string message, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation($"Sending a new message...");

        try
        {
            _ = await _botClient.SendMessage(chatId, message, ParseMode.Html, cancellationToken: cancellationToken);
        }
        catch (ApiRequestException ex)
        {
            _logger.LogError(ex, "{Message}", $"Telegram API error: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Message}", $"Unexpected error: {ex.Message}");
        }
    }
}
