using InvoiceReminder.Application.Interfaces;
using InvoiceReminder.Application.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace InvoiceReminder.API.Endpoints;

public class JobScheduleEndpoints : IEndpointDefinition
{
    private const string basepath = "/api/job_schedule";

    public void RegisterEndpoints(IEndpointRouteBuilder endpoints)
    {
        var endpoint = endpoints.MapGroup(basepath).WithName("JobScheduleEndpoints");

        MapGetJobSchedules(endpoint);
        MapGetJobSchedule(endpoint);
        MapGetJobScheduleByUserId(endpoint);
        MapCreateJobSchedule(endpoint);
        MapUpdateJobSchedule(endpoint);
        MapDeleteJobSchedule(endpoint);
    }

    private static void MapGetJobSchedules(RouteGroupBuilder endpoint)
    {
        _ = endpoint.MapGet("/", (IJobScheduleAppService appService) =>
            {
                var result = appService.GetAll();

                return result.IsSuccess
                    ? Results.Ok(result.Value)
                    : Results.Problem(result.Error);
            })
            .WithName("GetJobSchedules")
            .RequireAuthorization()
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status500InternalServerError);
    }

    private static void MapGetJobSchedule(RouteGroupBuilder endpoint)
    {
        _ = endpoint.MapGet("/{id}",
            async (IJobScheduleAppService appService, Guid id, CancellationToken ct) =>
            {
                var result = await appService.GetByIdAsync(id, ct);

                return result.IsSuccess
                    ? Results.Ok(result.Value)
                    : Results.NotFound(result.Error);
            })
            .WithName("GetJobSchedule")
            .RequireAuthorization()
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status500InternalServerError);
    }

    private static void MapGetJobScheduleByUserId(RouteGroupBuilder endpoint)
    {
        _ = endpoint.MapGet("/getby-userid/{id}",
            async (IJobScheduleAppService appService, Guid id, CancellationToken ct) =>
            {
                var result = await appService.GetByUserIdAsync(id, ct);

                return result.IsSuccess
                    ? Results.Ok(result.Value)
                    : Results.NotFound(result.Error);
            })
            .WithName("GetJobScheduleByUserId")
            .RequireAuthorization()
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status500InternalServerError);
    }

    private static void MapCreateJobSchedule(RouteGroupBuilder endpoint)
    {
        _ = endpoint.MapPost("/",
            async (IJobScheduleAppService appService, [FromBody] JobScheduleViewModel viewModel,
            CancellationToken ct) =>
            {
                var result = await appService.AddNewJobAsync(viewModel, ct);

                return result.IsSuccess
                    ? Results.Created($"{basepath}/{result.Value.Id}", result.Value)
                    : Results.Problem(result.Error);
            })
            .WithName("CreateJobSchedule")
            .RequireAuthorization()
            .Produces(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status500InternalServerError);
    }

    private static void MapUpdateJobSchedule(RouteGroupBuilder endpoint)
    {
        _ = endpoint.MapPut("/",
            async (IJobScheduleAppService appService, [FromBody] JobScheduleViewModel viewModel,
            CancellationToken ct) =>
            {
                var result = await appService.UpdateAsync(viewModel, ct);

                return result.IsSuccess
                    ? Results.Ok(result.Value)
                    : Results.Problem(result.Error);
            })
            .WithName("UpdateJobSchedule")
            .RequireAuthorization()
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status500InternalServerError);
    }

    private static void MapDeleteJobSchedule(RouteGroupBuilder endpoint)
    {
        _ = endpoint.MapDelete("/",
            async (IJobScheduleAppService appService, [FromBody] JobScheduleViewModel viewModel,
            CancellationToken ct) =>
            {
                var result = await appService.RemoveAsync(viewModel, ct);

                return result.IsSuccess
                    ? Results.NoContent()
                    : Results.Problem(result.Error);
            })
            .WithName("DeleteJobSchedule")
            .RequireAuthorization()
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status500InternalServerError);
    }
}
