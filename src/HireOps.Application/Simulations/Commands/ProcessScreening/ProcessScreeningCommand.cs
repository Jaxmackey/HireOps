using HireOps.Domain.Simulations;
using MediatR;

namespace HireOps.Application.Simulations.Commands.ProcessScreening;

public record ProcessScreeningCommand(Guid ApplicantId, Guid TenantId, string Skills) : IRequest;