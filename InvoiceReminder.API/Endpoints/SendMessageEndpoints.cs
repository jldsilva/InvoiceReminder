using InvoiceReminder.ExternalServices.SendMessage;

namespace InvoiceReminder.API.Endpoints;

public class SendMessageEndpoints : IEndpointDefinition
{
    private const string basepath = "/api/send_message";

    public void RegisterEndpoints(IEndpointRouteBuilder endpoints)
    {
        var endpoint = endpoints.MapGroup(basepath).WithName("SendMessageEndpoints");

        MapSendMessage(endpoint);
    }

    private static void MapSendMessage(RouteGroupBuilder endpoint)
    {
        _ = endpoint.MapGet("/{id}",
            async (ISendMessageService messageService, CancellationToken ct, Guid id) =>
            {
                var result = await messageService.SendMessage(id, ct);

                return !string.IsNullOrEmpty(result)
                    ? Results.Ok(result)
                    : Results.Problem(
                        statusCode: StatusCodes.Status500InternalServerError,
                        title: "Erro ao enviar mensagem",
                        detail: "O serviço não retornou um resultado válido."
                    );
            })
            .WithName("SendMessage")
            .RequireAuthorization()
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status500InternalServerError);
    }
}
