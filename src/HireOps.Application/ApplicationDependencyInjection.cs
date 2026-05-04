using HireOps.Application.Consumers;
using Microsoft.Extensions.DependencyInjection;

namespace HireOps.Application;

public static class ApplicationDependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddHostedService<PipelineConsumerService>();
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(ApplicationDependencyInjection).Assembly));
        return services;
    }
}