using HireOps.Application.Simulations.DTOs;
using MediatR;

namespace HireOps.Application.Simulations.Commands.StartWave;

public record StartWaveCommand(Guid TenantId, int ApplicantCount) : IRequest<SimulationResultDto>;