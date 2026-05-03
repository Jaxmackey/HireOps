using HireOps.Domain.Interfaces;
using HireOps.Domain.Simulations;
using HireOps.Infrastructure.Data;
using HireOps.Infrastructure.Mappings;
using Microsoft.EntityFrameworkCore;

namespace HireOps.Infrastructure.Repositories;

public class SimulationRepository : ISimulationRepository
{
    private readonly AppDbContext _db;
    public SimulationRepository(AppDbContext db) => _db = db;

    public async Task<Simulation?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken ct)
    {
        var dbModel = await _db.Simulations.FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == id, ct);
        return dbModel?.ToDomain();
    }

    public async Task AddAsync(Simulation simulation, CancellationToken ct)
    {
        var dbModel = InfrastructureMapper.ToDbModel(simulation);
        await _db.Simulations.AddAsync(dbModel, ct);
    }

    public async Task<int> SaveChangesAsync(CancellationToken ct) => await _db.SaveChangesAsync(ct);
}