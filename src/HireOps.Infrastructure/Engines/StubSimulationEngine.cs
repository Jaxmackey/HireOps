using HireOps.Domain.Interfaces;

namespace HireOps.Infrastructure.Engines;

public class StubSimulationEngine : ISimulationEngine
{
    public Task<int> ProcessWaveAsync(Guid tenantId, int applicantCount, CancellationToken ct) =>
        Task.FromResult(applicantCount);
}