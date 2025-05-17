namespace InvoiceReminder.API.Endpoints;

public interface IEndpointDefinition
{
    void RegisterEndpoints(IEndpointRouteBuilder endpoints);
}
