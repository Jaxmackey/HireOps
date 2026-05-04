using HireOps.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace HireOps.Application.Simulations.Commands.ProcessScreening;

public class ProcessScreeningCommandHandler(IRabbitMqService rabbitMq, 
    ILogger<ProcessScreeningCommandHandler> logger,
    IChaosService chaosService)
    : IRequestHandler<ProcessScreeningCommand>
{
    public async Task Handle(ProcessScreeningCommand request, CancellationToken ct)
    {
        logger.LogInformation("🔍 Screening applicant {ApplicantId}", request.ApplicantId);
        await chaosService.SimulateAsync(ct);   
        // 🔹 Имитация бизнес-логики
        await Task.Delay(100, ct);
        
        // 🔹 Если всё ок — пушим в СЛЕДУЮЩУЮ очередь (Tech Review)
        var nextMessage = new { 
            request.ApplicantId, 
            request.TenantId, 
            Score = 85 // Пример скоринга
        };
        
        await rabbitMq.PublishAsync(
            exchange: "sim.pipeline",
            routingKey: "sim.screening", // Ключ для следующей очереди
            message: nextMessage,
            ct: ct);
            
        logger.LogInformation("✅ Applicant {ApplicantId} passed screening", request.ApplicantId);
    }
}