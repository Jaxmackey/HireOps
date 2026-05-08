using HireOps.Domain.Simulations;
using MediatR;

namespace HireOps.Application.Simulations.Commands.ProcessHrDecision;

public record ProcessHrDecisionCommand(Guid ApplicantId, Guid TenantId, bool Recommended,
    string? WaveId = null) : IRequest;