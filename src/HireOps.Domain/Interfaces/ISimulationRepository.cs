using HireOps.Domain.Simulations;

namespace HireOps.Domain.Interfaces;

public interface ISimulationRepository
{
    Task<Simulation?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken ct);
    Task AddAsync(Simulation simulation, CancellationToken ct);
    Task<int> SaveChangesAsync(CancellationToken ct);
}