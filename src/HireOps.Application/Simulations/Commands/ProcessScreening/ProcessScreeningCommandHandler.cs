using System.Diagnostics;
using HireOps.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace HireOps.Application.Simulations.Commands.ProcessScreening;

public class ProcessScreeningCommandHandler(
    IRabbitMqService rabbitMq, 
    ILogger<ProcessScreeningCommandHandler> logger,
    IProcessingMetricsStore metricsStore,
    IWaveTrackerService waveTracker,
    IChaosService chaosService)
    : IRequestHandler<ProcessScreeningCommand>
{
    public async Task Handle(ProcessScreeningCommand request, CancellationToken ct)
    {
        logger.LogInformation("🔍 [Tenant:{TenantId}] Screening applicant {ApplicantId} (wave: {WaveId})", 
            request.TenantId, request.ApplicantId, request.WaveId ?? "*");
        
        var stopwatch = Stopwatch.StartNew();
        bool success = false;
    
        try
        {
            await chaosService.SimulateAsync(ct);
            await Task.Delay(100, ct);

            var nextMessage = new
            {
                request.ApplicantId,
                request.TenantId,
                request.WaveId,
                Score = 85
            };

            await rabbitMq.PublishAsync(
                exchange: "sim.pipeline",
                routingKey: "sim.screening",
                message: nextMessage,
                ct: ct);
            
            success = true;
        
            // 🔹 НОВОЕ: инкремент прогресса за этап "screening"
            if (!string.IsNullOrEmpty(request.WaveId))
            {
                await waveTracker.IncrementStageProgressAsync(request.WaveId, "sim.screening", ct);
            }
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error screening applicant {ApplicantId}", request.ApplicantId);
        }
        finally
        {
            stopwatch.Stop();
            metricsStore.Record("sim.received", stopwatch.Elapsed, success);
        }
    }
}