using InvoiceReminder.API.AuthenticationSetup;
using InvoiceReminder.API.Extensions;
using InvoiceReminder.CrossCutting.IoC;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthentication().AddJwtBearer();
builder.Services.AddAuthorization();
builder.Services.AddExceptionHandler();
builder.Services.AddInfrastructure();
builder.Services.AddOpenApi(opt => opt.AddDocumentTransformer<BearerSecuritySchemeTransformer>());
builder.Services.AddHealthChecks().AddNpgSql
(
    connectionString: builder.Configuration.GetConnectionString("DataBaseConnection"),
    name: "postgres",
    healthQuery: "SELECT 1;",
    tags: ["db", "sql", "critical"],
    failureStatus: HealthStatus.Unhealthy
);

builder.Services.ConfigureOptions<JwtOptionsSetup>();
builder.Services.ConfigureOptions<JwtBearerOptionsSetup>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    _ = app.MapOpenApi();
    _ = app.MapScalarApiReference();
    _ = app.UseHsts();
}

app.RegisterEndpoints();
app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapHealthChecks("/healthz");

await app.RunAsync();
