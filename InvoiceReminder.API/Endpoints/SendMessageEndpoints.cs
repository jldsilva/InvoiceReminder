using InvoiceReminder.ExternalServices.SendMessage;

namespace InvoiceReminder.API.Endpoints;

public class SendMessageEndpoints : IEndpointDefinition
{
    public void RegisterEndpoints(IEndpointRouteBuilder endpoints)
    {
        _ = endpoints.MapGet("/api/send_message/{id}",
            async (ISendMessageService messageService, CancellationToken ct, Guid id) =>
            {
                var result = await messageService.SendMessage(id, ct);

                return !string.IsNullOrEmpty(result)
                    ? Results.Ok(result)
                    : Results.Problem(result);
            })
            .WithName("SendMessage")
            .RequireAuthorization()
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status500InternalServerError);
    }
}
