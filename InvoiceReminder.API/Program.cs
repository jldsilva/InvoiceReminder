using InvoiceReminder.API.AuthenticationSetup;
using InvoiceReminder.API.Extensions;
using InvoiceReminder.API.Middleware;
using InvoiceReminder.CrossCutting.IoC;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthentication().AddJwtBearer();
builder.Services.AddAuthorization();
builder.Services.AddExceptionHandler();
builder.Services.AddInfrastructure();
builder.Services.AddOpenApi(opt => opt.AddDocumentTransformer<BearerSecuritySchemeTransformer>());
builder.Services.ConfigureOptions<JwtOptionsSetup>();
builder.Services.ConfigureOptions<JwtBearerOptionsSetup>();
builder.Services.AddCors(opt =>
    opt.AddPolicy("CorsPolicy", policy =>
    {
        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

        _ = policy
            .AllowAnyMethod()
            .AllowCredentials()
            .WithOrigins(allowedOrigins)
            .WithHeaders("Content-Type", "Authorization");
    })
);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    _ = app.MapOpenApi();
    _ = app.MapScalarApiReference();
    _ = app.UseHsts();
}

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseHttpsRedirection();
app.UseCors("CorsPolicy");
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<UserContextMiddlerware>();
app.RegisterEndpoints();
app.MapHealthChecks("/healthz");

await app.RunAsync();
