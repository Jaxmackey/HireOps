using HireOps.Application.Simulations.Commands.ProcessHrDecision;
using HireOps.Application.Simulations.Commands.ProcessScreening;
using HireOps.Application.Simulations.Commands.ProcessTechReview;
using HireOps.Domain.Interfaces;
using HireOps.Domain.Simulations;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HireOps.Application.Consumers;

public class PipelineConsumerService : IHostedService
{
    private readonly IRabbitMqService _rabbitMq;
    private readonly ILogger<PipelineConsumerService> _logger;
    private readonly IServiceProvider _services;
    private CancellationTokenSource? _cts;

    public PipelineConsumerService(
        IRabbitMqService rabbitMq, 
        ILogger<PipelineConsumerService> logger,
        IServiceProvider services)
    {
        _rabbitMq = rabbitMq;
        _logger = logger;
        _services = services;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        
        // 🔹 Запускаем консьюмеры на каждый этап конвейера
        _ = _rabbitMq.ConsumeAndMediateAsync<ApplicantMessage, ProcessScreeningCommand>(
            queue: "sim.received",
            msg => new ProcessScreeningCommand(msg.Id, msg.TenantId, msg.Skills),
            _cts.Token);
            
        _ = _rabbitMq.ConsumeAndMediateAsync<ScreeningResult, ProcessTechReviewCommand>(
            queue: "sim.screening",
            msg => new ProcessTechReviewCommand(msg.ApplicantId, msg.TenantId, msg.Score),
            _cts.Token);
            
        _ = _rabbitMq.ConsumeAndMediateAsync<TechReviewResult, ProcessHrDecisionCommand>(
            queue: "sim.tech",
            msg => new ProcessHrDecisionCommand(msg.ApplicantId, msg.TenantId, msg.Recommended),
            _cts.Token);

        _logger.LogInformation("Pipeline consumers started");
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _cts?.Cancel();
        await _rabbitMq.DisposeAsync();
        _logger.LogInformation("Pipeline consumers stopped");
    }
}