namespace HireOps.Domain.Interfaces;

public interface ISimulationEngine
{
    Task<int> ProcessWaveAsync(Guid tenantId, int applicantCount, CancellationToken ct);
}