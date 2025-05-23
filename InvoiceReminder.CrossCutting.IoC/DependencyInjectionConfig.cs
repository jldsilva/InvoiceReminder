using InvoiceReminder.Application.Interfaces;
using InvoiceReminder.Authentication.Interfaces;
using InvoiceReminder.Authentication.Jwt;
using InvoiceReminder.Data.Interfaces;
using InvoiceReminder.Data.Persistence;
using InvoiceReminder.Domain.Services.Configuration;
using InvoiceReminder.ExternalServices.BackgroundServices;
using InvoiceReminder.ExternalServices.Gmail;
using InvoiceReminder.JobScheduler.HostedService;
using InvoiceReminder.JobScheduler.JobSettings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Quartz;
using Quartz.Impl;
using Quartz.Spi;
using Scrutor;
using System.Diagnostics.CodeAnalysis;

namespace InvoiceReminder.CrossCutting.IoC;

[ExcludeFromCodeCoverage]
public static class DependencyInjectionConfig
{
    private readonly static string[] _testEnvironments = ["Endpoints_Unit_Tests"];
    private readonly static string _environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        _ = services.AddScoped<IJwtProvider, JwtProvider>();

        _ = services.AddSingleton<IConfigurationBuilder, ConfigurationBuilder>();

        _ = services.AddSingleton<IConfigurationService, ConfigurationService>();

        _ = services.AddHostedService<TelegramBotBackgroundService>();

        _ = services.AddAppServices()
                    .AddDbContext()
                    .AddExternalServices()
                    .AddQuartzJobService()
                    .AddRepositories()
                    .AddScheduledJobs();

        return services;
    }

    private static IServiceCollection AddAppServices(this IServiceCollection services)
    {
        _ = services.Scan(scan =>
            scan.FromAssembliesOf(typeof(IBaseAppService<,>))
                .AddClasses(classes => classes.InNamespaces("InvoiceReminder.Application.AppServices"))
                .UsingRegistrationStrategy(RegistrationStrategy.Skip)
                .AsImplementedInterfaces()
                .WithScopedLifetime()
        );

        return services;
    }

    private static IServiceCollection AddDbContext(this IServiceCollection services)
    {
        _ = _testEnvironments.Contains(_environment)
            ? services.AddDbContext<CoreDbContext>(options => options.UseInMemoryDatabase("TestDb"))
            : services.AddDbContext<CoreDbContext>(options => options.UseNpgsql(services.GetConnectionString()));

        return services;
    }

    private static IServiceCollection AddExternalServices(this IServiceCollection services)
    {
        var namespaces = typeof(IGmailServiceWrapper).Assembly.GetTypes()
            .Where(x => x.Namespace != null && !x.Namespace.Contains("BackgroundService"))
            .Select(x => x.Namespace)
            .Distinct()
            .ToArray();

        _ = services.Scan(scan =>
            scan.FromAssembliesOf(typeof(IGmailServiceWrapper))
                .AddClasses(classes => classes.InNamespaces(namespaces))
                .UsingRegistrationStrategy(RegistrationStrategy.Skip)
                .AsImplementedInterfaces()
                .WithScopedLifetime()
        );

        return services;
    }

    private static IServiceCollection AddQuartzJobService(this IServiceCollection services)
    {
        _ = services.AddHostedService<QuartzHostedService>();
        _ = services.AddSingleton<IJobFactory, JobFactory>();
        _ = services.AddSingleton<ISchedulerFactory, StdSchedulerFactory>();
        _ = services.AddSingleton<CronJob>();

        return services;
    }

    private static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        _ = services.Scan(scan =>
            scan.FromAssembliesOf(typeof(IBaseRepository<>))
                .AddClasses(classes => classes.InNamespaces("InvoiceReminder.Data.Repository"))
                .UsingRegistrationStrategy(RegistrationStrategy.Skip)
                .AsImplementedInterfaces()
                .WithScopedLifetime()
        );

        return services;
    }

    private static IServiceCollection AddScheduledJobs(this IServiceCollection services)
    {
        if (!IsMigrationRunning() && !_testEnvironments.Contains(_environment))
        {
            using var scope = services.BuildServiceProvider().CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IJobScheduleRepository>();
            var jobsList = repository.GetAll();

            foreach (var job in jobsList)
            {
                _ = services.AddSingleton(job);
            }
        }

        return services;
    }

    private static string GetConnectionString(this IServiceCollection services)
    {
        using var scope = services.BuildServiceProvider().CreateScope();
        var cofig = scope.ServiceProvider.GetRequiredService<IConfigurationService>();

        return cofig.GetConnectionString("DatabaseConnection");
    }

    private static bool IsMigrationRunning()
    {
        var args = Environment.GetCommandLineArgs();
        var efcommands = new[] { "migration", "update", "add", "remove", "drop", "list" };

        return args.Any(arg => efcommands.Any(command => arg.Contains(command, StringComparison.CurrentCultureIgnoreCase)));
    }
}
