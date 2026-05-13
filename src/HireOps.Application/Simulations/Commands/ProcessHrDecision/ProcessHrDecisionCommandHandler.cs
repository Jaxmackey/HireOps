using HireOps.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace HireOps.Application.Simulations.Commands.ProcessHrDecision;

public class ProcessHrDecisionCommandHandler(
    ILogger<ProcessHrDecisionCommandHandler> logger,
    IChaosService chaosService,
    IWaveTrackerService waveTracker)
    : IRequestHandler<ProcessHrDecisionCommand>
{
    public async Task Handle(ProcessHrDecisionCommand request, CancellationToken ct)
    {
        logger.LogInformation("🤝 [Tenant:{TenantId}] HR decision for {ApplicantId}: {Decision} (wave: {WaveId})", 
            request.TenantId, request.ApplicantId, request.Recommended ? "HIRED" : "REJECTED", request.WaveId ?? "*");
        
        await chaosService.SimulateAsync(ct);
    
        
        // 🔹 Инкремент прогресса за финальный этап "hr"
        if (!string.IsNullOrEmpty(request.WaveId))
        {
            await waveTracker.IncrementStageProgressAsync(request.WaveId, "sim.screening", ct);
        }
    
        await Task.CompletedTask;
    }
}