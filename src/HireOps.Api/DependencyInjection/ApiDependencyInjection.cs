using HireOps.Api.Context;
using HireOps.Api.Services;
using HireOps.Domain.Interfaces;

namespace HireOps.Api.DependencyInjection;

public static class ApiDependencyInjection
{
    public static IServiceCollection AddApi(this IServiceCollection services)
    {
        services.AddHostedService<MetricsBroadcaster>();
        services.AddHttpContextAccessor();
        services.AddScoped<ITenantContext, HttpContextTenantContext>();
        services.AddControllers();
        return services;
    }
}