using HireOps.Api.Contracts;
using HireOps.Application.Simulations.Commands;
using HireOps.Application.Simulations.DTOs;

namespace HireOps.Api.Mappings;

public static class ApiMapper
{
    public static StartWaveCommand ToCommand(int applicantCount, Guid tenantId) =>
        new(tenantId, applicantCount);

    public static SimulationResponse ToResponse(this SimulationResultDto dto) =>
        new(dto.Id, dto.State, dto.ProcessedCount, dto.CreatedAt);
}