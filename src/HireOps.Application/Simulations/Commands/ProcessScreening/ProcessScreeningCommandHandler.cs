using System.Diagnostics;
using HireOps.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace HireOps.Application.Simulations.Commands.ProcessScreening;

public class ProcessScreeningCommandHandler(IRabbitMqService rabbitMq, 
    ILogger<ProcessScreeningCommandHandler> logger,
    IProcessingMetricsStore metricsStore,
    IChaosService chaosService)
    : IRequestHandler<ProcessScreeningCommand>
{
    public async Task Handle(ProcessScreeningCommand request, CancellationToken ct)
    {
        logger.LogInformation("🔍 [Tenant:{TenantId}] Screening applicant {ApplicantId}", 
        request.TenantId, request.ApplicantId);
        var stopwatch = Stopwatch.StartNew();
        bool success = false;
        try
        {
            logger.LogInformation("🔍 Screening applicant {ApplicantId}", request.ApplicantId);
            await chaosService.SimulateAsync(ct);
            // 🔹 Имитация бизнес-логики
            await Task.Delay(100, ct);

            // 🔹 Если всё ок — пушим в СЛЕДУЮЩУЮ очередь (Tech Review)
            var nextMessage = new
            {
                request.ApplicantId,
                request.TenantId,
                Score = 85 // Пример скоринга
            };

            await rabbitMq.PublishAsync(
                exchange: "sim.pipeline",
                routingKey: "sim.screening", // Ключ для следующей очереди
                message: nextMessage,
                ct: ct);
            success = true;
            logger.LogInformation("✅ Applicant {ApplicantId} passed screening", request.ApplicantId);
        }
        catch (Exception e)
        {
            logger.LogError(e.Message, request.ApplicantId);
        }
        finally
        {
            stopwatch.Stop();
            metricsStore.Record("sim.received", stopwatch.Elapsed, success);
        }
    }
}