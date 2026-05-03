using HireOps.Application.Simulations.DTOs;
using HireOps.Domain.Simulations;

namespace HireOps.Application.Simulations.Mappings;

public static class ApplicationMapper
{
    public static SimulationResultDto ToDto(this Simulation domain) =>
        new(domain.Id, domain.State.ToString(), domain.ProcessedCount, domain.CreatedAt);
}