using HireOps.Domain.Interfaces;
using HireOps.Infrastructure.Data;
using HireOps.Infrastructure.Engines;
using HireOps.Infrastructure.Repositories;
using HireOps.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HireOps.Infrastructure;

public static class InfrastructureDependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<AppDbContext>(opt =>
            opt.UseNpgsql(config.GetConnectionString("Default")));

        services.AddSingleton<IRabbitMqService, RabbitMqService>();
        services.AddScoped<ISimulationRepository, SimulationRepository>();
        services.AddSingleton<ISimulationEngine, StubSimulationEngine>();
        
        return services;
    }
}