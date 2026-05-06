using System.Diagnostics;
using HireOps.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace HireOps.Application.Simulations.Commands.ProcessHrDecision;

public class ProcessHrDecisionCommandHandler(
    ILogger<ProcessHrDecisionCommandHandler> logger,
    IProcessingMetricsStore metrics,
    IChaosService chaosService)
    : IRequestHandler<ProcessHrDecisionCommand>
{
    public async Task Handle(ProcessHrDecisionCommand request, CancellationToken ct)
    {
        logger.LogInformation("🤝 [Tenant:{TenantId}] HR decision for {ApplicantId}: {Decision}", 
            request.TenantId, request.ApplicantId, request.Recommended ? "HIRED" : "REJECTED");
        await chaosService.SimulateAsync(ct);
        await Task.CompletedTask;
    }
}