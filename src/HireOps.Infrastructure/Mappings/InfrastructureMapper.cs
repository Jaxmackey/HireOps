using HireOps.Domain.Simulations;
using HireOps.Infrastructure.Models;

namespace HireOps.Infrastructure.Mappings;

public static class InfrastructureMapper
{
    public static SimulationDbModel ToDbModel(Simulation domain) => new()
    {
        Id = domain.Id,
        TenantId = domain.TenantId,
        State = domain.State.ToString(),
        CreatedAt = domain.CreatedAt,
        ProcessedCount = domain.ProcessedCount
    };

    public static Simulation ToDomain(this SimulationDbModel db) =>
        new(db.TenantId) 
        { 
            Id = db.Id, 
            State = Enum.Parse<SimulationState>(db.State), 
            CreatedAt = db.CreatedAt, 
            ProcessedCount = db.ProcessedCount 
        };
}