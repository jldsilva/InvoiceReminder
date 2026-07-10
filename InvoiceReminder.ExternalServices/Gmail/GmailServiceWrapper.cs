using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Services;
using InvoiceReminder.Domain.Entities;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.RegularExpressions;

namespace InvoiceReminder.ExternalServices.Gmail;

[ExcludeFromCodeCoverage]
public class GmailServiceWrapper : IGmailServiceWrapper
{
    private readonly ILogger<GmailServiceWrapper> _logger;
    private readonly IGoogleOAuthService _oAuthProvider;

    public GmailServiceWrapper(ILogger<GmailServiceWrapper> logger, IGoogleOAuthService oAuthProvider)
    {
        _logger = logger;
        _oAuthProvider = oAuthProvider;
    }

    public async Task<IDictionary<string, byte[]>> GetAttachmentsAsync(
        User user,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(user);

        var emailAuthToken = user.EmailAuthTokens.FirstOrDefault(t => t.TokenProvider == "Google");
        var credential = await _oAuthProvider.AuthenticateAsync(emailAuthToken, cancellationToken);

        var service = new GmailService(new BaseClientService.Initializer()
        {
            HttpClientInitializer = credential,
            ApplicationName = "Gmail API"
        });

        _logger.LogInformation("Launching Gmail service...");

        var result = new Dictionary<string, byte[]>();
        var messagesResource = service.Users.Messages;
        var resource = messagesResource.List("me");
        resource.LabelIds = "INBOX";
        resource.Q = "is:unread";

        var response = await resource.ExecuteAsync(cancellationToken);

        if (response != null && response.Messages != null)
        {
            var messages = response.Messages.Select(message => message.Id);
            var senderAddresses = user.ScanEmailDefinitions.Select(x => x.SenderEmailAddress);
            var attachments = user.ScanEmailDefinitions.Select(x => x.AttachmentFileName.ToLowerInvariant());

            foreach (var messageId in messages)
            {
                var emailMessage = await messagesResource.Get("me", messageId).ExecuteAsync(cancellationToken);
                var emailAddress = FilterEmailAddress(emailMessage.Payload.Headers.First(h => h.Name == "From").Value);
                var emailSentDate = emailMessage.Payload.Headers.First(h => h.Name == "Date").Value;

                if (IsEmailFromCurrentMonth(emailSentDate) && senderAddresses.Any(emailAddress.Contains))
                {
                    var msgPart = emailMessage.Payload.Parts
                        .First(p => !string.IsNullOrWhiteSpace(p.Filename)
                        && attachments.Any(p.Filename.ToLowerInvariant().Contains));

                    var attachment = await messagesResource.Attachments.Get("me", messageId, msgPart.Body.AttachmentId)
                        .ExecuteAsync(cancellationToken);

                    var modifyMessage = new ModifyMessageRequest
                    {
                        RemoveLabelIds = ["UNREAD"]
                    };

                    result.Add(emailAddress, Convert.FromBase64String(attachment.Data.Replace('-', '+')
                        .Replace('_', '/')));

                    _ = await service.Users.Messages.Modify(modifyMessage, "me", messageId)
                        .ExecuteAsync(cancellationToken);
                }
            }
        }

        return result;
    }

    private static string FilterEmailAddress(string email)
    {
        var pattern = @"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}";
        var match = Regex.Match(email, pattern, RegexOptions.None, TimeSpan.FromMilliseconds(10));

        return match.Value;
    }

    private static bool IsEmailFromCurrentMonth(string emailDateString)
    {
        try
        {
            var emailDate = DateTime.Parse(emailDateString, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces);
            var today = DateTime.UtcNow;

            return emailDate.Year == today.Year && emailDate.Month == today.Month;
        }
        catch
        {
            return false;
        }
    }
}
