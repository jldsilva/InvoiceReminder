using InvoiceReminder.Application.Interfaces;
using InvoiceReminder.Application.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace InvoiceReminder.API.Endpoints;

public class JobScheduleEndpoints : IEndpointDefinition
{
    public void RegisterEndpoints(IEndpointRouteBuilder endpoints)
    {
        var basepath = "/api/job_schedule";
        var endpoint = endpoints.MapGroup(basepath).WithName("JobScheduleEndpoints");

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

        _ = endpoint.MapPost("/",
            async (IJobScheduleAppService jobScheduleAppService,
                CancellationToken ct,
                [FromBody] JobScheduleViewModel jobScheduleViewModel) =>
            {
                var result = await jobScheduleAppService.AddNewJobAsync(jobScheduleViewModel, ct);

                return result.IsSuccess
                    ? Results.Created($"/{basepath}/{result.Value.Id}", result.Value)
                    : Results.Problem(result.Error);
            })
            .WithName("CreateJobSchedule")
            .RequireAuthorization()
            .Produces(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status500InternalServerError);

        _ = endpoint.MapPut("/",
            async (IJobScheduleAppService jobScheduleAppService,
                CancellationToken ct,
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

        _ = endpoint.MapDelete("/",
            async (IJobScheduleAppService jobScheduleAppService,
                CancellationToken ct,
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
