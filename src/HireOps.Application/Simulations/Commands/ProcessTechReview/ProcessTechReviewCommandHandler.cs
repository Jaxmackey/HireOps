using HireOps.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace HireOps.Application.Simulations.Commands.ProcessTechReview;

public class ProcessTechReviewCommandHandler(
    IRabbitMqService rabbitMq,
    ILogger<ProcessTechReviewCommandHandler> logger,
    IChaosService chaosService)
    : IRequestHandler<ProcessTechReviewCommand>
{
    public async Task Handle(ProcessTechReviewCommand request, CancellationToken ct)
    {
        logger.LogInformation("💻 Tech review for {ApplicantId} (score: {Score})", request.ApplicantId, request.Score);
        await chaosService.SimulateAsync(ct);
        await Task.Delay(150, ct);
        
        // Пушим в HR очередь
        await rabbitMq.PublishAsync(
            exchange: "sim.pipeline",
            routingKey: "sim.tech",
            message: new { request.ApplicantId, request.TenantId, Recommended = request.Score > 50 },
            ct: ct);
    }
}