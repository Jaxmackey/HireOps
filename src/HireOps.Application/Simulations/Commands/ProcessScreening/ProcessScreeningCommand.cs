using MediatR;

namespace HireOps.Application.Simulations.Commands.ProcessScreening;

public record ProcessScreeningCommand(Guid ApplicantId, Guid TenantId, string Skills,
    string? WaveId = null) : IRequest;