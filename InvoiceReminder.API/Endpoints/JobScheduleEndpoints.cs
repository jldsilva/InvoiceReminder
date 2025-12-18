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
        _ = endpoint.MapGet("/", (IJobScheduleAppService jobScheduleAppService) =>
            {
                var result = jobScheduleAppService.GetAll();

                return result.IsSuccess
                    ? Results.Ok(result.Value)
                    : Results.Problem(result.Error);
            })
            .WithName("GetJobSchedules")
            .RequireAuthorization()
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status500InternalServerError);
    }

    private static void MapGetJobSchedule(RouteGroupBuilder endpoint)
    {
        _ = endpoint.MapGet("/{id}", async (IJobScheduleAppService jobScheduleAppService, CancellationToken ct, Guid id) =>
            {
                var result = await jobScheduleAppService.GetByIdAsync(id, ct);

                return result.IsSuccess
                    ? Results.Ok(result.Value)
                    : Results.NotFound(result.Error);
            })
            .WithName("GetJobSchedule")
            .RequireAuthorization()
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status500InternalServerError);
    }

    private static void MapGetJobScheduleByUserId(RouteGroupBuilder endpoint)
    {
        _ = endpoint.MapGet("/getby-userid/{id}",
            async (IJobScheduleAppService jobScheduleAppService, CancellationToken ct, Guid id) =>
            {
                var result = await jobScheduleAppService.GetByUserIdAsync(id, ct);

                return result.IsSuccess
                    ? Results.Ok(result.Value)
                    : Results.NotFound(result.Error);
            })
            .WithName("GetJobScheduleByUserId")
            .RequireAuthorization()
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status500InternalServerError);
    }

    private static void MapCreateJobSchedule(RouteGroupBuilder endpoint)
    {
        _ = endpoint.MapPost("/",
            async (IJobScheduleAppService jobScheduleAppService, CancellationToken ct,
                [FromBody] JobScheduleViewModel jobScheduleViewModel) =>
            {
                var result = await jobScheduleAppService.AddNewJobAsync(jobScheduleViewModel, ct);

                return result.IsSuccess
                    ? Results.Created($"{basepath}/{result.Value.Id}", result.Value)
                    : Results.Problem(result.Error);
            })
            .WithName("CreateJobSchedule")
            .RequireAuthorization()
            .Produces(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status500InternalServerError);
    }

    private static void MapUpdateJobSchedule(RouteGroupBuilder endpoint)
    {
        _ = endpoint.MapPut("/",
            async (IJobScheduleAppService jobScheduleAppService, CancellationToken ct,
                [FromBody] JobScheduleViewModel jobScheduleViewModel) =>
            {
                var result = await jobScheduleAppService.UpdateAsync(jobScheduleViewModel, ct);

                return result.IsSuccess
                    ? Results.Ok(result.Value)
                    : Results.Problem(result.Error);
            })
            .WithName("UpdateJobSchedule")
            .RequireAuthorization()
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status500InternalServerError);
    }

    private static void MapDeleteJobSchedule(RouteGroupBuilder endpoint)
    {
        _ = endpoint.MapDelete("/",
            async (IJobScheduleAppService jobScheduleAppService, CancellationToken ct,
                [FromBody] JobScheduleViewModel jobScheduleViewModel) =>
            {
                var result = await jobScheduleAppService.RemoveAsync(jobScheduleViewModel, ct);

                return result.IsSuccess
                    ? Results.NoContent()
                    : Results.Problem(result.Error);
            })
            .WithName("DeleteJobSchedule")
            .RequireAuthorization()
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status500InternalServerError);
    }
}
