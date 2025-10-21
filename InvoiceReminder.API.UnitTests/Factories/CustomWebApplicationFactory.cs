using InvoiceReminder.Application.Interfaces;
using InvoiceReminder.Authentication.Interfaces;
using InvoiceReminder.ExternalServices.Gmail;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using NSubstitute;

namespace InvoiceReminder.API.UnitTests.Factories;

public class CustomWebApplicationFactory<TStartup> : WebApplicationFactory<TStartup> where TStartup : class
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Endpoints_Unit_Tests");

        _ = builder.ConfigureServices(services =>
        {
            _ = services.RemoveAll<IAuthorizationService>();
            _ = services.RemoveAll<IGoogleOAuthService>();
            _ = services.RemoveAll<IJwtProvider>();
            _ = services.RemoveAll<IHostedService>();
            _ = services.RemoveAll<IInvoiceAppService>();
            _ = services.RemoveAll<IJobScheduleAppService>();
            _ = services.RemoveAll<IScanEmailDefinitionAppService>();
            _ = services.RemoveAll<IUserAppService>();

            _ = services.AddSingleton(Substitute.For<IAuthorizationService>());
            _ = services.AddSingleton(Substitute.For<IGoogleOAuthService>());
            _ = services.AddSingleton(Substitute.For<IJwtProvider>());
            _ = services.AddSingleton(Substitute.For<IInvoiceAppService>());
            _ = services.AddSingleton(Substitute.For<IJobScheduleAppService>());
            _ = services.AddSingleton(Substitute.For<IScanEmailDefinitionAppService>());
            _ = services.AddSingleton(Substitute.For<IUserAppService>());
        });
    }
}
