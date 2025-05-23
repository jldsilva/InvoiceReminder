using InvoiceReminder.Domain.Entities;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Services;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;

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

    public async Task<IDictionary<string, byte[]>> GetAttachmentsAsync(string userEmail,
        IEnumerable<ScanEmailDefinition> scanEmailDefinitions)
    {
        var (_, credential) = await _oAuthProvider.AuthorizeAsync(userEmail);

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

        var response = await resource.ExecuteAsync();

        if (response != null && response.Messages != null)
        {
            var messages = response.Messages.Select(message => message.Id);
            var senderAddresses = scanEmailDefinitions.Select(x => x.SenderEmailAddress);
            var attachments = scanEmailDefinitions.Select(x => x.AttachmentFileName.ToLowerInvariant());

            foreach (var messageId in messages)
            {
                var email = await messagesResource.Get("me", messageId).ExecuteAsync();
                var from = email.Payload.Headers.First(h => h.Name == "From").Value;

                if (senderAddresses.Any(from.Contains))
                {
                    var beneficiary = scanEmailDefinitions.First(x => from.Contains(x.SenderEmailAddress)).Beneficiary;

                    var msgPart = email.Payload.Parts
                        .First(p => !string.IsNullOrWhiteSpace(p.Filename)
                        && attachments.Any(p.Filename.ToLowerInvariant().Contains));

                    var attachment = await messagesResource.Attachments.Get("me", messageId, msgPart.Body.AttachmentId)
                        .ExecuteAsync();

                    var modifyMessage = new ModifyMessageRequest
                    {
                        RemoveLabelIds = ["UNREAD"]
                    };

                    result.Add(beneficiary, Convert.FromBase64String(attachment.Data.Replace('-', '+').Replace('_', '/')));

                    _ = await service.Users.Messages.Modify(modifyMessage, "me", messageId).ExecuteAsync();
                }
            }
        }

        return result;
    }
}
