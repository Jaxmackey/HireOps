using HireOps.Application.Services;
using HireOps.Application.Workers;
using HireOps.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace HireOps.Application;

public static class ApplicationDependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton<IWorkerManagerService, WorkerManagerService>();
        services.AddSingleton<IProcessingMetricsStore, ProcessingMetricsStore>();
        services.AddSingleton<IChaosService, ChaosService>();
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(ApplicationDependencyInjection).Assembly));
        return services;
    }
}