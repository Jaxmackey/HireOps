using HireOps.Api.Context;
using HireOps.Domain.Interfaces;

namespace HireOps.Api.DependencyInjection;

public static class ApiDependencyInjection
{
    public static IServiceCollection AddApi(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ITenantContext, HttpContextTenantContext>();
        services.AddControllers();
        return services;
    }
}