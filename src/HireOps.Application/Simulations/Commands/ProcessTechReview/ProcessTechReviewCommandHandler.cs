using HireOps.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace HireOps.Application.Simulations.Commands.ProcessTechReview;

public class ProcessTechReviewCommandHandler : IRequestHandler<ProcessTechReviewCommand>
{
    private readonly IRabbitMqService _rabbitMq;
    private readonly ILogger<ProcessTechReviewCommandHandler> _logger;

    public ProcessTechReviewCommandHandler(IRabbitMqService rabbitMq, ILogger<ProcessTechReviewCommandHandler> logger)
    {
        _rabbitMq = rabbitMq;
        _logger = logger;
    }

    public async Task Handle(ProcessTechReviewCommand request, CancellationToken ct)
    {
        _logger.LogInformation("💻 Tech review for {ApplicantId} (score: {Score})", request.ApplicantId, request.Score);
        await Task.Delay(150, ct);
        
        // Пушим в HR очередь
        await _rabbitMq.PublishAsync(
            exchange: "sim.pipeline",
            routingKey: "sim.tech",
            message: new { request.ApplicantId, request.TenantId, Recommended = request.Score > 50 },
            ct: ct);
    }
}