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
        await chaosService.SimulateAsync(ct);
        if (request.Recommended)
        {
            logger.LogInformation("🤝 HR HIRED applicant {ApplicantId}", request.ApplicantId);
        }
        else
        {
            logger.LogInformation("❌ HR REJECTED applicant {ApplicantId}", request.ApplicantId);
        }
        await Task.CompletedTask;
    }
}