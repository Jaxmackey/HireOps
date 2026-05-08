using HireOps.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace HireOps.Application.Simulations.Commands.ProcessTechReview;

public class ProcessTechReviewCommandHandler(
    IRabbitMqService rabbitMq,
    ILogger<ProcessTechReviewCommandHandler> logger,
    IWaveTrackerService waveTracker,
    IChaosService chaosService)
    : IRequestHandler<ProcessTechReviewCommand>
{
    public async Task Handle(ProcessTechReviewCommand request, CancellationToken ct)
    {
        logger.LogInformation("💻 [Tenant:{TenantId}] Tech review for {ApplicantId} (score: {Score}, wave: {WaveId})", 
            request.TenantId, request.ApplicantId, request.Score, request.WaveId ?? "*");
        
        await chaosService.SimulateAsync(ct);
        await Task.Delay(150, ct);
    
        // 🔹 Инкремент прогресса за этап "tech"
        if (!string.IsNullOrEmpty(request.WaveId))
        {
            await waveTracker.IncrementStageProgressAsync(request.WaveId, "sim.tech", ct);
        }
    
        await rabbitMq.PublishAsync(
            exchange: "sim.pipeline",
            routingKey: "sim.tech",
            message: new 
            { 
                request.ApplicantId, 
                request.TenantId, 
                request.WaveId,
                Recommended = request.Score > 50 
            },
            ct: ct);
    }
}